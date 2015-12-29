﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.ComponentModel;
using RssToolkit.Rss;
using System.Xml;
using System.Globalization;
using OnlineVideos._3rdParty.Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class CCUtil : GenericSiteUtil
    {
        [Category("OnlineVideosUserConfiguration"), DefaultValue(false), Description("Whether to download subtitles"), LocalizableDisplayName("Retrieve Subtitles")]
        protected bool retrieveSubtitles;

       // private const string showsURL = @"http://www.cc.com/feeds/ent_m081_cc/2.0/4043f1d9-d18f-48a3-89e0-68acad5236f1"; //@"http://www.cc.com/feeds/ent_m069_cc/1.0/5ab40787-7d35-4449-84eb-efadc941cd34";

        private int vidListPage = 1;
        private int vidListCount = 0;

        public override int DiscoverDynamicCategories()
        {
            List<Category> dynamicCategories = new List<Category>(); // put all new discovered Categories in a separate list

            JObject jsonData = GetWebData<JObject>(baseUrl);

            if (jsonData != null)
            {
                Log.Debug("Got baseUrl");
                JToken shows = jsonData["result"]["shows"];
                foreach (JToken show in shows)
                {
                    Log.Debug(show["show"].Value<string>("title"));
                    string totalEps = show.Value<string>("totalEpisodes");

                    RssLink cat = new RssLink();
                    cat.Url = show.Value<string>("fullEpisodesFeedURL");
                    cat.Name = show["show"].Value<string>("title") + " (" + totalEps + ")";
                    cat.Thumb = String.Format("{0}?quality=0.85&width=400&height=400&crop=true", show["show"]["images"][0].Value<string>("url"));
                    if (!String.IsNullOrEmpty(cat.Thumb) && !Uri.IsWellFormedUriString(cat.Thumb, System.UriKind.Absolute)) cat.Thumb = new Uri(new Uri(baseUrl), cat.Thumb).AbsoluteUri;
                    cat.Description = show["show"].Value<string>("description");
                    cat.HasSubCategories = false;
                    cat.Other = new ShowInfo{ id = (string)show["show"].Value<string>("id"), totalEpisodes = totalEps, feedUrl = show.Value<string>("fullEpisodesFeedURL") };
                    dynamicCategories.Add(cat);
                }

                // discovery finished, copy them to the actual list -> prevents double entries if error occurs in the middle of adding
                if (Settings.Categories == null) Settings.Categories = new BindingList<Category>();
                foreach (Category cat in dynamicCategories) Settings.Categories.Add(cat);
                Settings.DynamicCategoriesDiscovered = dynamicCategories.Count > 0; // only set to true if actually discovered (forces re-discovery until found)
            }
            return dynamicCategories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            ShowInfo info = (ShowInfo)category.Other;
            string url = (category as RssLink).Url;

            /*string manUrl = getManifestFromUrl(url);

            var jsonData = GetWebData<JObject>(manUrl);

            string manId = jsonData["manifest"].Value<string>("id");
            string showId = info.id;
            Log.Debug("ShowId: " + showId);
            Log.Debug("ManID: " + manId);
            string reportingId = jsonData["manifest"]["reporting"].Value<string>("itemId");
            Log.Debug("reportingID: " + reportingId);
            
            JEnumerable<JProperty> zones = jsonData["manifest"]["zones"].Children<JProperty>();
            string feed = "";
            foreach (JProperty zone in zones)
            {
                feed = (string)zone.Value["feed"];
                if (feed.Contains(showId))
                    break;
            }*/
            string epsUrl = url;
            vidListPage = 1; //reset vidListPage
            vidListCount = 0;

            return Parse(epsUrl, info.totalEpisodes);
        }

        protected override List<VideoInfo> Parse(string url, string data)
        {
            JObject jsonEpsData = GetWebData<JObject>(url);

            List<VideoInfo> videoList = new List<VideoInfo>();

            //int totalEpisodes = Convert.ToInt32(data);

            if (jsonEpsData != null)
            {
                JToken episodes = jsonEpsData["result"]["episodes"];
                foreach (var ep in episodes)
                {
                    string preTitle = "";
                    if (ep.Value<bool>("showEpisodeNumber") == true)
                        preTitle = String.Format("S{0:00}E{1:00} - ", ep["season"].Value<int>("seasonNumber"), ep.Value<int>("pureNumber"));

                    DateTime airdate = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(Convert.ToDouble(ep.Value<string>("airDate")));
                    VideoInfo videoInfo = CreateVideoInfo();
                    videoInfo.Title = preTitle + ep.Value<string>("title");
                    videoInfo.VideoUrl = String.Format("http://www.cc.com/feeds/mrss?uri=mgid:arc:episode:comedycentral.com:{0}", ep.Value<string>("id"));
                    videoInfo.Thumb = String.Format("{0}?quality=0.85&width=560&height=315&crop=true", ep["images"][0].Value<string>("url"));
                    videoInfo.Length = ep.Value<string>("duration");
                    videoInfo.Airdate = String.Format("{0}, {1} {2}", airdate.ToString("dddd"), airdate.ToShortDateString(), airdate.ToShortTimeString());
                    videoInfo.Description = "Views: " + ep.Value<string>("views") + "\n" + ep.Value<string>("description");
                    videoList.Add(videoInfo);
                }
                vidListCount += videoList.Count;

                nextPageUrl = jsonEpsData["result"].Value<string>("nextPageURL");
                Log.Debug("NextPage: " + nextPageUrl);

                nextPageAvailable = !string.IsNullOrEmpty(nextPageUrl);
            }
            else
                return null;

            vidListPage++;

            return videoList;
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            return Parse(nextPageUrl, null);
        }
        

        public string getManifestFromUrl(string url)
        {
            if (url.Contains("southpark"))
                url = "http://www.cc.com/shows/south-park/";
            else
            {
                if (url[url.Length - 1] != '/')
                    url += "/";
            }

            string baseUri = new Uri(url).GetLeftPart(System.UriPartial.Authority);
            string fullEpUrl = String.Format("{0}/feeds/triforce/manifest/v3?url={1}full-episodes", baseUri, url);
            var redirectJson = GetWebData<JObject>(fullEpUrl);

            string redirectLocation;
            try
            {
                redirectLocation = redirectJson["manifest"]["newLocation"].Value<string>("url");
            }
            catch
            {
                redirectLocation = redirectJson["manifest"].Value<string>("newLocation");
            }
            

            return String.Format("{0}/feeds/triforce/manifest/v3?url={1}", baseUri, redirectLocation);
        }


        public override List<String> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            List<string> result = new List<string>();

            string data = GetWebData(video.VideoUrl);

            if (!string.IsNullOrEmpty(data))
            {
                data = data.Replace("&amp;", "&");
                data = data.Replace("&", "&amp;");
                foreach (RssItem item in RssToolkit.Rss.RssDocument.Load(data).Channel.Items)
                {
                    PlaybackOptions vidopts = getPlaybackOptions(item.MediaGroups[0].MediaContents[0].Url);
                    Log.Debug("Load: " + item.Title);
                    if (video.PlaybackOptions == null)
                    {
                        video.PlaybackOptions = vidopts.videoSrc;
                        if (retrieveSubtitles)
                            video.SubtitleText = Helpers.SubtitleUtils.Webvtt2SRT(GetWebData(vidopts.subtitleSrc["vtt"]));

                    }
                    result.Add(item.MediaGroups[0].MediaContents[0].Url);
                }
            }

            return result;
        }

        PlaybackOptions getPlaybackOptions(string videoUrl)
        {
            string data = GetWebData(videoUrl);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(data);

            PlaybackOptions vidopts = new PlaybackOptions();

            XmlNodeList list = doc.SelectNodes("//rendition");
            XmlNodeList sublist = doc.SelectNodes("//typographic");
            
            foreach (XmlNode subtitle in sublist)
            {
                string subFormat = subtitle.Attributes["format"].Value;
                string subSrc = subtitle.Attributes["src"].Value;
                vidopts.subtitleSrc.Add(subFormat, subSrc);
                Log.Debug("Subtitle url: " + subFormat + " : " + subSrc);
            }

            for (int i = list.Count-1; i >=0 ; i--)
            {
                Log.Debug("Get resolution information " + i.ToString());
                string bitrate = list[i].Attributes["bitrate"].Value;
                string videoType = list[i].Attributes["type"].Value.Replace(@"video/", String.Empty);
                string url = list[i].FirstChild.InnerText;
                string resolution = "";
                Regex resRegex = new Regex(@"_([\d]+x[\d]+)_");
                Match m = resRegex.Match(url);
                if (m.Success)
                    resolution = m.Groups[1].Captures[0].Value;

                url = url.Replace(@"viacomccstrmfs.fplive.net/viacomccstrm", @"viacommtvstrmfs.fplive.net/viacommtvstrm");
                string br = bitrate + "K " + resolution + " " + videoType;
                if (!vidopts.videoSrc.ContainsKey(br))
                    vidopts.videoSrc.Add(br, new MPUrlSourceFilter.RtmpUrl(url) { SwfVerify = false, SwfUrl = null }.ToString());
            }

            return vidopts;
        }

        public override string GetPlaylistItemVideoUrl(VideoInfo clonedVideoInfo, string chosenPlaybackOption, bool inPlaylist = false)
        {
            if (String.IsNullOrEmpty(chosenPlaybackOption))
                return clonedVideoInfo.VideoUrl;

            PlaybackOptions options = getPlaybackOptions(clonedVideoInfo.VideoUrl);

            //set subtitle for part
            if (retrieveSubtitles)
                clonedVideoInfo.SubtitleText = Helpers.SubtitleUtils.Webvtt2SRT(GetWebData(options.subtitleSrc["vtt"]));

            if (options.videoSrc.ContainsKey(chosenPlaybackOption))
            {
                return options.videoSrc[chosenPlaybackOption];
            }
            var enumerator = options.videoSrc.GetEnumerator();
            enumerator.MoveNext();
            return enumerator.Current.Value;
        }
    }

    public class PlaybackOptions
    {
        public Dictionary<string, string> subtitleSrc { get; set;}
        public Dictionary<string, string> videoSrc { get; set; }

        public PlaybackOptions()
        {
            subtitleSrc = new Dictionary<string,string>();
            videoSrc = new Dictionary<string,string>();
        }
    }

    public class ShowInfo
    {
        public string id {get; set;}
        public string totalEpisodes { get; set; }
        public string feedUrl { get; set; }
    }
}

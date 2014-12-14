using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using RssToolkit.Rss;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class CCUtil : GenericSiteUtil
    {
        private const string showsURL = @"http://www.cc.com/feeds/ent_m069_cc/1.0/5ab40787-7d35-4449-84eb-efadc941cd34";

        public override int DiscoverDynamicCategories()
        {
            List<Category> dynamicCategories = new List<Category>(); // put all new discovered Categories in a separate list

            JObject jsonData = GetWebData<JObject>(baseUrl);

            if (jsonData != null)
            {
                JToken shows = jsonData["result"]["shows"];


                //Log.Debug("Number of shows" + shows);

                foreach (JToken show in shows)
                {
                    Log.Debug(show.Value<string>("title") + ": " + show.Value<string>("canonicalURL"));

                    RssLink cat = new RssLink();
                    cat.Url = show.Value<string>("canonicalURL");
                    cat.Name = show.Value<string>("title");
                    cat.Thumb = String.Format("{0}?quality=0.85&width=400&height=400&crop=true", show["images"][0].Value<string>("url"));
                    if (!String.IsNullOrEmpty(cat.Thumb) && !Uri.IsWellFormedUriString(cat.Thumb, System.UriKind.Absolute)) cat.Thumb = new Uri(new Uri(baseUrl), cat.Thumb).AbsoluteUri;
                    cat.Description = show.Value<string>("description");
                    cat.HasSubCategories = false;
                    cat.Other = (string)show.Value<string>("id");
                    dynamicCategories.Add(cat);
                }

                // discovery finished, copy them to the actual list -> prevents double entries if error occurs in the middle of adding
                if (Settings.Categories == null) Settings.Categories = new BindingList<Category>();
                foreach (Category cat in dynamicCategories) Settings.Categories.Add(cat);
                Settings.DynamicCategoriesDiscovered = dynamicCategories.Count > 0; // only set to true if actually discovered (forces re-discovery until found)
            }
            return dynamicCategories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string url = (category as RssLink).Url;
            string manUrl = getManifestFromUrl(url);
            Log.Debug("manUrl: " + manUrl);

            var jsonData = GetWebData<JObject>(manUrl);
            string showId = (string)category.Other;
            string reportingId = jsonData["manifest"]["reporting"].Value<string>("itemId");
            Log.Debug("ShowID: " + showId + ", reportingID: " + reportingId);

            string epsUrl = String.Format("http://www.cc.com/feeds/f1010/1.0/5a123a71-d8b9-45d9-85d5-e85508b1b37c/{0}/{1}/1", showId, reportingId);
            Log.Debug("epsUrl: " + epsUrl);

            JObject jsonEpsData = GetWebData<JObject>(epsUrl);

            List<VideoInfo> videoList = new List<VideoInfo>();

            if (jsonEpsData != null)
            {
                JToken episodes = jsonEpsData["result"]["episodes"];
                foreach (var ep in episodes)
                {
                    DateTime airdate = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(Convert.ToDouble(ep.Value<string>("airDate")));
                    VideoInfo videoInfo = CreateVideoInfo();
                    videoInfo.Title = String.Format("{0} - {1}", ep.Value<string>("pureNumber"), ep.Value<string>("title"));
                    videoInfo.VideoUrl = String.Format("http://www.cc.com/feeds/mrss?uri=mgid:arc:episode:comedycentral.com:{0}", ep.Value<string>("id"));
                    videoInfo.ImageUrl = String.Format("{0}?quality=0.85&width=560&height=315&crop=true", ep["images"][0].Value<string>("url"));
                    videoInfo.Length = ep.Value<string>("duration");
                    videoInfo.Airdate = airdate.ToString("dddd, ") + airdate.ToShortDateString();
                    videoInfo.Description = "Views: " + ep.Value<string>("views") + "\n" + ep.Value<string>("description");
                    videoList.Add(videoInfo);
                }
            }

            return videoList;
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


        public override List<String> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            List<string> result = new List<string>();

            string data = GetWebData(video.VideoUrl);

            if (!string.IsNullOrEmpty(data))
            {
                data = data.Replace("&amp;", "&");
                data = data.Replace("&", "&amp;");
                foreach (RssItem item in RssToolkit.Rss.RssDocument.Load(data).Channel.Items)
                {
                    Log.Debug("Load: " + item.Title);
                    if (video.PlaybackOptions == null)
                        video.PlaybackOptions = getPlaybackOptions(item.MediaGroups[0].MediaContents[0].Url);
                    result.Add(item.MediaGroups[0].MediaContents[0].Url);
                }
            }

            return result;
        }

        Dictionary<string, string> getPlaybackOptions(string videoUrl)
        {
            Dictionary<string, string> res = new Dictionary<string, string>();

            string data = GetWebData(videoUrl);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(data);

            XmlNodeList list = doc.SelectNodes("//src");

            for (int i = list.Count-1; i >=0 ; i--)
            {
                string bitrate = list[i].ParentNode.Attributes["bitrate"].Value;
                string videoType = list[i].ParentNode.Attributes["type"].Value.Replace(@"video/", String.Empty);
                string url = list[i].InnerText;
                string resolution = "";
                Regex resRegex = new Regex(@"_([\d]+x[\d]+)_" + bitrate + "_");
                Match m = resRegex.Match(url);
                if (m.Success)
                    resolution = m.Groups[1].Captures[0].Value;

                url = url.Replace(@"viacomccstrmfs.fplive.net/viacomccstrm", @"viacommtvstrmfs.fplive.net/viacommtvstrm");
                string br = bitrate + "K " + resolution + " " + videoType;
                if (!res.ContainsKey(br))
                    res.Add(br, new MPUrlSourceFilter.RtmpUrl(url) { SwfVerify = false, SwfUrl = null }.ToString());
            }
            return res;
        }

        public override string getPlaylistItemUrl(VideoInfo clonedVideoInfo, string chosenPlaybackOption, bool inPlaylist = false)
        {
            if (String.IsNullOrEmpty(chosenPlaybackOption))
                return clonedVideoInfo.VideoUrl;

            Dictionary<string, string> options = getPlaybackOptions(clonedVideoInfo.VideoUrl);
            if (options.ContainsKey(chosenPlaybackOption))
            {
                return options[chosenPlaybackOption];
            }
            var enumerator = options.GetEnumerator();
            enumerator.MoveNext();
            return enumerator.Current.Value;
        }
    }
}

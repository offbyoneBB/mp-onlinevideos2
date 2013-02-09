using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using RssToolkit.Rss;

namespace OnlineVideos.Sites
{
    public class SouthParkUtil : GenericSiteUtil
    {
        Regex episodePlayerRegEx = new Regex(@"swfobject.embedSWF\(""(?<url>[^""]*)""", RegexOptions.Compiled);

        public override int DiscoverDynamicCategories()
        {
            int res = base.DiscoverDynamicCategories();
            foreach (Category cat in Settings.Categories)
                cat.Name = "Season " + cat.Name;
            return res;
        }

        protected override void ExtraVideoMatch(VideoInfo video, GroupCollection matchGroups)
        {

            TrackingInfo ti = new TrackingInfo();

            // for southpark world
            System.Text.RegularExpressions.Group epGroup = matchGroups["Episode"];
            if (epGroup.Success)
                ti.Regex = Regex.Match(epGroup.Value, @"(?<Season>\d\d)(?<Episode>\d\d)");

            // for nl and de
            if (ti.Season == 0)
                ti.Regex = Regex.Match(video.VideoUrl, @"\/S(?<Season>\d{1,3})E(?<Episode>\d{1,3})-", RegexOptions.IgnoreCase);

            if (ti.Season != 0)
            {
                ti.Title = "South Park";
                ti.VideoKind = VideoKind.TvSeries;
                video.Other = new VideoInfoOtherHelper() { TI = ti };
            }

            video.Other = new VideoInfoOtherHelper();
        }

        private enum SouthParkCountry { Unknown, World, Nl, De };

        public override List<String> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            List<string> result = new List<string>();

            string data = GetWebData(video.VideoUrl);
            if (!string.IsNullOrEmpty(data))
            {
                Match m = episodePlayerRegEx.Match(data);
                if (m.Success)
                {
                    string playerUrl = m.Groups["url"].Value;
                    playerUrl = GetRedirectedUrl(playerUrl);
                    playerUrl = System.Web.HttpUtility.ParseQueryString(new Uri(playerUrl).Query)["uri"];
                    SouthParkCountry spc = SouthParkCountry.Unknown;
                    if (video.VideoUrl.Contains("southparkstudios.com"))
                        spc = SouthParkCountry.World;
                    else if (video.VideoUrl.ToLower().Contains(".de") || video.VideoUrl.ToLower().Contains("de."))
                        spc = SouthParkCountry.De;
                    else if (video.VideoUrl.Contains("southpark.nl"))
                        spc = SouthParkCountry.Nl;
                    if (spc == SouthParkCountry.World || spc == SouthParkCountry.Nl || spc == SouthParkCountry.De)
                    {
                        playerUrl = System.Web.HttpUtility.UrlEncode(playerUrl);
                        playerUrl = new Uri(new Uri(baseUrl), @"/feeds/video-player/mrss/" + playerUrl).AbsoluteUri;
                    }
                    else
                    {
                        playerUrl = System.Web.HttpUtility.UrlDecode(playerUrl);
                        playerUrl = new Uri(new Uri(baseUrl), @"/feeds/as3player/mrss.php?uri=" + playerUrl).AbsoluteUri;
                    }
                    //http://www.southparkstudios.com/feeds/as3player/mrss.php?uri=mgid:cms:content:southparkstudios.com:164823
                    //http://www.southparkstudios.com/feeds/video-player/mrss/mgid%3Acms%3Acontent%3Asouthparkstudios.com%3A164823

                    data = GetWebData(playerUrl);
                    if (!string.IsNullOrEmpty(data))
                    {
                        data = data.Replace("&amp;", "&");
                        data = data.Replace("&", "&amp;");
                        (video.Other as VideoInfoOtherHelper).SPCountry = spc;
                        foreach (RssItem item in RssToolkit.Rss.RssDocument.Load(data).Channel.Items)
                        {
                            if (item.Title.ToLowerInvariant().Contains("intro") || item.Title.ToLowerInvariant().Contains("vorspann")) continue;
                            if (video.PlaybackOptions == null)
                                video.PlaybackOptions = getPlaybackOptions(item.MediaGroups[0].MediaContents[0].Url, spc);
                            result.Add(item.MediaGroups[0].MediaContents[0].Url);
                        }
                    }
                }
            }
            return result;
        }

        Dictionary<string, string> getPlaybackOptions(string videoUrl, SouthParkCountry spc)
        {
            Dictionary<string, string> res = new Dictionary<string, string>();

            string data = GetWebData(videoUrl);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(data);

            XmlNodeList list = doc.SelectNodes("//src");
            for (int i = 0; i < list.Count; i++)
            {
                string bitrate = list[i].ParentNode.Attributes["bitrate"].Value;
                string videoType = list[i].ParentNode.Attributes["type"].Value.Replace(@"video/", String.Empty);
                string url = list[i].InnerText;

                string swfUrl = null;
                if (spc == SouthParkCountry.World)
                    url = url.Replace(@"viacomspstrmfs.fplive.net/viacomspstrm", @"cp10740.edgefcs.net/ondemand/mtvnorigin");
                /*switch (spc)
                {
                    case SouthParkCountry.World:
                    case SouthParkCountry.De: 
                        swfUrl = @"http://media.mtvnservices.com/player/prime/mediaplayerprime.1.11.3.swf"; break;
                    //case SouthParkCountry.Nl: swfUrl = String.Empty; break;
                }*/
                string br = bitrate + "K " + videoType;
                if (!res.ContainsKey(br))
                    res.Add(br, new MPUrlSourceFilter.RtmpUrl(url) { SwfVerify = swfUrl != null, SwfUrl = swfUrl }.ToString());

            }
            return res;
        }

        public override string getPlaylistItemUrl(VideoInfo clonedVideoInfo, string chosenPlaybackOption, bool inPlaylist = false)
        {
            if (String.IsNullOrEmpty(chosenPlaybackOption))
                return clonedVideoInfo.VideoUrl;

            Dictionary<string, string> options = getPlaybackOptions(clonedVideoInfo.VideoUrl, (clonedVideoInfo.Other as VideoInfoOtherHelper).SPCountry);
            if (options.ContainsKey(chosenPlaybackOption))
            {
                return options[chosenPlaybackOption];
            }
            var enumerator = options.GetEnumerator();
            enumerator.MoveNext();
            return enumerator.Current.Value;
        }

        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            return (video.Other as VideoInfoOtherHelper).TI == null ? base.GetTrackingInfo(video) : (video.Other as VideoInfoOtherHelper).TI;
        }

        private class VideoInfoOtherHelper
        {
            public SouthParkCountry SPCountry = SouthParkCountry.Unknown;
            public TrackingInfo TI = null;

            public VideoInfoOtherHelper()
            {
            }

            public VideoInfoOtherHelper(SouthParkCountry spCountry, TrackingInfo trackingInfo)
            {
                this.SPCountry = spCountry;
                this.TI = trackingInfo;
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

using OnlineVideos.AMF;
using OnlineVideos.MPUrlSourceFilter;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Site Utility for tvokids.com
    /// </summary>
    public class TVOKidsUtil : GenericSiteUtil
    {
        private static string baseUrlPrefix = @"http://www.tvokids.com";
        private static string mainCategoriesUrl = baseUrlPrefix + "/feeds/all/{0}/shows";
        private static string videoListUrl = baseUrlPrefix + @"/feeds/{0}/all/videos_list.xml?random={1}";
        // following were found by looking at AMF POST requests using Firebug/Flashbug
        private static string hashValue = @"466faf0229239e70a6df8fe66fc04f25f50e6fa7";
        private static string playerId = @"48543011001";
        private static string publisherId = @"15364602001";
        private static string brightcoveUrl = @"http://c.brightcove.com/services/messagebroker/amf?playerId=" + playerId;
        
        private static Regex rtmpUrlRegex = new Regex(@"(?<rtmp>rtmpe?)://(?<host>[^/]+)/(?<app>[^&]*)&(?<leftover>.*)", RegexOptions.Compiled);
        
        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            Settings.Categories.Add(
                new RssLink() { Name = "Ages 2 to 5", Url = String.Format(mainCategoriesUrl, "97"), HasSubCategories = true }
               );
            Settings.Categories.Add(
                new RssLink() { Name = "Ages 11 and under", Url = String.Format(mainCategoriesUrl, "98"), HasSubCategories = true }
               );
            
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }
        
        public override int DiscoverSubCategories(Category parentCategory)
        {
            long epoch = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
            parentCategory.SubCategories = new List<Category>();

            string url = ((RssLink) parentCategory).Url;
            XmlDocument xml = GetWebData<XmlDocument>(url);
            
            foreach (XmlNode node in xml.SelectNodes(@"//node"))
            {
                RssLink cat = new RssLink();
                cat.ParentCategory = parentCategory;
                // TODO: remove HTML markup
                cat.Name = node.SelectSingleNode("./node_title").InnerText;
                cat.Url = string.Format(videoListUrl, node.SelectSingleNode("./node_id").InnerText, epoch);
                cat.Description = node.SelectSingleNode("./node_short_description").InnerText;
                cat.HasSubCategories = false;
                Log.Debug("cat: {0}", cat);
                parentCategory.SubCategories.Add(cat);
            }
            
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }
        
        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            XmlDocument xml = GetWebData<XmlDocument>(((RssLink ) category).Url);
            
            foreach (XmlNode node in xml.SelectNodes(@"//node"))
            {
                result.Add(new VideoInfo() {
                               VideoUrl = node.SelectSingleNode("./node_bc_id").InnerText,
                               ImageUrl = node.SelectSingleNode("./node_thumbnail").InnerText,
                               Title = node.SelectSingleNode("./node_title").InnerText,
                               Description = node.SelectSingleNode("./node_short_description").InnerText
                           });
            }
            
            return result;
        }
        
        public override string getUrl(VideoInfo video)
        {
            /*
             * sample AMF input (expressed as JSON)
             * ["466faf0229239e70a6df8fe66fc04f25f50e6fa7",48543011001,1401332946001,15364602001]
             */
            string result = string.Empty;
            
            string videoId = video.VideoUrl;
            
            object[] values = new object[4];
            values[0] = hashValue;
            values[1] = Convert.ToDouble(playerId);
            values[2] = Convert.ToDouble(videoId);
            values[3] = Convert.ToDouble(publisherId);

            AMFSerializer serializer = new AMFSerializer();
            AMFObject response = AMFObject.GetResponse(brightcoveUrl, serializer.Serialize2("com.brightcove.player.runtime.PlayerMediaFacade.findMediaById", values));

            AMFArray renditions = response.GetArray("renditions");

            video.PlaybackOptions = new Dictionary<string, string>();
            // keep track of bitrates and URLs
            Dictionary<string, string> urlsDictionary = new Dictionary<string, string>();

            for (int i = 0; i < renditions.Count; i++)
            {
                AMFObject rendition = renditions.GetObject(i);
                int bitrate = rendition.GetIntProperty("encodingRate");
                string optionKey = String.Format("{0}x{1} {2}K",
                    rendition.GetIntProperty("frameWidth"),
                    rendition.GetIntProperty("frameHeight"),
                    bitrate / 1024);
                string url = HttpUtility.UrlDecode(rendition.GetStringProperty("defaultURL"));
                Log.Debug("Option: {0} URL: {1}", optionKey, url);

                // typical rtmp url from "defaultURL" is:
                // rtmp://brightcove.fcod.llnwd.net/a500/d20/&mp4:media/1351824783/1351824783_1411974095001_109856X-640x360-1500k.mp4&1330020000000&1b163106256f448754aff72969869479
                // 
                // following rtmpdump style command works
                // rtmpdump 
                // --app 'a500/d20/?videoId=1411956407001&lineUpId=&pubId=18140038001&playerId=756015080001&playerTag=&affiliateId='
                // --auth 'mp4:media/1351824783/1351824783_1411974097001_109856X-400x224-300k.mp4&1330027200000&23498dd8f4659cd07ad1b6c4ee5a013d'
                // --rtmp 'rtmp://brightcove.fcod.llnwd.net/a500/d20/?videoId=1411956407001&lineUpId=&pubId=18140038001&playerId=756015080001&playerTag=&affiliateId='
                // --flv 'TVO_org_-_Safe_as_Houses.flv'
                // --playpath 'mp4:media/1351824783/1351824783_1411974097001_109856X-400x224-300k.mp4'
                
                Match rtmpUrlMatch = rtmpUrlRegex.Match(url);
                
                if (rtmpUrlMatch.Success)
                {
                    string query = String.Format(@"videoId={0}&lineUpId=&pubId={1}&playerId={2}&playerTag=&affiliateId=",
                                                videoId, publisherId, playerId);
                    string app = String.Format("{0}?{1}", rtmpUrlMatch.Groups["app"], query);
                    string auth = rtmpUrlMatch.Groups["leftover"].Value;
                    string rtmpUrl = String.Format("{0}://{1}/{2}?{3}",
                                                   rtmpUrlMatch.Groups["rtmp"].Value,
                                                   rtmpUrlMatch.Groups["host"].Value,
                                                   rtmpUrlMatch.Groups["app"].Value,
                                                   query);
                    string leftover = rtmpUrlMatch.Groups["leftover"].Value;
                    string playPath = leftover.Substring(0, leftover.IndexOf('&'));;
                    Log.Debug(@"rtmpUrl: {0}, PlayPath: {1}, App: {2}, Auth: {3}", rtmpUrl, playPath, app, auth);
                    urlsDictionary.Add(optionKey, new RtmpUrl(rtmpUrl) {
                                                 PlayPath = playPath,
                                                 App = app,
                                                 Auth = auth
                                              }.ToString());
                }
            }
            
            // sort the URLs ascending by bitrate
            foreach (var item in urlsDictionary.OrderBy(u => u.Key, new BitrateComparer()))
            {
                video.PlaybackOptions.Add(item.Key, item.Value);
                // return last URL as the default (will be the highest bitrate)
                result = item.Value;
            }
            return result;
        }
    }
    
    class BitrateComparer : IComparer<string>
    {
        private static Regex bitrateRegex = new Regex(@"\d+x\d+\s(?<bitrate>\d+)K", RegexOptions.Compiled);

        public int Compare(string x, string y)
        {
            int xKbps = 0, yKbps = 0;
            Match match;
            match = bitrateRegex.Match(x);
            if (match.Success && !int.TryParse(match.Groups["bitrate"].Value, out xKbps)) return 1;
            match = bitrateRegex.Match(y);
            if (match.Success && !int.TryParse(match.Groups["bitrate"].Value, out yKbps)) return -1;
            return xKbps.CompareTo(yKbps);
        }
    }
}

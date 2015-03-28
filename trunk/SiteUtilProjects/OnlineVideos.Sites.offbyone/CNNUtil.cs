using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System;

namespace OnlineVideos.Sites
{
    public class CNNUtil : GenericSiteUtil
    {
        public override Dictionary<string, string> GetPlaybackOptions(string playlistUrl)
        {
            string dataPage = GetWebData<string>(playlistUrl, cookies: GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);
            Dictionary<string, string> playbackOptions = new Dictionary<string, string>();
            var matchFileUrl = regEx_FileUrl.Match(dataPage);
            var smilXml = GetWebData<XmlDocument>(matchFileUrl.Groups["url"].Value, cookies: GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);
            
            var manager = new XmlNamespaceManager(smilXml.NameTable);
            manager.AddNamespace("s", smilXml.DocumentElement.NamespaceURI);
            var httpBase = smilXml.SelectSingleNode("//s:meta[@name='httpBase']/@content", manager).Value;
            
            foreach (XmlElement videoElem in smilXml.SelectNodes("//s:video", manager))
            {
                var src = videoElem.GetAttribute("src");
                var bitrate = int.Parse(videoElem.GetAttribute("system-bitrate")) / 1000;

                playbackOptions.Add(string.Format("{0} K", bitrate), httpBase + src);
            }
            
            return playbackOptions;
        }

        protected override List<VideoInfo> Parse(string url, string data)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();
            if (string.IsNullOrEmpty(data)) data = GetWebData<string>(url, cookies: GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);
            foreach (var rssItem in RssToolkit.Rss.RssDocument.Load(data).Channel.Items)
            {
                VideoInfo video = Helpers.RssUtils.VideoInfoFromRssItem(rssItem, regEx_FileUrl != null, new Predicate<string>(IsPossibleVideo));
                video.VideoUrl = rssItem.Guid.Text;
                // only if a video url was set, add this Video to the list
                if (!string.IsNullOrEmpty(video.VideoUrl) && video.VideoUrl.Contains("/video/"))
                {
                    if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 1)
                    {
                        video.Other = "PlaybackOptions://\n" + Helpers.CollectionUtils.DictionaryToString(video.PlaybackOptions);
                    }
                    videoList.Add(video);
                }
            }
            return videoList;
        }

    }
}

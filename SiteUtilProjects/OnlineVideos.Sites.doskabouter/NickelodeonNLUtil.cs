using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class NickelodeonNLUtil : GenericSiteUtil
    {
        private string playListRegex = @"mrss\s*:\s'(?<url>[^']*)',";
        private string urlRegex = @"<media:content\sduration='[^']*'\sisDefault='true'\stype='text/xml'\surl='(?<url>[^']*)'></media:content>";

        private Regex regEx_PlayList;
        private Regex regex_Url;

        public override int DiscoverDynamicCategories()
        {
            regEx_PlayList = new Regex(playListRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regex_Url = new Regex(urlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            int res = base.DiscoverDynamicCategories();
            foreach (Category cat in Settings.Categories)
                if (cat.Name == "New")
                    cat.HasSubCategories = false;
            return res;
        }

        public override List<string> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            List<string> result = new List<string>();

            if ("livestream".Equals(video.Other))
            {
                result.Add(video.VideoUrl);
                return result;
            }

            string webData = GetWebData(video.VideoUrl);
            Match m = regEx_PlayList.Match(webData);
            if (!m.Success)
                return null;

            webData = GetWebData(m.Groups["url"].Value);
            m = regex_Url.Match(webData);
            string furl = null;
            while (m.Success)
            {
                if (furl == null)
                    furl = m.Groups["url"].Value;
                result.Add(m.Groups["url"].Value);
                m = m.NextMatch();
            }

            video.PlaybackOptions = new Dictionary<string, string>();

            XmlDocument doc = new XmlDocument();
            webData = GetWebData(furl);
            doc.LoadXml(webData);
            foreach (XmlNode node in doc.SelectNodes("//package/video/item/rendition"))
            {
                string bitrate = node.Attributes["bitrate"].Value + "K";
                string turl = node.SelectSingleNode("src").InnerText;
                video.PlaybackOptions.Add(bitrate, turl);
            }
            return result;
        }

        public override string GetPlaylistItemVideoUrl(VideoInfo clonedVideoInfo, string chosenPlaybackOption, bool inPlaylist = false)
        {
            if (String.IsNullOrEmpty(chosenPlaybackOption))
                return clonedVideoInfo.VideoUrl;

            XmlDocument doc = new XmlDocument();
            string webData = GetWebData(clonedVideoInfo.VideoUrl);
            doc.LoadXml(webData);
            int chosenBitrate;
            if (!Int32.TryParse(chosenPlaybackOption.TrimEnd('K'), out chosenBitrate))
                chosenBitrate = Int32.MaxValue; // fallback to highest quality

            string url = null;
            int diff = Int32.MaxValue;
            foreach (XmlNode node in doc.SelectNodes("//package/video/item/rendition"))
            {
                int thisBitrate;
                if (Int32.TryParse(node.Attributes["bitrate"].Value, out thisBitrate))
                {
                    // bitrates are not exactly equal for different parts...
                    if (Math.Abs(thisBitrate - chosenBitrate) < diff)
                    {
                        diff = Math.Abs(thisBitrate - chosenBitrate);
                        url = node.SelectSingleNode("src").InnerText;
                    }
                }
            }
            return url;
        }
    }
}

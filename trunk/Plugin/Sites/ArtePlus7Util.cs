using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Windows.Forms;
using System.Net;
using System.IO;

namespace OnlineVideos.Sites
{
    public class ArtePlus7Util : SiteUtilBase
    {
        public enum MediaType { flv, wmv };
        public enum MediaQuality { medium, high };
        MediaType preferredMediaType = MediaType.wmv;
        MediaQuality preferredMediaQuality = MediaQuality.high;

        string catUrlRegex = @"<div\sid=""nuage"">\s*
<noscript><div><a\shref=""(?<url>[^""]+)""[^>]*>(?<title>[^<]+)</a></div>\s*";


        string catRegex = @"<span\sclass=""niveau[^""]""\s><a\shref=""(?<url>[^""]+)""[^>]*>(?<title>[^<]+)</a></span>
";
        string videolistRegex = @"so.addVariable\(""xmlURL"",\s""(?<url>[^""]+)""\)";

        string playlistRegex = @"availableFormats\[[0-9]\]\[""format""\]\s=\s""(?<format>[^""]+)"";\s*availableFormats\[[0-9]\]\[""quality""\]\s=\s""(?<quality>[^""]+)"";\s*availableFormats\[[0-9]\]\[""url""\]\s=\s""(?<url>[^""]+)"";";

        string playlistItemRegex = @"<REF\sHREF=""(?<url>[^""]+)""/>";

        string baseUrl = "http://plus7.arte.tv/de/1697480,filter=emissions.html";

        Regex regEx_CategoryUrl;
        Regex regEx_Category;
        Regex regEx_Videolist;
        Regex regEx_Playlist;
        Regex regEx_PlaylistItem;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_CategoryUrl = new Regex(catUrlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_Category = new Regex(catRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_Videolist = new Regex(videolistRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_Playlist = new Regex(playlistRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_PlaylistItem = new Regex(playlistItemRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            string data = GetWebData(baseUrl);
            string categoryUrl = String.Empty;

            if (!string.IsNullOrEmpty(data))
            {
                Match m = regEx_CategoryUrl.Match(data);
                while (m.Success)
                {
                    categoryUrl = m.Groups["url"].Value;
                    categoryUrl = "http://plus7.arte.tv" + categoryUrl;
                    m = m.NextMatch();
                }
            }
            if (!string.IsNullOrEmpty(categoryUrl))
            {
                data = String.Empty;
                data = GetWebData(categoryUrl);

                if (!string.IsNullOrEmpty(data))
                {
                    Match m = regEx_Category.Match(data);
                    while (m.Success)
                    {
                        RssLink cat = new RssLink();

                        cat.Url = m.Groups["url"].Value;
                        cat.Url = "http://plus7.arte.tv" + cat.Url;

                        cat.Name = m.Groups["title"].Value;

                        Settings.Categories.Add(cat);
                        m = m.NextMatch();
                    }
                    Settings.DynamicCategoriesDiscovered = true;
                    return Settings.Categories.Count;
                }
            }
            return 0;
        }

        public override String getUrl(VideoInfo video)
        {
            string data = GetWebData(video.VideoUrl);
            if (!string.IsNullOrEmpty(data))
            {
                Match m = regEx_Playlist.Match(data);
                while (m.Success)
                {
                    if (preferredMediaType == MediaType.wmv && m.Groups["format"].Value.Contains("WMV"))
                    {
                        if (preferredMediaQuality == MediaQuality.high && m.Groups["quality"].Value.Contains("HQ"))
                        {
                            string playlistUrl = m.Groups["url"].Value;
                            string playlist = GetWebData(playlistUrl);
                            if (!string.IsNullOrEmpty(playlist))
                            {
                                Match n = regEx_PlaylistItem.Match(playlist);
                                if (n.Success)
                                {
                                    string videoUrl = n.Groups["url"].Value;
                                    return videoUrl;
                                }
                            }
                            return null;
                        }
                        else if (preferredMediaQuality == MediaQuality.medium && m.Groups["quality"].Value.Contains("MQ"))
                        {
                            string playlistUrl = m.Groups["url"].Value;
                            string playlist = GetWebData(playlistUrl);
                            if (!string.IsNullOrEmpty(playlist))
                            {
                                Match n = regEx_PlaylistItem.Match(playlist);
                                if (n.Success)
                                {
                                    string videoUrl = n.Groups["url"].Value;
                                    return videoUrl;
                                }
                            }
                            return null;
                        }
                    }
                    else if (preferredMediaType == MediaType.flv && m.Groups["format"].Value.Contains("FLV"))
                    {
                        if (preferredMediaQuality == MediaQuality.high && m.Groups["quality"].Value.Contains("HQ"))
                        {
                            //TODO: RTMP link generation
                            return null;
                        }
                        else if (preferredMediaQuality == MediaQuality.medium && m.Groups["quality"].Value.Contains("MQ"))
                        {
                            //TODO: RTMP link generation
                            return null;
                        }
                    }
                    m = m.NextMatch();
                }
            }
            return null;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();

            string data = GetWebData((category as RssLink).Url);
            if (!string.IsNullOrEmpty(data))
            {
                //re is kidding me.. -_-
                Match m = regEx_Videolist.Match(data);
                string xmlUrl ="";
                if (m.Success)
                {
                    xmlUrl = m.Groups["url"].Value;
                    xmlUrl = "http://plus7.arte.tv" + xmlUrl;
                }
                
                if (!string.IsNullOrEmpty(xmlUrl))
                {

                    data = String.Empty;
                    data = GetWebData(xmlUrl);

                    if (!string.IsNullOrEmpty(data))
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(data);
                        XmlElement root = doc.DocumentElement;
                        XmlNodeList list;
                        list = root.SelectNodes("./video/title");
                        foreach (XmlNode node in list)
                        {
                            VideoInfo video = new VideoInfo();

                            video.Title = HttpUtility.HtmlDecode(node.SelectSingleNode("../title").InnerText);
                            video.ImageUrl = node.SelectSingleNode("../previewPictureURL").InnerText;
                            video.VideoUrl = node.SelectSingleNode("../targetURL").InnerText;

                            videos.Add(video);
                        }
                    }

                }
            }
            return videos;
        }
    }
}
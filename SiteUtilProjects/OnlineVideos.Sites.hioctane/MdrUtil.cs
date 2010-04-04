using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class MdrUtil : SiteUtilBase
    {
        string xmlRegex = @"<div\sclass=""noflashteaser"">\s*<h2><a\shref=""(?<url>[^""]+)""[^>]*>(?<title>[^<]+)</a></h2>\s*";

        string playlistRegex = @"<REF\sHREF\s=\s""(?<url>[^""]+)""[^>]*>";
        string baseUrl = "http://www.mdr.de/mediathek/fernsehen/a-z";

        Regex regEx_Xml;
        Regex regEx_Playlist;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_Xml = new Regex(xmlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_Playlist = new Regex(playlistRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            string data = GetWebData(baseUrl);
            string xmlUrl = String.Empty;

            if (!string.IsNullOrEmpty(data))
            {
                Match m = regEx_Xml.Match(data);
                while (m.Success)
                {
                    xmlUrl = m.Groups["url"].Value;
                    xmlUrl = xmlUrl.Substring(xmlUrl.LastIndexOf("/"));
                    xmlUrl = xmlUrl.Replace(".html", "-meta.xml");
                    xmlUrl = "http://www.mdr.de/mediathek/" + xmlUrl;
                    m = m.NextMatch();
                }
            }
            if(!string.IsNullOrEmpty(xmlUrl))
            {
                data = String.Empty;
                data = GetWebData(xmlUrl);

                if (!string.IsNullOrEmpty(data))
                {

                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(data);
                    XmlElement root = doc.DocumentElement;
                    XmlNodeList list;
                    list = root.SelectNodes("./object/children/group/object/headline");
                    foreach (XmlNode node in list)
                    {
                        RssLink cat = new RssLink();
                        cat.Name = HttpUtility.HtmlDecode(node.InnerText);

                        cat.Url = node.SelectSingleNode("../nimex").InnerText;
                        cat.Url = cat.Url.Replace(".xml", "-meta.xml");
                        
                        cat.Thumb = node.SelectSingleNode("../image/data").InnerText;
                        cat.Description = node.SelectSingleNode("../description").InnerText;

                        XmlNodeList videoItems;
                        videoItems = node.SelectNodes("../children/object");
                        cat.EstimatedVideoCount = (uint)videoItems.Count;
                        if(cat.EstimatedVideoCount > 0)
                            Settings.Categories.Add(cat);
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
                    if (m.Success)
                    {
                        string videoUrl = m.Groups["url"].Value;
                        return videoUrl;
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
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(data);
                XmlElement root = doc.DocumentElement;
                XmlNodeList list;
                list = root.SelectNodes("./object/children/object/headline");
                foreach (XmlNode node in list)
                {
                    VideoInfo video = new VideoInfo();

                    video.Title = HttpUtility.HtmlDecode(node.InnerText);
                    video.Length = node.SelectSingleNode("../attributes/length").InnerText;
                    video.ImageUrl = node.SelectSingleNode("../image/data").InnerText;
                    video.Description = node.SelectSingleNode("../description").InnerText;
                    video.Tags = node.SelectSingleNode("../keywords").InnerText;

                    XmlNodeList videoSources;
                    videoSources = node.SelectNodes("../attributes/av/mime_type");
                    foreach (XmlNode videoNode in videoSources)
                    {
                        if (videoNode.InnerText.Contains("video/x-ms-wmv"))
                        {
                            video.VideoUrl = videoNode.SelectSingleNode("../streaming_url").InnerText;
                        }
                    }
                    videos.Add(video);
                }
            }
            return videos;
        }
    }
}
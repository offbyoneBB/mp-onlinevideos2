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
    public class MdrUtil : SiteUtilBase
    {
        public enum MediaType { mp4, wmv };
        MediaType preferredMediaType = MediaType.wmv;

        string xmlRegex = @"<div\sclass=""noflashteaser"">\s*
<h2><a\shref=""(?<url>[^""]+)""[^>]*>(?<title>[^<]+)</a></h2>\s*";

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
                        cat.Url = node.SelectSingleNode("../nimex").InnerText;
                        cat.Url = cat.Url.Replace(".xml", "-meta.xml");
                        cat.Name = HttpUtility.HtmlDecode(node.SelectSingleNode("../headline").InnerText);
                        cat.Thumb = node.SelectSingleNode("../image/data").InnerText;

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
            if (preferredMediaType == MediaType.wmv)
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
            }
            else
            {
                //TODO: Add rtmp stream url decryption code
                return null;
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

                    video.Title = HttpUtility.HtmlDecode(node.SelectSingleNode("../headline").InnerText);
                    video.Length = node.SelectSingleNode("../attributes/length").InnerText;
                    video.ImageUrl = node.SelectSingleNode("../image/data").InnerText;

                    XmlNodeList videoSources;
                    videoSources = node.SelectNodes("../attributes/av/mime_type");
                    foreach (XmlNode videoNode in videoSources)
                    {
                        if (preferredMediaType == MediaType.wmv)
                        {
                            if (videoNode.InnerText.Contains("video/x-ms-wmv"))
                            {
                                video.VideoUrl = videoNode.SelectSingleNode("../streaming_url").InnerText;
                            }
                        }
                        else
                        {

                            if (videoNode.InnerText.Contains("video/mp4"))
                            {
                                //TODO: Add rtmp stream parsing code

                                /*
                                    <av type="F4V">
                                    <mime_type>video/mp4</mime_type>
                                    <media_type>F4V-Video</media_type>
                                    <streaming_url/>
                                    <download_url/>
                                    −
                                    <fms_application>
                                    rtmp://c22033-o.f.core.cdn.streamfarm.net/22033mdr/ondemand
                                    </fms_application>
                                    −
                                    <fms_url>
                                    3087mdr/MDR_vgnf4v/FCMS-55802c8d-7941-4204-a40f-f6bb3b922fac-1
                                    </fms_url>
                                    <size>30989099</size>
                                    <size_readable>29,6 MB</size_readable>
                                    </av>
                                 */
                            }
                        }

                    }
                    videos.Add(video);
                }
            }
            return videos;
        }
    }
}
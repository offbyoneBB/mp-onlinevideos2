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
    public class NdrUtil : SiteUtilBase
    {
        public enum MediaType { mp4, flv };

        [Category("OnlineVideosUserConfiguration"), Description("Prefered Stream format")]
        MediaType preferredMediaType = MediaType.mp4;

        public enum MediaQuality { low, high };

        [Category("OnlineVideosUserConfiguration"), Description("Videoquality")]
        MediaQuality preferredMediaQuality = MediaQuality.high;

        //<broadcast id="4" site="ndrfernsehen">Bücherjournal</broadcast>
        string xmlCategoryRegex = @"<broadcast\sid=""(?<url>[^""]+)""\ssite=""(?<dummy>[^""]+)"">(?<title>[^<]+)</broadcast>";

        string baseUrl = "http://www1.ndr.de/mediathek/mediathek100-mediathek_medium-tv_searchtype-broadcasts.xml";

        Regex regEx_CategoryXml;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_CategoryXml = new Regex(xmlCategoryRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            string data = GetWebData(baseUrl);
            if (!string.IsNullOrEmpty(data))
            {
                Match m = regEx_CategoryXml.Match(data);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Url = m.Groups["url"].Value;
                    cat.Name = m.Groups["title"].Value;
                    Settings.Categories.Add(cat);
                    m = m.NextMatch();
                }
                Settings.DynamicCategoriesDiscovered = true;
                return Settings.Categories.Count;
            }
            return 0;
        }

        public override String getUrl(VideoInfo video)
        {
            string url = string.Format("http://www1.ndr.de/mediathek/{0}-mediathek_details-true.xml", video.VideoUrl);
            string data = GetWebData(url);
            string videoUrl = "";

            if (!string.IsNullOrEmpty(data))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(data);
                XmlElement root = doc.DocumentElement;
                XmlNodeList list;
                list = root.SelectNodes("./sources/source");
                foreach (XmlNode node in list)
                {
                    if (string.IsNullOrEmpty(videoUrl))
                    {
                        videoUrl = node.InnerText;
                    }
                    else if(preferredMediaQuality == MediaQuality.high)
                    {
                        if (preferredMediaType == MediaType.flv && node.SelectSingleNode("./@mimetype").InnerText.Contains("flv"))
                        {
                            videoUrl = node.InnerText;
                        }
                        else
                        {
                            videoUrl = node.InnerText;
                        }
                    }
                }
                videoUrl = videoUrl.Replace("rtmpt://fms.edge.newmedia.nacamar.net", "rtmpt://ndr.fcod.llnwd.net/a3715/d1/flashmedia/streams");
                string resultUrl = string.Format("http://127.0.0.1:{0}/stream.flv?rtmpurl={1}", OnlineVideoSettings.RTMP_PROXY_PORT, System.Web.HttpUtility.UrlEncode(videoUrl));
                return resultUrl;
            }
            return null;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();

            string url = string.Format("http://www1.ndr.de/mediathek/mediathek100-mediathek_medium-tv_broadcast-{0}_pageSize-24.xml", (category as RssLink).Url);
            string data = GetWebData(url);

            if (!string.IsNullOrEmpty(data))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(data);
                XmlElement root = doc.DocumentElement;
                XmlNodeList list;
                list = root.SelectNodes("./mediaItem/title");
                foreach (XmlNode node in list)
                {
                    VideoInfo video = new VideoInfo();

                    video.Title = HttpUtility.HtmlDecode(node.SelectSingleNode("../title").InnerText);
                    if(video.Title.StartsWith("\""))
                        video.Title = video.Title.Substring(1, video.Title.Length - 2);

                    video.Length = node.SelectSingleNode("../duration").InnerText;
                    video.ImageUrl = node.SelectSingleNode("../images/image").InnerText;

                    video.VideoUrl = node.SelectSingleNode("../@id").InnerText;
                    videos.Add(video);
                }
            }
            return videos;
        }
    }
}
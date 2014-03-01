using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using OnlineVideos.AMF;

namespace OnlineVideos.Sites
{
    public class BigPondVideoUtil : GenericSiteUtil
    {
        public override int DiscoverDynamicCategories()
        {
            XmlDocument doc = new XmlDocument();
            string data = GetWebData(baseUrl);
            doc.LoadXml(data);
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("a", "http://www.sitemaps.org/schemas/sitemap/0.9");

            if (Settings.Categories == null) Settings.Categories = new BindingList<Category>();
            foreach (XmlNode node in doc.SelectNodes("//a:sitemap/a:loc", nsmgr))
            {
                RssLink cat = new RssLink();
                string s = node.InnerText;
                cat.Url = s;
                int p = s.LastIndexOf("-");
                int q = s.IndexOf('.', p);
                s = s.Substring(p + 1, q - p - 1);
                if (!String.IsNullOrEmpty(s))
                    s = s.Substring(0, 1).ToUpperInvariant() + s.Substring(1);
                cat.Name = s;
                Settings.Categories.Add(cat);
            }
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> res = new List<VideoInfo>();

            string data = GetWebData(((RssLink)category).Url);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(data);
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("a", "http://www.sitemaps.org/schemas/sitemap/0.9");
            nsmgr.AddNamespace("video", "http://www.google.com/schemas/sitemap-video/1.1");

            foreach (XmlNode node in doc.SelectNodes("a:urlset/a:url/video:video", nsmgr))
            {
                VideoInfo video = new VideoInfo();
                video.Title = node.SelectSingleNode("video:title", nsmgr).InnerText;
                video.ImageUrl = node.SelectSingleNode("video:thumbnail_loc", nsmgr).InnerText;
                video.Description = node.SelectSingleNode("video:description", nsmgr).InnerText;
                video.Airdate = DateTime.Parse(node.SelectSingleNode("video:publication_date", nsmgr).InnerText).ToString();
                XmlNode durNode = node.SelectSingleNode("video:duration", nsmgr);
                if (durNode != null)
                    video.Length = VideoInfo.GetDuration(durNode.InnerText);
                XmlNode locNode = node.SelectSingleNode("video:player_loc", nsmgr);
                if (locNode != null)
                {
                    video.VideoUrl = locNode.InnerText;
                    res.Add(video);
                }
            }
            return res;
        }

        public override string getUrl(VideoInfo video)
        {
            Match matchVideoUrl = regEx_VideoUrl.Match(video.VideoUrl);
            if (matchVideoUrl.Success)
            {
                string cid = matchVideoUrl.Groups["cid"].Value;
                AMFObject root = new AMFObject("");
                root.Add("cid", cid);

                AMFSerializer ser = new AMFSerializer();
                byte[] data = ser.Serialize2("SEOPlayer.getMediaURL", new object[] { root });

                AMFObject obj = AMFObject.GetResponse(@"http://bigpondvideo.com/App/AmfPhp/gateway.php", data);
                return obj.Name;

            }
            return String.Empty;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using RssToolkit.Rss;
using OnlineVideos.MPUrlSourceFilter;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class RTEPlayerUtil : GenericSiteUtil
    {

        public override int DiscoverDynamicCategories()
        {
            int n = base.DiscoverDynamicCategories();
            foreach (Category cat in Settings.Categories)
                cat.HasSubCategories = (cat.Name != "Latest" && cat.Name != "Last Chance" && cat.Name != "Live");
            return n;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            XmlDocument doc = new XmlDocument();
            string xmlData = GetWebData(((RssLink)parentCategory).Url, forceUTF8: true);
            doc.LoadXml(xmlData);
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("a", "http://www.w3.org/2005/Atom");
            parentCategory.SubCategories = new List<Category>();
            foreach (XmlNode node in doc.SelectNodes(@"a:feed/a:entry", nsmgr))
            {
                RssLink cat = new RssLink();
                cat.ParentCategory = parentCategory;
                cat.Name = node.SelectSingleNode("a:title", nsmgr).InnerText;
                cat.Url = node.SelectSingleNode("a:id", nsmgr).InnerText;
                if (String.IsNullOrEmpty(cat.Url))
                    cat.Url = String.Format(@"http://dj.rte.ie/vodfeeds/feedgenerator/az/?id={0}", cat.Name);

                parentCategory.SubCategories.Add(cat);
            }
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            if (category is Group)
                return base.GetVideos(category);

            string xmlData = GetWebData(((RssLink)category).Url, forceUTF8: true);
            List<VideoInfo> videoList = new List<VideoInfo>();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlData);
            XmlNamespaceManager nsmgr = GetNameSpaceManager(doc);
            foreach (XmlNode node in doc.SelectNodes("//a:feed/a:entry", nsmgr))
            {
                VideoInfo video = new VideoInfo();
                video.Title = node.SelectSingleNode("a:title", nsmgr).InnerText;
                video.VideoUrl = node.SelectSingleNode("a:id", nsmgr).InnerText;
                video.ImageUrl = node.SelectSingleNode("m:thumbnail", nsmgr).Attributes["url"].InnerText;
                video.Length = node.SelectSingleNode("r:duration", nsmgr).Attributes["formatted"].Value;
                DateTime airdate = DateTime.Parse(node.SelectSingleNode("a:published", nsmgr).InnerText);
                video.Airdate = airdate.ToString("g", OnlineVideoSettings.Instance.Locale);
                video.Description = node.SelectSingleNode("a:content", nsmgr).InnerText;
                videoList.Add(video);
            }
            return videoList;
        }

        public override List<string> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            List<string> result = new List<string>();
            if ("livestream".Equals(video.Other))
            {
                result.Add(video.VideoUrl + "&swfVfy=http://www.rte.ie/player/assets/player_403.swf");
                return result;
            }
            string xmlData = GetWebData(video.VideoUrl);
            xmlData = Regex.Replace(xmlData, @"url=""http[^""]*""", @"url=""""");
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlData);
            XmlNamespaceManager nsmgr = GetNameSpaceManager(doc);
            XmlNode nd = doc.SelectSingleNode("//a:feed/a:entry/m:group", nsmgr);
            foreach (XmlNode node in doc.SelectNodes("//a:feed/a:entry/m:group/m:content[@r:format!='advertising']", nsmgr))
            {
                string s = node.Attributes["url"].Value;

                RtmpUrl rtmpUrl = new RtmpUrl(s)
                {
                    SwfVerify = true,
                    App = "rtevod",
                    SwfUrl = @"http://www.rte.ie/player/assets/player_403.swf"
                };

                result.Add(rtmpUrl.ToString());
            }
            return result;
        }

        private XmlNamespaceManager GetNameSpaceManager(XmlDocument doc)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("a", "http://www.w3.org/2005/Atom");
            nsmgr.AddNamespace("m", "http://search.yahoo.com/mrss/");
            nsmgr.AddNamespace("r", "http://www.rte.ie/schemas/vod");
            return nsmgr;
        }
    }
}

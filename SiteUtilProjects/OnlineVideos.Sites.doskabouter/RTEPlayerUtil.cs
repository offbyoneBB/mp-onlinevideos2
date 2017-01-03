using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace OnlineVideos.Sites
{
    public class RTEPlayerUtil : GenericSiteUtil
    {

        public override int DiscoverDynamicCategories()
        {
            int n = base.DiscoverDynamicCategories();
            foreach (Category cat in Settings.Categories)
            {
                cat.HasSubCategories = (cat.Name != "Latest" && cat.Name != "Most Popular" && cat.Name != "Live");
                if (((RssLink)cat).Url.Contains(@"/a-z/"))
                    cat.Other = true;
            }
            return n;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (true.Equals(parentCategory.Other))
            {
                var oldRegex = regEx_dynamicSubCategories;
                regEx_dynamicSubCategories = new Regex(@"<td\sclass=""[^""]*""><a\shref=""(?<url>[^""]*)"">(?<title>[^<]*)</a></td>", defaultRegexOptions);
                int r = base.DiscoverSubCategories(parentCategory);
                regEx_dynamicSubCategories = oldRegex;
                foreach (var s in parentCategory.SubCategories)
                    s.HasSubCategories = true;
                return r;
            }
            int n = base.DiscoverSubCategories(parentCategory);

            foreach (var subcat in parentCategory.SubCategories)
            {
                if (subcat.Description.Contains("Programmes"))
                    subcat.HasSubCategories = true;
            }
            return n;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            var res = base.GetVideos(category);
            res.RemoveAll(t => t.Airdate.Contains("episode"));
            if (res.Count == 0)
            {
                //probably one video
                var data = GetWebData(((RssLink)category).Url);
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(data);
                HtmlNode vidNode = htmlDoc.DocumentNode.SelectSingleNode("//article[@class='video-content']");
                var video = new VideoInfo();
                video.Title = vidNode.SelectSingleNode(".//header/h1[@id='show-title']").InnerText;
                video.Airdate = vidNode.SelectSingleNode(".//ul/li[@class='broadcast-date']/span").InnerText;
                video.Thumb = vidNode.SelectSingleNode(".//meta[@itemprop='thumbnailUrl']").Attributes["content"].Value;
                video.VideoUrl = ((RssLink)category).Url;
                video.Length = vidNode.SelectSingleNode(".//li[strong[text()='Duration']]/text()").InnerText;
                res.Add(video);
            }
            return res;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string playListUrl = GetPlaylistUrl(video.VideoUrl);
            var xml = new XmlDocument();
            xml.LoadXml(GetWebData(playListUrl));
            var nsmgr = GetNameSpaceManager(xml);
            string feedUrl = xml.SelectSingleNode("//m:content", nsmgr).Attributes["url"].Value;
            xml = new XmlDocument();
            xml.LoadXml(GetWebData(feedUrl).Replace(@"xmlns=""http://ns.adobe.com/f4m/2.0""", ""));
            video.PlaybackOptions = new Dictionary<string, string>();
            foreach (XmlNode node in xml.SelectNodes("//media[@href]"))
            {
                var qual = node.Attributes["bitrate"].Value;
                var s = node.Attributes["href"].Value;
                s = FormatDecodeAbsolutifyUrl(feedUrl, s, null, UrlDecoding.None);
                video.PlaybackOptions.Add(qual, s);
            }
            return video.PlaybackOptions.Last().Value;
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

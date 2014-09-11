using System.Collections.Generic;
using System.Web;
using HtmlAgilityPack;

namespace OnlineVideos.Sites
{
    public class DeRedactieUtil : GenericSiteUtil
    {
        public override int DiscoverDynamicCategories()
        {
            string data = GetWebData(baseUrl);
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(data);
            HtmlNode ul = htmlDoc.DocumentNode.SelectSingleNode("//ul[@class = 'hnav nav2']");
            HtmlNodeCollection mainMenu = ul.SelectNodes("li");
            foreach (HtmlNode node in mainMenu)
            {
                RssLink category = new RssLink()
                {
                    Name = HttpUtility.HtmlDecode(node.SelectSingleNode("a/span").InnerText)
                };
                HtmlAttribute classAtt = node.Attributes["class"];
                if (classAtt != null && classAtt.Value == "hasSubMenu")
                    AddSubcats(category, node.SelectNodes(".//ul/li"));
                else
                    category.Url = FormatDecodeAbsolutifyUrl(baseUrl, node.SelectSingleNode("a").Attributes["href"].Value, null, UrlDecoding.None);
                Settings.Categories.Add(category);
            }
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        private void AddSubcats(Category category, HtmlNodeCollection subcats)
        {
            category.HasSubCategories = true;
            category.SubCategories = new List<Category>();
            foreach (HtmlNode sub in subcats)
            {
                HtmlNode aNode = sub.SelectSingleNode("a");
                RssLink subcat = new RssLink()
                {
                    Name = HttpUtility.HtmlDecode(aNode.ChildNodes[1].InnerText),
                    Url = FormatDecodeAbsolutifyUrl(baseUrl, aNode.Attributes["href"].Value, null, UrlDecoding.None),
                    ParentCategory = category
                };
                category.SubCategories.Add(subcat);
            }
            category.SubCategoriesDiscovered = true;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string url = ((RssLink)category).Url;
            string data = GetWebData(url);
            int p = data.IndexOf(@"class=""splitter split12-12");
            if (p != -1)
                data = data.Substring(0, p);

            return Parse(url, data);
        }

        public override string getUrl(VideoInfo video)
        {
            string s = base.getUrl(video);
            return s.Replace("_definst_/", "_definst_/mp4:") + "/manifest.f4m";
        }
    }
}

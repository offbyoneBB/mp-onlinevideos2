using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Web;

namespace OnlineVideos.Sites
{
    public class ThatGuyWithTheGlassesUtil : GenericSiteUtil
    {

        private void AddSubcats(XmlNode node, Category parentCategory)
        {
            RssLink cat = new RssLink();
            cat.Name = HttpUtility.HtmlDecode(node.SelectSingleNode("a/span").InnerText);
            cat.Url = baseUrl + node.SelectSingleNode("a").Attributes["href"].Value;
            cat.SubCategoriesDiscovered = true;

            if (parentCategory == null)
                Settings.Categories.Add(cat);
            else
            {
                if (parentCategory.SubCategories == null)
                    parentCategory.SubCategories = new List<Category>();
                parentCategory.SubCategories.Add(cat);
                cat.ParentCategory = parentCategory;
            }

            XmlNodeList subs = node.SelectNodes("div/ul/li");
            foreach (XmlNode sub in subs)
            {
                cat.HasSubCategories = true;
                AddSubcats(sub, cat);
            }
        }

        public override int DiscoverDynamicCategories()
        {
            XmlDocument doc = new XmlDocument();
            string data = GetWebData(baseUrl);
            int p = data.IndexOf(@"<ul class=""menutop"" >");
            int q = data.IndexOf(@"<div class=""clr"">", p);
            data = data.Substring(p, q - p);
            data = @"<?xml version=""1.0"" encoding=""iso-8859-1""?>" + data;
            doc.LoadXml(data);
            XmlNode node = doc.FirstChild.NextSibling;
            foreach (XmlNode ch in node.ChildNodes)
            {
                string s = ch.FirstChild.FirstChild.InnerText;
                if (s == "Videos" || s == "BlisteredThumbs")
                    AddSubcats(ch, null);
            }
            Settings.DynamicCategoriesDiscovered = true;
            return base.DiscoverDynamicCategories();
        }

    }
}

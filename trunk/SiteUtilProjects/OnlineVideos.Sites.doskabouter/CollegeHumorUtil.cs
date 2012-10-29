using System;
using System.Text.RegularExpressions;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class CollegeHumorUtil : GenericSiteUtil
    {
        private RegexOptions defaultRegexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture;
        public override int DiscoverDynamicCategories()
        {
            if (Settings.Categories != null)
                foreach (Category cat in Settings.Categories)
                    cat.HasSubCategories = true;
            return base.DiscoverDynamicCategories();
        }

        public override int ParseSubCategories(Category parentCategory, string data)
        {
            if (true.Equals(parentCategory.Other))
            {
                Regex tmp = regEx_dynamicSubCategories;
                regEx_dynamicSubCategories = new Regex(@"<div\sclass=""grid3\ssketch-group"">\s*<a\shref=""(?<url>[^""]*)""\stitle=""[^""]*""\sclass=""thumb"">\s*<img\ssrc=""(?<thumb>[^""]*)""\swidth=""175""\sheight=""98""\salt=""[^""]*"">\s*<strong>(?<title>[^<]*)</strong>\s*</a>", defaultRegexOptions);
                regEx_dynamicSubCategoriesNextPage = regEx_NextPage;
                int result = base.ParseSubCategories(parentCategory, data);
                regEx_dynamicSubCategories = tmp;
                regEx_dynamicSubCategoriesNextPage = null;
                return result;
            }

            int res = base.ParseSubCategories(parentCategory, data);
            if (parentCategory.Name == "Sketch Comedy")
                foreach (Category subcat in parentCategory.SubCategories)
                {
                    subcat.HasSubCategories = true;
                    subcat.Other = true;
                }
            return res;
        }

        public override string getUrl(VideoInfo video)
        {
            string res = base.getUrl(video);// for embedded youtube
            if (String.IsNullOrEmpty(res))
            {
                string webData = GetWebData(video.VideoUrl);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(webData);
                XmlNode node = doc.SelectSingleNode("//videoplayer/video/file");
                if (node != null) return node.InnerText + "?hdcore=2.6.8";
            }
            return res;
        }
    }
}

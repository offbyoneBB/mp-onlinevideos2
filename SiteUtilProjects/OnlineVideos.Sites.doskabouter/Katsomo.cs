using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class KatsomoUtil : GenericSiteUtil
    {
        private string UserAgent = @"Mozilla/5.0 (iPhone; U; CPU iPhone OS 3_0 like Mac OS X; en-us) AppleWebKit/528.18 (KHTML, like Gecko) Version/4.0 Mobile/7A341 Safari/528.16";

        public override int DiscoverDynamicCategories()
        {
            int res = base.DiscoverDynamicCategories();
            foreach (Category cat in Settings.Categories)
                cat.HasSubCategories = !((RssLink)cat).Url.Contains("?treeId");
            return res;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string webData = GetWebData(((RssLink)parentCategory).Url, userAgent: UserAgent);

            Category aakkosjärjestyksessä = GetNewCat("Aakkosjärjestyksessä", parentCategory);
            AddSubs(webData, @"""program-group"" id=", new Regex(@"""(?<title>[^""]*)""", defaultRegexOptions), aakkosjärjestyksessä);

            Category aiheittain = GetNewCat("Aiheittain", parentCategory);
            AddSubs(webData, @"class=""initial""", new Regex(@">(?<title>[^<]*)<", defaultRegexOptions), aiheittain);
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        private void AddSubs(string webData, string split, Regex titleRegex, Category parentCategory)
        {
            string[] subs = webData.Split(new[] { split }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string sub in subs)
            {
                Match title = titleRegex.Match(sub);
                if (title.Success && title.Index == 0)
                {
                    RssLink subCat = new RssLink()
                    {
                        Name = title.Groups["title"].Value,
                        ParentCategory = parentCategory,
                        HasSubCategories = true
                    };
                    if (subCat.Name == "U-")
                        subCat.Name = "U-Ö";
                    parentCategory.SubCategories.Add(subCat);
                    ParseSubCategories(subCat, sub);
                }
            }
        }

        private Category GetNewCat(string name, Category parentCat)
        {
            Category res = new Category()
            {
                Name = name,
                HasSubCategories = true,
                SubCategories = new List<Category>(),
                SubCategoriesDiscovered = true,
                ParentCategory = parentCat
            };
            if (parentCat != null)
            {
                if (parentCat.SubCategories == null)
                    parentCat.SubCategories = new List<Category>();

                parentCat.SubCategories.Add(res);
            }
            return res;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string url = ((RssLink)category).Url;
            string webData = GetWebData(url, userAgent: UserAgent);
            return Parse(url, webData);
        }

        public override string getUrl(VideoInfo video)
        {
            string webData = GetWebData(video.VideoUrl, userAgent: UserAgent);
            Match m = regEx_FileUrl.Match(webData);
            if (m.Success)
                return m.Groups["m0"].Value;
            return null;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace OnlineVideos.Sites
{
    public class BrassBandTubeUtil : GenericSiteUtil
    {
        private Regex myregEx_NextPage;

        public override int DiscoverDynamicCategories()
        {
            myregEx_NextPage = regEx_NextPage;
            regEx_NextPage = null;

            string data = GetWebData(baseUrl);
            string[] parts = data.Split(new[] { @"</select>" }, StringSplitOptions.RemoveEmptyEntries);

            if (Settings.Categories == null) Settings.Categories = new BindingList<Category>();

            Match m = regEx_dynamicCategories.Match(parts[0]);
            while (m.Success)
            {
                Category cat = new Category();
                cat.Name = m.Groups["title"].Value;
                cat.Other = String.Format(dynamicCategoryUrlFormatString, m.Groups["url"].Value);
                cat.HasSubCategories = true;
                Match m2 = regEx_dynamicCategories.Match(parts[1]);
                cat.SubCategories = new List<Category>();
                while (m2.Success)
                {
                    Category cat2 = new Category();
                    cat2.Name = m2.Groups["title"].Value;
                    cat2.Other = String.Format(dynamicSubCategoryUrlFormatString, m2.Groups["url"].Value);
                    cat2.ParentCategory = cat;
                    cat.SubCategories.Add(cat2);
                    m2 = m2.NextMatch();
                }
                cat.SubCategoriesDiscovered = true;
                Settings.Categories.Add(cat);

                m = m.NextMatch();
            }
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            string postData = String.Format("{0}&{1}&view=pages&controller=&limitstart=&limit=",
                category.ParentCategory.Other as String, category.Other as String);
            string data = GetWebData(baseUrl, postData);
            Match m = myregEx_NextPage.Match(data);
            nextPageAvailable = m.Success;
            if (nextPageAvailable)
                nextPageUrl = String.Format("{0}&{1}&view=pages&controller=&limitstart={2}&limit=",
                category.ParentCategory.Other as String, category.Other as String, m.Groups["limitstart"].Value);
            return Parse(baseUrl, data);
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            string data = GetWebData(baseUrl, nextPageUrl);
            return Parse(baseUrl, data);
        }
    }
}

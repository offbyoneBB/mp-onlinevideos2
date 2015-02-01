using System;
using System.Collections.Generic;

namespace OnlineVideos.Sites
{
    public class GamersNetUtil : GenericSiteUtil
    {
        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (parentCategory.Name == "Recent")
                return GetDays(parentCategory);
            else
                return base.DiscoverSubCategories(parentCategory);
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            string other = category.Other as string;
            if (other == null)
                return base.GetVideos(category);
            else
                return Parse(baseUrl, other);
        }

        private int GetDays(Category parentCat)
        {
            string data = GetWebData(((RssLink)parentCat).Url);
            string[] days = data.Split(new[] { "<p><b>" }, StringSplitOptions.RemoveEmptyEntries);
            parentCat.SubCategories = new List<Category>();
            for (int i = 1; i < days.Length; i++)
            {
                int p = days[i].IndexOf('<');
                RssLink cat = new RssLink()
                {
                    Name = days[i].Substring(0, p),
                    Other = days[i],
                    ParentCategory = parentCat
                };
                parentCat.SubCategories.Add(cat);
            }
            parentCat.SubCategoriesDiscovered = true;
            return parentCat.SubCategories.Count;
        }
    }
}

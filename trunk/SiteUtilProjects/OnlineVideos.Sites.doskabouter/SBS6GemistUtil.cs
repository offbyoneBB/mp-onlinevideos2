using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace OnlineVideos.Sites
{
    public class SBS6GemistUtil : BrightCoveUtil
    {
        public override int DiscoverDynamicCategories()
        {
            string data = GetWebData(baseUrl);
            List<Category> dynamicCategories = new List<Category>();
            List<Category> dynamicCategoriesAlfabet = new List<Category>();
            Match m = regEx_dynamicCategories.Match(data);
            while (m.Success)
            {
                RssLink cat = new RssLink();
                if (m.Groups["data"].Value != String.Empty)
                {
                    cat.Name = m.Groups["title"].Value.Trim();
                    data = m.Groups["data"].Value;
                    cat.Other = Parse(null, data);
                    cat.HasSubCategories = false;
                    dynamicCategories.Add(cat);
                }
                else
                {
                    cat.Url = m.Groups["url"].Value;
                    if (!Uri.IsWellFormedUriString(cat.Url, UriKind.Absolute)) cat.Url = new Uri(new Uri(baseUrl), cat.Url).AbsoluteUri;
                    cat.Name = m.Groups["title"].Value.Trim();
                    cat.HasSubCategories = true;
                    dynamicCategoriesAlfabet.Add(cat);
                }
                m = m.NextMatch();
            }

            foreach (Category cat in dynamicCategoriesAlfabet) dynamicCategories.Add(cat);

            // discovery finished, copy them to the actual list -> prevents double entries if error occurs in the middle of adding
            if (Settings.Categories == null) Settings.Categories = new BindingList<Category>();
            foreach (Category cat in dynamicCategories) Settings.Categories.Add(cat);
            Settings.DynamicCategoriesDiscovered = dynamicCategories.Count > 0; // only set to true if actually discovered (forces re-discovery until found)
            return dynamicCategories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            if (category.Other is List<VideoInfo>)
                return (List<VideoInfo>)category.Other;
            return base.getVideoList(category);
        }
    }
}

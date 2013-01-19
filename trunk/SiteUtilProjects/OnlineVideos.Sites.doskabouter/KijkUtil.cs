using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Linq;

namespace OnlineVideos.Sites
{

    public class KijkUtil : BrightCoveUtil
    {
        public override int DiscoverDynamicCategories()
        {
            if (Settings.Categories == null) Settings.Categories = new BindingList<Category>();
            Settings.Categories.Add(GetGemist(baseUrl));
            return 1 + base.DiscoverDynamicCategories();
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (parentCategory.SubCategories == null) parentCategory.SubCategories = new List<Category>();
            Category gemist = GetGemist(((RssLink)parentCategory).Url);
            gemist.ParentCategory = parentCategory;
            parentCategory.SubCategories.Add(gemist);

            int res = base.DiscoverSubCategories(parentCategory);
            foreach (Category cat in parentCategory.SubCategories)
                if (!cat.HasSubCategories) //skip gemist
                {
                    cat.HasSubCategories = true;
                    cat.SubCategories = new List<Category>();
                    cat.SubCategories.Add(new RssLink()
                    {
                        ParentCategory = cat,
                        HasSubCategories = false,
                        Name = "Clips",
                        Url = ((RssLink)cat).Url,
                        Other = 0
                    });
                    cat.SubCategories.Add(new RssLink()
                    {
                        ParentCategory = cat,
                        HasSubCategories = false,
                        Name = "Afleveringen",
                        Url = ((RssLink)cat).Url,
                        Other = 1
                    });
                    cat.SubCategoriesDiscovered = true;
                }
            return 1 + res;
        }

        private Category GetGemist(string url)
        {
            string webData = GetWebData(url, forceUTF8: true);
            string[] days = webData.Split(new[] { "<li id=" }, StringSplitOptions.RemoveEmptyEntries);
            Category result = new Category()
            {
                Name = "Gemist",
                HasSubCategories = true,
                SubCategoriesDiscovered = true,
                SubCategories = new List<Category>()
            };
            for (int i = days.Length - 1; i > 0; i--)
            {
                Category subcat = new Category()
                {
                    ParentCategory = result,
                    HasSubCategories = false
                };
                int p = days[i].IndexOf('"', 1);
                subcat.Name = days[i].Substring(1, p - 1);
                subcat.Other = Parse(baseUrl, days[i]);
                result.SubCategories.Add(subcat);

            }
            return result;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> res = category.Other as List<VideoInfo>;
            if (res != null)
                return res;
            int? nr = category.Other as int?;
            if (nr.HasValue)
            {
                string webdata = GetWebData(((RssLink)category).Url, forceUTF8: true);
                string[] parts = webdata.Split(new[] { @"<ul class=""horizontal-scroll-holder"">" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 2)
                    return Parse(baseUrl, parts[nr.Value + 1]);
            }
            return base.getVideoList(category);
        }

        public override string getUrl(VideoInfo video)
        {
            string result = base.getUrl(video);
            Dictionary<string, string> oldPlaybackOptions = video.PlaybackOptions;
            video.PlaybackOptions = new Dictionary<string, string>();

            foreach (var item in oldPlaybackOptions.OrderBy(u => u.Key, new BitrateComparer()))
            {
                video.PlaybackOptions.Add(item.Key, item.Value);
                // return last URL as the default (will be the highest bitrate)
                result = item.Value;
            }
            return result;
        }

        // borrowd from corporategadfly, will be properly returned after Onlinevideos 1.5
        private class BitrateComparer : IComparer<string>
        {
            private static Regex bitrateRegex = new Regex(@"\d+x\d+\s(?<bitrate>\d+)K", RegexOptions.Compiled);

            public int Compare(string x, string y)
            {
                int xKbps = 0, yKbps = 0;
                Match match;
                match = bitrateRegex.Match(x);
                if (match.Success && !int.TryParse(match.Groups["bitrate"].Value, out xKbps)) return 1;
                match = bitrateRegex.Match(y);
                if (match.Success && !int.TryParse(match.Groups["bitrate"].Value, out yKbps)) return -1;
                return xKbps.CompareTo(yKbps);
            }
        }
    }

}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;
using OnlineVideos.AMF;

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
            string url = video.VideoUrl;
            string[] parts = url.Split('/');
            video.VideoUrl = @"http://embed.kijk.nl/?width=868&height=491&video=" + parts[parts.Length - 1];
            string webdata = GetWebData(video.VideoUrl, referer: url);
            Match m = regEx_FileUrl.Match(webdata);

            if (!m.Success)
            {
                video.VideoUrl = url;
                return String.Empty;
            }

            MethodInfo methodInfo = typeof(BrightCoveUtil).GetMethod("GetResultsFromViewerExperienceRequest", BindingFlags.NonPublic | BindingFlags.Instance);
            if (methodInfo != null)
            {
                object[] parameters = new object[2];
                parameters[0] = m;
                parameters[1] = video;
                AMFArray renditions = (AMFArray)methodInfo.Invoke(this, parameters);

                methodInfo = typeof(BrightCoveUtil).GetMethod("FillPlaybackOptions", BindingFlags.NonPublic | BindingFlags.Instance);
                if (methodInfo == null)
                {
                    video.VideoUrl = url;
                    return String.Empty;
                }

                parameters[0] = video;
                parameters[1] = renditions;

                string result = (String)methodInfo.Invoke(this, parameters);
                video.VideoUrl = url;
                return result;
            }
            {
                video.VideoUrl = url;
                return String.Empty;
            }

        }
    }

}

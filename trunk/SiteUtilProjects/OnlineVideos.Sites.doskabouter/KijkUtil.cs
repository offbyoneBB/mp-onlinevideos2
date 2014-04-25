using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OnlineVideos.AMF;

namespace OnlineVideos.Sites
{

    public class KijkUtil : BrightCoveUtil
    {
        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (parentCategory.SubCategories == null) parentCategory.SubCategories = new List<Category>();
            Category gemist = GetGemist(((RssLink)parentCategory).Url);
            gemist.ParentCategory = parentCategory;
            parentCategory.SubCategories.Add(gemist);
            string webdata = GetWebData(((RssLink)parentCategory).Url);
            string[] parts = webdata.Split(new[] { "all-shows-a\">" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                if (!part.StartsWith("<"))
                {
                    Category cat = new RssLink()
                    {
                        ParentCategory = parentCategory,
                        Name = part.Substring(0, 1),
                        HasSubCategories = true
                    };
                    parentCategory.SubCategories.Add(cat);
                    base.ParseSubCategories(cat, part);
                }
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        private Category GetGemist(string url)
        {
            return new RssLink()
            {
                Name = "Gemist",
                HasSubCategories = false,
                Url = url
            };
        }

        public override string getUrl(VideoInfo video)
        {
            string[] parts = video.VideoUrl.Split('/');
            string url = @"http://embed.kijk.nl/?width=868&height=491&video=" + parts[parts.Length - 1];
            string webdata = GetWebData(url, referer: video.VideoUrl);
            Match m = regEx_FileUrl.Match(webdata);

            if (!m.Success)
                return String.Empty;

            AMFArray renditions = GetResultsFromViewerExperienceRequest(m, url);
            return FillPlaybackOptions(video, renditions);
        }
    }

}

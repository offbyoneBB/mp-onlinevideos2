using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using OnlineVideos.AMF;

namespace OnlineVideos.Sites
{

    public class KijkUtil : BrightCoveUtil
    {

        public override int DiscoverDynamicCategories()
        {
            int res = base.DiscoverDynamicCategories();
            foreach (Category cat in Settings.Categories)
            {
                if (cat.SubCategories != null && cat.SubCategories.Count > 0)
                {
                    cat.SubCategoriesDiscovered = true;
                    foreach (Category subcat in cat.SubCategories)
                    {
                        subcat.HasSubCategories = true;
                        subcat.Other = true;
                    }
                }
            }
            return res;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (parentCategory.SubCategories == null) parentCategory.SubCategories = new List<Category>();
            string url = ((RssLink)parentCategory).Url;
            string webdata = GetWebData(url);
            string[] parts = webdata.Split(new[] { @"showcase-heading"">" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                if (!part.StartsWith("<"))
                {
                    Category cat = new RssLink()
                    {
                        ParentCategory = parentCategory,
                        Name = part.Substring(0, part.IndexOf('<')),
                        HasSubCategories = parentCategory.Other == null
                    };
                    if (!cat.HasSubCategories)
                    {
                        cat.Other = Parse(url, part);
                        parentCategory.SubCategories.Add(cat);
                    }
                    else
                    {
                        if (ParseSubCategories(cat, part) > 0)
                            parentCategory.SubCategories.Add(cat);
                    }
                }
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            if (category.Other is List<VideoInfo>)
                return category.Other as List<VideoInfo>;
            return base.getVideoList(category);
        }

        public override string getUrl(VideoInfo video)
        {
            string[] parts = video.VideoUrl.Split('/');
            string url = @"http://www.kijk.nl/ajax/entitlement/" + parts[parts.Length - 2];
            string webdata = GetWebData(url, referer: video.VideoUrl);
            JToken jt = JObject.Parse(webdata) as JToken;
            string contentId = jt["entitlement"]["playerInfo"]["hostingkey"].Value<string>();

            url = @"http://embed.kijk.nl/?width=868&height=491&video=" + parts[parts.Length - 1];
            webdata = GetWebData(url, referer: video.VideoUrl);
            webdata = webdata.Replace(@"<param name=\""@videoPlayer\"" value=\""\"" />", @"<param name=\""@videoPlayer\"" value=\""" + contentId + @"\"" />");

            Match m = regEx_FileUrl.Match(webdata);

            if (!m.Success)
                return String.Empty;

            AMFArray renditions = GetResultsFromViewerExperienceRequest(m, url);
            return FillPlaybackOptions(video, renditions);
        }
    }

}

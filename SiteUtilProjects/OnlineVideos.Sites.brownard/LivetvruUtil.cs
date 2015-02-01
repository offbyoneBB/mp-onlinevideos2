using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace OnlineVideos.Sites
{
    public class LivetvruUtil : SiteUtilBase
    {
        [Category("OnlineVideosUserConfiguration"), Description("Only show matches that are currently playing?")]
        bool onlyShowCurrentlyPlaying = false;

        [Category("OnlineVideosUserConfiguration"), Description("The minimum time in seconds to wait before refreshing categories")]
        int refreshIntervalSeconds = 300;

        [Category("OnlineVideosConfiguration"), Description("Regex pattern to use for matching categories")]
        string categoryRegex = @"<div id=""sport\d+"" .*?<img src=(?<thumb>[^>]*).*?<span class=""sltitle"">(?<title>.*?)</span>(?<subcats>.*?)</div>";
        [Category("OnlineVideosConfiguration"), Description("Regex pattern to use for matching subcategories")]
        string subCategoryRegex = @"<tr><td[^>]*> <img.*? src=""(?<thumb>[^""]*)""> </td><td> <a.*? href=""(?<url>[^""]*)"">(?<name>[^<]*)</a><br>(?<live><a[^>]*><img[^>]*></a><span class=""live"">)?.*?<span class=""evdesc"">(?<time>.*?)<br>(?<info>.*?)</span>";
        [Category("OnlineVideosConfiguration"), Description("Regex pattern to use for matching videos")]
        string videoRegex = @"<td[^>]*>(?<bitrate>[^<]*)</td>[\s\n]*<td.*?</td>[\s\n]*<td[^>]*><div[^>]*>&nbsp;(?<rating>\d+)<span class=""pc"">%</span></div></td>([\s\n]*<td.*?</td>){2}[\s\n]*<td[^>]*><a href=""(?<url>sop://[^""]*)"".*?</td>[\s\n]*</tr>[\s\n]*</table>[\s\n]*</td><td>(<span class=""date""><b>(?<info>[^<]*))?";

        DateTime lastRefresh = DateTime.MinValue;
        public override int DiscoverDynamicCategories()
        {
            if (Settings.Categories.Count < 1 || DateTime.Now.Subtract(lastRefresh).TotalSeconds > refreshIntervalSeconds)
            {
                List<Category> cats = new List<Category>();
                string html = GetWebData("http://livetv.ru/en/allupcomingsports");
                foreach (Match m in new Regex(categoryRegex, RegexOptions.Singleline).Matches(html))
                {
                    RssLink cat = new RssLink();
                    cat.Thumb = m.Groups["thumb"].Value;
                    cat.Name = m.Groups["title"].Value;
                    cat.HasSubCategories = true;
                    cat.Url = m.Groups["subcats"].Value;
                    cat.Other = onlyShowCurrentlyPlaying;
                    cats.Add(cat);
                }
                Settings.Categories = new System.ComponentModel.BindingList<Category>();
                foreach (Category cat in cats)
                    Settings.Categories.Add(cat);

                lastRefresh = DateTime.Now;
            }
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (parentCategory.SubCategories == null || (bool)parentCategory.Other != onlyShowCurrentlyPlaying)
                parentCategory.SubCategories = parseSubCats(parentCategory);
            return parentCategory.SubCategories.Count;
        }

        List<Category> parseSubCats(Category parentCat)
        {
            List<Category> cats = new List<Category>();

            foreach (Match m in new Regex(subCategoryRegex).Matches((parentCat as RssLink).Url))
            {
                if (onlyShowCurrentlyPlaying && string.IsNullOrEmpty(m.Groups["live"].Value))
                    continue;
                RssLink cat = new RssLink();
                cat.Thumb = m.Groups["thumb"].Value;
                cat.Url = "http://livetv.ru" + m.Groups["url"].Value;
                cat.Name = m.Groups["name"].Value.Replace("&ndash;", "-");
                cat.Description = string.Format("{0}\r\n{1}", m.Groups["time"].Value, m.Groups["info"].Value);
                cat.ParentCategory = parentCat;
                cats.Add(cat);
            }
            parentCat.Other = onlyShowCurrentlyPlaying;
            return cats;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> vids = new List<VideoInfo>();
            string html = GetWebData((category as RssLink).Url);
            foreach (Match m in new Regex(videoRegex).Matches(html))
            {
                VideoInfo vid = new VideoInfo();
                string bitrate = string.IsNullOrEmpty(m.Groups["bitrate"].Value) ? "" : " " + m.Groups["bitrate"].Value;
                string info = string.IsNullOrEmpty(m.Groups["info"].Value) ? "" : " (" + m.Groups["info"].Value + ")";

                vid.Title = string.Format("{0}{1} - {2}%{3}", category.Name, bitrate, m.Groups["rating"].Value, info);
                vid.Description = category.Description;
                vid.ImageUrl = category.Thumb;
                vid.VideoUrl = m.Groups["url"].Value;
                vids.Add(vid);
            }
            return vids;
        }
    }
}

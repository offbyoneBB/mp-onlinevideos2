using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class WDRUtil : GenericSiteUtil
    {
        public override int DiscoverDynamicCategories()
        {
            int result = base.DiscoverDynamicCategories();
            Settings.Categories.AsQueryable().Where(c => c.Description == "audio").ToList().ForEach(c => Settings.Categories.Remove(c));
            foreach (Category c in Settings.Categories) c.Description = string.Empty;
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            var result = base.getVideoList(category);
            result.ForEach(v => 
            { 
                v.Title = Regex.Replace(v.Title, @".*?\:\s*?\d\d\.\d\d.\d\d\d\d,\s*", "");
                v.Title = v.Title.StartsWith(category.Name) ? v.Title.Substring(category.Name.Length) : v.Title;
                v.Title = v.Title.Trim(new char[] {':', ' ', ',', '-'});
            });
            return result;
        }
    }
}

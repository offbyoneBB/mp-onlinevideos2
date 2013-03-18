using System;
using System.Collections.Generic;

namespace OnlineVideos.Sites
{
    public class RockpalastUtil : GenericSiteUtil
    {
        public override int DiscoverDynamicCategories()
        {
            int res = base.DiscoverDynamicCategories();
            RssLink extra = new RssLink() { Url = baseUrl, Name = "Nur noch kurz online:", Other = 1 };
            Settings.Categories.Insert(0, extra);
            extra = new RssLink() { Url = baseUrl, Name = "Zuletzt hinzugekommen:", Other = 2 };
            Settings.Categories.Insert(1, extra);
            return res + 2;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string url = ((RssLink)category).Url;
            string data = GetWebData(url);
            string[] parts = data.Split(new[] { "wsArticleUnit" }, StringSplitOptions.RemoveEmptyEntries);
            int? n = category.Other as int?;
            if (n.HasValue)
                return Parse(url, parts[n.Value]);
            else
                return Parse(url, parts[3]);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites
{
    public class TheEscapistUtil : GenericSiteUtil
    {
        public override int DiscoverDynamicCategories()
        {
            base.DiscoverDynamicCategories();
            foreach (Category cat in Settings.Categories)
                cat.HasSubCategories = (((RssLink)cat).Url.EndsWith(@"videos/galleries"));
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string webData = GetWebData(((RssLink)category).Url);
            int p = webData.IndexOf(@"<div class='video_box_footer'>");
            if (p >= 0) webData = webData.Substring(0, p);
            return Parse(null, webData);
        }
    }
}

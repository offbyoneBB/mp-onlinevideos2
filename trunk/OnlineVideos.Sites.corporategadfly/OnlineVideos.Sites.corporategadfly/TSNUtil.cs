using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public class TSNUtil : CTVUtilBase
    {
        private Regex _episodeListRegex = new Regex(@"<dt><a\shref=""[^#]*#clip(?<episode>[^""]*)""\sonclick.*?Thumbnail:'(?<thumb>[^']*)'.*?Description:'(?<description>[^']*)'.*?Title:'(?<title>[^']*)'",
            RegexOptions.Compiled | RegexOptions.Singleline);

        public override string BaseUrl { get { return @"http://watch.tsn.ca"; } }

        public override string Swf { get { return @"http://watch.tsn.ca/Flash/player.swf?themeURL=http://watch.ctv.ca/themes/TSN/player/theme.aspx"; } }

        public override int StartingPanelLevel { get { return 2; } }

        public override string VideoLibraryParameter { get { return @"ShowId"; } }

        public override Regex EpisodeListRegex { get { return _episodeListRegex; } }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            string webData = GetWebData(BaseUrl + videoLibraryUri);

            if (!string.IsNullOrEmpty(webData))
            {
                foreach (Match m in mainCategoriesRegex.Matches(webData))
                {
                    RssLink cat = new RssLink();

                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    cat.Url = HttpUtility.HtmlDecode(m.Groups["url"].Value);
                    // this id will be used later on, so store it in the .Other property
                    cat.Other = HttpUtility.HtmlDecode(m.Groups["id"].Value);
                    cat.HasSubCategories = false;

                    Settings.Categories.Add(cat);
                }
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }
    }
}

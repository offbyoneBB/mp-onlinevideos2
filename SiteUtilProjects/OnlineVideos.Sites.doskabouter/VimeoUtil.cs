using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class VimeoUtil : GenericSiteUtil
    {
        private string searchToken;

        public override int DiscoverDynamicCategories()
        {
            string webData = GetWebData(@"http://vimeo.com/");
            Match m = Regex.Match(webData, @"class=""xsrft""\sname=""token""\svalue=""(?<token>[^""]*)""");
            if (m.Success)
                searchToken = m.Groups["token"].Value.Substring(0, 8);
            cookies = "searchtoken=" + searchToken;
            searchUrl = @"http://vimeo.com/search/videos/search:{0}/" + searchToken;

            return base.DiscoverDynamicCategories();
        }

        public override bool CanSearch { get { return true; } }

    }
}

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace OnlineVideos.Sites
{
    public class SBS6GemistUtil : GenericSiteUtil
    {
        [Category("OnlineVideosConfiguration"), Description("Alternate Url Regex")]
        string alternateUrlRegex = null;

        Regex regex_AlternateUrl;

        public override void Initialize(SiteSettings siteSettings)
        {
            RegexOptions defaultRegexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture;

            base.Initialize(siteSettings);
            regex_AlternateUrl = new Regex(alternateUrlRegex, defaultRegexOptions);
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string url = ((RssLink)category).Url;
            if (!url.StartsWith(@"http://www.sbs6.nl"))
            {
                string webData = GetWebData(url);
                Match m = regex_AlternateUrl.Match(webData);
                url = m.Groups["url"].Value;
            }
            return base.Parse(url, null);
        }
    }
}

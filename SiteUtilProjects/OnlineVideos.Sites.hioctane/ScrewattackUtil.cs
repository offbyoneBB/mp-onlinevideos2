using System;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class ScrewattackUtil : GenericSiteUtil
    {
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for videoUrl. Group names: 'vid', 'pid'.")]
        string urlRegEx;

        Regex regEx_Url;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            regEx_Url = new Regex(urlRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
        }

        public override String getUrl(VideoInfo video)
        {
            string data = GetWebData(video.VideoUrl);

            if (!string.IsNullOrEmpty(data))
            {
                Match m = regEx_Url.Match(data);
                if (m.Success)
                {
                    string url = "http://v.giantrealm.com/sax/" + m.Groups["pid"].Value + "/" + m.Groups["vid"].Value;
                    data = GetWebData(url);
                    if (!string.IsNullOrEmpty(data))
                    {
                        m = Regex.Match(data, @"<file-hq>\s*(.+?)\s*</file-hq>");
                        if (m.Success) return m.Groups[1].Value;

                        m = Regex.Match(data, @"<file>\s*(.+?)\s*</file>");
                        if (m.Success) return m.Groups[1].Value;
                    }
                }
            }
            return null;
        }
    }
}
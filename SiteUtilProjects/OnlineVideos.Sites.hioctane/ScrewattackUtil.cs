using System;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Collections.Generic;

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
                    XmlDocument xDoc = GetWebData<XmlDocument>(url);

                    XmlElement fileElem = (XmlElement)xDoc.SelectSingleNode("//*[local-name() = 'file']");
                    XmlElement fileHQElem = (XmlElement)xDoc.SelectSingleNode("//*[local-name() = 'file-hq']");

                    if (fileElem != null || fileHQElem != null)
                    {
                        video.PlaybackOptions = new Dictionary<string, string>();
                        if (fileElem != null) video.PlaybackOptions.Add("SD", fileElem.InnerText.Trim());
                        if (fileHQElem != null) video.PlaybackOptions.Add("HD", fileHQElem.InnerText.Trim());
                        return video.PlaybackOptions.First().Value;
                    }
                }
            }
            return null;
        }
    }
}
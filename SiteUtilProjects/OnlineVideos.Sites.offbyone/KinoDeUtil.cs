using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public class KinoDeUtil : GenericSiteUtil
    {
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a search results html page for videos. Group names: 'VideoUrl', 'Title'.")]
        protected string searchResultsListRegEx;

        protected Regex regEx_searchResultsList;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            if (!string.IsNullOrEmpty(searchResultsListRegEx)) regEx_searchResultsList = new Regex(searchResultsListRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
        }

        public override List<ISearchResultItem> Search(string query, string category = null)
        {
            nextPageAvailable = false;
            string data = GetWebData(searchUrl, string.Format(searchPostString, query), referer: "http://www.kino.de/showroom/", allowUnsafeHeader: allowUnsafeHeaders);

            var results = new List<ISearchResultItem>();

            if (regEx_searchResultsList != null)
            {
                Match m = regEx_searchResultsList.Match(data);
                while (m.Success)
                {
                    VideoInfo videoInfo = CreateVideoInfo();
                    videoInfo.Title = HttpUtility.HtmlDecode(m.Groups["Title"].Value);
                    // get, format and if needed absolutify the video url
                    videoInfo.VideoUrl = m.Groups["VideoUrl"].Value;
                    if (!string.IsNullOrEmpty(videoListRegExFormatString)) videoInfo.VideoUrl = string.Format(videoListRegExFormatString, videoInfo.VideoUrl);
                    videoInfo.VideoUrl = ApplyUrlDecoding(videoInfo.VideoUrl, videoListUrlDecoding);
					if (!Uri.IsWellFormedUriString(videoInfo.VideoUrl, System.UriKind.Absolute)) videoInfo.VideoUrl = new Uri(new Uri(baseUrl), videoInfo.VideoUrl).AbsoluteUri;

                    results.Add(videoInfo);

                    m = m.NextMatch();
                }
            }

            return results;
        }
    }
}

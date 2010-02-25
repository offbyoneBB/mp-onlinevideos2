using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Web;
using RssToolkit.Rss;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Description of GenericSiteUtil.
    /// </summary>
    public class GenericSiteUtil : SiteUtilBase
    {
        [Category("OnlineVideosConfiguration"), Description("Format string used as Url for getting the results of a search. {0} will be replaced with the query.")]
        string searchUrl;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page retrieved from the video url that was found in the rss.")]
        string fileUrlRegEx;
        [Category("OnlineVideosConfiguration"), Description("FormatString that will take the groups from the fileUrlRegEx as parameters.")]
        string fileUrlFormat;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to match on the video url retrieved for the item in the rss. Only used if set.")]
        string videoUrlRegEx;
        [Category("OnlineVideosConfiguration"), Description("Format string applied to the video url of an item that was found in the rss. If videoUrlRegEx is set those groups will be taken as parameters.")]
        string videoUrlFormatString = "{0}";

        Regex regEx_FileUrl, regEx_VideoUrl;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            if (!string.IsNullOrEmpty(fileUrlRegEx)) regEx_FileUrl = new Regex(fileUrlRegEx, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            if (!string.IsNullOrEmpty(videoUrlRegEx)) regEx_VideoUrl = new Regex(videoUrlRegEx, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
        }

        public override String getUrl(VideoInfo video)
        {
            string resultUrl = "";

            if (regEx_VideoUrl != null)
            {
                Match m = regEx_VideoUrl.Match(video.VideoUrl);
                if (m.Success)
                {
                    string[] regExGroupValues = new string[m.Groups.Count - 1];
                    for (int i = 1; i < m.Groups.Count; i++) regExGroupValues[i - 1] = m.Groups[i].Value;
                    video.VideoUrl = HttpUtility.UrlDecode(string.Format(videoUrlFormatString, regExGroupValues));
                }
            }
            else
            {
                string.Format(videoUrlFormatString, video.VideoUrl);
            }

            if (regEx_FileUrl == null)
            {
                resultUrl = GetRedirectedUrl(video.VideoUrl);
                if (resultUrl.ToLower().EndsWith(".asx"))
                {
                    resultUrl = ParseASX(resultUrl)[0];
                }
            }
            else
            {
                string html = GetWebData(video.VideoUrl);
                if (!string.IsNullOrEmpty(html))
                {
                    Match urlField = regEx_FileUrl.Match(html);
                    if (urlField.Success)
                    {
                        string[] regExGroupValues = new string[urlField.Groups.Count - 1];
                        for (int i = 1; i < urlField.Groups.Count; i++) regExGroupValues[i - 1] = urlField.Groups[i].Value;
                        resultUrl = HttpUtility.UrlDecode(string.Format(fileUrlFormat, regExGroupValues));
                    }
                }
            }

            return resultUrl;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> loVideoList = new List<VideoInfo>();
            VideoInfo video;
            if (category is RssLink)
            {
                foreach (RssItem rssItem in GetWebDataAsRss(((RssLink)category).Url).Channel.Items)
                {
                    video = VideoInfo.FromRssItem(rssItem, regEx_FileUrl != null, new Predicate<string>(isPossibleVideo));
                    // only if a video url was set, add this Video to the list
                    if (!string.IsNullOrEmpty(video.VideoUrl)) loVideoList.Add(video);
                }
            }
            else if (category is Group)
            {
                foreach (Channel channel in ((Group)category).Channels)
                {
                    video = new VideoInfo();
                    video.Title = channel.StreamName;
                    video.VideoUrl = channel.Url;
                    video.ImageUrl = channel.Thumb;
                    loVideoList.Add(video);
                }
            }
            return loVideoList;
        }

        #region Search

        public override bool CanSearch { get { return !string.IsNullOrEmpty(searchUrl); } }

        public override List<VideoInfo> Search(string query)
        {
            string url = string.Format(searchUrl, query);
            return getVideoList(new RssLink() { Url = url });
        }

        #endregion
    }
}

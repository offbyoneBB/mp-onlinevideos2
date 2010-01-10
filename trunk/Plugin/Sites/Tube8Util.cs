using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Utility used to browse videos of tube8.com
    /// </summary>
    public class Tube8Util : SiteUtilBase
    {
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for videos.")]
        string videoListRegEx = @"<div\sid=""video_i\d+"">\s*
<a\shref=""(?<VideoUrl>[^""]+)"">\s*
<img\s(?:(?!src).)*src=""(?<ImageUrl>[^""]+)""\s*/></a>\s*
<h2><a\shref=""[^""]*""\stitle=""(?<Title>[^""]+)"">\s*
(?:(?!<span).)*<span[^>]*>(?<Duration>[^<]*)</span>";

        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for a next page link.")]
        string nextPageRegEx = @"<li\sclass=""button-pag[^""]*""><a\shref=""(?<url>[^""]+)"">NEXT";
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for a previous page link.")]
        string prevPageRegEx = @"<li\sclass=""button-pag[^""]*""><a\shref=""(?<url>[^""]+)"">PREVIOUS";
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page embedding a video for a link to the actual video.")]
        string playlistUrlRegEx = @"var\svideourl=""(?<url>[^""]+)""";

        [Category("OnlineVideosConfiguration"), Description("Url used for getting the results of a search. {0} will be replaced with the query.")]
        string searchUrl = "http://www.tube8.com/search.html?q={0}";

        Regex regEx_VideoList, regEx_PlaylistUrl, regEx_NextPage, regEx_PrevPage;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_VideoList = new Regex(videoListRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
            regEx_PlaylistUrl = new Regex(playlistUrlRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant);
            regEx_NextPage = new Regex(nextPageRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant);
            regEx_PrevPage = new Regex(prevPageRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant);
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            return Parse(((RssLink)category).Url);
        }

        public override String getUrl(VideoInfo video)
        {
            string dataPage = GetWebData(video.VideoUrl);
            if (dataPage.Length > 0)
            {
                Match m = regEx_PlaylistUrl.Match(dataPage);
                if (m.Success) return m.Groups["url"].Value;
            }
            return video.VideoUrl;
        }

        List<VideoInfo> Parse(string url)
        {
            List<VideoInfo> loVideoList = new List<VideoInfo>();
            string data = GetWebData(url);
            if (data.Length > 0)
            {
                Match m = regEx_VideoList.Match(data);
                while (m.Success)
                {
                    VideoInfo videoInfo = new VideoInfo();
                    videoInfo.Title = m.Groups["Title"].Value;
                    videoInfo.VideoUrl = m.Groups["VideoUrl"].Value;
                    videoInfo.ImageUrl = m.Groups["ImageUrl"].Value;
                    videoInfo.Length = m.Groups["Duration"].Value;
                    loVideoList.Add(videoInfo);
                    m = m.NextMatch();
                }

                // check for previous page link
                Match mPrev = regEx_PrevPage.Match(data);
                if (mPrev.Success)
                {
                    previousPageAvailable = true;
                    previousPageUrl = mPrev.Groups["url"].Value;

                    if (!Uri.IsWellFormedUriString(previousPageUrl, System.UriKind.Absolute))
                    {
                        Uri uri = null;
                        if (Uri.TryCreate(new Uri(url), previousPageUrl, out uri))
                        {
                            previousPageUrl = uri.ToString();
                        }
                        else
                        {
                            previousPageAvailable = false;
                            previousPageUrl = "";
                        }
                    }
                }
                else
                {
                    previousPageAvailable = false;
                    previousPageUrl = "";
                }

                // check for next page link
                Match mNext = regEx_NextPage.Match(data);
                if (mNext.Success)
                {
                    nextPageAvailable = true;
                    nextPageUrl = mNext.Groups["url"].Value;
                }
                else
                {
                    nextPageAvailable = false;
                    nextPageUrl = "";
                }
            }
            return loVideoList;
        }

        #region Next/Previous Page

        string nextPageUrl = "";
        bool nextPageAvailable = false;
        public override bool HasNextPage
        {
            get { return nextPageAvailable; }
        }

        string previousPageUrl = "";
        bool previousPageAvailable = false;
        public override bool HasPreviousPage
        {
            get { return previousPageAvailable; }
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return Parse(nextPageUrl);
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            return Parse(previousPageUrl);
        }

        #endregion

        #region Search

        public override bool CanSearch { get { return true; } }

        public override List<VideoInfo> Search(string query)
        {
            return Parse(string.Format(searchUrl, query));
        }

        #endregion
    }
}

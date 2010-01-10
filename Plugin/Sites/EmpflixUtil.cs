using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Utility used to browse videos of empflix.com
    /// </summary>
    public class EmpflixUtil : SiteUtilBase
    {
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for videos.")]
        string videoListRegEx = @"<a\shref=""(?<VideoUrl>http\://www\.empflix\.com/view\.php\?id\=\d+)""\s*title=""(?<Title>[^""]+)""[^>]*>\s*
<img\ssrc=""\s*(?<ImageUrl>http\://[^""]+)""[^>]*>\s*
(?:(?!<p).)*\s*
<p\sclass=""length"">(?<Duration>[^<]*)</p>";

        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for a next page link.")]
        string nextPageRegEx = @"<a\sonclick=""killVideoThumbs\(\)\;""\shref=""(?<url>.*)"">next\s&gt;&gt;</a>";
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for a previous page link.")]
        string prevPageRegEx = @"<a\sonclick=""killVideoThumbs\(\)\;""\shref=""(?<url>.*)"">&lt;&lt;\sprev</a>";
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page embedding a video for a link to the actual video.")]
        string playlistUrlRegEx = @"so\.addVariable\('config',\s*'(?<url>[^']+)'\);";
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the playlist data for the playback url.")]
        string fileUrlRegEx = @"<file>(?<url>[^<]+)</file>";

        [Category("OnlineVideosConfiguration"), Description("Url used for getting the results of a search. Search is done via POST.")]
        string searchUrl = "http://www.empflix.com/search.php";

        Regex regEx_VideoList, regEx_PlaylistUrl, regEx_NextPage, regEx_PrevPage, regEx_FileUrl;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_VideoList = new Regex(videoListRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_PlaylistUrl = new Regex(playlistUrlRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant);
            regEx_NextPage = new Regex(nextPageRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant);
            regEx_PrevPage = new Regex(prevPageRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant);
            regEx_FileUrl = new Regex(fileUrlRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant);
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            return Parse(GetWebData(((RssLink)category).Url));
        }

        public override String getUrl(VideoInfo video)
        {
            string dataPage = GetWebData(video.VideoUrl);
            if (dataPage.Length > 0)
            {
                Match m = regEx_PlaylistUrl.Match(dataPage);
                if (m.Success)
                {
                    string playlistUrl = m.Groups["url"].Value;
                    playlistUrl = System.Web.HttpUtility.UrlDecode(playlistUrl);
                    dataPage = GetWebData(playlistUrl);
                    if (dataPage.Length > 0)
                    {
                        m = regEx_FileUrl.Match(dataPage);
                        if (m.Success)
                        {
                            return m.Groups["url"].Value + "&filetype=.flv";
                        }
                    }
                }
            }
            return video.VideoUrl;
        }

        List<VideoInfo> Parse(string data)
        {
            List<VideoInfo> loVideoList = new List<VideoInfo>();
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
            return Parse(GetWebData("http://www.empflix.com/" + nextPageUrl));
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            return Parse(GetWebData("http://www.empflix.com/" + previousPageUrl));
        }

        #endregion

        #region Search

        public override bool CanSearch { get { return true; } }

        public override List<VideoInfo> Search(string query)
        {
            string dataPage = GetWebDataFromPost(searchUrl, "what=" + query);
            return Parse(dataPage);
        }

        #endregion
    }
}

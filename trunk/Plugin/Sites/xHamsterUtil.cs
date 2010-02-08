using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using MediaPortal.GUI.Library;

namespace OnlineVideos.Sites
{
    public class xHamsterUtil : SiteUtilBase
    {
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for videos.")]
        string videoListRegEx = @"<img\ssrc='(?<ImageUrl>[^']+)'[^/]*/>(?:(?!<div).)*
<div\sclass=""moduleFeaturedTitle"">\s*
<a\shref=""(?<VideoUrl>[^""]+)"">(?<Title>[^<]+)</a>\s*
</div>\s*
<div\sclass=""moduleFeaturedDetails"">Runtime:\s*(?<Duration>[^<]+)<";

        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for a next page link.")]
        string nextPageRegEx = @"<SPAN\sclass=navNext><A\sHREF=""(?<url>.+)"">Next</A></SPAN>";
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for a previous page link.")]
        string prevPageRegEx = @"<SPAN\sclass=navPrev><A\sHREF=""(?<url>.+)"">Prev</A></SPAN>";
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the html page for the playback url.")]
        string fileUrlRegEx = @"'srv':\s'(?<srv>[^']+)',\s*
(?:'[^']+':\s'[^']+',\s*)?
'file':\s'(?<file>[^']+)'";
        
        [Category("OnlineVideosConfiguration"), Description("Url used for getting the results of a search. {0} will be replaced with the query.")]
        string searchUrl = "http://www.xhamster.com/search.php?q={0}";

        Regex regEx_VideoList, regEx_NextPage, regEx_PrevPage, regEx_FileUrl;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_VideoList = new Regex(videoListRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
            regEx_NextPage = new Regex(nextPageRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            regEx_PrevPage = new Regex(prevPageRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            regEx_FileUrl = new Regex(fileUrlRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            return Parse(GetWebData(((RssLink)category).Url));
        }
        public override String getUrl(VideoInfo video)
        {
            string dataPage = GetWebData("http://www.xhamster.com" + video.VideoUrl);
            if (dataPage.Length > 0)
            {
                Match m = regEx_FileUrl.Match(dataPage);
                if (m.Success)
                {
                    string result_url = string.Format("{0}flv2/{1}", m.Groups["srv"], m.Groups["file"]);
                    return result_url;
                }
            }
            return "";
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
                    videoInfo.Title = System.Web.HttpUtility.HtmlDecode(m.Groups["Title"].Value);
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
        
        #region Next|Previous Page

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
            return Parse(GetWebData("http://www.xhamster.com" + nextPageUrl));
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            return Parse(GetWebData("http://www.xhamster.com" + previousPageUrl));
        }

        #endregion

        #region Search

        public override bool CanSearch { get { return true; } }

        public override List<VideoInfo> Search(string query)
        {
            return Parse(GetWebData(string.Format(searchUrl, query)));
        }

        #endregion
    }
}

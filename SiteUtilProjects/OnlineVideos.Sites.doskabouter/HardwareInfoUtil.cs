using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Xml;
using System.IO;
using System.Web;
using System.Net;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class HardwareInfoUtil : SiteUtilBase
    {
        private string videoListRegex = @"class=""videothumb""><a\shref=""(?<url>[^""]+)"".*?img\ssrc=""(?<thumb>[^""]+)"".*?class=""videotitle"">(?<title>[^<]+)<.*?class=""articleleft"">(?<date>[^<]+)<.*?colspan=[^>]+>(?<descr>[^<]+)<";
        private string videoUrlRegex = @"'file=(?<url>[^&]+)&";
        private string otherUrlRegex = @"<title>(?<title>[^<]+)</title><location>(?<url>[^<]+)<";

        private string baseUrl;
        private string searchQuery = String.Empty;
        private int pageNr = 0;
        private bool hasNextPage;

        public HardwareInfoUtil()
        {
        }

        private Regex regEx_VideoList;
        private Regex regEx_VideoUrl;
        private Regex regEx_OtherUrl;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            regEx_VideoList = new Regex(videoListRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
            regEx_VideoUrl = new Regex(videoUrlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
            regEx_OtherUrl = new Regex(otherUrlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
        }

        public override bool HasMultipleVideos
        {
            get { return true; }
        }

        public override String getUrl(VideoInfo video)
        {
            string webData = GetWebData(video.VideoUrl);
            Match m = regEx_VideoUrl.Match(webData);
            if (m.Success)
                return m.Groups["url"].Value;

            return video.VideoUrl;
        }

        public override List<VideoInfo> getOtherVideoList(VideoInfo video)
        {
            List<VideoInfo> videos = new List<VideoInfo>();

            string webData = GetWebData(video.VideoUrl);
            Match m = regEx_VideoUrl.Match(webData);
            if (!m.Success) return videos;

            webData = GetWebData(m.Groups["url"].Value);

            if (!string.IsNullOrEmpty(webData))
            {
                m = regEx_OtherUrl.Match(webData);
                while (m.Success)
                {
                    VideoInfo newvideo = new VideoInfo();
                    newvideo.Title2 = HttpUtility.HtmlDecode(HttpUtility.HtmlDecode(m.Groups["title"].Value));
                    newvideo.Title = video.Title + '-' + newvideo.Title2;
                    newvideo.VideoUrl = HttpUtility.HtmlDecode(m.Groups["url"].Value);
                    newvideo.ImageUrl = video.ImageUrl;
                    newvideo.Description = video.Description;
                    videos.Add(newvideo);
                    m = m.NextMatch();
                }
            }
            return videos;
        }


        public override bool CanSearch
        {
            get { return true; }
        }

        public override List<VideoInfo> Search(string query)
        {
            pageNr = 0;
            searchQuery = query;
            return getPagedVideoList("http://www.hardware.info/nl-NL/video/");
        }

        public override bool HasNextPage
        {
            get { return hasNextPage; }
        }

        public override bool HasPreviousPage
        {
            get { return pageNr > 0; }
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            pageNr++;
            return getPagedVideoList(baseUrl);
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            pageNr--;
            return getPagedVideoList(baseUrl);
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            RssLink rssLink = (RssLink)category;
            baseUrl = rssLink.Url;
            pageNr = 0;
            searchQuery = String.Empty;
            return getPagedVideoList(baseUrl);
        }

        private List<VideoInfo> getPagedVideoList(string url)
        {
            string webData;
            if (pageNr == 0 && searchQuery == String.Empty)
                webData = GetWebData(url);
            else
            {
                webData = GetWebDataFromPost(url,
                    String.Format("sSearchTerms={0}&iStart={1}", searchQuery, pageNr * 25));
            }
            List<VideoInfo> videos = new List<VideoInfo>();
            if (!string.IsNullOrEmpty(webData))
            {
                Match m = regEx_VideoList.Match(webData);
                while (m.Success)
                {
                    VideoInfo video = new VideoInfo();

                    video.Title = HttpUtility.HtmlDecode(HttpUtility.HtmlDecode(m.Groups["title"].Value));
                    video.VideoUrl = HttpUtility.HtmlDecode(m.Groups["url"].Value);
                    video.ImageUrl = HttpUtility.HtmlDecode(m.Groups["thumb"].Value);                    
                    video.Description = HttpUtility.HtmlDecode(m.Groups["descr"].Value) + '\n' + m.Groups["date"].Value;

                    videos.Add(video);
                    m = m.NextMatch();
                }
                hasNextPage = webData.Contains(@"value=""Vorige");

            }
            return videos;

        }

    }
}

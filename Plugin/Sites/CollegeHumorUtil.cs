using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Windows.Forms;
using System.Net;
using System.IO;

namespace OnlineVideos.Sites
{
    public class CollegeHumorUtil : SiteUtilBase
    {

        string baseUrl = "http://www.collegehumor.com/";
        int maxPages = 0;
        int pageCounter = 0;
        string pageUrl = "";

        //<li><a href="/videos">Recent</a></li>
        string categoryRegex = @"<li><a\shref=""(?<url>[^""]+)"">(?<title>[^<]+)</a>";

        /*	<a href="/video:1928558" class="video_link" title="The iPad is a Comedy Gold Mi" data-json="{context: 'YTozOntpOjA7czoxMzoidmlld2VkX3ZpZGVvcyI7aToxO2E6NTp7czoxMjoiY29udGV4dF9uYW1lIjtzOjI5OiJNb3N0IFZpZXdlZCBWaWRlb3MgVGhpcyBNb250aCI7czo5OiJkYXlfcmFuZ2UiO2k6MzA7czo4OiJjYXRlZ29yeSI7TjtzOjg6ImJhc2VfdXJsIjtzOjMwOiIvdmlkZW9zL21vc3Qtdmlld2VkL3RoaXMtbW9udGgiO3M6MTM6ImNvbnRfYXV0b3BsYXkiO2I6MDt9aToyO2k6MjA7fQ%3D%3D'}">
		<img src="http://2.media.collegehumor.com/collegehumor/ch6/3/8/collegehumor.4ec89e13138f8e4751bf3da861d003e5.jpg" alt="" class="media_thumb" alt="funny video - The iPad is a Comedy Gold Mine" width="150" height="113" />
        */
        string videoListRegex = @"<a\shref=""(?<url>[^""]+)""\sclass=""video_link""\stitle=""(?<title>[^""]+)""\sdata-json=""{context[^<]*<img\ssrc=""(?<thumb>[^""]+)""\salt=";

        //<span class="ellipses">&hellip;</span><a href="/videos/page:610">610</a>
        string maxPagesRegex = @"<span\sclass=""ellipses"">&hellip;</span><a\shref=""(?<url>[^""]+)"">(?<title>[^<]+)</a>";

        Regex regEx_Category;
        Regex regEx_VideoList;
        Regex regEx_MaxPages;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_Category = new Regex(categoryRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_MaxPages = new Regex(maxPagesRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_VideoList = new Regex(videoListRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            string data = GetWebData(baseUrl);
            data = data.Substring(data.IndexOf("nav-b"), data.IndexOf("nav-c") - data.IndexOf("nav-b"));
            string xmlUrl = String.Empty;

            if (!string.IsNullOrEmpty(data))    
            {
                Match m = regEx_Category.Match(data);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Name = HttpUtility.HtmlDecode( m.Groups["title"].Value );
                    cat.Url = baseUrl + m.Groups["url"].Value;

                    if(!cat.Name.Contains("Playlist"))
                        Settings.Categories.Add(cat);
                    m = m.NextMatch();
                }
                Settings.DynamicCategoriesDiscovered = true;
                return Settings.Categories.Count;
            }
            return 0;
        }

        public override String getUrl(VideoInfo video)
        {
            string url = "http://www.collegehumor.com/moogaloop" + video.VideoUrl.Substring(video.VideoUrl.LastIndexOf("/"));
            string data = GetWebData(url);
            if (!string.IsNullOrEmpty(data))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(data);
                XmlElement root = doc.DocumentElement;
                return root.SelectSingleNode("./video/file").InnerText;
            }
            return null;
        }

        protected List<VideoInfo> getVideoListForCurrentCategory()
        {
            List<VideoInfo> videos = new List<VideoInfo>();

            string data = GetWebData(pageUrl);
            if (!string.IsNullOrEmpty(data))
            {
                Match m = regEx_VideoList.Match(data);
                while (m.Success)
                {
                    VideoInfo video = new VideoInfo();

                    video.Title = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    video.VideoUrl = baseUrl + m.Groups["url"].Value;
                    video.ImageUrl = m.Groups["thumb"].Value;

                    videos.Add(video);
                    m = m.NextMatch();
                }
            }
            return videos;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            pageUrl = (category as RssLink).Url;
            string data = GetWebData(pageUrl);
            pageCounter = 1;

            if (!string.IsNullOrEmpty(data))
            {
                Match m = regEx_MaxPages.Match(data);
                if (m.Success)
                {
                    maxPages = Convert.ToInt32(m.Groups["title"].Value);
                    pageUrl = baseUrl + m.Groups["url"].Value;
                    pageUrl = pageUrl.Substring(0, pageUrl.LastIndexOf("/"));
                    pageUrl = pageUrl + "/";
                }
                else
                    maxPages = 1;
            }
            return getVideoListForCurrentCategory();
        }

        public override bool HasNextPage
        {
            get { return pageCounter+1 < maxPages; }
        }

        public override bool HasPreviousPage
        {
            get { return pageCounter-1 > 0; }
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            pageCounter++;
            pageUrl = pageUrl.Substring(0, pageUrl.LastIndexOf("/"));
            pageUrl = pageUrl + "/page:" + pageCounter;
            return getVideoListForCurrentCategory();
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            pageCounter--;
            pageUrl = pageUrl.Substring(0, pageUrl.LastIndexOf("/"));
            pageUrl = pageUrl + "/page:" + pageCounter;
            return getVideoListForCurrentCategory();
        }
    }
}
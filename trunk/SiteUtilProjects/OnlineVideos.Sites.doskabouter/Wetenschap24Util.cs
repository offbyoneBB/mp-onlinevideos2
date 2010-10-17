using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Xml;
using System.IO;
using System.Web;
using System.Net;
//using RssToolkit.Rss;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class Wetenschap24Util : SiteUtilBase
    {
        private int pageNr = 1;
        private bool hasNextPage = false;
        private RssLink bareCategory;
        private Dictionary<String, String> filterCategoriesList;
        private string defaultCategory = "latest";
        private string FilterCategory;

        private string baseUrl = "http://www.wetenschap24.nl";

        //<label for="channel-alle"><a href="/videos/highlight/alle/" rel=""><span>Alle kanalen</span></a></label>
        private string categoryRegex = @"<label\sfor=""[^""]*""><a\shref=""(?<url>[^""]+)""\srel=""""><span>(?<title>[^<]+)</span>";

        /*
				<p class="show-desc">
					<a href="/video/bekijk/hoe-komen-we-aan-onze-vooroordelen-wisebit-306.htm" rel=\"Link naar video detailpagina\">
					<strong>Hoe komen we aan onze vooroordelen? Wisebit 306</strong>
					 Wisebit nummer 306...					</a>
				</p>
				<!-- onmouseover=\"$(this).parent().children('p.show-desc').show();\" -->
				<img src=\"/data/images/2009/12/11/1189306.mobiel_0_video_thumb.jpg\" width=\"160\" height=\"90\" alt=\"\"   />

				<div class=\"meta-info\">
					<h3><span>Hoe komen we aan on...</span></h3>
					<ul>
						<li class=\"rating\"><span class=\"stars-0\">Gewaardeerd met 0 sterren</span></li>
						<li class=\"comments\"><em><span>0</span></em></li>
						<li class=\"duration\"><em><span>00:01:41</span></em></li>
					</ul>
				</div>
         */
        private string videoListRegex = @"<p\sclass=""show-desc"">\s+<a\shref=""(?<url>[^""]+)""\srel=""[^""]+"">\s+<strong>(?<title>[^<]+)</strong>\s+(?<description>.+?)</a>\s+</p>\s+<[^>]+>\s+<img\ssrc=""(?<thumb>[^""]+)""";
        //						,file:"http://cgi.omroep.nl/cgi-bin/streams?/teleacnot/scienceonline/wmv/306.mobiel.wmv"
        private string urlRegex = @",file:""(?<url>[^""]+)""";

        public Wetenschap24Util()
        {
            filterCategoriesList = new Dictionary<string, string>();
            filterCategoriesList.Add("Laatst toegevoegd", defaultCategory);
            filterCategoriesList.Add("Meest bekeken", "mostviewed");
            filterCategoriesList.Add("Hoogst gewaardeerd", "highestrating");
        }

        private Regex regEx_Category;
        private Regex regEx_VideoList;
        private Regex regEx_GetUrl;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_Category = new Regex(categoryRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_GetUrl = new Regex(urlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_VideoList = new Regex(videoListRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            string data = GetWebData("http://www.wetenschap24.nl/video/videotab/type/latest/channels/alle/page/1/");
            if (!string.IsNullOrEmpty(data))
            {

                Match m = regEx_Category.Match(data);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    string url = baseUrl + m.Groups["url"].Value;
                    url = url.Replace("/videos/", "/video/videotab/type/");
                    if (url.EndsWith("1/"))
                        url = url.Substring(0, url.Length - 2);
                    int p = url.LastIndexOf('/', url.Length - 2);
                    url = url.Insert(p, "/channels");
                    url = url.Replace("/latest/", "/{0}/");

                    cat.Url = url + "page/";
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
            string url = GetRedirectedUrl(video.VideoUrl);

            string data = GetWebData(url);
            string s = GetSubString(data, @"<center><iframe src=""", @"""");
            if (s != string.Empty && (s.StartsWith(@"http://player.omroep.nl/")))
                return GenericSiteUtil.GetVideoUrl(s, video);

            if (!string.IsNullOrEmpty(data))
            {
                Match m = regEx_GetUrl.Match(data);
                if (m.Success)
                {
                    //return ParseASX(m.Groups["url"].Value)[0];
                    return m.Groups["url"].Value;
                }
            }
            return null;
        }
        /// <summary>
        /// This will be called to find out if there is a next page for the videos that have just been returned 
        /// by a call to <see cref="getVideoList"/>. If returns true, the menu entry for "next page" will be enabled, otherwise disabled.<br/>
        /// Example: <see cref="MtvMusicVideosUtil"/><br/>
        /// default: always false
        /// </summary>
        public override bool HasNextPage
        {
            get { return hasNextPage; }
        }

        /// <summary>
        /// This function should return the videos of the next page. No state is given, 
        /// so the class implementation has to remember and set the current category and page itself.
        /// It will only be called if <see cref="HasNextPage"/> returned true on the last call 
        /// and after the user selected the menu entry for "next page".<br/>
        /// Example: <see cref="MtvMusicVideosUtil"/><br/>
        /// default: empty list
        /// </summary>
        /// <returns>a list of <see cref="VideoInfo"/> objects for the next page of the last queried category.</returns>
        public override List<VideoInfo> getNextPageVideos()
        {
            pageNr++;
            return getPagedVideoList(bareCategory);
        }

        /// <summary>
        /// This will be called to find out if there is a previous page for the videos that have just been returned 
        /// by a call to <see cref="getVideoList"/>. If returns true, the menu entry for "previous page" will be enabled, otherwise disabled.<br/>
        /// Example: <see cref="MtvMusicVideosUtil"/><br/>
        /// default: always false
        /// </summary>
        public override bool HasPreviousPage
        {
            get { return pageNr > 1; }
        }

        /// <summary>
        /// This function should return the videos of the previous page. No state is given, 
        /// so the class implementation has to remember and set the current category and page itself.
        /// It will only be called if <see cref="HasPreviousPage"/> returned true on the last call 
        /// and after the user selected the menu entry for "previous page".<br/>
        /// Example: <see cref="MtvMusicVideosUtil"/><br/>
        /// default: empty list
        /// </summary>
        /// <returns>a list of <see cref="VideoInfo"/> objects for the previous page of the last queried category.</returns>
        public override List<VideoInfo> getPreviousPageVideos()
        {
            pageNr--;
            return getPagedVideoList(bareCategory);
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            RssLink rssLink = (RssLink)category;
            bareCategory = rssLink;
            pageNr = 1;
            FilterCategory = defaultCategory;
            return getPagedVideoList(rssLink);
        }

        private List<VideoInfo> getPagedVideoList(RssLink category)
        {
            string url = category.Url + pageNr.ToString();
            url = String.Format(url, FilterCategory);
            string referer = url.Replace("/video/videotab/type/", "/videos/");
            referer = referer.Replace("/page/", "/");
            string webData = GetWebData(url, null, referer, null, true);
            List<VideoInfo> videos = new List<VideoInfo>();
            if (!string.IsNullOrEmpty(webData))
            {

                Match m = regEx_VideoList.Match(webData);
                while (m.Success)
                {
                    VideoInfo video = new VideoInfo();

                    video.Title = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    video.VideoUrl = baseUrl + m.Groups["url"].Value;
                    video.ImageUrl = baseUrl + m.Groups["thumb"].Value;
                    video.Description = m.Groups["description"].Value;
                    videos.Add(video);
                    m = m.NextMatch();
                }

                hasNextPage = webData.IndexOf(@"page//');""><span><img src=""/images/buttons/next-button") == -1;
            }
            return videos;

        }

        public override bool HasFilterCategories
        {
            get { return true; }
        }

        public override Dictionary<string, string> GetSearchableCategories()
        {
            return filterCategoriesList;
        }

        public override List<VideoInfo> Search(string query, string category)
        {
            pageNr = 1;
            FilterCategory = category;
            return getPagedVideoList(bareCategory);
        }


        private string GetSubString(string s, string start, string until)
        {
            int p = s.IndexOf(start);
            if (p == -1) return String.Empty;
            p += start.Length;
            int q = s.IndexOf(until, p);
            if (q == -1) return s.Substring(p);
            return s.Substring(p, q - p);
        }
    }
}

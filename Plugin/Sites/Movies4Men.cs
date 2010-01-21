using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Net;

namespace OnlineVideos.Sites
{
    public class Movies4MenUtil : SiteUtilBase
    {
        private int pageNr = 1;
        private bool hasNextPage = false;
        private RssLink bareCategory;
        private string currentSearch = null;

        private string baseUrl = "http://www.viewtv.co.uk";
        //"ToggleCategory('ctl00_ChannelSelector1_CategoryGroup326');document.location.href='/movies4men/AdventureandActionMovies'">Adventure and Action Movies</div>
        private string categoryRegex = @"ctl00_ChannelSelector1_CategoryGroup(?<groupid>[^']+)'\);document\.location\.href='(?<url>[^']+)'"">(?<title>[^<]+)</div>";
        //class="SearchResult Link" onclick="document.location.href='/movies4men/YellowHairandtheFortressofGold'"><table id="ctl00_ContentPlaceHolder1_SearchResult5575" cellpadding="0" cellspacing="0" class="">
        //<tr id="ctl00_ContentPlaceHolder1_ctl01">
        //	<td id="ctl00_ContentPlaceHolder1_ctl02" class="Col1Of2" colspan="1" rowspan="3"><div id="ctl00_ContentPlaceHolder1_ctl03" class="ThumbnailContainer"><div id="ctl00_ContentPlaceHolder1_ctl04" class="ThumbnailOverlay"><img id="ctl00_ContentPlaceHolder1_ctl06" src="/images/icons/film.png" alt="Video Media" style="border-width:0px;" /></div><img id="ctl00_ContentPlaceHolder1_ctl05" class="ThumbnailImage" src="http://joe.viewtv.co.uk/thumbs/718/5575/0" alt="A thrilling and dangerous adventure that follows a beautiful girl and her sidekick, The Pecos Kid, as they try to capture a fortune in ancient Mayan gold. Stars Laurene Landon (Airplane II)." style="border-width:0px;" /></div></td>
        //	<td id="ctl00_ContentPlaceHolder1_ctl07" class="Col2Of2B LeftCol TopCol Text2" colspan="1" rowspan="1">Yellow Hair and the Fortress of Gold <img src="/images/Ratings/3.1.gif" alt="3.1 star rating"/></td>
        //</tr>
        //<tr id="ctl00_ContentPlaceHolder1_ctl08">
        //	<td id="ctl00_ContentPlaceHolder1_ctl09" class="Col2Of2 LeftCol TopCol" colspan="1" rowspan="1">A thrilling and dangerous adventure that follows a beautiful girl and her sidekick, The Pecos Kid, as they try to capture a fortune in ancient Mayan gold. Stars Laurene Landon (Airplane II).</td>
        //	</tr>
        private string videoListRegex = @"class=""SearchResult\sLink""\sonclick=""document.location.href='(?<url>[^']+)'""><[^>]+>[^>]+>[^>]+>[^>]+>[^>]+>[^>]+>[^>]+>[^""]+""[^""]+""[^""]+""[^""]+""\ssrc=""(?<thumb>[^""]+)""[^<]+<[^<]+<[^<]+<[^>]+>(?<title>[^<]+)<[^>]+>[^>]+>[^>]+>[^>]+>[^>]+>(?<descr>[^<]+)<";
        private string videoListMainPageRegex = @"class=""""\sonclick=""document.location.href='(?<url>[^']+)'""><[^>]+>[^>]+>[^>]+>[^>]+>[^>]+>[^>]+>[^>]+>[^""]+""[^""]+""[^""]+""[^""]+""\ssrc=""(?<thumb>[^""]+)""[^<]+<[^<]+<[^<]+<[^>]+>[^>]+>(?<title>[^<]+)<[^>]+>[^>]+>[^>]+>[^>]+>[^>]+>(?<descr>[^<]+)<";

        private Regex regEx_Category;
        private Regex regEx_VideoList;
        private Regex regEx_MainPageVideoList;
        private string popUrl = "@POP";
        private string newUrl = "@NEW";

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_Category = new Regex(categoryRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_VideoList = new Regex(videoListRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_MainPageVideoList = new Regex(videoListMainPageRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            string data = GetWebData(baseUrl + "/movies4men");
            if (!string.IsNullOrEmpty(data))
            {
                RssLink cat2 = new RssLink();
                cat2.Name = "New Media";
                cat2.Url = newUrl;
                Settings.Categories.Add(cat2);

                cat2 = new RssLink();
                cat2.Name = "Most Popular";
                cat2.Url = popUrl;
                Settings.Categories.Add(cat2);

                Match m = regEx_Category.Match(data);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    string url = baseUrl + m.Groups["url"].Value;
                    cat.Url = m.Groups["groupid"].Value + '!' + url;
                    Settings.Categories.Add(cat);
                    m = m.NextMatch();
                }

                Settings.DynamicCategoriesDiscovered = true;
                return Settings.Categories.Count;
            }
            return 0;
        }

        public override string getUrl(VideoInfo video)
        {
            string webData = GetWebData(video.VideoUrl);
            int p = webData.IndexOf("initParams");
            p = webData.IndexOf("m=", p);
            int q = webData.IndexOf('"', p);
            string url = webData.Substring(p + 2, q - p - 2);
            return ParseASX(url)[1];
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            pageNr = 1;
            bareCategory = (RssLink)category;
            currentSearch = null;
            return getPagedVideoList(bareCategory);
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            pageNr++;
            return getPagedVideoList(bareCategory);
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            pageNr--;
            return getPagedVideoList(bareCategory);
        }

        public override bool HasPreviousPage
        {
            get { return pageNr > 1; }
        }

        public override bool HasNextPage
        {
            get { return hasNextPage; }
        }

        public override bool CanSearch
        {
            get { return true; }
        }

        public override List<VideoInfo> Search(string query)
        {
            pageNr = 1;
            currentSearch = query;
            return getPagedVideoList(bareCategory);
        }

        private List<VideoInfo> getPagedVideoList(RssLink category)
        {
            string[] strings = category.Url.Split('!');
            string url = strings[strings.Length - 1];


            Regex regEx = regEx_VideoList;
            string webData;
            if (url == popUrl || url == newUrl)
            {
                webData = GetWebData(baseUrl + "/movies4men");
                //new
                int i = webData.IndexOf(@"<td id=""ctl00_ContentPlaceHolder1_");
                if (i >= 0) webData = webData.Substring(i);

                i = webData.IndexOf(@"id=""ctl00_ContentPlaceHolder1_RadPageViewMostPopular""");
                if (i >= 0)
                {
                    if (url == newUrl)
                        webData = webData.Substring(0, i);
                    else
                        webData = webData.Substring(i);
                }
                regEx = regEx_MainPageVideoList;
            }
            else
            {
                string urlTodo;
                if (String.IsNullOrEmpty(currentSearch))
                    urlTodo = url;
                else
                    urlTodo = baseUrl + "/movies4men/search/" + currentSearch;

                if (pageNr > 1)
                {
                    string catnr;
                    if (strings.Length > 1)
                        catnr = strings[0];
                    else
                        catnr = String.Empty;
                    string postData = @"__VIEWSTATE=%2FwEPDwULLTE3MTc1MjQ1MjVkGAEFHl9fQ29udHJvbHNSZXF1aXJlUG9zdEJhY2tLZXlfXxYBBRpjdGwwMCRMb2dpbjEkY2hrUmVtZW1iZXJNZWP9N6bNXvOFNqZhcS6zHcVsPGuC" +
                                   "&__EVENTVALIDATION=%2FwEWFAKxoeXXCQLApPGVAgL3tfOMCwLH9rKFDwKZ6cP1BgK0%2FeC5DwKrt5vPBALHsYuuAQLepdDDAgLJuaHFAQL344WcAgL6uf6QCgL8ypvBAwLKna%2FYAQKm7LaXBgLqxu7xCAKM6%2BHxDQL%2Bxv%2BnDwKnpt8nAve684YCPECezm0uHuwFS6RR138PoH6D3Q0%3D";
                    if (String.IsNullOrEmpty(currentSearch))
                        postData = postData + "&ctl00%24ContentPlaceHolder1%24hdnCategory=" + catnr;
                    else
                        postData = postData + "&ctl00%24ContentPlaceHolder1%24txtSearch=" + currentSearch;
                    postData = postData + "&ctl00%24ContentPlaceHolder1%24hdnCurrentPage=" + pageNr.ToString();

                    webData = GetWebDataFromPost(urlTodo, postData);
                }
                else
                    webData = GetWebData(urlTodo);
            }

            hasNextPage = webData.IndexOf(@">>>") >= 0;

            List<VideoInfo> videos = new List<VideoInfo>();
            if (!string.IsNullOrEmpty(webData))
            {
                Match m = regEx.Match(webData);
                while (m.Success)
                {
                    VideoInfo video = new VideoInfo();

                    video.Title = HttpUtility.HtmlDecode(HttpUtility.HtmlDecode(m.Groups["title"].Value));
                    video.VideoUrl = baseUrl + HttpUtility.HtmlDecode(m.Groups["url"].Value);
                    video.ImageUrl = HttpUtility.HtmlDecode(m.Groups["thumb"].Value);
                    video.Description = HttpUtility.HtmlDecode(m.Groups["descr"].Value);

                    videos.Add(video);
                    m = m.NextMatch();
                }
            }
            return videos;
        }
    }

}

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class ArtePlus7Util : SiteUtilBase
    {
        protected string baseUrl = "http://videos.arte.tv";
        protected string videoListRegEx = @"<a\s+href=""(?<VideoUrl>[^""]+)""><img(?:(?!src).)*src=""(?<ImageUrl>[^""]+)""\s*/></a>\s*
(<div[^>]*>\s*<p\s+class=""teaserText"">(?<Description>[^<]+)</p>\s*</div>)?
(?:(?!<h2>).)*<h2><a[^>]*>(?<Title>[^<]*)?</a></h2>\s*
(?:(?!<p>).)*<p>(?<Duration>[^<]+)</p>";

        protected Regex regEx_VideoList;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            RegexOptions defaultRegexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture;

            if (!string.IsNullOrEmpty(videoListRegEx)) regEx_VideoList = new Regex(videoListRegEx, defaultRegexOptions);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            Settings.Categories.Add(
                new RssLink()
                {
                    Name = "DE - Sendungen",
                    Url = "http://videos.arte.tv/de/videos/sendungen",
                    HasSubCategories = true
                });
            Settings.Categories.Add(
                new RssLink()
                {
                    Name = "FR - Programmes",
                    Url = "http://videos.arte.tv/fr/videos/programmes",
                    HasSubCategories = true
                });
            Settings.Categories.Add(
                new RssLink()
                {
                    Name = "EN - Programs",
                    Url = "http://videos.arte.tv/en/videos/programs",
                    HasSubCategories = true
                });
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string data = GetWebData((parentCategory as RssLink).Url);
            parentCategory.SubCategories = new List<Category>();            
            if (!string.IsNullOrEmpty(data))
            {
                Match m = Regex.Match(data, @"<a\shref=""(?<url>[^""]+)"">(?<title>[^<]+)</a>\s*\((?<amount>\d+)\)");
                while (m.Success)
                {
                    RssLink cat = new RssLink() { ParentCategory = parentCategory };
                    string url = "http://videos.arte.tv" + m.Groups["url"].Value.Replace("/videos", "/do_delegate/videos");
                    if (!url.EndsWith(".html")) url += "/index.html";
                    cat.Url = url.Replace(".html", "-3188698,view,asThumbnail.html?hash=tv/thumb///{0}/25/");
                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    cat.EstimatedVideoCount = uint.Parse(m.Groups["amount"].Value);
                    parentCategory.SubCategories.Add(cat);
                    
                    m = m.NextMatch();
                }
                parentCategory.SubCategoriesDiscovered = true;
                return Settings.Categories.Count;
            }
            return 0;
        }

        public override String getUrl(VideoInfo video)
        {
            string data = GetWebData(video.VideoUrl);
            data = HttpUtility.UrlDecode(data);

            if (video.PlaybackOptions == null)
            {
                video.PlaybackOptions = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(data))
                {
                    string xmlUrl = Regex.Match(data, @"videorefFileUrl=(?<url>[^""]+)""/>").Groups["url"].Value;
                    if (!string.IsNullOrEmpty(xmlUrl))
                    {
                        List<string> langValues = new List<string>();
                        List<string> urlValues = new List<string>();

                        data = GetWebData(xmlUrl);

                        Match m = Regex.Match(data, @"<video\slang=""(?<lang>[^""]+)""\sref=""(?<url>[^""]+)""/>");
                        while (m.Success)
                        {
                            langValues.Add(m.Groups["lang"].Value);
                            urlValues.Add(m.Groups["url"].Value);
                            m = m.NextMatch();
                        }
                        for (int i = 0; i < langValues.Count; i++)
                        {
                            if (!string.IsNullOrEmpty(urlValues[i]))
                            {
                                string xmlFile = GetWebData(urlValues[i]);
                                if (!string.IsNullOrEmpty(xmlFile))
                                {
                                    Match n = Regex.Match(xmlFile, @"<url\squality=""(?<quality>[^""]+)"">(?<url>[^<]+)</url>");
                                    while (n.Success)
                                    {
                                        string title = langValues[i] + " - " + n.Groups["quality"].Value;

                                        string url = n.Groups["url"].Value;
                                        string host = url.Substring(url.IndexOf(":") + 3, url.IndexOf("/", url.IndexOf(":") + 3) - (url.IndexOf(":") + 3));
                                        string app = url.Substring(host.Length + url.IndexOf(host) + 1, (url.IndexOf("/", url.IndexOf("/", (host.Length + url.IndexOf(host) + 1)) + 1)) - (host.Length + url.IndexOf(host) + 1));
                                        string tcUrl = "rtmp://" + host + ":1935" + "/" + app;
                                        string playPath = url.Substring(url.IndexOf(app) + app.Length + 1);

                                        string resultUrl = ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                                            string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&hostname={1}&tcUrl={2}&app={3}&swfurl={4}&swfsize={5}&swfhash={6}&pageurl={7}&playpath={8}",
                                                url, //rtmpUrl
                                                host, //host
                                                tcUrl, //tcUrl
                                                app, //app
                                                "http://artestras.vo.llnwd.net/o35/geo/arte7/player/ALL/artep7_hd_16_9_v2.swf", //swfurl
                                                "105878",
                                                "061e498c18ca7ce1244caaa0311f35cddc6cf69b4ff810ab88caf7b546a6795e",
                                                video.VideoUrl, //pageUrl
                                                playPath //playpath
                                                ));

                                        if(video.PlaybackOptions.ContainsKey(title)) title += " - 2";
                                        video.PlaybackOptions.Add(title, resultUrl);
                                        n = n.NextMatch();
                                    }
                                }
                            }
                        }

                    }
                }
            }
            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
            {
                var enumer = video.PlaybackOptions.GetEnumerator();
                enumer.MoveNext();
                return enumer.Current.Value;
            }
            return "";
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            currentCategory = category as RssLink;
            currentPage = 1;
            currentCategoryMaxPages = 1;
            return GetPagedVideoList();
        }

        List<VideoInfo> GetPagedVideoList()
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            string data = GetWebData(string.Format((currentCategory as RssLink).Url, currentPage));

            foreach (Match pageMatch in Regex.Matches(data, @"<li><a\s+href=""\#""\s+class=""(current\s)?{page:'\d+'}"">(?<page>\d+)</a></li>"))
            {
                int counter = int.Parse(pageMatch.Groups["page"].Value);
                if (counter > currentCategoryMaxPages) currentCategoryMaxPages = counter;
            }

            Match m = regEx_VideoList.Match(data);
            while (m.Success)
            {
                VideoInfo videoInfo = new VideoInfo();
                videoInfo.Title = HttpUtility.HtmlDecode(m.Groups["Title"].Value);
                videoInfo.VideoUrl = m.Groups["VideoUrl"].Value;
                if (!Uri.IsWellFormedUriString(videoInfo.VideoUrl, System.UriKind.Absolute)) videoInfo.VideoUrl = new Uri(new Uri(baseUrl), videoInfo.VideoUrl).AbsoluteUri;
                videoInfo.ImageUrl = m.Groups["ImageUrl"].Value;                
                if (!string.IsNullOrEmpty(videoInfo.ImageUrl) && !Uri.IsWellFormedUriString(videoInfo.ImageUrl, System.UriKind.Absolute)) videoInfo.ImageUrl = new Uri(new Uri(baseUrl), videoInfo.ImageUrl).AbsoluteUri;
                videoInfo.Length = Translation.Airdate + ": " + m.Groups["Duration"].Value;
                videoInfo.Description = HttpUtility.HtmlDecode(m.Groups["Description"].Value);
                videos.Add(videoInfo);
                m = m.NextMatch();
            }
            return videos;
        }

        #region Next/Previous Page

        RssLink currentCategory = null;
        int currentPage = 0;
        int currentCategoryMaxPages = 0;

        public override bool HasNextPage
        {
            get { return currentCategory != null && currentCategoryMaxPages > currentPage; }
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            currentPage++;
            if (currentPage >= currentCategoryMaxPages) currentPage = currentCategoryMaxPages;
            return GetPagedVideoList();
        }

        public override bool HasPreviousPage
        {
            get { return currentCategory != null && currentPage > 1; }
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            currentPage--;
            if (currentPage < 1) currentPage = 1;
            return GetPagedVideoList();
        }

        #endregion        
    }
}
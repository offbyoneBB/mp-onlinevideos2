using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public class TEDTalksUtil : SiteUtilBase
    {
        string tagsUrl = "http://www.ted.com/talks/tags";
        string themesUrl = "http://www.ted.com/themes";
        string newestReleasesUrl = "http://www.ted.com/talks?lang=en&event=&duration=&sort=newest&tag=&page=1";
        string mostViewedUrl = "http://www.ted.com/talks?lang=en&event=&duration=&sort=mostviewed&tag=&page=1";
        string mostPopularUrl = "http://www.ted.com/talks?lang=en&event=&duration=&sort=mostpopular&tag=&page=1";
        string tagsRegex = @"<li><a\s+href=""(?<url>[^""]+)"">(?<title>[^\(\n]+)\((?<amount>\d+)\)</a></li>";
        string themesRegex = @"<li\s+class=""clearfix"">\s*<div\s+class=""col"">\s*<a\s+title=""[^""]*""\s+href=""(?<url>[^""]+)"">\s*
<img\s+alt=""[^""]*""\s+src=""(?<thumb>[^""]+)""\s*/>\s*</a>\s*</div>\s*
<h4>\s*<a[^>]*>(?<title>[^<]+)</a>\s*</h4>\s*<p[^>]*>\s*<span[^>]*>(?<amount>[^<]+)</span>[^<]*</p>\s*
<p>(?<desc>[^<]*)</p>\s*</li>";
        string themeRPCUrlRegex = @"YAHOO.util.DataSource\(""(?<url>[^""]+)""\);";
        string lastPageIndexRegex = @"Showing page \d+ of (?<lastpageindex>\d+)";
        string nextPageUrlRegex = @"<li><a class=""next"" href=""(?<url>[^""]+)"">Next <span class=""bull"">&raquo;</span></a></li>";
        string videosRegex = @"<img alt=""[^""]*"" src=""(?<thumb>[^""]+)"".*?<h\d[^>]*>\s*<a.*?href=""(?<url>[^""]+)"">(?<title>[^<]*)</a>\s*</h\d>.*?<em[^>]*>\s*(<span[^>]*>)?(?<length>\d+\:\d+)(</span>)?\s+Posted\:\s*(?<aired>.*?)</em>";
        string downloadPageUrlRegex = @"(\$\('#download_dialog'\).load\('(?<url>[^']+)')|(<embed src=""(?<youtubeurl>http://www.youtube.com/[^&]+)&)";
        string downloadOptionsRegex = @"<input name=""download_quality"" id=""[^""]+"" type=""radio"" data-name=""[^""]+"" value=""(?<value>[^""]*)"".*?/> <label for=""[^""]*"">(?<label>[^<]*)</label>";
        string downloadFileUrlRegex = @"var url = '(?<url>[^{]+{quality}{lang}.mp4)'";

        string nextPageUrl;

        public override int DiscoverDynamicCategories()
        {
            if (Settings.Categories == null) Settings.Categories = new BindingList<Category>();
            Settings.Categories.Clear();
            Settings.Categories.Add(new RssLink() { Name = "Newest Releases", Url = newestReleasesUrl, Other = "X-Requested-With:XMLHttpRequest" });
            Settings.Categories.Add(new RssLink() { Name = "Most Viewed", Url = mostViewedUrl, Other = "X-Requested-With:XMLHttpRequest" });
            Settings.Categories.Add(new RssLink() { Name = "Most Popular", Url = mostPopularUrl, Other = "X-Requested-With:XMLHttpRequest" });
            Settings.Categories.Add(new RssLink() { Name = "Tags", Url = tagsUrl, Other = tagsRegex, HasSubCategories = true });
            Settings.Categories.Add(new RssLink() { Name = "Themes", Url = themesUrl, Other = themesRegex, HasSubCategories = true });
            Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > 0;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string pageData = GetWebData((parentCategory as RssLink).Url);
            if (parentCategory.SubCategories == null) parentCategory.SubCategories = new List<Category>();
            parentCategory.SubCategories.Clear();
            var match = Regex.Match(pageData, (string)parentCategory.Other, RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
            while (match.Success)
            {
                parentCategory.SubCategories.Add(new RssLink()
                {
                    Name = HttpUtility.HtmlDecode(match.Groups["title"].Value).Trim(),
                    Url = new Uri(new Uri((parentCategory as RssLink).Url), match.Groups["url"].Value).AbsoluteUri,
                    EstimatedVideoCount = uint.Parse(match.Groups["amount"].Value),
                    Description = HttpUtility.HtmlDecode(match.Groups["desc"].Value).Trim(),
                    Thumb = match.Groups["thumb"].Value,
                    ParentCategory = parentCategory,
                    Other = parentCategory.Name == "Themes" ? themeRPCUrlRegex : ""
                });                
                match = match.NextMatch();
            }
            parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            HasNextPage = false;
            nextPageUrl = null;

            string firstPageData = "";
            if (category.ParentCategory != null)
                firstPageData = GetWebData((category as RssLink).Url);
            else
            {
                string[] headers = ((string)category.Other).Split(':');
                firstPageData = GetWebData((category as RssLink).Url, additionalHeaders: new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>(headers[0], headers[1]) });
            }

            if (category.ParentCategory != null)
            {
                if (!string.IsNullOrEmpty((string)category.Other))
                {
                    List<VideoInfo> result = new List<VideoInfo>();
                    var extraUrlMatch = Regex.Match(firstPageData, (string)category.Other);
                    if (extraUrlMatch.Success)
                    {
                        string extraRPCUrl = new Uri(new Uri((category as RssLink).Url), extraUrlMatch.Groups[1].Value).AbsoluteUri;
                        var jsonResult = GetWebData<Newtonsoft.Json.Linq.JObject>(extraRPCUrl);
                        foreach (var markup in jsonResult["resultSet"]["result"])
                        {
                            string html = markup.Value<string>("markup");
                            result.AddRange(ParseVideos(html));
                        }
                    }
                    return result;
                }
                else
                {
                    var lastPageMatch = Regex.Match(firstPageData, lastPageIndexRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
                    if (lastPageMatch.Success)
                    {
                        uint lastPageIndex = 0;
                        if (uint.TryParse(lastPageMatch.Groups["lastpageindex"].Value, out lastPageIndex))
                        {
                            var nextPageUrlMatch = Regex.Match(firstPageData, nextPageUrlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
                            if (nextPageUrlMatch.Success)
                            {
                                string baseUrl = new Uri(new Uri((category as RssLink).Url), nextPageUrlMatch.Groups["url"].Value).AbsoluteUri;
                                baseUrl = baseUrl.Substring(0, baseUrl.LastIndexOf('/'));
                                firstPageData = GetWebDataFromPost(string.Format("{0}/{1}", baseUrl, lastPageIndex), "sort=date");
                                HasNextPage = true;
                                nextPageUrl = string.Format("{0}/{1}", baseUrl, lastPageIndex - 1);
                            }
                        }
                    }
                    return ParseVideos(firstPageData);
                }
            }
            else
            {
                HasNextPage = true;
                nextPageUrl = (category as RssLink).Url.Substring(0, (category as RssLink).Url.LastIndexOf('=') + 1) + "2";
                return ParseVideos(firstPageData, false);
            }
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            if (nextPageUrl.Contains("page="))
            {
                var videos = ParseVideos(GetWebData(nextPageUrl, additionalHeaders: new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("X-Requested-With", "XMLHttpRequest") }), false);
                uint pageIndex = uint.Parse(nextPageUrl.Substring(nextPageUrl.LastIndexOf('=') + 1)) + 1;
                nextPageUrl = nextPageUrl.Substring(0, nextPageUrl.LastIndexOf('=') + 1) + pageIndex;
                return videos;
            }
            else
            {
                var videos = ParseVideos(GetWebDataFromPost(nextPageUrl, "sort=date"));
                uint pageIndex = uint.Parse(nextPageUrl.Substring(nextPageUrl.LastIndexOf('/') + 1));
                if (pageIndex <= 1)
                {
                    HasNextPage = false;
                    nextPageUrl = null;
                }
                else
                {
                    nextPageUrl = nextPageUrl.Substring(0, nextPageUrl.LastIndexOf('/') + 1) + (pageIndex - 1).ToString();
                }
                return videos;
            }
        }

        List<VideoInfo> ParseVideos(string data, bool reverse = true)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            var match = Regex.Match(data, videosRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Multiline | RegexOptions.Singleline);
            while (match.Success)
            {
                result.Add(new VideoInfo()
                {
                    Title = HttpUtility.HtmlDecode(match.Groups["title"].Value).Trim(),
                    VideoUrl = new Uri(new Uri(tagsUrl), match.Groups["url"].Value).AbsoluteUri,
                    ImageUrl = match.Groups["thumb"].Value,
                    Length = match.Groups["length"].Value.Trim(),
                    Airdate = Utils.PlainTextFromHtml(match.Groups["aired"].Value).Trim()
                });
                match = match.NextMatch();
            }
            if (reverse) result.Reverse();
            return result;
        }

        public override string getUrl(VideoInfo video)
        {
            video.PlaybackOptions = new Dictionary<string, string>();
            string data = GetWebData(video.VideoUrl);
            var downloadUrlMatch = Regex.Match(data, downloadPageUrlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
            if (downloadUrlMatch.Success)
            {
                if (downloadUrlMatch.Groups["youtubeurl"].Success)
                {
                    var youtubeHoster = Hoster.Base.HosterFactory.GetHoster("Youtube");
                    video.PlaybackOptions = youtubeHoster.getPlaybackOptions(downloadUrlMatch.Groups["youtubeurl"].Value);
                }
                else
                {
                    data = GetWebData(new Uri(new Uri(video.VideoUrl), downloadUrlMatch.Groups["url"].Value).AbsoluteUri);
                    var optionMatch = Regex.Match(data, downloadOptionsRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
                    var dlUrlMatch = Regex.Match(data, downloadFileUrlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
                    if (optionMatch.Success && dlUrlMatch.Success)
                    {
                        while (optionMatch.Success)
                        {
                            string dlUrl = dlUrlMatch.Groups["url"].Value;
                            dlUrl = dlUrl.Replace("{quality}", optionMatch.Groups["value"].Value);
                            dlUrl = dlUrl.Replace("{lang}", "");
                            video.PlaybackOptions.Add(optionMatch.Groups["label"].Value, dlUrl);
                            optionMatch = optionMatch.NextMatch();
                        }
                    }
                }
            }
            return video.PlaybackOptions.Count > 0 ? video.PlaybackOptions.Last().Value : "";
        }

        public static string GetWebData(string url, CookieContainer cc = null, string referer = null, IWebProxy proxy = null, bool forceUTF8 = false, bool allowUnsafeHeader = false, string userAgent = null, Encoding encoding = null, List<KeyValuePair<string,string>> additionalHeaders = null)
        {
            HttpWebResponse response = null;
            try
            {
                string requestCRC = Utils.EncryptLine(string.Format("{0}{1}{2}{3}{4}", url, referer, userAgent, proxy != null ? proxy.GetProxy(new Uri(url)).AbsoluteUri : "", cc != null ? cc.GetCookieHeader(new Uri(url)) : ""));

                // try cache first
                string cachedData = WebCache.Instance[requestCRC];
                Log.Debug("GetWebData{1}: '{0}'", url, cachedData != null ? " (cached)" : "");
                if (cachedData != null) return cachedData;

                // request the data
                if (allowUnsafeHeader) Utils.SetAllowUnsafeHeaderParsing(true);
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                if (request == null) return "";
                if (!String.IsNullOrEmpty(userAgent))
                    request.UserAgent = userAgent; // set specific UserAgent if given
                else
                    request.UserAgent = OnlineVideoSettings.Instance.UserAgent; // set OnlineVideos default UserAgent
                request.Accept = "*/*"; // we accept any content type
                request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate"); // we accept compressed content
                if (!String.IsNullOrEmpty(referer)) request.Referer = referer; // set referer if given
                if (cc != null) request.CookieContainer = cc; // set cookies if given
                if (proxy != null) request.Proxy = proxy; // send the request over a proxy if given
                if (additionalHeaders != null) // add user defined headers
                {
                    foreach (var additionalheader in additionalHeaders)
                    {
                        request.Headers.Set(additionalheader.Key, additionalheader.Value);
                    }
                }
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch (WebException webEx)
                {
                    Log.Debug(webEx.Message);
                    response = (HttpWebResponse)webEx.Response; // if the server returns a 404 or similar .net will throw a WebException that has the response
                }
                Stream responseStream;
                if (response == null) return "";
                if (response.ContentEncoding.ToLower().Contains("gzip"))
                    responseStream = new System.IO.Compression.GZipStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                else if (response.ContentEncoding.ToLower().Contains("deflate"))
                    responseStream = new System.IO.Compression.DeflateStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                else
                    responseStream = response.GetResponseStream();

                // UTF8 is the default encoding as fallback
                Encoding responseEncoding = Encoding.UTF8;
                // try to get the response encoding if one was specified and neither forceUTF8 nor encoding were set as parameters
                if (!forceUTF8 && encoding == null && response.CharacterSet != null && !String.IsNullOrEmpty(response.CharacterSet.Trim())) responseEncoding = Encoding.GetEncoding(response.CharacterSet.Trim(new char[] { ' ', '"' }));
                // the caller did specify a forced encoding
                if (encoding != null) responseEncoding = encoding;
                // the caller wants to force UTF8
                if (forceUTF8) responseEncoding = Encoding.UTF8;

                using (StreamReader reader = new StreamReader(responseStream, responseEncoding, true))
                {
                    string str = reader.ReadToEnd().Trim();
                    // add to cache if HTTP Status was 200 and we got more than 500 bytes (might just be an errorpage otherwise)
                    if (response.StatusCode == HttpStatusCode.OK && str.Length > 500) WebCache.Instance[requestCRC] = str;
                    return str;
                }
            }
            finally
            {
                if (response != null) ((IDisposable)response).Dispose();
                // disable unsafe header parsing if it was enabled
                if (allowUnsafeHeader) Utils.SetAllowUnsafeHeaderParsing(false);
            }
        }
    }
}

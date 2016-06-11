using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using HtmlAgilityPack;
using HtmlNode = HtmlAgilityPack.HtmlNode;
using OnlineVideos.Helpers;

namespace OnlineVideos.Sites
{
    public class RedBull : BrightCoveUtil
    {

        public override int DiscoverDynamicCategories()
        {
            int res = base.DiscoverDynamicCategories();
            foreach (Category cat in Settings.Categories)
            {
                cat.Thumb = FixImageUrl(cat.Thumb);
            }
            return res;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (parentCategory.Other is HtmlNode)//sports/../shows
                return GetShowsSubcats((RssLink)parentCategory);
            if (parentCategory.Name != "Sports")
                return getSubcats((RssLink)parentCategory);

            int res = base.DiscoverSubCategories(parentCategory);
            foreach (Category cat in parentCategory.SubCategories)
            {
                cat.HasSubCategories = true;
                cat.Thumb = FixImageUrl(cat.Thumb);
            }
            parentCategory.SubCategories.RemoveAll(c => c.Name == "All Sports");
            return res;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            if (category.Other is HtmlNode)
            {
                RssLink parentCat = (RssLink)category;
                HtmlNode subNode = (HtmlNode)category.Other;
                return GetVids(subNode, parentCat.Url);
            }
            else
            {
                string url = ((RssLink)category).Url;
                string data = WebCache.Instance.GetWebData(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(data);
                return GetVids(doc.DocumentNode, url);
            }
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            Uri uri = DotNetFrameworkHelper.UriWithoutUrlDecoding.Create(nextPageUrl);
            string data = MyGetWebData(uri);
            var doc = new HtmlDocument();
            doc.LoadHtml(data);
            return GetVids(doc.DocumentNode, nextPageUrl);
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            Uri uri = DotNetFrameworkHelper.UriWithoutUrlDecoding.Create(category.Url);
            string data = MyGetWebData(uri);

            category.ParentCategory.SubCategories.Remove(category);
            var doc = new HtmlDocument();
            doc.LoadHtml(data);

            return AddSubcats(doc.DocumentNode, (RssLink)category.ParentCategory);
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string res = base.GetVideoUrl(video);
            if (res.StartsWith(@"http://live", StringComparison.InvariantCultureIgnoreCase))
            {
                if (video.PlaybackOptions != null)
                {
                    Dictionary<String, String> newpb = new Dictionary<string, string>();
                    foreach (var pb in video.PlaybackOptions)
                        newpb.Add(pb.Key, Fix(pb.Value));
                    video.PlaybackOptions = newpb;
                }
                res = Fix(res);
            }
            return res;
        }

        private string Fix(string url)
        {
            var a = lastResponse.GetArray("programmedContent").GetObject("videoPlayer").GetObject("mediaDTO");
            return url + "?videoId=" + Convert.ToInt64(a.GetDoubleProperty("id")).ToString() + "&playerId=" +
                Convert.ToInt64(lastResponse.GetDoubleProperty("id")).ToString() + "&v=3.4.0&fp=WIN%2020,0,0,306&r=OKDIV&g=LCNJPLGEAQUO";
        }

        private List<VideoInfo> GetVids(HtmlNode node, string parentUrl)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            var vids = node.SelectNodes(".//article");
            foreach (var vid in vids)
            {
                VideoInfo video = new VideoInfo();
                if (vid.SelectSingleNode(".//h2[contains(@class,'h3')]") == null)
                {
                    video.Title = HttpUtility.HtmlDecode(vid.SelectSingleNode(".//a[@title]").Attributes["title"].Value.Trim());
                    video.VideoUrl = FormatDecodeAbsolutifyUrl(parentUrl, vid.SelectSingleNode(".//a[@href]").Attributes["href"].Value, null, UrlDecoding.None);
                }
                else
                {
                    video.Title = vid.SelectSingleNode(".//h2[contains(@class,'h3')]").InnerText.Trim();
                    video.VideoUrl = FormatDecodeAbsolutifyUrl(parentUrl, vid.SelectSingleNode(".//a[@class='teaser__link' and @href]").Attributes["href"].Value, null, UrlDecoding.None);
                    if (vid.SelectSingleNode(".//p[contains(@class,'teaser__description')]") != null)
                        video.Description = vid.SelectSingleNode(".//p[contains(@class,'teaser__description')]").InnerText.Trim();
                    else
                        video.Description = vid.SelectSingleNode(".//h3[contains(@class,'teaser__subtitle')]").InnerText.Trim();
                }
                var moNode = vid.SelectSingleNode(".//span[@data-month]");
                var daNode = vid.SelectSingleNode(".//span[@data-date]");
                if (moNode != null && daNode != null)
                {
                    video.Airdate = moNode.InnerText.Trim() + ' ' + daNode.InnerText.Trim();
                }

                video.Thumb = getThumb(vid.SelectSingleNode(".//picture/img"));
                videos.Add(video);
            }

            var np = node.SelectSingleNode(".//a[@href and contains(text(),'More ')]");
            nextPageAvailable = false;
            if (np != null)
            {
                nextPageAvailable = true;
                nextPageUrl = CreateUrl(parentUrl, np.Attributes["href"].Value);
            }
            return videos;
        }

        private int AddSubcats(HtmlNode node, RssLink parentCat)
        {
            var subs = node.SelectNodes(".//article");
            foreach (var sub in subs)
            {
                RssLink subcat = new RssLink() { ParentCategory = parentCat };
                subcat.Name = HttpUtility.HtmlDecode(sub.SelectSingleNode(".//a[@title]").Attributes["title"].Value.Trim());
                subcat.Url = FormatDecodeAbsolutifyUrl(parentCat.Url, sub.SelectSingleNode(".//a[@href]").Attributes["href"].Value, null, UrlDecoding.None);
                subcat.Thumb = getThumb(sub.SelectSingleNode(".//picture/img"));

                parentCat.SubCategories.Add(subcat);
            }

            var np = node.SelectSingleNode(".//a[@href and text()='More shows']");
            nextPageAvailable = false;
            if (np != null)
            {
                string url = CreateUrl(parentCat.Url, np.Attributes["href"].Value);
                var npCat = new NextPageCategory() { Url = url, ParentCategory = parentCat };
                parentCat.SubCategories.Add(npCat);
            }

            parentCat.SubCategoriesDiscovered = true;
            return parentCat.SubCategories.Count;
        }

        private string getThumb(HtmlNode imgNode)
        {
            if (imgNode == null)
                return String.Empty;
            if (imgNode.Attributes["data-src"] != null)
                return FixImageUrl(imgNode.Attributes["data-src"].Value);
            else
                if (imgNode.Attributes["src"] != null)
                    return FixImageUrl(imgNode.Attributes["src"].Value);
            return String.Empty;
        }

        private string CreateUrl(string baseUrl, string relativeUrl)
        {
            if (!Uri.IsWellFormedUriString(relativeUrl, UriKind.Absolute))
            {
                Uri uri = DotNetFrameworkHelper.UriWithoutUrlDecoding.Create(baseUrl, relativeUrl);
                return uri.OriginalString;
            }
            return relativeUrl;
        }

        private int GetShowsSubcats(RssLink parentCat)
        {
            HtmlNode node = parentCat.Other as HtmlNode;
            parentCat.SubCategories = new List<Category>();
            return AddSubcats(node, parentCat);
        }

        private int getSubcats(RssLink parentCat)
        {
            string webData = GetWebData(parentCat.Url);
            var doc = new HtmlDocument();
            doc.LoadHtml(webData);
            var subNodes = doc.DocumentNode.SelectNodes(@".//div[contains(@class,'tab-content')]");
            parentCat.SubCategories = new List<Category>();
            parentCat.SubCategoriesDiscovered = true;
            foreach (var subNode in subNodes)
            {
                RssLink sub = new RssLink()
                {
                    Name = subNode.SelectSingleNode(".//h2[@class='h2']").InnerText,
                    ParentCategory = parentCat,
                    Url = parentCat.Url
                };
                if (sub.Name == "Shows")
                    sub.HasSubCategories = true;
                parentCat.SubCategories.Add(sub);
                sub.Other = subNode;
            };

            return parentCat.SubCategories.Count;

        }

        private string FixImageUrl(string url)
        {
            if (!String.IsNullOrEmpty(url))
            {
                if (url.StartsWith(@"https://api.redbull.tv/v1/images/http"))
                    url = HttpUtility.UrlDecode(url.Substring(33));
            }
            return url;
        }

        private string MyGetWebData(Uri uri, string postData = null, CookieContainer cookies = null, string referer = null, IWebProxy proxy = null, bool forceUTF8 = false, bool allowUnsafeHeader = false, string userAgent = null, Encoding encoding = null, NameValueCollection headers = null, bool cache = true)
        {
            // do not use the cache when doing a POST
            if (postData != null) cache = false;
            // set a few headers if none were given
            if (headers == null)
            {
                headers = new NameValueCollection();
                headers.Add("Accept", "*/*"); // accept any content type
                headers.Add("User-Agent", userAgent ?? OnlineVideoSettings.Instance.UserAgent); // set the default OnlineVideos UserAgent when none specified
            }
            if (referer != null) headers.Set("Referer", referer);
            HttpWebResponse response = null;
            try
            {
                // build a CRC of the url and all headers + proxy + cookies for caching
                string requestCRC = Helpers.EncryptionUtils.CalculateCRC32(
                    string.Format("{0}{1}{2}{3}",
                    uri.ToString(),
                    headers != null ? string.Join("&", (from item in headers.AllKeys select string.Format("{0}={1}", item, headers[item])).ToArray()) : "",
                    proxy != null ? proxy.GetProxy(uri).AbsoluteUri : "",
                    cookies != null ? cookies.GetCookieHeader(uri) : ""));

                // try cache first
                string cachedData = cache ? WebCache.Instance[requestCRC] : null;
                Log.Debug("GetWebData-{2}{1}: '{0}'", uri.ToString(), cachedData != null ? " (cached)" : "", postData != null ? "POST" : "GET");
                if (cachedData != null) return cachedData;

                // build the request
                if (allowUnsafeHeader) Helpers.DotNetFrameworkHelper.SetAllowUnsafeHeaderParsing(true);
                HttpWebRequest request = WebRequest.Create(uri) as HttpWebRequest;
                if (request == null) return "";
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate; // turn on automatic decompression of both formats (adds header "AcceptEncoding: gzip,deflate" to the request)
                if (cookies != null) request.CookieContainer = cookies; // set cookies if given
                if (proxy != null) request.Proxy = proxy; // send the request over a proxy if given
                if (headers != null) // set user defined headers
                {
                    foreach (var headerName in headers.AllKeys)
                    {
                        switch (headerName.ToLowerInvariant())
                        {
                            case "accept":
                                request.Accept = headers[headerName];
                                break;
                            case "user-agent":
                                request.UserAgent = headers[headerName];
                                break;
                            case "referer":
                                request.Referer = headers[headerName];
                                break;
                            default:
                                request.Headers.Set(headerName, headers[headerName]);
                                break;
                        }
                    }
                }
                if (postData != null)
                {
                    byte[] data = encoding != null ? encoding.GetBytes(postData) : Encoding.UTF8.GetBytes(postData);
                    request.Method = "POST";
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.ContentLength = data.Length;
                    request.ProtocolVersion = HttpVersion.Version10;
                    Stream requestStream = request.GetRequestStream();
                    requestStream.Write(data, 0, data.Length);
                    requestStream.Close();
                }

                // request the data
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch (WebException webEx)
                {
                    Log.Debug(webEx.Message);
                    response = (HttpWebResponse)webEx.Response; // if the server returns a 404 or similar .net will throw a WebException that has the response
                }
                Stream responseStream = response.GetResponseStream();

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
                    if (cache && response.StatusCode == HttpStatusCode.OK && str.Length > 500) WebCache.Instance[requestCRC] = str;
                    return str;
                }
            }
            finally
            {
                if (response != null) ((IDisposable)response).Dispose();
                // disable unsafe header parsing if it was enabled
                if (allowUnsafeHeader) Helpers.DotNetFrameworkHelper.SetAllowUnsafeHeaderParsing(false);
            }
        }

    }

}


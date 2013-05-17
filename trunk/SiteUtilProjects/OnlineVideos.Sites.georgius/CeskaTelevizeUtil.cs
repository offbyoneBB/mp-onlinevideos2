using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Net;
using System.IO;

namespace OnlineVideos.Sites.georgius
{
    public sealed class CeskaTelevizeUtil : SiteUtilBase
    {
        #region Private fields

        private static String baseUrl = "http://www.ceskatelevize.cz";
        private static String dynamicCategoryBaseUrl = "http://www.ceskatelevize.cz/ivysilani/podle-abecedy";

        private static String dynamicCategoryStart = @"<ul id=""programmeGenre"" class=""clearfix"">";
        private static String dynamicCategoryRegex = @"<a class=""pageLoadAjaxAlphabet"" href=""(?<dynamicCategoryUrl>[^""]+)"" rel=""[^""]*""><span>(?<dynamicCategoryTitle>[^<]+)";

        private static String subCategoryFormat = @"{0}{1}dalsi-casti";

        private static String showListStart = @"<div id=""programmeAlphabetContent"">";
        private static String showRegex = @"(<a href=""(?<showUrl>[^""]+)"" title=""[^""]*"">(?<showTitle>[^""]+)</a>)|(<a class=""toolTip"" href=""(?<showUrl>[^""]+)"" title=""[^""]*"">(?<showTitle>[^""]+)</a>)";
        private static String showEnd = @"</li>";
        private static string onlyBonuses = @"<span class=""labelBonus"">pouze bonusy</span>";

        private static String showEpisodesStartRegex = @"(<div class=""contentBox"">)|(<div class=""clearfix"">)";
        private static String showEpisodeBlockStartRegex = @"(<li class=""itemBlock clearfix"">)|(<li class=""itemBlock clearfix active"">)|(<div class=""channel"">)";
        private static String showEpisodeThumbUrlRegex = @"src=""(?<showThumbUrl>[^""]+)""";
        private static String showEpisodeUrlAndTitleRegex = @"(<a class=""itemSetPaging"" rel=""[^""]+"" href=""(?<showUrl>[^""]+)"">(?<showTitle>[^<]+)</a>)|(<a href=""(?<showUrl>[^""]+)"">(?<showTitle>[^<]+)</a>)";
        private static String showEpisodeNextPageRegex = @"<a title=""[^""]+"" rel=""[^""]*"" class=""detailProgrammePaging next"" href=""(?<url>[^""]+)"">";
        private static String showEpisodeDescriptionStart = @"</h3>";
        private static String showEpisodeDescriptionEnd = @"<p class=""itemRating";

        private static String liveNextProgramm = @"<p class=""next"">";

        private static String showEpisodePostStart = @"callSOAP(";
        private static String showEpisodePostEnd = @");";

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        #endregion

        #region Constructors

        public CeskaTelevizeUtil()
            : base()
        {
        }

        #endregion

        #region Methods

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
        }

        public override int DiscoverDynamicCategories()
        {
            int dynamicCategoriesCount = 0;

            this.Settings.Categories.Add(
                new RssLink()
                {
                    Name = "Živě",
                    HasSubCategories = false,
                    Url = "live"
                });
            dynamicCategoriesCount++;

            String baseWebData = SiteUtilBase.GetWebData(CeskaTelevizeUtil.dynamicCategoryBaseUrl, null, null, null, true);

            int index = baseWebData.IndexOf(CeskaTelevizeUtil.dynamicCategoryStart);
            if (index > 0)
            {
                baseWebData = baseWebData.Substring(index);
                Match match = Regex.Match(baseWebData, CeskaTelevizeUtil.dynamicCategoryRegex);
                while (match.Success)
                {
                    String dynamicCategoryUrl = match.Groups["dynamicCategoryUrl"].Value;
                    String dynamicCategoryTitle = match.Groups["dynamicCategoryTitle"].Value;

                    this.Settings.Categories.Add(
                        new RssLink()
                        {
                            Name = dynamicCategoryTitle,
                            HasSubCategories = true,
                            Url = String.Format("{0}{1}", CeskaTelevizeUtil.baseUrl, dynamicCategoryUrl)
                        });

                    dynamicCategoriesCount++;
                    match = match.NextMatch();
                }
            }

            this.Settings.DynamicCategoriesDiscovered = true;
            return dynamicCategoriesCount;
        }

        private List<VideoInfo> GetPageVideos(String pageUrl)
        {
            List<VideoInfo> pageVideos = new List<VideoInfo>();

            if (!String.IsNullOrEmpty(pageUrl))
            {
                this.nextPageUrl = String.Empty;
                if (this.currentCategory.Name == "Živě")
                {
                    TimeSpan span = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0);
                    pageUrl = "http://www.ceskatelevize.cz/ivysilani/ajax/liveBox.php?time=" + ((long)span.TotalMilliseconds).ToString();
                }
                String baseWebData = CeskaTelevizeUtil.GetWebData(pageUrl, null, null, null, true);

                Match showEpisodesStart = Regex.Match(baseWebData, CeskaTelevizeUtil.showEpisodesStartRegex);
                if (showEpisodesStart.Success)
                {
                    baseWebData = baseWebData.Substring(showEpisodesStart.Index + showEpisodesStart.Length);

                    Match nextPageMatch = Regex.Match(baseWebData, CeskaTelevizeUtil.showEpisodeNextPageRegex);
                    while (true)
                    {
                        Match showEpisodeBlockStart = Regex.Match(baseWebData, CeskaTelevizeUtil.showEpisodeBlockStartRegex);
                        nextPageMatch = Regex.Match(baseWebData, CeskaTelevizeUtil.showEpisodeNextPageRegex);

                        if (((nextPageMatch.Success) && (showEpisodeBlockStart.Success) && (nextPageMatch.Index > showEpisodeBlockStart.Index)) ||
                            ((!nextPageMatch.Success) && (showEpisodeBlockStart.Success)))
                        {
                            baseWebData = baseWebData.Substring(showEpisodeBlockStart.Index + showEpisodeBlockStart.Length);

                            String showTitle = String.Empty;
                            String showThumbUrl = String.Empty;
                            String showUrl = String.Empty;
                            String showDescription = String.Empty;

                            if (this.currentCategory.Name == "Živě")
                            {
                                int nextProgrammIndex = baseWebData.IndexOf(CeskaTelevizeUtil.liveNextProgramm);
                                if (nextProgrammIndex >= 0)
                                {
                                    String liveChannelData = baseWebData.Substring(0, nextProgrammIndex);

                                    Match showEpisodeUrlAndTitle = Regex.Match(liveChannelData, CeskaTelevizeUtil.showEpisodeUrlAndTitleRegex);
                                    if (showEpisodeUrlAndTitle.Success)
                                    {
                                        showUrl = Utils.FormatAbsoluteUrl(showEpisodeUrlAndTitle.Groups["showUrl"].Value, CeskaTelevizeUtil.baseUrl);
                                        showTitle = showEpisodeUrlAndTitle.Groups["showTitle"].Value.Trim();
                                    }

                                    Match showEpisodeThumbUrl = Regex.Match(liveChannelData, CeskaTelevizeUtil.showEpisodeThumbUrlRegex);
                                    if (showEpisodeThumbUrl.Success)
                                    {
                                        showThumbUrl = showEpisodeThumbUrl.Groups["showThumbUrl"].Value;
                                    }

                                    baseWebData = baseWebData.Substring(nextProgrammIndex);
                                }
                            }
                            else
                            {
                                Match showEpisodeThumbUrl = Regex.Match(baseWebData, CeskaTelevizeUtil.showEpisodeThumbUrlRegex);
                                if (showEpisodeThumbUrl.Success)
                                {
                                    showThumbUrl = showEpisodeThumbUrl.Groups["showThumbUrl"].Value;
                                    baseWebData = baseWebData.Substring(showEpisodeThumbUrl.Index + showEpisodeThumbUrl.Length);
                                }

                                Match showEpisodeUrlAndTitle = Regex.Match(baseWebData, CeskaTelevizeUtil.showEpisodeUrlAndTitleRegex);
                                if (showEpisodeUrlAndTitle.Success)
                                {
                                    showUrl = Utils.FormatAbsoluteUrl(showEpisodeUrlAndTitle.Groups["showUrl"].Value, CeskaTelevizeUtil.baseUrl);
                                    showTitle = showEpisodeUrlAndTitle.Groups["showTitle"].Value.Trim();
                                    baseWebData = baseWebData.Substring(showEpisodeUrlAndTitle.Index + showEpisodeUrlAndTitle.Length);
                                }

                                int startIndex = baseWebData.IndexOf(CeskaTelevizeUtil.showEpisodeDescriptionStart);
                                if (startIndex >= 0)
                                {
                                    int endIndex = baseWebData.IndexOf(CeskaTelevizeUtil.showEpisodeDescriptionEnd, startIndex);
                                    if (endIndex >= 0)
                                    {
                                        showDescription = baseWebData.Substring(startIndex + CeskaTelevizeUtil.showEpisodeDescriptionStart.Length, endIndex - startIndex - CeskaTelevizeUtil.showEpisodeDescriptionStart.Length).Trim().Replace('\t', ' ').Replace("</p>", "\n").Trim();
                                    }
                                }
                            }

                            if (String.IsNullOrEmpty(showTitle) || String.IsNullOrEmpty(showUrl) || String.IsNullOrEmpty(showThumbUrl))
                            {
                                continue;
                            }

                            VideoInfo videoInfo = new VideoInfo()
                            {
                                ImageUrl = showThumbUrl,
                                Title = showTitle,
                                VideoUrl = showUrl,
                                Description = showDescription
                            };

                            pageVideos.Add(videoInfo);
                        }
                        else
                        {
                            break;
                        }
                    }

                    this.nextPageUrl = (nextPageMatch.Success) ? String.Format("{0}{1}", CeskaTelevizeUtil.baseUrl, nextPageMatch.Groups["url"].Value) : String.Empty;
                }
            }

            return pageVideos;
        }

        private List<VideoInfo> GetVideoList(Category category)
        {
            hasNextPage = false;
            String baseWebData = String.Empty;
            RssLink parentCategory = (RssLink)category;
            List<VideoInfo> videoList = new List<VideoInfo>();

            if (parentCategory.Name != this.currentCategory.Name)
            {
                this.currentStartIndex = 0;
                this.nextPageUrl = parentCategory.Url;
                this.loadedEpisodes.Clear();
            }

            this.currentCategory = parentCategory;
            this.loadedEpisodes.AddRange(this.GetPageVideos(this.nextPageUrl));
            while (this.currentStartIndex < this.loadedEpisodes.Count)
            {
                videoList.Add(this.loadedEpisodes[this.currentStartIndex++]);
            }

            if (!String.IsNullOrEmpty(this.nextPageUrl))
            {
                hasNextPage = true;
            }

            return videoList;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            this.currentStartIndex = 0;
            return this.GetVideoList(category);
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return this.GetVideoList(this.currentCategory);
        }

        public override bool HasNextPage
        {
            get
            {
                return this.hasNextPage;
            }
            protected set
            {
                this.hasNextPage = value;
            }
        }

        private String SerializeJsonForPost(Newtonsoft.Json.Linq.JToken token)
        {
            Newtonsoft.Json.JsonSerializer serializer = Newtonsoft.Json.JsonSerializer.Create(null);
            StringBuilder builder = new StringBuilder();
            serializer.Serialize(new CeskaTelevizeJsonTextWriter(new System.IO.StringWriter(builder), token), token);
            return builder.ToString();
        }

        public override List<string> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            List<String> resultUrls = new List<string>();

            Boolean live = (this.currentCategory.Name == "Živě");

            System.Net.CookieContainer container = new System.Net.CookieContainer();
            String baseWebData = SiteUtilBase.GetWebData(video.VideoUrl, container, null, null, true);

            int start = baseWebData.IndexOf(CeskaTelevizeUtil.showEpisodePostStart);
            if (start >= 0)
            {
                start += CeskaTelevizeUtil.showEpisodePostStart.Length;
                int end = baseWebData.IndexOf(CeskaTelevizeUtil.showEpisodePostEnd, start);
                if (end >= 0)
                {
                    String postData = baseWebData.Substring(start, end - start);

                    Newtonsoft.Json.Linq.JObject jObject = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(postData);
                    String serializedDataForPost = this.SerializeJsonForPost(jObject);
                    serializedDataForPost = HttpUtility.UrlEncode(serializedDataForPost).Replace("%3d", "=").Replace("%26", "&");
                    String videoDataUrl = CeskaTelevizeUtil.GetWebDataFromPost("http://www.ceskatelevize.cz/ajax/playlistURL.php", serializedDataForPost, container, video.VideoUrl);

                    CeskaTelevizeVideoCollection videos = new CeskaTelevizeVideoCollection();
                    int videoPart = 1;

                    XmlDocument videoData = new XmlDocument();
                    videoData.LoadXml(SiteUtilBase.GetWebData(videoDataUrl));

                    XmlNodeList videoItems = videoData.SelectNodes("//PlaylistItem[@id]");
                    foreach (XmlNode videoItem in videoItems)
                    {
                        if (videoItem.Attributes["id"].Value.IndexOf("ad", StringComparison.CurrentCultureIgnoreCase) == (-1))
                        {
                            // skip advertising
                            XmlNode itemData = videoData.SelectSingleNode(String.Format("//switchItem[@id = \"{0}\"]", videoItem.Attributes["id"].Value));
                            if (itemData != null)
                            {
                                // now select source with highest bitrate
                                XmlNodeList sources = itemData.SelectNodes("./video");

                                foreach (XmlNode source in sources)
                                {
                                    // create rtmp proxy for selected source
                                    String baseUrl = itemData.Attributes["base"].Value.Replace("/_definst_", "");
                                    String playPath = source.Attributes["src"].Value;

                                    String rtmpUrl = baseUrl + "/" + playPath;

                                    String host = new Uri(baseUrl).Host;
                                    String app = baseUrl.Substring(baseUrl.LastIndexOf('/') + 1);
                                    String tcUrl = baseUrl;

                                    int swfobjectIndex = baseWebData.IndexOf("swfobject.embedSWF(");
                                    if (swfobjectIndex >= 0)
                                    {
                                        int firstQuote = baseWebData.IndexOf("\"", swfobjectIndex);
                                        int secondQuote = baseWebData.IndexOf("\"", firstQuote + 1);

                                        if ((firstQuote >= 0) && (secondQuote >= 0) && ((secondQuote - firstQuote) > 0))
                                        {
                                            String swfUrl = baseWebData.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                                            String resultUrl = new OnlineVideos.MPUrlSourceFilter.RtmpUrl(rtmpUrl) { TcUrl = tcUrl, App = app, PlayPath = playPath, SwfUrl = swfUrl, PageUrl = video.VideoUrl, LiveStream = live }.ToString();

                                            videos.Add(new CeskaTelevizeVideo()
                                            {
                                                Part = videoPart,
                                                Label = source.Attributes["label"].Value,
                                                Url = resultUrl
                                            });

                                        }
                                    }
                                }
                            }

                            videoPart++;
                        }
                    }

                    // remember all videos with their quality
                    video.Other = videos;

                    videoPart = 0;
                    foreach (var ctVideo in videos)
                    {
                        if (ctVideo.Part != videoPart)
                        {
                            resultUrls.Add(ctVideo.Url);
                            videoPart = ctVideo.Part;
                        }
                    }
                }
            }

            return resultUrls;
        }

        public override string getPlaylistItemUrl(VideoInfo clonedVideoInfo, string chosenPlaybackOption, bool inPlaylist = false)
        {
            CeskaTelevizeVideoCollection videos = (CeskaTelevizeVideoCollection)clonedVideoInfo.Other;
            CeskaTelevizeVideo keyVideo = videos[clonedVideoInfo.VideoUrl];

            if (clonedVideoInfo.PlaybackOptions == null)
            {
                clonedVideoInfo.PlaybackOptions = new Dictionary<string, string>();
            }
            clonedVideoInfo.PlaybackOptions.Clear();

            foreach (var ctVideo in videos)
            {
                if (ctVideo.Part == keyVideo.Part)
                {
                    clonedVideoInfo.PlaybackOptions.Add(ctVideo.Label, ctVideo.Url);
                }
            }

            if (clonedVideoInfo.PlaybackOptions.Count > 0)
            {
                var enumer = clonedVideoInfo.PlaybackOptions.GetEnumerator();
                enumer.MoveNext();
                return enumer.Current.Value;
            }

            return clonedVideoInfo.VideoUrl;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            int dynamicSubCategoriesCount = 0;
            RssLink category = (RssLink)parentCategory;

            String baseWebData = SiteUtilBase.GetWebData(category.Url, null, null, null, true);

            int index = baseWebData.IndexOf(CeskaTelevizeUtil.showListStart);
            if (index > 0)
            {
                baseWebData = baseWebData.Substring(index);
                category.SubCategories = new List<Category>();

                MatchCollection matches = Regex.Matches(baseWebData, CeskaTelevizeUtil.showRegex);
                for (int i = 0; i < matches.Count; i++)
                {
                    String showUrl = matches[i].Groups["showUrl"].Value;
                    String showTitle = matches[i].Groups["showTitle"].Value;

                    int showEndIndex = baseWebData.IndexOf(CeskaTelevizeUtil.showEnd, matches[i].Index + matches[i].Length);
                    int onlyBonusesIndex = baseWebData.IndexOf(CeskaTelevizeUtil.onlyBonuses, matches[i].Index + matches[i].Length);

                    if (((onlyBonusesIndex != (-1)) && (onlyBonusesIndex > showEndIndex)) ||
                        (onlyBonusesIndex == (-1)))
                    {
                        category.SubCategoriesDiscovered = true;
                        category.SubCategories.Add(
                            new RssLink()
                            {
                                Name = showTitle,
                                HasSubCategories = false,
                                Url = String.Format(CeskaTelevizeUtil.subCategoryFormat, CeskaTelevizeUtil.baseUrl, showUrl)
                            });

                        dynamicSubCategoriesCount++;
                    }
                }
            }

            return dynamicSubCategoriesCount;
        }

        public new static string GetWebData(string url, CookieContainer cc = null, string referer = null, IWebProxy proxy = null, bool forceUTF8 = false, bool allowUnsafeHeader = false, string userAgent = null, Encoding encoding = null)
        {
            HttpWebResponse response = null;
            try
            {
                string requestCRC = OnlineVideos.Utils.EncryptLine(string.Format("{0}{1}{2}{3}{4}", url, referer, userAgent, proxy != null ? proxy.GetProxy(new Uri(url)).AbsoluteUri : "", cc != null ? cc.GetCookieHeader(new Uri(url)) : ""));

                // try cache first
                string cachedData = WebCache.Instance[requestCRC];
                Log.Debug("GetWebData{1}: '{0}'", url, cachedData != null ? " (cached)" : "");
                if (cachedData != null) return cachedData;

                // request the data
                if (allowUnsafeHeader) OnlineVideos.Utils.SetAllowUnsafeHeaderParsing(true);
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                if (request == null) return "";
                if (!String.IsNullOrEmpty(userAgent))
                    request.UserAgent = userAgent; // set specific UserAgent if given
                else
                    request.UserAgent = OnlineVideoSettings.Instance.UserAgent; // set OnlineVideos default UserAgent
                request.Accept = "*/*"; // we accept any content type
                request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate"); // we accept compressed content

                request.Headers.Add("X-Requested-With: XMLHttpRequest");
                request.Headers.Add("x-addr: 127.0.0.1");

                if (!String.IsNullOrEmpty(referer)) request.Referer = referer; // set referer if given
                if (cc != null) request.CookieContainer = cc; // set cookies if given
                if (proxy != null) request.Proxy = proxy; // send the request over a proxy if given
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
                if (allowUnsafeHeader) OnlineVideos.Utils.SetAllowUnsafeHeaderParsing(false);
            }
        }

        public new static string GetWebDataFromPost(string url, string postData, CookieContainer cc = null, string referer = null, IWebProxy proxy = null, bool forceUTF8 = false, bool allowUnsafeHeader = false, string userAgent = null, Encoding encoding = null)
        {
            try
            {
                Log.Debug("GetWebDataFromPost: '{0}'", url);

                // request the data
                if (allowUnsafeHeader) OnlineVideos.Utils.SetAllowUnsafeHeaderParsing(true);
                byte[] data = encoding != null ? encoding.GetBytes(postData) : Encoding.UTF8.GetBytes(postData);

                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                if (request == null) return "";
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                if (!String.IsNullOrEmpty(userAgent))
                    request.UserAgent = userAgent;
                else
                    request.UserAgent = OnlineVideoSettings.Instance.UserAgent;
                request.ContentLength = data.Length;
                request.ProtocolVersion = HttpVersion.Version10;
                request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");

                request.Headers.Add("X-Requested-With: XMLHttpRequest");
                request.Headers.Add("x-addr: 127.0.0.1");

                if (!String.IsNullOrEmpty(referer)) request.Referer = referer;
                if (cc != null) request.CookieContainer = cc;
                if (proxy != null) request.Proxy = proxy;

                Stream requestStream = request.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Stream responseStream;
                    if (response.ContentEncoding.ToLower().Contains("gzip"))
                        responseStream = new System.IO.Compression.GZipStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                    else if (response.ContentEncoding.ToLower().Contains("deflate"))
                        responseStream = new System.IO.Compression.DeflateStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                    else
                        responseStream = response.GetResponseStream();

                    // UTF8 is the default encoding as fallback
                    Encoding responseEncoding = Encoding.UTF8;
                    // try to get the response encoding if one was specified and neither forceUTF8 nor encoding were set as parameters
                    if (!forceUTF8 && encoding == null && !String.IsNullOrEmpty(response.CharacterSet.Trim())) responseEncoding = Encoding.GetEncoding(response.CharacterSet.Trim(new char[] { ' ', '"' }));
                    // the caller did specify a forced encoding
                    if (encoding != null) responseEncoding = encoding;
                    // the caller wants to force UTF8
                    if (forceUTF8) responseEncoding = Encoding.UTF8;

                    using (StreamReader reader = new StreamReader(responseStream, responseEncoding, true))
                    {
                        string str = reader.ReadToEnd();
                        return str.Trim();
                    }
                }
            }
            finally
            {
                // disable unsafe header parsing if it was enabled
                if (allowUnsafeHeader) OnlineVideos.Utils.SetAllowUnsafeHeaderParsing(false);
            }
        }

        #endregion
    }
}
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using OnlineVideos.Sites.Brownard.Extensions;
using OnlineVideos.Sites.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class BBCiPlayerUtil : SiteUtilBase
    {
        #region Constants
        // https://open.live.bbc.co.uk/mediaselector/6/select/version/2.0/mediaset/apple-ipad-hls/vpid/m00031q0/format/xml
        const string MEDIA_SELECTOR_URL = "http://open.live.bbc.co.uk/mediaselector/5/select/version/2.0/mediaset/pc/vpid/"; //"http://www.bbc.co.uk/mediaselector/4/mtis/stream/";
        const string HLS_MEDIA_SELECTOR_URL = "https://open.live.bbc.co.uk/mediaselector/6/select/version/2.0/mediaset/{0}/vpid/{1}/format/xml"; // "https://open.live.bbc.co.uk/mediaselector/6/select/version/2.0/mediaset/apple-ipad-hls/vpid/{0}/format/xml";
        const string DEFAULT_MEDIA_SET = "iptv-all";
        const string ATOZ_URL = "https://www.bbc.co.uk/iplayer/a-z/";
        static readonly string[] atoz = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "0-9" };

        static readonly Uri BASE_URL = new Uri("https://www.bbc.co.uk");

        #endregion

        #region Settings

        [Category("OnlineVideosUserConfiguration"), Description("Select stream automatically?")]
        protected bool AutoSelectStream = false;
        [Category("OnlineVideosUserConfiguration"), Description("Proxy to use for WebRequests (must be in the UK). Define like this: 83.84.85.86:8116")]
        string proxy = null;
        [Category("OnlineVideosUserConfiguration"), Description("If your proxy requires a username, set it here.")]
        string proxyUsername = null;
        [Category("OnlineVideosUserConfiguration"), Description("If your proxy requires a password, set it here.")]
        string proxyPassword = null;
        [Category("OnlineVideosUserConfiguration"), Description("Whether to download subtitles")]
        protected bool RetrieveSubtitles = false;
        [Category("OnlineVideosConfiguration"), Description("Format string used as Url for getting the results of a search. {0} will be replaced with the query.")]
        string searchUrl = "http://www.bbc.co.uk/iplayer/search?q={0}";
        [Category("OnlineVideosConfiguration"), Description("Debug setting to change the media set to use when retrieving urls.")]
        protected string mediaSet = DEFAULT_MEDIA_SET;

        //TV Guide options
        [Category("OnlineVideosUserConfiguration"), Description("Whether to retrieve current program info for live streams.")]
        protected bool retrieveTVGuide = true;
        [Category("OnlineVideosConfiguration"), Description("The layout to use to display TV Guide info, possible wildcards are <nowtitle>,<nowdescription>,<nowstart>,<nowend>,<nexttitle>,<nextstart>,<nextend>,<newline>")]
        protected string tvGuideFormatString;

        #endregion

        #region Regex
        
        static readonly Regex srcsetRegex = new Regex(@"http[^\s""]*");
        static readonly Regex videoJsonRegex = new Regex(@"__IPLAYER_REDUX_STATE__ = (.*?);</script>");

        #endregion

        #region Init

        string defaultThumb;
        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            defaultThumb = string.Format(@"{0}\Icons\{1}.png", OnlineVideoSettings.Instance.ThumbsDir, siteSettings.Name);
        }

        #endregion

        #region GetUrl

        public override List<string> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            string url = GetVideoUrl(video);
            if (inPlaylist)
                video.PlaybackOptions.Clear();
            return new List<string>() { url };
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            if (video.Other as string == "livestream")
                return getLiveUrls(video);
            else
                return getCatchupUrls(video);
        }

        string getCatchupUrls(VideoInfo video)
        {
            WebProxy proxyObj = getProxy();
            string html = GetWebData(video.VideoUrl, proxy: proxyObj);

            string vpid;
            if (!TryParseVpid(html, out vpid))
            {
                Log.Warn("BBCiPlayer: Failed to parse vpid from '{0}'", video.VideoUrl);
                return null;
            }

            XmlDocument doc = GetWebData<XmlDocument>(MEDIA_SELECTOR_URL + vpid, proxy: proxyObj); //uk only
            XmlNamespaceManager nsmRequest = new XmlNamespaceManager(doc.NameTable);
            nsmRequest.AddNamespace("ns1", "http://bbc.co.uk/2008/mp/mediaselection");

            if (RetrieveSubtitles)
            {
                XmlNode captionNode = doc.SelectSingleNode("//ns1:media[@kind='captions']", nsmRequest);
                if (captionNode != null)
                {
                    XmlNode captionConnection = captionNode.SelectSingleNode("ns1:connection", nsmRequest);
                    if (captionConnection != null && captionConnection.Attributes["href"] != null)
                    {
                        string sub = GetWebData(captionConnection.Attributes["href"].Value);
                        video.SubtitleText = OnlineVideos.Sites.Utils.SubtitleReader.TimedText2SRT(sub);
                    }
                }
            }

            SortedList<string, string> sortedPlaybackOptions = new SortedList<string, string>(new StreamComparer());
            foreach (XmlElement mediaElem in doc.SelectNodes("//ns1:media[@kind='video']", nsmRequest))
            {
                string info = "";
                string resultUrl = "";
                foreach (XmlElement connectionElem in mediaElem.SelectNodes("ns1:connection", nsmRequest))
                {
                    string supplier = connectionElem.Attributes["supplier"].Value; //"kind"
                    if (Array.BinarySearch<string>(new string[] { "akamai", "level3", "limelight" }, supplier) >= 0)
                    {
                        // rtmp
                        if (connectionElem.Attributes["protocol"] == null || connectionElem.Attributes["protocol"].Value != "rtmp")
                            continue;

                        string server = connectionElem.Attributes["server"].Value;
                        string identifier = connectionElem.Attributes["identifier"].Value;
                        string auth = connectionElem.Attributes["authString"].Value;
                        string application = connectionElem.GetAttribute("application");
                        if (string.IsNullOrEmpty(application)) application = "ondemand";
                        string SWFPlayer = "http://www.bbc.co.uk/emp/releases/iplayer/revisions/617463_618125_4/617463_618125_4_emp.swf"; // "http://www.bbc.co.uk/emp/10player.swf";

                        info = string.Format("{0}x{1} | {2} kbps | {3}", mediaElem.GetAttribute("width"), mediaElem.GetAttribute("height"), mediaElem.GetAttribute("bitrate"), supplier);
                        resultUrl = "";
                        if (supplier == "limelight")
                        {
                            resultUrl = new MPUrlSourceFilter.RtmpUrl(string.Format("rtmp://{0}:1935/{1}", server, application + "?" + auth), server, 1935)
                            {
                                App = application + "?" + auth,
                                PlayPath = identifier,
                                SwfUrl = SWFPlayer,
                                SwfVerify = true,
                            }.ToString();
                        }
                        else if (supplier == "level3")
                        {
                            resultUrl = new MPUrlSourceFilter.RtmpUrl(string.Format("rtmp://{0}:1935/{1}", server, application + "?" + auth), server, 1935)
                            {
                                App = application + "?" + auth,
                                PlayPath = identifier,
                                SwfUrl = SWFPlayer,
                                SwfVerify = true,
                                Token = auth,
                            }.ToString();
                        }
                        else if (supplier == "akamai")
                        {
                            resultUrl = new MPUrlSourceFilter.RtmpUrl(string.Format("rtmp://{0}:1935/{1}?{2}", server, application, auth))
                            {
                                PlayPath = identifier + "?" + auth,
                                SwfUrl = SWFPlayer,
                                SwfVerify = true,
                            }.ToString();
                        }
                    }
                    if (resultUrl != "") sortedPlaybackOptions.Add(info, resultUrl);
                }
            }

            video.PlaybackOptions = new Dictionary<string, string>();
            if (sortedPlaybackOptions.Count > 0)
            {
                if (AutoSelectStream)
                {
                    var last = sortedPlaybackOptions.Last();
                    video.PlaybackOptions.Add(last.Key, last.Value);
                    return last.Value;
                }
                else
                {
                    foreach (var option in sortedPlaybackOptions)
                        video.PlaybackOptions.Add(option.Key, option.Value);
                    return sortedPlaybackOptions.Last().Value;
                }
            }

            //Fallback to HLS streams
            string url = getHLSVideoUrls(video, vpid, proxyObj, mediaSet);
            if (!string.IsNullOrEmpty(url))
                return url;

            var errorNodes = doc.SelectNodes("//ns1:error", nsmRequest);
            if (errorNodes.Count > 0)
                throw new OnlineVideosException(string.Format("BBC says: {0}", ((XmlElement)errorNodes[0]).GetAttribute("id")));
            return null;
        }

        bool TryParseVpid(string html, out string vpid)
        {
            vpid = null;
            Match videoJsonMatch = videoJsonRegex.Match(html);
            if (!videoJsonMatch.Success)
                return false;

            var versions = (JArray)JObject.Parse(videoJsonMatch.Groups[1].Value)?["versions"];
            if (versions == null || !versions.HasValues)
                return false;

            vpid = (string)versions[0]["id"];
            return !string.IsNullOrWhiteSpace(vpid);
        }

        string getHLSVideoUrls(VideoInfo video, string vpid, WebProxy proxyObj, string mediaSet)
        {
            string url = string.Format(HLS_MEDIA_SELECTOR_URL, string.IsNullOrEmpty(mediaSet) ? DEFAULT_MEDIA_SET : mediaSet, vpid);
            XmlDocument doc = GetWebData<XmlDocument>(url, proxy: proxyObj); //uk only

            XmlNamespaceManager nsmRequest = new XmlNamespaceManager(doc.NameTable);
            nsmRequest.AddNamespace("ns1", "http://bbc.co.uk/2008/mp/mediaselection");

            video.PlaybackOptions = new Dictionary<string, string>();
            foreach (XmlElement mediaElem in doc.SelectNodes("//ns1:media[@kind='video']", nsmRequest))
            {
                foreach (XmlElement connectionElem in mediaElem.SelectNodes("ns1:connection", nsmRequest))
                {
                    if (connectionElem.Attributes["transferFormat"] == null || connectionElem.Attributes["transferFormat"].Value != "hls" ||
                        connectionElem.Attributes["href"] == null)
                        continue;
                    string playlistUrl = connectionElem.Attributes["href"].Value;
                    string playlistStr = GetWebData(playlistUrl, proxy: proxyObj, userAgent: HlsPlaylistParser.APPLE_USER_AGENT);
                    HlsPlaylistParser playlist = new HlsPlaylistParser(playlistStr, playlistUrl);
                    if (playlist.StreamInfos.Count > 0)
                        return populateHlsPlaybackOptions(video, playlist.StreamInfos);
                }
            }
            return null;
        }

        string getLiveUrls(VideoInfo video)
        {
            WebProxy proxyObj = getProxy();

            return getHLSVideoUrls(video, video.VideoUrl, proxyObj, DEFAULT_MEDIA_SET);
        }

        string populateHlsPlaybackOptions(VideoInfo video, List<HlsStreamInfo> streamInfos)
        {
            if (AutoSelectStream)
            {
                HlsStreamInfo streamInfo = streamInfos.Last();
                string name = string.Format("{0}x{1} | {2} kbps", streamInfo.Width, streamInfo.Height, streamInfo.Bandwidth / 1024);
                video.PlaybackOptions.Add(name, streamInfo.Url);
                return streamInfo.Url;
            }
            else
            {
                foreach (HlsStreamInfo streamInfo in streamInfos)
                {
                    string name = string.Format("{0}x{1} | {2} kbps", streamInfo.Width, streamInfo.Height, streamInfo.Bandwidth / 1024);
                    video.PlaybackOptions.Add(name, streamInfo.Url);
                }
                return streamInfos.Last().Url;
            }
        }

        public override string GetFileNameForDownload(VideoInfo video, Category category, string url)
        {
            string f = base.GetFileNameForDownload(video, category, url);
            if (f.EndsWith(".m3u8"))
                f = f.Substring(0, f.Length - 5) + ".mp4";
            return f;
        }

        WebProxy getProxy()
        {
            WebProxy proxyObj = null;// new System.Net.WebProxy("127.0.0.1", 8118);
            if (!string.IsNullOrEmpty(proxy))
            {
                proxyObj = new WebProxy(proxy);
                if (!string.IsNullOrEmpty(proxyUsername) && !string.IsNullOrEmpty(proxyPassword))
                    proxyObj.Credentials = new NetworkCredential(proxyUsername, proxyPassword);
            }
            return proxyObj;
        }

        #endregion

        #region Categories

        DateTime lastRefresh = DateTime.MinValue;
        public override int DiscoverDynamicCategories()
        {
            if ((DateTime.Now - lastRefresh).TotalMinutes > 15)
            {
                foreach (Category cat in Settings.Categories)
                {
                    if (cat is RssLink)
                    {
                        if ((cat as RssLink).Url == ATOZ_URL)
                        {
                            if (!cat.SubCategoriesDiscovered)
                            {
                                cat.Thumb = defaultThumb;
                                cat.HasSubCategories = true;
                                cat.SubCategoriesDiscovered = true;
                                cat.SubCategories = getAtoZCategories(cat);
                            }
                        }
                        else
                        {
                            cat.HasSubCategories = true;
                            cat.SubCategoriesDiscovered = false;
                            if (string.IsNullOrEmpty(cat.Thumb)) cat.Thumb = defaultThumb;
                        }
                    }
                }
                lastRefresh = DateTime.Now;
            }
            return Settings.Categories.Count;
        }

        List<Category> getAtoZCategories(Category parentCategory)
        {
            List<Category> cats = new List<Category>();
            foreach (string s in atoz)
            {
                cats.Add(new RssLink()
                {
                    Url = ATOZ_URL + s,
                    Name = s,
                    Thumb = defaultThumb,
                    ParentCategory = parentCategory,
                    HasSubCategories = true
                });
            }
            return cats;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string url = (parentCategory as RssLink).Url;
            parentCategory.SubCategories = url.StartsWith(ATOZ_URL) ?
                discoverAtoZSubCategories(parentCategory, url) :
                discoverSubCategoriesLocal(parentCategory, url);
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        List<Category> discoverAtoZSubCategories(Category parentCategory, string url)
        {
            List<Category> categories = new List<Category>();
            HtmlDocument document = GetWebData<HtmlDocument>(url);
            var programmes = document.DocumentNode.SelectNodes(@"//div[contains(@class, 'atoz-grid')]/ul/li");
            foreach (var programme in programmes)
            {
                var urlNode = programme.SelectSingleNode(@".//a");
                if (urlNode == null)
                    continue;


                categories.Add(new RssLink()
                {
                    Url = GetAbsoluteUri(urlNode.GetAttributeValue("href", string.Empty), BASE_URL).ToString(),
                    Name = programme.SelectSingleNode(@".//p[contains(@class, 'list-content-item__title')]").GetCleanInnerText(),
                    Description = programme.SelectSingleNode(@".//p[contains(@class, 'list-content-item__synopsis')]").GetCleanInnerText(),
                    Thumb = getImageUrl(programme.SelectSingleNode(@".//img")),
                    ParentCategory = parentCategory
                });
            }
            return categories;
        }

        List<Category> discoverSubCategoriesLocal(Category parentCategory, string url)
        {
            List<Category> categories = new List<Category>();

            var document = GetWebData<HtmlDocument>(url).DocumentNode;

            int pageCount = getPageCount(document);
            int currentPage = 1;

            while (true)
            {
                var scriptNode = document.SelectSingleNode(@"//script[@id='tvip-script-app-store']");
                if (scriptNode != null)
                {
                    int index = scriptNode.InnerText.IndexOf('=');
                    if (index > -1)
                    {
                        string json = scriptNode.InnerText.Substring(index + 1).Trim().TrimEnd(';');
                        addCategoriesFromJSON(json, categories, parentCategory);
                    }
                }

                currentPage++;
                if (currentPage > pageCount)
                    break;

                document = GetWebData<HtmlDocument>(url + "?page=" + currentPage).DocumentNode;
            }
            return categories;
        }

        void addCategoriesFromJSON(string json, List<Category> categories, Category parentCategory)
        {
            JObject data = JObject.Parse(json);

            var entities = data["entities"];
            if (entities != null)
            {
                foreach (var entity in entities)
                {
                    var properties = entity["contentItemProps"];
                    categories.Add(new RssLink
                    {
                        ParentCategory = parentCategory,
                        Url = GetAbsoluteUri(properties["href"].ToString(), BASE_URL).ToString(),
                        Name = properties["title"].ToString(),
                        Description = properties["synopsis"].ToString(),
                        Thumb = getImageUrl(properties["sources"][0]["srcset"].ToString())
                    });
                }
            }
        }

        #endregion

        #region Videos

        public override List<VideoInfo> GetVideos(Category category)
        {
            if (category is Group)
                return getLiveVideoList((Group)category);

            return getVideos(category);
        }

        List<VideoInfo> getVideos(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();

            string url = (category as RssLink).Url;

            var document = GetWebData<HtmlDocument>(url).DocumentNode;
            List<HtmlNode> videoNodes = null;

            // Check for an 'all episdoes' link if not currently on the episodes page
            if (!url.Contains("/iplayer/episodes/"))
            {
                var allEpisodes = document.SelectSingleNode(@"//a[starts-with(@href, '/iplayer/episodes/')]");
                if (allEpisodes != null)
                {
                    document = GetWebData<HtmlDocument>(BASE_URL + allEpisodes.GetAttributeValue("href", "")).DocumentNode;
                    videoNodes = document.SelectNodes(@"//div[contains(@class, 'content-item--')]")?.ToList();
                }
            }

            if (videoNodes == null)
                videoNodes = document.SelectNodes(@"//div[contains(@class, 'content-item--')]")?.ToList();

            if (videoNodes != null)
            {
                // If we've found episodes, look for additional series
                HtmlNodeCollection additionalSeriesNodes = document.SelectNodes(@"//a[contains(@class, 'series-nav__button')]");
                if (additionalSeriesNodes != null)
                {
                    foreach (HtmlNode seriesNode in additionalSeriesNodes)
                    {
                        // Load page for the series and get all episodes
                        var seriesEpisodeNodes = GetWebData<HtmlDocument>(GetAbsoluteUri(seriesNode.Attributes["href"].Value, BASE_URL).ToString())?
                            .DocumentNode?.SelectNodes(@"//div[contains(@class, 'content-item--')]");
                        if (seriesEpisodeNodes != null)
                            videoNodes.AddRange(seriesEpisodeNodes);
                    }
                }
            }

            var singleVideoNode = document.SelectSingleNode(@"//div[@id='main']");

            // Single video
            if (videoNodes == null)
            {
                if (singleVideoNode != null)
                {
                    VideoInfo video = createSingleVideo(singleVideoNode, url, category.Name);
                    if (video != null)
                        videos.Add(video);
                }
                return videos;
            }

            bool usedSingleVideo = false;
            foreach (var videoNode in videoNodes)
            {
                var urlNode = videoNode.SelectSingleNode(@"./a");
                if (urlNode != null)
                {
                    VideoInfo video = createVideo(videoNode, category.Name);
                    if (video != null)
                        videos.Add(video);
                }
                else
                {
                    if (usedSingleVideo)
                        continue;
                    usedSingleVideo = true;
                    VideoInfo video = createSingleVideo(singleVideoNode, url, category.Name);
                    if (video != null)
                        videos.Add(video);
                }
            }

            int pageCount = getPageCount(document);
            int currentPage = 1;

            while (currentPage < pageCount)
            {
                currentPage++;
                document = GetWebData<HtmlDocument>(url + "?page=" + currentPage).DocumentNode;
                videoNodes = document.SelectNodes(@"//div[contains(@class, 'content-item--')]")?.ToList();
                if (videoNodes == null)
                    break;
                videos.AddRange(videoNodes.Select(v => createVideo(v, category.Name)).Where(v => v != null));
            }
            return videos;
        }

        VideoInfo createVideo(HtmlNode videoNode, string defaultTitle)
        {
            var urlNode = videoNode.SelectSingleNode(@"./a");
            if (urlNode == null)
                return null;

            string title = videoNode.SelectSingleNode(@".//div[contains(@class, 'content-item__title')]").GetCleanInnerText();
            if (string.IsNullOrEmpty(title))
                title = defaultTitle;

            return new VideoInfo()
            {
                VideoUrl = GetAbsoluteUri(urlNode.GetAttributeValue("href", ""), BASE_URL).ToString(),
                Title = title,
                Description = videoNode.SelectSingleNode(@".//div[contains(@class, 'content-item__description')]").GetCleanInnerText(),
                Length = videoNode.SelectSingleNode(@".//div[contains(@class, 'content-item__sublabels')]/span").GetCleanInnerText(),
                Thumb = getImageUrl(videoNode.SelectSingleNode(@".//img"))                
            };
        }

        VideoInfo createSingleVideo(HtmlNode videoNode, string url, string defaultTitle)
        {
            string title = videoNode.SelectSingleNode(@".//span[contains(@class, 'play-cta__title')]").GetCleanInnerText();
            if (title == defaultTitle)
            {
                string subtitle = videoNode.SelectSingleNode(@".//span[contains(@class, 'play-cta__subtitle')]").GetCleanInnerText();
                if (!string.IsNullOrWhiteSpace(subtitle))
                    title = subtitle;
            }
            return new VideoInfo()
            {
                VideoUrl = url,
                Title = title,
                Description = videoNode.SelectSingleNode(@".//div[contains(@class, 'synopsis')]/p").GetCleanInnerText(),
                Airdate = videoNode.SelectSingleNode(@".//div[contains(@class, 'metadata__container--first')]/p").GetCleanInnerText().Replace("First shown:", "").Trim(),
                Length = videoNode.SelectSingleNode(@".//p[contains(@class, 'metadata__item--last')]").GetCleanInnerText().Replace("Duration", "").Trim(),
                Thumb = getImageUrl(videoNode.SelectSingleNode(@".//picture/source"))
            };
        }

        List<VideoInfo> getLiveVideoList(Group category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            foreach (Channel channel in category.Channels)
            {
                VideoInfo video = new VideoInfo();
                video.Title = channel.StreamName;
                video.Other = "livestream";
                video.Thumb = channel.Thumb;
                string url = channel.Url;
                string guideId;
                if (TVGuideGrabber.TryGetIdAndRemove(ref url, out guideId))
                {
                    NowNextDetails guide;
                    if (retrieveTVGuide && TVGuideGrabber.TryGetNowNext(guideId, out guide))
                        video.Description = guide.Format(tvGuideFormatString);
                }
                video.VideoUrl = url;
                videos.Add(video);
            }
            return videos;
        }

        #endregion

        #region Search

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            List<SearchResultItem> results = new List<SearchResultItem>();
            foreach (Category cat in discoverSubCategoriesLocal(null, string.Format(searchUrl, query)))
                results.Add(cat);
            return results;
        }

        public override bool CanSearch { get { return !string.IsNullOrEmpty(searchUrl); } }

        #endregion

        #region TrackingInfo

        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            TrackingDetails details = video.Other as TrackingDetails;
            if (details != null)
            {
                TrackingInfo ti = new TrackingInfo() { Title = details.SeriesTitle, VideoKind = VideoKind.TvSeries };
                if (!string.IsNullOrEmpty(details.EpisodeTitle))
                {
                    Match m = Regex.Match(details.EpisodeTitle, @"Series ((?<num>\d+)|(?<alpha>[A-z]+))");
                    if (m.Success)
                    {
                        if (m.Groups["num"].Success)
                            ti.Season = uint.Parse(m.Groups["num"].Value);
                        else
                            ti.Season = (uint)(m.Groups["alpha"].Value[0] % 32); //special case for QI, convert char to position in alphabet
                    }

                    if ((m = Regex.Match(details.EpisodeTitle, @"Episode (\d+)")).Success || (m = Regex.Match(details.EpisodeTitle, @":\s*(\d+)\.")).Success || (m = Regex.Match(details.EpisodeTitle, @"^(\d+)\.")).Success)
                    {
                        ti.Episode = uint.Parse(m.Groups[1].Value);
                        //if we've got an episode number but no season, presume season 1
                        if (ti.Season == 0)
                            ti.Season = 1;
                    }
                }
                Log.Debug("BBCiPlayer: Parsed tracking info: Title '{0}' Season {1} Episode {2}", ti.Title, ti.Season, ti.Episode);
                return ti;
            }
            return base.GetTrackingInfo(video);
        }

        #endregion

        #region Utils

        static int getPageCount(HtmlNode document)
        {
            var pageNumbers = document.SelectNodes(@"//li[contains(@class, 'pagination__number')]/a");
            if (pageNumbers == null)
                pageNumbers = document.SelectNodes(@"//li[contains(@class, 'pagination__item--page')]/a");

            int pageCount = 1;
            if (pageNumbers != null && pageNumbers.Count > 0)
            {
                string lastPageUrl = pageNumbers.Last().GetAttributeValue("href", "").ParamsCleanup();
                Match m = Regex.Match(lastPageUrl, @"page=(\d+)");
                if (m.Success)
                    pageCount = int.Parse(m.Groups[1].Value);
            }
            return pageCount;
        }

        static string getImageUrl(HtmlNode sourceNode)
        {
            if (sourceNode != null)
                return getImageUrl(sourceNode.GetAttributeValue("srcset", ""));
            return null;
        }

        static string getImageUrl(string srcset)
        {
            MatchCollection srcMatch = srcsetRegex.Matches(srcset);
            if (srcMatch.Count > 0)
                return srcMatch[srcMatch.Count - 1].Value.Trim();
            return null;
        }

        static Uri GetAbsoluteUri(string possibleAbsoluteUrl, Uri baseUrl)
        {
            Uri uri = new Uri(possibleAbsoluteUrl, UriKind.RelativeOrAbsolute);
            return uri.IsAbsoluteUri ? uri : new Uri(baseUrl, uri);
        }

        #endregion
    }

    class TrackingDetails
    {
        public string SeriesTitle { get; set; }
        public string EpisodeTitle { get; set; }
    }
}

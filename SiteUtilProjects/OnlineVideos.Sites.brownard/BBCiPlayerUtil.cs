using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Xml;
using OnlineVideos.Sites.Brownard.Extensions;
using System.Net;
using OnlineVideos.Sites.Utils;
using System.Linq;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class BBCiPlayerUtil : SiteUtilBase
    {
        #region Constants
        // https://open.live.bbc.co.uk/mediaselector/6/select/version/2.0/mediaset/apple-ipad-hls/vpid/m00031q0/format/xml
        const string MEDIA_SELECTOR_URL = "http://open.live.bbc.co.uk/mediaselector/5/select/version/2.0/mediaset/pc/vpid/"; //"http://www.bbc.co.uk/mediaselector/4/mtis/stream/";
        const string HLS_MEDIA_SELECTOR_URL = "https://open.live.bbc.co.uk/mediaselector/6/select/version/2.0/mediaset/iptv-all/vpid/{0}/format/xml"; // "https://open.live.bbc.co.uk/mediaselector/6/select/version/2.0/mediaset/apple-ipad-hls/vpid/{0}/format/xml";
        const string MOST_POPULAR_URL = "https://www.bbc.co.uk/iplayer/most-popular";
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
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used on a video thumbnail for matching a string to be replaced for higher quality")]
        protected string thumbReplaceRegExPattern;
        [Category("OnlineVideosConfiguration"), Description("The string used to replace the match if the pattern from the thumbReplaceRegExPattern matched")]
        protected string thumbReplaceString;

        //TV Guide options
        [Category("OnlineVideosUserConfiguration"), Description("Whether to retrieve current program info for live streams.")]
        protected bool retrieveTVGuide = true;
        [Category("OnlineVideosConfiguration"), Description("The layout to use to display TV Guide info, possible wildcards are <nowtitle>,<nowdescription>,<nowstart>,<nowend>,<nexttitle>,<nextstart>,<nextend>,<newline>")]
        protected string tvGuideFormatString;

        #endregion

        #region Regex
        
        static readonly Regex urlVpidRegex = new Regex(@"/iplayer/(episodes?|brand)/([^/""]*)");
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
            string url = getHLSVideoUrls(video, vpid, proxyObj);
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

        string getHLSVideoUrls(VideoInfo video, string vpid, WebProxy proxyObj)
        {
            XmlDocument doc = GetWebData<XmlDocument>(string.Format(HLS_MEDIA_SELECTOR_URL, vpid), proxy: proxyObj); //uk only
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
            string playlistStr = GetWebData(video.VideoUrl, proxy: proxyObj, userAgent: HlsPlaylistParser.APPLE_USER_AGENT);
            HlsPlaylistParser playlist = new HlsPlaylistParser(playlistStr, video.VideoUrl);

            video.PlaybackOptions = new Dictionary<string, string>();
            if (playlist.StreamInfos.Count == 0)
            {
                video.PlaybackOptions.Add(video.Title, video.VideoUrl);
                return video.VideoUrl;
            }

            return populateHlsPlaybackOptions(video, playlist.StreamInfos);
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
                            cat.HasSubCategories = (cat as RssLink).Url != MOST_POPULAR_URL;
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
                    Thumb = getImageUrl(programme.SelectSingleNode(@".//picture/source")),
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
                var programmes = document.SelectNodes(@"//div[contains(@class, 'content-item')]");
                bool isAlternate = false;
                if (programmes == null)
                {
                    programmes = document.SelectNodes(@"//li[contains(@class, 'list-item--programme')]");
                    if (programmes == null)
                        break;
                    isAlternate = true;
                }

                foreach (var programme in programmes)
                {
                    RssLink category = isAlternate ? createProgrammeCategoryAlternate(programme) : createProgrammeCategory(programme);
                    if (category != null)
                    {
                        category.Thumb = getImageUrl(programme.SelectSingleNode(@".//source"));
                        category.ParentCategory = parentCategory;
                        categories.Add(category);
                    }
                }

                currentPage++;
                if (currentPage > pageCount)
                    break;

                document = GetWebData<HtmlDocument>(url + "?page=" + currentPage).DocumentNode;
            }
            return categories;
        }

        RssLink createProgrammeCategory(HtmlNode programme)
        {
            //If multiple episodes are available there should be an 'All Episodes' link
            var urlNode = programme.SelectSingleNode(@".//a[contains(@class, 'lnk')]");

            //If it's a single video we link directly to the video
            if (urlNode == null)
                urlNode = programme.SelectSingleNode(@"./a");

            if (urlNode == null)
                return null;

            string url = urlNode.GetAttributeValue("href", null);
            if (string.IsNullOrEmpty(url))
                return null;

            var titleNode = programme.SelectSingleNode(@".//div[contains(@class, 'content-item__title')]");
            if (titleNode == null)
                return null;

            return new RssLink()
            {
                Url = GetAbsoluteUri(url, BASE_URL).ToString(),
                Name = titleNode.GetCleanInnerText()
            };
        }

        RssLink createProgrammeCategoryAlternate(HtmlNode programme)
        {
            var titleNode = programme.SelectSingleNode(@".//h2[contains(@class, 'list-item__title')]");
            if (titleNode == null)
                return null;

            var episodesNode = programme.SelectSingleNode(@".//div[contains(@class, 'list-item__episodes-button')]//a");
            if (episodesNode == null)
                episodesNode = programme.SelectSingleNode(@".//a[contains(@class, 'list-item__main-link')]");
            if (episodesNode == null)
                return null;

            string url = episodesNode.GetAttributeValue("href", "");
            if (string.IsNullOrEmpty(url))
                return null;

            return new RssLink()
            {
                Url = GetAbsoluteUri(url, BASE_URL).ToString(),
                Name = titleNode.GetCleanInnerText()
            };
        }

        #endregion

        #region Videos

        public override List<VideoInfo> GetVideos(Category category)
        {
            if (category is Group)
                return getLiveVideoList((Group)category);

            if ((category as RssLink).Url == MOST_POPULAR_URL)
                return getMostPopularVideos(category);

            return getVideos(category);
        }

        List<VideoInfo> getVideos(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();

            string url = (category as RssLink).Url;

            var document = GetWebData<HtmlDocument>(url).DocumentNode;
            HtmlNodeCollection videoNodes = null;

            // Check for an 'all episdoes' link if we couldn't find any episodes 
            var allEpisodes = document.SelectSingleNode(@"//a[starts-with(@href, '/iplayer/episodes/')]");
            if (allEpisodes != null)
            {
                document = GetWebData<HtmlDocument>(BASE_URL + allEpisodes.GetAttributeValue("href", "")).DocumentNode;
                videoNodes = document.SelectNodes(@"//div[contains(@class, 'content-item--')]");
            }

            if (videoNodes == null)
                videoNodes = document.SelectNodes(@"//div[contains(@class, 'content-item--')]");

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
                videoNodes = document.SelectNodes(@"//div[contains(@class, 'content-item--')]");
                if (videoNodes == null)
                    break;
                videos.AddRange(videoNodes.Select(v => createVideo(v, category.Name)).Where(v => v != null));
            }
            return videos;
        }

        List<VideoInfo> getMostPopularVideos(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            string url = (category as RssLink).Url;
            string pageUrl = url;

            while (!string.IsNullOrEmpty(pageUrl))
            {
                HtmlDocument document = GetWebData<HtmlDocument>(pageUrl);
                var videoNodes = document.DocumentNode.SelectNodes(@"//div[contains(@class, 'content-item')]");

                foreach (var videoNode in videoNodes)
                {
                    VideoInfo video = createMostPopularVideo(videoNode);
                    if (video != null)
                        videos.Add(video);
                }
                pageUrl = getNextPageUrl(document, url);
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
                Thumb = getImageUrl(videoNode.SelectSingleNode(@".//source"))                
            };
        }

        VideoInfo createSingleVideo(HtmlNode videoNode, string url, string defaultTitle)
        {
            string title = videoNode.SelectSingleNode(@".//span[contains(@class, 'play-cta__text__title')]").GetCleanInnerText();
            if (title == defaultTitle)
            {
                string subtitle = videoNode.SelectSingleNode(@".//span[contains(@class, 'play-cta__text__subtitle')]").GetCleanInnerText();
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
                Thumb = getImageUrl(videoNode.SelectSingleNode(@".//source"))
            };
        }

        VideoInfo createMostPopularVideo(HtmlNode videoNode)
        {
            var urlNode = videoNode.SelectSingleNode(@".//a");
            if (urlNode == null)
                return null;

            string seriesTitle = videoNode.SelectSingleNode(@".//div[contains(@class, 'content-item__title')]").GetCleanInnerText();
            string episodeTitle = videoNode.SelectSingleNode(@".//div[contains(@class, 'content-item__info__primary')]").GetCleanInnerText();
            string title;
            if (!string.IsNullOrEmpty(seriesTitle))
                title = seriesTitle + (string.IsNullOrEmpty(episodeTitle) ? "" : ": " + episodeTitle);
            else
                title = string.IsNullOrEmpty(episodeTitle) ? seriesTitle : episodeTitle;

            return new VideoInfo()
            {
                VideoUrl = GetAbsoluteUri(urlNode.GetAttributeValue("href", ""), BASE_URL).ToString(),
                Title = title,
                Description = videoNode.SelectSingleNode(@".//div[contains(@class, 'content-item__info__secondary')]/div").GetCleanInnerText(),
                Length = videoNode.SelectSingleNode(@".//div[@class='content-item__sublabels']/span").GetCleanInnerText().Trim(),
                Thumb = getImageUrl(videoNode.SelectSingleNode(@".//source"))
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
        
        static string getNextPageUrl(HtmlDocument document, string originalUrl)
        {
            var nextPageNode = document.DocumentNode.SelectSingleNode(@"//span[contains(@class, 'next txt')]/a");
            if (nextPageNode != null)
                return BASE_URL + nextPageNode.GetAttributeValue("href", "").ParamsCleanup();

            nextPageNode = document.DocumentNode.SelectSingleNode(@"//li[contains(@class, 'pagination__item--next')]/a");
            if (nextPageNode != null)
                return originalUrl + nextPageNode.GetAttributeValue("href", "").ParamsCleanup();

            return null;
        }

        static int getPageCount(HtmlNode document)
        {
            var pageNumbers = document.SelectNodes(@"//li[contains(@class, 'pagination__number')]/a");
            if (pageNumbers == null)
                pageNumbers = document.SelectNodes(@"//li[contains(@class, 'pagination__item--page')]/a");

            int pageCount = 1;
            if (pageNumbers != null && pageNumbers.Count > 0)
            {
                string lastPageUrl = pageNumbers.Last().GetAttributeValue("href", "").ParamsCleanup();
                Match m = Regex.Match(lastPageUrl, @"\d+");
                if (m.Success)
                    pageCount = int.Parse(m.Value);
            }
            return pageCount;
        }

        static string getImageUrl(HtmlNode sourceNode)
        {
            if (sourceNode != null)
            {
                MatchCollection srcMatch = srcsetRegex.Matches(sourceNode.GetAttributeValue("srcset", ""));
                if (srcMatch.Count > 0)
                    return srcMatch[srcMatch.Count - 1].Value.Trim();
            }
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

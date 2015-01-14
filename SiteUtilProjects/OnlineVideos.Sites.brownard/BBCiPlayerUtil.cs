using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Xml;
using OnlineVideos.Sites.Brownard.Extensions;
using System.Net;

namespace OnlineVideos.Sites
{
    public class BBCiPlayerUtil : SiteUtilBase
    {
        #region Constants

        const string MOST_POPULAR_URL = "http://www.bbc.co.uk/iplayer/group/most-popular";
        const string ATOZ_URL = "http://www.bbc.co.uk/iplayer/a-z/";
        static readonly string[] atoz = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "0-9" };
        static readonly List<Category> aToZCategories = new List<Category>();

        #endregion

        #region Settings

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

        static readonly Regex seriesRegex = new Regex(@"<li class=""list-item[^""]*"" data-ip-id=""([^""]*)"">.*?<div class=""title top-title"">([^<]*)</div>.*?data-ip-src=""([^""]*)"">", RegexOptions.Singleline);
        static readonly Regex aToZRegex = new Regex(@"<li>\s*<a href=""/iplayer/brand/([^""]*)"".*?<span class=""title"">([^<]*)", RegexOptions.Singleline);
        static readonly Regex nextPageRegex = new Regex(@"<span class=""next txt"">\s*<a href=""([^""]*)"">\s*Next");

        static readonly Regex episodeInfoRegex = new Regex(@"<li class=""list-item[^>]*>(.*?)</li>", RegexOptions.Singleline);
        static readonly Regex episodeUrlRegex = new Regex(@"href=""([^""]*)");
        static readonly Regex episodeImageRegex = new Regex(@"data-ip-src=""([^""]*)");
        static readonly Regex episodeTitleRegex = new Regex(@"class=""subtitle"">([^<]*)");
        static readonly Regex episodeParentTitleRegex = new Regex(@"class=""title"">([^<]*)");
        static readonly Regex episodeDescriptionRegex = new Regex(@"class=""synopsis"">([^<]*)");
        static readonly Regex episodeAirDateRegex = new Regex(@"class=""release"">([^<]*)");
        static readonly Regex episodeDurationRegex = new Regex(@"Duration\s*</span>([^<]*)");

        static readonly Regex videoPidRegex = new Regex(@"vpid"":""([^""]*)");

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

        public override string getUrl(VideoInfo video)
        {
            if (video.Other == "livestream")
                return getLiveUrls(video);

            WebProxy proxyObj = getProxy();
            Match m = videoPidRegex.Match(GetWebData(video.VideoUrl, null, null, proxyObj));
            if (!m.Success)
            {
                Log.Warn("BBCiPlayer: Failed to parse vpid from '{0}'", video.VideoUrl);
                return null;
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(GetWebData("http://www.bbc.co.uk/mediaselector/4/mtis/stream/" + m.Groups[1].Value, null, null, proxyObj)); //uk only
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

            SortedList<string, string> sortedPlaybackOptions = new SortedList<string, string>(new QualityComparer());
            foreach (XmlElement mediaElem in doc.SelectNodes("//ns1:media[@kind='video']", nsmRequest))
            {
                string info = "";
                string resultUrl = "";
                foreach (XmlElement connectionElem in mediaElem.SelectNodes("ns1:connection", nsmRequest))
                {
                    if (Array.BinarySearch<string>(new string[] { "akamai", "level3", "limelight" }, connectionElem.Attributes["kind"].Value) >= 0)
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

                        info = string.Format("{0}x{1} | {2} kbps | {3}", mediaElem.GetAttribute("width"), mediaElem.GetAttribute("height"), mediaElem.GetAttribute("bitrate"), connectionElem.Attributes["kind"].Value);
                        resultUrl = "";
                        if (connectionElem.Attributes["kind"].Value == "limelight")
                        {
                            resultUrl = new MPUrlSourceFilter.RtmpUrl(string.Format("rtmp://{0}:1935/{1}", server, application + "?" + auth), server, 1935)
                            {
                                App = application + "?" + auth,
                                PlayPath = identifier,
                                SwfUrl = SWFPlayer,
                                SwfVerify = true,
                            }.ToString();
                        }
                        else if (connectionElem.Attributes["kind"].Value == "level3")
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
                        else if (connectionElem.Attributes["kind"].Value == "akamai")
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

            if (sortedPlaybackOptions.Count == 0)
            {
                var errorNodes = doc.SelectNodes("//ns1:error", nsmRequest);
                if (errorNodes.Count > 0) throw new OnlineVideosException(string.Format("BBC says: {0}", ((XmlElement)errorNodes[0]).GetAttribute("id")));
            }

            string lastUrl = "";
            video.PlaybackOptions = new Dictionary<string, string>();
            var enumer = sortedPlaybackOptions.GetEnumerator();
            while (enumer.MoveNext())
            {
                if (lastUrl == "" || !enumer.Current.Key.Contains("2800"))
                    lastUrl = enumer.Current.Value;
                video.PlaybackOptions.Add(enumer.Current.Key, enumer.Current.Value);
            }
            return lastUrl;
        }

        string getLiveUrls(VideoInfo video)
        {
            WebProxy proxyObj = getProxy();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(GetWebData("http://www.bbc.co.uk/mediaselector/playlists/hds/pc/ak/" + video.VideoUrl, null, null, proxyObj));
            SortedList<string, string> sortedPlaybackOptions = new SortedList<string, string>(new QualityComparer());
            foreach (XmlElement mediaElem in doc.GetElementsByTagName("media"))
            {
                string url = null;
                if (mediaElem.Attributes["href"] != null)
                    url = mediaElem.Attributes["href"].Value + "?live=true";
                string bitrate = "";
                if (mediaElem.Attributes["bitrate"] != null)
                    bitrate = mediaElem.Attributes["bitrate"].Value;
                if (!string.IsNullOrEmpty(url))
                    sortedPlaybackOptions.Add(bitrate + " kbps", url);
            }

            string lastUrl = "";
            video.PlaybackOptions = new Dictionary<string, string>();
            var enumer = sortedPlaybackOptions.GetEnumerator();
            while (enumer.MoveNext())
            {
                lastUrl = enumer.Current.Value;
                video.PlaybackOptions.Add(enumer.Current.Key, enumer.Current.Value);
            }
            return lastUrl; //"http://bbcfmhds.vo.llnwd.net/hds-live/livepkgr/_definst_/bbc1/bbc1_1500.f4m";
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
            if (url.StartsWith(ATOZ_URL))
            {
                parentCategory.SubCategories = new List<Category>();
                string html = GetWebData(url);
                foreach (Match m in aToZRegex.Matches(html))
                {
                    parentCategory.SubCategories.Add(new RssLink()
                    {
                        Url = "http://www.bbc.co.uk/iplayer/episodes/" + m.Groups[1].Value,
                        Name = m.Groups[2].Value.HtmlCleanup(),
                        Thumb = defaultThumb,
                        ParentCategory = parentCategory
                    });
                }
            }
            else
            {
                parentCategory.SubCategories = discoverSubCategoriesLocal(parentCategory, url);
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        List<Category> discoverSubCategoriesLocal(Category parentCategory, string url)
        {
            List<Category> cats = new List<Category>();
            while (true)
            {
                string html = GetWebData(url);
                foreach (Match m in seriesRegex.Matches(html))
                {
                    cats.Add(new RssLink()
                    {
                        Url = "http://www.bbc.co.uk/iplayer/episodes/" + m.Groups[1].Value,
                        Name = m.Groups[2].Value.HtmlCleanup(),
                        Thumb = m.Groups[3].Value,
                        ParentCategory = parentCategory
                    });
                }

                Match nextPage = nextPageRegex.Match(html);
                if (!nextPage.Success)
                    break;
                url = "http://www.bbc.co.uk" + nextPage.Groups[1].Value.Replace("&amp;", "&");
            }
            return cats;
        }

        #endregion

        #region Videos

        public override List<VideoInfo> getVideoList(Category category)
        {
            if (category is Group)
                return getLiveVideoList((Group)category);

            List<VideoInfo> videos = new List<VideoInfo>();
            string url = (category as RssLink).Url;
            bool isMostPopular = url == MOST_POPULAR_URL;
            while (true)
            {
                string html = GetWebData(url);
                foreach (Match m in episodeInfoRegex.Matches(html))
                {
                    VideoInfo video = new VideoInfo();
                    string episodeHtml = m.Groups[1].Value;

                    string series = episodeParentTitleRegex.Match(episodeHtml).Groups[1].Value.HtmlCleanup();
                    Match n = episodeTitleRegex.Match(episodeHtml);
                    string episode = n.Success ? n.Groups[1].Value.HtmlCleanup() : null;
                    video.Other = new TrackingDetails() { SeriesTitle = series, EpisodeTitle = episode };

                    if (isMostPopular)
                    {
                        //Most Popular category jumps straight to videos so need to include series title
                        video.Title = series;
                        if (!string.IsNullOrEmpty(episode))
                            video.Title += ": " + episode;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(episode))
                            video.Title = episode;
                        else
                            video.Title = series;
                    }

                    if ((n = episodeUrlRegex.Match(episodeHtml)).Success)
                        video.VideoUrl = "http://www.bbc.co.uk" + n.Groups[1].Value;
                    if ((n = episodeDescriptionRegex.Match(episodeHtml)).Success)
                        video.Description = n.Groups[1].Value.HtmlCleanup();
                    if ((n = episodeAirDateRegex.Match(episodeHtml)).Success)
                        video.Airdate = n.Groups[1].Value.Replace("First shown:", "").HtmlCleanup();
                    if ((n = episodeDurationRegex.Match(episodeHtml)).Success)
                        video.Length = n.Groups[1].Value.HtmlCleanup();

                    if ((n = episodeImageRegex.Match(episodeHtml)).Success)
                    {
                        video.ImageUrl = n.Groups[1].Value;
                        if (!string.IsNullOrEmpty(thumbReplaceRegExPattern))
                            video.ImageUrl = Regex.Replace(video.ImageUrl, thumbReplaceRegExPattern, thumbReplaceString);
                    }
                    videos.Add(video);
                }

                Match nextPage = nextPageRegex.Match(html);
                if (!nextPage.Success)
                    break;
                url = "http://www.bbc.co.uk" + nextPage.Groups[1].Value;
            }
            return videos;
        }

        List<VideoInfo> getLiveVideoList(Group category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            foreach (Channel channel in category.Channels)
            {
                VideoInfo video = new VideoInfo();
                video.Title = channel.StreamName;

                int argIndex = channel.Url.IndexOf('?');
                if (argIndex < 0) video.VideoUrl = channel.Url;
                else video.VideoUrl = channel.Url.Remove(argIndex);

                video.Other = "livestream";
                video.ImageUrl = channel.Thumb;
                if (retrieveTVGuide && argIndex > -1)
                {
                    Utils.TVGuideGrabber tvGuide = new Utils.TVGuideGrabber();
                    if (tvGuide.GetNowNextForChannel(channel.Url))
                        video.Description = tvGuide.FormatTVGuide(tvGuideFormatString);
                }
                videos.Add(video);
            }
            return videos;
        }

        #endregion

        #region Search

        public override List<ISearchResultItem> DoSearch(string query)
        {
            List<ISearchResultItem> results = new List<ISearchResultItem>();
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
    }

    class QualityComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            int x_kbps = 0;
            if (!int.TryParse(Regex.Match(x, @"(\d+) kbps").Groups[1].Value, out x_kbps)) return 1;
            int y_kbps = 0;
            if (!int.TryParse(Regex.Match(y, @"(\d+) kbps").Groups[1].Value, out y_kbps)) return -1;

            int compare = x_kbps.CompareTo(y_kbps);
            if (compare == 0) //if bitrates same, sort alphabetically
                compare = x.CompareTo(y);
            return compare;
        }
    }

    class TrackingDetails
    {
        public string SeriesTitle { get; set; }
        public string EpisodeTitle { get; set; }
    }
}


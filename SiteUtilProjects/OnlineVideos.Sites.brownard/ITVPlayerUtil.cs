using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Xml;
using OnlineVideos.Sites.Utils;
using System.Web;

namespace OnlineVideos.Sites
{
    public class ITVPlayerUtil : SiteUtilBase
    {
        #region Site Config
        [Category("OnlineVideosUserConfiguration"), Description("Select stream automatically?")]
        protected bool AutoSelectStream = false;
        [Category("OnlineVideosUserConfiguration"), Description("Proxy to use for WebRequests (must be in the UK). Define like this: 83.84.85.86:8116")]
        string proxy = null;
        [Category("OnlineVideosUserConfiguration"), Description("If your proxy requires a username, set it here.")]
        string proxyUsername = null;
        [Category("OnlineVideosUserConfiguration"), Description("If your proxy requires a password, set it here.")]
        string proxyPassword = null;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used on a video thumbnail for matching a string to be replaced for higher quality")]
        protected string thumbReplaceRegExPattern;
        [Category("OnlineVideosConfiguration"), Description("The string used to replace the match if the pattern from the thumbReplaceRegExPattern matched")]
        protected string thumbReplaceString;
        [Category("OnlineVideosUserConfiguration"), Description("Whether to download subtitles")]
        protected bool RetrieveSubtitles = false;
        [Category("OnlineVideosUserConfiguration"), Description("Whether to retrieve current program info for live streams.")]
        protected bool retrieveTVGuide = true;
        [Category("OnlineVideosConfiguration"), Description("The layout to use to display TV Guide info, possible wildcards are <nowtitle>,<nowdescription>,<nowstart>,<nowend>,<nexttitle>,<nextstart>,<nextend>,<newline>")]
        protected string tvGuideFormatString;// = "Now: <nowtitle> - <nowstart> - <nowend><newline>Next: <nexttitle> - <nextstart> - <nextend><newline><nowdescription>";        
        #endregion

        #region Consts
        const string SOAP_TEMPLATE = @"<?xml version='1.0' encoding='utf-8'?>
<SOAP-ENV:Envelope xmlns:SOAP-ENV='http://schemas.xmlsoap.org/soap/envelope/' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'>
    <SOAP-ENV:Body>
	    <tem:GetPlaylist xmlns:tem='http://tempuri.org/' xmlns:itv='http://schemas.datacontract.org/2004/07/Itv.BB.Mercury.Common.Types' xmlns:com='http://schemas.itv.com/2009/05/Common'>
	        <tem:request>
                {0}
		        <itv:RequestGuid>FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF</itv:RequestGuid>
		        <itv:Vodcrid>
		            <com:Id>{1}</com:Id>
		            <com:Partition>itv.com</com:Partition>
		        </itv:Vodcrid>
	        </tem:request>
	        <tem:userInfo>
		        <itv:GeoLocationToken>
		            <itv:Token/>
		        </itv:GeoLocationToken>
		        <itv:RevenueScienceValue>scc=true; svisit=1; sc4=Other</itv:RevenueScienceValue>
	        </tem:userInfo>
	        <tem:siteInfo>
		        <itv:Area>ITVPLAYER.VIDEO</itv:Area>
		        <itv:Platform>DotCom</itv:Platform>
		        <itv:Site>ItvCom</itv:Site>
	        </tem:siteInfo>
            <tem:deviceInfo> 
                <itv:ScreenSize>Big</itv:ScreenSize> 
            </tem:deviceInfo>
	    </tem:GetPlaylist>
	</SOAP-ENV:Body>
</SOAP-ENV:Envelope>";
        #endregion

        #region Regex
        //Shows        
        static readonly Regex noEpisodesRegex = new Regex(@"No episodes available");
        static readonly Regex showRegex = new Regex(@"<a href=""([^""]*)""[^>]*?data-content-type=""programme""(.*?)</a>", RegexOptions.Singleline);
        static readonly Regex showCountRegex = new Regex(@"<p[^>]*>\s*(\d+)[^<]*</p>");

        static readonly Regex showsVideoRegex = new Regex(@"<a href=""([^""]*)""[^>]*?data-content-type=""episode""(.*?)</a>", RegexOptions.Singleline);
        static readonly Regex showsVideoTimeRegex = new Regex(@"<time[^>]*>(.*?)</time>", RegexOptions.Singleline);
        static readonly Regex showsVideoSummaryRegex = new Regex(@"<p [^>]*>(.*?)</p>", RegexOptions.Singleline);

        static readonly Regex titleRegex = new Regex(@"<h3[^>]*>([^<]*)</h3>");
        static readonly Regex imageRegex = new Regex(@"<source srcset=""([^""]*)""");

        static readonly Regex singleVideoTitleRegex = new Regex(@"<h1 id=""programme-title""[^>]*>(.*?)</h1>");
        static readonly Regex singleVideoEpisodeInfoRegex = new Regex(@"<h2 class=""episode-info__episode-title"">(.*?)</h2>", RegexOptions.Singleline);
        static readonly Regex singleVideoSummaryRegex = new Regex(@"<p class=""episode-info__synopsis theme__subtle"">(.*?)</p>", RegexOptions.Singleline);
        static readonly Regex singleVideoTimeRegex = new Regex(@"<time[^>]*><span[^>]*>[^<]*</span><span[^>]*>(.*?)</time>");
        static readonly Regex singleVideoImageRegex = new Regex(@"background-image: url\('(.*?)'\)");

        //Search
        static readonly Regex searchRegex = new Regex(@"<div class=""search-wrapper"">.*?<div class=""search-result-image"">[\s\n]*(<a.*?><img.*?src=""(.*?)"")?.*?<h4 class=""programme-title""><a href=""(.*?)"">(.*?)</a>.*?<div class=""programme-description"">[\s\n]*(.*?)</div>", RegexOptions.Singleline);
        //ProductionId
        static readonly Regex productionIdRegex = new Regex(@"data-video-id=""(.*?)""");
        #endregion

        #region SiteUtil Overrides
        DateTime lastRefresh = DateTime.MinValue;
        public override int DiscoverDynamicCategories()
        {
            if ((DateTime.Now - lastRefresh).TotalMinutes > 15)
            {
                foreach (Category cat in Settings.Categories)
                {
                    if (cat is RssLink)
                    {
                        cat.HasSubCategories = true;
                        cat.SubCategoriesDiscovered = false;
                    }
                    if (string.IsNullOrEmpty(cat.Thumb))
                        cat.Thumb = string.Format(@"{0}\Icons\{1}.png", OnlineVideoSettings.Instance.ThumbsDir, Settings.Name);
                }
                lastRefresh = DateTime.Now;
            }
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            return getShowsList(parentCategory);
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            Group group = category as Group;
            if (group != null)
                return getLiveStreams(group);
            return getShowsVids(category);
        }

        public override List<string> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            string url = GetVideoUrl(video);
            if (inPlaylist)
                video.PlaybackOptions.Clear();
            return new List<string>() { url };
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            bool isLiveStream = video.VideoUrl.StartsWith("sim");
            string url = isLiveStream ? video.VideoUrl : getProductionId(video.VideoUrl);
            return populateUrlsFromXml(video, getPlaylistDocument(url, !isLiveStream), isLiveStream);
        }
        #endregion

        #region Search
        public override bool CanSearch
        {
            get
            {
                return false;
            }
        }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            string html = GetWebData(string.Format("http://www.itv.com/itvplayer/search/term/{0}/catch-up", query));
            List<SearchResultItem> cats = new List<SearchResultItem>();
            foreach (Match match in searchRegex.Matches(html))
            {
                RssLink cat = new RssLink();
                cat.Thumb = match.Groups[2].Value;
                cat.Url = match.Groups[3].Value;
                cat.Name = cleanString(match.Groups[4].Value);
                cat.Description = cleanString(match.Groups[5].Value);
                cats.Add(cat);
            }

            return cats;
        }
        #endregion

        #region Shows
        int getShowsList(Category parentCategory)
        {
            string html = GetWebData((parentCategory as RssLink).Url);
            List<Category> subCats = new List<Category>();
            foreach (Match match in showRegex.Matches(html))
            {
                string showHtml = match.Groups[2].Value;
                Match m;
                if ((m = noEpisodesRegex.Match(showHtml)).Success)
                    continue;

                RssLink cat = new RssLink();
                cat.ParentCategory = parentCategory;
                cat.Url = match.Groups[1].Value;

                if ((m = titleRegex.Match(showHtml)).Success)
                    cat.Name = cleanString(m.Groups[1].Value);
                if ((m = imageRegex.Match(showHtml)).Success)
                {
                    string thumb = HttpUtility.HtmlDecode(m.Groups[1].Value);
                    if (!string.IsNullOrEmpty(thumbReplaceRegExPattern))
                        thumb = Regex.Replace(thumb, thumbReplaceRegExPattern, thumbReplaceString);
                    cat.Thumb = thumb;
                }
                if ((m = showCountRegex.Match(showHtml)).Success)
                    cat.EstimatedVideoCount = uint.Parse(m.Groups[1].Value);
                subCats.Add(cat);
            }
            parentCategory.SubCategories = subCats;
            parentCategory.SubCategoriesDiscovered = true;
            return subCats.Count;
        }

        List<VideoInfo> getShowsVids(Category category)
        {
            string html = GetWebData((category as RssLink).Url);
            List<VideoInfo> vids = new List<VideoInfo>();
            foreach (Match match in showsVideoRegex.Matches(html))
            {
                VideoInfo vid = new VideoInfo();
                vid.VideoUrl = match.Groups[1].Value;

                string videoHtml = match.Groups[2].Value;
                Match m;
                if ((m = titleRegex.Match(videoHtml)).Success)
                    vid.Title = cleanString(m.Groups[1].Value);
                if ((m = imageRegex.Match(html)).Success)
                {
                    string thumb = HttpUtility.HtmlDecode(m.Groups[1].Value);
                    if (!string.IsNullOrEmpty(thumbReplaceRegExPattern))
                        thumb = Regex.Replace(thumb, thumbReplaceRegExPattern, thumbReplaceString);
                    vid.Thumb = thumb;
                }
                if ((m = showsVideoTimeRegex.Match(videoHtml)).Success)
                    vid.Airdate = cleanString(m.Groups[1].Value);
                if ((m = showsVideoSummaryRegex.Match(videoHtml)).Success)
                    vid.Description = cleanString(m.Groups[1].Value);
                vids.Add(vid);
            }

            if (vids.Count < 1)
            {
                //Single episode
                VideoInfo vid = new VideoInfo();
                Match m;
                if ((m = singleVideoTitleRegex.Match(html)).Success)
                    vid.Title = cleanString(m.Groups[1].Value);
                if ((m = singleVideoImageRegex.Match(html)).Success)
                {
                    string thumb = m.Groups[1].Value;
                    if (!string.IsNullOrEmpty(thumbReplaceRegExPattern))
                        thumb = Regex.Replace(thumb, thumbReplaceRegExPattern, thumbReplaceString);
                    vid.Thumb = thumb;
                }
                if ((m = singleVideoTimeRegex.Match(html)).Success)
                    vid.Airdate = cleanString(m.Groups[1].Value);
                string description = "";
                if ((m = singleVideoEpisodeInfoRegex.Match(html)).Success)
                    description = string.Format("{0} - ", cleanString(m.Groups[1].Value));
                if ((m = singleVideoSummaryRegex.Match(html)).Success)
                    description += cleanString(m.Groups[1].Value);
                vid.Description = description;
                vid.VideoUrl = (category as RssLink).Url;
                vids.Add(vid);
            }

            return vids;
        }
        #endregion

        #region Live Streams
        List<VideoInfo> getLiveStreams(Group group)
        {
            List<VideoInfo> vids = new List<VideoInfo>();
            foreach (Channel channel in group.Channels)
            {
                VideoInfo video = new VideoInfo();
                video.Title = channel.StreamName;
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
                vids.Add(video);
            }
            return vids;
        }
        #endregion

        #region Playlist Methods

        string populateUrlsFromXml(VideoInfo video, XmlDocument streamPlaylist, bool live)
        {
            if (streamPlaylist == null)
            {
                Log.Warn("ITVPlayer: Stream playlist is null");
                return "";
            }

            XmlNode videoEntry = streamPlaylist.SelectSingleNode("//VideoEntries/Video");
            if (videoEntry == null)
            {
                Log.Warn("ITVPlayer: Could not find video entry");
                return "";
            }

            XmlNode node;
            node = videoEntry.SelectSingleNode("./MediaFiles");
            if (node == null || node.Attributes["base"] == null)
            {
                Log.Warn("ITVPlayer: Could not find base url");
                return "";
            }

            string rtmpUrl = node.Attributes["base"].Value;
            SortedList<string, string> options = new SortedList<string, string>(new StreamComparer());
            foreach (XmlNode mediaFile in node.SelectNodes("./MediaFile"))
            {
                if (mediaFile.Attributes["delivery"] == null || mediaFile.Attributes["delivery"].Value != "Streaming")
                    continue;

                string title = "";
                if (mediaFile.Attributes["bitrate"] != null)
                {
                    title = mediaFile.Attributes["bitrate"].Value;
                    int bitrate;
                    if (int.TryParse(title, out bitrate))
                        title = string.Format("{0} kbps", bitrate / 1000);
                }

                if (!options.ContainsKey(title))
                {
                    string url = new MPUrlSourceFilter.RtmpUrl(rtmpUrl)
                    {
                        PlayPath = mediaFile.InnerText,
                        SwfUrl = "http://mediaplayer.itv.com/2.18.5%2Bbuild.ad408a9c67/ITVMediaPlayer.swf",
                        SwfVerify = true,
                        Live = live
                    }.ToString();
                    options.Add(title, url);
                }
            }

            if (RetrieveSubtitles)
            {
                node = videoEntry.SelectSingleNode("./ClosedCaptioningURIs");
                if (node != null && Helpers.UriUtils.IsValidUri(node.InnerText))
                    video.SubtitleText = SubtitleReader.TimedText2SRT(GetWebData(node.InnerText));
            }

            video.PlaybackOptions = new Dictionary<string, string>();
            if (options.Count == 0)
                return null;

            if (AutoSelectStream)
            {
                var last = options.Last();
                video.PlaybackOptions.Add(last.Key, last.Value);
            }
            else
            {
                foreach (KeyValuePair<string, string> key in options)
                    video.PlaybackOptions.Add(key.Key, key.Value);
            }
            return options.Last().Value;
        }

        string getProductionId(string url)
        {
            string html = GetWebData(url);
            Match m = productionIdRegex.Match(html);
            string productionId;
            if (m.Success)
            {
                productionId = m.Groups[1].Value.Replace("\\", "");
                Log.Debug("ITVPlayer: Found production id '{0}'", productionId);
            }
            else
            {
                productionId = "";
                Log.Warn("ITVPlayer: Failed to get production id");
            }
            return productionId;
        }

        string getPlaylist(string id, bool isProductionId = false)
        {
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            if (isProductionId)
                doc.InnerXml = string.Format(SOAP_TEMPLATE, "<itv:ProductionId>" + id + "</itv:ProductionId>", "");
            else
                doc.InnerXml = string.Format(SOAP_TEMPLATE, "", id);

            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://mercury.itv.com/PlaylistService.svc");
                req.Headers.Add("SOAPAction", "http://tempuri.org/PlaylistService/GetPlaylist");
                req.Referer = "http://www.itv.com/mediaplayer/ITVMediaPlayer.swf?v=12.18.4/[[DYNAMIC]]/2";
                req.ContentType = "text/xml;charset=\"utf-8\"";
                req.Accept = "text/xml";
                req.Method = "POST";

                WebProxy proxy = getProxy();
                if (proxy != null)
                    req.Proxy = proxy;

                Stream stream;
                using (stream = req.GetRequestStream())
                    doc.Save(stream);

                using (stream = req.GetResponse().GetResponseStream())
                using (StreamReader sr = new StreamReader(stream))
                {
                    string responseXml = sr.ReadToEnd();
                    Log.Debug("ITVPlayer: Playlist response:\r\n\t {0}", responseXml);
                    return responseXml;
                }
            }
            catch (Exception ex)
            {
                Log.Warn("ITVPlayer: Failed to get playlist - {0}\r\n{1}", ex.Message, ex.StackTrace);
                return null;
            }
        }

        XmlDocument getPlaylistDocument(string id, bool isProductionId = false)
        {
            string xml = getPlaylist(id, isProductionId);
            if (!string.IsNullOrEmpty(xml))
            {
                XmlDocument document = new XmlDocument();
                try
                {
                    document.LoadXml(xml);
                    return document;
                }
                catch (Exception ex)
                {
                    Log.Warn("ITVPlayer: Failed to load plalist xml '{0}' - {1}\r\n{2}", xml, ex.Message, ex.StackTrace);
                }
            }
            return null;
        }
        #endregion

        #region Utils
        static string cleanString(string s)
        {
            s = stripTags(s);
            s = s.Replace("&amp;", "&").Replace("&pound;", "£").Replace("&#039;", "'");
            return Regex.Replace(s, @"\s\s+", " ").Trim();
        }

        static string stripTags(string s)
        {
            return Regex.Replace(s, "<[^>]*>", "");
        }

        System.Net.WebProxy getProxy()
        {
            System.Net.WebProxy proxyObj = null;
            if (!string.IsNullOrEmpty(proxy))
            {
                proxyObj = new System.Net.WebProxy(proxy);
                if (!string.IsNullOrEmpty(proxyUsername) && !string.IsNullOrEmpty(proxyPassword))
                    proxyObj.Credentials = new System.Net.NetworkCredential(proxyUsername, proxyPassword);
            }
            return proxyObj;
        }
        #endregion
    }
}
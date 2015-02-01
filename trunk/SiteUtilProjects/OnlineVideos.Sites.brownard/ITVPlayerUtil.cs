using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class ITVPlayerUtil : SiteUtilBase
    {
        #region Category Type
        enum CategoryType
        {
            AtoZ,
            Default
        }
        #endregion

        #region Site Config
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
        [Category("OnlineVideosUserConfiguration"), Description("Select stream automatically?")]
        protected bool AutoSelectStream = false;
        [Category("OnlineVideosUserConfiguration"), Description("Stream quality preference\r\n1 is low, 5 high")]
        protected int StreamQualityPref = 5;
        [Category("OnlineVideosUserConfiguration"), Description("Whether to retrieve current program info for live streams.")]
        protected bool retrieveTVGuide = true;
        [Category("OnlineVideosConfiguration"), Description("The layout to use to display TV Guide info, possible wildcards are <nowtitle>,<nowdescription>,<nowstart>,<nowend>,<nexttitle>,<nextstart>,<nextend>,<newline>")]
        protected string tvGuideFormatString;// = "Now: <nowtitle> - <nowstart> - <nowend><newline>Next: <nexttitle> - <nextstart> - <nextend><newline><nowdescription>";        
        #endregion

        #region Consts
        const string BASE_URL = "http://www.itv.com";
        
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
        static readonly Regex showsRegex = new Regex(@"<img src='(.*?)'>[\s\n]*</a>[\s\n]*<div.*?>[\s\n]*<a href=""(.*?)"">(.*?)</a>[\s\n]*.*?[\s\n]*<div.*>[\s\n]*<span.*?>(\d+)");
        static readonly Regex showsVideoRegex = new Regex(@"<a href=""(.*?)""><img.*?src=""(.*?)"".*?title=""(.*?)"".*[\s\n]*<a.*?>[\s\n]*<div.*?>[\s\n]*.*?<span.*?>(.*?)</span>.*?<br />[\s\n]*<div.*?>Series (<div.*?>){3}(.*?)</div>.*[\s\n]*<div.*?>Episode (<div.*?>){3}(.*?)</div>.*[\s\n]*</div>[\s\n]*(<div.*?>){3}(.*?)</div>.*?(<div.*?>){3}(.*?)</div>");
        static readonly Regex singleShowsVideoRegex = new Regex(@"<h1 class=""title episode-title"".*?>[\s\n]*(.*?)<span.*?>Catch up</span>.*[\s\n]*(<div.*?>){3}<span.*?>(.*?)</span>.*?<div.*?>.*?</div>(<div.*?>){2}(.*?)</div>(.|\n)*?name=""poster"" value=""(.*?)""(.|\n)*?<div class=""description"">[\s\n]*(.*?)</div>");
        //AtoZ
        static readonly Regex atozVideoRegex = new Regex(@"<div class=""listItem.*?<a href=""http://.*?&amp;Filter=(\d+).*?<img.*? src=""([^""]*)"".*?<a.*?>([^<]*).*?<p class=""date"">([^<]*).*?<p class=""progDesc"">([^<]*).*?<li>\s*Duration:([^<]*)", RegexOptions.Singleline);
        //Search
        static readonly Regex searchRegex = new Regex(@"<div class=""search-wrapper"">.*?<div class=""search-result-image"">[\s\n]*(<a.*?><img.*?src=""(.*?)"")?.*?<h4 class=""programme-title""><a href=""(.*?)"">(.*?)</a>.*?<div class=""programme-description"">[\s\n]*(.*?)</div>", RegexOptions.Singleline);
        //ProductionId
        static readonly Regex productionIdRegex = new Regex(@"""productionId"":""(.*?)""");
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
            if ((parentCategory as RssLink).Url.StartsWith("http://www.itv.com/_data"))
                return getAtoZList(parentCategory);
            return getShowsList(parentCategory);
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            Group group = category as Group;
            if (group != null)
                return getLiveStreams(group);

            switch (category.Other as CategoryType?)
            {
                case CategoryType.AtoZ:
                    return getAtoZVids(category);
                default:
                    return getShowsVids(category);
            }
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            //Direct stream
            if (video.VideoUrl.StartsWith("http://") || video.VideoUrl.StartsWith("rtmp://") || video.VideoUrl.StartsWith("rtmpe://"))
                return video.VideoUrl;

            bool isProductionId = false;
            bool isLiveStream = video.VideoUrl.StartsWith("sim");
            if (!isLiveStream && video.VideoUrl.StartsWith("/itvplayer"))
            {
                isProductionId = true;
                video.VideoUrl = getProductionId(video.VideoUrl);
            }
            return populateUrlsFromXml(video, getPlaylistDocument(video.VideoUrl, isProductionId), isLiveStream);
        }
        #endregion

        #region Search
        public override bool CanSearch
        {
            get
            {
                return true;
            }
        }

        public override List<ISearchResultItem> Search(string query, string category = null)
        {
            string html = GetWebData(string.Format("http://www.itv.com/itvplayer/search/term/{0}/catch-up", query));
            List<ISearchResultItem> cats = new List<ISearchResultItem>();
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
            foreach (Match match in showsRegex.Matches(html))
            {
                RssLink cat = new RssLink();
                cat.ParentCategory = parentCategory;
                cat.Url = match.Groups[2].Value;
                cat.Name = cleanString(match.Groups[3].Value);
                string thumb = match.Groups[1].Value;
                if (!string.IsNullOrEmpty(thumbReplaceRegExPattern))
                    thumb = Regex.Replace(thumb, thumbReplaceRegExPattern, thumbReplaceString);
                cat.Thumb = thumb;
                cat.EstimatedVideoCount = uint.Parse(match.Groups[4].Value);
                cat.Other = CategoryType.Default;
                subCats.Add(cat);
            }
            parentCategory.SubCategories = subCats;
            parentCategory.SubCategoriesDiscovered = true;
            return subCats.Count;
        }

        List<VideoInfo> getShowsVids(Category category)
        {
            string html = GetWebData(BASE_URL + (category as RssLink).Url);
            List<VideoInfo> vids = new List<VideoInfo>();
            foreach (Match match in showsVideoRegex.Matches(html))
            {
                VideoInfo vid = new VideoInfo();
                vid.VideoUrl = match.Groups[1].Value;
                string thumb = match.Groups[2].Value;
                if (!string.IsNullOrEmpty(thumbReplaceRegExPattern))
                    thumb = Regex.Replace(thumb, thumbReplaceRegExPattern, thumbReplaceString);
                vid.ImageUrl = thumb;
                vid.Title = cleanString(match.Groups[3].Value);
                vid.Airdate = match.Groups[4].Value;
                vid.Description = cleanString(match.Groups[10].Value);
                vid.Length = match.Groups[12].Value;
                vids.Add(vid);
            }

            if (vids.Count < 1)
            {
                //Single episode
                Match match = singleShowsVideoRegex.Match(html);
                if (match.Success)
                {
                    VideoInfo vid = new VideoInfo();
                    vid.Title = cleanString(match.Groups[1].Value);
                    vid.Airdate = match.Groups[3].Value;
                    vid.Length = match.Groups[5].Value;
                    vid.ImageUrl = match.Groups[7].Value;
                    vid.Description = cleanString(match.Groups[9].Value);
                    vid.VideoUrl = (category as RssLink).Url;
                    vids.Add(vid);
                }
            }

            return vids;
        }
        #endregion

        #region AtoZ
        int getAtoZList(Category parentCategory)
        {
            XmlDocument doc = GetWebData<XmlDocument>((parentCategory as RssLink).Url);
            List<Category> subCats = new List<Category>();
            foreach (XmlNode node in doc.SelectNodes("//ITVCatchUpProgramme"))
            {
                RssLink cat = new RssLink();
                cat.ParentCategory = parentCategory;
                cat.Url = node.SelectSingleNode("ProgrammeId").InnerText;
                cat.Name = cleanString(node.SelectSingleNode("ProgrammeTitle").InnerText);
                cat.Thumb = node.SelectSingleNode("ProgrammeMediaUrl").InnerText;
                cat.Other = CategoryType.AtoZ;
                subCats.Add(cat);
            }
            parentCategory.SubCategories = subCats;
            parentCategory.SubCategoriesDiscovered = true;
            return subCats.Count;
        }

        List<VideoInfo> getAtoZVids(Category category)
        {
            string html = GetWebData(string.Format("http://www.itv.com/_app/Dynamic/CatchUpData.ashx?ViewType=1&Filter={0}&moduleID=115107", (category as RssLink).Url));
            List<VideoInfo> vids = new List<VideoInfo>();
            foreach (Match match in atozVideoRegex.Matches(html))
            {
                VideoInfo vid = new VideoInfo();
                vid.VideoUrl = match.Groups[1].Value;
                vid.ImageUrl = match.Groups[2].Value;
                vid.Length = match.Groups[6].Value.Trim();

                if (cleanString(match.Groups[3].Value) == category.Name && !string.IsNullOrEmpty(match.Groups[4].Value))
                {
                    vid.Title = cleanString(match.Groups[4].Value);
                    vid.Description = cleanString(match.Groups[5].Value);
                }
                else
                {
                    vid.Title = cleanString(match.Groups[3].Value);
                    vid.Description = cleanString(string.Format("{0}\r\n{1}", match.Groups[4], match.Groups[5]));
                }
                vids.Add(vid);
            }
            return vids;
        }
        #endregion

        #region Live Streams
        List<VideoInfo> getLiveStreams(Group group)
        {
            List<VideoInfo> vids = new List<VideoInfo>();
            foreach (Channel chan in group.Channels)
            {
                VideoInfo vid = new VideoInfo();
                vid.Title = chan.StreamName;
                vid.ImageUrl = chan.Thumb;
                vid.VideoUrl = chan.Url;
                int argIndex = chan.Url.IndexOf('?');
                if (argIndex >= 0)
                {
                    if (retrieveTVGuide)
                    {
                        //retrieve tv guide
                        Utils.TVGuideGrabber guide = new Utils.TVGuideGrabber();
                        if (guide.GetNowNextForChannel(vid.VideoUrl))
                            vid.Description = guide.FormatTVGuide(tvGuideFormatString);
                    }
                    vid.VideoUrl = vid.VideoUrl.Remove(argIndex);
                }
                vids.Add(vid);
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
                        SwfUrl = "http://www.itv.com/mediaplayer/ITVMediaPlayer.swf?v=12.18.4",
                        SwfVerify = true,
                        Live = live
                    }.ToString();
                    options.Add(title, url);
                }
            }

            video.PlaybackOptions = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> key in options)
                video.PlaybackOptions.Add(key.Key, key.Value);

            if (RetrieveSubtitles)
            {
                node = videoEntry.SelectSingleNode("./ClosedCaptioningURIs");
                if (node != null && OnlineVideos.Utils.IsValidUri(node.InnerText))
                    video.SubtitleText = OnlineVideos.Sites.Utils.SubtitleReader.TimedText2SRT(GetWebData(node.InnerText));
            }

            return StreamComparer.GetBestPlaybackUrl(video.PlaybackOptions, StreamQualityPref, AutoSelectStream);
        }

        string getProductionId(string url)
        {
            string html = GetWebData(BASE_URL + url);
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

                System.Net.WebProxy proxy = getProxy();
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
        string cleanString(string s)
        {
            s = stripTags(s);
            return s.Replace("&amp;", "&").Replace("&pound;", "£").Replace("&#039;", "'").Trim();
        }

        string stripTags(string s)
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

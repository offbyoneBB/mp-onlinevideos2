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

        int getShowsList(Category parentCategory)
        {
            string html = GetWebData((parentCategory as RssLink).Url);
            Regex reg = new Regex(@"<img src='(.*?)'>[\s\n]*</a>[\s\n]*<div.*?>[\s\n]*<a href=""(.*?)"">(.*?)</a>[\s\n]*.*?[\s\n]*<div.*>[\s\n]*<span.*?>(\d+)");

            List<Category> subCats = new List<Category>();
            foreach (Match match in reg.Matches(html))
            {
                RssLink cat = new RssLink();
                cat.ParentCategory = parentCategory;
                cat.Url = "http://www.itv.com" + match.Groups[2].Value;
                cat.Name = cleanString(match.Groups[3].Value);
                string thumb = match.Groups[1].Value;
                if(!string.IsNullOrEmpty(thumbReplaceRegExPattern))
                    thumb = Regex.Replace(thumb, thumbReplaceRegExPattern, thumbReplaceString);
                cat.Thumb = thumb;
                cat.EstimatedVideoCount = uint.Parse(match.Groups[4].Value);
                subCats.Add(cat);
            }

            parentCategory.SubCategories = subCats;
            parentCategory.SubCategoriesDiscovered = true;
            return subCats.Count;
        }

        int getAtoZList(Category parentCategory)
        {
            string xml = GetWebData((parentCategory as RssLink).Url);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            List<Category> subCats = new List<Category>();            
            foreach (XmlNode node in doc.SelectNodes("//ITVCatchUpProgramme"))
            {
                RssLink cat = new RssLink();
                cat.ParentCategory = parentCategory;
                cat.Url = node.SelectSingleNode("ProgrammeId").InnerText;
                cat.Name = cleanString(node.SelectSingleNode("ProgrammeTitle").InnerText);
                cat.Thumb = node.SelectSingleNode("ProgrammeMediaUrl").InnerText;
                subCats.Add(cat);
            }

            parentCategory.SubCategories = subCats;
            parentCategory.SubCategoriesDiscovered = true;
            return subCats.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            if (category is Group)
            {
                //Live streams
                List<VideoInfo> vids = new List<VideoInfo>();
                foreach (Channel chan in ((Group)category).Channels)
                {
                    VideoInfo vid = new VideoInfo();
                    vid.Title = chan.StreamName;
                    vid.ImageUrl = chan.Thumb;

                    int argIndex = chan.Url.IndexOf('?');
                    if (argIndex < 0)
                        vid.VideoUrl = chan.Url;
                    else
                        vid.VideoUrl = chan.Url.Remove(argIndex);

                    if (retrieveTVGuide && argIndex > -1) //retrieve tv guide
                    {
                        Utils.TVGuideGrabber guide = new Utils.TVGuideGrabber();
                        if (guide.GetNowNextForChannel(chan.Url))
                            vid.Description = guide.FormatTVGuide(tvGuideFormatString);
                    }

                    vids.Add(vid);
                }
                return vids;
            }

            if ((category as RssLink).Url.StartsWith("http"))
                return getShowsVids(category);
            return getAtoZVids(category);
        }

        List<VideoInfo> getShowsVids(Category category)
        {
            string html = GetWebData((category as RssLink).Url);
            Regex reg = new Regex(@"<a href=""(.*?)""><img.*?src=""(.*?)"".*?title=""(.*?)"".*[\s\n]*<a.*?>[\s\n]*<div.*?>[\s\n]*.*?<span.*?>(.*?)</span>.*?<span.*?>Catch up</span><br />[\s\n]*<div.*?>Series (<div.*?>){3}(.*?)</div>.*[\s\n]*<div.*?>Episode (<div.*?>){3}(.*?)</div>.*[\s\n]*</div>[\s\n]*(<div.*?>){3}(.*?)</div>.*?(<div.*?>){3}(.*?)</div>");

            List<VideoInfo> vids = new List<VideoInfo>();
            foreach (Match match in reg.Matches(html))
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
                Match match = Regex.Match(html, @"<h1 class=""title episode-title"".*?>[\s\n]*(.*?)<span.*?>Catch up</span>.*[\s\n]*(<div.*?>){3}<span.*?>(.*?)</span>.*?<div.*?>.*?</div>(<div.*?>){2}(.*?)</div>(.|\n)*?name=""poster"" value=""(.*?)""(.|\n)*?<div class=""description"">[\s\n]*(.*?)</div>(.|\n)*?<a href=""(.*?)""");
                if (match.Success)
                {
                    VideoInfo vid = new VideoInfo();
                    vid.Title = cleanString(match.Groups[1].Value);
                    vid.Airdate = match.Groups[3].Value;
                    vid.Length = match.Groups[5].Value;
                    vid.ImageUrl = match.Groups[7].Value;
                    vid.Description = cleanString(match.Groups[9].Value);
                    vid.VideoUrl = match.Groups[11].Value;
                    vids.Add(vid);
                }
            }

            return vids;
        }
        
        List<VideoInfo> getAtoZVids(Category category)
        {
            string html = GetWebData(string.Format("http://www.itv.com/_app/Dynamic/CatchUpData.ashx?ViewType=1&Filter={0}&moduleID=115107", (category as RssLink).Url));
            Regex vidReg = new Regex(@"<div class=""listItem.*?<a href=""http://.*?&amp;Filter=(\d+).*?<img.*? src=""([^""]*)"".*?<a.*?>([^<]*).*?<p class=""date"">([^<]*).*?<p class=""progDesc"">([^<]*).*?<li>\s*Duration:([^<]*)", RegexOptions.Singleline);

            List<VideoInfo> vids = new List<VideoInfo>();
            foreach (Match match in vidReg.Matches(html))
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

        public override string getUrl(VideoInfo video)
        {
            if (video.VideoUrl.StartsWith("http://") || video.VideoUrl.StartsWith("rtmp://") || video.VideoUrl.StartsWith("rtmpe://"))
                return video.VideoUrl;
            else if (video.VideoUrl.StartsWith("sim"))
                return getLiveUrl(video);

            bool isProductionId = false;
            if (video.VideoUrl.StartsWith("/itvplayer"))
            {
                isProductionId = true;
                video.VideoUrl = getProductionId(video.VideoUrl);
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(getPlaylist(video.VideoUrl, isProductionId));
            XmlNode videoEntry = doc.SelectSingleNode("//VideoEntries/Video");
            if (videoEntry == null)
                return "";

            XmlNode node;
            if (RetrieveSubtitles)
            {
                node = videoEntry.SelectSingleNode("./ClosedCaptioningURIs");
                if (node != null && OnlineVideos.Utils.IsValidUri(node.InnerText))
                    video.SubtitleText = OnlineVideos.Sites.Utils.SubtitleReader.TimedText2SRT(GetWebData(node.InnerText));
            }
            node = videoEntry.SelectSingleNode("./MediaFiles");
            if(node == null || node.Attributes["base"] == null)
                return "";
            string rtmpUrl = node.Attributes["base"].Value;

            SortedList<string, string> options = new SortedList<string, string>(new StreamComparer());
            foreach (XmlNode mediaFile in node.SelectNodes("./MediaFile"))
            {
                if (mediaFile.Attributes["delivery"] == null || mediaFile.Attributes["delivery"].Value != "Streaming")
                    continue;
                string title = ""; int br;
                if (mediaFile.Attributes["bitrate"] != null)
                {
                    title = mediaFile.Attributes["bitrate"].Value;
                    if (int.TryParse(title, out br))
                        title = string.Format("{0} kbps", br / 1000);
                }
                string playPath = mediaFile.InnerText;
                string url = new MPUrlSourceFilter.RtmpUrl(rtmpUrl)
                {
                    PlayPath = playPath,
                    SwfUrl = "http://www.itv.com/mediaplayer/ITVMediaPlayer.swf?v=12.18.4", //"http://www.itv.com/mercury/Mercury_VideoPlayer.swf",
                    SwfVerify = true
                }.ToString();

                if (!options.ContainsKey(title))
                    options.Add(title, url);
            }

            video.PlaybackOptions = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> key in options)
                video.PlaybackOptions.Add(key.Key, key.Value);

            return StreamComparer.GetBestPlaybackUrl(video.PlaybackOptions, StreamQualityPref, AutoSelectStream);
        }

        private string getProductionId(string url)
        {
            string html = GetWebData("http://www.itv.com" + url);
            Match m = Regex.Match(html,  @"""productionId"":""(.*?)""");
            if (m.Success)
            {
                return m.Groups[1].Value.Replace("\\", "");
            }
            return "";
        }

        string getPlaylist(string id, bool isProductionId = false)
        {
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            if (isProductionId)
                doc.InnerXml = string.Format(SOAP_TEMPLATE_PRODID, id);
            else
                doc.InnerXml = string.Format(SOAP_TEMPLATE, id);

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://mercury.itv.com/PlaylistService.svc");

            req.Headers.Add("SOAPAction", "http://tempuri.org/PlaylistService/GetPlaylist");
            req.Referer = "http://www.itv.com/mediaplayer/ITVMediaPlayer.swf?v=12.18.4/[[DYNAMIC]]/2";
            req.ContentType = "text/xml;charset=\"utf-8\"";
            req.Accept = "text/xml";
            req.Method = "POST";

            System.Net.WebProxy proxy = getProxy();
            if (proxy != null)
                req.Proxy = proxy;

            Stream stm;
            using (stm = req.GetRequestStream())
                doc.Save(stm);

            using (stm = req.GetResponse().GetResponseStream())
            using (StreamReader r = new StreamReader(stm))
            {
                string ret = r.ReadToEnd();
                Log.Debug("ITV Response:\r\n\t {0}", ret);
                return ret;
            }
        }

        string getLiveUrl(VideoInfo video)
        {
            bool loadMultiple = false;            
            string xml;
            //hack for ITV1
            if (video.VideoUrl == "sim1")
            {
                xml = GetWebData("http://www.itv.com/ukonly/mediaplayer/xml/channels.itv1.xml");
                loadMultiple = true;
            }
            else
            {
                xml = getPlaylist(video.VideoUrl);
            }

            video.PlaybackOptions = new Dictionary<string, string>();

            string res = new Regex("<ClosedCaptioningURIs.+", RegexOptions.Singleline).Match(xml).Groups[0].Value;
            string rtmpUrl = new Regex("(rtmp[^\"]+)").Match(res).Groups[1].Value.Replace("&amp;", "&");

            string lastUrl = "";
            string[] streams = res.Split(new string[] { "<MediaFile delivery=" }, StringSplitOptions.RemoveEmptyEntries);
            for (int x = 1; x < streams.Length; x++)
            {
                string stream = streams[x];
                if (!stream.StartsWith("\"Streaming\""))
                    continue;

                Match match = new Regex(@"bitrate=""([^""]+)"" base=""([^""]*)""").Match(stream);
                string title = match.Groups[1].Value;
                int br;
                if (int.TryParse(title, out br))
                {
                    br = br / 1000;
                    title = string.Format("{0} kbps", br);
                }

                string playPath = new Regex("<URL><![[]CDATA[[]([^]]+)").Match(stream).Groups[1].Value;
                string baseUrl = match.Groups[2].Value;
                if (string.IsNullOrEmpty(baseUrl))
                    baseUrl = rtmpUrl;

                baseUrl = baseUrl.Replace(".net", ".net:1935");
                string url = new MPUrlSourceFilter.RtmpUrl(baseUrl)
                {
                    PlayPath = playPath,
                    SwfUrl = "http://www.itv.com/mediaplayer/ITVMediaPlayer.swf?v=12.18.4",
                    SwfVerify = true,
                    Live = true
                }.ToString();

                if (video.PlaybackOptions.ContainsKey(title))
                    video.PlaybackOptions[title] = url;
                else
                    video.PlaybackOptions.Add(title, url);
                lastUrl = url;

                if (!loadMultiple)
                    break; //hack, only lowest quality stream seems to play
            }

            return lastUrl;
        }

        #region Search

        public override bool CanSearch
        {
            get
            {
                return true;
            }
        }

        public override List<ISearchResultItem> DoSearch(string query)
        {
            string html = GetWebData(string.Format("http://www.itv.com/itvplayer/search/term/{0}/catch-up", query));
            string regStr = @"<div class=""search-wrapper"">.*?<div class=""search-result-image"">[\s\n]*(<a.*?><img.*?src=""(.*?)"")?.*?<h4 class=""programme-title""><a href=""(.*?)"">(.*?)</a>.*?<div class=""programme-description"">[\s\n]*(.*?)</div>";
            Regex reg = new Regex(regStr, RegexOptions.Singleline);

            List<ISearchResultItem> cats = new List<ISearchResultItem>();            
            foreach (Match match in reg.Matches(html))
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

        const string SOAP_TEMPLATE = @"<?xml version='1.0' encoding='utf-8'?>
<SOAP-ENV:Envelope xmlns:SOAP-ENV='http://schemas.xmlsoap.org/soap/envelope/' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'>
    <SOAP-ENV:Body>
	    <tem:GetPlaylist xmlns:tem='http://tempuri.org/' xmlns:itv='http://schemas.datacontract.org/2004/07/Itv.BB.Mercury.Common.Types' xmlns:com='http://schemas.itv.com/2009/05/Common'>
	        <tem:request>
		        <itv:RequestGuid>FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF</itv:RequestGuid>
		        <itv:Vodcrid>
		            <com:Id>{0}</com:Id>
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

        const string SOAP_TEMPLATE_PRODID = @"<?xml version='1.0' encoding='utf-8'?>
<SOAP-ENV:Envelope xmlns:SOAP-ENV='http://schemas.xmlsoap.org/soap/envelope/' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'>
    <SOAP-ENV:Body>
	    <tem:GetPlaylist xmlns:tem='http://tempuri.org/' xmlns:itv='http://schemas.datacontract.org/2004/07/Itv.BB.Mercury.Common.Types' xmlns:com='http://schemas.itv.com/2009/05/Common'>
	        <tem:request>
                <itv:ProductionId>{0}</itv:ProductionId> 
		        <itv:RequestGuid>FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF</itv:RequestGuid>
		        <itv:Vodcrid>
		            <com:Id />
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
        
    }
}

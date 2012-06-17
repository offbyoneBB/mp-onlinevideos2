using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using OnlineVideos.Sites.brownardPRIVATE;

namespace OnlineVideos.Sites
{
    enum VidType
    {
        ATOZ,
        SHOWS,
        DYNAMIC,
        LIVE
    }

    public class ITVPlayerUtil : SiteUtilBase
    {
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
                List<Category> removeCats = new List<Category>();
                foreach (Category cat in Settings.Categories)
                {
                    if (cat is RssLink)
                    {
                        VidType vidType;
                        if (!(cat.Other is VidType))
                        {
                            vidType = getVidType((cat as RssLink).Url);
                            cat.Other = vidType;
                        }
                        else
                            vidType = (VidType)cat.Other;

                        if (vidType == VidType.DYNAMIC)
                        {
                            removeCats.Add(cat);//remove dynamic cats as they will be re-added
                            continue;
                        }
                        else if (vidType != VidType.LIVE)
                        {
                            cat.HasSubCategories = true;
                            cat.SubCategoriesDiscovered = false;
                        }
                    }
                    if (string.IsNullOrEmpty(cat.Thumb))
                        cat.Thumb = string.Format(@"{0}\Icons\{1}.png", OnlineVideoSettings.Instance.ThumbsDir, Settings.Name);
                }

                foreach (Category cat in removeCats)
                    Settings.Categories.Remove(cat);

                getDynamicCategories();

                lastRefresh = DateTime.Now;
            }

            return Settings.Categories.Count;
        }

        void getDynamicCategories()
        {
            string html = GetWebData("http://www.itv.com/ITVPlayer/#intcmp=NAV_ITVPLAYE");
            Regex catReg = new Regex(@"<h2 class=""moduleHeading"">([^<]*)</h2><div.*?><div.*?><ul class=""pux-linkList"">(.*?)</ul>");
            foreach (Match match in catReg.Matches(html))
            {
                RssLink category = new RssLink();
                Regex showReg = new Regex(@"<li.*?><a.*? href=""([^""]*)""><img.*? src=""([^""]*)"".*?</a><div.*?><h3>.*?<a.*?>([^<]*)</a></h3></div></li>");
                List<Category> subCats = new List<Category>();
                foreach (Match showMatch in showReg.Matches(match.Groups[2].Value))
                {
                    RssLink subCat = new RssLink();
                    subCat.Url = showMatch.Groups[1].Value;
                    subCat.Name = cleanString(showMatch.Groups[3].Value);
                    subCat.ParentCategory = category;

                    if (Regex.IsMatch(showMatch.Groups[1].Value, @"Filter=\d+"))
                    {
                        subCat.Thumb = showMatch.Groups[2].Value;
                        subCat.Other = VidType.SHOWS;
                    }
                    else
                    {
                        subCat.Thumb = "http://www.itv.com/" + showMatch.Groups[2].Value.Replace("&amp;", "&");
                        subCat.Other = VidType.DYNAMIC;
                    }

                    subCats.Add(subCat);
                }

                if (subCats.Count == 0)
                    continue;

                category.Name = cleanString(match.Groups[1].Value);
                category.Thumb = string.Format(@"{0}\Icons\{1}.png", OnlineVideoSettings.Instance.ThumbsDir, Settings.Name);
                category.HasSubCategories = true;
                category.SubCategoriesDiscovered = true;
                category.SubCategories = subCats;
                category.Other = VidType.DYNAMIC;
                Settings.Categories.Add(category);
            }
        }

        VidType getVidType(string url)
        {
            if (string.IsNullOrEmpty(url) || !url.StartsWith("http://www.itv.com/_data"))
                return VidType.SHOWS;
            else
                return VidType.ATOZ;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            if ((VidType)parentCategory.Other == VidType.ATOZ)
                return getAtoZList(parentCategory);

            return getShowsList(parentCategory);
        }

        int getShowsList(Category parentCategory)
        {
            List<Category> subCats = new List<Category>();

            string xml = GetWebData((parentCategory as RssLink).Url);
            Regex reg = new Regex(@"<li><a href=""([^""]*)""><img.*? src=""([^""]*)"".*?</a><h3><a.*?>([^<]*)</a></h3></li>");
            foreach (Match match in reg.Matches(xml))
            {
                RssLink cat = new RssLink();
                cat.ParentCategory = parentCategory;
                cat.Other = parentCategory.Other;
                cat.Url = match.Groups[1].Value;
                cat.Name = cleanString(match.Groups[3].Value);
                cat.Thumb = match.Groups[2].Value;

                subCats.Add(cat);
            }

            parentCategory.SubCategories = subCats;
            parentCategory.SubCategoriesDiscovered = true;
            return subCats.Count;
        }

        int getAtoZList(Category parentCategory)
        {
            List<Category> subCats = new List<Category>();

            string xml = GetWebData((parentCategory as RssLink).Url);

            Regex reg = new Regex("<ProgrammeId>(.+?)</ProgrammeId>\r\n      <ProgrammeTitle>(.+?)</ProgrammeTitle>\r\n      <ProgrammeMediaId>.+?</ProgrammeMediaId>\r\n      <ProgrammeMediaUrl>(.+?)</ProgrammeMediaUrl>\r\n      <LastUpdated>.+?</LastUpdated>\r\n      <Url>.+?</Url>\r\n      <EpisodeCount>(.+?)</EpisodeCount>");
            foreach (Match match in reg.Matches(xml))
            {
                int vidCount;
                if (!int.TryParse(match.Groups[4].Value, out vidCount) || vidCount < 1)
                    continue;

                RssLink cat = new RssLink();
                cat.ParentCategory = parentCategory;
                cat.Other = parentCategory.Other;
                cat.Url = match.Groups[1].Value;
                cat.Name = cleanString(match.Groups[2].Value);
                cat.Thumb = match.Groups[3].Value;
                cat.EstimatedVideoCount = (uint)vidCount;

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

                    int argIndex = chan.Url.IndexOf('?');
                    if (argIndex < 0) vid.VideoUrl = chan.Url;
                    else vid.VideoUrl = chan.Url.Remove(argIndex);

                    vid.ImageUrl = chan.Thumb;

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

            VidType vidType = (VidType)category.Other;

            if (vidType == VidType.ATOZ)
                return getAtoZVids(category);
            else if (vidType == VidType.DYNAMIC)
                return getTimeSavingVids(category);

            return getShowsVids(category);

        }

        private List<VideoInfo> getTimeSavingVids(Category category)
        {
            List<VideoInfo> vids = new List<VideoInfo>();
            string html = GetWebData((category as RssLink).Url);
            Regex reg = new Regex(@"ItvPlayer.embedVideoPlayer[(](\d+)[)].*?<span>([^<]*)</span></h1></div><p>([^<]*)</p></div>", RegexOptions.Singleline);
            Match match = reg.Match(html);
            if (!match.Success)
                return vids;

            VideoInfo vid = new VideoInfo();
            vid.VideoUrl = match.Groups[1].Value;
            vid.Title = cleanString(match.Groups[2].Value);
            vid.Description = cleanString(match.Groups[2].Value);
            vid.ImageUrl = category.Thumb;
            vid.Other = VidType.DYNAMIC;
            vids.Add(vid);
            return vids;
        }

        List<VideoInfo> getShowsVids(Category category)
        {
            List<VideoInfo> vids = new List<VideoInfo>();

            string html = GetWebData((category as RssLink).Url);
            Regex reg = new Regex(@"<li.*?><a.*? href=""http://www.itv.com/itvplayer/video/[?]Filter=(\d+)""><img.*? src=""([^""]*)"".*?<h3><a.*?>([^<]*)</a></h3>.*?<p>([^<]*)</p></div></li>");
            foreach (Match match in reg.Matches(html))
            {
                VideoInfo vid = new VideoInfo();
                vid.VideoUrl = match.Groups[1].Value;
                vid.ImageUrl = match.Groups[2].Value;
                vid.Title = cleanString(match.Groups[3].Value);
                vid.Description = cleanString(match.Groups[4].Value);
                vid.Other = VidType.SHOWS;
                vids.Add(vid);
            }

            return vids;
        }
        
        List<VideoInfo> getAtoZVids(Category category)
        {
            List<VideoInfo> vids = new List<VideoInfo>();

            string html = GetWebData(string.Format("http://www.itv.com/_app/Dynamic/CatchUpData.ashx?ViewType=1&Filter={0}&moduleID=115107", (category as RssLink).Url));

            Regex vidReg = new Regex(@"<div class=""listItem.*?<a href=""http://.*?&amp;Filter=(\d+).*?<img.*? src=""([^""]*)"".*?<a.*?>([^<]*).*?<p class=""date"">([^<]*).*?<p class=""progDesc"">([^<]*).*?<li>\s*Duration:([^<]*)", RegexOptions.Singleline);
            foreach (Match match in vidReg.Matches(html))
            {
                VideoInfo vid = new VideoInfo();
                vid.Other = VidType.ATOZ;
                vid.VideoUrl = match.Groups[1].Value;
                vid.ImageUrl = match.Groups[2].Value;

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
                vid.Length = match.Groups[6].Value.Trim();
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

            string xml = getPlaylist(video.VideoUrl);
            
            string res = new Regex("<ClosedCaptioningURIs.+", RegexOptions.Singleline).Match(xml).Groups[0].Value;
            string rtmpUrl = new Regex("(rtmp[^\"]+)").Match(res).Groups[1].Value.Replace("&amp;", "&");

            string[] streams = res.Split(new string[] { "<MediaFile delivery=" }, StringSplitOptions.RemoveEmptyEntries);

            SortedList<string, string> options = new SortedList<string, string>(new StreamComparer());
            for (int x = 1; x < streams.Length; x++ )
            {
                string stream = streams[x];
                if (!stream.StartsWith("\"Streaming"))
                    continue;
                Match match = new Regex(@"bitrate=""([^""]+)"" base=""([^""]*)""").Match(stream);
                string title = match.Groups[1].Value; int br;
                if (int.TryParse(title, out br))
                    title = string.Format("{0} kbps", br / 1000);

                string playPath = new Regex("<URL><![[]CDATA[[]([^]]+)").Match(stream).Groups[1].Value;

                string url = new MPUrlSourceFilter.RtmpUrl(rtmpUrl)
                {
                    PlayPath = playPath,
                    SwfUrl = "http://www.itv.com/mediaplayer/ITVMediaPlayer.swf?v=12.11.3", //"http://www.itv.com/mercury/Mercury_VideoPlayer.swf",
                    SwfVerify = true,
                    Live = (VidType)video.Other == VidType.LIVE
                }.ToString();

                if (!options.ContainsKey(title))
                    options.Add(title, url);
            }

            video.PlaybackOptions = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> key in options)
                video.PlaybackOptions.Add(key.Key, key.Value);

            return StreamComparer.GetBestPlaybackUrl(video.PlaybackOptions, StreamQualityPref, AutoSelectStream);
        }
        
        string getPlaylist(string id)
        {
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            doc.InnerXml = string.Format(SOAP_TEMPLATE, id);

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://mercury.itv.com/PlaylistService.svc");

            req.Headers.Add("SOAPAction", "http://tempuri.org/PlaylistService/GetPlaylist");
            req.Referer = "http://www.itv.com/mercury/Mercury_VideoPlayer.swf?v=1.6.479/[[DYNAMIC]]/2";
            req.ContentType = "text/xml;charset=\"utf-8\"";
            req.Accept = "text/xml";
            req.Method = "POST";

            Stream stm = req.GetRequestStream();
            doc.Save(stm);
            stm.Close();

            WebResponse resp = req.GetResponse();
            stm = resp.GetResponseStream();
            StreamReader r = new StreamReader(stm);

            string ret = r.ReadToEnd();
            r.Close();

            return ret;
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
            Regex reg = new Regex(@"<li class=""result""><a href=""([^""]*)""><img src=""([^""]*)"".*?</a><h3><a.*?>([^<]*)</a>.*?<p>[(](\d+) Episodes[)]</p><p>(.*?)</p></li>");
            string html = GetWebData("http://www.itv.com/itvplayer/search/?Filter=" + query);
            List<ISearchResultItem> cats = new List<ISearchResultItem>();

            foreach (Match match in reg.Matches(html))
            {
                int vidCount;
                if (!int.TryParse(match.Groups[4].Value, out vidCount) || vidCount < 1)
                    continue;

                RssLink cat = new RssLink();
                cat.EstimatedVideoCount = (uint)vidCount;
                cat.Url = match.Groups[1].Value;
                cat.Thumb = match.Groups[2].Value;
                cat.Name = cleanString(match.Groups[3].Value);
                cat.Description = cleanString(match.Groups[5].Value);
                cat.Other = VidType.SHOWS;

                cats.Add(cat);
            }

            return cats;
        }

        #endregion

        string stripTags(string s)
        {
            s = cleanString(s);
            return s.Replace("<p>", "").Replace("</p>", "\r\n");
        }

        string cleanString(string s)
        {
            return s.Replace("&amp;", "&").Replace("&pound;", "£").Trim();
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

        
        string getLiveUrl(VideoInfo video)
        {
            string xml = getPlaylist(video.VideoUrl);            
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
                    SwfUrl = "http://www.itv.com/mediaplayer/ITVMediaPlayer.swf?v=12.11.3",
                    SwfVerify = true,
                    Live = true
                }.ToString();                

                if (video.PlaybackOptions.ContainsKey(title))
                    video.PlaybackOptions[title] = url;
                else
                    video.PlaybackOptions.Add(title, url);
                lastUrl = url;

                break; //hack, only lowest quality stream seems to play
            }

            return lastUrl;
        }

    }
}

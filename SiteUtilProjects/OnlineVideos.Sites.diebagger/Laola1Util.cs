using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.ComponentModel;
using Microsoft.Win32;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Site util for video streaming site www.laola1.tv
    /// 
    /// The site is available in 3 different locales at the moment (at/de/int), each offering different contents. The locale can
    /// be chosen with laola1TvBaseUrl .
    /// 
    /// TODO:
    /// - Next pages are currently not processed
    /// - 
    /// </summary>
    public class Laola1Util : SiteUtilBase
    {
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Quality"), Description("Choose your preferred quality for the videos according to bandwidth.")]
        VideoQuality videoQuality = VideoQuality.High;

        [Category("OnlineVideosConfiguration"), LocalizableDisplayName("Base Url"), Description("The url of the main loala1.tv site (e.g. int/de/at)")]
        String laola1TvBaseUrl = "http://www.laola1.tv/en/int/home/"; //default is international

        private const String PLAYDATA_URL = "http://www.laola1.tv/server/ondemand_xml_esi.php?playkey={0}-{1}";//0=playkey1, 1=playkey2
        //65196_high.xml?partnerid=22&streamid=65196
        //private const String ACCESSDATA_URL_ARCHIVE = "http://streamaccess.laola1.tv/flash/vod/22/{0}_{1}.xml";//0=playkey1, 1=quality
        private const String ACCESSDATA_URL_ARCHIVE = "http://streamaccess.laola1.tv/flash/vod/22/{0}_{1}.xml?partnerid=22&streamdid={0}";//0=playkey1, 1=quality
        private const String ACCESSDATA_URL_LIVE = "http://streamaccess.laola1.tv/flash/1/{0}_{1}.xml";//0=playkey1, 1=quality
        private const String IDENT_URL = "http://{0}/fcs/ident";//0=ident server

        /// <summary>
        /// Archive=VOD service of laola1.tv, Live=Livestreams of laola1.tv
        /// </summary>
        public enum LaolaCategoryTypes { Archive, Live }

        /// <summary>
        /// Type of the video format
        /// </summary>
        private enum LiveStreamType { Default, HD }

        /// <summary>
        /// Currently, only Low and High are available video qualities on laola1.tv
        /// </summary>
        public enum VideoQuality { Low, Medium, High };


        //different regexes for parsing laola1.tv content
        private string regexSitemap = @"<div id=""sitemap""><a (.+?)</div>";//get sitemap(s)
        private string regexVideoCategories = @"href=""(?<url>.+?)""(?<optional>.*?)>(?<title>.+?)</a>";//get video categories
        private string regexVideoSubCategories = @"<td style="".+?"" width="".+?""><h2><a href=""(?<url>.+?)"" style="".+?"">(?<title>.+?)</a></h2></td>";//get sub-categories
        private string regexVideosArchive = @"<div class="".+?"" title="".+?""><a href=""(?<url>.+?)""><img src=""(?<img>.+?)"" border="".+?"" /></a></div>.+?<div class=""teaser_head_video"" title="".+?"">(?<date>.+?)</div>.+?<div class=""teaser_text"" title="".+?"">(?<title>.+?)</div>";//get archived videos
        private string regexVideoNextPage = @"<a href=""(?<url>.+?)"" class=""teaser_text"">vor|next</a>";//next page in videos
        private string regexGetPlaykeys = @"AC_FL_RunContent.*?""src"", ""(?<flashplayer>.*?)"".*?""width"", ""(?<width>[0-9]*)"".*?""height"", ""(?<height>[0-9]*)"".*?playkey=(?<playkey1>.+?)-(?<playkey2>.+?)&adv.*?fversion=(?<flashversion>.+?)""";//get playkeys
        private string regexGetVideoInfo = @"<(?<quality>[^<]+?)server=""(?<server>.+?)/(?<servertype>.+?)"" pfad=""(?<path>.+?)"" .+? ptitle=""(?<title>.+?)""";//get playkeys
        private string regexGetAuthArchive = @"auth=""(?<auth>.+?)"".+?url=""(?<url>.+?)"".+?stream=""(?<stream>.+?)"".+?status=""(?<status>.+?)"".+?statustext=""(?<statustext>.+?)"".+?aifp=""(?<aifp>.+?)""";//get auth
        private string regexGetIp = @"<ip>(?<ip>.+?)</ip>";
        private string regexGetUpcomingLiveStreams = @"<h2><a href=""(?<url>http://www.laola1.tv/(?<lang>.+?)/upcoming-livestreams/(?<page>.+?))""";//link to upcoming live streams
        private string regexVideosLive = @"<div class="".+?"" title="".+?""><a href=""(?<url>.+?)""><img src=""(?<img>.+?)"" border="".+?"" /></a></div>.+?<div class=""teaser_head_live"" title="".+?"">(?<date>.+?)</div>.+?<div class=""teaser_text"" title="".+?"">(?<title>.+?)</div>";//get live videos
        private string regexIsLiveAvailable = @"(?<message>(<p>Lieber LAOLA1-User,</p>|<p>Dear LAOLA1-User,</p>).*?<p style=.*?>(?<inner>.*?)</p>.*?)</td>";
        private string regexGetAuthLive = @"auth=""(?<auth>.+?)&amp;p=.+?"".+?url=""(?<url>.+?)/live"".+?stream=""(?<stream>.+?)"".+?status=""(?<status>.+?)"".+?statustext=""(?<statustext>.+?)"".+?aifp=""(?<aifp>.+?)""";


        //hd live streams
        private string regexLiveHdPlaykes = @"AC_FL_RunContent.*?""src"", ""(?<flashplayer>.*?)"".*?""width"", ""(?<width>[0-9]*)"".*?""height"", ""(?<height>[0-9]*)"".*?videopfad=(?<streamaccess>http://streamaccess.*?(?<streamid>[0-9]*))&";
        private string regexLiveHdBaseUrls = @"name=""httpBase"" content=""(?<httpbase>.*?)"".*?name=""rtmpPlaybackBase"" content=""(?<rtmpbase>.*?)""";
        private string regexLiveHdSources = @"<video src=""(?<src>.+?)"" system-bitrate=""(?<bitrate>.+?)""/>";

        Regex regEx_Sitemap;
        Regex regEx_Categories;
        Regex regEx_SubCategories;
        Regex regEx_VideosArchive;
        Regex regEx_VideosNextPage;
        Regex regEx_GetPlaykeys;
        Regex regEx_GetVideoInfo;
        Regex regEx_GetAuthArchive;
        Regex regEx_GetIp;
        Regex regEx_GetUpcomingLiveStreams;
        Regex regEx_VideosLive;
        Regex regEx_IsLiveAvailable;
        Regex regEx_GetAuthLive;
        Regex regEx_GetLiveHdBaseUrls;
        Regex regEx_GetLiveHdSources;
        Regex regEx_GetLiveHdPlaykeys;
        private string nextVideoPage;
        private Category currentCategory;

        /// <summary>
        /// Initialize the site util
        /// </summary>
        /// <param name="siteSettings">the sitesettings as set in xml</param>
        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            regEx_Sitemap = new Regex(regexSitemap, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regEx_Categories = new Regex(regexVideoCategories, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regEx_SubCategories = new Regex(regexVideoSubCategories, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regEx_VideosArchive = new Regex(regexVideosArchive, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
            regEx_VideosNextPage = new Regex(regexVideoNextPage, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regEx_GetPlaykeys = new Regex(regexGetPlaykeys, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
            regEx_GetVideoInfo = new Regex(regexGetVideoInfo, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
            regEx_GetAuthArchive = new Regex(regexGetAuthArchive, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
            regEx_GetIp = new Regex(regexGetIp, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
            regEx_GetUpcomingLiveStreams = new Regex(regexGetUpcomingLiveStreams, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
            regEx_VideosLive = new Regex(regexVideosLive, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
            regEx_IsLiveAvailable = new Regex(regexIsLiveAvailable, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
            regEx_GetAuthLive = new Regex(regexGetAuthLive, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

            regEx_GetLiveHdPlaykeys = new Regex(regexLiveHdPlaykes, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
            regEx_GetLiveHdBaseUrls = new Regex(regexLiveHdBaseUrls, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
            regEx_GetLiveHdSources = new Regex(regexLiveHdSources, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
        }

        /// <summary>
        /// Gets the root categories for the base url containing
        /// a) upcoming live streams
        /// b) root categories of archived videos
        /// </summary>
        /// <returns>Number of categories</returns>
        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            string data = GetWebData(laola1TvBaseUrl);

            //Parse and add the url for upcoming livestreams
            Match l = regEx_GetUpcomingLiveStreams.Match(data);
            while (l.Success)
            {
                RssLink cat = new RssLink();
                cat.Name = "[Live-Streams]";
                cat.Url = l.Groups["url"].Value; ;
                cat.HasSubCategories = false;
                cat.Other = LaolaCategoryTypes.Live;
                //cat.Thumb = "http://" + host + n.Groups["ImageUrl"].Value;
                Settings.Categories.Add(cat);

                l = l.NextMatch();
            }

            //Parse and add the url for archived video
            //TODO: currently we are using the sitemap for this, however the proper way would be to parse the javascript menu. This
            //has some elements that the sitemap and even the detail pages of the categories don't have (e.g. La Liga 10/11). 
            //The javascript loads the data from: http://www.laola1.tv/server/menu.php?menu=seochannels&geo=only_aut&lang=DE&rnd=13315931
            Match c = regEx_Sitemap.Match(data);
            while (c.Success)
            {
                Match c2 = regEx_Categories.Match(c.Value);
                RssLink currentToplevelCategory = null;
                while (c2.Success)
                {
                    String categoryUrl = c2.Groups["url"].Value;
                    String categoryTitle = c2.Groups["title"].Value;
                    String optional = c2.Groups["optional"].Value;

                    RssLink cat = new RssLink();
                    cat.Name = categoryTitle;
                    cat.Url = categoryUrl;
                    cat.HasSubCategories = true;
                    cat.Other = LaolaCategoryTypes.Archive;

                    if (currentToplevelCategory == null || optional.Contains("line1"))
                    {
                        //only top-level categories
                        Settings.Categories.Add(cat);
                        currentToplevelCategory = cat;
                    }
                    else
                    {
                        if (currentToplevelCategory.SubCategories == null)
                        {
                            currentToplevelCategory.SubCategories = new List<Category>();
                        }
                        currentToplevelCategory.SubCategories.Add(cat);
                    }

                    c2 = c2.NextMatch();
                }

                c = c.NextMatch();
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        /// <summary>
        /// Dynamically discover sub-categories for a given parent category
        /// </summary>
        /// <param name="parentCategory">parent category</param>
        /// <returns>Count of sub-categories</returns>
        public override int DiscoverSubCategories(Category parentCategory)
        {
            return GetSubCategories(parentCategory, ((RssLink)parentCategory).Url);
        }

        /// <summary>
        /// Downloads and parses subcategories for a given category
        /// </summary>
        /// <param name="parentCategory">parent category</param>
        /// <param name="url">Url of the parent category</param>
        /// <returns>Count of sub-categories</returns>
        private int GetSubCategories(Category parentCategory, string url)
        {
            if (parentCategory.SubCategories == null)
            {
                //only take subcategories from site directly if we didn't already get them from the sitemap. The subcategories from the sitemap are
                //better structured than what we get in the direct page for top-level-categories (e.g. Fuﬂball Int.). For subcategories (e.g. La Liga)
                //we still have to get the subcategories from the page.
                parentCategory.SubCategories = new List<Category>();

                String data = GetWebData(url);
                Match c = regEx_SubCategories.Match(data);
                while (c.Success)
                {

                    String categoryUrl = c.Groups["url"].Value;
                    String categoryTitle = c.Groups["title"].Value;

                    RssLink cat = new RssLink();
                    cat.Name = categoryTitle;
                    cat.Url = categoryUrl;
                    cat.HasSubCategories = false;

                    //if the title ends with "LIVE" or "- LIVE" the containing items are live videos
                    if (categoryTitle.EndsWith(" LIVE", StringComparison.OrdinalIgnoreCase))
                    {
                        cat.Other = LaolaCategoryTypes.Live;
                    }
                    else
                    {
                        cat.Other = LaolaCategoryTypes.Archive;
                    }

                    parentCategory.SubCategories.Add(cat);
                    cat.ParentCategory = parentCategory;

                    c = c.NextMatch();
                }
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        /// <summary>
        /// Gets the list of videos for the given category
        /// </summary>
        /// <param name="category">The parent category</param>
        /// <returns>List of video items</returns>
        public override List<VideoInfo> GetVideos(Category category)
        {
            currentCategory = category;
            List<VideoInfo> videos = new List<VideoInfo>();
            LaolaCategoryTypes type = (LaolaCategoryTypes)category.Other;
            if (type == LaolaCategoryTypes.Archive)
            {
                String data = GetWebData((category as RssLink).Url);

                Match videoMatches = regEx_VideosArchive.Match(data);
                while (videoMatches.Success)
                {
                    String categoryUrl = videoMatches.Groups["url"].Value;
                    String categoryTitle = videoMatches.Groups["title"].Value;
                    String categoryDate = videoMatches.Groups["date"].Value;
                    String categoryImage = videoMatches.Groups["img"].Value;

                    VideoInfo video = new VideoInfo();
                    video.Title = categoryTitle;
                    video.VideoUrl = categoryUrl;
                    video.Thumb = categoryImage;
                    video.Airdate = categoryDate;
                    video.Other = LaolaCategoryTypes.Archive;
                    
                    videos.Add(video);

                    videoMatches = videoMatches.NextMatch();
                }

                Match nextMatch = regEx_VideosNextPage.Match(data);
                if (nextMatch.Success)
                {
                    nextVideoPage = nextMatch.Groups["url"].Value;
                }
                else
                {
                    nextVideoPage = null;
                }
            }
            else if (type == LaolaCategoryTypes.Live)
            {
                String data = GetWebData((category as RssLink).Url);

                Match liveMatches = regEx_VideosLive.Match(data);
                while (liveMatches.Success)
                {
                    String categoryUrl = liveMatches.Groups["url"].Value;
                    String categoryTitle = liveMatches.Groups["title"].Value;
                    String categoryDate = liveMatches.Groups["date"].Value;
                    String categoryImage = liveMatches.Groups["img"].Value;

                    VideoInfo video = new VideoInfo();
                    video.Title = categoryTitle;
                    video.VideoUrl = categoryUrl;
                    video.Thumb = categoryImage;
                    video.Airdate = categoryDate;
                    video.Other = LaolaCategoryTypes.Live;

                    videos.Add(video);

                    liveMatches = liveMatches.NextMatch();
                }
            }

            return videos;
        }

        public override bool HasNextPage
        {
            get
            {
                return nextVideoPage != null;
            }
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            (currentCategory as RssLink).Url = nextVideoPage;
            return GetVideos(currentCategory);
        }

        /// <summary>
        /// Gets the actual rtmp url for a given video
        /// </summary>
        /// <param name="video">The video that is started</param>
        /// <returns>The actual playback url</returns>
        public override string GetVideoUrl(VideoInfo video)
        {
            Log.Info("Get url: " + video.VideoUrl);

            LaolaCategoryTypes type = (LaolaCategoryTypes)video.Other;
            if (type == LaolaCategoryTypes.Archive)
            {
                return getVideoArchiveUrl(video);
            }
            else if (type == LaolaCategoryTypes.Live)
            {
                return getVideoLiveUrl(video);
            }

            return null;
        }

        /// <summary>
        /// Get playback url for live video items
        /// </summary>
        /// <param name="video"></param>
        /// <returns>Playback url</returns>
        private string getVideoLiveUrl(VideoInfo video)
        {
            String data = GetWebData(video.VideoUrl);
            Match isLiveStreamActive = regEx_IsLiveAvailable.Match(data);
            if (isLiveStreamActive.Success)
            {
                String msg = isLiveStreamActive.Groups["inner"].Value;
                Log.Info("Stream not available: " + msg);
                //TODO: better detect parts of error message (e.g. Dieser Stream beginnt Donnerstag, 12.09.20111 um 14:00 Uhr CET)
                //and insert linebreaks (whole line is too long for dialogs)
                if (msg.Contains(","))
                {
                    //msg = msg.Replace(",  ", ",\n");
                }
                throw new OnlineVideosException(msg);
            }
            else
            {
                if (data.Contains("isLiveStream=true"))
                {
                    //throw new OnlineVideosException("This stream type isn't supported yet");
                    return GetHdStreamingUrl(video, data);
                }
                else
                {
                    return GetHdStreaming2Url(video, data);
                    //return GetDefaultStreamingUrl(video, data);
                }
                /*
                Match c = regEx_GetPlaykeys.Match(data);
                if (c.Success)
                {
                    String playkey1 = c.Groups["playkey1"].Value;
                    String playkey2 = c.Groups["playkey2"].Value;
                    String flashPlayer = c.Groups["flashplayer"].Value + ".swf";
                    String flashVersion = c.Groups["flashversion"].Value;
                    LiveStreamType liveType = LiveStreamType.Default;
                    if (data.Contains("http://www.laola1.tv/swf/hdplayer_2"))
                    {
                        //TODO: better detection of stream type
                        liveType = LiveStreamType.HD;
                    }
                    if (liveType == LiveStreamType.HD)
                    {

                    }
                    else
                    {
                        return GetDefaultStreamingUrl(video, data);
                    }
                }*/
            }

            //something has gone wrong -> return null
            return null;
        }

        /// <summary>
        /// The default streaming from laola1.tv
        /// 
        /// The stream starts correctly, but fails after some time for an unknown reason.
        /// </summary>
        /// <param name="video">Video object</param>
        /// <param name="playkey1">Playkey1 for this video</param>
        /// <param name="playkey2">Playkey2 for this video</param>
        /// <returns>Url for streaming</returns>
        private string GetDefaultStreamingUrl(VideoInfo video, string data)
        {
            Match c = regEx_GetPlaykeys.Match(data);
            if (c.Success)
            {
                String playkey1 = c.Groups["playkey1"].Value;
                String playkey2 = c.Groups["playkey2"].Value;
                String flashPlayer = c.Groups["flashplayer"].Value + ".swf";
                String flashVersion = c.Groups["flashversion"].Value;

                String playData = GetWebData(String.Format(PLAYDATA_URL, playkey1, playkey2));
                Match c2 = regEx_GetVideoInfo.Match(playData);
                bool videoQualityFound = false;
                while (c2.Success)
                {
                    String server = c2.Groups["server"].Value;
                    String path = c2.Groups["path"].Value;
                    String streamQuality = c2.Groups["quality"].Value.Trim();

                    if (String.Compare(streamQuality, videoQuality.ToString(), true) == 0)
                    {
                        videoQualityFound = false;
                        String accessData = GetWebData(String.Format(ACCESSDATA_URL_LIVE, playkey1, streamQuality));
                        Match c3 = regEx_GetAuthLive.Match(accessData);
                        String servertype = c2.Groups["servertype"].Value; ;
                        String auth = null;
                        String aifp = null;
                        String stream = null;
                        String url = null;
                        if (c3.Success)
                        {
                            auth = c3.Groups["auth"].Value;
                            auth = auth.Replace("amp;", "");
                            aifp = c3.Groups["aifp"].Value;
                            stream = c3.Groups["stream"].Value;
                            url = c3.Groups["url"].Value;
                            c3 = c3.NextMatch();
                        }
                        else
                        {
                            Log.Warn("Couldn't parse " + accessData);
                        }

                        String ip = null;
                        String identData = GetWebData(String.Format(IDENT_URL, server));
                        Match c4 = regEx_GetIp.Match(identData);
                        if (c4.Success)
                        {
                            ip = c4.Groups["ip"].Value;
                            c4 = c4.NextMatch();
                        }
                        else
                        {
                            Log.Warn("Couldn't parse " + identData);
                        }

                        String rtmpUrl = String.Format("rtmp://{0}:1935/{1}?_fcs_vhost={2}/{3}?auth={4}&p=1&e={5}&u=&t=livevideo&l=&a=&aifp={6}", ip, servertype, url, stream, auth, playkey1, aifp);
                        MPUrlSourceFilter.RtmpUrl resultUrl = new MPUrlSourceFilter.RtmpUrl(rtmpUrl);
                        //resultUrl.FlashVersion = flashVersion;
                        resultUrl.Live = true;
                        resultUrl.PageUrl = video.VideoUrl;
                        resultUrl.SwfUrl = flashPlayer;
                        //TODO: I need the mp4, otherwise the stream isn't found, check if there are other formats than mp4 on laola1.tv
                        resultUrl.PlayPath = "mp4:" + stream;
                        //Log.Info("Playback Url: " + playpath);

                        return resultUrl.ToString();

                    }
                    c2 = c2.NextMatch();



                }
                if (!videoQualityFound)
                {
                    //this shouldn't happen, maybe the site has added/removed video qualities
                    Log.Warn("Couldn't find the video stream with quality " + videoQuality.ToString());
                }
            }
            return null;
        }
        private string GetHdStreaming2Url(VideoInfo video, String data)
        {
            Match c = regEx_GetPlaykeys.Match(data);
            if (c.Success)
            {
                String playkey1 = c.Groups["playkey1"].Value;
                String playkey2 = c.Groups["playkey2"].Value;
                String flashPlayer = c.Groups["flashplayer"].Value + ".swf";
                String flashVersion = c.Groups["flashversion"].Value;


                String playData = GetWebData("http://streamaccess.unas.tv/hdflash/1/hdlaola1_" + playkey1 + ".xml?streamid=" + playkey1 + "&partnerid=1&quality=hdlive&t=.smil");

                Match baseUrls = regEx_GetLiveHdBaseUrls.Match(playData);
                Match sources = regEx_GetLiveHdSources.Match(playData);

                //TODO: don't rely on the fact, the the quality is sorted (first item = worst quality, third = best)
                if (videoQuality == VideoQuality.Medium)
                {
                    sources = sources.NextMatch();
                }
                else if (videoQuality == VideoQuality.High)
                {
                    sources = sources.NextMatch().NextMatch();
                }


                String httpUrl = baseUrls.Groups["httpbase"].Value + sources.Groups["src"].Value + "&v=2.4.5&fp=WIN%2011,1,102,55&r=" + GetHdLiveRandomString(5) + "&g=" + GetHdLiveRandomString(12);
                httpUrl = httpUrl.Replace("amp;", "");
                MPUrlSourceFilter.HttpUrl url = new MPUrlSourceFilter.HttpUrl(httpUrl);

                return url.ToString();
            }
            return null;
        }

        /// <summary>
        /// HD streaming from laola1.tv
        /// http://streamaccess.unas.tv/hdflash/1/hdlaola1_70429.xml?streamid=70429&partnerid=1&quality=hdlive&t=.smil
        /// This streaming type is used for many major sport events (e.g. soccer games, ice hockey, etc.) 
        /// </summary>
        /// <param name="video">Video object</param>
        /// <param name="playkey1">Playkey1 for this video</param>
        /// <param name="playkey2">Playkey2 for this video</param>
        /// <returns>Url for streaming</returns>
        private string GetHdStreamingUrl(VideoInfo video, String data)
        {
            //TODO: this isn't working yet. MediaPortal doesn't play the stream for some reason.
            //Downloading works though and the file can be played after that.

            Match c = regEx_GetLiveHdPlaykeys.Match(data);
            if (c.Success)
            {
                String streamAccess = c.Groups["streamaccess"].Value;
                String streamId = c.Groups["streamid"].Value;
                String flashPlayer = c.Groups["flashplayer"].Value + ".swf";
                //String flashVersion = c.Groups["flashversion"].Value;

                //String playData = GetWebData(String.Format("http://streamaccess.laola1.tv/hdflash/1/hdlaola1_{0}.xml?streamid={1}&partnerid=1&quality=hdlive&t=.smil", playkey1, playkey1));

                System.Net.CookieContainer container = new System.Net.CookieContainer();
                String playData = GetWebData(streamAccess, cookies: container);
                Match baseUrls = regEx_GetLiveHdBaseUrls.Match(playData);
                Match sources = regEx_GetLiveHdSources.Match(playData);

                //TODO: don't rely on the fact, the the quality is sorted (first item = worst quality, third = best)
                if (videoQuality == VideoQuality.Medium)
                {
                    sources = sources.NextMatch();
                }
                else if (videoQuality == VideoQuality.High)
                {
                    sources = sources.NextMatch().NextMatch();
                }


                String httpUrl = baseUrls.Groups["httpbase"].Value + sources.Groups["src"].Value;// +"&v=2.6.6&fp=WIN%2011,1,102,62&r=" + GetHdLiveRandomString(5) + "&g=" + GetHdLiveRandomString(12);
                httpUrl = httpUrl.Replace("amp;", "");
                httpUrl = httpUrl.Replace("e=&", "e=" + streamId + "&");
                httpUrl = httpUrl.Replace("p=&", "p=1&");

                /*String rtmpUrl = String.Format("rtmp://{0}:1935/{1}?_fcs_vhost={2}/{3}?auth={4}&p=1&e={5}&u=&t=livevideo&l=&a=&aifp={6}", ip, servertype, url, stream, auth, playkey1, aifp);
                //Log.Info("RTMP Url: " + rtmpUrl);
                String playpath = ReverseProxy.Instance.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                        string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&swfVfy={1}&live={2}",
                        System.Web.HttpUtility.UrlEncode(rtmpUrl),
                        System.Web.HttpUtility.UrlEncode("true"),
                        System.Web.HttpUtility.UrlEncode("true")
                        ));
                String playpath = string.Format("{0}&swfVfy={1}&live={2}",
                        rtmpUrl,
                        System.Web.HttpUtility.UrlEncode("true"),
                        System.Web.HttpUtility.UrlEncode("true")
                        );*/

                //String playpath = ReverseProxy.Instance.GetProxyUri(this, httpUrl);
                MPUrlSourceFilter.HttpUrl url = new MPUrlSourceFilter.HttpUrl(httpUrl);
                //url.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-GB; rv:1.9.0.3) Gecko/2008092417 Firefox/3.0.3";
                url.Cookies.Add(container.GetCookies(new Uri(streamAccess)));
                return url.ToString();
            }
            return null;
        }

        /*
         		public static function getCacheBustString(_arg1:number=5):string{
    var _local2 = "";
    var _local3:number = 0;
    while (_local3 < _arg1) {
    _local2 = (_local2 + string.fromcharcode((65 + math.round((math.random() * 25)))));
    _local3++;
    };
    return (_local2);
    }
         */

        /// <summary>
        /// Returns a random String mimicing the function getCacheBustString from the hd flash player
        /// </summary>
        /// <param name="_count"></param>
        /// <returns></returns>
        public String GetHdLiveRandomString(int _count)
        {
            String text = "";
            Random rnd = new Random(DateTime.Now.Millisecond);
            for (int i = 0; i < _count; i++)
            {
                text = text + char.ConvertFromUtf32(rnd.Next(65, 89));//capital chars
            }
            return text;
        }

        /// <summary>
        /// Get playback url for archived video items (VOD)
        /// </summary>
        /// <param name="video">Video object</param>
        /// <returns>Playback url</returns>
        private string getVideoArchiveUrl(VideoInfo video)
        {
            String data = GetWebData(video.VideoUrl);
            Match c = regEx_GetPlaykeys.Match(data);
            while (c.Success)
            {
                String playkey1 = c.Groups["playkey1"].Value;
                String playkey2 = c.Groups["playkey2"].Value;
                String flashplayer = c.Groups["flashplayer"].Value + ".swf";
                String flashVersion = c.Groups["flashversion"].Value;

                String playData = GetWebData(String.Format(PLAYDATA_URL, playkey1, playkey2));
                Match c2 = regEx_GetVideoInfo.Match(playData);
                bool videoQualityFound = false;
                while (c2.Success)
                {
                    String server = c2.Groups["server"].Value;
                    String path = c2.Groups["path"].Value;
                    String streamQuality = c2.Groups["quality"].Value.Trim();

                    if (String.Compare(streamQuality, videoQuality.ToString(), true) == 0)
                    {
                        videoQualityFound = true;
                        String accessData = GetWebData(String.Format(ACCESSDATA_URL_ARCHIVE, playkey1, streamQuality));
                        Match c3 = regEx_GetAuthArchive.Match(accessData);
                        String servertype = c2.Groups["servertype"].Value; ;
                        String auth = null;
                        String aifp = null;
                        String stream = null;
                        if (c3.Success)
                        {
                            auth = c3.Groups["auth"].Value;
                            auth = auth.Replace("amp;", "");
                            auth = auth.Replace("e=", "e=" + playkey1);
                            aifp = c3.Groups["aifp"].Value;
                            stream = c3.Groups["stream"].Value;
                            c3 = c3.NextMatch();
                        }
                        else
                        {
                            Log.Warn("Couldn't parse " + accessData);
                        }

                        String ip = null;
                        String identData = GetWebData(String.Format(IDENT_URL, server));
                        Match c4 = regEx_GetIp.Match(identData);
                        if (c4.Success)
                        {
                            ip = c4.Groups["ip"].Value;
                            c4 = c4.NextMatch();
                        }

                        String url = String.Format("rtmp://{0}:1935/{1}?_fcs_vhost={2}&auth={3}&aifp={4}&slist={5}", ip, servertype, server, auth, aifp, stream);
                        MPUrlSourceFilter.RtmpUrl resultUrl = new MPUrlSourceFilter.RtmpUrl(url);
                        resultUrl.FlashVersion = flashVersion;
                        resultUrl.Live = false;
                        resultUrl.PageUrl = video.VideoUrl;
                        resultUrl.SwfUrl = flashplayer;

                        if (stream.EndsWith(".mp4"))
                        {
                            //for videos where the returned stream ends with .mp4, the laola1.tv server wants a play command like
                            //play('mp4:77154/flash/2011/volleyball/CEV_CL/111215_innsbruck_macerata_cut_high.mp4')
                            resultUrl.PlayPath = "mp4:" + stream;
                        }
                        else
                        {
                            resultUrl.PlayPath = stream;
                        }


                        return resultUrl.ToString();
                    }
                    c2 = c2.NextMatch();
                }
                if (!videoQualityFound)
                {
                    //this shouldn't happen, maybe the site has added/removed video qualities
                    Log.Warn("Couldn't find the video stream with quality " + videoQuality.ToString());
                }
            }

            //something has gone wrong -> return null
            return null;
        }

        public override string GetFileNameForDownload(VideoInfo video, Category category, string url)
        {
            Log.Info("Get download name for : " + video.VideoUrl);

            LaolaCategoryTypes type = (LaolaCategoryTypes)video.Other;
            if (type == LaolaCategoryTypes.Archive)
            {
                String title = video.Title + ".flv";
                return Helpers.FileUtils.GetSaveFilename(title);
            }
            else if (type == LaolaCategoryTypes.Live)
            {
                String title = "LIVE - " + video.Title + ".flv";
                return Helpers.FileUtils.GetSaveFilename(title);
            }

            return null;
        }
    }
}

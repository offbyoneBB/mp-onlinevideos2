using System;
using System.Text.RegularExpressions;
using System.Net;
using System.Text;
using System.Xml;
using System.IO;
using System.ComponentModel;
using System.Collections.Generic;
using Google.GData.Client;
using Google.GData.Extensions;
using Google.GData.YouTube;
using Google.GData.Extensions.MediaRss;
using MediaPortal.GUI.Library;

namespace OnlineVideos.Sites
{

    public class YouTubeUtil : SiteUtilBase, IFilter, IFavorite
    {
        public enum VideoQuality { Low, High, HD };

        [Category("OnlineVideosUserConfiguration"), Description("Defines the maximum quality for the video to be played.")]
        VideoQuality videoQuality = VideoQuality.High;
        [Category("OnlineVideosUserConfiguration"), Description("Your YouTube username. Used for favorites.")]
        string username = "";
        [Category("OnlineVideosUserConfiguration"), Description("Your YouTube password. Used for favorites.")]
        string password = "";

        [Category("OnlineVideosConfiguration"), Description("Add some dynamic categories found at startup to the list of configured ones.")]
        bool useDynamicCategories = true;

        static int[] fmtOptionsQualitySorted = new int[] { 37,22,35,18,34,5,0,17,13 };

        static Regex PageStartIndex = new Regex(@"start-index=(?<item>[\d]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        static Regex swfJsonArgs = new Regex(@"(?:var\s)?(?:swfArgs|'SWF_ARGS')\s*(?:=|\:)\s(?<json>\{.+\})", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private List<int> steps;
        private Dictionary<String, String> orderByList;
        private Dictionary<String, String> timeFrameList;
        private YouTubeQuery _LastPerformedQuery;
        private const String CLIENT_ID = "ytapi-GregZ-OnlineVideos-s2skvsf5-0";
        private const String DEVELOPER_KEY = "AI39si5x-6x0Nybb_MvpC3vpiF8xBjpGgfq-HTbyxWP26hdlnZ3bTYyERHys8wyYsbx3zc5f9bGYj0_qfybCp-wyBF-9R5-5kA";
        private const String FAVORITE_FEED = "http://gdata.youtube.com/feeds/api/users/{0}/favorites";
        private const String RELATED_VIDEO_FEED = "http://gdata.youtube.com/feeds/api/videos/{0}/related";
        private const String VIDEO_URL = "http://www.youtube.com/get_video?video_id={0}&t={1}";
        private const String CATEGORY_FEED = "http://gdata.youtube.com/feeds/api/videos/-/{0}";
        private const String USER_PLAYLISTS_FEED = "http://gdata.youtube.com/feeds/api/users/[\\w]+/playlists";
        private const String PLAYLIST_FEED = "http://gdata.youtube.com/feeds/api/playlists/{0}";
        
        private YouTubeService service;

        public YouTubeUtil()
        {
            steps = new List<int>();
            steps.Add(10);
            steps.Add(20);
            steps.Add(30);
            steps.Add(40);
            steps.Add(50);
            orderByList = new Dictionary<String, String>();
            orderByList.Add("Relevance", "relevance");
            orderByList.Add("Published", "published");
            orderByList.Add("View Count", "viewCount");
            orderByList.Add("Rating", "rating");

            timeFrameList = new Dictionary<string, string>();
            foreach(String name in Enum.GetNames(typeof(YouTubeQuery.UploadTime))){
                if(name.Equals("ThisWeek",StringComparison.InvariantCultureIgnoreCase)){
                    timeFrameList.Add("This Week",name);
                }else if(name.Equals("ThisMonth",StringComparison.InvariantCultureIgnoreCase)){
                    timeFrameList.Add("This Month",name);
                }else if(name.Equals("Today",StringComparison.InvariantCultureIgnoreCase)){
                    timeFrameList.Add("Today",name);
                }else if(name.Equals("AllTime",StringComparison.InvariantCultureIgnoreCase)){
                    timeFrameList.Add("All Time",name);
                }else{
                    timeFrameList.Add(name,name);
                }
            }            

            service = new YouTubeService("OnlineVideos", CLIENT_ID, DEVELOPER_KEY);
            

            //orderByList.Add("")
        }

        string nextPageUrl = "";
        string previousPageUrl = "";
        bool nextPageAvailable = false;
        bool previousPageAvailable = false;

        private CookieCollection moCookies;
        private Regex regexId = new Regex("/videos/(.+)");

        public override bool HasRelatedVideos
        {
            get { return true; }
        }

        public override List<VideoInfo> getRelatedVideos(VideoInfo video)
        {
            string fsId = video.VideoUrl;
            YouTubeQuery query = new YouTubeQuery(String.Format(RELATED_VIDEO_FEED, fsId));
            return parseGData(query);
        }

        protected List<VideoInfo> getSiteFavorites(String fsUser)
        {
            //http://www.youtube.com/api2_rest?method=%s&dev_id=7WqJuRKeRtc&%s"   # usage   base_api %( method, extra)   eg base_api %( youtube.videos.get_detail, video_id=yyPHkJMlD0Q)
            //String lsUrl = "http://www.youtube.com/api2_rest?method=youtube.users.list_favorite_videos&dev_id=7WqJuRKeRtc&user="+fsUser;
            YouTubeQuery query = new YouTubeQuery(String.Format(FAVORITE_FEED, fsUser));
            //return parseRestXML(lsUrl);
            return parseGData(query);
            //String lsXMLResponse = getHTMLData(lsUrl);
            //Log.Info(lsXMLResponse);
        }

        public List<VideoInfo> parseGData(YouTubeQuery query)
        {           

            YouTubeFeed feed = service.Query(query);

            List<VideoInfo> loRssItems = new List<VideoInfo>();

            // check for previous page link
            if (feed.PrevChunk != null)
            {
                previousPageAvailable = true;
                previousPageUrl = feed.PrevChunk;
            }
            else
            {
                previousPageAvailable = false;
                previousPageUrl = "";
            }

            // check for next page link
            if (feed.NextChunk != null)
            {
                nextPageAvailable = true;
                nextPageUrl = feed.NextChunk;
            }
            else
            {
                nextPageAvailable = false;
                nextPageUrl = "";
            }

            foreach (YouTubeEntry entry in feed.Entries)
            {
                loRssItems.Add(getVideoInfo(entry));
            }
            _LastPerformedQuery = query;

            return loRssItems;
        }

        public VideoInfo getVideoInfo(YouTubeEntry entry)
        {
            VideoInfo video = new VideoInfo();
            video.Other = entry;

            video.Description = entry.Media.Description != null ? entry.Media.Description.Value : "";
            int maxHeight = 0;
            foreach (MediaThumbnail thumbnail in entry.Media.Thumbnails)
            {
                if (Int32.Parse(thumbnail.Height) > maxHeight)
                {
                    video.ImageUrl = thumbnail.Url;
                }
            }
            video.Length = entry.Media.Duration != null ? entry.Media.Duration.Seconds : "";
            video.Title = entry.Title.Text;            
            video.VideoUrl = entry.Media.VideoId.Value;
            return video;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string fsUrl = ((RssLink)category).Url;

            if (fsUrl.StartsWith("fav:"))
            {
                return getSiteFavorites(fsUrl.Substring(4));
            }
            YouTubeQuery query = new YouTubeQuery(fsUrl);
            List<VideoInfo> loRssItemList = parseGData(query);
            //List<VideoInfo> loVideoList = new List<VideoInfo>();
            //VideoInfo video;			
            return loRssItemList;
        }

        public static String ConvertUrl(String youtubeUrl)
        {
            int p=youtubeUrl.LastIndexOf('/');
            p++;
            int q=youtubeUrl.IndexOf('&',p);
            if (q <0) q = youtubeUrl.Length;
            return ConvertUrl(youtubeUrl.Substring(p,q-p),VideoQuality.HD);
        }

        private static String ConvertUrl(String videoId, VideoQuality videoQuality)
        {
            Dictionary<string, string> Items = new Dictionary<string, string>();
            GetVideInfo(videoId, Items);

            string Token = "";
            string[] FmtMap = null;

            if (Items.ContainsKey("token"))
                Token = Items["token"];
            if (Token == "" && Items.ContainsKey("t"))
                Token = Items["t"];
            if (Token == "")
                return "";

            if (Items.ContainsKey("fmt_map"))
            {
                FmtMap = System.Web.HttpUtility.UrlDecode(Items["fmt_map"]).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                Array.Sort(FmtMap, new Comparison<string>(delegate(string a, string b)
                {
                    int a_i = int.Parse(a.Substring(0, a.IndexOf("/")));
                    int b_i = int.Parse(b.Substring(0, b.IndexOf("/")));
                    int index_a = Array.IndexOf(fmtOptionsQualitySorted, a_i);
                    int index_b = Array.IndexOf(fmtOptionsQualitySorted, b_i);
                    return index_b.CompareTo(index_a);
                }));
            }

            string lsUrl = "";
            if (FmtMap == null || FmtMap.Length == 0) // no or empty fmt_map
            {
                lsUrl = string.Format("http://youtube.com/get_video?video_id={0}&t={1}&ext=.flv", videoId, Token);
            }
            else if (FmtMap.Length == 1) // only one fmt_map option available
            {
                lsUrl = string.Format("http://youtube.com/get_video?video_id={0}&t={1}&ext=.{2}", videoId, Token, MapFtmValueToExtension(FmtMap[0]));
            }
            else if (videoQuality == VideoQuality.Low) //user wants low quality -> use first available option
            {
                lsUrl = string.Format("http://youtube.com/get_video?video_id={0}&t={1}&ext=.{2}", videoId, Token, MapFtmValueToExtension(FmtMap[0]));
            }
            else if (videoQuality == VideoQuality.HD) // take highest available quality
            {
                lsUrl = string.Format("http://youtube.com/get_video?video_id={0}&t={1}&fmt={2}&ext=.{3}", videoId, Token, FmtMap[FmtMap.Length - 1].Substring(0, FmtMap[FmtMap.Length - 1].IndexOf("/")), MapFtmValueToExtension(FmtMap[FmtMap.Length - 1]));
            }
            else // choose a high quality from options (highest below the HD formats (37 22)
            {
                int index = FmtMap.Length - 1;
                while (index > 0 && (FmtMap[index].StartsWith("37") || FmtMap[index].StartsWith("22"))) index--;
                lsUrl = string.Format("http://youtube.com/get_video?video_id={0}&t={1}&fmt={2}&ext=.{3}", videoId, Token, FmtMap[index].Substring(0, FmtMap[index].IndexOf("/")), MapFtmValueToExtension(FmtMap[index]));
            }

            return lsUrl;
        }

        public override String getUrl(VideoInfo foVideo)
        {
            String lsUrl = ConvertUrl(foVideo.VideoUrl,videoQuality);
            Log.Info("youtube video url={0}", lsUrl);
            return lsUrl;
        }

        static string MapFtmValueToExtension(string fmt)
        {
            // Note the following formats for reference (2009-10-16)
            // fmt=0  -> flv:  320x240 (flv1) / mp3 1.0 22KHz (same as fmt=5)
            // fmt=5  -> flv:  320x240 (flv1) / mp3 1.0 22KHz
            // fmt=13 -> 3gp:  176x144 (mpg4) / ??? 2.0  8KHz
            // fmt=17 -> 3gp:  176x144 (mpg4) / ??? 1.0 22KHz
            // fmt=18 -> mp4:  480x360 (H264) / AAC 2.0 44KHz
            // fmt=22 -> mp4: 1280x720 (H264) / AAC 2.0 44KHz
            // fmt=34 -> flv:  320x240 (flv?) / ??? 2.0 44KHz (default now)
            // fmt=35 -> flv:  640x380 (flv?) / ??? 2.0 44KHz
            // fmt=37 -> mp4: 1080p
            switch (fmt)
            {
                case "13":
                case "17":
                case "18":
                case "22":
                case "37":
                    return "mp4";
                default:
                    return "flv";
            }
        }
        
        public static void GetVideInfo(string videoId,Dictionary<string, string> Items )
        {
            WebClient client = new WebClient();
            client.CachePolicy = new System.Net.Cache.RequestCachePolicy();
            client.UseDefaultCredentials = true;
            client.Proxy.Credentials = CredentialCache.DefaultCredentials;
            try
            {                
                string contents = client.DownloadString(string.Format("http://youtube.com/get_video_info?video_id={0}", videoId));
                string[] elemest = (contents).Split('&');

                foreach (string s in elemest)
                {
                    Items.Add(s.Split('=')[0], s.Split('=')[1]);
                }

                if (Items["status"] == "fail")
                {
                    contents = client.DownloadString(string.Format("http://www.youtube.com/watch?v={0}", videoId));
                    Match m = swfJsonArgs.Match(contents);
                    if (m.Success)
                    {
                        Items.Clear();
                        object data = Jayrock.Json.Conversion.JsonConvert.Import(m.Groups["json"].Value);
                        foreach (string z in (data as Jayrock.Json.JsonObject).Names)
                        {
                            Items.Add(z,(data as Jayrock.Json.JsonObject)[z].ToString());
                        }
                    }
                }
            }
            catch
            {
            }
        }

        public bool login(String fsUser, String fsPassword)
        {
            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create("http://www.youtube.com/login?next=/");
            //HttpWebRequest Request = (HttpWebRequest)WebRequest.Create("https://www.google.com/youtube/accounts/ClientLogin");
            Request.Method = "POST";
            Request.ContentType = "application/x-www-form-urlencoded";
            Request.CookieContainer = new CookieContainer();


            Stream RequestStream = Request.GetRequestStream();
            ASCIIEncoding ASCIIEncoding = new ASCIIEncoding();
            //Byte [] PostData = ASCIIEncoding.GetBytes("username=" + fsUser +"&password="+ fsPassword);

            Byte[] PostData = ASCIIEncoding.GetBytes("current_form=loginForm&next=%%2F&username=" + fsUser + "&password=" + fsPassword + "&action_login=Log+In");
            //Byte [] PostData = ASCIIEncoding.GetBytes("Email="+fsUser+"&Passwd="+fsPassword+"&service=youtube&source=MP-OnlineVideos");
            RequestStream.Write(PostData, 0, PostData.Length);
            RequestStream.Close();
            HttpWebResponse response = (HttpWebResponse)Request.GetResponse();
            //StreamReader Reader  = new StreamReader(Request.GetResponse().GetResponseStream());
            //String ResultHTML = Reader.ReadToEnd();
            response.Cookies = Request.CookieContainer.GetCookies(Request.RequestUri);
            moCookies = response.Cookies;
            //	Log.Info("Found {0} cookies after login ",response.Cookies.Count);

            //foreach(Cookie cky in response.Cookies)
            //{
            //	Log.Info(cky.Name + " = " + cky.Value +" expires on "+cky.Expires);
            //}
            response.Close();
            return isLoggedIn();
        }

        private bool isLoggedIn()
        {
            return moCookies != null && moCookies["LOGIN_INFO"] != null;
        }
        
        public override int DiscoverDynamicCategories()
        {
            // walk the categories and see if there are user playlists - they need to be set to have subcategories
            foreach(RssLink link in Settings.Categories)
            {
                if (Regex.Match(link.Url, USER_PLAYLISTS_FEED).Success)
                {
                    link.HasSubCategories = true;
                    link.SubCategoriesDiscovered = false;
                }
            }

            if (!useDynamicCategories) return base.DiscoverDynamicCategories();

            Dictionary<String, String> categories = getYoutubeCategories();
            foreach (KeyValuePair<String, String> cat in categories)
            {
                RssLink item = new RssLink();
                item.Name = cat.Key;
                item.Url = String.Format(CATEGORY_FEED, cat.Value);
                Settings.Categories.Add(item);
            }
            Settings.DynamicCategoriesDiscovered = true;
            return categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            YouTubeQuery ytq = new YouTubeQuery((parentCategory as RssLink).Url) { NumberToRetrieve = 50 };
            YouTubeFeed feed = service.Query(ytq);
            foreach(PlaylistsEntry entry in feed.Entries)
            {
                RssLink playlistLink = new RssLink();
                playlistLink.Name = entry.Title.Text;
                playlistLink.EstimatedVideoCount = (uint)entry.CountHint;
                XmlExtension playlistExt = entry.FindExtension(YouTubeNameTable.PlaylistId, YouTubeNameTable.NSYouTube) as XmlExtension;                
                if (playlistExt != null)
                {
                    playlistLink.Url = string.Format(PLAYLIST_FEED, playlistExt.Node.InnerText);
                    parentCategory.SubCategories.Add(playlistLink);
                    playlistLink.ParentCategory = parentCategory;                    
                }                               
            }
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        private Dictionary<String, String> getYoutubeCategories()
        {
            Dictionary<String, String> categories = new Dictionary<string, string>();
            try
            {
                foreach (YouTubeCategory cat in YouTubeQuery.GetYouTubeCategories())
                {
                    if (cat.Assignable && ! cat.Deprecated)
                    {
                        categories.Add(cat.Label, cat.Term);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("OnlineVideos : Error retrieving YouTube Categories: " + ex.Message);
            }            
            return categories;
        }

        public override bool HasNextPage
        {
            get { return nextPageAvailable; }
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            YouTubeQuery query = _LastPerformedQuery;

            Match mIndex = PageStartIndex.Match(nextPageUrl);
            if (mIndex.Success)
            {
                query.StartIndex = Convert.ToInt16(mIndex.Groups["item"].Value);
            }

            return parseGData(query);
        }

        public override bool HasPreviousPage
        {
            get { return previousPageAvailable; }
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            YouTubeQuery query = _LastPerformedQuery;

            Match mIndex = PageStartIndex.Match(previousPageUrl);
            if (mIndex.Success)
            {
                query.StartIndex = Convert.ToInt16(mIndex.Groups["item"].Value);
            }

            return parseGData(query);
        }

        #region IFilter Members

        //private String buildFilterUrl(string catUrl, int maxResult, string orderBy, string timeFrame)
        //{
        //    if (catUrl == null)
        //    {
        //        catUrl = "";
        //    }
        //    String newCatUrl = catUrl;
        //    if (catUrl.IndexOf("time=", StringComparison.CurrentCultureIgnoreCase) > 0)
        //    {
        //        Regex timeRgx = new Regex(@"\?.*time=([^&]*)");
        //        newCatUrl = timeRgx.Replace(catUrl, new MatchEvaluator(delegate(Match match) { return timeFrame; }));
        //    }
        //    else
        //    {
        //        if (catUrl.Contains("?"))
        //        {
        //            newCatUrl += "&";
        //        }
        //        else
        //        {
        //            newCatUrl += "?";
        //        }
        //        newCatUrl += "time=" + timeFrame;
        //    }
        //    if (catUrl.IndexOf("orderby=", StringComparison.CurrentCultureIgnoreCase) > 0)
        //    {
        //        Regex timeRgx = new Regex(@"\?.*orderby=([^&]*)");
        //        newCatUrl = timeRgx.Replace(catUrl, new MatchEvaluator(delegate(Match match) { return orderBy; }));
        //    }
        //    else
        //    {
        //        newCatUrl += "&orderby=" + orderBy;
        //    }
        //    if (catUrl.IndexOf("max-results=", StringComparison.CurrentCultureIgnoreCase) > 0)
        //    {
        //        Regex timeRgx = new Regex(@"\?.*max-results=([^&]*)");
        //        newCatUrl = timeRgx.Replace(catUrl, new MatchEvaluator(delegate(Match match) { return maxResult + ""; }));
        //    }
        //    else
        //    {
        //        newCatUrl += "&max-results=" + maxResult;
        //    }
        //    return newCatUrl;
        //}

        public List<VideoInfo> filterVideoList(Category category, int maxResult, string orderBy, string timeFrame)
        {
            YouTubeQuery query = _LastPerformedQuery;
            query.StartIndex = 1;
            query.NumberToRetrieve = maxResult;
            query.OrderBy = orderBy;

            ///-------------------------------------------------------------------------------------------------
            /// 2009-06-09 MichelC
            /// Youtube doesn't allow the following parameter for Recently Featured clips and return and error.
            ///-------------------------------------------------------------------------------------------------
            if (category.Name != "Recently Featured")
            {
                if (Enum.IsDefined(typeof(YouTubeQuery.UploadTime), timeFrame))
                {
                    query.Time = (YouTubeQuery.UploadTime)Enum.Parse(typeof(YouTubeQuery.UploadTime), timeFrame, true);
                }
            }

            return parseGData(query);
            //String filteredUrl = buildFilterUrl(catUrl, maxResult, orderBy, timeFrame); 
            //Log.Info("Youtube Filtered url:" + filteredUrl);
            //return getVideoList(filteredUrl);
        }

        public List<VideoInfo> filterSearchResultList(string queryStr, int maxResult, string orderBy, string timeFrame)
        {
            //String filteredUrl = buildFilterUrl(buildSearchUrl(query,String.Empty), maxResult, orderBy, timeFrame);
            //Log.Info("Youtube Filtered url:" + filteredUrl);
            //return getVideoList(filteredUrl);
            YouTubeQuery query = _LastPerformedQuery;
            query.StartIndex = 1;
            query.NumberToRetrieve = maxResult;
            query.OrderBy = orderBy;
            if (Enum.IsDefined(typeof(YouTubeQuery.UploadTime), timeFrame))
            {
                query.Time = (YouTubeQuery.UploadTime)Enum.Parse(typeof(YouTubeQuery.UploadTime), timeFrame, true);
            }
            return parseGData(query);
        }

        public List<VideoInfo> filterSearchResultList(string queryStr, string category, int maxResult, string orderBy, string timeFrame)
        {
            //String filteredUrl = buildFilterUrl(buildSearchUrl(query, category), maxResult, orderBy, timeFrame);
            //Log.Info("Youtube Filtered url:" + filteredUrl);
            //return getVideoList(filteredUrl);
            YouTubeQuery query = _LastPerformedQuery;
            query.StartIndex = 1;
            query.NumberToRetrieve = maxResult;
            query.OrderBy = orderBy;
            if (Enum.IsDefined(typeof(YouTubeQuery.UploadTime), timeFrame))
            {
                query.Time = (YouTubeQuery.UploadTime)Enum.Parse(typeof(YouTubeQuery.UploadTime), timeFrame, true);
            }
            return parseGData(query);
        } 

        public List<int> getResultSteps()
        {
            return steps;
        }

        public Dictionary<string, String> getOrderbyList()
        {
            return orderByList;
        }

        public Dictionary<string, String> getTimeFrameList()
        {
            return timeFrameList;
        }

        #endregion

        #region Search

        public override bool CanSearch { get { return true; } }

        Dictionary<string, string> cachedSearchCategories = null;
        public override Dictionary<string, string> GetSearchableCategories()
        {
            if (cachedSearchCategories == null) cachedSearchCategories = getYoutubeCategories();
            return cachedSearchCategories;
        }

        public override List<VideoInfo> Search(string queryStr)
        {
            YouTubeQuery query = new YouTubeQuery(YouTubeQuery.DefaultVideoUri);
            query.Query = queryStr;           
            List<VideoInfo> loRssItemList = parseGData(query);            
            return loRssItemList;
            
        }
        
        //private String buildSearchUrl(string query, string category)
        //{
        //    String searchUrl;
        //    if (!String.IsNullOrEmpty(category))
        //    {
        //        searchUrl = String.Format("http://gdata.youtube.com/feeds/api/videos?vq={0}&category={1}", query, category);
        //    }
        //    else
        //    {
        //        searchUrl = String.Format("http://gdata.youtube.com/feeds/api/videos?vq={0}", query);
        //    }
        //    return searchUrl;

        //}

        public override List<VideoInfo> Search(string queryStr, string category)
        {
            YouTubeQuery query = new YouTubeQuery(YouTubeQuery.DefaultVideoUri);
            query.Query = queryStr;  
            AtomCategory category1 = new AtomCategory(category, YouTubeNameTable.CategorySchema);
            query.Categories.Add(new QueryCategory(category1));            
            
            List<VideoInfo> loRssItemList = parseGData(query);
            return loRssItemList;
        }

        #endregion

        #region IFavorite Members

        public List<VideoInfo> getFavorites()
        {
            if (string.IsNullOrEmpty(username)) return new List<VideoInfo>();

            //service.setUserCredentials(fsUsername,fsPassword);
            
            YouTubeQuery query =new YouTubeQuery(String.Format(FAVORITE_FEED, username));           
        
            return parseGData(query);
        }               

        public void addFavorite(VideoInfo video)
        {
            if (CheckUsernameAndPassword())
            {
                service.setUserCredentials(username, password);
                YouTubeEntry entry = (YouTubeEntry)video.Other;
                service.Insert(new Uri(String.Format(FAVORITE_FEED, username)), entry);
                //    String lsPostUrl = "http://gdata.youtube.com/feeds/api/users/default/favorites";
                //    String authToken = getAuthToken(fsUsername, fsPassword);
                //    HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(lsPostUrl);
                //    Request.Method = "POST";
                //    Request.ContentType = "application/atom+xml";
                //    Request.Headers.Add(
                //        HttpRequestHeader.Authorization, "GoogleLogin auth=" + authToken);
                //    Request.Headers.Add("X-GData-Client: " + CLIENT_ID);
                //    Request.Headers.Add("X-GData-Key: key=" + DEVELOPER_KEY);
                //    Request.Headers.Add("GData-Version","2");
                //    ASCIIEncoding ASCIIEncoding = new ASCIIEncoding();
                //    Byte [] PostData = ASCIIEncoding.GetBytes(String.Format("<?xml version=\"1.0\" encoding=\"UTF-8\"?><entry xmlns=\"http://www.w3.org/2005/Atom\"><id>{0}</id></entry>","J2N0t4OEUbc"));
                //     Stream RequestStream = Request.GetRequestStream();
                //    RequestStream.Write(PostData, 0, PostData.Length);
                //    RequestStream.Close();


                //HttpWebResponse response = (HttpWebResponse)Request.GetResponse();
                //StreamReader Reader  = new StreamReader(response.GetResponseStream());
                //String lsResponse = Reader.ReadToEnd();
                //Log.Info("Youtube authorization token:"+lsResponse);
                //response.Close();
                //throw new Exception("");
            }
        }

        public void removeFavorite(VideoInfo video)
        {
            if (CheckUsernameAndPassword())
            {
                service.setUserCredentials(username, password);
                ((YouTubeEntry)video.Other).Delete();
                //String lsPostUrl = String.Format("http://gdata.youtube.com/feeds/api/users/{0}/favorites/{1}", fsUsername, "vjVQa1PpcFOKheU6YrMZmZ6GRqLUdhAz8qZtu8cCzBs");
                //String authToken = getAuthToken(fsUsername, fsPassword);
                //HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(lsPostUrl);
                //Request.Method = "DELETE";
                //Request.ContentType = "application/atom+xml";
                //Request.Headers.Add(
                //    HttpRequestHeader.Authorization, "GoogleLogin auth=" + authToken);
                //Request.Headers.Add("X-GData-Client: " + CLIENT_ID);
                //Request.Headers.Add("X-GData-Key: key=" + DEVELOPER_KEY);
                //Request.Headers.Add("GData-Version", "2");

                //HttpWebResponse response = (HttpWebResponse)Request.GetResponse();
                //StreamReader Reader = new StreamReader(response.GetResponseStream());
                //String lsResponse = Reader.ReadToEnd();
                //Log.Info("Youtube authorization token:" + lsResponse);
                //response.Close();
            }
        }

        bool CheckUsernameAndPassword()
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(username))
            {
                MediaPortal.Dialogs.GUIDialogOK dlg_error = (MediaPortal.Dialogs.GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
                dlg_error.SetHeading("YouTube");
                dlg_error.SetLine(1, "Please set your username and password in the Configuration");
                dlg_error.SetLine(2, String.Empty);
                dlg_error.DoModal(GUIWindowManager.ActiveWindow);

                return false;
            }
            else
            {
                return true;
            }
        }

        #endregion

        //private String getAuthToken(String fsUsername, String fsPassword)
        //{
        //    String lsClientLoginUrl = "https://www.google.com/youtube/accounts/ClientLogin";
        //    HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(lsClientLoginUrl);
        //    Request.Method = "POST";
        //    Request.ContentType = "application/x-www-form-urlencoded";
        //    //Request.CookieContainer = new CookieContainer();
			
        //    //Request.CookieContainer.Add(moCookies);
			
        //    //Stream RequestStream  = Request.GetRequestStream();
        //    ASCIIEncoding ASCIIEncoding  =  new ASCIIEncoding();
            
        //    Byte [] PostData = ASCIIEncoding.GetBytes(
        //        "Email="+fsUsername+
        //        "&Passwd="+fsPassword+
        //        "&service=youtube"+
        //        "&source=OnlineVideos");
        //    /*
                //    Stream RequestStream = Request.GetRequestStream();
        //    RequestStream.Write(PostData, 0, PostData.Length);
        //    RequestStream.Close();
            
        
        //HttpWebResponse response = (HttpWebResponse)Request.GetResponse();
        //StreamReader Reader  = new StreamReader(response.GetResponseStream());
        //String lsResponse = Reader.ReadToEnd();
        //Log.Info("Youtube authorization token:"+lsResponse);
        //response.Close();
        //Regex authRegex = new Regex("Auth=([^\n]*)");
        //return authRegex.Match(lsResponse).Groups[1].Value;

        ////return lsResponse;
        //}

    }

}

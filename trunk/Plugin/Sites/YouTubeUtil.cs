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
    public class YouTubeUtil : SiteUtilBase, IFilter, ISearch, IFavorite
    {
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
        private const String ALL_CATEGORIES_FEED = "http://gdata.youtube.com/schemas/2007/categories.cat?hl=en-US";
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
        private CookieCollection moCookies;
        private Regex regexId = new Regex("/videos/(.+)");

        public override bool hasLoginSupport()
        {
            return true;
        }

        public override List<OnlineVideos.VideoInfo> getRelatedVideos(string fsId)
        {
            YouTubeQuery query = new YouTubeQuery(String.Format(RELATED_VIDEO_FEED, fsId));
            return parseGData(query);
        }


        public override List<VideoInfo> getSiteFavorites(String fsUser)
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
            
            video.Description = entry.Media.Description.Value;
            int maxHeight = 0;
            foreach (MediaThumbnail thumbnail in entry.Media.Thumbnails)
            {
                if (Int32.Parse(thumbnail.Height) > maxHeight)
                {
                    video.ImageUrl = thumbnail.Url;
                }
            }
            video.Length = entry.Media.Duration.Seconds;
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
        public override String getUrl(VideoInfo foVideo, SiteSettings foSite)
        {
            String lsSessionId = getSessionId(foVideo.VideoUrl, foSite);
            String lsUrl = String.Format(VIDEO_URL,foVideo.VideoUrl ,lsSessionId);
            Log.Info("youtube video url={0}", lsUrl);
            
            return lsUrl + "&txe=.flv";
        }
        public String getSessionId(String fsId, SiteSettings foSite)
        {
            //Log.Info("getting youtube session id");
            String lsUrl;
            String lsNextUrl = "";
            String lsPostData = "";
            if (foSite.ConfirmAge && !String.IsNullOrEmpty(foSite.Username) && !String.IsNullOrEmpty(foSite.Password))
            {
                Log.Info("confirmAge is set to yes");
                lsUrl = "http://www.youtube.com/verify_age?next_url=/watch?v=" + fsId;
                lsNextUrl = "/watch?v=" + fsId;
                lsPostData = "next_url=" + lsNextUrl + "&action_confirm=Confirm+Birth+Date";
                if (!isLoggedIn())
                {
                    Log.Info("Not currently logged in. Trying to log in");
                    //try to login
                    if (login(foSite.Username, foSite.Password))
                    {
                        Log.Info("logged in successfully");
                        //foreach(Cookie cookie in moCookies){
                        //    Log.Info("Found cookie:" + cookie.Name);
                        //}

                    }
                    else
                    {
                        Log.Info("login failed");
                    }
                }
            }
            else
            {
                lsUrl = "http://www.youtube.com/watch?v=" + fsId;
            }

            //String lsHtml = getHTMLData(lsUrl);
            //WebClient loClient1 = new WebClient();
            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(lsUrl);
            Request.Method = "POST";
            Request.ContentType = "application/x-www-form-urlencoded";

            Request.CookieContainer = new CookieContainer();
            if (moCookies != null)
            {
                Log.Info("setting the cookies for the request");
                Request.CookieContainer.Add(moCookies);
            }
            Stream RequestStream = Request.GetRequestStream();
            ASCIIEncoding ASCIIEncoding = new ASCIIEncoding();
            //Byte [] PostData = ASCIIEncoding.GetBytes("username=" + fsUser +"&password="+ fsPassword);
            Byte[] PostData = ASCIIEncoding.GetBytes(lsPostData);
            RequestStream.Write(PostData, 0, PostData.Length);
            RequestStream.Close();
            HttpWebResponse response = (HttpWebResponse)Request.GetResponse();
            StreamReader Reader = new StreamReader(Request.GetResponse().GetResponseStream());
            String lsHtml = Reader.ReadToEnd();
            //Log.Info("Session Html:{0}",lsHtml);            



            //String lsHtml = getHTMLData(lsUrl);
            Regex loRegex;
            Match loMatch;
            String session;          
            loRegex = new Regex(@"fullscreenUrl\s=\s'.*&t=([^&]*)");

            //}
            loMatch = loRegex.Match(lsHtml);
            session = loMatch.Groups[1].Value;
            Log.Info("Session id={0}", session);

            //Log.Info("finished getting youtube session id");
            return session;
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
       
        
        public override List<Category> getDynamicCategories()
        {
            List<Category> result = new List<Category>();
            Dictionary<String, String> categories = getYoutubeCategories();            
            Log.Info("Youtube - dynamic Categories: " + categories.Count);
            foreach (KeyValuePair<String, String> cat in categories)
            {
                RssLink item = new RssLink();
                item.Name = cat.Key;
                item.Url = String.Format(CATEGORY_FEED, cat.Value);
                result.Add(item);
                Log.Info("Found category: " + cat.Value);
            }            
            result.Sort();            
            return result;
        }
        //private Dictionary<String, String> getYoutubeCategories()
        //{
        //    YouTubeQuery query = new YouTubeQuery(ALL_CATEGORIES_FEED);
        //    YouTubeFeed feed = service.Query(query);
        //    Dictionary<String, String> categories = new Dictionary<string, string>();
        //    foreach (YouTubeCategory category in feed.Categories)
        //    {
        //        categories.Add(category.Label,category.Term);   
        //    }
        //    return categories;
        //}
        private Dictionary<String, String> getYoutubeCategories()
        {
            XmlDocument doc = new XmlDocument();

            try
            {
                doc.Load(XmlReader.Create("http://gdata.youtube.com/schemas/2007/categories.cat?hl=en-US"));
            }
            catch
            {

                return null;
            }
            XmlNamespaceManager nsMgr = new XmlNamespaceManager(doc.NameTable);
            nsMgr.AddNamespace("atom", "http://www.w3.org/2005/Atom");
            XmlNodeList nodeList = doc.SelectNodes("/*/atom:category", nsMgr);
            Log.Info("Youtube - dynamic Categories: " + nodeList.Count);
            Dictionary<String, String> categories = new Dictionary<string, string>();
            foreach (XmlNode node in nodeList)
            {
                categories.Add(node.Attributes["label"].Value, node.Attributes["term"].Value);
            }
            return categories;
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
            query.NumberToRetrieve = maxResult;
            query.OrderBy = orderBy;
            if (Enum.IsDefined(typeof(YouTubeQuery.UploadTime), timeFrame)){
                query.Time = (YouTubeQuery.UploadTime)Enum.Parse(typeof(YouTubeQuery.UploadTime), timeFrame, true);                 
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

        #region ISearch Members

        public Dictionary<string, string> getSearchableCategories()
        {
            return getYoutubeCategories();            
        }

        public List<VideoInfo> search(string searchUrl, string queryStr)
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
        public List<VideoInfo> search(string searchUrl, string queryStr, string category)
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

        public List<VideoInfo> getFavorites(string fsUsername, string fsPassword)
        {
            if (fsUsername == null || fsUsername.Trim() == string.Empty) return new List<VideoInfo>();

            //service.setUserCredentials(fsUsername,fsPassword);
            
            YouTubeQuery query =new YouTubeQuery(String.Format(FAVORITE_FEED,fsUsername));           
        
            return parseGData(query);
        }               

        public void addFavorite(VideoInfo video, string fsUsername, string fsPassword)
        {
            service.setUserCredentials(fsUsername, fsPassword);
            YouTubeEntry entry = (YouTubeEntry)video.Other;
            service.Insert(new Uri(String.Format(FAVORITE_FEED, fsUsername)), entry);
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
        public void removeFavorite(VideoInfo video, string fsUsername, string fsPassword)
        {
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

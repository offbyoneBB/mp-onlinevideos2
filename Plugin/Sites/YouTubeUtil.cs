using System;
using System.Text.RegularExpressions;
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
        [Category("OnlineVideosUserConfiguration"), Description("Defines the default number of videos to display per page.")]
        int pageSize = 27;

        [Category("OnlineVideosConfiguration"), Description("Add some dynamic categories found at startup to the list of configured ones.")]
        bool useDynamicCategories = true;
        
        static Regex swfJsonArgs = new Regex(@"(?:var\s)?(?:swfArgs|'SWF_ARGS')\s*(?:=|\:)\s(?<json>\{.+\})", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private YouTubeService service;
        private List<int> steps = new List<int>() { 10, 20, 30, 40, 50 };
        private Dictionary<String, String> orderByList;
        private Dictionary<String, String> timeFrameList = new Dictionary<string, string>();
        private YouTubeQuery lastPerformedQuery;

        static readonly int[] fmtOptionsQualitySorted = new int[] { 37, 22, 35, 18, 34, 5, 0, 17, 13 };

        const string CLIENT_ID = "ytapi-GregZ-OnlineVideos-s2skvsf5-0";
        const string DEVELOPER_KEY = "AI39si5x-6x0Nybb_MvpC3vpiF8xBjpGgfq-HTbyxWP26hdlnZ3bTYyERHys8wyYsbx3zc5f9bGYj0_qfybCp-wyBF-9R5-5kA";        
        const string USER_PLAYLISTS_FEED = "http://gdata.youtube.com/feeds/api/users/[\\w]+/playlists";
        const string PLAYLIST_FEED = "http://gdata.youtube.com/feeds/api/playlists/{0}";                
        
        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            orderByList = new Dictionary<String, String>() {{"Relevance", "relevance"},
                                                            {"Published", "published"},
                                                            {"View Count", "viewCount"},
                                                            {"Rating", "rating"}};
            foreach (string name in Enum.GetNames(typeof(YouTubeQuery.UploadTime))) timeFrameList.Add(Utils.ToFriendlyCase(name), name);
            service = new YouTubeService("OnlineVideos", CLIENT_ID, DEVELOPER_KEY);
        }

        public override bool HasRelatedVideos { get { return true; } }

        public override List<VideoInfo> getRelatedVideos(VideoInfo video)
        {
            YouTubeQuery query = new YouTubeQuery() { Uri = new Uri((video.Other as YouTubeEntry).RelatedVideosUri.Content), NumberToRetrieve = pageSize };
            return parseGData(query);
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string fsUrl = ((RssLink)category).Url;
            YouTubeQuery query;
            if (fsUrl.StartsWith("fav:")) query = new YouTubeQuery() { Uri = new Uri(YouTubeQuery.CreateFavoritesUri(fsUrl.Substring(4))) };
            else query = new YouTubeQuery() { Uri = new Uri(fsUrl) };
            query.NumberToRetrieve = pageSize;
            return parseGData(query);
        }

        public override String getUrl(VideoInfo foVideo)
        {
            return ConvertUrl(foVideo.VideoUrl, videoQuality, out foVideo.PlaybackOptions);            
        }

        public override int DiscoverDynamicCategories()
        {
            // walk the categories and see if there are user playlists - they need to be set to have subcategories
            foreach (Category link in Settings.Categories)
            {
                if ((link is RssLink) && Regex.Match(((RssLink)link).Url, USER_PLAYLISTS_FEED).Success)
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
                YouTubeQuery query = new YouTubeQuery(YouTubeQuery.DefaultVideoUri);
                query.Categories.Add(new QueryCategory(cat.Value, QueryCategoryOperator.AND));
                item.Url = query.Uri.ToString();
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
            foreach (PlaylistsEntry entry in feed.Entries)
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
       
        List<VideoInfo> parseGData(YouTubeQuery query)
        {
            List<VideoInfo> loRssItems = new List<VideoInfo>();

            YouTubeFeed feed = service.Query(query);
            hasPreviousPage = !string.IsNullOrEmpty(feed.PrevChunk);
            hasNextPage = !string.IsNullOrEmpty(feed.NextChunk);
            foreach (YouTubeEntry entry in feed.Entries)
            {
                VideoInfo video = new VideoInfo();
                video.Other = entry;
                video.Description = entry.Media.Description != null ? entry.Media.Description.Value : "";
                // get the largest thumbnail
                int maxHeight = 0; 
                foreach (MediaThumbnail thumbnail in entry.Media.Thumbnails)
                {
                    int height = int.Parse(thumbnail.Height);
                    if (height > maxHeight)
                    {
                        video.ImageUrl = thumbnail.Url;
                        maxHeight = height;
                    }
                }
                video.Length = entry.Media.Duration != null ? TimeSpan.FromSeconds(int.Parse(entry.Media.Duration.Seconds)).ToString() : "";
                video.Length += (video.Length != "" ? " | " : "") + entry.Published.ToString("g");
                video.Title = entry.Title.Text;
                video.VideoUrl = entry.Media.VideoId.Value;
                loRssItems.Add(video);
            }
            lastPerformedQuery = query;

            return loRssItems;
        }

        Dictionary<string, string> getYoutubeCategories()
        {
            Dictionary<String, String> categories = new Dictionary<string, string>();
            try
            {
                foreach (YouTubeCategory cat in YouTubeQuery.GetYouTubeCategories())
                {
                    if (cat.Assignable && !cat.Deprecated)
                    {
                        categories.Add(cat.Label, cat.Term);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error retrieving YouTube Categories: " + ex.Message);
            }
            return categories;
        }

        public static string ConvertUrl(string youtubeUrl)
        {
            int p=youtubeUrl.LastIndexOf('/');
            p++;
            int q=youtubeUrl.IndexOf('&',p);
            if (q <0) q = youtubeUrl.Length;
            Dictionary<string, string> playbackOptions = null;
            return ConvertUrl(youtubeUrl.Substring(p,q-p),VideoQuality.HD, out playbackOptions);
        }

        static string ConvertUrl(string videoId, VideoQuality videoQuality, out Dictionary<string, string> playbackOptions)
        {
            playbackOptions = null;

            Dictionary<string, string> Items = new Dictionary<string, string>();
            GetVideoParamHash(videoId, Items);

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
            playbackOptions = FmtMapToPlaybackOptions(FmtMap, Token, videoId);
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

        static Dictionary<string, string> FmtMapToPlaybackOptions(string[] fmtMap, string token, string videoId)
        {
            if (fmtMap == null || fmtMap.Length < 2) return null;

            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (string fmtValue in fmtMap)
            {
                int fmtValueInt = int.Parse(fmtValue.Substring(0, fmtValue.IndexOf("/")));
                switch (fmtValueInt)
                {
                    case 0:
                    case 5:
                    case 34:
                        result.Add("320x240 | (" + fmtValueInt.ToString().PadLeft(2, ' ') + ") | .flv", string.Format("http://youtube.com/get_video?video_id={0}&t={1}&fmt={2}&ext=.{3}", videoId, token, fmtValueInt, "flv")); break;
                    case 13:
                    case 17:
                        result.Add("176x144 | (" + fmtValueInt.ToString().PadLeft(2, ' ') + ") | .mp4", string.Format("http://youtube.com/get_video?video_id={0}&t={1}&fmt={2}&ext=.{3}", videoId, token, fmtValueInt, "mp4")); break;
                    case 18:
                        result.Add("480x360 | (" + fmtValueInt.ToString().PadLeft(2, ' ') + ") | .mp4", string.Format("http://youtube.com/get_video?video_id={0}&t={1}&fmt={2}&ext=.{3}", videoId, token, fmtValueInt, "mp4")); break;
                    case 35:
                        result.Add("640x480 | (" + fmtValueInt.ToString().PadLeft(2, ' ') + ") | .flv", string.Format("http://youtube.com/get_video?video_id={0}&t={1}&fmt={2}&ext=.{3}", videoId, token, fmtValueInt, "flv")); break;
                    case 22:
                        result.Add("1280x720 | (" + fmtValueInt.ToString().PadLeft(2, ' ') + ") | .mp4", string.Format("http://youtube.com/get_video?video_id={0}&t={1}&fmt={2}&ext=.{3}", videoId, token, fmtValueInt, "mp4")); break;
                    case 37:
                        result.Add("1920x1080 | (" + fmtValueInt.ToString().PadLeft(2, ' ') + ") | .mp4", string.Format("http://youtube.com/get_video?video_id={0}&t={1}&fmt={2}&ext=.{3}", videoId, token, fmtValueInt, "mp4")); break;
                    default:
                        result.Add("Unknown | (" + fmtValueInt.ToString().PadLeft(2, ' ') + ") | .???", string.Format("http://youtube.com/get_video?video_id={0}&t={1}&fmt={2}", videoId, token, fmtValueInt)); break;
                }                
            }
            return result;
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
            // fmt=35 -> flv:  640x480 (flv?) / ??? 2.0 44KHz
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
        
        static void GetVideoParamHash(string videoId, Dictionary<string, string> Items)
        {            
            try
            {                
                string contents = GetWebData(string.Format("http://youtube.com/get_video_info?video_id={0}", videoId));
                foreach (string s in contents.Split('&')) Items.Add(s.Split('=')[0], s.Split('=')[1]);
                if (Items["status"] == "fail")
                {
                    contents = GetWebData(string.Format("http://www.youtube.com/watch?v={0}", videoId));
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
            catch {}
        }

        #region Paging

        bool hasNextPage;
        bool hasPreviousPage;

        public override bool HasNextPage
        {
            get { return hasNextPage; }
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            lastPerformedQuery.StartIndex += lastPerformedQuery.NumberToRetrieve;
            return parseGData(lastPerformedQuery);
        }

        public override bool HasPreviousPage
        {
            get { return hasPreviousPage; }
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            lastPerformedQuery.StartIndex -= lastPerformedQuery.NumberToRetrieve;
            return parseGData(lastPerformedQuery);
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
            YouTubeQuery query = new YouTubeQuery(YouTubeQuery.DefaultVideoUri) { NumberToRetrieve = pageSize };
            query.Query = queryStr;
            return parseGData(query);            
        }        
        public override List<VideoInfo> Search(string queryStr, string category)
        {
            YouTubeQuery query = new YouTubeQuery(YouTubeQuery.DefaultVideoUri) { NumberToRetrieve = pageSize };
            query.Categories.Add(new QueryCategory(category, QueryCategoryOperator.AND));
            query.Query = queryStr;            
            return parseGData(query);
        }

        #endregion

        #region IFavorite Members

        public List<VideoInfo> getFavorites()
        {
            if (string.IsNullOrEmpty(username)) return new List<VideoInfo>();
            YouTubeQuery query = new YouTubeQuery() { Uri = new Uri(YouTubeQuery.CreateFavoritesUri(username)), NumberToRetrieve = pageSize };
            return parseGData(query);
        }               

        public void addFavorite(VideoInfo video)
        {
            if (CheckUsernameAndPassword())
            {
                service.setUserCredentials(username, password);
                YouTubeEntry entry = (YouTubeEntry)video.Other;
                service.Insert(new Uri(YouTubeQuery.CreateFavoritesUri(username)), entry);                
            }
        }

        public void removeFavorite(VideoInfo video)
        {
            if (CheckUsernameAndPassword())
            {
                service.setUserCredentials(username, password);
                ((YouTubeEntry)video.Other).Delete();               
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

        #region IFilter Members

        public List<VideoInfo> filterVideoList(Category category, int maxResult, string orderBy, string timeFrame)
        {
            lastPerformedQuery.StartIndex = 1;
            lastPerformedQuery.NumberToRetrieve = maxResult;
            lastPerformedQuery.OrderBy = orderBy;
            // Youtube doesn't allow the following parameter for Recently Featured clips and returns an error
            if (category.Name != "Recently Featured")
            {
                lastPerformedQuery.Time = (YouTubeQuery.UploadTime)Enum.Parse(typeof(YouTubeQuery.UploadTime), timeFrame, true);
            }
            return parseGData(lastPerformedQuery);
        }

        public List<VideoInfo> filterSearchResultList(string queryStr, int maxResult, string orderBy, string timeFrame)
        {
            lastPerformedQuery.StartIndex = 1;
            lastPerformedQuery.NumberToRetrieve = maxResult;
            lastPerformedQuery.OrderBy = orderBy;
            lastPerformedQuery.Time = (YouTubeQuery.UploadTime)Enum.Parse(typeof(YouTubeQuery.UploadTime), timeFrame, true);
            return parseGData(lastPerformedQuery);
        }

        public List<VideoInfo> filterSearchResultList(string queryStr, string category, int maxResult, string orderBy, string timeFrame)
        {
            lastPerformedQuery.StartIndex = 1;
            lastPerformedQuery.NumberToRetrieve = maxResult;
            lastPerformedQuery.OrderBy = orderBy;
            lastPerformedQuery.Time = (YouTubeQuery.UploadTime)Enum.Parse(typeof(YouTubeQuery.UploadTime), timeFrame, true);
            return parseGData(lastPerformedQuery);
        }

        public List<int> getResultSteps() { return steps; }

        public Dictionary<string, String> getOrderbyList() { return orderByList; }

        public Dictionary<string, String> getTimeFrameList() { return timeFrameList; }

        #endregion
    }
}

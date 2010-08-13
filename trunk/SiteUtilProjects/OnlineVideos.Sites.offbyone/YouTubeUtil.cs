using System;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Collections.Generic;
using Google.GData.Client;
using Google.GData.Extensions;
using Google.GData.YouTube;
using Google.GData.Extensions.MediaRss;

namespace OnlineVideos.Sites
{
    public class YouTubeUtil : SiteUtilBase, IFilter, IFavorite
    {
        public enum VideoQuality { Low, High, HD };

        /// <summary>
        /// http://code.google.com/intl/de-DE/apis/youtube/2.0/reference.html#Standard_feeds
        /// </summary>
        public enum StandartFeedCountry 
        {
            [Description("Australia")]AU,
            [Description("Brazil")]BR,
            [Description("Canada")]CA,
            [Description("Czech Republic")]CZ,
            [Description("France")]FR,
            [Description("Germany")]DE,
            [Description("Great Britain")]GB,
            [Description("Holland")]NL,
            [Description("Hong Kong")]HK,
            [Description("India")]IN,
            [Description("Ireland")]IE,
            [Description("Israel")]IL,
            [Description("Italy")]IT,
            [Description("Japan")]JP,
            [Description("Mexico")]MX,
            [Description("New Zealand")]NZ,
            [Description("Poland")]PL,
            [Description("Russia")]RU,
            [Description("South Korea")]KR,
            [Description("Spain")]ES,
            [Description("Sweden")]SE,
            [Description("Taiwan")]TW,
            [Description("United States")]US 
        }
        
        /// <summary>
        /// http://code.google.com/intl/de-DE/apis/youtube/2.0/reference.html#Localized_Category_Lists
        /// </summary>
        public enum CategoryLocale
        {
            [Description("Chinese")]zh,
            [Description("Czech")]cs,
            [Description("Dutch")]nl,            
            [Description("English")]en,
            [Description("French")]fr,
            [Description("German")]de,
            [Description("Italian")]it, 
            [Description("Japanese")]ja,
            [Description("Korean")]ko,
            [Description("Polish")]pl,
            [Description("Portuguese")]pt,
            [Description("Russian")]ru,
            [Description("Spanish")]es,
            [Description("Swedish")]sv
        }

        [Category("OnlineVideosUserConfiguration"), Description("Defines the maximum quality for the video to be played.")]
        VideoQuality videoQuality = VideoQuality.High;
        [Category("OnlineVideosUserConfiguration"), Description("Your YouTube username. Used for favorites.")]
        string username = "";
        [Category("OnlineVideosUserConfiguration"), Description("Your YouTube password. Used for favorites.")]
        string password = "";
        [Category("OnlineVideosUserConfiguration"), Description("Defines the default number of videos to display per page.")]
        int pageSize = 27;
        [Category("OnlineVideosUserConfiguration"), Description("Try to retrieve data specific for your region.")]
        bool localize = false;

        [Category("OnlineVideosConfiguration"), Description("Add some dynamic categories found at startup to the list of configured ones.")]
        bool useDynamicCategories = true;               

        private YouTubeService service;
        private List<int> steps = new List<int>() { 10, 20, 30, 40, 50 };
        private Dictionary<String, String> orderByList;
        private Dictionary<String, String> timeFrameList = new Dictionary<string, string>();
        private YouTubeQuery lastPerformedQuery;        

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

        public override string getUrl(VideoInfo foVideo)
        {
            foVideo.GetYouTubePlaybackOptions();

            if (foVideo.PlaybackOptions == null || foVideo.PlaybackOptions.Count == 0)
            {
                return ""; // no url to play available
            }
            else if (foVideo.PlaybackOptions.Count == 1 || videoQuality == VideoQuality.Low)
            {
                //user wants low quality or only one playback option -> use first
                string[] values = new string[foVideo.PlaybackOptions.Count];
                foVideo.PlaybackOptions.Values.CopyTo(values, 0);
                return values[0];
            }
            else if (videoQuality == VideoQuality.HD)
            {
                // take highest available quality
                string[] values = new string[foVideo.PlaybackOptions.Count];
                foVideo.PlaybackOptions.Values.CopyTo(values, 0);
                return values[values.Length - 1];
            }
            else // choose a high quality from options (highest below the HD formats (37 22)
            {
                string[] keys = new string[foVideo.PlaybackOptions.Count];
                foVideo.PlaybackOptions.Keys.CopyTo(keys, 0);
                int index = keys.Length - 1;
                while (index > 0 && (!keys[index].EndsWith(".flv"))) index--;
                return foVideo.PlaybackOptions[keys[index]];
            }
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

            if (localize)
            {
                if (query.BaseAddress.StartsWith(YouTubeQuery.StandardFeeds))
                {
                    // standartfeeds don't honor the LR parameter and have their own way of country specific query
                    string postStandartFeedUriPart = query.BaseAddress.Substring(YouTubeQuery.StandardFeeds.Length);
                    if (postStandartFeedUriPart.IndexOf("/") != 2)
                    {
                        // try to add a country from the StandartFeedCountry enumeration
                        try
                        {
                            StandartFeedCountry result = (StandartFeedCountry)Enum.Parse(typeof(StandartFeedCountry), OnlineVideoSettings.Instance.Locale.TwoLetterISOLanguageName.ToUpper());
                            query.BaseAddress = YouTubeQuery.StandardFeeds + result.ToString() + "/" + postStandartFeedUriPart;
                        }
                        catch { }
                    }
                }
                else
                {
                    //http://code.google.com/intl/de-DE/apis/youtube/2.0/reference.html#lrsp
                    query.LR = OnlineVideoSettings.Instance.Locale.TwoLetterISOLanguageName;
                }
            }

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
                string catUri = YouTubeService.DefaultCategory;
                if (localize)
                {
                    try
                    {
                        Enum.Parse(typeof(CategoryLocale), OnlineVideoSettings.Instance.Locale.TwoLetterISOLanguageName.ToLower());
                        catUri += "?hl=" + OnlineVideoSettings.Instance.Locale.Name;
                    }
                    catch { }
                }

                foreach (YouTubeCategory cat in YouTubeQuery.GetCategories(new Uri(catUri), new YouTubeCategoryCollection()))
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
                throw new OnlineVideosException("Please set your username and password in the Configuration");
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

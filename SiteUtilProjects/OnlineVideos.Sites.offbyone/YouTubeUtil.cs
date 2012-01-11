using System;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Collections.Generic;
using Google.GData.Client;
using Google.GData.Extensions;
using Google.GData.YouTube;
using Google.GData.Extensions.MediaRss;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace OnlineVideos.Sites
{
	public class YouTubeUtil : LatestVideosSiteUtilBase, IFilter
    {
        private class MyYouTubeEntry : IVideoDetails
        {
            public YouTubeEntry YouTubeEntry { get; private set; }

            public MyYouTubeEntry(YouTubeEntry entry)
            {
                YouTubeEntry = entry;
            }

            public Dictionary<string, string> GetExtendedProperties()
            {
                Dictionary<string, string> properties = new Dictionary<string, string>();
                properties.Add("Uploader", YouTubeEntry.Uploader.Value.ToString());
                if (YouTubeEntry.Rating != null)
                {
                    properties.Add("Rating", YouTubeEntry.Rating.Average.ToString("F1", OnlineVideoSettings.Instance.Locale));
                    properties.Add("NumRaters", YouTubeEntry.Rating.NumRaters.ToString());
                }
                if (YouTubeEntry.Statistics != null)
                {
                    if (!string.IsNullOrEmpty(YouTubeEntry.Statistics.FavoriteCount)) properties.Add("FavoriteCount", YouTubeEntry.Statistics.FavoriteCount);
                    if (!string.IsNullOrEmpty(YouTubeEntry.Statistics.ViewCount)) properties.Add("ViewCount", YouTubeEntry.Statistics.ViewCount);
                }
                return properties;
            }
        }

        public enum VideoQuality { Low, Medium, High, HD, FullHD };

		public enum VideoFormat { flv, mp4, webm };

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

		[Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Preferred Format"), Description("Prefer this format when there are more than one for the desired quality.")]
		VideoFormat preferredFormat = VideoFormat.mp4;
		[Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Undesired Format"), Description("Try to avoid this format when there are more than one for the desired quality.")]
		VideoFormat undesiredFormat = VideoFormat.webm;
		[Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Video Quality", TranslationFieldName="VideoQuality"), Description("Defines the maximum quality for the video to be played.")]
        VideoQuality videoQuality = VideoQuality.High;
		[Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Account Name"), Description("Your YouTube account name (not Email!). Used for favorites and subscriptions.")]
        string accountname = "";
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Login"), Description("Your YouTube login (mostly an Email!). Used for favorites and subscriptions.")]
        string login = "";
		[Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Password"), Description("Your YouTube password. Used for favorites and subscriptions."), PasswordPropertyText(true)]
        string password = "";
		[Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Videos per Page"), Description("Defines the default number of videos to display per page.")]
        int pageSize = 26;
		[Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Localize"), Description("Try to retrieve data specific for your region.")]
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
		const string USER_NEWSUBSCRIPTIONS_FEED = "http://gdata.youtube.com/feeds/api/users/default/newsubscriptionvideos";
		const string USER_RECOMMENDATIONS_FEED = "https://gdata.youtube.com/feeds/api/users/default/recommendations";
		const string USER_WATCH_LATER_FEED = "https://gdata.youtube.com/feeds/api/users/default/watch_later?v=2";
		const string USER_WATCH_HISTORY_FEED = "https://gdata.youtube.com/feeds/api/users/default/watch_history?v=2";
        
        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            orderByList = new Dictionary<String, String>() {{"Relevance", "relevance"},
                                                            {"Published", "published"},
                                                            {"View Count", "viewCount"},
                                                            {"Rating", "rating"}};
            foreach (string name in Enum.GetNames(typeof(YouTubeQuery.UploadTime))) timeFrameList.Add(Utils.ToFriendlyCase(name), name);
            service = new YouTubeService("OnlineVideos", DEVELOPER_KEY);
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            currentVideosTitle = null;  // use default title for videos retrieved via this method (which is the Category Name)
			if (((RssLink)category).Other == "Login") Login();
            string fsUrl = ((RssLink)category).Url;
            YouTubeQuery query = new YouTubeQuery() { Uri = new Uri(fsUrl) };
            query.NumberToRetrieve = pageSize;
            return parseGData(query);
        }

        public override List<String> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            video.PlaybackOptions = Hoster.Base.HosterFactory.GetHoster("Youtube").getPlaybackOptions(video.VideoUrl);
            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
            {
				if (video.PlaybackOptions.Count == 1)
				{
					// nothing to chose from, only one options available
					return new List<string>() { video.PlaybackOptions.First().Value };
				}
				else
				{
					KeyValuePair<string, string> foundQuality = default(KeyValuePair<string, string>);
					switch (videoQuality)
					{
						case VideoQuality.Low:		//use first available option
							foundQuality = video.PlaybackOptions.First(); break;
						case VideoQuality.Medium:	//first above 320 that is not 3D
							foundQuality = video.PlaybackOptions.FirstOrDefault(q => !q.Key.Contains("320") && !q.Key.Contains("3D")); break;
						case VideoQuality.High:		//highest below the HD formats that is not 3D
							foundQuality = video.PlaybackOptions.LastOrDefault(q => !q.Key.Contains("1920") && !q.Key.Contains("1280") && !q.Key.Contains("3D")); break;
						case VideoQuality.HD:		//first below full HD that is not 3D
							foundQuality = video.PlaybackOptions.LastOrDefault(q => !q.Key.Contains("1920") && !q.Key.Contains("3D")); break;
						case VideoQuality.FullHD:	//use highest available quality that is not 3D
							foundQuality = video.PlaybackOptions.Last(q => !q.Key.Contains("3D")); break;
					}
					if (!string.IsNullOrEmpty(foundQuality.Key))
					{
						string resolution = foundQuality.Key.Substring(0, foundQuality.Key.IndexOf('|'));
						// try to find one that has the same resolution and the preferred format
						var bestMatch = video.PlaybackOptions.FirstOrDefault(q => q.Key.Contains(resolution) && !q.Key.Contains("3D") && q.Key.Contains(preferredFormat.ToString()));
						// try to find one that has the same resolution and not the undesired format
						if (string.IsNullOrEmpty(bestMatch.Key)) bestMatch = video.PlaybackOptions.FirstOrDefault(q => q.Key.Contains(resolution) && !q.Key.Contains("3D") && !q.Key.Contains(undesiredFormat.ToString()));
						if (!string.IsNullOrEmpty(bestMatch.Key)) foundQuality = bestMatch;
					}
					// fallback when no match was found -> use first choice
					if (string.IsNullOrEmpty(foundQuality.Key)) foundQuality = video.PlaybackOptions.First();
					if (inPlaylist) video.PlaybackOptions = null;
					return new List<string>() { foundQuality.Value };
				}				
            }
			return null; // no playback options
        }

        public override int DiscoverDynamicCategories()
        {
            // walk the categories and see if there are user's playlists - they need to be set to have subcategories
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

            // if a username was set add a category for the users a) favorites and b) subscriptions
            if (!string.IsNullOrEmpty(accountname))
            {
				Settings.Categories.Add(new RssLink() { Name = string.Format("{0}'s {1}", accountname, Translation.Instance.Favourites), Url = YouTubeQuery.CreateFavoritesUri(accountname), Other="Login" });
				Settings.Categories.Add(new RssLink() { Name = string.Format("{0}'s {1}", accountname, Translation.Instance.Recommendations), Url = USER_RECOMMENDATIONS_FEED, Other = "Login" });
				Settings.Categories.Add(new RssLink() { Name = string.Format("{0}'s {1}", accountname, "Watch Later Playlist"), Url = USER_WATCH_LATER_FEED, Other = "Login" });
				Settings.Categories.Add(new RssLink() { Name = string.Format("{0}'s {1}", accountname, "Watch History Feed"), Url = USER_WATCH_HISTORY_FEED, Other = "Login" });
				Settings.Categories.Add(new Category() { Name = string.Format("{0}'s {1}", accountname, Translation.Instance.Subscriptions), HasSubCategories = true });
				Settings.Categories.Add(new Category() { Name = string.Format("{0}'s {1}", accountname, Translation.Instance.Playlists), HasSubCategories = true });
            }

            Settings.DynamicCategoriesDiscovered = true;
            return categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            if (parentCategory is RssLink)
            {
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
            }
            else
            {
                Login();

				if (parentCategory.Name.EndsWith(Translation.Instance.Playlists))
				{
					// users playlists
					YouTubeQuery query = new YouTubeQuery() { Uri = new Uri(YouTubeQuery.CreatePlaylistsUri(accountname)), StartIndex = 1, NumberToRetrieve = 50 }; // max. 50 per query
					YouTubeFeed feed = null;
					try
					{
						feed = service.Query(query);
					}
					catch (Google.GData.Client.GDataRequestException queryEx)
					{
						string reason = ((XText)((IEnumerable<object>)XDocument.Parse(queryEx.ResponseString).XPathEvaluate("//*[local-name() = 'internalReason']/text()")).FirstOrDefault()).Value;
						if (!string.IsNullOrEmpty(reason)) throw new OnlineVideosException(reason);
						else throw queryEx;
					}
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
				}
				else
				{
					// users subscriptions
					RssLink newVidsLink = new RssLink();
					newVidsLink.Name = Translation.Instance.NewVideos;
					newVidsLink.Url = USER_NEWSUBSCRIPTIONS_FEED;
					parentCategory.SubCategories.Add(newVidsLink);
					newVidsLink.ParentCategory = parentCategory;

					YouTubeQuery query = new YouTubeQuery() { Uri = new Uri(YouTubeQuery.CreateSubscriptionUri(accountname)), StartIndex = 1, NumberToRetrieve = 50 }; // max. 50 per query
					bool hasNextPage = false;
					do
					{
						YouTubeFeed feed = null;
						try
						{
							feed = service.Query(query);
						}
						catch (Google.GData.Client.GDataRequestException queryEx)
						{
							string reason = ((XText)((IEnumerable<object>)XDocument.Parse(queryEx.ResponseString).XPathEvaluate("//*[local-name() = 'internalReason']/text()")).FirstOrDefault()).Value;
							if (!string.IsNullOrEmpty(reason)) throw new OnlineVideosException(reason);
							else throw queryEx;
						}
						foreach (SubscriptionEntry subScr in feed.Entries)
						{
							RssLink subScrLink = new RssLink();
							subScrLink.Name = subScr.UserName;
							subScrLink.Url = YouTubeQuery.CreateUserUri(subScr.UserName);
							parentCategory.SubCategories.Add(subScrLink);
							subScrLink.ParentCategory = parentCategory;
						}
						hasNextPage = !string.IsNullOrEmpty(feed.NextChunk);
						query.StartIndex += 50;
					} while (hasNextPage);
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
                string myAddress = query.BaseAddress.Substring(query.BaseAddress.IndexOf("//")+2);
                string ytAddress = YouTubeQuery.StandardFeeds.Substring(YouTubeQuery.StandardFeeds.IndexOf("//")+2);
                if (myAddress.StartsWith(ytAddress))
                {
                    // standartfeeds don't honor the LR parameter and have their own way of country specific query
                    string postStandartFeedUriPart = myAddress.Substring(ytAddress.Length);
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
            YouTubeFeed feed = null;
            try
            {
                feed = service.Query(query);
            }
            catch (Google.GData.Client.GDataRequestException queryEx)
            {
                string reason = ((XText)((IEnumerable<object>)XDocument.Parse(queryEx.ResponseString).XPathEvaluate("//*[local-name() = 'internalReason']/text()")).FirstOrDefault()).Value;
                if (!string.IsNullOrEmpty(reason)) throw new OnlineVideosException(reason);
                else throw queryEx;
            }
            
            hasPreviousPage = !string.IsNullOrEmpty(feed.PrevChunk);
            hasNextPage = !string.IsNullOrEmpty(feed.NextChunk);
            foreach (YouTubeEntry entry in feed.Entries)
            {
                VideoInfo video = new VideoInfo();
                video.Other = new MyYouTubeEntry(entry);
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
                video.Airdate = entry.Published.ToString("g", OnlineVideoSettings.Instance.Locale);
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
            if (lastPerformedQuery.StartIndex == 0) lastPerformedQuery.StartIndex = 1;
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

        #region YouTube Favorites, Related Videos Handling

        string currentVideosTitle = null;
        public override string getCurrentVideosTitle()
        {
            return currentVideosTitle;
        }

        public override List<string> GetContextMenuEntries(Category selectedCategory, VideoInfo selectedItem)
        {
            List<string> result = new List<string>();
            if (selectedItem != null)
            {
				result.Add(Translation.Instance.RelatedVideos);

                MyYouTubeEntry ytEntry = selectedItem.Other as MyYouTubeEntry;
                if (ytEntry != null && ytEntry.YouTubeEntry != null && ytEntry.YouTubeEntry.Uploader != null && !string.IsNullOrEmpty(ytEntry.YouTubeEntry.Uploader.Value))
                {
					result.Add(Translation.Instance.UploadsBy + " [" + ytEntry.YouTubeEntry.Uploader.Value + "]");
                }
                if (selectedCategory is RssLink)
                {
					result.Add(Translation.Instance.AddToFavourites + " (" + Settings.Name + ")");
                }
                else if (selectedCategory is Category)
                {
					result.Add(Translation.Instance.RemoveFromFavorites + " (" + Settings.Name + ")");
                }
            }
            return result;
        }

        public override bool ExecuteContextMenuEntry(Category selectedCategory, VideoInfo selectedItem, string choice, out List<ISearchResultItem> newVideos)
        {
            newVideos = null;
			if (choice == Translation.Instance.AddToFavourites + " (" + Settings.Name + ")")
            {
                addFavorite(selectedItem);
                return false;
            }
			else if (choice == Translation.Instance.RemoveFromFavorites + " (" + Settings.Name + ")")
            {
                removeFavorite(selectedItem);
                return true;
            }
			else if (choice == Translation.Instance.RelatedVideos)
            {
                YouTubeQuery query = new YouTubeQuery() { Uri = new Uri((selectedItem.Other as MyYouTubeEntry).YouTubeEntry.RelatedVideosUri.Content), NumberToRetrieve = pageSize };
                newVideos = parseGData(query).ConvertAll<ISearchResultItem>(v => v as ISearchResultItem);
				currentVideosTitle = Translation.Instance.RelatedVideos + " [" + selectedItem.Title + "]";
                return false;
            }
			else if (choice.StartsWith(Translation.Instance.UploadsBy))
            {
                YouTubeEntry ytEntry = (selectedItem.Other as MyYouTubeEntry).YouTubeEntry;
                YouTubeQuery query = new YouTubeQuery(YouTubeQuery.CreateUserUri(ytEntry.Uploader.Value)) { NumberToRetrieve = pageSize };
                newVideos = parseGData(query).ConvertAll<ISearchResultItem>(v => v as ISearchResultItem);
				currentVideosTitle = Translation.Instance.UploadsBy + " [" + ytEntry.Uploader.Value + "]";
            }
            return false;
        }

        protected void addFavorite(VideoInfo video)
        {
            if (CheckUsernameAndPassword())
            {
                Login();
                YouTubeEntry entry = ((MyYouTubeEntry)video.Other).YouTubeEntry;
                service.Insert(new Uri(YouTubeQuery.CreateFavoritesUri(accountname)), entry);                
            }
        }

        protected void removeFavorite(VideoInfo video)
        {
            if (CheckUsernameAndPassword())
            {
                Login();
                ((MyYouTubeEntry)video.Other).YouTubeEntry.Delete();
            }
        }

        protected bool CheckUsernameAndPassword()
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(login))
            {
                throw new OnlineVideosException("Please set your login and password in the Configuration");
            }
            else
            {
                return true;
            }
        }

        protected bool Login()
        {
            //check if already logged in
            if (!string.IsNullOrEmpty((service.RequestFactory as Google.GData.Client.GDataGAuthRequestFactory).GAuthToken)) return true;
            // check if we can login
            if (!string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(login))
            {
                // do login
                service.setUserCredentials(login, password);
				try
				{
					string token = service.QueryClientLoginToken();
					service.SetAuthenticationToken(token);
					return true;
				}
				catch (Exception ex)
				{
					throw new OnlineVideosException(ex.Message);
				}
            }
            return false;
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

		#region LatestVideosSiteUtilBase Member

		public override uint LatestVideosCount
		{
			get { return useDynamicCategories ? latestVideosCount : 0; }
		}

		public override List<VideoInfo> GetLatestVideos()
		{
			if (Login())
			{
				YouTubeQuery query = new YouTubeQuery() { Uri = new Uri(USER_NEWSUBSCRIPTIONS_FEED) };
				query.NumberToRetrieve = (int)latestVideosCount;
				return parseGData(query);
			}
			else
			{
				YouTubeQuery query = new YouTubeQuery() { Uri = new Uri("http://gdata.youtube.com/feeds/api/videos?orderby=published") };
				query.NumberToRetrieve = (int)latestVideosCount;
				return parseGData(query);
			}
		}
		#endregion
	}
}

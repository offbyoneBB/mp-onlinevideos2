using System.Threading;
using System.Web;

namespace OnlineVideos.Sites.WebAutomation.Properties
{
	[global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
	public class Resources
	{
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;

        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager 
		{
            get 
			{
                if (object.ReferenceEquals(resourceMan, null)) 
				{
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("OnlineVideos.Sites.WebAutomation.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
			set
			{
				resourceMan = value;
			}
        }

        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }

        /// <summary>
        ///   Returns the formatted resource string.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        private static string GetResourceString(string key, params string[] tokens)
		{
            var str = ResourceManager.GetString(key, resourceCulture);

			for(int i = 0; i < tokens.Length; i += 2)
				str = str.Replace(tokens[i], tokens[i+1]);
										
            return str;
        }
        
		
		public static string SkyGoLiveTvImageUrl( string CHANNEL_ID) { return GetResourceString("SkyGoLiveTvImageUrl", "{CHANNEL_ID}", CHANNEL_ID); }

		public static string SkyGo_CategoryAToZUrl( string CATEGORY, string CHARACTER, string PAGE) { return GetResourceString("SkyGo_CategoryAToZUrl", "{CATEGORY}", CATEGORY, "{CHARACTER}", CHARACTER, "{PAGE}", PAGE); }

		public static string SkyGo_LiveTvGetNowNextUrl( string CHANNEL_IDS) { return GetResourceString("SkyGo_LiveTvGetNowNextUrl", "{CHANNEL_IDS}", CHANNEL_IDS); }

		public static string SkyGo_LiveTvListingUrl { get { return GetResourceString("SkyGo_LiveTvListingUrl"); } }

		public static string SkyGo_LiveTvPlayUrl( string VIDEO_ID) { return GetResourceString("SkyGo_LiveTvPlayUrl", "{VIDEO_ID}", VIDEO_ID); }

		public static string SkyGo_LoginUrl { get { return GetResourceString("SkyGo_LoginUrl"); } }

		public static string SkyGo_RootUrl { get { return GetResourceString("SkyGo_RootUrl"); } }

		public static string SkyGo_SeriesDetailsUrl( string SERIES_ID) { return GetResourceString("SkyGo_SeriesDetailsUrl", "{SERIES_ID}", SERIES_ID); }

		public static string SkyGo_VideoActionsUrl( string VIDEO_ID) { return GetResourceString("SkyGo_VideoActionsUrl", "{VIDEO_ID}", VIDEO_ID); }

		public static string SkyGo_VideoDetailsUrl( string VIDEO_ID) { return GetResourceString("SkyGo_VideoDetailsUrl", "{VIDEO_ID}", VIDEO_ID); }

		public static string SkyGo_VideoPlayUrl( string ASSET_ID, string VIDEO_ID) { return GetResourceString("SkyGo_VideoPlayUrl", "{ASSET_ID}", ASSET_ID, "{VIDEO_ID}", VIDEO_ID); }

		public static string _4OD_CatchUpUrl { get { return GetResourceString("_4OD_CatchUpUrl"); } }

		public static string _4OD_CategoryListUrl( string CATEGORY) { return GetResourceString("_4OD_CategoryListUrl", "{CATEGORY}", CATEGORY); }

		public static string _4OD_CollectionListUrl { get { return GetResourceString("_4OD_CollectionListUrl"); } }

		public static string _4OD_LoginUrl { get { return GetResourceString("_4OD_LoginUrl"); } }

		public static string _4OD_RootUrl { get { return GetResourceString("_4OD_RootUrl"); } }

		public static string _4OD_VideoDetailsUrl( string VIDEO_ID) { return GetResourceString("_4OD_VideoDetailsUrl", "{VIDEO_ID}", VIDEO_ID); }

		public static string _4OD_VideoPlayUrl( string VIDEO_NAME, string VIDEO_ID) { return GetResourceString("_4OD_VideoPlayUrl", "{VIDEO_NAME}", VIDEO_NAME, "{VIDEO_ID}", VIDEO_ID); }

		public static string AmazonLoginUrl { get { return GetResourceString("AmazonLoginUrl"); } }

		public static string AmazonMovieUrl( string VIDEO_ID) { return GetResourceString("AmazonMovieUrl", "{VIDEO_ID}", VIDEO_ID); }

		public static string AmazonMovieCategoriesUrl { get { return GetResourceString("AmazonMovieCategoriesUrl"); } }

		public static string AmazonTVCategoriesUrl { get { return GetResourceString("AmazonTVCategoriesUrl"); } }

		public static string AmazonRootUrl { get { return GetResourceString("AmazonRootUrl"); } }

		public static string AmazonMovieIcon { get { return GetResourceString("AmazonMovieIcon"); } }

		public static string AmazonTvIcon { get { return GetResourceString("AmazonTvIcon"); } }

		public static string Resources_de { get { return GetResourceString("Resources.de"); } }

		public static class Names
		{
			public const string SkyGoLiveTvImageUrl = "SkyGoLiveTvImageUrl";
			public const string SkyGo_CategoryAToZUrl = "SkyGo_CategoryAToZUrl";
			public const string SkyGo_LiveTvGetNowNextUrl = "SkyGo_LiveTvGetNowNextUrl";
			public const string SkyGo_LiveTvListingUrl = "SkyGo_LiveTvListingUrl";
			public const string SkyGo_LiveTvPlayUrl = "SkyGo_LiveTvPlayUrl";
			public const string SkyGo_LoginUrl = "SkyGo_LoginUrl";
			public const string SkyGo_RootUrl = "SkyGo_RootUrl";
			public const string SkyGo_SeriesDetailsUrl = "SkyGo_SeriesDetailsUrl";
			public const string SkyGo_VideoActionsUrl = "SkyGo_VideoActionsUrl";
			public const string SkyGo_VideoDetailsUrl = "SkyGo_VideoDetailsUrl";
			public const string SkyGo_VideoPlayUrl = "SkyGo_VideoPlayUrl";
			public const string _4OD_CatchUpUrl = "_4OD_CatchUpUrl";
			public const string _4OD_CategoryListUrl = "_4OD_CategoryListUrl";
			public const string _4OD_CollectionListUrl = "_4OD_CollectionListUrl";
			public const string _4OD_LoginUrl = "_4OD_LoginUrl";
			public const string _4OD_RootUrl = "_4OD_RootUrl";
			public const string _4OD_VideoDetailsUrl = "_4OD_VideoDetailsUrl";
			public const string _4OD_VideoPlayUrl = "_4OD_VideoPlayUrl";
			public const string AmazonLoginUrl = "AmazonLoginUrl";
			public const string AmazonMovieUrl = "AmazonMovieUrl";
			public const string AmazonMovieCategoriesUrl = "AmazonMovieCategoriesUrl";
			public const string AmazonTVCategoriesUrl = "AmazonTVCategoriesUrl";
			public const string AmazonRootUrl = "AmazonRootUrl";
			public const string AmazonMovieIcon = "AmazonMovieIcon";
			public const string AmazonTvIcon = "AmazonTvIcon";
			public const string Resources_de = "Resources_de";
		}
	}
}

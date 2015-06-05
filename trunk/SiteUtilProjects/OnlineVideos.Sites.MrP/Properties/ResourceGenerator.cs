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

		public static string SkyGo_LiveTvGetNowNextUrl( string CHANNEL_IDS) { return GetResourceString("SkyGo_LiveTvGetNowNextUrl", "{CHANNEL_IDS}", CHANNEL_IDS); }

		public static string SkyGo_LiveTvListingUrl { get { return GetResourceString("SkyGo_LiveTvListingUrl"); } }

		public static string SkyGo_LiveTvPlayUrl( string VIDEO_ID) { return GetResourceString("SkyGo_LiveTvPlayUrl", "{VIDEO_ID}", VIDEO_ID); }

		public static string SkyGo_LoginUrl { get { return GetResourceString("SkyGo_LoginUrl"); } }

		public static string SkyGo_RootUrl { get { return GetResourceString("SkyGo_RootUrl"); } }

		public static string SkyGo_VideoPlayUrl( string INTERNAL_ID) { return GetResourceString("SkyGo_VideoPlayUrl", "{INTERNAL_ID}", INTERNAL_ID); }

		public static string _4OD_CatchUpUrl { get { return GetResourceString("_4OD_CatchUpUrl"); } }

		public static string _4OD_CategoryListUrl( string CATEGORY) { return GetResourceString("_4OD_CategoryListUrl", "{CATEGORY}", CATEGORY); }

		public static string _4OD_CollectionListUrl { get { return GetResourceString("_4OD_CollectionListUrl"); } }

		public static string _4OD_LoginUrl { get { return GetResourceString("_4OD_LoginUrl"); } }

		public static string _4OD_RootUrl { get { return GetResourceString("_4OD_RootUrl"); } }

		public static string _4OD_VideoDetailsUrl( string VIDEO_ID) { return GetResourceString("_4OD_VideoDetailsUrl", "{VIDEO_ID}", VIDEO_ID); }

		public static string _4OD_VideoPlayUrl( string VIDEO_NAME, string VIDEO_ID) { return GetResourceString("_4OD_VideoPlayUrl", "{VIDEO_NAME}", VIDEO_NAME, "{VIDEO_ID}", VIDEO_ID); }

		public static string SkyGo_AllListUrl( string INTERNAL_ID, string CHARACTER) { return GetResourceString("SkyGo_AllListUrl", "{INTERNAL_ID}", INTERNAL_ID, "{CHARACTER}", CHARACTER); }

		public static string SkyGo_SeriesInfoUrl( string INTERNAL_ID) { return GetResourceString("SkyGo_SeriesInfoUrl", "{INTERNAL_ID}", INTERNAL_ID); }

		public static string SkyGo_CatchUpCategoriesUrl { get { return GetResourceString("SkyGo_CatchUpCategoriesUrl"); } }

		public static string Resources_de { get { return GetResourceString("Resources.de"); } }

		public static string SkyGo_CatchUpSubItemsUrl( string INTERNAL_ID) { return GetResourceString("SkyGo_CatchUpSubItemsUrl", "{INTERNAL_ID}", INTERNAL_ID); }

		public static class Names
		{
			public const string SkyGoLiveTvImageUrl = "SkyGoLiveTvImageUrl";
			public const string SkyGo_LiveTvGetNowNextUrl = "SkyGo_LiveTvGetNowNextUrl";
			public const string SkyGo_LiveTvListingUrl = "SkyGo_LiveTvListingUrl";
			public const string SkyGo_LiveTvPlayUrl = "SkyGo_LiveTvPlayUrl";
			public const string SkyGo_LoginUrl = "SkyGo_LoginUrl";
			public const string SkyGo_RootUrl = "SkyGo_RootUrl";
			public const string SkyGo_VideoPlayUrl = "SkyGo_VideoPlayUrl";
			public const string _4OD_CatchUpUrl = "_4OD_CatchUpUrl";
			public const string _4OD_CategoryListUrl = "_4OD_CategoryListUrl";
			public const string _4OD_CollectionListUrl = "_4OD_CollectionListUrl";
			public const string _4OD_LoginUrl = "_4OD_LoginUrl";
			public const string _4OD_RootUrl = "_4OD_RootUrl";
			public const string _4OD_VideoDetailsUrl = "_4OD_VideoDetailsUrl";
			public const string _4OD_VideoPlayUrl = "_4OD_VideoPlayUrl";
			public const string SkyGo_AllListUrl = "SkyGo_AllListUrl";
			public const string SkyGo_SeriesInfoUrl = "SkyGo_SeriesInfoUrl";
			public const string SkyGo_CatchUpCategoriesUrl = "SkyGo_CatchUpCategoriesUrl";
			public const string Resources_de = "Resources_de";
			public const string SkyGo_CatchUpSubItemsUrl = "SkyGo_CatchUpSubItemsUrl";
		}
	}
}

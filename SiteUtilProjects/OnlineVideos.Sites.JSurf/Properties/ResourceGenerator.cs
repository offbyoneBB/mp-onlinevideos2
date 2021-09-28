using System.Threading;
using System.Web;

namespace OnlineVideos.Sites.JSurf.Properties
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("OnlineVideos.Sites.JSurf.Properties.Resources", typeof(Resources).Assembly);
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
        
		
		public static string AmazonLoginUrl { get { return GetResourceString("AmazonLoginUrl"); } }

		public static string AmazonMovieUrl( string VIDEO_ID) { return GetResourceString("AmazonMovieUrl", "{VIDEO_ID}", VIDEO_ID); }

		public static string AmazonMovieCategoriesUrl { get { return GetResourceString("AmazonMovieCategoriesUrl"); } }

		public static string AmazonTVCategoriesUrl { get { return GetResourceString("AmazonTVCategoriesUrl"); } }

		public static string AmazonRootUrl { get { return GetResourceString("AmazonRootUrl"); } }

		public static string AmazonMovieIcon { get { return GetResourceString("AmazonMovieIcon"); } }

		public static string AmazonTvIcon { get { return GetResourceString("AmazonTvIcon"); } }

		public static string AmazonMovieWatchlistUrl { get { return GetResourceString("AmazonMovieWatchlistUrl"); } }

		public static string AmazonTVWatchlistUrl { get { return GetResourceString("AmazonTVWatchlistUrl"); } }

		public static string AmazonMoviePopularUrl { get { return GetResourceString("AmazonMoviePopularUrl"); } }

		public static string AmazonMovieRecentUrl { get { return GetResourceString("AmazonMovieRecentUrl"); } }

		public static string AmazonMovieEditorsUrl { get { return GetResourceString("AmazonMovieEditorsUrl"); } }

		public static string AmazonTVPopularUrl { get { return GetResourceString("AmazonTVPopularUrl"); } }

		public static string AmazonTVRecentUrl { get { return GetResourceString("AmazonTVRecentUrl"); } }

		public static string AmazonTVEditorsUrl { get { return GetResourceString("AmazonTVEditorsUrl"); } }

		public static string AmazonSearchUrl( string QUERY) { return GetResourceString("AmazonSearchUrl", "{QUERY}", QUERY); }

		public static string Resources_de { get { return GetResourceString("Resources.de"); } }

		public static string Resources_en_US { get { return GetResourceString("Resources.en-US"); } }

		public static string AmazonATVUrl { get { return GetResourceString("AmazonATVUrl"); } }

		public static string AmazonMarketId { get { return GetResourceString("AmazonMarketId"); } }

		public static string AmazonMovie30DaysUrl { get { return GetResourceString("AmazonMovie30DaysUrl"); } }

		public static string AmazonTV30DaysUrl { get { return GetResourceString("AmazonTV30DaysUrl"); } }

		public static class Names
		{
			public const string AmazonLoginUrl = "AmazonLoginUrl";
			public const string AmazonMovieUrl = "AmazonMovieUrl";
			public const string AmazonMovieCategoriesUrl = "AmazonMovieCategoriesUrl";
			public const string AmazonTVCategoriesUrl = "AmazonTVCategoriesUrl";
			public const string AmazonRootUrl = "AmazonRootUrl";
			public const string AmazonMovieIcon = "AmazonMovieIcon";
			public const string AmazonTvIcon = "AmazonTvIcon";
			public const string AmazonMovieWatchlistUrl = "AmazonMovieWatchlistUrl";
			public const string AmazonTVWatchlistUrl = "AmazonTVWatchlistUrl";
			public const string AmazonMoviePopularUrl = "AmazonMoviePopularUrl";
			public const string AmazonMovieRecentUrl = "AmazonMovieRecentUrl";
			public const string AmazonMovieEditorsUrl = "AmazonMovieEditorsUrl";
			public const string AmazonTVPopularUrl = "AmazonTVPopularUrl";
			public const string AmazonTVRecentUrl = "AmazonTVRecentUrl";
			public const string AmazonTVEditorsUrl = "AmazonTVEditorsUrl";
			public const string AmazonSearchUrl = "AmazonSearchUrl";
			public const string Resources_de = "Resources_de";
			public const string Resources_en_US = "Resources_en_US";
			public const string AmazonATVUrl = "AmazonATVUrl";
			public const string AmazonMarketId = "AmazonMarketId";
			public const string AmazonMovie30DaysUrl = "AmazonMovie30DaysUrl";
			public const string AmazonTV30DaysUrl = "AmazonTV30DaysUrl";
		}
	}
}

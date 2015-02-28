using System.Collections.Generic;

namespace OnlineVideos
{
    /// <summary>
    /// This interface defines the contract a class should implement that can be used to manage favorite videos and categories for sites.
    /// </summary>
    public interface IFavoritesDatabase
    {
        /// <summary>Gets a list of sites that have favorites (videos or categories).</summary>
        /// <returns>A list of key value pairs that represent the site's name and the number of favorite videos.</returns>
        List<KeyValuePair<string, uint>> GetSiteIds();
        
        /// <summary>Adds a video to the favorites for the given site.</summary>
        /// <param name="video">The video object to use for storing data.</param>
        /// <param name="titleFromUtil">A title for the favorite.</param>
        /// <param name="siteName">The name of the site this favorite video belongs to.</param>
        /// <returns>true when the video was successfully added.</returns>
        bool AddFavoriteVideo(VideoInfo video, string titleFromUtil, string siteName);

        /// <summary>Removes a favorite video.</summary>
        /// <param name="video">The video to remove.</param>
        /// <returns>true when the video was removed from favorites.</returns>
        bool RemoveFavoriteVideo(FavoriteDbVideoInfo video);
        
        /// <summary>Remove all favorite videos for the given site.</summary>
        /// <param name="siteId">The name of a site to remove favorite videos for</param>
        /// <returns>true when at least 1 video was removed, otherwise false</returns>
        bool RemoveAllFavoriteVideos(string siteId);

        /// <summary>
        /// Gets all favorite videos, with restriction to the given site or search query (both can be null).
        /// The implementation should already filter videos in case the age confirmation is used and no age has been confirmed yet.
        /// </summary>
        /// <param name="siteId">The name of a site to restrict the returned videos to. (if null, don't restrict to a site)</param>
        /// <param name="query">A search string to filter the resulting videos. (if null, don't restrict to a term)</param>
        /// <returns>A list of <see cref="VideoInfo"/> objects matching the parameters.</returns>
        List<VideoInfo> GetFavoriteVideos(string siteId, string query);
        
        /// <summary>Gets a list of favorite categories of the given site.</summary>
        /// <param name="siteId">The name of a site to return categories for</param>
        /// <returns>A list of <see cref="FavoriteDbCategory"/> objects for the given site.</returns>
        List<FavoriteDbCategory> GetFavoriteCategories(string siteId);
        
        /// <summary>Gets the names of the favorite categories of the given site.</summary>
        /// <param name="siteId">The name of the site to return categories for</param>
        /// <returns>A list of category names</returns>
        List<string> GetFavoriteCategoriesNames(string siteId);
        
        /// <summary>Adds a category to the favorites of the given site.</summary>
        /// <param name="category">The category object to use for storing data.</param>
        /// <param name="siteId">The name of the site this category belongs to.</param>
        /// <returns>true when the category was successfully added</returns>
        bool AddFavoriteCategory(Category category, string siteId);
        
        /// <summary>Remove the category from favorites.</summary>
        /// <param name="category">The category to remove.</param>
        /// <returns>true when the category was removed.</returns>
        bool RemoveFavoriteCategory(FavoriteDbCategory category);
        
        /// <summary>Remove a cetegory from favorites using the recursive catgeory name.</summary>
        /// <param name="siteName">The name of the site this category belongs to.</param>
        /// <param name="recursiveCategoryName">The recursive category name.</param>
        /// <returns>true when the category was removed from the favorites of this site.</returns>
        bool RemoveFavoriteCategory(string siteName, string recursiveCategoryName);
    }

    /// <summary>A special class for videos retrieved from the favorite database.</summary>
    public class FavoriteDbVideoInfo : VideoInfo
    {
        /// <summary>Holds an unique Id for the Video, so it can be deleted from the DB.</summary>
        public int Id { get; set; }

        /// <summary>Holds the name of the Site where this video came from.</summary>
        public string SiteName { get; set; }
    }

    /// <summary>A special class for categories retrieved from the favorite database.</summary>
    public class FavoriteDbCategory : Category
    {
        /// <summary>Holds an unique Id for the Category, so it can be deleted from the DB.</summary>
        public int Id { get; set; }

        /// <summary>Holds the recursive name of the original category.</summary>
        public string RecursiveName { get; set; }
    }
}

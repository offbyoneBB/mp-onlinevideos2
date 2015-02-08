using System.Collections.Generic;

namespace OnlineVideos
{
    public interface IFavoritesDatabase
    {
        List<KeyValuePair<string, uint>> GetSiteIds();
        
        bool AddFavoriteVideo(VideoInfo video, string titleFromUtil, string siteName);

        bool RemoveFavoriteVideo(FavoriteVideoInfo video);
        
        bool RemoveAllFavoriteVideos(string siteId);

        /// <summary>
        /// Method used to get all favorite videos, with restriction to a given site or searchquery (both can be null).
        /// The implementation should already filter videos in case the age confirmation is used and no age has been confirmed yet.
        /// </summary>
        /// <param name="siteId">The name of a site to restrict the returned videos to. (if null, don't restrict to a site)</param>
        /// <param name="query">A search string to filter the resulting videos. (if null, don't restrict to a term)</param>
        /// <returns>A list of <see cref="VideoInfo"/> objects matching the parameters.</returns>
        List<VideoInfo> GetFavoriteVideos(string siteId, string query);
        
        /// <summary>
        /// Get a list of categories the use
        /// </summary>
        /// <param name="siteId">The name of a site to return categories for</param>
        /// <returns>A list of <see cref="Category"/> objects for the given site.</returns>
        List<Category> GetFavoriteCategories(string siteId);
        
        List<string> GetFavoriteCategoriesNames(string siteId);
        
        bool AddFavoriteCategory(Category category, string siteName);
        
        bool RemoveFavoriteCategory(Category category);
        
        bool RemoveFavoriteCategory(string siteName, string recursiveCategoryName);
    }

    public class FavoriteVideoInfo : VideoInfo
    {
        /// <summary>Holds an unique Id for the Video, so it can be deleted from the DB.</summary>
        public int Id { get; set; }

        /// <summary>Holds the name of the Site where this video came from.</summary>
        public string SiteName { get; set; }
    }
}

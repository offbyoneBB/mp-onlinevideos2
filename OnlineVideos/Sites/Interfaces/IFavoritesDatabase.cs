using System;
using System.Collections.Generic;

namespace OnlineVideos
{
    public interface IFavoritesDatabase
    {
        List<KeyValuePair<string, uint>> getSiteIDs();
        bool addFavoriteVideo(VideoInfo foVideo, string titleFromUtil, string siteName);
        bool removeFavoriteVideo(VideoInfo foVideo);
        bool removeAllFavoriteVideos(string fsSiteId);
        /// <summary>
        /// Method used to get all favorite videos, with restriction to a given site or searchquery (both can be null).
        /// The implementation should already filter videos in case the age confirmation is used and no age has been confirmed yet.
        /// </summary>
        /// <param name="fsSiteId">The name of a site to restrict the returned videos to. (if null, don't restrict to a site)</param>
        /// <param name="fsQuery">A search string to filter the resulting videos. (if null, don't restrict to a term)</param>
        /// <returns>A list of <see cref="VideoInfo"/> objects matching the parameters.</returns>
        List<VideoInfo> getFavoriteVideos(string fsSiteId, string fsQuery);
        /// <summary>
        /// Get a list of categories the use
        /// </summary>
        /// <param name="siteId">The name of a site to return categories for</param>
        /// <returns>A list of <see cref="Category"/> objects for the given site.</returns>
        List<Category> getFavoriteCategories(string siteId);
        bool addFavoriteCategory(Category cat, string siteName);
        bool removeFavoriteCategory(Category cat);
    }
}

using System;
using System.Collections.Generic;

namespace OnlineVideos
{
    public interface IFavoritesDatabase
    {
        string[] getSiteIDs();
        bool addFavoriteVideo(VideoInfo foVideo, string titleFromUtil, string siteName);
        bool removeFavoriteVideo(VideoInfo foVideo);
        List<VideoInfo> getFavoriteVideos(string fsSiteId, string fsQuery);
    }
}

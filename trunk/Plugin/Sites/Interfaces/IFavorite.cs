using System;
using System.Collections.Generic;

namespace OnlineVideos
{
    interface IFavorite
    {
        List<VideoInfo> getFavorites();
        void addFavorite(VideoInfo video);
        void removeFavorite(VideoInfo video);
    }
}

using System;
using System.Collections.Generic;

namespace OnlineVideos
{
    public interface IFavorite
    {
        List<VideoInfo> getFavorites();
        void addFavorite(VideoInfo video);
        void removeFavorite(VideoInfo video);
    }
}

using System;
using System.Collections.Generic;

namespace OnlineVideos
{
    interface IFavorite
    {
        List<VideoInfo> getFavorites(String fsUsername, String fsPassword);
        void addFavorite(VideoInfo video, String fsUsername, String Password);
        void removeFavorite(VideoInfo video, String fsUsername, String Password);
    }
}

using System;
using System.IO;
using OnlineVideos;
using TraktPlugin;
using MediaPortal.Configuration;

namespace OnlineVideos.MediaPortal1.Trakt
{
    public static class TraktPluginHelper
    {
        private static bool traktPresent = File.Exists(Path.Combine(Config.GetSubFolder(Config.Dir.Plugins, "Windows"), "TraktPlugin.dll"));
        
        // alternate way of checking: 
        //private const int TraktWindowId = 87258;
        //private static bool traktLoaded = GUIWindowManager.GetWindow(TraktWindowId) != null;

        public static bool IsWatched(ITrackingInfo trackingInfo)
        {
            if (traktPresent && trackingInfo != null)
            {
                try
                {
                    switch (trackingInfo.VideoKind)
                    {
                        case VideoKind.TvSeries: return SafeIsWatchedSeriesSimple(trackingInfo);
                        case VideoKind.Movie: return SafeIsWatchedMovie(trackingInfo);
                        case VideoKind.Other: break;
                        default:
                            {
                                OnlineVideos.Log.Error("{0} not supported for Trakt", trackingInfo.VideoKind);
                                break;
                            }
                    }
                }
                catch (Exception e)
                {
                    OnlineVideos.Log.Error("Exception getting watched status: {0}", e.Message);
                }
            }
            return false;
        }


        // keep all references to TraktPlugin in separate methods, so that methods that are called from GUIOnlineVideos don't throw an ecxeption it Trakt isn't loaded
        
        
        /*
        for the time being: disabled because of too much requests to trakt in some cases
        private static Dictionary<string, int> seriesTitleCache = new Dictionary<string, int>();
        private static bool SafeIsWatchedSeriesFuzzy(ITrackingInfo trackingInfo)
        {
            string cacheKey = trackingInfo.Title.ToLowerInvariant();
            if (!seriesTitleCache.ContainsKey(cacheKey))
            {
                var shows = TraktAPI.SearchShows(cacheKey, 1);
                var first = shows.FirstOrDefault();
                if (first != null && first.Show.Ids.Trakt.HasValue)
                    seriesTitleCache.Add(cacheKey, first.Show.Ids.Trakt.Value);
            }

            if (seriesTitleCache.ContainsKey(cacheKey))
            {
                int id = seriesTitleCache[cacheKey];
                {
                    var watchedEpisodes = TraktCache.GetWatchedEpisodesFromTrakt(true);
                    foreach (var episode in watchedEpisodes)
                    {
                        if (id == episode.ShowId && episode.Season == trackingInfo.Season && episode.Number == trackingInfo.Episode)
                            return true;
                    }
                };
            };
            return false;
        }
        */

        private static bool SafeIsWatchedSeriesSimple(ITrackingInfo trackingInfo)
        {
            var watchedEpisodes = TraktCache.GetWatchedEpisodesFromTrakt(true);
            if (watchedEpisodes != null)
                foreach (var episode in watchedEpisodes)
                {
                    if (episode.ShowTitle.ToLowerInvariant() == trackingInfo.Title.ToLowerInvariant() &&
                        episode.Season == trackingInfo.Season &&
                        episode.Number == trackingInfo.Episode)
                        return true;
                }
            return false;
        }

        private static bool SafeIsWatchedMovie(ITrackingInfo trackingInfo)
        {
            var watchedMovies = TraktCache.GetWatchedMoviesFromTrakt(true);
            if (watchedMovies != null)
                foreach (var movies in watchedMovies)
                {
                    if ((trackingInfo.Year == 0 || trackingInfo.Year == movies.Movie.Year) &&
                        (String.IsNullOrEmpty(trackingInfo.ID_IMDB) || String.IsNullOrEmpty(movies.Movie.Ids.Imdb) || trackingInfo.ID_IMDB == movies.Movie.Ids.Imdb) &&
                        trackingInfo.Title.ToLowerInvariant() == movies.Movie.Title.ToLowerInvariant())
                        return true;
                }
            return false;
        }
    }

}

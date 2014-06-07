using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace OnlineVideos.Sites.DavidCalder.TMDB
{
  class BackgroundWorker
  {
    Thread backgroundThread = null;

    public void start(List<VideoInfo> videos)
    {
      backgroundThread = new Thread(delegate()
                      {
                        try
                        {
                          foreach (VideoInfo video in videos)
                          {
                            if (TMDB.TMDB_Movie25.SearchForMovie(video))
                            {
                              TMDbVideoDetails newInfo = new TMDbVideoDetails();
                              newInfo.TMDbDetails = TMDB.TMDB_Movie25.MovieInfo;
                              if (!string.IsNullOrEmpty(newInfo.TMDbDetails.runtime))
                                video.Length = newInfo.TMDbDetails.runtime + " min";
                              if (!string.IsNullOrEmpty(newInfo.TMDbDetails.ReleaseDateAsString()))
                                video.Airdate = newInfo.TMDbDetails.ReleaseDateAsString();
                              if (!string.IsNullOrEmpty(newInfo.TMDbDetails.overview))
                                video.Description = newInfo.TMDbDetails.overview;
                              if (!string.IsNullOrEmpty(newInfo.TMDbDetails.PosterPathFullUrl()))
                                video.ImageUrl = newInfo.TMDbDetails.PosterPathFullUrl();
                              if (newInfo.TMDbDetails.id != null)
                                video.Id = Convert.ToInt32(newInfo.TMDbDetails.id);
                              video.Title2 = "Full Movie";
                              if (newInfo != null)
                                video.Other = newInfo;
                              video.HasDetails = true;
                            }
                            else video.HasDetails = false;
                          }
                        }

                        catch
                        {

                        }
                      });
      backgroundThread.Start();
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites.DavidCalder.TMDB
{
  class TMDB_MopVideo
  {
    public static TVSeries SeriesInfo { get; set; }
    public static SearchResult results { get; set; }
    public static string Title;

    public static bool SearchForTVSeries(VideoInfo video)
    {
      Title = video.Title;
      return SearchForTVSeries(Title);
    }

    public static bool SearchForTVSeries(string Title)
    {
      results = TMDB_API.SearchTitle(Title);
      if (results.total_results == 1)
      {
        SeriesInfo = GetTVSeriesData(results.ID);
        return true;
      }
      else if (results.total_results > 1)
      {
        return FilterToOne();
      }
      return false;
    }

    private static bool FilterToOne()
    {
      List<Result> movielist = new List<Result>();
      foreach (Result result in results.results)
      {
        if (result.title.Replace(" ", "") == Title.Replace(" ", ""))
        {
          movielist.Add(result);
        }
      }
      if (movielist.Count == 1)
      {
        SeriesInfo = GetTVSeriesData(movielist[0].id); return true;
      }
      return false;
    }

    public static TVSeries GetTVSeriesData(string Id)
    {
      SeriesInfo = TMDB_API.GetSeriesDetails(Id);
      return SeriesInfo;
    }
  }
}

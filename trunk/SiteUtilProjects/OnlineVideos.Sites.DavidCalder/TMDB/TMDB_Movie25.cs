using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites.DavidCalder.TMDB
{
  class TMDB_Movie25
  {
    public static Movie MovieInfo { get; set; }
    public static SearchResult results { get; set; }
    public static string Title;
    public static string Year;

    public static Movie GetMovieData(string MovieId)
    {
      MovieInfo = TMDB_API.GetMovieDetails(MovieId);
      return MovieInfo;
    }

    public static bool SearchForMovie(VideoInfo video)
    {
      if (Regex.IsMatch(video.Title, @"\(\d{4}\)", RegexOptions.IgnoreCase))
      {
        Match m0 = Regex.Match(video.Title, @"(?<title>[^(]*)\((?<year>[^\)]*)\)", RegexOptions.IgnoreCase);
        Title = m0.Groups["title"].Value; Year = m0.Groups["year"].Value;
        return SearchForMovie(Title, Year);
      }
      else return false;
    }

    public static bool SearchForMovie(string Title, string Year)
    {
      results = TMDB_API.SearchTitleAndYear(Title, Year);
      if (results.total_results == 1) 
      {
        MovieInfo = GetMovieData(results.ID);
        return true; 
      }
      else if (results.total_results > 1)
      {
        return FilterToOne();
      }
        return false;
    }

    //public static List<VideoInfo> GetTrailers()
    //{
    //  List<VideoInfo> videos = new List<VideoInfo>();
    //  foreach (TMDB.Trailer trailer in MovieInfo.videos.TrailerList())
    //  {
    //    VideoInfo video = new VideoInfo();
    //    video.Description = TMDB.TMDB_Movie25.MovieInfo.overview;
    //    video.ImageUrl = string.Format("http://image.tmdb.org/t/p/w1000/{0}", TMDB.TMDB_Movie25.MovieInfo.Poster_Path);
    //    video.Title = string.Format("{0}  ({1})  –  {2}", Title, Year, trailer.name);
    //    video.Airdate = Convert.ToDateTime(TMDB.TMDB_Movie25.MovieInfo.release_date).ToShortDateString();
    //    video.Title2 = trailer.name;
    //    video.VideoUrl = "https://www.youtube.com/watch?v=" + trailer.key;
    //    videos.Add(video);
    //  }
    //  return videos;
    //}

    private static bool FilterToOne()
    {
      List<Result> movielist = new List<Result>();
      foreach (Result result in results.results)
      {     
        if (result.title.Replace(" ","") == Title.Replace(" ",""))
        {
          movielist.Add(result);
        }
      }
      if (movielist.Count == 1)
      {
        MovieInfo = GetMovieData(movielist[0].id); return true;
      }
      return false;
    }
  }
}

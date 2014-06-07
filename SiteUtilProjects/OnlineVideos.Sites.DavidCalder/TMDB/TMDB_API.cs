using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Net;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites.DavidCalder
{
  public static class TMDB_API
  {
    static string _baseUrl = "https://api.themoviedb.org";
    static string _imageBase = "https://image.tmdb.org/t/p/";
    static string apiKey = "eea3d01f1dc81b27651bf893ec063437";
    static string TmdbId = string.Empty;

    public static SearchResult SearchTitleAndYear(string Title, string Year)
    {
      string searchStr = string.Format("https://api.themoviedb.org/3/search/movie?query={0}&api_key=eea3d01f1dc81b27651bf893ec063437&year={1}", Title, Year);
      var b = GetWebResponce<SearchResult>(searchStr); 
      return b;
    }

    public static SearchResult SearchTitle(string Title)
    {
      string searchStr = string.Format("https://api.themoviedb.org/3/search/movie?query={0}&api_key=eea3d01f1dc81b27651bf893ec063437", Title);
      var b = GetWebResponce<SearchResult>(searchStr);
      return b;
    }

    public static TMDB.Movie GetMovieDetails(string TmdbId)
    {
      string searchStr = string.Format("https://api.themoviedb.org/3/movie/{0}?api_key=eea3d01f1dc81b27651bf893ec063437&append_to_response=releases,videos,credits,images,translations", TmdbId);
      return GetWebResponce<TMDB.Movie>(searchStr);
    }

    public static TMDB.TVSeries GetSeriesDetails(string TmdbId)
    {
      string searchStr = string.Format("https://api.themoviedb.org/3/tv/{0}?api_key=eea3d01f1dc81b27651bf893ec063437&append_to_response=releases,videos,credits,images,translations", TmdbId);
      return GetWebResponce<TMDB.TVSeries>(searchStr);
    }

    private static TMDBInfo GetWebResponce<TMDBInfo>(string PostString) where TMDBInfo : new()
    {
      var request = System.Net.WebRequest.Create(PostString) as System.Net.HttpWebRequest;
      request.Method = "GET";
      request.Accept = "application/json";
      request.ContentLength = 0;
      string responseContent;
      using (var response = request.GetResponse() as System.Net.HttpWebResponse)
      {
        using (var reader = new System.IO.StreamReader(response.GetResponseStream()))
        {
          responseContent = reader.ReadToEnd();
        }
      }
      return !string.IsNullOrEmpty(responseContent) ? JsonConvert.DeserializeObject<TMDBInfo>(responseContent) : new TMDBInfo();
    }
  }
}

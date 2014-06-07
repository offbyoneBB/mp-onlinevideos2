using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;



namespace OnlineVideos.Sites.DavidCalder
{
  public class Movie25Util : DeferredResolveUtil, IChoice
  {
    public static List<VideoInfo> videoList = new List<VideoInfo>();

    public override int DiscoverDynamicCategories()
    {
      base.DiscoverDynamicCategories();
      int i = 0;
      do
      {
        RssLink cat = (RssLink)Settings.Categories[i];
        if (cat.Name == "Submit Links" || cat.Name == "TV Shows")
          Settings.Categories.Remove(cat);
        else
        {
          i++;
        }
      }
      while (i < Settings.Categories.Count);
      return Settings.Categories.Count;
    }

    public override ITrackingInfo GetTrackingInfo(VideoInfo video)
    {
      try
      {
        TrackingInfo tInfo = new TrackingInfo()
        {
          Regex = Regex.Match(video.Title, "(?<Title>[^(]*)((?<Airdate>.*))"),
          VideoKind = VideoKind.Movie
        };

      }
      catch (Exception e)
      {
        Log.Warn("Error parsing TrackingInfo data: {0}", e.ToString());
      }

      return base.GetTrackingInfo(video);
    }

    public override string ResolveVideoUrl(string url)
    {
      string newUrl = url;
      string webData = GetWebData(newUrl);
      Match match = Regex.Match(webData, @"onclick=""location.href='(?<url>[^']*)'""\s*value=""Click\sHere\sto\sPlay""", RegexOptions.IgnoreCase);
      if (match.Success)
      {
        newUrl = match.Groups["url"].Value;
        return GetVideoUrl(newUrl);
      }
      //Log.Info(newUrl);
      return newUrl;
    }

    public override Dictionary<string, string> GetPlaybackOptions(string playlistUrl)
    {
      if (playlistUrl.StartsWith("https://www.youtube.com/watch?v="))
        return Hoster.Base.HosterFactory.GetHoster("Youtube").getPlaybackOptions(playlistUrl);
      else return base.GetPlaybackOptions(playlistUrl);
    }

    public override bool CanSearch
    {
      get
      {
        return true;
      }
    }

    public override List<VideoInfo> getVideoList(Category category)
    {
      List<VideoInfo> videos = base.getVideoList(category);
      TMDB.BackgroundWorker worker = new TMDB.BackgroundWorker();
      worker.start(videos);
      return videos;
    }

    public List<VideoInfo> getVideoChoices(VideoInfo video)
    {
      List<VideoInfo> videos = new List<VideoInfo>();
      if (video.Other.GetType() == typeof(TMDbVideoDetails))
      {
        TMDbVideoDetails videoDetail = (TMDbVideoDetails)video.Other;
        
        foreach (TMDB.Trailer trailer in videoDetail.TMDbDetails.videos.TrailerList())
        {
          VideoInfo clip = new VideoInfo();
          clip.Description = videoDetail.TMDbDetails.overview;
          clip.ImageUrl = videoDetail.TMDbDetails.PosterPathFullUrl();
          clip.Title = string.Format("{0}  –  {1}", video.Title, trailer.name);
          clip.Airdate = videoDetail.TMDbDetails.ReleaseDateAsString();
          clip.Title2 = trailer.name;
          clip.VideoUrl = "https://www.youtube.com/watch?v=" + trailer.key;
          videos.Add(clip);
        }
      }
      videos.Insert(0, video);
      return videos;
    }

    public override List<VideoInfo> Search(string query)
    {    
      searchUrl = string.Format("http://www.movie25.so/search.php?key={0}&submit=", query.Replace(" ", "+"));
      List<VideoInfo> videos = base.Search(searchUrl);
      TMDB.BackgroundWorker worker = new TMDB.BackgroundWorker();
      worker.start(videos);
      return videos;
    }

    public override List<VideoInfo> getNextPageVideos()
    {
      List<VideoInfo> videos = base.getNextPageVideos();
      TMDB.BackgroundWorker worker = new TMDB.BackgroundWorker();
      worker.start(videos);
      return videos;
    }
  }
}

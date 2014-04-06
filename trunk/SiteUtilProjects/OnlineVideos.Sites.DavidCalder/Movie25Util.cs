using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites.DavidCalder
{
  public class Movie25Util : DeferredResolveUtil
  {

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

    //public override List<VideoInfo> getVideoList(Category category)
    //{
    //  List<VideoInfo> vids = base.getVideoList(category); int i = 1;
    //  string search = string.Format("http://www.movieweb.com/search/movies?search={0}", category.Name);
    //  string[] words = category.Name.Split(' ');
    //  string webData = SiteUtilBase.GetWebData(search);
    //  Match m = Regex.Match(webData, @"<div\sclass=""result"">\s*<div\sclass=""img"">\s*<a\shref=""(?<url>[^""]*)""><img\ssrc=""(?<img>[^""]*)""\s*alt=""Noah""\stitle=""(?<title>[^""]*)""\s/></a>\s*</div>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

    //  if (m.Success && words.Any(m.Groups["title"].Value.Contains))
    //  {
    //    webData = SiteUtilBase.GetWebData("http://www.movieweb.com" + m.Groups["url"].Value);
    //    Match n = Regex.Match(webData, @"<a\sonclick=""popVid\('(?<url>[^']*)'\);"">\s*<div\sclass=""txt"">\s*<div\sclass=""title"">(?<title>[^<]*)</div>\s*<div\sclass=""stats"">(?<duration>[^<]*)</div>\s*</div>");

    //    while (n.Success)
    //    {
    //      VideoInfo vid = new VideoInfo();
    //      vid.Title = n.Groups["title"].Value; 
    //      vid.ImageUrl = n.Groups["img"].Value;
    //      vid.VideoUrl = "http://www.movieweb.com/v/" + n.Groups["url"].Value + "/embed_video";
    //      vids.Add(vid); i++;
    //      n = n.NextMatch();
    //    }
    //  }     
    //  return vids;
    //}

    public override string ResolveVideoUrl(string url)
    {
      string newUrl = url;
      string webData = GetWebData(newUrl);
      Match match = Regex.Match(webData, @"onclick=""location.href='(?<url>[^']*)'""\s*value=""Click\sHere\sto\sPlay""", RegexOptions.IgnoreCase);
      if (match.Success)
      {
        newUrl = match.Groups["url"].Value;
      }
      Log.Info(newUrl);
      return GetVideoUrl(newUrl);
    }

  }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites.DavidCalder
{
  public class MopVideoUtil : DeferredResolveUtil//, IChoice
  {
    public override ITrackingInfo GetTrackingInfo(VideoInfo video)
    {
      Log.Debug("Debug Video.Title === " + video.Title);
      try
      {
        TrackingInfo tInfo = new TrackingInfo()
        {
          //Grimm – Season 3 Episode 3 – A Dish Best Served Cold
          Regex = Regex.Match(video.Title, @"(?<Title>.*)\s–\sS(?<Season>\d+)E(?<Episode>\d+)", RegexOptions.IgnoreCase),
          VideoKind = VideoKind.TvSeries
        };
        Match match = Regex.Match(video.Title, @"(?<Title>.*)\s–\sS(?<Season>\d+)E(?<Episode>\d+)");
        Log.Debug("Debug regex match === " + tInfo.Title + " Season " + tInfo.Season + " Episode " + tInfo.Episode);
      }
      catch (Exception e)
      {
        Log.Warn("Error parsing TrackingInfo data: {0}", e.ToString());
      }

      return base.GetTrackingInfo(video);
    }

    public override List<VideoInfo> GetVideos(Category category)
    {
      RssLink link = category as RssLink;
      string page = WebCache.Instance.GetWebData(link.Url);
      Match n = Regex.Match(page, @"<div\sstyle=""width:\s120px;\sheight:20px;overflow:hidden;""><center><a href=""(?<url>[^""]*)"">");
      if (n.Success)
      {
        link.Url = n.Groups["url"].Value;
      }
      return base.GetVideos(link);
    }

    public override VideoInfo CreateVideoInfo()
    {
      return new SeriesVideoInfo();
    }

    public class SeriesVideoInfo : DeferredResolveVideoInfo
    {
      public override string GetPlaybackOptionUrl(string url)
      {
        CookieContainer cc = new CookieContainer();
        string newUrl = base.PlaybackOptions[url];
        string stripedurl = newUrl.Substring(newUrl.IndexOf("http://mopvideo"));
        string data = WebCache.Instance.GetWebData(stripedurl, cookies: cc, referer: newUrl);

        //<a href="http://hoster/ekqiej2ito9p"
        Match n = Regex.Match(data, @"<a\shref=""(?<url>[^""]*)""[^>]*>");
        if (n.Success)
          return GetVideoUrl(n.Groups["url"].Value);

        Match n1 = Regex.Match(data, @"<iframe\ssrc="".(?<url>[^""]*)""");
        if (n1.Success)
        {
          string linkurl = n1.Groups["url"].Value.Substring(n1.Groups["url"].Value.IndexOf("linkurl="));
          //linkurl = linkurl.Remove(linkurl.IndexOf("&linkfrom"));
          string[] parts = linkurl.Split(new[] { "linkurl=" }, StringSplitOptions.None);
          if (parts.Length == 2)
          {
            byte[] tmp = Convert.FromBase64String(parts[1]);
            string i = Encoding.ASCII.GetString(tmp);
            byte[] tmp2 = Convert.FromBase64String(i);
            string i2 = Encoding.ASCII.GetString(tmp2);

            return GetVideoUrl(i2);
          }
        }
        return url;
      }
    }

    public override int DiscoverDynamicCategories()
    {
      int res = base.DiscoverDynamicCategories();

      foreach (Category cat in Settings.Categories)
      {
        Match m = Regex.Match(cat.Name, @"S\d+E\d+");
        Match m1 = Regex.Match(cat.Name, @"\d\d\d\d\.\d+\.\d+");
        if (m.Success)
          cat.Name = Regex.Replace(cat.Name, m.Value, "").Trim();
        else if (m1.Success)
          cat.Name = Regex.Replace(cat.Name, m1.Value, "").Trim();
      }
      return res;
    }

    public override int DiscoverNextPageCategories(NextPageCategory category)
    {
      int res = base.DiscoverNextPageCategories(category);

      foreach (RssLink cat in Settings.Categories)
      {
        Match m = Regex.Match(cat.Name, @"S\d+E\d+");
        Match m1 = Regex.Match(cat.Name, @"\d\d\d\d\.\d+\.\d+");
        if (m.Success)
          cat.Name = Regex.Replace(cat.Name, @"S\d+E\d+", "");
        else if (m1.Success)
          cat.Name = Regex.Replace(cat.Name, m1.Value, "").Trim();
      }
      return res;
    }
  }
}

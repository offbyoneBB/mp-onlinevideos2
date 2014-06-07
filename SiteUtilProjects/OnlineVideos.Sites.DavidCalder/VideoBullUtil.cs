using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;


namespace OnlineVideos.Sites.DavidCalder
{
    public class VideoBullUtil : DeferredResolveUtil
    {
      public override ITrackingInfo GetTrackingInfo(VideoInfo video)
      {
        Log.Debug("Debug Video.Title === " + video.Title);
        try
        {
          TrackingInfo tInfo = new TrackingInfo()
          {
            //Grimm – Season 3 Episode 3 – A Dish Best Served Cold
            Regex = Regex.Match(video.Title, @"(?<Title>.*)\s–\sSeason\s(?<Season>\d+)\sEpisode\s(?<Episode>\d+)", RegexOptions.IgnoreCase),
            VideoKind = VideoKind.TvSeries
          };
          Match match = Regex.Match(video.Title, @"(?<Title>.*)\s–\sSeason\s(?<Season>\d+)\sEpisode\s(?<Episode>\d+)");
          Log.Debug("Debug regex match === " + tInfo.Title + " Season " + tInfo.Season + " Episode " + tInfo.Episode);
        }
        catch (Exception e)
        {
          Log.Warn("Error parsing TrackingInfo data: {0}", e.ToString());
        }

        return base.GetTrackingInfo(video);
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
          string stripedurl = newUrl.Substring(newUrl.IndexOf("http://videobull"));
          string data = SiteUtilBase.GetWebData(stripedurl, cc, newUrl);

          //<a href="http://hoster/ekqiej2ito9p"
          Match n = Regex.Match(data, @"<a\shref=""(?<url>[^""]*)""\starget=""_blank"">");
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
          if (cat.Name.Contains("Season") == true)
          {
            cat.Name = cat.Name.Substring(0, cat.Name.IndexOf("Season")).Trim();

          }
        }
        return res;
      }


      public override int DiscoverNextPageCategories(NextPageCategory category)
      {
        int res = base.DiscoverNextPageCategories(category);

        foreach (RssLink cat in Settings.Categories)
        {
          if (cat.Name.Contains("Season") == true)
          {
            cat.Name = cat.Name.Substring(0, cat.Name.IndexOf("Season")).Trim();
          }
        }
        return res;
      }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites.DavidCalder
{
  public class MovieBeUtil : DeferredResolveUtil
  {
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
        string stripedurl = newUrl.Substring(newUrl.IndexOf("http://moviebe"));
        string data = SiteUtilBase.GetWebData(stripedurl, cc, newUrl);

        //<a href="http://hoster/ekqiej2ito9p"
        Match n = Regex.Match(data, @"<a\shref=""http://(?<m0>[^/]*)/(?<m1>[^/]*)[^""]*"">");
        if (n.Success)
          return GetVideoUrl(string.Format("http://{0}/{1}", n.Groups["m0"].Value, n.Groups["m1"].Value));

        //<iframe src="./vidiframe.php?linkurl=aHR0cDovL3d3dy5wdXRsb2NrZXIuY29tL2ZpbGUvRENEQUZEOEEzQkVBMjgzMg=="          
        Match n1 = Regex.Match(data, @"<iframe\ssrc="".(?<url>[^""]*)""");

        string videoUrl = "http://moviebe.com/wp-content/themes/videozoom" + n1.Groups["url"].Value;
        string newData = SiteUtilBase.GetWebData(videoUrl, cc);
        Match n2 = Regex.Match(newData, @"<iframe\ssrc=""(?<url>[^""]*)""");
        if (n2.Success)
          return GetVideoUrl(n2.Groups["url"].Value);

        return string.Empty;
      }
    }
  }
}

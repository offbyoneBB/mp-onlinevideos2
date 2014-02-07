using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;


namespace OnlineVideos.Sites.DavidCalder
{
    public class MopVideoUtil : DeferredResolveUtil
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
                string stripedurl = newUrl.Substring(newUrl.IndexOf("http://www.mopvideo"));
                string data = SiteUtilBase.GetWebData(stripedurl, cc, newUrl);

                //<a href="http://hoster/ekqiej2ito9p"
                Match n = Regex.Match(data, @"<a\shref=""(?<url>[^""]*)""\starget=""_blank");
                if (n.Success)
                    return GetVideoUrl(n.Groups["url"].Value);

                //<iframe src="./vidiframe.php?linkurl=aHR0cDovL3d3dy5wdXRsb2NrZXIuY29tL2ZpbGUvRENEQUZEOEEzQkVBMjgzMg=="          
                Match n1 = Regex.Match(data, @"<iframe\ssrc="".(?<url>[^""]*)""");

                string videoUrl = "http://www.mopvideo.com/wp-content/themes/videozoom" + n1.Groups["url"].Value;
                string newData = SiteUtilBase.GetWebData(videoUrl, cc);
                Match n2 = Regex.Match(newData, @"<iframe\ssrc=""(?<url>[^""]*)""");
                if (n2.Success)
                    return GetVideoUrl(n2.Groups["url"].Value);

                return string.Empty;
            }
        }

    }
}

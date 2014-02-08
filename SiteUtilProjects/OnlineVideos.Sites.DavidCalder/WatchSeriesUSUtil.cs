using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites.DavidCalder
{

    public class WatchSeriesUSUtil : DeferredResolveUtil
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
                string newUrl = base.PlaybackOptions[url];
                string[] parts = newUrl.Split(new[] { "embeded.php?title=" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    byte[] tmp = Convert.FromBase64String(parts[1]);
                    return GetVideoUrl(Encoding.ASCII.GetString(tmp));
                }
                string result = GetVideoUrl(newUrl);
                return (result);
            }
        }
    }
}

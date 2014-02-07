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

        

        public override string ResolveVideoUrl(string url)
        {
            string newUrl = url;
            string webData = GetWebData(newUrl);
            Match match = Regex.Match(webData, @"onclick=""location.href='(?<url>[^']*)'""\s*value=""Click\sHere\sto\sPlay""",RegexOptions.IgnoreCase);
            if (match.Success == false)
            {
                Log.Info("Internal Regex failed to match, Please contact Quarter to update");

            }
            newUrl = match.Groups["url"].Value;
            Log.Info(newUrl);
            return GetVideoUrl(newUrl);
        }

    }

}

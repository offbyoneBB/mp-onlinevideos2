using System;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class MyVideoSerienUtil : GenericSiteUtil
    {
        public override String getUrl(VideoInfo video)
        {
          string videoUrl = video.ImageUrl.Replace("/thumbs", "").Replace(".jpg", ".flv");
          videoUrl = Regex.Replace(videoUrl, @"(?<before>.*\d+)_\d*(?<after>.flv)", "${before}${after}");
          return videoUrl;
        }
    }
}
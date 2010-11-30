using System;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class MyVideoSerienUtil : GenericSiteUtil
    {
        public override String getUrl(VideoInfo video)
        {
            string videoId = video.VideoUrl.Substring(0, video.VideoUrl.LastIndexOf("/"));
            videoId = videoId.Substring(videoId.LastIndexOf("/") + 1);
            string url = video.ImageUrl.Substring(0, video.ImageUrl.IndexOf("/thumbs")) + "/" + videoId + ".flv";
            return url;
        }
    }
}
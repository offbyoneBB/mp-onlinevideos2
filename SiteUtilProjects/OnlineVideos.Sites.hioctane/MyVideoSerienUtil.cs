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
            
            string data = GetWebData(video.VideoUrl);
            string regex = @"/de/(?<url>[^t]+)thumbs/" + videoId;
            Match m = Regex.Match(data, regex);
            if (m.Success)
            {
                string part = m.Groups["url"].Value;
                m = Regex.Match(data, @"addVariable\('SERVER','(?<url>[^']+)'\)");
                if(m.Success){
                    string server = m.Groups["url"].Value;
                    return "http://" + server + "/" + part + videoId + ".flv";
                }
            }
            return null;
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using RssToolkit.Rss;

namespace OnlineVideos.Sites
{
    public class MyVideoUtil : SiteUtilBase
    {
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to get the actual file url from the video link.")]
        string videoUrlRegEx = @"V=(http[^&]+\.flv)";

        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the rss item description.")]
        string infoRegEx = @"\</a\>(?<desc>.*)Stichwörter\:.*Länge\:\s(?<duration>[0-9\:]+)\<br/\>";

        Regex regEx_VideoUrl, regEx_Info;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            if (!string.IsNullOrEmpty(videoUrlRegEx)) regEx_VideoUrl = new Regex(videoUrlRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant);
            if (!string.IsNullOrEmpty(infoRegEx)) regEx_Info = new Regex(infoRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant);
        }

        public override String getUrl(VideoInfo video)
        {
            string lsUrl = GetRedirectedUrl(video.VideoUrl);
            Match m = regEx_VideoUrl.Match(lsUrl);
            if (m.Success) lsUrl = System.Web.HttpUtility.UrlDecode(m.Groups[1].Value);
            return lsUrl;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {            
            List<VideoInfo> loVideoList = new List<VideoInfo>();
            foreach (RssItem rssItem in GetWebData<RssDocument>(((RssLink)category).Url).Channel.Items)
            {
                Match mInfo = regEx_Info.Match(rssItem.Description);
                if (mInfo.Success)
                {
                    VideoInfo video = new VideoInfo();
                    video.Description = mInfo.Groups["desc"].Value.Replace("<br />", "\n").Trim();
                    video.Length = mInfo.Groups["duration"].Value;                    
                    video.ImageUrl = rssItem.MediaThumbnails[0].Url;
                    video.Title = rssItem.MediaTitle;
                    video.VideoUrl = string.Format("http://www.myvideo.de/movie/{0}", Regex.Match(rssItem.Link, "watch/([\\d]*)").Groups[1].Value);
                    loVideoList.Add(video);
                }
            }
            return loVideoList;
        }
    }
}

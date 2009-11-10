using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using RssToolkit.Rss;

namespace OnlineVideos.Sites
{
    public class DreiSatUtil : SiteUtilBase
    {
        public enum VideoQuality { low, high }

        [Category("OnlineVideosUserConfiguration"), Description("Defines the maximum quality for the video to be played.")]
        VideoQuality videoQuality = VideoQuality.high;

        public override String getUrl(VideoInfo video)
        {
            return ParseASX(video.VideoUrl)[0];
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> loVideoList = new List<VideoInfo>();
            foreach (RssItem rssItem in GetWebDataAsRss(((RssLink)category).Url).Channel.Items)
            {
                if (rssItem.MediaGroups.Count > 0)
                {
                    VideoInfo video = new VideoInfo();
                    video.Title = rssItem.Title;
                    video.Description = rssItem.Description;
                    if (rssItem.MediaGroups[0].MediaThumbnails.Count > 0) video.ImageUrl = rssItem.MediaGroups[0].MediaThumbnails[0].Url;
                    foreach(RssItem.MediaContent videoChoice in rssItem.MediaGroups[0].MediaContents)
                    {
                        if (videoChoice.Type == "video/x-ms-asf" && videoChoice.Bitrate > 300) // only wmv and no mobile versions
                        {
                            video.VideoUrl = videoChoice.Url;                            
                            video.Length = TimeSpan.FromSeconds(int.Parse(videoChoice.Duration)).ToString();
                            if ((videoQuality == VideoQuality.high && videoChoice.Bitrate >= 1000) || 
                                (videoQuality == VideoQuality.low && videoChoice.Bitrate < 1000)) break;
                        }
                    }
                    video.Length += " | " + rssItem.PubDateParsed.ToString("g");                    
                    loVideoList.Add(video);
                }
            }
            return loVideoList;
        }
    }
}

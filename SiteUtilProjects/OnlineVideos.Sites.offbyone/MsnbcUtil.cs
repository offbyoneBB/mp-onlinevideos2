using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using RssToolkit.Rss;

namespace OnlineVideos.Sites
{
	/// <summary>
    /// Description of MsnbcUtil.
	/// </summary>
	public class MsnbcUtil: SiteUtilBase
	{
        [Category("OnlineVideosConfiguration"), Description("Format string applied to the video url of an item that was found in the rss.")]
        string videoUrlFormatString = "http://www.msnbc.msn.com/default.cdnx/id/{0}/displaymode/1157/?t=.flv";
        
		public override string GetVideoUrl(OnlineVideos.VideoInfo video)
		{
            if (video.VideoUrl.Contains("/vp/"))
            {
                return String.Format(videoUrlFormatString, video.VideoUrl.Substring(video.VideoUrl.LastIndexOf("#") + 1));
            }
            else
            {
                return String.Format(videoUrlFormatString, new Uri(video.VideoUrl).Segments.Last().Trim('/'));
            }
		}

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> loVideoList = new List<VideoInfo>();
            foreach (RssItem rssItem in GetWebData<RssDocument>(((RssLink)category).Url).Channel.Items)
            {
                if (rssItem.Guid.Text.ToLower().StartsWith("http"))
                {
                    VideoInfo video = new VideoInfo();
                    video.Description = rssItem.Description;
                    if (rssItem.MediaContents.Count > 0 && rssItem.MediaContents[0].Url != null && rssItem.MediaContents[0].Url.ToLower().EndsWith(".jpg"))
                        video.ImageUrl = rssItem.MediaContents[0].Url;
                    video.Title = rssItem.Title.Replace("Video: ", "");
                    video.Airdate = rssItem.PubDateParsed.ToString("g", OnlineVideoSettings.Instance.Locale);
                    video.VideoUrl = rssItem.Guid.Text;
                    loVideoList.Add(video);
                }
            }
            return loVideoList;
        }
	}
}

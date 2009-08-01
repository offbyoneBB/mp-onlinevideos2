using System;
using System.Collections.Generic;
using System.Collections;
using System.Xml;
using System.Text;
using System.Net;
using MediaPortal.GUI.Library;
using MediaPortal.Configuration;

namespace OnlineVideos.Sites
{
	/// <summary>
	/// Description of GenericSiteUtil.
	/// </summary>
	public class GenericSiteUtil : SiteUtilBase
	{
        public override String getUrl(VideoInfo video, SiteSettings foSite)
        {
            string url = GetRedirectedUrl(video.VideoUrl);
            if (url.ToLower().EndsWith(".asx"))
            {
                url = ParseASX(url)[0];
            }
            return url;
        }

		public override List<VideoInfo> getVideoList(Category category)
		{
            List<VideoInfo> loVideoList = new List<VideoInfo>();
            VideoInfo video;

            if (category is RssLink)
            {
                List<RssItem> loRssItemList = getRssDataItems(((RssLink)category).Url);
                foreach (RssItem rssItem in loRssItemList)
                {
                    video = new VideoInfo();
                    if (!String.IsNullOrEmpty(rssItem.description))
                    {
                        video.Description = rssItem.description;
                    }
                    else
                    {
                        video.Description = rssItem.mediaDescription;
                    }
                    if (!String.IsNullOrEmpty(rssItem.mediaThumbnail))
                    {
                        video.ImageUrl = rssItem.mediaThumbnail;
                    }
                    else if (!String.IsNullOrEmpty(rssItem.exInfoImage))
                    {
                        video.ImageUrl = rssItem.exInfoImage;
                    }
                    //get the video
                    if (!String.IsNullOrEmpty(rssItem.enclosure) && isPossibleVideo(rssItem.enclosure))
                    {
                        video.VideoUrl = rssItem.enclosure;
                        video.Length = rssItem.enclosureDuration != null ? rssItem.enclosureDuration : rssItem.pubDate; // if no duration at least display the Publication date
                    }
                    else if (rssItem.contentList.Count > 0)
                    {
                        foreach (MediaContent content in rssItem.contentList)
                        {
                            if (isPossibleVideo(content.url))
                            {
                                video.VideoUrl = content.url;
                                video.Length = content.duration;
                                break;
                            }
                        }
                    }
                    video.Title = rssItem.title;
                    if (String.IsNullOrEmpty(video.VideoUrl) == false)
                    {
                        loVideoList.Add(video);
                    }
                }
            }
            else if (category is Group)
            {
                    foreach (Channel channel in ((Group)category).Channels)
                    {
                        video = new VideoInfo();
                        video.Title = channel.StreamName;
                        video.VideoUrl = channel.Url;
                        video.ImageUrl = channel.Thumb;
                        loVideoList.Add(video);      
                    }                            
            }
			return loVideoList;
		}
		
	}
}

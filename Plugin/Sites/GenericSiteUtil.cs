using System;
using System.Collections.Generic;
using System.Collections;
using System.Xml;
using System.Text;
using System.Net;
using MediaPortal.GUI.Library;
using MediaPortal.Configuration;
using RssToolkit.Rss;

namespace OnlineVideos.Sites
{
	/// <summary>
	/// Description of GenericSiteUtil.
	/// </summary>
	public class GenericSiteUtil : SiteUtilBase
	{
        public override String getUrl(VideoInfo video)
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
                foreach (RssItem rssItem in GetWebDataAsRss(((RssLink)category).Url).Channel.Items)
                {
                    video = new VideoInfo();
                    if (!String.IsNullOrEmpty(rssItem.Description))
                    {
                        video.Description = rssItem.Description;
                    }
                    else
                    {
                        video.Description = rssItem.MediaDescription;
                    }
                    if (rssItem.MediaThumbnails.Count > 0)
                    {
                        video.ImageUrl = rssItem.MediaThumbnails[0].Url;
                    }
                    else if (!string.IsNullOrEmpty(rssItem.GT_Image))
                    {
                        video.ImageUrl = rssItem.GT_Image;
                    }
                    else if (rssItem.MediaContents.Count > 0 && rssItem.MediaContents[0].MediaThumbnails.Count > 0)
                    {
                        video.ImageUrl = rssItem.MediaContents[0].MediaThumbnails[0].Url;
                    }
                    //get the video
                    if (rssItem.Enclosure != null && isPossibleVideo(rssItem.Enclosure.Url))
                    {
                        video.VideoUrl = rssItem.Enclosure.Url;

                        if (!string.IsNullOrEmpty(rssItem.Enclosure.Length))
                        {
                            int bytesOrSeconds = 0;
                            if (int.TryParse(rssItem.Enclosure.Length, out bytesOrSeconds))
                            {
                                if (bytesOrSeconds > 18000) // won't be longer than 5 hours if Length is guessed as seconds, so it's bytes
                                    video.Length = (bytesOrSeconds / 1024).ToString("N0") + " KB";
                                else
                                    video.Length = rssItem.Enclosure.Length + " sec";
                            }
                            else
                            {
                                video.Length = rssItem.Enclosure.Length;
                            }
                        }                        

                        if (!string.IsNullOrEmpty(rssItem.PubDate))
                        {
                            if (video.Length != "") video.Length += " | ";
                            try
                            {
                                video.Length += rssItem.PubDateParsed.ToString("g");
                            }
                            catch
                            {
                                video.Length += rssItem.PubDate;
                            }                            
                        }
                    }
                    else if (rssItem.MediaContents.Count > 0)
                    {
                        foreach (RssItem.MediaContent content in rssItem.MediaContents)
                        {
                            if (isPossibleVideo(content.Url))
                            {
                                video.VideoUrl = content.Url;
                                video.Length = content.Duration;
                                break;
                            }
                        }
                    }
                    video.Title = rssItem.Title;
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

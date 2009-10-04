using System;
using MediaPortal.GUI.Library;
using System.Text.RegularExpressions;
using System.Net;
using System.Text;
using MediaPortal.Player;
using System.Collections.Generic;
using MediaPortal.GUI.View ;
using MediaPortal.Dialogs;
using System.Xml;
using System.Xml.XPath;
using System.ComponentModel;
using System.Threading;

namespace OnlineVideos.Sites
{
    public class GameTrailersUtil : SiteUtilBase 
    {
        static string videoRegExp = @"<a\shref=""(http://www\.gametrailers\.com/download/[^""]+\.wmv)"">";
        static Regex loUrlRegex = new Regex(videoRegExp, RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public override String getUrl(VideoInfo video)
        {
            string lsUrl = "";
            string lsHtml = GetWebData(video.VideoUrl);

            Match urlField = loUrlRegex.Match(lsHtml);
            if (urlField.Success)
            {
                lsUrl = urlField.Groups[1].Value;
            }
            return lsUrl;
        }

        public override List<VideoInfo> getVideoList(Category category)
		{
			List<RssItem> loRssItemList = getRssDataItems((category as RssLink).Url);
			List<VideoInfo> loVideoList = new List<VideoInfo>();
			VideoInfo video;
			foreach(RssItem rssItem in loRssItemList){
				video = new VideoInfo();
				video.Description = rssItem.description;
				video.ImageUrl = rssItem.exInfoImage;
				video.Title = rssItem.title;
                video.Other = rssItem.gameID;
                if (rssItem.contentList != null && rssItem.contentList.Count > 0)
                {
                    foreach (MediaContent media in rssItem.contentList)
                    {
                        if (!string.IsNullOrEmpty(media.url) && media.type=="wmv")
                        {
                            video.VideoUrl = media.url;                            
                        }
                    }
                }
                if (!(string.IsNullOrEmpty(video.VideoUrl))) loVideoList.Add(video);
			}
			return loVideoList;
		}        
    }
}

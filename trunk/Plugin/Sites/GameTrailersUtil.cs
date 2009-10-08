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
using RssToolkit.Rss;

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
			List<VideoInfo> loVideoList = new List<VideoInfo>();			
            foreach (RssItem rssItem in GetWebDataAsRss(((RssLink)category).Url).Channel.Items)
            {
                VideoInfo video = new VideoInfo();
				video.Description = rssItem.Description;
				video.ImageUrl = rssItem.GT_Image;
				video.Title = rssItem.Title;
                video.Length = rssItem.PubDateParsed.ToString("g");
                video.Other = rssItem.GT_GameId;                
                foreach (RssItem.GT_File media in rssItem.GT_Files)
                {
                    if (!string.IsNullOrEmpty(media.Url) && media.Type=="wmv")
                    {
                        video.VideoUrl = media.Url;
                        break;
                    }
                }                
                if (!(string.IsNullOrEmpty(video.VideoUrl))) loVideoList.Add(video);
			}
			return loVideoList;
		}        
    }
}

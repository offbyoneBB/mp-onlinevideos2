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
        public enum MediaType { mov, wmv };

        string videoRegExp = @"<a\shref=""(http://www\.gametrailers\.com/download/[^""]+\.{0})"">";
        Regex loUrlRegex;

        [Category("OnlineVideosUserConfiguration"), Description("GT offers up to 4 different file types for the same trailer.")]
        MediaType preferredMediaType = MediaType.wmv;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            loUrlRegex = new Regex(string.Format(videoRegExp, preferredMediaType.ToString()), RegexOptions.Compiled | RegexOptions.CultureInvariant);
        }

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
                try { video.Length= rssItem.PubDateParsed.ToString("g"); }
                catch { video.Length = rssItem.PubDate; }
                video.Other = rssItem.GT_GameId;                
                foreach (RssItem.GT_File media in rssItem.GT_Files)
                {
                    if (media.Type != "mp4")
                    {
                        if (!string.IsNullOrEmpty(media.Url)) video.VideoUrl = media.Url;
                        if (media.Type == preferredMediaType.ToString()) break;
                    }
                }                
                if (!(string.IsNullOrEmpty(video.VideoUrl))) loVideoList.Add(video);
			}
			return loVideoList;
		}        
    }
}

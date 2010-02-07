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
        public enum MediaType { mov, wmv,flv };

        [Category("OnlineVideosUserConfiguration"), Description("GT offers up to 4 different file types for the same trailer.")]
        MediaType preferredMediaType = MediaType.flv;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
        }

        public override String getUrl(VideoInfo video)
        {
            string xmlUrl = "http://www.gametrailers.com/neo/?page=xml.mediaplayer.Mediagen&movieId=";
            string data;
            int idx1 = video.VideoUrl.LastIndexOf("/")+1;
            int idx2 = video.VideoUrl.IndexOf(".", idx1);
            xmlUrl = xmlUrl + video.VideoUrl.Substring(idx1, idx2 - idx1);
            string lsUrl = "";

            if (!string.IsNullOrEmpty(xmlUrl))
            {
                data = String.Empty;
                data = GetWebData(xmlUrl);

                if (!string.IsNullOrEmpty(data))
                {

                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(data);
                    XmlElement root = doc.DocumentElement;
                    XmlNodeList list;
                    list = root.SelectNodes("./video/item/rendition/src");
                    lsUrl = root.SelectSingleNode("./video/item/rendition/src").InnerText;
                }
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

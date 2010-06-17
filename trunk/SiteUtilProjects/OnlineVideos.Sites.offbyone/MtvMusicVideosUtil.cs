using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using RssToolkit.Rss;
using System.ComponentModel;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// API documentation at http://developer.mtvnservices.com/docs/
    /// </summary>
    public class MtvMusicVideosUtil : SiteUtilBase
    {
        [Category("OnlineVideosConfiguration"), Description("Format string applied to the video url of an item that was found in the rss.")]
        string videoUrlFormatString = "http://api-media.mtvnservices.com/player/embed/includes/mediaGen.jhtml?uri={0}&ref=None";
        [Category("OnlineVideosConfiguration"), Description("Format string used as Url for getting the results of a search. {0} will be replaced with the query.")]
        string searchUrl = "http://api.mtvnservices.com/1/video/search/?term={0}&sort=date_descending";
        [Category("OnlineVideosUserConfiguration"), Description("Defines number of videos to display per page.")]
        int pageSize = 27;

        List<VideoInfo> GetVideoForCurrentCategory()
        {
            string finalUrl = string.Format("{0}&max-results={1}&start-index={2}", currentCategory.Url, pageSize, currentStart);
            RssDocument rss = GetWebDataAsRss(finalUrl);
            currentCategory.EstimatedVideoCount = (uint)rss.Channel.TotalResults;
            List<VideoInfo> videoList = new List<VideoInfo>();
            foreach (RssItem rssItem in rss.Channel.Items)
            {
                VideoInfo video = new VideoInfo();
                video.Title = rssItem.Description.Substring(0, rssItem.Description.LastIndexOf('|') - 1).Replace('|', '-');
                if (rssItem.MediaThumbnails.Count > 0)
                {
                    video.ImageUrl = rssItem.MediaThumbnails[Math.Min(rssItem.MediaThumbnails.Count, rssItem.MediaThumbnails.Count / 2)].Url;
                }                
                if (rssItem.MediaContents.Count > 0)
                {
                    video.Length = ((int)float.Parse(rssItem.MediaContents[0].Duration, new System.Globalization.CultureInfo("en-us"))).ToString();
                    video.VideoUrl = rssItem.MediaContents[0].Url;
                    videoList.Add(video);
                }
            }
            return videoList;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            currentCategory = category as RssLink;
            currentStart = 0;
            return GetVideoForCurrentCategory();
        }

        public override string getUrl(VideoInfo video)
        {
            string playlist = GetWebData(string.Format(videoUrlFormatString, new System.Uri(video.VideoUrl).AbsolutePath.Substring(1)));
            if (playlist.Length > 0)
            {
                XmlDocument data = new XmlDocument();
                data.LoadXml(playlist);
                string url = ((XmlElement)data.SelectSingleNode("//src")).InnerText;
                if (!url.EndsWith(".swf")) // country block
                    return url;
            }
            return "";
        }       

        #region Next/Previous Page

        RssLink currentCategory;
        int currentStart = 0;

        public override bool HasNextPage
        {
            get { return currentCategory != null && currentCategory.EstimatedVideoCount > currentStart; }
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            currentStart += pageSize;
            return GetVideoForCurrentCategory();
        }

        public override bool HasPreviousPage
        {
            get { return currentCategory != null && currentStart > 0; }
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            currentStart -= pageSize;
            if (currentStart < 0) currentStart = 0;
            return GetVideoForCurrentCategory();
        }

        #endregion        

        #region Search

        public override bool CanSearch { get { return true; } }

        public override List<VideoInfo> Search(string query)
        {
            //You must URL-escape all spaces, punctuation and quotes. The search term "buddy holly" would look like this %22buddy+holly%22 
            query = System.Web.HttpUtility.UrlEncode(query.Replace(" ", "+"));
            return getVideoList(new RssLink() { Url = string.Format(searchUrl, query) });
        }

        #endregion
    }
}

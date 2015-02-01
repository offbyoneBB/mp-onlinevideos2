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
        int pageSize = 26;
        [Category("OnlineVideosUserConfiguration"), Description("Proxy to use for getting the playback url. Define like this: 83.84.85.86:8118")]
        string proxy = null;

        List<VideoInfo> GetVideoForCurrentCategory()
        {
            string finalUrl = string.Format("{0}&max-results={1}&start-index={2}", currentCategory.Url, pageSize, currentStart);
            RssDocument rss = GetWebData<RssDocument>(finalUrl);
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

        public override List<VideoInfo> GetVideos(Category category)
        {
            currentCategory = category as RssLink;
            currentStart = 1;
            return GetVideoForCurrentCategory();
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            System.Net.WebProxy proxyObj = null; // new System.Net.WebProxy("127.0.0.1", 8118);
            if (!string.IsNullOrEmpty(proxy)) proxyObj = new System.Net.WebProxy(proxy);

            string playlist = GetWebData(string.Format(videoUrlFormatString, new System.Uri(video.VideoUrl).AbsolutePath.Substring(1)), proxy: proxyObj);
            if (playlist.Length > 0)
            {
                if (playlist.IndexOf("error_country_block.swf") >= 0) throw new OnlineVideosException("Video blocked for your country.");
                string url = "";
                XmlDocument data = new XmlDocument();
                data.LoadXml(playlist);
                video.PlaybackOptions = new Dictionary<string, string>();
                foreach (XmlElement elem in data.SelectNodes("//rendition"))
                {
                    url = ((XmlElement)elem.SelectSingleNode("src")).InnerText;
                    if (!url.EndsWith(".swf"))
                    {
                        video.PlaybackOptions.Add(string.Format("{0}x{1} | {2} | .{3}", elem.GetAttribute("width"), elem.GetAttribute("height"), elem.GetAttribute("bitrate"), elem.GetAttribute("type").Substring(elem.GetAttribute("type").Length - 3)), url);
                    }
                }
                return url;
            }
            return "";
        }

        #region Next Page

        RssLink currentCategory;
        int currentStart = 1;

        public override bool HasNextPage
        {
            get { return currentCategory != null && currentCategory.EstimatedVideoCount > currentStart; }
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            currentStart += pageSize;
            return GetVideoForCurrentCategory();
        }
        
        #endregion        

        #region Search

        public override bool CanSearch { get { return true; } }

        public override List<ISearchResultItem> Search(string query, string category = null)
        {
            //You must URL-escape all spaces, punctuation and quotes. The search term "buddy holly" would look like this %22buddy+holly%22 
            query = System.Web.HttpUtility.UrlEncode(query.Replace(" ", "+"));
            return GetVideos(new RssLink() { Url = string.Format(searchUrl, query) })
                .ConvertAll<ISearchResultItem>(v => v as ISearchResultItem);
        }

        #endregion
    }
}

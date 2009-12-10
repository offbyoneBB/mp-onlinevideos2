using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using RssToolkit.Rss;
using System.IO;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// API documentation at http://developer.mtvnservices.com/docs/Home
    /// </summary>
    public class MtvMusicVideosUtil : SiteUtilBase
    {
        string videoUrls = "http://api-media.mtvnservices.com/player/embed/includes/mediaGen.jhtml?uri={0}&ref=None";
        string genreVideosMethod = "http://api.mtvnservices.com/1/genre/{0}/videos/?sort=date_descending";
        string searchMethod = "http://api.mtvnservices.com/1/video/search/?term={0}&sort=date_descending";
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

        public override List<VideoInfo> getVideoList(Category category)
        {
            currentCategory = category as RssLink;
            currentStart = 0;
            return GetVideoForCurrentCategory();
        }

        public override string getUrl(VideoInfo video)
        {
            string playlist = GetWebData(string.Format(videoUrls, new System.Uri(video.VideoUrl).AbsolutePath.Substring(1)));
            if (playlist.Length > 0)
            {
                XmlDocument data = new XmlDocument();
                data.LoadXml(playlist);
                string url = ((XmlElement)data.SelectSingleNode("//rendition/src")).InnerText;
                string resultUrl = string.Format("http://127.0.0.1:{0}/stream.flv?rtmpurl={1}", OnlineVideoSettings.RTMP_PROXY_PORT, System.Web.HttpUtility.UrlEncode(url));
                return resultUrl;
            }
            return "";
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            Settings.Categories.Add(new RssLink() { Url = string.Format(genreVideosMethod, "world_reggae"), Name = "World/Reggae", HasSubCategories = false, SubCategoriesDiscovered = true });
            Settings.Categories.Add(new RssLink() { Url = string.Format(genreVideosMethod, "pop"), Name = "Pop", HasSubCategories = false, SubCategoriesDiscovered = true });            
            Settings.Categories.Add(new RssLink() { Url = string.Format(genreVideosMethod, "metal"), Name = "Metal", HasSubCategories = false, SubCategoriesDiscovered = true });                        
            Settings.Categories.Add(new RssLink() { Url = string.Format(genreVideosMethod, "environmental"), Name = "Environmental", HasSubCategories = false, SubCategoriesDiscovered = true });                       
            Settings.Categories.Add(new RssLink() { Url = string.Format(genreVideosMethod, "latin"), Name = "Latin", HasSubCategories = false, SubCategoriesDiscovered = true });            
            Settings.Categories.Add(new RssLink() { Url = string.Format(genreVideosMethod, "randb"), Name = "R&B", HasSubCategories = false, SubCategoriesDiscovered = true });            
            Settings.Categories.Add(new RssLink() { Url = string.Format(genreVideosMethod, "rock"), Name = "Rock", HasSubCategories = false, SubCategoriesDiscovered = true });                        
            Settings.Categories.Add(new RssLink() { Url = string.Format(genreVideosMethod, "easy_listening"), Name = "Easy", HasSubCategories = false, SubCategoriesDiscovered = true });            
            Settings.Categories.Add(new RssLink() { Url = string.Format(genreVideosMethod, "jazz"), Name = "Jazz", HasSubCategories = false, SubCategoriesDiscovered = true });            
            Settings.Categories.Add(new RssLink() { Url = string.Format(genreVideosMethod, "country"), Name = "Country", HasSubCategories = false, SubCategoriesDiscovered = true });            
            Settings.Categories.Add(new RssLink() { Url = string.Format(genreVideosMethod, "hip_hop"), Name = "Hip-Hop", HasSubCategories = false, SubCategoriesDiscovered = true });            
            Settings.Categories.Add(new RssLink() { Url = string.Format(genreVideosMethod, "classical"), Name = "Classical", HasSubCategories = false, SubCategoriesDiscovered = true });                       
            Settings.Categories.Add(new RssLink() { Url = string.Format(genreVideosMethod, "electronic_dance"), Name = "Electronic / Dance", HasSubCategories = false, SubCategoriesDiscovered = true });            
            Settings.Categories.Add(new RssLink() { Url = string.Format(genreVideosMethod, "blues_folk"), Name = "Blues / Folk", HasSubCategories = false, SubCategoriesDiscovered = true });            
            Settings.Categories.Add(new RssLink() { Url = string.Format(genreVideosMethod, "alternative"), Name = "Alternative", HasSubCategories = false, SubCategoriesDiscovered = true });            
            Settings.Categories.Add(new RssLink() { Url = string.Format(genreVideosMethod, "soundtracks_musicals"), Name = "Soundtracks / Musicals", HasSubCategories = false, SubCategoriesDiscovered = true });
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override string GetFileNameForDownload(VideoInfo video, string url)
        {
            string safeName = ImageDownloader.GetSaveFilename(video.Title);
            return safeName + ".flv";
        }

        #region Search

        public override bool CanSearch { get { return true; } }

        public override List<VideoInfo> Search(string query)
        {
            //You must URL-escape all spaces, punctuation and quotes. The search term "buddy holly" would look like this %22buddy+holly%22 
            query = System.Web.HttpUtility.UrlEncode(query.Replace(" ", "+"));
            return getVideoList(new RssLink() { Url = string.Format(searchMethod, query) });
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Web;
using System.Net;

namespace OnlineVideos.Sites
{
    public class SweflixUtil : LatestVideosSiteUtilBase, IChoice
    {
        #region OnlineVideosConfiguration

        [Category("OnlineVideosConfiguration"), Description("Base url")]
        protected string baseUrl;
        [Category("OnlineVideosConfiguration"), Description("Url for search")]
        protected string searchUrl;
        [Category("OnlineVideosConfiguration"), Description("Url for latest videos")]
        protected string latestUrl;

        #endregion

        private string currentUrl;
        private int currentPage;

        #region Categories


        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Insert(0, new RssLink() { Name = "Senast inlagda", Url = latestUrl });
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }
        #endregion

        #region Videos

        private List<VideoInfo> GetVideos()
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            HtmlDocument doc = GetWebData<HtmlDocument>(currentUrl + currentPage + "/");
            foreach (HtmlNode item in doc.DocumentNode.Descendants("a").Where(a => a.SelectNodes("div[@class = 'movie']") != null && (a.GetAttributeValue("href", "").Contains("/film/") /* || a.GetAttributeValue("href","").Contains("/tv/") */)))
            {
                videos.Add(new VideoInfo() { Title = HttpUtility.HtmlDecode(item.SelectSingleNode("div/img").GetAttributeValue("alt", "").Trim()), VideoUrl = item.GetAttributeValue("href", ""), Thumb = item.SelectSingleNode("div/img").GetAttributeValue("src", ""), HasDetails = true });
            }
            HasNextPage = videos.Count > 0;
            currentPage++;
            return videos;
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            return GetVideos();
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            currentUrl = (category as RssLink).Url;
            currentPage = 1;
            return GetVideos();
        }

        public override string GetFileNameForDownload(VideoInfo video, Category category, string url)
        {
            //Extension always .mp4
            return Helpers.FileUtils.GetSaveFilename(video.Title) + ".mp4";
        }

        public override List<VideoInfo> GetLatestVideos()
        {
            currentPage = 1;
            currentUrl = latestUrl;
            List<VideoInfo> videos = GetVideos();
            return videos.Count >= LatestVideosCount ? videos.GetRange(0, (int)LatestVideosCount) : new List<VideoInfo>();
        }

        List<DetailVideoInfo> IChoice.GetVideoChoices(VideoInfo video)
        {
            string url = baseUrl + video.VideoUrl.Replace("/film/", "/play/");
            List<DetailVideoInfo> choices = new List<DetailVideoInfo>();
            HtmlDocument data = GetWebData<HtmlDocument>(url);
            HtmlNode doc = data.DocumentNode;
            url = doc.SelectSingleNode("//source").GetAttributeValue("src", "");

            DetailVideoInfo detailNoSub = new DetailVideoInfo();
            detailNoSub.Title = video.Title;
            detailNoSub.Thumb = video.Thumb;
            detailNoSub.VideoUrl = url;
            detailNoSub.Title2 = "Ingen textning";
            choices.Add(detailNoSub);

            foreach (HtmlNode track in doc.SelectNodes("//track"))
            {
                DetailVideoInfo detail = new DetailVideoInfo();
                detail.Title = video.Title;
                detail.Thumb = video.Thumb;
                detail.VideoUrl = url;
                detail.Title2 = "Textning: " + track.GetAttributeValue("label", "");
                string sub = GetWebData(baseUrl + track.GetAttributeValue("src", ""));
                string[] lines = Regex.Split(sub, "\n").Skip(1).ToArray();
                detail.SubtitleText = string.Join("\n", lines);
                choices.Add(detail);
            }

            return choices;
        }

        #endregion

        #region Search

        public override bool CanSearch
        {
            get
            {
                return true;
            }
        }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            List<SearchResultItem> results = new List<SearchResultItem>();
            Regex rgx = new Regex(@"<a href=""(?<url>[^""]*)"".*?<img title=""(?<title>[^""]*)"".*?src=""(?<img>[^""]*)", RegexOptions.Singleline);
            foreach (Match m in rgx.Matches(GetWebData(searchUrl + HttpUtility.UrlEncode(query))))
            {
                string url = m.Groups["url"].Value;
                if (url.Contains("/film/")) //only movies
                    results.Add(new VideoInfo() { Title = m.Groups["title"].Value, VideoUrl = url, Thumb = m.Groups["img"].Value, HasDetails = true });
            }
            HasNextPage = false;
            return results;
        }

        #endregion

    }
}

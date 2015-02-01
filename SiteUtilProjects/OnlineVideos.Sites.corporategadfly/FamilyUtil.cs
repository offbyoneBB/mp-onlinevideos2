using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;

using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Site Util for Family
    /// </summary>
    public class FamilyUtil : GenericSiteUtil
    {
        private static string baseUrlPrefix = @"http://www.family.ca";
        private static string categoriesUrl = baseUrlPrefix + @"/video/scripts/getCats.php";
        private static string videosUrlFormat = baseUrlPrefix + @"/video/scripts/getVids.php?id={0}&page={1}";
        private static string loadTokenUrl = baseUrlPrefix + @"/video/scripts/loadTokenAS3.php";
        private static string rtmpUrlFormat = @"rtmpe://{0}.edgefcs.net/ondemand?{1}";
        private static string playPathFormat = @"mp4:videos/family/{0}";
        private static string swfUrl = @"http://www.family.ca/static/swf/VideoPlayer.swf?20120425";
        
        private static Regex loadTokenRegex = new Regex(@"error=&uri=(?<token>.+)",
                                                        RegexOptions.Compiled);
        
        private Category currentCategory = null;
        
        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            Settings.Categories.Add(
                new RssLink() { Name = "Featured Videos", Url = string.Format(videosUrlFormat, "featured", "1")}
               );
            Settings.Categories.Add(
                new RssLink() { Name = "Highest Rated", Url = string.Format(videosUrlFormat, "rated", "1")}
               );
            Settings.Categories.Add(
                new RssLink() { Name = "Most Watched", Url = string.Format(videosUrlFormat, "most", "1")}
               );
            Settings.Categories.Add(
                new RssLink() { Name = "Recently Watched", Url = string.Format(videosUrlFormat, "recent", "1")}
               );
            Settings.Categories.Add(
                new RssLink() { Name = "All Video", Url = string.Format(videosUrlFormat, "all", "1")}
               );
            
            string data = GetWebData(categoriesUrl);
            if (!string.IsNullOrEmpty(data))
            {
                JArray categories = JArray.Parse(data);
                if (categories != null)
                {
                    foreach (JToken category in categories)
                    {
                        Settings.Categories.Add(
                            new RssLink() { Name = category.Value<string>("title"), Url = string.Format(videosUrlFormat, category.Value<int>("id").ToString(), "1")}
                           );
                    }
                }
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            return getVideoListForSinglePage(category, ((RssLink) category).Url);
        }

        private List<VideoInfo> getVideoListForSinglePage(Category category, string url)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            nextPageUrl = "";
            currentCategory = category;

            // retrieve contents of URL using JSON
            JObject json = GetWebData<JObject>(url);
            if (json != null)
            {
                JArray videos = json["video"] as JArray;
                int totalVideoCount = json.Value<int>("total");
                
                var queryParamList = HttpUtility.ParseQueryString(new Uri(url).Query);
                int currentPage = int.Parse(queryParamList["page"]);

                // 12 videos per page, so figure out how many videos are on previous pages + current page videos
                if (totalVideoCount > (videos.Count + (currentPage - 1) * 12))
                {
                    int nextPage = currentPage + 1;
                    Log.Debug(@"Found next page: {0}", nextPage);
                    
                    // replace page parameter with new value
                    queryParamList["page"] = nextPage.ToString();
                    // construct next page URL
                    string urlWithoutQuery = url.IndexOf('?') >= 0 ? url.Substring(0, url.IndexOf('?')) : url;
                    nextPageUrl = string.Format(@"{0}?{1}", urlWithoutQuery, queryParamList);
                }
                
                if (videos != null)
                {
                    foreach (JToken video in videos)
                    {
                        result.Add(new VideoInfo() {
                                       VideoUrl = video.Value<string>("filename"),
                                       Length = TimeSpan.FromSeconds(video.Value<int>("length")).ToString(),
                                       ImageUrl = string.Format(@"{0}{1}{2}", baseUrlPrefix, video.Value<string>("imagePath"), video.Value<string>("image")),
                                       Title = video.Value<string>("title"),
                                       Description = video.Value<string>("description")
                                   });
                    }
                }
                
            }

            return result;
        }
        
        public override bool HasNextPage {
            get { return !string.IsNullOrEmpty(nextPageUrl); }
        }
        
        public override List<VideoInfo> GetNextPageVideos()
        {
            return getVideoListForSinglePage(currentCategory, nextPageUrl);
        }
        
        public override string GetVideoUrl(VideoInfo video)
        {
            string result = "";
            string webData = GetWebData(loadTokenUrl);
            
            if (!string.IsNullOrEmpty(webData))
            {
                Match loadTokenMatch = loadTokenRegex.Match(webData);
                
                if (loadTokenMatch.Success)
                {
                    string token = HttpUtility.UrlDecode(loadTokenMatch.Groups["token"].Value);
                    // scientific way to figure out which host the video is hosted on
                    string host = video.VideoUrl.StartsWith("D_") ? "cp107996" : "cp107997";
                    string url = string.Format(rtmpUrlFormat, host, token);
                    MPUrlSourceFilter.RtmpUrl rtmpUrl = new MPUrlSourceFilter.RtmpUrl(url) {
                        PlayPath = string.Format(playPathFormat, video.VideoUrl),
                        SwfVerify = true,
                        SwfUrl = swfUrl
                    };
                    Log.Debug(@"rtmp url: {0} playpath: {1}", url, rtmpUrl.PlayPath);
                    result = rtmpUrl.ToString();
                }
            }
            return result;
        }
    }
}

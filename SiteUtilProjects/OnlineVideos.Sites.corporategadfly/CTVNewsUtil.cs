using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;

using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class CTVNewsUtil : GenericSiteUtil
    {
        private static Regex mainCategoriesRegex = new Regex(@"<li>\s+<a\s+href=""(?<url>[^""]*)""[^>]*>(?<title>[^<]*)</a>",
                                                             RegexOptions.Compiled);
        private static Regex videoListRegex = new Regex(@"<img.*?src='(?<thumb>[^']*)'\s/>.*?clip\.id\s=\s(?<clipId>[^;]*);\s+clip\.title\s=\s""(?<title>[^""]*)"";.*?clip\.description\s=\s""(?<description>[^""]*)"";",
                                                        RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex manifestRegex = new Regex(@"Video\.Load\((?<json>[^\)]*)\)",
                                                       RegexOptions.Compiled);
        private static Regex nextPageRegex = new Regex(@"<span\sclass=""videoPaginationNext"">\s+<a\shref=""javascript:getPlaylists\('(?<binId>[^']*)',\s'(?<pageNumber>[^']*)',\s'12'\);"">Next\s&gt;</a>\s+</span>",
                                                       RegexOptions.Compiled);
        
        protected static string videoListUrlFormat = @"{0}/{1}?ot=example.AjaxPageLayout.ot&maxItemsPerPage=12&pageNum={2}";
        private static string urlgenFormat = @"http://esi.ctv.ca/datafeed/flv/urlgenjsext.aspx?formatid=27&timeZone=4&vid={0}";
        private static string manifestUrlFormat = @"{0}?hdcore=";
            
        private Category currentCategory = null;

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            string webData = GetWebData(baseUrl);

            if (!string.IsNullOrEmpty(webData))
            {
                foreach (Match m in mainCategoriesRegex.Matches(webData))
                {
                    string url = m.Groups["url"].Value;
                    
                    if (url.IndexOf("binId") == -1) continue;
                    RssLink cat = new RssLink();

                    cat.Name = m.Groups["title"].Value;
                    string binId = HttpUtility.ParseQueryString(new Uri(url).Query)["binId"];
                    cat.Url = string.Format(videoListUrlFormat, baseUrl, binId, "1");
                    cat.HasSubCategories = false;

                    Settings.Categories.Add(cat);
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
            
            string webData = GetWebData(url);

            if (!string.IsNullOrEmpty(webData))
            {
                foreach (Match m in videoListRegex.Matches(webData))
                {
                    VideoInfo info = new VideoInfo();
                    info.Title = m.Groups["title"].Value;
                    info.ImageUrl = m.Groups["thumb"].Value;
                    info.Description = m.Groups["description"].Value;
                    info.VideoUrl = m.Groups["clipId"].Value;

                    result.Add(info);
                }
            }

            Match nextPageMatch = nextPageRegex.Match(webData);
            if (nextPageMatch.Success)
            {
                nextPageUrl = string.Format(videoListUrlFormat, baseUrl, nextPageMatch.Groups["binId"], nextPageMatch.Groups["pageNumber"]);
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
            string result = string.Empty;
            string webData = GetWebData(string.Format(urlgenFormat, video.VideoUrl));
            Log.Debug(@"urlgen output: {0}", webData);
            if (!string.IsNullOrEmpty(webData))
            {
                Match manifestMatch = manifestRegex.Match(webData);
                JToken json = JToken.Parse(manifestMatch.Groups["json"].Value);
                result = string.Format(manifestUrlFormat, json.Value<string>("url"));
                Log.Debug(@"Manifest URL: {0}", result);
            }
            return result;
        }
    }
}

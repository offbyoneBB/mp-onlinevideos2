using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Collections.Specialized;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class TEDTalksUtil : SiteUtilBase
    {
        string tagsUrl = "http://www.ted.com/talks/tags";
        string themesUrl = "http://www.ted.com/themes";
        string newestReleasesUrl = "http://www.ted.com/talks?lang=en&event=&duration=&sort=newest&tag=&page=1";
        string mostViewedUrl = "http://www.ted.com/talks?lang=en&event=&duration=&sort=mostviewed&tag=&page=1";
        string mostPopularUrl = "http://www.ted.com/talks?lang=en&event=&duration=&sort=mostpopular&tag=&page=1";
        string tagsRegex = @"<li><a\s+href=""(?<url>/talks[^""]+)"">(?<title>[^\(\n]+)\((?<amount>\d+)\)</a></li>";
        string themesRegex = @"<li\s+class=""clearfix"">\s*<div\s+class=""col"">\s*<a\s+title=""[^""]*""\s+href=""(?<url>[^""]+)"">\s*
<img\s+alt=""[^""]*""\s+src=""(?<thumb>[^""]+)""\s*/>\s*</a>\s*</div>\s*
<h4>\s*<a[^>]*>(?<title>[^<]+)</a>\s*</h4>\s*<p[^>]*>\s*<span[^>]*>(?<amount>[^<]+)</span>[^<]*</p>\s*
<p>(?<desc>[^<]*)</p>\s*</li>";
        string lastPageIndexRegex = @"Showing page \d+ of (?<lastpageindex>\d+)";
        string nextPageUrlRegex = @"<li><a class=""next"" href=""(?<url>[^""]+)"">Next <span class=""bull"">&raquo;</span></a></li>";
        string videosRegex = @"<img alt=""[^""]*"" src=""(?<thumb>[^""]+)"".*?<h\d[^>]*>\s*<a.*?href=""(?<url>[^""]+)"">(?<title>[^<]*)</a>\s*</h\d>.*?<em[^>]*>\s*(<span[^>]*>)?(?<length>\d+\:\d+)(</span>)?\s+Posted\:\s*(?<aired>.*?)</em>";
        string talkDetailsRegex = @"<script\s+type\s*=\s*""text/javascript"">var\s+talkDetails\s*=\s*(?<json>{.*?})\s*</script>";

        string nextPageUrl;

        public override int DiscoverDynamicCategories()
        {
            if (Settings.Categories == null) Settings.Categories = new BindingList<Category>();
            Settings.Categories.Clear();
            Settings.Categories.Add(new RssLink() { Name = "Newest Releases", Url = newestReleasesUrl, Other = "X-Requested-With:XMLHttpRequest" });
            Settings.Categories.Add(new RssLink() { Name = "Most Viewed", Url = mostViewedUrl, Other = "X-Requested-With:XMLHttpRequest" });
            Settings.Categories.Add(new RssLink() { Name = "Most Popular", Url = mostPopularUrl, Other = "X-Requested-With:XMLHttpRequest" });
            Settings.Categories.Add(new RssLink() { Name = "Tags", Url = tagsUrl, Other = tagsRegex, HasSubCategories = true });
            Settings.Categories.Add(new RssLink() { Name = "Themes", Url = themesUrl, Other = themesRegex, HasSubCategories = true });
            Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > 0;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string pageData = GetWebData((parentCategory as RssLink).Url);
            if (parentCategory.SubCategories == null) parentCategory.SubCategories = new List<Category>();
            parentCategory.SubCategories.Clear();
            var match = Regex.Match(pageData, (string)parentCategory.Other, RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
            while (match.Success)
            {
                parentCategory.SubCategories.Add(new RssLink()
                {
                    Name = HttpUtility.HtmlDecode(match.Groups["title"].Value).Trim(),
					Url = new Uri(new Uri((parentCategory as RssLink).Url), (parentCategory.Name == "Themes" ? match.Groups["url"].Value + "?page=1" : match.Groups["url"].Value.Replace("/tags", "/tags/name") + "/page/1")).AbsoluteUri,
                    EstimatedVideoCount = uint.Parse(match.Groups["amount"].Value),
                    Description = HttpUtility.HtmlDecode(match.Groups["desc"].Value).Trim(),
                    Thumb = match.Groups["thumb"].Value,
                    ParentCategory = parentCategory,
					Other = parentCategory.Name == "Themes" ? "X-Requested-With:XMLHttpRequest" : "sort=date"
                });                
                match = match.NextMatch();
            }
            parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
            return parentCategory.SubCategories.Count;
        }

		public override List<VideoInfo> getVideoList(Category category)
		{
			HasNextPage = false;
			nextPageUrl = null;

			string firstPageData = "";

			if (((string)category.Other).Contains('='))
			{
				firstPageData = GetWebDataFromPost((category as RssLink).Url, (string)category.Other);

				HasNextPage = true;
				nextPageUrl = (category as RssLink).Url.Substring(0, (category as RssLink).Url.LastIndexOf('/') + 1) + "2";
			}
			else if (((string)category.Other).Contains(':'))
			{
				string[] headerArray = ((string)category.Other).Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
				var headers = new NameValueCollection();
				headers.Add(headerArray[0], headerArray[1]);
				headers.Add("Accept", "*/*");
				headers.Add("User-Agent", OnlineVideoSettings.Instance.UserAgent);
				firstPageData = GetWebData((category as RssLink).Url, null, headers, null, null, false, false, null, true);

				HasNextPage = true;
				nextPageUrl = (category as RssLink).Url.Substring(0, (category as RssLink).Url.LastIndexOf('=') + 1) + "2";
			}

			return ParseVideos(firstPageData, false);
		}

        public override List<VideoInfo> getNextPageVideos()
        {
            if (nextPageUrl.Contains("page="))
            {
				var headers = new NameValueCollection();
				headers.Add("X-Requested-With", "XMLHttpRequest");
				headers.Add("Accept", "*/*");
				headers.Add("User-Agent", OnlineVideoSettings.Instance.UserAgent);

                var videos = ParseVideos(GetWebData(nextPageUrl, null, headers, null, null, false, false, null, true));
                uint pageIndex = uint.Parse(nextPageUrl.Substring(nextPageUrl.LastIndexOf('=') + 1)) + 1;
                nextPageUrl = nextPageUrl.Substring(0, nextPageUrl.LastIndexOf('=') + 1) + pageIndex;
                return videos;
            }
            else
            {
                var videos = ParseVideos(GetWebDataFromPost(nextPageUrl, "sort=date"));
                uint pageIndex = uint.Parse(nextPageUrl.Substring(nextPageUrl.LastIndexOf('/') + 1));
                if (pageIndex <= 1)
                {
                    HasNextPage = false;
                    nextPageUrl = null;
                }
                else
                {
                    nextPageUrl = nextPageUrl.Substring(0, nextPageUrl.LastIndexOf('/') + 1) + (pageIndex + 1).ToString();
                }
                return videos;
            }
        }

        List<VideoInfo> ParseVideos(string data, bool reverse = true)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            var match = Regex.Match(data, videosRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Multiline | RegexOptions.Singleline);
            while (match.Success)
            {
                result.Add(new VideoInfo()
                {
                    Title = HttpUtility.HtmlDecode(match.Groups["title"].Value).Trim(),
                    VideoUrl = new Uri(new Uri(tagsUrl), match.Groups["url"].Value).AbsoluteUri,
                    ImageUrl = match.Groups["thumb"].Value,
                    Length = match.Groups["length"].Value.Trim(),
                    Airdate = Utils.PlainTextFromHtml(match.Groups["aired"].Value).Trim()
                });
                match = match.NextMatch();
            }
            if (reverse) result.Reverse();
            return result;
        }

        public override string getUrl(VideoInfo video)
        {
            video.PlaybackOptions = new Dictionary<string, string>();
            string data = GetWebData(video.VideoUrl);
            var talkDetailsMatch = Regex.Match(data, talkDetailsRegex, RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            if (talkDetailsMatch.Success)
            {
                JObject talkDetails = JsonConvert.DeserializeObject(talkDetailsMatch.Groups["json"].Value) as JObject;
                foreach (var htmlStream in talkDetails["htmlStreams"])
                {
                    video.PlaybackOptions.Add(htmlStream.Value<string>("id"), htmlStream.Value<string>("file"));
                }
            }
            return video.PlaybackOptions.Count > 0 ? video.PlaybackOptions.Last().Value : "";
        }
    }
}

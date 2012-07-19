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
        string downloadPageUrlRegex = @"(download_dialog.load\('(?<url>[^']+)')|(<iframe src=""(?<youtubeurl>https?://www.youtube.com/[^&]+)&)";
        string downloadOptionsRegex = @"<input name=""download_quality"" id=""[^""]+"" type=""radio"" data-name=""[^""]+"" value=""(?<value>[^""]*)"".*?/> <label for=""[^""]*"">(?<label>[^<]*)</label>";
        string downloadFileUrlRegex = @"var url = '(?<url>[^{]+{quality}{lang}.mp4)'";

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
            var downloadUrlMatch = Regex.Match(data, downloadPageUrlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
            if (downloadUrlMatch.Success)
            {
                if (downloadUrlMatch.Groups["youtubeurl"].Success)
                {
                    var youtubeHoster = Hoster.Base.HosterFactory.GetHoster("Youtube");
                    video.PlaybackOptions = youtubeHoster.getPlaybackOptions(downloadUrlMatch.Groups["youtubeurl"].Value);
                }
                else
                {
                    data = GetWebData(new Uri(new Uri(video.VideoUrl), downloadUrlMatch.Groups["url"].Value).AbsoluteUri);
                    var optionMatch = Regex.Match(data, downloadOptionsRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
                    var dlUrlMatch = Regex.Match(data, downloadFileUrlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
                    if (optionMatch.Success && dlUrlMatch.Success)
                    {
                        while (optionMatch.Success)
                        {
                            string dlUrl = dlUrlMatch.Groups["url"].Value;
                            dlUrl = dlUrl.Replace("{quality}", optionMatch.Groups["value"].Value);
                            dlUrl = dlUrl.Replace("{lang}", "");
                            video.PlaybackOptions.Add(optionMatch.Groups["label"].Value, dlUrl);
                            optionMatch = optionMatch.NextMatch();
                        }
                    }
                }
            }
            return video.PlaybackOptions.Count > 0 ? video.PlaybackOptions.Last().Value : "";
        }
    }
}

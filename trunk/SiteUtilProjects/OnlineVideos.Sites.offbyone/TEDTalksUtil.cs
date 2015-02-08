using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class TEDTalksUtil : SiteUtilBase
    {
		string baseUrl = "http://www.ted.com/talks/browse";
		string topicsUrl = "http://www.ted.com/watch/topics";
		
		string queryOption_Sort_Newest = "sort=newest";
		string queryOption_Sort_Popular = "sort=popular";
		string queryOption_Filter_Topic = "topics%5B%5D={0}";
		string queryOption_Page = "page={0}";

		string topicsRegex = @"<div\s+class='topics__list__topic'>\s*<div\s+class='h9'>\s*<a\s+href='/topics/(?<query>[^']+)'>(?<name>[^<]+)</a>\s*</div>\s*(?<amount>\d+)\s+talks\s*</div>";
		string nextPageUrlRegex = @"<a\s+class=""pagination__next pagination__flipper pagination__link""\s+rel=""next""\s+href=""(?<url>[^""]+)"">Next</a>";
		string videosRegex = @"<img.*?src=""(?<thumb>[^""]+)"".*?<span\s+class=""thumb__duration"">(?<length>[^<]*)<.*?<h4[^>]*>(?<speaker>[^<]*)</h4>.*?<a\s+href='(?<url>[^']+)'>\s*(?<title>[^<]*)<(.*?<span\s+class='meta__val'>\s*(?<aired>[^>]*)</span>){2}";
        string talkDetailsRegex = @"<script>q\(""talkPage.init"",(?<json>.*?)\)</script>";

        string nextPageUrl;

        public override int DiscoverDynamicCategories()
        {
            if (Settings.Categories == null) Settings.Categories = new BindingList<Category>();
            Settings.Categories.Clear();
			Settings.Categories.Add(new RssLink() { Name = "Newest Releases", Url = string.Format("{0}?{1}&{2}", baseUrl, queryOption_Sort_Newest, string.Format(queryOption_Page, 1)) });
			Settings.Categories.Add(new RssLink() { Name = "Most Viewed", Url = string.Format("{0}?{1}&{2}", baseUrl, queryOption_Sort_Popular, string.Format(queryOption_Page, 1)) });
			Settings.Categories.Add(new RssLink() { Name = "Topics", Url = topicsUrl, Other = topicsRegex, HasSubCategories = true });
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
                    Name = HttpUtility.HtmlDecode(match.Groups["name"].Value).Trim(),
					Url = string.Format("{0}?{1}&{2}&{3}", baseUrl, queryOption_Sort_Newest, string.Format(queryOption_Filter_Topic, match.Groups["query"].Value), string.Format(queryOption_Page, 1)),
                    EstimatedVideoCount = uint.Parse(match.Groups["amount"].Value),
                    ParentCategory = parentCategory
                });                
                match = match.NextMatch();
            }
            parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
            return parentCategory.SubCategories.Count;
        }

		public override List<VideoInfo> GetVideos(Category category)
		{
			return getVideoList((category as RssLink).Url);
		}

        public override List<VideoInfo> GetNextPageVideos()
        {
            return getVideoList(nextPageUrl);
        }

		List<VideoInfo> getVideoList(string url)
		{
			HasNextPage = false;
			nextPageUrl = null;

			string data = GetWebData(url);

            List<VideoInfo> result = new List<VideoInfo>();
            var match = Regex.Match(data, videosRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Multiline | RegexOptions.Singleline);
            while (match.Success)
            {
                result.Add(new VideoInfo()
                {
					Title = string.Format("{0} ({1})", HttpUtility.HtmlDecode(match.Groups["title"].Value).Trim(), HttpUtility.HtmlDecode(match.Groups["speaker"].Value).Trim()),
					VideoUrl = new Uri(new Uri(topicsUrl), match.Groups["url"].Value).AbsoluteUri,
                    Thumb = match.Groups["thumb"].Value,
                    Length = match.Groups["length"].Value.Trim(),
                    Airdate = Helpers.StringUtils.PlainTextFromHtml(match.Groups["aired"].Value).Trim()
                });
                match = match.NextMatch();
            }

			match = Regex.Match(data, nextPageUrlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Multiline | RegexOptions.Singleline);
			if (match.Success)
			{
				HasNextPage = true;
				nextPageUrl = new Uri(new Uri(url), HttpUtility.HtmlDecode(match.Groups["url"].Value)).AbsoluteUri;
			}

            return result;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            video.PlaybackOptions = new Dictionary<string, string>();
            string data = GetWebData(video.VideoUrl);
            var talkDetailsMatch = Regex.Match(data, talkDetailsRegex, RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            if (talkDetailsMatch.Success)
            {
                JObject talkDetails = JsonConvert.DeserializeObject(talkDetailsMatch.Groups["json"].Value) as JObject;
				foreach (JProperty htmlStream in talkDetails["talks"][0]["nativeDownloads"])
                {
                    video.PlaybackOptions.Add(htmlStream.Name, htmlStream.Value.ToString());
                }
            }
            return video.PlaybackOptions.Count > 0 ? video.PlaybackOptions.Last().Value : "";
        }
    }
}

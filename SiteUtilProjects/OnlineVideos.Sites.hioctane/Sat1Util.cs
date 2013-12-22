using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public class Sat1Util : SiteUtilBase
    {
        [Category("OnlineVideosConfiguration"), Description("Url used for category generation.")]
        protected string baseUrl;
        [Category("OnlineVideosConfiguration"), Description("RegEx used to parse Categories from baseUrl")]
		protected string dynamicCategoriesRegEx;
        [Category("OnlineVideosConfiguration"), Description("Second RegEx used to parse Categories from baseUrl")]
        protected string dynamicCategoriesRegEx2;
		[Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for videos. Group names: 'VideoUrl', 'ImageUrl', 'Title', 'Duration', 'Description', 'Airdate'.")]
		protected string videoListRegEx;
        [Category("OnlineVideosConfiguration"), Description("Url to rtmp Server")]
		protected string rtmpBase;

        private Dictionary<string, List<VideoInfo>> data = new Dictionary<string, List<VideoInfo>>();

		public override void Initialize(SiteSettings siteSettings)
		{
			base.Initialize(siteSettings);
		}

		public override int DiscoverDynamicCategories()
		{
            if (Settings.Categories == null) Settings.Categories = new BindingList<Category>();
			string page = GetWebData(baseUrl);
			Match m = Regex.Match(page, dynamicCategoriesRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
			List<RssLink> cats = new List<RssLink>();
			while (m.Success)
			{
                cats.Add(CategoryFromMatch(m));
				m = m.NextMatch();
			}
            if (!string.IsNullOrEmpty(dynamicCategoriesRegEx2))
            {
                m = Regex.Match(page, dynamicCategoriesRegEx2, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
                while (m.Success)
                {
                    var c2 = CategoryFromMatch(m);
                    var c1 = cats.FirstOrDefault(cat => ((RssLink)cat).Url == c2.Url);
                    if (c1 == null) cats.Add(c2);
                    else c1.Name = c2.Name;
                    m = m.NextMatch();
                }
                cats.Sort((c1, c2) => { return c1.Name.CompareTo(c2.Name); });
            }
            if (cats.Count > 0)
            {
                Settings.Categories.Clear();
                cats.ForEach(c => Settings.Categories.Add(c));
                Settings.DynamicCategoriesDiscovered = true;
            }
			return Settings.Categories.Count;
		}

        private RssLink CategoryFromMatch(Match m)
        {
            RssLink cat = new RssLink();
            cat.Url = m.Groups["url"].Value;
            if (!Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(baseUrl), cat.Url).AbsoluteUri;
            cat.Thumb = m.Groups["thumb"].Value;
            if (!String.IsNullOrEmpty(cat.Thumb) && !Uri.IsWellFormedUriString(cat.Thumb, System.UriKind.Absolute)) cat.Thumb = new Uri(new Uri(baseUrl), cat.Thumb).AbsoluteUri;
            cat.Description = Utils.PlainTextFromHtml(m.Groups["description"].Value).Replace('\n', ' ');
            if (m.Groups["title"].Success && !string.IsNullOrEmpty(m.Groups["title"].Value))
                cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
            else
                cat.Name = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cat.Url.Substring(cat.Url.LastIndexOf('/') + 1).Replace('-', ' '));
            if (!Regex.Match(cat.Url, "/videos?$").Success) cat.Url += "/video";
            return cat;
        }

		public override List<VideoInfo> getVideoList(Category category)
		{
			List<VideoInfo> videoList = new List<VideoInfo>();
			string page = GetWebData(((RssLink)category).Url);
			Match m = Regex.Match(page, videoListRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
			while (m.Success)
			{
				VideoInfo videoInfo = new VideoInfo();
                videoInfo.Title = Utils.PlainTextFromHtml(m.Groups["Title"].Value).Replace("\r", " ").Replace('\n', ' ').Trim();
				// get, format and if needed absolutify the video url
				videoInfo.VideoUrl = m.Groups["VideoUrl"].Value;
				if (!Uri.IsWellFormedUriString(videoInfo.VideoUrl, System.UriKind.Absolute)) videoInfo.VideoUrl = new Uri(new Uri(baseUrl), videoInfo.VideoUrl).AbsoluteUri;
				// get, format and if needed absolutify the thumb url
				videoInfo.ImageUrl = m.Groups["ImageUrl"].Value;
				if (!string.IsNullOrEmpty(videoInfo.ImageUrl) && !Uri.IsWellFormedUriString(videoInfo.ImageUrl, System.UriKind.Absolute)) videoInfo.ImageUrl = new Uri(new Uri(baseUrl), videoInfo.ImageUrl).AbsoluteUri;
                videoInfo.Length = Utils.PlainTextFromHtml(m.Groups["Duration"].Value).Replace("\r", " ").Replace('\n', ' ').Trim();
				videoInfo.Airdate = Utils.PlainTextFromHtml(m.Groups["Airdate"].Value);
				videoList.Add(videoInfo);
				m = m.NextMatch();
			}
			return videoList;
		}
 
        public override String getUrl(VideoInfo video)
        {
            string webData = GetWebData(video.VideoUrl);
            string clipId = Regex.Match(webData, @",""id"":""(?<Value>[^""]+)""").Groups["Value"].Value;
            if (!string.IsNullOrEmpty(clipId))
            {
                string link = GetRedirectedUrl("http://www.prosieben.de/dynamic/h264/h264map/?ClipID=" + clipId);
                if (!string.IsNullOrEmpty(link))
                {
                    return link;
                }
            }
            return string.Empty;
        }

    }
}
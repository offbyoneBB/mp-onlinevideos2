using System;
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
        [Category("OnlineVideosConfiguration"), Description("Url used to parse Category Content from Page")]
		protected string dynamicCategoriesRegEx;
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
			string page = GetWebData(baseUrl);
			Match m = Regex.Match(page, dynamicCategoriesRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
			Settings.Categories.Clear();
			while (m.Success)
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
					cat.Name = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cat.Url.Substring(cat.Url.LastIndexOf('/')+1).Replace('-', ' '));
				if (!Regex.Match(cat.Url, "/videos?$").Success) cat.Url += "/video";
				Settings.Categories.Add(cat);
				m = m.NextMatch();
			}
			Settings.DynamicCategoriesDiscovered = true;
			return Settings.Categories.Count;
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
            string url = string.Empty;

            if (webData.Contains("flashdrm_url"))
            {
                url = Regex.Match(webData, @"flashdrm_url"":""(?<Value>[^""]+)""").Groups["Value"].Value;
                url = url.Replace("\\/", "/");
                url = url.Replace("rtmpte", "rtmpe");
                url = url.Replace(".net", ".net:1935");
				url = new MPUrlSourceFilter.RtmpUrl(url) { SwfUrl = "http://www.sat1.de/imperia/moveplayer/HybridPlayer.swf", SwfVerify = true }.ToString();
            }
            else
            {
                string filename = Regex.Match(webData, @"downloadFilename"":""(?<Value>[^""]+)""").Groups["Value"].Value;
                string geo = Regex.Match(webData, @"geoblocking"":""(?<Value>[^""]+)""").Groups["Value"].Value;
                string geoblock = string.Empty;
                if (string.IsNullOrEmpty(geo))
                    geoblock = "geo_d_at_ch/";
                else if (geo.Contains("ww"))
                    geoblock = "geo_worldwide/";
                else if (geo.Contains("de_at_ch"))
                    geoblock = "geo_d_at_ch/";
                else
                    geoblock = "geo_d/";


                if (webData.Contains("flashSuffix") || filename.Contains(".mp4"))
                {
                    url = rtmpBase + geoblock + "mp4:" + filename;
                    if (!url.EndsWith(".mp4")) url = url + ".mp4";
                }
                else
                    url = rtmpBase + geoblock + filename;

				url = new MPUrlSourceFilter.RtmpUrl(url) { SwfUrl = "http://www.sat1.de/imperia/moveplayer/HybridPlayer.swf", SwfVerify = true }.ToString();
            }

            string clipId = Regex.Match(webData, @",""id"":""(?<Value>[^""]+)""").Groups["Value"].Value;
            if (!string.IsNullOrEmpty(clipId))
            {
                string link = GetRedirectedUrl("http://www.prosieben.de/dynamic/h264/h264map/?ClipID=" + clipId);
                if (!string.IsNullOrEmpty(link))
                {
                    if(!link.Contains("h264_na.mp4")){
                        video.PlaybackOptions = new Dictionary<string, string>();
                        video.PlaybackOptions.Add("Flv", url);
                        video.PlaybackOptions.Add("Mp4", link);
                    }
                }
            }

            return url;
        }

    }
}
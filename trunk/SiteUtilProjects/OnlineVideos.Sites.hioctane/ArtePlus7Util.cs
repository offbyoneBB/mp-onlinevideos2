using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public class ArtePlus7Util : SiteUtilBase
    {
		public enum VideoQuality { SD, HD };

		[Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Video Quality", TranslationFieldName = "VideoQuality"), Description("Defines the preferred quality for the video to be played.")]
		VideoQuality videoQuality = VideoQuality.HD;

        protected string baseUrl = "http://videos.arte.tv";
        protected string videoListRegEx = @"<div\s+class=""duration_thumbnail"">(?<Duration>[^<]*)</div>\s*
<a\s+href=""(?<VideoUrl>[^""]+)""><img(?:(?!src).)*src=""(?<ImageUrl>[^""]+)""\s*/></a>\s*
(<div[^>]*>\s*<p\s+class=""teaserText"">(?<Description>[^<]+)</p>\s*</div>)?
(?:(?!<h2>).)*<h2><a[^>]*>(?<Title>[^<]*)?</a></h2>\s*
(?:(?!<p>).)*<p>(?<Airdate>[^<]+)</p>";

        protected Regex regEx_VideoList;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            RegexOptions defaultRegexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture;

            if (!string.IsNullOrEmpty(videoListRegEx)) regEx_VideoList = new Regex(videoListRegEx, defaultRegexOptions);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            Settings.Categories.Add(
                new RssLink()
                {
                    Name = "DE - Sendungen",
                    Url = "http://videos.arte.tv/de/videos/sendungen",
                    HasSubCategories = true
                });
            Settings.Categories.Add(
                new RssLink()
                {
                    Name = "FR - Programmes",
                    Url = "http://videos.arte.tv/fr/videos/programmes",
                    HasSubCategories = true
                });
            Settings.Categories.Add(
                new RssLink()
                {
                    Name = "EN - Programs",
                    Url = "http://videos.arte.tv/en/videos/programs",
                    HasSubCategories = true
                });
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string data = GetWebData((parentCategory as RssLink).Url);
            parentCategory.SubCategories = new List<Category>();            
            if (!string.IsNullOrEmpty(data))
            {
                Match m = Regex.Match(data, @"<a\shref=""(?<url>[^""]+)"">(?<title>[^<]+)</a>\s*\((?<amount>\d+)\)");
                while (m.Success)
                {
                    RssLink cat = new RssLink() { ParentCategory = parentCategory };
                    string url = "http://videos.arte.tv" + m.Groups["url"].Value.Replace("/videos", "/do_delegate/videos");
                    if (!url.EndsWith(".html")) url += "/index.html";
                    cat.Url = url.Replace(".html", "-3188698,view,asThumbnail.html?hash=tv/thumb///{0}/25/");
                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    cat.EstimatedVideoCount = uint.Parse(m.Groups["amount"].Value);
                    parentCategory.SubCategories.Add(cat);
                    
                    m = m.NextMatch();
                }
                parentCategory.SubCategoriesDiscovered = true;
                return Settings.Categories.Count;
            }
            return 0;
        }

        public override String getUrl(VideoInfo video)
        {
            string data = GetWebData(video.VideoUrl);
            data = HttpUtility.UrlDecode(data);

            if (video.PlaybackOptions == null)
            {
                video.PlaybackOptions = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(data))
                {
                    string xmlUrl = Regex.Match(data, @"videorefFileUrl=(?<url>[^""]+)""/>").Groups["url"].Value;
                    if (!string.IsNullOrEmpty(xmlUrl))
                    {
                        List<string> langValues = new List<string>();
                        List<string> urlValues = new List<string>();

                        data = GetWebData(xmlUrl);

                        Match m = Regex.Match(data, @"<video\slang=""(?<lang>[^""]+)""\sref=""(?<url>[^""]+)""\s*/>");
                        while (m.Success)
                        {
                            langValues.Add(m.Groups["lang"].Value);
                            urlValues.Add(m.Groups["url"].Value);
                            m = m.NextMatch();
                        }
                        for (int i = 0; i < langValues.Count; i++)
                        {
                            if (!string.IsNullOrEmpty(urlValues[i]))
                            {
                                string xmlFile = GetWebData(urlValues[i]);
                                if (!string.IsNullOrEmpty(xmlFile))
                                {
                                    Match n = Regex.Match(xmlFile, @"<url\squality=""(?<quality>[^""]+)"">(?<url>[^<]+)</url>");
                                    while (n.Success)
                                    {
                                        string title = langValues[i].ToUpper() + " - " + n.Groups["quality"].Value.ToUpper();

                                        string url = n.Groups["url"].Value;
                                        string host = url.Substring(url.IndexOf(":") + 3, url.IndexOf("/", url.IndexOf(":") + 3) - (url.IndexOf(":") + 3));
                                        string app = url.Substring(host.Length + url.IndexOf(host) + 1, (url.IndexOf("/", url.IndexOf("/", (host.Length + url.IndexOf(host) + 1)) + 1)) - (host.Length + url.IndexOf(host) + 1));
                                        string tcUrl = "rtmp://" + host + ":1935" + "/" + app;
                                        string playPath = url.Substring(url.IndexOf(app) + app.Length + 1);

                                        string resultUrl = new MPUrlSourceFilter.RtmpUrl(url)
										{
											TcUrl = tcUrl,
											App = app,
											SwfUrl = "http://artestras.vo.llnwd.net/o35/geo/arte7/player/ALL/artep7_hd_16_9_v2.swf",
											SwfVerify = true,
											PageUrl = video.VideoUrl,
											PlayPath = playPath
										}.ToString();

                                        if(video.PlaybackOptions.ContainsKey(title)) title += " - 2";
                                        video.PlaybackOptions.Add(title, resultUrl);
                                        n = n.NextMatch();
                                    }
                                }
                            }
                        }

                    }
                }
            }
            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
            {
				string lang = video.Other as string ?? "";
				var keyList = video.PlaybackOptions.Keys.ToList();
				keyList.Sort((s1, s2) => 
				{ 
					if (s1.StartsWith(lang))
					{
						if (!s2.StartsWith(lang)) return -1;
					}
					else if (s2.StartsWith(lang))
					{
						if (!s1.StartsWith(lang)) return 1;
					}
					return videoQuality == VideoQuality.HD ? s1.CompareTo(s2) : s2.CompareTo(s1);
				});
				Dictionary<string, string> newPlaybackOptions = new Dictionary<string, string>();
				keyList.ForEach(k => newPlaybackOptions.Add(k, video.PlaybackOptions[k]));
				video.PlaybackOptions = newPlaybackOptions;
                return video.PlaybackOptions.First().Value;
            }
            return "";
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            currentCategory = category as RssLink;
            currentPage = 1;
            currentCategoryMaxPages = 1;
            return GetPagedVideoList();
        }

        List<VideoInfo> GetPagedVideoList()
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            string data = GetWebData(string.Format((currentCategory as RssLink).Url, currentPage));

            foreach (Match pageMatch in Regex.Matches(data, @"<li><a\s+href=""\#""\s+class=""(current\s)?{page:'\d+'}"">(?<page>\d+)</a></li>"))
            {
                int counter = int.Parse(pageMatch.Groups["page"].Value);
                if (counter > currentCategoryMaxPages) currentCategoryMaxPages = counter;
            }

            Match m = regEx_VideoList.Match(data);
            while (m.Success)
            {
                VideoInfo videoInfo = new VideoInfo();
                videoInfo.Title = m.Groups["Title"].Value;
                videoInfo.VideoUrl = m.Groups["VideoUrl"].Value;
                if (!Uri.IsWellFormedUriString(videoInfo.VideoUrl, System.UriKind.Absolute)) videoInfo.VideoUrl = new Uri(new Uri(baseUrl), videoInfo.VideoUrl).AbsoluteUri;
                videoInfo.ImageUrl = m.Groups["ImageUrl"].Value;                
                if (!string.IsNullOrEmpty(videoInfo.ImageUrl) && !Uri.IsWellFormedUriString(videoInfo.ImageUrl, System.UriKind.Absolute)) videoInfo.ImageUrl = new Uri(new Uri(baseUrl), videoInfo.ImageUrl).AbsoluteUri;
                videoInfo.Length = m.Groups["Duration"].Value;
                videoInfo.Airdate = m.Groups["Airdate"].Value;
                videoInfo.Description = m.Groups["Description"].Value;
				videoInfo.Other = currentCategory.ParentCategory.Name.Substring(0, 2);
                videos.Add(videoInfo);
                m = m.NextMatch();
            }
            return videos;
        }

        #region Next/Previous Page

        RssLink currentCategory = null;
        int currentPage = 0;
        int currentCategoryMaxPages = 0;

        public override bool HasNextPage
        {
            get { return currentCategory != null && currentCategoryMaxPages > currentPage; }
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            currentPage++;
            if (currentPage >= currentCategoryMaxPages) currentPage = currentCategoryMaxPages;
            return GetPagedVideoList();
        }
		/*
        public override bool HasPreviousPage
        {
            get { return currentCategory != null && currentPage > 1; }
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            currentPage--;
            if (currentPage < 1) currentPage = 1;
            return GetPagedVideoList();
        }
		*/
        #endregion        
    }
}
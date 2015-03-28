using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class MdrUtil : SiteUtilBase
    {
		public enum VideoQuality { low, medium, high };

		[Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Video Quality", TranslationFieldName = "VideoQuality"), Description("Defines the preferred quality for the video to be played.")]
		VideoQuality videoQuality = VideoQuality.high;

        string showStartLetterPages = @"<li\s+class=""[^""]*item\w"">\s*<a\s+href=""(?<url>[^""]+)""[^>]*>\s*\w*\s*</a>\s*</li>";
        string showsRegex = @"<div\sclass=""teaserImage"">\s*<a\shref=""(?<url>[^""]+)""[^>]*>\s*<img.+?src=""(?<img>[^""]+)""[^>]*>\s*</a>\s*</div>\s*<h3><a[^>]*>(?<title>[^<]+)</a>";
        string videolistRegEx = @"<div\sclass=""teaserImage"">\s*
<a\shref=""(?<url>[^""]+)""[^>]*>\s*
(<img.+?src=""(?<img>[^""]+)""[^>]*>\s*)?
</a>\s*</div>\s*
<h3>\s*<a[^>]*>(?<title>[^<]+)</a>\s*</h3>\s*
<p\s+class=""subtitle"">(?<subtitle>[^<]*)</p>\s*
<p\s+class=""avAirTime"">\s*(?<airdate>.+?)</p>\s*
<div[^>]*><.*?></div>\s*
<a.*?dataURL:'(?<dataurl>[^']+)'";

        string playlistRegex = @"<REF\sHREF\s=\s""(?<url>[^""]+)""[^>]*>";
        string baseUrl = "http://www.mdr.de/mediathek/fernsehen/a-z";

        Regex regEx_Shows, regEx_Playlist, regEx_Videolist;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_Shows = new Regex(showsRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_Playlist = new Regex(playlistRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_Videolist = new Regex(videolistRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            List<string> startLetterPages = new List<string>();
            string data = GetWebData(baseUrl);
            Match m = Regex.Match(data, showStartLetterPages);
            while (m.Success)
            {
                startLetterPages.Add(m.Groups["url"].Value);
                m = m.NextMatch();
            }
            System.Threading.ManualResetEvent[] threadWaitHandles = new System.Threading.ManualResetEvent[startLetterPages.Count];
            for (int i = 0; i < startLetterPages.Count; i++)
            {
                threadWaitHandles[i] = new System.Threading.ManualResetEvent(false);
                new System.Threading.Thread(delegate(object o)
                    {
                        int o_i = (int)o;
                        string addDataPage = GetWebData(new Uri(new Uri(baseUrl), startLetterPages[o_i]).AbsoluteUri);
                        Match addM = regEx_Shows.Match(addDataPage);
                        if (o_i > 0) System.Threading.WaitHandle.WaitAny(new System.Threading.ManualResetEvent[] { threadWaitHandles[o_i - 1] });
                        while (addM.Success)
                        {
                            RssLink show = new RssLink()
                            {
                                Name = HttpUtility.HtmlDecode(addM.Groups["title"].Value),
                                Thumb = new Uri(new Uri(baseUrl), addM.Groups["img"].Value).AbsoluteUri,
                                Url = new Uri(new Uri(baseUrl), addM.Groups["url"].Value).AbsoluteUri,
                                HasSubCategories = true
                            };
                            Settings.Categories.Add(show);

                            addM = addM.NextMatch();
                        }

                        threadWaitHandles[o_i].Set();
                    }) { IsBackground = true }.Start(i);
            }
            System.Threading.WaitHandle.WaitAll(threadWaitHandles);
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();

            string data = GetWebData((parentCategory as RssLink).Url);
            if (!string.IsNullOrEmpty(data))
            {
                ParseSubCategories(parentCategory, data);

                List<string> addPageUrls = new List<string>();
                Match addPages = Regex.Match(data, @"<a\s+href=""(?<url>[^""]+)""\s+title=""Seite\s+\d+[^""]*"">");
                while (addPages.Success)
                {
                    addPageUrls.Add(new Uri(new Uri(baseUrl), addPages.Groups["url"].Value).AbsoluteUri);
                    addPages = addPages.NextMatch();
                }
                addPageUrls = addPageUrls.Distinct().ToList();
                if (addPageUrls.Count > 0) SubCategoriedForAdditionalPages(parentCategory as RssLink, addPageUrls);
            }

            return parentCategory.SubCategories.Count;
        }

        private void ParseSubCategories(Category parentCategory, string data)
        {
            Match m = regEx_Videolist.Match(data);
            while (m.Success)
            {
                RssLink video = new RssLink()
                {
                    Name = string.Format("{0} ({1})", HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim()), Helpers.StringUtils.PlainTextFromHtml(m.Groups["airdate"].Value).Replace("\n", " ")),
                    Thumb = new Uri(new Uri(baseUrl), m.Groups["img"].Value).AbsoluteUri,
                    Url = new Uri(new Uri(baseUrl), m.Groups["dataurl"].Value).AbsoluteUri,
                    Description = Helpers.StringUtils.PlainTextFromHtml(m.Groups["subtitle"].Value),
                    ParentCategory = parentCategory
                };
                parentCategory.SubCategories.Add(video);

                m = m.NextMatch();
            }
        }

        void SubCategoriedForAdditionalPages(RssLink parentCategory, List<string> urls)
        {
            System.Threading.ManualResetEvent[] threadWaitHandles = new System.Threading.ManualResetEvent[urls.Count];
            for (int i = 0; i < urls.Count; i++)
            {
                threadWaitHandles[i] = new System.Threading.ManualResetEvent(false);
                new System.Threading.Thread(delegate(object o)
                    {
                        int o_i = (int)o;
                        string addDataPage = GetWebData(urls[o_i]);
                        if (o_i > 0) System.Threading.WaitHandle.WaitAny(new System.Threading.ManualResetEvent[] { threadWaitHandles[o_i - 1] });
                        ParseSubCategories(parentCategory, addDataPage);
                        threadWaitHandles[o_i].Set();
                    }) { IsBackground = true }.Start(i);
            }
            System.Threading.WaitHandle.WaitAll(threadWaitHandles);
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();

            string data = GetWebData((category as RssLink).Url, forceUTF8: true);
            if (!string.IsNullOrEmpty(data))
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.LoadXml(data);
                foreach (XmlElement avDoc in xDoc.SelectNodes("//avDocument"))
                {
                    VideoInfo video = new VideoInfo()
                    {
                        Title = avDoc.SelectSingleNode("title").InnerText,
                        Airdate = avDoc.SelectSingleNode("webTime").InnerText,
                        Description = avDoc.SelectSingleNode("teaserText").InnerText,
                        Thumb = avDoc.SelectSingleNode("teaserimages/teaserimage/url") != null ? avDoc.SelectSingleNode("teaserimages/teaserimage/url").InnerText : null,
                        Length = avDoc.SelectSingleNode("duration").InnerText,
                        PlaybackOptions = new Dictionary<string,string>()
                    };

                    foreach (XmlElement asset in avDoc.SelectNodes("assets/asset[not(*[contains(name(),'rtsp')])]"))
                    {
                        string baseInfo = string.Format("{0}x{1} ({2}) | {3}:// | {4}",
						asset.SelectSingleNode("frameWidth") != null ? asset.SelectSingleNode("frameWidth").InnerText : "?",
                        asset.SelectSingleNode("frameHeight") != null ? asset.SelectSingleNode("frameHeight").InnerText : "?",
                        ((int.Parse(asset.SelectSingleNode("bitrateVideo").InnerText) + (asset.SelectSingleNode("bitrateAudio") != null ? int.Parse(asset.SelectSingleNode("bitrateAudio").InnerText) : 0)) / 1000).ToString() + " kbps",
                        new Uri(asset.SelectSingleNode("*[contains(name(),'URL') or contains(name(), 'Url')]").InnerText).Scheme,
                        asset.SelectSingleNode("mediaType").InnerText);

                        string url = asset.SelectSingleNode("*[contains(name(),'URL') or contains(name(), 'Url')]").InnerText;
                        if (url.StartsWith("rtmp")) url += "/" + asset.SelectSingleNode("flashMediaServerURL").InnerText;

                        video.PlaybackOptions.Add(baseInfo, url);
                    }

					if (video.PlaybackOptions.Count > 0)
					{
						video.VideoUrl = video.PlaybackOptions.First().Value;
						// set serialized version of PlaybackOptions to Other so it can be deserialized from a favorite
						if (video.PlaybackOptions.Count > 1)
                            video.Other = "PlaybackOptions://\n" + Helpers.CollectionUtils.DictionaryToString(video.PlaybackOptions);

						videos.Add(video);
					}
                }
            }
            
            return videos;
        }

        public override String GetVideoUrl(VideoInfo video)
        {
            // Get playbackoptins back from favorite video if they were saved in Other object
            if (video.PlaybackOptions == null && video.Other is string && (video.Other as string).StartsWith("PlaybackOptions://"))
                video.PlaybackOptions = Helpers.CollectionUtils.DictionaryFromString((video.Other as string).Substring("PlaybackOptions://".Length));

            // resolve any asx to WMV
            foreach (var v in video.PlaybackOptions)
            {
                if (v.Value.EndsWith(".asx"))
                {
                    var resolved = Helpers.AsxUtils.ParseASX(GetWebData(v.Value));
                    if (resolved != null && resolved.Count > 0) { video.PlaybackOptions[v.Key] = resolved[0]; break; }
                }
            }

			// sort
			var keyList = video.PlaybackOptions.Keys.ToList();
			keyList.Sort((s1, s2) =>
			{
				try
				{
					int b1 = int.Parse(Regex.Match(s1, @"\((?<bitrate>\d+)\skbps\)").Groups["bitrate"].Value);
					int b2 = int.Parse(Regex.Match(s2, @"\((?<bitrate>\d+)\skbps\)").Groups["bitrate"].Value);
					return b1.CompareTo(b2);
				}
				catch
				{
					return 0;
				}
			});
			Dictionary<string, string> newPlaybackOptions = new Dictionary<string, string>();
			keyList.ForEach(k => newPlaybackOptions.Add(k, video.PlaybackOptions[k]));
			video.PlaybackOptions = newPlaybackOptions;
			if (videoQuality == VideoQuality.low)
				return video.PlaybackOptions.First().Value;
			else if (videoQuality == VideoQuality.high)
				return video.PlaybackOptions.Last().Value;
			else
			{
				if (video.PlaybackOptions.Count > 2)
					return video.PlaybackOptions.ElementAt(video.PlaybackOptions.Count-2).Value;
				else
					return video.PlaybackOptions.First().Value;
			}
			
        }

    }
}
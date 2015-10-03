using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;

namespace OnlineVideos.Sites
{
    public class MovieNightUtil : GenericSiteUtil
    {
        [Category("OnlineVideosUserConfiguration"), Description("Select subtitle source, for example: TvSubtitles")]
        [TypeConverter(typeof(OnlineVideos.Subtitles.SubtitleSourceConverter))]
        protected string subtitleSource = "";
        [Category("OnlineVideosUserConfiguration"), Description("Select subtitle language preferences (; separated and ISO 639-2), for example: eng;ger")]
        protected string subtitleLanguages = "";
        [Category("OnlineVideosUserConfiguration"), Description("Select native subtitle language preferences (; separated and ISO 639-1), for example: en;fr;es")]
        protected string nativeSubtitleLanguages = "";

        protected OnlineVideos.Subtitles.SubtitleHandler sh = null;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            sh = new OnlineVideos.Subtitles.SubtitleHandler(subtitleSource, subtitleLanguages);
        }

        protected override List<VideoInfo> Parse(string url, string data)
        {
            if (String.IsNullOrEmpty(data)) data = GetWebData(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(data);
            var nodes = doc.DocumentNode.SelectNodes(@".//a[@href][img[@title]]");
            var result = new List<VideoInfo>();
            foreach (var node in nodes)
            {
                VideoInfo video = new VideoInfo()
                {
                    VideoUrl = node.Attributes["href"].Value,
                    Thumb = htmlValue(node.SelectSingleNode(".//img").Attributes["src"])
                };
                string title = htmlValue(node.SelectSingleNode(".//img").Attributes["title"]);
                title = HttpUtility.HtmlDecode(title);
                var docTitle = new HtmlDocument();
                docTitle.LoadHtml(title);
                var node2 = docTitle.DocumentNode.SelectSingleNode(@"//h4/a");
                if (node2 == null)
                    node2 = docTitle.DocumentNode.SelectSingleNode(@"//div[@class='in_title']");
                if (node2 != null)
                {
                    string s = node2.InnerText;
                    int p = s.IndexOf('(');
                    if (p >= 0)
                    {
                        int q = s.IndexOf(')', p);
                        if (q >= 0)
                        {
                            video.Airdate = s.Substring(p + 1, q - p - 1);
                            s = s.Substring(0, p).Trim();
                        }
                    }
                    video.Title = s;
                }
                node2 = docTitle.DocumentNode.SelectSingleNode(@"//p");
                if (node2 != null)
                    video.Description = node2.InnerText;
                if (video.Title.ToUpperInvariant() != "IMPORTANT!")
                    result.Add(video);
            }

            Match mNext = regEx_NextPage.Match(data);
            if (mNext.Success)
            {
                nextPageUrl = FormatDecodeAbsolutifyUrl(url, mNext.Groups["url"].Value, nextPageRegExUrlFormatString, nextPageRegExUrlDecoding);
                nextPageAvailable = !string.IsNullOrEmpty(nextPageUrl);
            }
            else
            {
                nextPageAvailable = false;
                nextPageUrl = "";
            }

            return result;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string resultUrl = GetFormattedVideoUrl(video);
            string playListUrl = GetPlaylistUrl(resultUrl);
            if (String.IsNullOrEmpty(playListUrl))
                return String.Empty;


            string data = GetWebData(playListUrl);
            Match m = Regex.Match(data, @"defaultQuality: ""(?<default>[^""]+)"",");
            string defaultQuality = null;
            if (m.Success)
                defaultQuality = m.Groups["default"].Value;

            m = Regex.Match(data, @"qualities:\s\[(?<qualities>[^]]+)]");
            var baseOptions = base.GetPlaybackOptions(playListUrl);
            string bareUrl = baseOptions.Values.First();
            int inspos = bareUrl.IndexOf(".mp4");
            video.PlaybackOptions = new Dictionary<string, string>();
            if (m.Success)
            {
                string[] qualities = m.Groups["qualities"].Value.Split(',');
                foreach (string quality in qualities)
                {
                    var q = quality.Trim(new[] { '"', ' ' });
                    if (q == defaultQuality)
                        video.PlaybackOptions.Add(q, bareUrl);
                    else
                        if (!String.IsNullOrEmpty(q))
                            video.PlaybackOptions.Add(q, bareUrl.Insert(inspos, '-' + q));
                }
            }

            if (video.PlaybackOptions.Count == 0)
                video.PlaybackOptions = null;

            m = Regex.Match(data, @"kind:\s""subtitles"",\ssrclang:\s""(?<langcode>[^""]+)"",\slabel:\s""(?<langname>[^""]*)"",\ssrc:\s""(?<url>[^""]+)""\s}");
            Dictionary<string, string> subs = new Dictionary<string, string>();
            while (m.Success)
            {
                subs.Add(m.Groups["langcode"].Value, m.Groups["url"].Value);
                m = m.NextMatch();
            }

            string[] prefSubs = nativeSubtitleLanguages.Split(';');
            foreach (string prefSub in prefSubs)
            {
                if (subs.ContainsKey(prefSub) && !String.IsNullOrEmpty(subs[prefSub]))
                {
                    string subData = GetWebData(subs[prefSub]);
                    if (subData.StartsWith(@"WEBVTT"))
                        subData = subData.Substring(8);
                    video.SubtitleText = subData;
                    break;
                }
            }

            if (String.IsNullOrEmpty(video.SubtitleUrl))
            {
                sh.SetSubtitleText(video, this.GetTrackingInfo);
            }
            if (video.PlaybackOptions == null)
                return bareUrl;
            return video.PlaybackOptions.Values.Last();
        }

        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            uint year = 0;
            UInt32.TryParse(video.Airdate, out year);
            return new TrackingInfo() { VideoKind = VideoKind.Movie, Title = video.Title, Year = year };
        }

        private string htmlValue(HtmlAttribute node)
        {
            if (node == null)
                return String.Empty;
            else
                return node.Value;
        }

    }
}

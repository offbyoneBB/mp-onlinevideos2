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
            var nodes = doc.DocumentNode.SelectNodes(@".//div[@id='box_movies']/div[@class='movie']");
            var result = new List<VideoInfo>();
            foreach (var node in nodes)
            {
                VideoInfo video = new VideoInfo()
                {
                    VideoUrl = node.SelectSingleNode(".//a[@href]").Attributes["href"].Value,
                    Thumb = htmlValue(node.SelectSingleNode(".//img[@src]").Attributes["src"])
                };
                string title = node.SelectSingleNode(".//h2").InnerText;
                title = HttpUtility.HtmlDecode(title);
                if (!String.IsNullOrEmpty(title))
                {
                    Match m = Regex.Match(title, @"\((?<airdate>\d+)\)");
                    if (m.Success)
                    {
                        video.Airdate = m.Groups["airdate"].Value;
                        title = title.Remove(m.Captures[0].Index, m.Captures[0].Length).Trim();
                    }

                    video.Title = title;
                }
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
                        subData = Helpers.SubtitleUtils.Webvtt2SRT(subData);

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

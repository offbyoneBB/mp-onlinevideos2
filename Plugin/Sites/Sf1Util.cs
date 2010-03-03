using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using RssToolkit.Rss;

namespace OnlineVideos.Sites
{
    public class Sf1Util : SiteUtilBase
    {
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the baseUrl for dynamic categories. Group names: 'url', 'title', 'thumb', 'desc'.")]
        string dynamicCategoriesRegEx = @"<img\sclass=""az_thumb""\ssrc=""(?<thumb>[^""]+)""\swidth=""\d*""\sheight=""\d*""\salt=""(?<alt>[^""]+)""\s/></a><a\sclass=""sendung_name""\shref=""(?<url>[^""]+)"">(?<title>[^<]+)</a><p\sclass=""az_description"">(?<desc>[^<]+)</p>";
        [Category("OnlineVideosConfiguration"), Description("Format string applied to the 'url' match retrieved from the dynamicCategoriesRegEx.")]
        string dynamicCategoryUrlFormatString = "http://videoportal.sf.tv/rss/{0}";
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the video 'title' and 'url' from a category rss page.")]
        string videoItemRegex = @"a\shref=""/video\?id=(?<url>[^;]+);DCSext.zugang=videoportal_sendungsuebersicht"">(?<title>[^<]+)</a>";
        [Category("OnlineVideosConfiguration"), Description("Format string applied to the video url of an item that was found in the rss.")]
        string videoUrlFormatString = "http://videoportal.sf.tv/cvis/segment/{0}/.json";
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the actual playback urls with some info ('audio', 'video', 'bitrate', 'url').")]
        string videoUrlRegex = @"""codec_video"":""(?<video>[^""]+)"",""codec_audio"":""(?<audio>[^""]+)"",""bitrate"":(?<bitrate>[^,]+),""frame_width"":\d*,""frame_height"":\d*,""url"":""(?<url>[^""]+)""";
        [Category("OnlineVideosConfiguration"), Description("Url used for to get the categories.")]
        string baseUrl = "http://videoportal.sf.tv/sendungen";

        Regex regEx_dynamicCategories, regEx_VideoItem, regEx_VideoUrl;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_dynamicCategories = new Regex(dynamicCategoriesRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_VideoItem = new Regex(videoItemRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_VideoUrl = new Regex(videoUrlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            string data = GetWebData(baseUrl);
            string xmlUrl = String.Empty;

            if (!string.IsNullOrEmpty(data))
            {
                Match m = regEx_dynamicCategories.Match(data);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Name = m.Groups["title"].Value;
                    cat.Url = string.Format(dynamicCategoryUrlFormatString, m.Groups["url"].Value);
                    cat.Thumb = m.Groups["thumb"].Value.Replace("?width=80", "");
                    cat.Description = m.Groups["desc"].Value;

                    Settings.Categories.Add(cat);
                    m = m.NextMatch();
                }
                Settings.DynamicCategoriesDiscovered = true;
            }
            return Settings.Categories.Count;
        }
        
        public override String getUrl(VideoInfo video)
        {
            if (video.PlaybackOptions == null)
            {
                string data = GetWebData(string.Format(videoUrlFormatString, video.VideoUrl));
                Match m = regEx_VideoUrl.Match(data);
                video.PlaybackOptions = new Dictionary<string, string>();
                while (m.Success)
                {
                    if (!m.Groups["url"].Value.Contains("no streaming"))
                    {
                        if (m.Groups["video"].Value.Contains("wmv3"))
                        {
                            string title = "WMV (" + m.Groups["bitrate"].Value + "K)";
                            string url = m.Groups["url"].Value.Substring(0, m.Groups["url"].Value.IndexOf("?")).Replace("\\/", "/");
                            url = ParseASX(url)[0];
                            video.PlaybackOptions.Add(title, url);
                        }
                        else
                        {
                            string title = "FLV (" + m.Groups["bitrate"].Value + "K)";
                            string url = m.Groups["url"].Value.Replace("\\/", "/");
                            url = string.Format("http://127.0.0.1:{0}/stream.flv?rtmpurl={1}", OnlineVideoSettings.RTMP_PROXY_PORT, System.Web.HttpUtility.UrlEncode(url));
                            video.PlaybackOptions.Add(title, url);
                        }
                    }
                    m = m.NextMatch();
                }                
            }
            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
            {
                var enumer = video.PlaybackOptions.GetEnumerator();
                enumer.MoveNext();
                return enumer.Current.Value;
            }
            return "";
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();

            foreach (RssItem rssItem in GetWebDataAsRss(((RssLink)category).Url).Channel.Items)
            {
                Match m = regEx_VideoItem.Match(rssItem.Description);
                if (m.Success) videoList.Add(new VideoInfo() { Title = rssItem.Title, VideoUrl = m.Groups["url"].Value });
            }
            return videoList;
        }
    }
}
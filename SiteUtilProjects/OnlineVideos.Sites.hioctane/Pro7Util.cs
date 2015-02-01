using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;

namespace OnlineVideos.Sites
{
    public class Pro7Util : GenericSiteUtil
    {
        [Category("OnlineVideosConfiguration"), Description("Url to rtmp Server")]
		protected string rtmpBase;

        [Category("OnlineVideosConfiguration"), Description("")]
        protected string atomFeedUrlRegex;

        protected Regex regEx_atomFeedUrl;

        private Dictionary<string, List<VideoInfo>> data = new Dictionary<string, List<VideoInfo>>();

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            if (!string.IsNullOrEmpty(atomFeedUrlRegex)) regEx_atomFeedUrl = new Regex(atomFeedUrlRegex, defaultRegexOptions);
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();

            var videosUrl = (category as RssLink).Url;
            if (!videosUrl.EndsWith("video/")) videosUrl += "video/";
            var data = GetWebData(videosUrl);
            var m = regEx_atomFeedUrl.Match(data);
            if (m.Success)
            {
                var link = m.Groups["url"].Value;
                if (!Uri.IsWellFormedUriString(link, System.UriKind.Absolute)) link = new Uri(new Uri((category as RssLink).Url), link).AbsoluteUri;

                var feed = GetWebData<XDocument>(link);

                var defaultNs = feed.Root.GetDefaultNamespace();
                var psdNs = feed.Root.GetNamespaceOfPrefix("psd");

                foreach (var entry in feed.Root.Elements(defaultNs + "entry").Where(e => e.Element(psdNs + "type").Value == "video"))
                {
                    VideoInfo video = CreateVideoInfo();
                    video.Title = entry.Element(defaultNs + "title").Value;
                    video.VideoUrl = entry.Element(defaultNs + "link").Attribute("href").Value;
                    video.Airdate = DateTime.ParseExact(entry.Element(defaultNs + "published").Value.Substring(0, 19), "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture).ToString("g", OnlineVideoSettings.Instance.Locale);
                    video.Description = string.Format("{0}\n{1}", entry.Element(defaultNs + "summary").Value, entry.Element(defaultNs + "content").Value);
                    if (entry.Element(psdNs + "imagemedium") != null) video.ImageUrl = entry.Element(psdNs + "imagemedium").Value;

                    videoList.Add(video);
                }
            }

            return videoList;
        }

        public override String GetVideoUrl(VideoInfo video)
        {
            string webData = HttpUtility.UrlDecode(GetWebData(video.VideoUrl));
            string url = string.Empty;

            //TODO: Fix flashdrm Videos
            if (webData.Contains("flashdrm_url"))
            {
                url = Regex.Match(webData, @"flashdrm_url"":""(?<Value>[^""]+)""").Groups["Value"].Value;
                url = HttpUtility.UrlDecode(url);
                while(url.Contains("\\/"))
                    url = url.Replace("\\/", "/");
                url = url.Replace("rtmpte", "rtmpe");
                url = url.Replace(".net", ".net:1935");
				url = new MPUrlSourceFilter.RtmpUrl(url) { SwfUrl = "http://www.prosieben.de/static/videoplayer/swf/HybridPlayer.swf", SwfVerify = true }.ToString();
            }
            else
            {
                string jsonData = Regex.Match(webData, @"SIMVideoPlayer.extract\(""json"",\s*""(?<json>.*?)""\s*\);", RegexOptions.Singleline).Groups["json"].Value;
                var json = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(Regex.Unescape(jsonData));
                
                // try http mp4 file from id
                string clipId = json["categoryList"][0]["clipList"][0].Value<string>("id");
                if (!string.IsNullOrEmpty(clipId))
                {
                    string link = GetRedirectedUrl("http://www.prosieben.de/dynamic/h264/h264map/?ClipID=" + clipId);
                    if (!string.IsNullOrEmpty(link))
                    {
                        if (!link.Contains("not_available"))
                        {
                            url = link;
                        }
                    }
                }
                if (string.IsNullOrEmpty(url))
                {
                    var dl = json.Descendants().Where(j => j.Type == Newtonsoft.Json.Linq.JTokenType.Property && ((Newtonsoft.Json.Linq.JProperty)j).Name == "downloadFilename");

                    foreach (var prop in dl)
                    {
                        string filename = (prop as Newtonsoft.Json.Linq.JProperty).Value.ToString();
                        string geo = (prop.Parent as Newtonsoft.Json.Linq.JObject).Value<string>("geoblocking");
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
                            url = rtmpBase + geoblock + /*"mp4:" +*/ filename;
                            if (!url.EndsWith(".mp4")) url = url + ".mp4";
                        }
                        else
                            url = rtmpBase + geoblock + filename;
                    }
                }

            }

            return url;
        }

        
    }
}
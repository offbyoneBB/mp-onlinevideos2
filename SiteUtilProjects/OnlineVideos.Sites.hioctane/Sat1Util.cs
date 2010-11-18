using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace OnlineVideos.Sites
{
    public class Sat1Util : SiteUtilBase
    {
        [Category("OnlineVideosConfiguration"), Description("Url used for category generation.")]
        string baseUrl;
        [Category("OnlineVideosConfiguration"), Description("Url used to parse the pagingtoken for dynamic category generation")]
        string regExPagingToken;
        [Category("OnlineVideosConfiguration"), Description("Url used to parse Category Content from Page")]
        string regExDynamicCategory;
        [Category("OnlineVideosConfiguration"), Description("Url to rtmp Server")]
        string rtmpBase;


        private Dictionary<string, List<VideoInfo>> data = new Dictionary<string, List<VideoInfo>>();

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

        }
        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            string page = GetWebData(baseUrl);

            Match m = Regex.Match(page, regExPagingToken);
            if (m.Success)
            {
                for (int i = 0; i < Convert.ToInt32(m.Groups["pages"].Value); i++)
                {
                    string queryUrl = "http://www.sat1.de/imperia/teasermanager/ajax_pagination.php?uebersicht_" + m.Groups["tag"].Value + "=" + i + ":1:0:0&parameter=" + m.Groups["token"].Value;
                    string webData = GetWebData(queryUrl);

                    Match n = Regex.Match(webData, regExDynamicCategory);
                    while (n.Success)
                    {
                        if(!data.ContainsKey(n.Groups["Title"].Value))
                        {
                            data[n.Groups["Title"].Value] = new List<VideoInfo>();

                            RssLink cat = new RssLink();
                            cat.Name = n.Groups["Title"].Value;
                            cat.Thumb = "http://www.sat1.de" + n.Groups["ImageUrl"].Value;
                            Settings.Categories.Add(cat);
                        }

                        VideoInfo video = new VideoInfo();
                        video.ImageUrl = "http://www.sat1.de" + n.Groups["ImageUrl"].Value;
                        video.Title = n.Groups["SubTitle"].Value;
                        video.VideoUrl = "http://www.sat1.de" + n.Groups["Url"].Value;
                        video.Description = n.Groups["Date"].Value;

                        data[n.Groups["Title"].Value].Add(video);
                        
                        n = n.NextMatch();
                    }
                }
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override String getUrl(VideoInfo video)
        {
            string webData = GetWebData(video.VideoUrl);
            string url = string.Empty;

            if (webData.Contains("flashdrm_url"))
            {
                url = Regex.Match(webData, @"flashdrm_url"":""(?<Value>[^""]+)""").Groups["Value"].Value;
                url = url.Replace("\\/", "/");
            }
            else
            {
                string filename = Regex.Match(webData, @"downloadFilename"":""(?<Value>[^""]+)""").Groups["Value"].Value;
                filename = filename.Substring(0, filename.Length - 3);
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


                if (webData.Contains("flashSuffix"))
                    url = rtmpBase + geoblock + "mp4:" + filename + "mp4";
                else
                    url = rtmpBase + geoblock + filename + "flv";

            }
            string host = url.Substring(url.IndexOf(":") + 3, url.IndexOf("/", url.IndexOf(":") + 3) - (url.IndexOf(":") + 3));
            string app = url.Substring(host.Length + url.IndexOf(host) + 1, (url.IndexOf("/", url.IndexOf("/", (host.Length + url.IndexOf(host) + 1)) + 1)) - (host.Length + url.IndexOf(host) + 1));
            if (host.Contains(":")) host = host.Substring(0, host.IndexOf(":"));
            string tcUrl = "rtmpe://" + host + ":1935" + "/" + app;
            string playpath = url.Substring(url.IndexOf(app) + app.Length + 1);

            string resultUrl = ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&hostname={1}&tcUrl={2}&app={3}&swfurl={4}&swfsize={5}&swfhash={6}&playpath={7}",
                    System.Web.HttpUtility.UrlEncode(tcUrl), //rtmpUrl
                    host, //host
                    System.Web.HttpUtility.UrlEncode(tcUrl), //tcUrl
                    app, //app
                    "http://www.sat1.de/php-bin/apps/VideoPlayer20/mediacenter/HybridPlayer.swf", //swfurl
                    "850680", //swfsize
                    "89b2c799c23569599472e3ed8b00a292a78de2ef7f181d4de64dccc99e43e1ff", //swfhash
                    System.Web.HttpUtility.UrlEncode(playpath) //playpath
                    ));

            string clipId = Regex.Match(webData, @",""id"":""(?<Value>[^""]+)""").Groups["Value"].Value;
            if (!string.IsNullOrEmpty(clipId))
            {
                string link = GetRedirectedUrl("http://www.prosieben.de/dynamic/h264/h264map/?ClipID=" + clipId);
                if (!string.IsNullOrEmpty(link))
                {
                    video.PlaybackOptions = new Dictionary<string, string>();
                    video.PlaybackOptions.Add("Flv", resultUrl);
                    video.PlaybackOptions.Add("Mp4", link);
                }
            }

            return resultUrl;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            return data[category.Name];
        }
    }
}
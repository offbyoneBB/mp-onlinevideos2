using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Web;

namespace OnlineVideos.Sites
{
    public class Pro7Util : SiteUtilBase
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
            string host = new Uri(baseUrl).Host;

            Match m = Regex.Match(page, regExPagingToken);
            if (m.Success)
            {
                for (int i = 1; i <= Convert.ToInt32(m.Groups["pages"].Value); i++)
                {
                    string queryUrl = "http://" + host + m.Groups["url"].Value + "?page=" + i + "&brand=Prosieben&query=" + HttpUtility.UrlEncode(m.Groups["query"].Value) + "&isMediacenterLayout=true";
                    string webData = GetWebData(queryUrl);

                    Match n = Regex.Match(webData, regExDynamicCategory);
                    while (n.Success)
                    {
                        if (!data.ContainsKey(n.Groups["Title"].Value))
                        {
                            data[n.Groups["Title"].Value] = new List<VideoInfo>();

                            RssLink cat = new RssLink();
                            cat.Name = n.Groups["Title"].Value;
                            cat.Thumb = "http://" + host + n.Groups["ImageUrl"].Value;
                            Settings.Categories.Add(cat);
                        }

                        VideoInfo video = new VideoInfo();
                        video.ImageUrl = "http://" + host + n.Groups["ImageUrl"].Value;
                        video.Title = n.Groups["SubTitle"].Value;
                        video.VideoUrl = "http://" + host + n.Groups["Url"].Value;
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
            string webData = HttpUtility.UrlDecode( GetWebData(video.VideoUrl));
            webData = webData.Replace("\\\"","\"");
            string url = string.Empty;

            //TODO: Fix flashdrm Videos
            if (webData.Contains("flashdrm_url"))
            {
                url = Regex.Match(webData, @"flashdrm_url"":""(?<Value>[^""]+)""").Groups["Value"].Value;
                url = HttpUtility.UrlDecode(url);
                url = url.Replace("\\", "");
                url = url.Replace("rtmpte", "rtmpe");
                return url;
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
            return url;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            return data[category.Name];
        }
    }
}
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using HtmlAgilityPack;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class TVSeriesCCUtil : GenericSiteUtil
    {
        public override int DiscoverDynamicCategories()
        {
            var data = GetWebData(baseUrl);
            var doc = new HtmlDocument();
            doc.LoadHtml(data);

            var nodes = doc.DocumentNode.SelectNodes(@"//li[@class='letter']");

            if (Settings.Categories == null) Settings.Categories = new BindingList<Category>();
            foreach (var node in nodes)
            {
                RssLink cat = new RssLink()
                {
                    Name = node.SelectSingleNode(".//text()").InnerText,
                    HasSubCategories = true,
                    SubCategoriesDiscovered = true,
                    SubCategories = new List<Category>()
                };
                var series = node.SelectNodes(@"./li/a[@href]");

                foreach (var serie in series)
                {
                    RssLink subCat = new RssLink()
                    {
                        Name = serie.InnerText.Trim(),
                        Url = serie.Attributes["href"].Value,
                        HasSubCategories = true,
                        ParentCategory = cat
                    };
                    cat.SubCategories.Add(subCat);
                }
                Settings.Categories.Add(cat);
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            var data = GetWebData(((RssLink)category).Url);
            var doc = new HtmlDocument();
            doc.LoadHtml(data);

            var urls = new Dictionary<string, string>();

            var m = Regex.Match(data, @"arr\[""(?<id>[^""]*)""]\s=\s""(?<url>[^""]*)"";");
            while (m.Success)
            {
                byte[] temp = Convert.FromBase64String(m.Groups["url"].Value);
                urls[m.Groups["id"].Value] = Encoding.ASCII.GetString(temp);
                m = m.NextMatch();
            }

            List<VideoInfo> res = new List<VideoInfo>();
            var nodes = doc.DocumentNode.SelectNodes(@"//div[@itemprop=""episode""]");
            foreach (var node in nodes)
            {
                TrackingInfo ti = null;
                var badgeNode = node.SelectSingleNode(".//div[@class='badge block-badge']");
                if (badgeNode != null)
                {
                    var badge = badgeNode.InnerText;
                    ti = new TrackingInfo();
                    ti.Regex = Regex.Match(badge, @"S(?<Season>\d+)E(?<Episode>\d+)");
                    ti.VideoKind = VideoKind.TvSeries;
                    ti.Title = category.ParentCategory.Name;
                }

                VideoInfo vid = new TvLinkVideo()
                {
                    Title = node.SelectSingleNode(".//div/h4").InnerText,
                    PlaybackOptions = new Dictionary<string, string>(),
                    Other = ti
                };

                var resolutions = node.SelectNodes(@".//div[@class='download-row']");
                foreach (var reso in resolutions)
                {
                    var resoNode = reso.SelectSingleNode(@".//div[@class='download-cell cell1']");
                    var nm = resoNode.InnerText.Trim();
                    var vidIdNode = reso.SelectSingleNode(".//button[@data-url and @data-target='#premium']");
                    var vidId = vidIdNode.Attributes["data-url"].Value;
                    vid.PlaybackOptions.Add(nm, urls[vidId]);
                }

                vid.VideoUrl = vid.GetPreferredUrl(false);

                res.Add(vid);
            }
            return res;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            return video.VideoUrl;
        }

        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            return video.Other as TrackingInfo;
        }
    }

    class TvLinkVideo : VideoInfo
    {
        public override string GetPlaybackOptionUrl(string url)
        {
            var m = Regex.Match(PlaybackOptions[url], @"https://k2s.cc/file/(?<id>[^/]*)/");
            if (!m.Success) return null;

            string id = m.Groups["id"].Value;

            var extraHeader = new NameValueCollection();
            extraHeader["Content-Type"] = "application/json;charset=utf-8";

            var data = WebCache.Instance.GetWebData<JObject>(@"https://api.k2s.cc/v1/auth/token", postData: @"{""grant_type"":""client_credentials"",""client_id"":""k2s_web_app"",""client_secret"":""pjc8pyZv7vhscexepFNzmu4P""}", headers: extraHeader);
            var access_token = data.Value<string>("access_token");

            extraHeader = new NameValueCollection();
            extraHeader["Authorization"] = "Bearer " + access_token;

            var data2 = WebCache.Instance.GetWebData<JObject>(@"https://api.k2s.cc/v1/files/" + id, headers: extraHeader);

            return data2["videoPreview"].Value<string>("video");

        }
    }
}

using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class MdrUtil : SiteUtilBase
    {
        string baseUrl = "http://www.mdr.de/mediathek/fernsehen/a-z";

        public override int DiscoverDynamicCategories()
        {
            var shows = new List<RssLink>();

            List<string> startLetterPages = new List<string>();
            string data = GetWebData(baseUrl);

            var startPage = GetWebData<HtmlDocument>(baseUrl);
            var baseUri = new Uri(baseUrl);
            var letterDiv = startPage.DocumentNode.Descendants("div").FirstOrDefault(d => d.GetAttributeValue("class", "") == "multiGroupNavi multiGroupNaviAlpha");
            foreach (var li in letterDiv.Element("ul").Elements("li"))
            {
                var a = li.Element("a");
                if (a != null) startLetterPages.Add(a.GetAttributeValue("href", ""));
            }

            var threadWaitHandles = new ManualResetEvent[startLetterPages.Count];
            for (int i = 0; i < startLetterPages.Count; i++)
            {
                threadWaitHandles[i] = new ManualResetEvent(false);
                new Thread(delegate(object o)
                    {
                        int o_i = (int)o;

                        var showsPage = GetWebData<HtmlDocument>(new Uri(new Uri(baseUrl), startLetterPages[o_i]).AbsoluteUri);
                        var content = showsPage.DocumentNode.Descendants("div").FirstOrDefault(d => d.GetAttributeValue("id", "") == "content");
                        foreach (var teaser in content.Descendants("div").Where(d => d.GetAttributeValue("class", "") == "innerTeaser"))
                        {
                            var imgSrc = teaser.Descendants("img").FirstOrDefault(img => img.GetAttributeValue("class", "") == "img").GetAttributeValue("src", "");
                            var a = teaser.Descendants("a").FirstOrDefault().GetAttributeValue("href", "");

                            var infoDiv = teaser.Descendants("div").FirstOrDefault(d => d.GetAttributeValue("class", "") == "shortInfos");
                            var title = infoDiv.Element("h4").InnerText;
                            var sub = infoDiv.Element("p");

                            RssLink show = new RssLink()
                            {
                                Name = HttpUtility.HtmlDecode(title.Trim()),
                                Thumb = new Uri(new Uri(baseUrl), imgSrc).AbsoluteUri,
                                Url = new Uri(new Uri(baseUrl), a).AbsoluteUri,
                                Description = (sub != null) ? HttpUtility.HtmlDecode(sub.InnerText) : ""
                            };
                            lock(show)
                                shows.Add(show);
                        }

                        threadWaitHandles[o_i].Set();
                    }) { IsBackground = true }.Start(i);
            }
            WaitHandle.WaitAll(threadWaitHandles);

            Settings.Categories.Clear();
            shows.Sort((c1, c2) => c1.Name.CompareTo(c2.Name));
            shows.ForEach(show => Settings.Categories.Add(show));
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();

            var page = GetWebData<HtmlDocument>((category as RssLink).Url);
            var content = page.DocumentNode.Descendants("div").FirstOrDefault(d => d.GetAttributeValue("id", "") == "content");
            var l = content.Descendants("div").FirstOrDefault(img => img.GetAttributeValue("class", "") == "lineWrapper");
            foreach (var teaser in l.Descendants("div").Where(d => d.GetAttributeValue("class", "") == "innerTeaser"))
            {
                var imgNode = teaser.Descendants("img").FirstOrDefault(img => img.GetAttributeValue("class", "") == "img");
                var a = teaser.Descendants("a").FirstOrDefault().GetAttributeValue("href", "");
                var infoDiv = teaser.Descendants("div").FirstOrDefault(d => d.GetAttributeValue("class", "") == "shortInfos");
                var dateSpan = teaser.Descendants("span").FirstOrDefault(d => d.GetAttributeValue("class", "") == "date");
                var startTimeSpan = teaser.Descendants("span").FirstOrDefault(d => d.GetAttributeValue("class", "") == "startTime");
                var endTimeSpan = teaser.Descendants("span").FirstOrDefault(d => d.GetAttributeValue("class", "") == "endTime");

                var title = HttpUtility.HtmlDecode(infoDiv.Element("h4").InnerText.Trim().Replace("\n", " "));
                var airDate = dateSpan.InnerText.Trim().Replace("\n", " ") + " " + startTimeSpan.InnerText.Trim();
                var thumb = (imgNode != null) ? new Uri(new Uri(baseUrl), imgNode.GetAttributeValue("src", "")).AbsoluteUri : null;

                var startTime =TimeSpan.Parse(startTimeSpan.InnerText);
                var endTime =TimeSpan.Parse(endTimeSpan.InnerText);
                if (endTime < startTime) endTime = endTime.Add(TimeSpan.FromHours(24));
                var mins = (endTime - startTime).TotalMinutes;
                
                if (title.StartsWith(category.Name) && title.Length > category.Name.Length + 5) title = title.Substring(category.Name.Length).Trim();
                if (title == category.Name) title += " - " + airDate;

                videos.Add(new VideoInfo()
                {
                    Title = title,
                    Airdate = airDate,
                    Thumb = thumb,
                    Length = string.Format("{0:F0} min", mins),
                    VideoUrl = new Uri(new Uri(baseUrl), a).AbsoluteUri
                });
            }

            return videos;
        }

        public override String GetVideoUrl(VideoInfo video)
        {
            var page = GetWebData<HtmlDocument>(video.VideoUrl);
            var mediaDiv = page.DocumentNode.Descendants("div").FirstOrDefault(d => d.GetAttributeValue("class", "").Contains("mediaCon") && d.GetAttributeValue("data-ctrl-player", "") != "");
            var dataJson = JObject.Parse(mediaDiv.GetAttributeValue("data-ctrl-player", ""));
            var xmlUri = new Uri(new Uri(baseUrl), dataJson.Value<string>("playerXml")).AbsoluteUri;
            var xmlDoc = GetWebData<XmlDocument>(xmlUri);

            video.PlaybackOptions = new Dictionary<string, string>();

            foreach (XmlElement asset in xmlDoc.SelectNodes("//asset"))
            {
                var urlNode = asset.SelectSingleNode("progressiveDownloadUrl");
                var profileNameNode = asset.SelectSingleNode("profileName");

                if (urlNode != null && urlNode.InnerText != null && urlNode.InnerText.Trim().StartsWith("http") &&
                    profileNameNode.InnerText != null && !profileNameNode.InnerText.ToLower().Contains("mobil") && !profileNameNode.InnerText.ToLower().Contains("download"))
                {
                    video.PlaybackOptions[profileNameNode.InnerText.Trim()] = urlNode.InnerText.Trim();
                }
            }

            return video.PlaybackOptions.FirstOrDefault().Value;
        }

    }
}
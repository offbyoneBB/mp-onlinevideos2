using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class ArtePlus7Util : SiteUtilBase
    {
        public enum VideoQuality { HD, MD, SD, LD };
        public enum Language { DE, FR };

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Video Quality", TranslationFieldName = "VideoQuality"), Description("Defines the preferred quality for the video to be played.")]
        VideoQuality videoQuality = VideoQuality.HD;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Language", TranslationFieldName = "Language"), Description("Arte offers their programm in German and French.")]
        Language language = Language.DE;

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            string url = language == Language.DE ? "http://www.arte.tv/guide/de/plus7" : "http://www.arte.tv/guide/fr/plus7";
            var doc = GetWebData<HtmlDocument>(url);
            var list = doc.DocumentNode.Descendants("ul").Where(ul => ul.GetAttributeValue("class", "").Contains("program-list")).FirstOrDefault();
            foreach (var li in list.Elements("li"))
            {
                var a = li.Element("a");
                RssLink cat = new RssLink();
                cat.Url = a.GetAttributeValue("href", "");
                cat.Name = a.Element("span").InnerText.Trim();
                cat.Description = a.Elements("span").Skip(1).First().InnerText.Trim();
                Settings.Categories.Add(cat);
            }
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            var result = new List<VideoInfo>();
            var doc = GetWebData<HtmlDocument>((category as RssLink).Url);
            var div = doc.DocumentNode.Descendants("div").Where(d => d.GetAttributeValue("id", "") == "content-videos").FirstOrDefault();
            if (div != null)
            {
                foreach (var li in div.Element("ul").Elements("li"))
                {
                    var itemDiv = li.Element("div");
                    if (itemDiv != null)
                    {
                        result.Add(getVideoInfo(itemDiv));
                    }
                }
            }
            return result;
        }

        public override String GetVideoUrl(VideoInfo video)
        {
            video.PlaybackOptions = new Dictionary<string, string>();
            var json = GetWebData<JObject>(video.VideoUrl);
            foreach (var quality in json["videoJsonPlayer"]["VSR"])
            {
                if (quality.First.Value<string>("mediaType") == "rtmp")
                {
                    string qualityName = quality.First.Value<string>("quality");
                    if (!video.PlaybackOptions.ContainsKey(qualityName))
                    {
                        string host = quality.First.Value<string>("streamer");
                        string file = quality.First.Value<string>("url");
                        string playbackUrl = new MPUrlSourceFilter.RtmpUrl(host) { TcUrl = host, PlayPath = "mp4:" + file }.ToString();
                        video.PlaybackOptions.Add(qualityName, playbackUrl);
                    }
                }
            }
            return video.PlaybackOptions.FirstOrDefault(q => q.Key.StartsWith(videoQuality.ToString())).Value;
        }

        private VideoInfo getVideoInfo(HtmlNode itemDiv)
        {
            VideoInfo video = new VideoInfo();

            video.VideoUrl = itemDiv.Elements("div").Last().GetAttributeValue("arte_vp_url", "");
            video.Length = itemDiv.Descendants("div").Where(d => d.GetAttributeValue("class", "").Contains("badge-holder")).FirstOrDefault().Element("div").NextSibling.InnerText.Trim().Trim('"').Trim();
            video.Airdate = itemDiv.Descendants("p").FirstOrDefault().ChildNodes.LastOrDefault().InnerText.Trim();
            video.Title = itemDiv.Descendants("h3").FirstOrDefault().InnerText.Trim();
            video.Thumb = itemDiv.Element("img").GetAttributeValue("src", "");
            video.Description = itemDiv.GetAttributeValue("data-description", "");

            return video;
        }

        #region Search

        public override bool CanSearch { get { return true; } }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            var result = new List<SearchResultItem>();
            string url = language == Language.DE ? "http://www.arte.tv/guide/de/suchergebnisse?keyword={0}" : "http://www.arte.tv/guide/fr/resultats-de-recherche?keyword={0}";
            var doc = GetWebData<HtmlDocument>(String.Format(url, HttpUtility.UrlEncode("kreta")));
            var list = doc.DocumentNode.Descendants("div").Where(div => !String.IsNullOrEmpty(div.GetAttributeValue("arte_vp_url", "")));
            foreach (var div in list)
            {
                result.Add(getVideoInfo(div.ParentNode));
            }
            return result;
        }

        #endregion
    }
}
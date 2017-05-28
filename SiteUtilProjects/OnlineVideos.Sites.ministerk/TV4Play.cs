using Newtonsoft.Json.Linq;
using OnlineVideos.Sites.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class TV4Play : SiteUtilBase
    {
        #region constants, vars and properties

        protected const string showsUrl = "http://www.tv4play.se/api/programs?per_page=1000&is_cmore=false&order_by=&page=0&tags=";
        protected const string episodesAndClipsUrl = "http://webapi.tv4play.se/play/video_assets?per_page=100&is_live=false&type={0}&page=1&node_nids={1}&start=0";
        protected const string videoPlayUrl = "https://prima.tv4play.se/api/web/asset/{0}/play";
        protected const string episodeSearchUrl = "http://webapi.tv4play.se/play/video_assets?per_page=100&is_live=false&page=1&sort_order=desc&type=episode&q={0}&start=0";

        #endregion

        #region Categories

        public override int DiscoverDynamicCategories()
        {
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("Accept-Language", "sv-SE,sv;q=0.8,en-US;q=0.6,en;q=0.4");
            JArray items = JArray.Parse(GetWebData<string>(showsUrl, headers: nvc));
            foreach (JToken item in items)
            {
                RssLink show = new RssLink();

                show.Name = (item["name"] == null) ? "" : item["name"].Value<string>();
                show.Url = (item["nid"] == null) ? "" : item["nid"].Value<string>();
                show.Description = (item["description"] == null) ? "" : item["description"].Value<string>();
                show.Thumb = (item["program_image"] == null) ? "" : item["program_image"].Value<string>();
                show.SubCategories = new List<Category>();
                show.HasSubCategories = true;
                show.Other = (Func<List<Category>>)(() => GetShow(show));
                Settings.Categories.Add(show);
            }
            Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > 0;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            Func<List<Category>> method = parentCategory.Other as Func<List<Category>>;
            if (method != null)
            {
                parentCategory.SubCategories = method.Invoke();
                parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
                return parentCategory.SubCategories.Count;
            }
            return 0;
        }


        private List<Category> GetShow(Category parentCategory)
        {
            List<Category> categories = new List<Category>();
            RssLink helaProgram = new RssLink() { Name = "Hela program", HasSubCategories = false, ParentCategory = parentCategory, Url = string.Format(episodesAndClipsUrl, "episode", (parentCategory as RssLink).Url)};
            if (GetWebData<JObject>(helaProgram.Url)["total_hits"].Value<int>() > 0)
                categories.Add(helaProgram);
            RssLink klipp = new RssLink() { Name = "Klipp", HasSubCategories = false, ParentCategory = parentCategory, Url = string.Format(episodesAndClipsUrl, "clip", (parentCategory as RssLink).Url) };
            if (GetWebData<JObject>(klipp.Url)["total_hits"].Value<int>() > 0)
                categories.Add(klipp);
            return categories;
        }

        #endregion

        #region Videos

        private List<VideoInfo> GetVideos(string url)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            JObject json = GetWebData<JObject>(url);
            JArray results = json["results"].Value<JArray>();
            if (results != null)
            {
                foreach (JToken token in results)
                {
                    VideoInfo video = new VideoInfo();
                    video.VideoUrl = token["id"].ToString();
                    video.Title = token["title"].Value<string>();
                    video.Description = token["description"].Value<string>();
                    video.Thumb = token["image"].Value<string>();
                    videos.Add(video);
                }
            }
            return videos;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            return GetVideos((category as RssLink).Url);
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string url = string.Format(videoPlayUrl, video.VideoUrl);
            XmlDocument xDoc = GetWebData<XmlDocument>(url);
            XmlNode errorElement = xDoc.SelectSingleNode("//error");
            if (errorElement != null)
            {
                throw new OnlineVideosException(errorElement.SelectSingleNode("./description/text()").InnerText);
            }
            XmlNode drm = xDoc.SelectSingleNode("//drmProtected");
            if (drm != null && drm.InnerText.Trim().ToLower() == "true")
            {
                    throw new OnlineVideosException("DRM protected content, sorry! :/");
            }
            foreach (XmlElement item in xDoc.SelectNodes("//items/item"))
            {
                string mediaformat = item.GetElementsByTagName("mediaFormat")[0].InnerText.ToLower();
                string itemUrl = item.GetElementsByTagName("url")[0].InnerText.Trim();
                if (mediaformat.StartsWith("mp4") && itemUrl.ToLower().EndsWith(".f4m"))
                {
                    url = string.Concat(itemUrl, "?hdcore=3.5.0&g=", HelperUtils.GetRandomChars(12));
                }
                else if (mediaformat.StartsWith("mp4") && itemUrl.ToLower().Contains(".f4m?"))
                {
                    url = string.Concat(itemUrl, "&hdcore=3.5.0&g=", HelperUtils.GetRandomChars(12));
                }
                else if (mediaformat.StartsWith("webvtt"))
                {
                    try
                    {
                        string srt = GetWebData(itemUrl, encoding: System.Text.Encoding.Default);
                        Regex rgx;
                        //Remove WEBVTT stuff
                        rgx = new Regex(@"WEBVTT");
                        srt = rgx.Replace(srt, new MatchEvaluator((Match m) =>
                        {
                            return string.Empty;
                        }));
                        //Add hours
                        rgx = new Regex(@"(\d\d:\d\d\.\d\d\d)\s*-->\s*(\d\d:\d\d\.\d\d\d).*?\n", RegexOptions.Multiline);
                        srt = rgx.Replace(srt, new MatchEvaluator((Match m) =>
                        {
                            return "00:" + m.Groups[1].Value + " --> 00:" + m.Groups[2].Value + "\n";
                        }));
                        // Remove all trailing stuff, ie in 00:45:21.960 --> 00:45:25.400 A:end L:82%
                        rgx = new Regex(@"(\d\d:\d\d:\d\d\.\d\d\d)\s*-->\s*(\d\d:\d\d:\d\d\.\d\d\d).*\n", RegexOptions.Multiline);
                        srt = rgx.Replace(srt, new MatchEvaluator((Match m) =>
                        {
                            return m.Groups[1].Value + " --> " + m.Groups[2].Value + "\n";
                        }));

                        //Remove all tags
                        rgx = new Regex(@"</{0,1}[^>]+>");
                        srt = rgx.Replace(srt, string.Empty);
                        //Add index
                        rgx = new Regex(@"(?<time>\d\d:\d\d:\d\d\.\d\d\d\s*?-->\s*?\d\d:\d\d:\d\d\.\d\d\d)");
                        int i = 0;
                        foreach (Match m in rgx.Matches(srt))
                        {
                            i++;
                            string time = m.Groups["time"].Value;
                            srt = srt.Replace(time, i + "\n" + time);
                        }
                        srt = HttpUtility.HtmlDecode(srt).Trim();
                        video.SubtitleText = srt;
                    }
                    catch { }
                }
            }
            return url;
        }

        #endregion

        #region Search

        public override bool CanSearch
        {
            get
            {
                return true;
            }
        }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            List<SearchResultItem> results = new List<SearchResultItem>();
            GetVideos(string.Format(episodeSearchUrl, HttpUtility.UrlEncode(query))).ForEach(v => results.Add(v));
            return results;
        }

        #endregion

    }
}
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json.Linq;
using HtmlAgilityPack;

namespace OnlineVideos.Sites
{
    public class UitzendingGemistUtil : GenericSiteUtil
    {

        public enum VideoQuality { H264_sb, H264_bb, H264_std };

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Preferred Format"), Description("Prefer this format when there are more than one for the desired quality.")]
        VideoQuality preferredQuality = VideoQuality.H264_std;

        private WebProxy webProxy = null;
        #region singleton
        public WebProxy GetProxy()
        {
            if (webProxy == null && !String.IsNullOrEmpty(httpSettings.ProxyServer))
                webProxy = new WebProxy(httpSettings.ProxyServer, httpSettings.ProxyServerPort);
            return webProxy;
        }
        #endregion

        public override int DiscoverDynamicCategories()
        {
            HtmlDocument data = GetWebData<HtmlDocument>(baseUrl);
            var nodes = data.DocumentNode.SelectNodes(@"//div[@class='npo-dropdown-container']");

            foreach (var node in nodes)
            {

                var cat = new Category()
                {
                    Name = node.SelectSingleNode(@"div[@class='dropdown-text']").InnerText,
                    HasSubCategories = true,
                    SubCategoriesDiscovered = true,
                    SubCategories = new List<Category>()
                };
                Settings.Categories.Add(cat);
                var subnodes = node.SelectNodes(".//li/a");
                foreach (var subnode in subnodes)
                {
                    var subcat = new RssLink() { Name = subnode.InnerText, ParentCategory = cat, HasSubCategories = true };
                    var arg = subnode.Attributes["data-argument"].Value;
                    var val = subnode.Attributes["data-value"].Value;
                    if (!String.IsNullOrEmpty(val))
                    {
                        subcat.Url = "https://www.npo.nl/media/series?" + arg + '=' + val + "&tilemapping=normal&tiletype=teaser&pageType=catalogue&page=1";
                        cat.SubCategories.Add(subcat);
                    }
                }
            }
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        private void addSubcats(Category parentCategory, string url)
        {
            NameValueCollection headers = new NameValueCollection();
            headers.Add("Accept", "*/*"); // accept any content type
            headers.Add("User-Agent", OnlineVideoSettings.Instance.UserAgent);
            headers.Add("X-Requested-With", "XMLHttpRequest");

            JObject data = GetWebData<JObject>(url, headers: headers);
            foreach (var jnode in data["tiles"])
            {
                var html = jnode.Value<String>();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                var subcat = new RssLink()
                {
                    Name = HttpUtility.HtmlDecode(doc.DocumentNode.SelectSingleNode("//h2").InnerText),
                    ParentCategory = parentCategory,
                    HasSubCategories = true,
                    Url = doc.DocumentNode.SelectSingleNode("//a").Attributes["href"].Value,
                    Other = true
                };
                var img = doc.DocumentNode.SelectSingleNode("//div[@style]");
                if (img != null)
                    subcat.Thumb = Helpers.StringUtils.GetSubString(img.Attributes["style"].Value, "url('", "')");
                parentCategory.SubCategories.Add(subcat);
            }
            if (!String.IsNullOrEmpty(data["nextLink"].Value<String>()))
                parentCategory.SubCategories.Add(new NextPageCategory()
                {
                    ParentCategory = parentCategory,
                    Url = FormatDecodeAbsolutifyUrl(baseUrl, data["nextLink"].Value<String>() + "&tilemapping=normal&tiletype=teaser&pageType=catalogue", "", UrlDecoding.None)
                }
                );

            parentCategory.SubCategoriesDiscovered = true;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            if (parentCategory.Other == null)
            {
                addSubcats(parentCategory, ((RssLink)parentCategory).Url);
                return parentCategory.SubCategories.Count;
            }
            else
            {
                var data = GetWebData<HtmlDocument>(((RssLink)parentCategory).Url);

                var episodesNode = data.DocumentNode.SelectSingleNode(@"//div[@id='component-grid-episodes']");
                if (episodesNode != null)
                {
                    var afleveringen = new RssLink() { Name = "Afleveringen", ParentCategory = parentCategory };
                    parentCategory.SubCategories.Add(afleveringen);
                    afleveringen.Url = "https://www.npo.nl" + episodesNode.SelectSingleNode(".//input[@name='selfLink']").Attributes["value"].Value + "?tilemapping=dedicated&pageType=catalogue&tiletype=asset";
                }

                var clipsNode = data.DocumentNode.SelectSingleNode(@"//div[@id='component-grid-clips']");
                if (clipsNode != null)
                {
                    //add extras
                    var sub = new Category() { Name = "Extra's", ParentCategory = parentCategory };
                    parentCategory.SubCategories.Add(sub);
                    sub.Other = base.Parse(baseUrl, clipsNode.InnerHtml);
                }

                parentCategory.SubCategoriesDiscovered = true;
                return parentCategory.SubCategories.Count;
            }
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {

            category.ParentCategory.SubCategories.Remove(category);
            addSubcats(category.ParentCategory, ((RssLink)category).Url);
            int oldAmount = category.ParentCategory.SubCategories.Count;
            return category.ParentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            if (category.Other is List<VideoInfo>)
                return (List<VideoInfo>)category.Other;

            return Parse(((RssLink)category).Url, null);
        }

        protected override List<VideoInfo> Parse(string url, string data)
        {

            NameValueCollection headers = new NameValueCollection();
            headers.Add("Accept", "*/*"); // accept any content type
            headers.Add("User-Agent", OnlineVideoSettings.Instance.UserAgent);
            headers.Add("X-Requested-With", "XMLHttpRequest");


            JObject jdata = GetWebData<JObject>(url, headers: headers);

            nextPageAvailable = false;
            if (!String.IsNullOrEmpty(jdata["nextLink"].Value<String>()))
            {
                nextPageAvailable = true;
                nextPageUrl = FormatDecodeAbsolutifyUrl(baseUrl, jdata["nextLink"].Value<String>() + "&tilemapping=dedicated&tiletype=asset&pageType=catalogue", "", UrlDecoding.None);

            }
            return base.Parse(url, jdata["tiles"].ToString().Replace(@"\""",@"""").Replace(@"\n","\n"));
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string result = String.Empty;

            int p = video.VideoUrl.LastIndexOf('/');
            if (p >= 0)
            {
                string id = video.VideoUrl.Substring(p + 1);
                string newToken = Doskabouter.Helpers.NPOHelper.GetToken(video.VideoUrl, GetProxy());
                if (!String.IsNullOrEmpty(newToken))
                {
                    string webData = GetWebData(String.Format(fileUrlFormatString, id) + newToken, proxy: GetProxy());
                    JObject contentData = (JObject)JObject.Parse(webData);
                    JArray items = contentData["items"][0] as JArray;
                    List<KeyValuePair<string, string>> playbackOptions = new List<KeyValuePair<string, string>>();
                    foreach (JToken item in items)
                    {
                        string s = item.Value<string>("url");

                        Match m = Regex.Match(s, @"/ida/(?<quality>[^/]*)/");
                        if (!m.Success)
                            m = Regex.Match(s, @"(?<quality>\d+x\d+)_");
                        if (m.Success)
                        {
                            string quality = m.Groups["quality"].Value;
                            try
                            {
                                VideoQuality vq = (VideoQuality)Enum.Parse(typeof(VideoQuality), quality, true);
                                if (Enum.IsDefined(typeof(VideoQuality), vq) && vq.Equals(preferredQuality))
                                    result = s;
                            }
                            catch (ArgumentException)
                            {
                            };

                            playbackOptions.Add(new KeyValuePair<string, string>(quality, s));
                        }
                    }
                    playbackOptions.Sort(Compare);
                    video.PlaybackOptions = new Dictionary<string, string>();
                    foreach (KeyValuePair<string, string> kv in playbackOptions)
                        video.PlaybackOptions.Add(kv.Key, kv.Value);
                }

                if (String.IsNullOrEmpty(result))
                    result = video.PlaybackOptions.Last().Value;
            }
            return result;
        }

        private static readonly string[] sortedQualities = new string[] { "h264_sb", "h264_bb", "h264_std" };

        private int Compare(KeyValuePair<string, string> a, KeyValuePair<string, string> b)
        {
            int res = Array.IndexOf(sortedQualities, a.Key).CompareTo(Array.IndexOf(sortedQualities, b.Key));
            if (res != 0)
                return res;
            return a.Value.CompareTo(b.Value);
        }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            return Parse(string.Format(searchUrl, query), null).ConvertAll<SearchResultItem>(v => v as SearchResultItem);
        }

        public override VideoInfo CreateVideoInfo()
        {
            return new UZGVideoInfo() { proxy = GetProxy() };
        }

    }

    public class UZGVideoInfo : VideoInfo
    {
        public WebProxy proxy;

        public override string GetPlaybackOptionUrl(string option)
        {
            string s = base.GetPlaybackOptionUrl(option);
            string webData = WebCache.Instance.GetWebData(s, proxy: proxy);
            Match m = Regex.Match(webData, @"\((?<res>.*)\)");
            if (m.Success)
                webData = m.Groups["res"].Value;
            JObject contentData = (JObject)JObject.Parse(webData);
            var url = contentData.Value<string>("url");
            if (!String.IsNullOrEmpty(url))
                return url;
            var error = contentData.Value<string>("errorstring");
            if (!String.IsNullOrEmpty(error))
                throw new OnlineVideosException(error);
            return String.Empty;
        }
    }

}

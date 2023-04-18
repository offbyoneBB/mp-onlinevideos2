using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json.Linq;
using OnlineVideos.Helpers;

namespace OnlineVideos.Sites
{
    public class NPOStartUtil : GenericSiteUtil, IWebViewHTMLMediaElement
    {
        [Category("OnlineVideosConfiguration")]
        protected string seriesRegEx;
        Regex regex_Series;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            siteSettings.Player = PlayerType.Webview;
            if (!string.IsNullOrEmpty(seriesRegEx)) regex_Series = new Regex(seriesRegEx, defaultRegexOptions);
        }

        public override int DiscoverDynamicCategories()
        {
            foreach (var cat in Settings.Categories)
                cat.HasSubCategories = cat.Name != "Live";
            return Settings.Categories.Count;
        }


        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (parentCategory.Other == null)
            {
                int res = base.DiscoverSubCategories(parentCategory);
                foreach (var sub in parentCategory.SubCategories)
                {
                    sub.HasSubCategories = true;
                    sub.Other = true;
                }
                return res;
            }

            return getSubcategories(((RssLink)parentCategory).Url, parentCategory);
        }

        private int getSubcategories(string url, Category parentCategory)
        {

            var headers = new NameValueCollection();
            headers.Add("X-Requested-With", "XMLHttpRequest");
            var data = GetWebData<JObject>(url, headers: headers);

            if (parentCategory.SubCategories == null) parentCategory.SubCategories = new List<Category>();
            foreach (var item in data["tiles"])
            {
                Match m = regex_Series.Match(item.ToString());
                if (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Url = FormatDecodeAbsolutifyUrl(baseUrl, m.Groups["url"].Value, null, UrlDecoding.None);
                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim());
                    cat.Thumb = m.Groups["thumb"].Value;
                    if (!String.IsNullOrEmpty(cat.Thumb) && !Uri.IsWellFormedUriString(cat.Thumb, System.UriKind.Absolute)) cat.Thumb = new Uri(new Uri(baseUrl), cat.Thumb).AbsoluteUri;
                    cat.ParentCategory = parentCategory;
                    ExtraSubCategoryMatch(cat, m.Groups);
                    parentCategory.SubCategories.Add(cat);
                }
                parentCategory.SubCategoriesDiscovered = true;
            }

            string nextCatPageUrl = data.Value<string>("nextLink");
            if (!String.IsNullOrEmpty(nextCatPageUrl))
            {
                if (!Uri.IsWellFormedUriString(nextCatPageUrl, System.UriKind.Absolute)) nextCatPageUrl = new Uri(new Uri(baseUrl), nextCatPageUrl).AbsoluteUri;
                if (!nextCatPageUrl.EndsWith(@"&tileMapping=normal&tileType=teaser&pageType=catalogue"))
                    nextCatPageUrl += "&tileMapping=normal&tileType=teaser&pageType=catalogue";
                parentCategory.SubCategories.Add(new NextPageCategory() { Url = nextCatPageUrl, ParentCategory = parentCategory });
            }
            return parentCategory.SubCategories.Count;
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            category.ParentCategory.SubCategories.Remove(category);
            return getSubcategories(category.Url, category.ParentCategory);
        }


        public override string GetVideoUrl(VideoInfo video)
        {
            vidSelector = null;
            Match m;

            if (video.VideoUrl.Contains("/live/"))
            {
                var data = GetWebData(video.VideoUrl);
                m = Regex.Match(data, @"player-id=""(?<id>[^""]*)""", defaultRegexOptions);
            }
            else
                m = Regex.Match(video.VideoUrl, @"/(?<id>[^/]*)$");

            if (m.Success)
            {
                string episode_id = m.Groups["id"].Value;
                var headers = new NameValueCollection();
                headers.Add("X-Requested-With", "XMLHttpRequest");
                wvh.GetHtml("https://www.npostart.nl/api/token", headers: headers);//needed to get cookies
                var cookies = wvh.GetCookies("https://www.npostart.nl");
                string xsrf = cookies.TryGetValue("XSRF-TOKEN", out Cookie tmp) ? tmp.Value : null;
                headers.Add("x-xsrf-token", HttpUtility.UrlDecode(xsrf));
                string data = wvh.GetHtml("https://www.npostart.nl/player/" + episode_id, postData: "", headers: headers);
                m = Regex.Match(data, @"""embedUrl"":""(?<url>[^""]*)""");
                if (m.Success)
                {
                    return m.Groups["url"].Value;
                }
                else
                    return String.Empty;
            }
            return String.Empty;
        }

        #region IWebViewHTMLMediaElement
        System.Windows.Forms.Timer timer = null;
        string vidSelector = null;
        WebViewHelper wvh = null;

        void INeedsWebView.SetWebviewHelper(WebViewHelper webViewHelper)
        {
            wvh = webViewHelper;
        }

        void IWebViewSiteUtilBase.StartPlayback()
        {
            wvh.Execute(@"document.getElementsByClassName(""vjs-big-play-button"")[0].click()");
            timer = new System.Windows.Forms.Timer();
            timer.Tick += (s, e) =>
            {
                vidSelector = @"document.getElementsByTagName(""video"")[0]";
                timer.Stop();
                timer = null;
            };
            timer.Interval = 2000;
            timer.Start();
        }

        public string VideoElementSelector
        {
            get { return vidSelector; }
        }
        #endregion

    }
}

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Net;
using System.Web;
using OnlineVideos.MPUrlSourceFilter;

namespace OnlineVideos.Sites
{
    public class NicoNicoUtil : GenericSiteUtil
    {
        [Category("OnlineVideosUserConfiguration"), Description("Email address of your NicoNico account")]
        string emailAddress = null;
        [Category("OnlineVideosUserConfiguration"), Description("Password of your NicoNico account")]
        string password = null;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for dynamic categories. Group names: 'url', 'title', 'thumb', 'description'. Will be used on the web pages resulting from the links from the dynamicCategoriesRegEx. Will not be used if not set.")]
        protected string dynamicSubSubCategoriesRegEx;

        private Regex regEx_dynamicSubSubCategories = null;
        private Regex regEx_dynamicSubLevel1Categories = null;
        private CookieContainer cc = new CookieContainer();
        NameValueCollection headers = new NameValueCollection();

        public override int DiscoverDynamicCategories()
        {
            if (String.IsNullOrEmpty(emailAddress) || String.IsNullOrEmpty(password))
            {
                Log.Error("NicoNico: You must provide email address and password for playback");
            }
            headers.Add("Accept", "*/*"); // accept any content type
            headers.Add("User-Agent", OnlineVideoSettings.Instance.UserAgent); // set the default OnlineVideos UserAgent when none specified
            headers.Add("Accept-Language", "en-us");

            if (!string.IsNullOrEmpty(dynamicSubSubCategoriesRegEx))
                regEx_dynamicSubSubCategories = new Regex(dynamicSubSubCategoriesRegEx, defaultRegexOptions);
            regEx_dynamicSubLevel1Categories = regEx_dynamicSubCategories;

            CookieContainer tmpCc = new CookieContainer();
            string result = GetWebData(@"https://secure.nicovideo.jp/secure/login?site=niconico",
                String.Format("next_url=&mail={0}&password={1}", emailAddress, password),
                tmpCc);

            CookieCollection ccol = tmpCc.GetCookies(new Uri(baseUrl));
            foreach (Cookie c in ccol)
            {
                Log.Debug("Add cookie " + c.ToString());
                cc.Add(c);
            }
            Cookie c2 = new Cookie();
            c2.Name = "lang";
            c2.Value = "en-us";
            c2.Expires = DateTime.Now.AddHours(1);
            c2.Domain = new Uri(baseUrl).Host;
            cc.Add(c2);
            if (Settings.Categories == null) Settings.Categories = new BindingList<Category>();
            RssLink totalCat = new RssLink()
            {
                Name = "Home",
                HasSubCategories = true,
                Url = baseUrl + "video_top/",
                Other = true
            };
            Settings.Categories.Add(totalCat);

            string data = GetNicoWebData(baseUrl + "video_top/");
            if (!string.IsNullOrEmpty(data))
            {
                return ParseCategories(data);
            }

            return 0;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            bool hasSub = false;
            if (parentCategory.ParentCategory == null && parentCategory.Other == null)
            {
                regEx_dynamicSubCategories = regEx_dynamicSubLevel1Categories;
                if (parentCategory.SubCategories == null) parentCategory.SubCategories = new List<Category>();
                RssLink totalCat = new RssLink()
                {
                    Name = "Total",
                    HasSubCategories = true,
                    ParentCategory = parentCategory,
                    Url = ((RssLink)parentCategory).Url
                };
                parentCategory.SubCategories.Add(totalCat);
                hasSub = true;
            }
            else
            {
                regEx_dynamicSubCategories = regEx_dynamicSubSubCategories;
            }

            string data = GetNicoWebData(((RssLink)parentCategory).Url);
            int res = ParseSubCategories(parentCategory, data);
            foreach (Category subcat in parentCategory.SubCategories)
                subcat.HasSubCategories = hasSub;
            parentCategory.SubCategoriesDiscovered = true;
            return res;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            string data = GetNicoWebData(((RssLink)category).Url);
            return Parse(baseUrl, data);
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            Match m = Regex.Match(video.VideoUrl, @"/(?<id>sm\d*)", defaultRegexOptions);
            if (m.Success)
            {
                string id = m.Groups["id"].Value;
                CookieContainer tmpCc = new CookieContainer();
                tmpCc.Add(cc.GetCookies(new Uri(baseUrl)));

                GetWebData(String.Format(@"http://www.nicovideo.jp/watch/{0}", id), cookies: tmpCc);// for getting cookies

                fileUrlPostString = "v=" + id;
                string oldUrl = video.VideoUrl;
                video.VideoUrl = @"http://flapi.nicovideo.jp/api/getflv";
                string res = HttpUtility.UrlDecode(base.GetVideoUrl(video));
                video.VideoUrl = oldUrl;
                HttpUrl result = new HttpUrl(res);

                CookieCollection ccol = tmpCc.GetCookies(new Uri(baseUrl));
                result.Cookies.Add(ccol);
                return result.ToString();
            }
            return String.Empty;
        }

        public override List<ISearchResultItem> Search(string query, string category = null)
        {
            string data = GetNicoWebData(String.Format(searchUrl, query));
            return Parse(baseUrl, data).ConvertAll<ISearchResultItem>(v => v as ISearchResultItem);
        }

        private string GetNicoWebData(string url)
        {
            return GetWebData(url,cookies: cc, forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, headers: headers, cache: true);
        }

        protected override CookieContainer GetCookie()
        {
            return cc;
        }
    }
}

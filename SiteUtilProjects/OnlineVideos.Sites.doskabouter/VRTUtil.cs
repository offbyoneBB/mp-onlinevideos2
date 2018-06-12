using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Net;
using System.Web;
using OnlineVideos.Helpers;

namespace OnlineVideos.Sites
{
    public class VRTUtil : GenericSiteUtil
    {
        [Category("OnlineVideosUserConfiguration"), Description("username")]
        private string userName = null;
        [Category("OnlineVideosUserConfiguration"), Description("Password"), PasswordPropertyText(true)]
        private string password = null;

        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the baseUrl for dynamic categories. Group names: 'url', 'title', 'thumb', 'description'. Will not be used if not set.")]
        protected string azRegex;

        private Regex regEx_ax;

        private enum listMode { Category, AZ, Episodes };

        public override int DiscoverDynamicCategories()
        {
            if (!string.IsNullOrEmpty(azRegex)) regEx_ax = new Regex(azRegex, defaultRegexOptions);
            Settings.Categories = new BindingList<Category>();
            RssLink cat = new RssLink { Name = "Categories", Url = "https://www.vrt.be/vrtnu/categorieen/", Other = listMode.Category, HasSubCategories = true };
            Settings.Categories.Add(cat);
            cat = new RssLink { Name = "A-Z", Url = "https://www.vrt.be/vrtnu/a-z/", Other = listMode.AZ, HasSubCategories = true };
            Settings.Categories.Add(cat);
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (listMode.Category.Equals(parentCategory.Other))
                return GetCategories(parentCategory);
            else
            if (listMode.Episodes.Equals(parentCategory.Other))
                return GetEpisodes(parentCategory);
            else
            if (listMode.AZ.Equals(parentCategory.Other))
                return GetAZ(parentCategory);
            else
                return 0;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            var res = base.GetVideos(category);
            if (res.Count == 0)
            {
                //probably one video
                var data = GetWebData(((RssLink)category).Url);
                if (data.IndexOf(@" <div class=""vrtvideo""") >= 0)
                {
                    var vid = new VideoInfo() { Title = category.Name, Thumb = category.Thumb };
                    var m = Regex.Match(data, @"&\#34;playlist&\#34;:&\#34;(?<url>[^&]*)&\#34");
                    if (m.Success)
                        vid.VideoUrl = fixUrl(m.Groups["url"].Value);
                    res.Add(vid);
                }
            }
            else
                foreach (VideoInfo vid in res)
                    vid.CleanDescriptionAndTitle();
            return res;
        }

        public override string GetVideoUrl(VideoInfo video)
        {

            string postData = "loginID=" + userName + "&password=" + password + "&APIKey=3_qhEcPa5JGFROVwu5SWKqJ4mVOIkwlFNMSKwzPDAh8QZOtHqu6L4nD5Q7lk0eXOOG&targetEnv=jssdk&includeSSOToken=true&authMode=cookie";
            var res = GetWebData<JToken>("https://accounts.eu1.gigya.com/accounts.login", postData);
            var uid = res.Value<String>("UID");
            var sig = res.Value<String>("UIDSignature");
            var ts = res.Value<String>("signatureTimestamp");

            postData = @"{""uid"": """ + uid + @""", ""uidsig"": """ + sig + @""", ""ts"": """ + ts + @""", ""email"": """ + userName + @"""}";

            var headers = new NameValueCollection();
            headers["Content-Type"] = "application/json";
            headers["Accept"] = "*/*";
            headers["User-Agent"] = OnlineVideoSettings.Instance.UserAgent;

            var cc = new CookieContainer();
            GetWebData(@"https://token.vrt.be", postData, referer: "https://www.vrt.be/vrtnu/", headers: headers, cookies: cc);
            var url = video.VideoUrl.TrimEnd('/') + ".mssecurevideo.json";
            var resp = GetWebData<JToken>(url, cookies: cc);
            url = @"https://mediazone.vrt.be/api/v1/vrtvideo/assets/" + resp.First.First.Value<String>("videoid");
            resp = GetWebData<JToken>(url);
            string hlsUrl = null;
            foreach (var src in resp["targetUrls"])
            {
                if (src.Value<String>("type") == "HLS")
                    hlsUrl = src.Value<String>("url");
            }

            var data = GetWebData(hlsUrl);
            video.PlaybackOptions = HlsPlaylistParser.GetPlaybackOptions(data, hlsUrl);
            return video.GetPreferredUrl(true);
        }

        private int GetEpisodes(Category parentCategory)
        {
            var data = GetWebData<JToken>(((RssLink)parentCategory).Url);
            parentCategory.SubCategories = new List<Category>();
            foreach (var cat in data)
            {
                RssLink subcat = new RssLink()
                {
                    Name = cat.Value<String>("title"),
                    Url = fixUrl(cat.Value<String>("targetUrl")),
                    Thumb = fixUrl(cat.Value<String>("thumbnail")),
                    ParentCategory = parentCategory,
                    Description = cat.Value<String>("description")
                };
                parentCategory.SubCategories.Add(subcat);
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        private string fixUrl(string url)
        {
            return url.Replace("//", "https://");
        }

        private int GetAZ(Category parentCategory)
        {
            var url = ((RssLink)parentCategory).Url;
            var data = GetWebData(url);

            parentCategory.SubCategories = new List<Category>();

            var azlist = data.Split(new[] { @"<h2 id=""" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < azlist.Length; i++)
            {
                int p = azlist[i].IndexOf('"');
                var azCat = new RssLink()
                {
                    Name = azlist[i].Substring(0, p).ToUpperInvariant(),
                    HasSubCategories = true,
                    ParentCategory = parentCategory,
                    SubCategoriesDiscovered = true,
                    SubCategories = new List<Category>()
                };
                parentCategory.SubCategories.Add(azCat);

                Match m = regEx_ax.Match(azlist[i]);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Url = FormatDecodeAbsolutifyUrl(baseUrl, m.Groups["url"].Value, null, dynamicSubCategoryUrlDecoding);
                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim());
                    cat.Thumb = m.Groups["thumb"].Value;
                    if (!String.IsNullOrEmpty(cat.Thumb) && !Uri.IsWellFormedUriString(cat.Thumb, System.UriKind.Absolute)) cat.Thumb = new Uri(new Uri(baseUrl), cat.Thumb).AbsoluteUri;
                    cat.Description = m.Groups["description"].Value;
                    cat.ParentCategory = azCat;
                    azCat.SubCategories.Add(cat);
                    m = m.NextMatch();
                }
            }

            return parentCategory.SubCategories.Count;
        }


        private int GetCategories(Category parentCategory)
        {
            var res = base.DiscoverSubCategories(parentCategory);
            foreach (var cat in parentCategory.SubCategories)
            {
                if (!String.IsNullOrEmpty(cat.Thumb))
                    cat.Thumb = cat.Thumb.Replace(@"https://www.vrt.be/images", @"https://images");
                cat.HasSubCategories = true;
                cat.Other = listMode.Episodes;
            }
            return res;
        }
    }
}

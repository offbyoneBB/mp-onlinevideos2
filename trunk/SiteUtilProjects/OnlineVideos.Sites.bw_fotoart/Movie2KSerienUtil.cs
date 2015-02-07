using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

using OnlineVideos.Hoster;


namespace OnlineVideos.Sites.bw_fotoart
{
    public class Movie2KSerienUtil : GenericSiteUtil
    {
        public class Movie2KSerienVideoInfo : VideoInfo
        {
            public Movie2KSerienUtil Util { get; set; }

            public override string GetPlaybackOptionUrl(string option)
            {
                return getPlaybackUrl(PlaybackOptions[option], Util);
            }

            public static string getPlaybackUrl(string playerUrl, Movie2KSerienUtil Util)
            {
                string data = WebCache.Instance.GetWebData(playerUrl, cookies: Util.GetCookie(), forceUTF8: Util.forceUTF8Encoding, allowUnsafeHeader: Util.allowUnsafeHeaders, encoding: Util.encodingOverride);
                Match m = Regex.Match(data, Util.hosterUrlRegEx);
                string url = m.Groups["url"].Value;
                Uri uri = new Uri(url);
                foreach (HosterBase hosterUtil in HosterFactory.GetAllHosters())
                    if (uri.Host.ToLower().Contains(hosterUtil.GetHosterUrl().ToLower()))
                    {
                        Dictionary<string, string> options = hosterUtil.GetPlaybackOptions(url);
                        if (options != null && options.Count > 0)
                        {
                            url = options.Last().Value;
                        }
                        break;
                    }
                return url;
            }
        }

        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for dynamic categories. Group names: 'url', 'title', 'thumb', 'description'. Will be used on the web pages resulting from the links from the dynamicSubCategoriesRegEx. Will not be used if not set.")]
        protected string dynamicSubSubCategoriesRegEx;
        [Category("OnlineVideosConfiguration"), Description("Format string applied to the 'url' match retrieved from the dynamicSubSubCategoriesRegEx.")]
        protected string dynamicSubSubCategoryUrlFormatString;
        [Category("OnlineVideosConfiguration"), Description("What type of decoding should be applied to the 'url' match of the dynamicSubSubCategoriesRegEx.")]
        protected UrlDecoding dynamicSubSubCategoryUrlDecoding = UrlDecoding.None;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the data retrieved to get the dynamic subsubcategories for a link to another page with more subcategories. Group names: 'url'. Will not be used if not set.")]
        protected string dynamicSubSubCategoriesNextPageRegEx;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the url for a specific hoster")]
        protected string hosterUrlRegEx;

        [Category("OnlineVideosUserConfiguration"), Description("Define if you only want to see German videos listed.")]
        protected bool OnlyGerman = true;

        protected Regex regEx_dynamicSubSubCategories, regEx_dynamicSubSubCategoriesNextPage;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            RegexOptions defaultRegexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture;

            if (!string.IsNullOrEmpty(dynamicSubSubCategoriesRegEx)) regEx_dynamicSubSubCategories = new Regex(dynamicSubSubCategoriesRegEx, defaultRegexOptions);
            if (!string.IsNullOrEmpty(dynamicSubSubCategoriesNextPageRegEx)) regEx_dynamicSubSubCategoriesNextPage = new Regex(dynamicSubSubCategoriesNextPageRegEx, defaultRegexOptions);
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (parentCategory.ParentCategory == null)
            {
                var subcats = base.DiscoverSubCategories(parentCategory);
                if (parentCategory.SubCategories != null && regEx_dynamicSubSubCategories != null)
                {
                    parentCategory.SubCategories.ForEach(sc => sc.HasSubCategories = true);
                }
                return subcats;
            }
            else
            {
                return ParseSubSubCategories(parentCategory, null);
            }
        }

        public virtual int ParseSubSubCategories(Category parentCategory, string data)
        {
            if (parentCategory is RssLink && regEx_dynamicSubSubCategories != null)
            {
                if (data == null)
                    data = GetWebData((parentCategory as RssLink).Url, cookies: GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);
                if (!string.IsNullOrEmpty(data))
                {
                    List<Category> dynamicSubCategories = new List<Category>(); // put all new discovered Categories in a separate list
                    Match m = regEx_dynamicSubSubCategories.Match(data);
                    while (m.Success)
                    {
                        RssLink cat = new RssLink();
                        cat.Url = m.Groups["url"].Value;
                        if (!string.IsNullOrEmpty(dynamicSubSubCategoryUrlFormatString)) cat.Url = string.Format(dynamicSubSubCategoryUrlFormatString, cat.Url);
                        cat.Url = ApplyUrlDecoding(cat.Url, dynamicSubSubCategoryUrlDecoding);
                        if (!Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(baseUrl), cat.Url).AbsoluteUri;
                        cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim());
                        cat.Thumb = m.Groups["thumb"].Value;
                        if (!String.IsNullOrEmpty(cat.Thumb) && !Uri.IsWellFormedUriString(cat.Thumb, System.UriKind.Absolute)) cat.Thumb = new Uri(new Uri(baseUrl), cat.Thumb).AbsoluteUri;
                        cat.Description = m.Groups["description"].Value;
                        cat.ParentCategory = parentCategory;
                        dynamicSubCategories.Add(cat);
                        m = m.NextMatch();
                    }
                    // discovery finished, copy them to the actual list -> prevents double entries if error occurs in the middle of adding
                    if (parentCategory.SubCategories == null) parentCategory.SubCategories = new List<Category>();
                    foreach (Category cat in dynamicSubCategories) parentCategory.SubCategories.Add(cat);
                    parentCategory.SubCategoriesDiscovered = dynamicSubCategories.Count > 0; // only set to true if actually discovered (forces re-discovery until found)
                    // Paging for SubCategories
                    if (parentCategory.SubCategories.Count > 0 && regEx_dynamicSubSubCategoriesNextPage != null)
                    {
                        m = regEx_dynamicSubSubCategoriesNextPage.Match(data);
                        if (m.Success)
                        {
                            string nextCatPageUrl = m.Groups["url"].Value;
                            if (!Uri.IsWellFormedUriString(nextCatPageUrl, System.UriKind.Absolute)) nextCatPageUrl = new Uri(new Uri(baseUrl), nextCatPageUrl).AbsoluteUri;
                            parentCategory.SubCategories.Add(new NextPageCategory() { Url = nextCatPageUrl, ParentCategory = parentCategory });
                        }
                    }
                }
                return parentCategory.SubCategories == null ? 0 : parentCategory.SubCategories.Count;
            }
            else
            {
                return base.DiscoverSubCategories(parentCategory);
            }
        }

        public override VideoInfo CreateVideoInfo()
        {
            return new Movie2KSerienVideoInfo() { Util = this };
        }

        //protected override CookieContainer GetCookie()
        //{
        //    if (OnlyGerman) return base.GetCookie();
        //    else return null;
        //}

        public override string GetVideoUrl(VideoInfo video)
        {
            string result = base.GetVideoUrl(video);
            if (video.PlaybackOptions == null && !string.IsNullOrEmpty(result)) 
                result = Movie2KSerienVideoInfo.getPlaybackUrl(result, this);
            return result;
        }

        public override List<ISearchResultItem> Search(string query, string category = null)
        {
            string html = GetWebData("http://www.movie2k.to/searchAutoCompleteNew.php?search=" + query);
            List<ISearchResultItem> results = new List<ISearchResultItem>();


            return results;
        }
    }
}
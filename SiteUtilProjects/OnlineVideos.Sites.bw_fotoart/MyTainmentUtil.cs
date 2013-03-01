<<<<<<< .mine
﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;


using OnlineVideos.Hoster.Base;


namespace OnlineVideos.Sites.bw_fotoart
{
    public class MyTainmentUtil : GenericSiteUtil
    {
        public class MyTainmentVideoInfo : VideoInfo
        {
            public MyTainmentUtil Util { get; set; }

            public override string GetPlaybackOptionUrl(string option)
            {
                return getPlaybackUrl(PlaybackOptions[option], Util);
            }

            public static string getPlaybackUrl(string playerUrl, MyTainmentUtil Util)
            {

                string data = GetWebData(playerUrl, Util.GetCookie(), forceUTF8: Util.forceUTF8Encoding, allowUnsafeHeader: Util.allowUnsafeHeaders, encoding: Util.encodingOverride);
                WebRequest request = WebRequest.Create(playerUrl);
                WebResponse response = request.GetResponse();
                string url = response.ResponseUri.ToString();
                Uri uri = new Uri(url);
                foreach (HosterBase hosterUtil in HosterFactory.GetAllHosters())
                    if (uri.Host.ToLower().Contains(hosterUtil.getHosterUrl().ToLower()))
                    {
                        Dictionary<string, string> options = hosterUtil.getPlaybackOptions(url);
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

        protected Regex regEx_dynamicSubSubCategories, regEx_dynamicSubSubCategoriesNextPage;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            RegexOptions defaultRegexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture;

            if (!string.IsNullOrEmpty(dynamicSubSubCategoriesRegEx)) regEx_dynamicSubSubCategories = new Regex(dynamicSubSubCategoriesRegEx, defaultRegexOptions);
            if (!string.IsNullOrEmpty(dynamicSubSubCategoriesNextPageRegEx)) regEx_dynamicSubSubCategoriesNextPage = new Regex(dynamicSubSubCategoriesNextPageRegEx, defaultRegexOptions);
        }        

        public override VideoInfo CreateVideoInfo()
        {

            return new MyTainmentVideoInfo() { Util = this };
        }

        public override string getUrl(VideoInfo video)
        {
            string result = base.getUrl(video);
            if (video.PlaybackOptions == null && !string.IsNullOrEmpty(result))
                result = MyTainmentVideoInfo.getPlaybackUrl(result, this);
            return result;
        }


        public override int DiscoverDynamicCategories()
        {
            if (regEx_dynamicCategories == null)
            {
                Settings.DynamicCategoriesDiscovered = true;

                if (Settings.Categories.Count > 0 && regEx_dynamicSubCategories != null)
                {
                    for (int i = 0; i < Settings.Categories.Count; i++)
                        if (!(Settings.Categories[i] is Group))
                            Settings.Categories[i].HasSubCategories = true;
                }
            }
            else
            {
                string data = GetWebData(baseUrl, GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);
                if (!string.IsNullOrEmpty(data))
                {
                    return ParseCategories(data);
                }
            }
            return 0; // coming here means no dynamic categories were discovered
        }

        public override int ParseCategories(string data)
        {
            List<Category> dynamicCategories = new List<Category>(); // put all new discovered Categories in a separate list
            Match m = regEx_dynamicCategories.Match(data);
            while (m.Success)
            {
                RssLink cat = new RssLink();
                cat.Url = FormatDecodeAbsolutifyUrl(baseUrl, m.Groups["url"].Value, dynamicCategoryUrlFormatString, dynamicCategoryUrlDecoding);
                cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim().Replace('\n', ' '));
                cat.Thumb = m.Groups["thumb"].Value;
                if (!String.IsNullOrEmpty(cat.Thumb) && !Uri.IsWellFormedUriString(cat.Thumb, System.UriKind.Absolute)) cat.Thumb = new Uri(new Uri(baseUrl), cat.Thumb).AbsoluteUri;
                cat.Description = m.Groups["description"].Value;
                if (regEx_dynamicSubCategories != null) cat.HasSubCategories = true;
                dynamicCategories.Add(cat);
                m = m.NextMatch();
            }
            // discovery finished, copy them to the actual list -> prevents double entries if error occurs in the middle of adding
            if (Settings.Categories == null) Settings.Categories = new BindingList<Category>();
            foreach (Category cat in dynamicCategories) Settings.Categories.Add(cat);
            Settings.DynamicCategoriesDiscovered = dynamicCategories.Count > 0; // only set to true if actually discovered (forces re-discovery until found)
            // Paging for Categories
            if (dynamicCategories.Count > 0 && regEx_dynamicCategoriesNextPage != null)
            {
                m = regEx_dynamicCategoriesNextPage.Match(data);
                if (m.Success)
                {
                    string nextCatPageUrl = m.Groups["url"].Value;
                    string[] urlSubString = nextCatPageUrl.Split('&');
                    nextCatPageUrl = urlSubString[0] + '&' + urlSubString[2];
                    nextCatPageUrl = System.Web.HttpUtility.HtmlDecode(nextCatPageUrl);                            
                    if (!Uri.IsWellFormedUriString(nextCatPageUrl, System.UriKind.Absolute)) nextCatPageUrl = new Uri(new Uri(baseUrl), nextCatPageUrl).AbsoluteUri;
                    Settings.Categories.Add(new NextPageCategory() { Url = nextCatPageUrl });
                }
            }
            return dynamicCategories.Count;
        }


        public override int DiscoverSubCategories(Category parentCategory)
        {
            return ParseSubCategories(parentCategory, null);
        }

        public override int ParseSubCategories(Category parentCategory, string data)
        {
            if (parentCategory is RssLink && regEx_dynamicSubCategories != null)
            {
                if (data == null)
                    data = GetWebData((parentCategory as RssLink).Url, GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);
                if (!string.IsNullOrEmpty(data))
                {
                    List<Category> dynamicSubCategories = new List<Category>(); // put all new discovered Categories in a separate list
                    Match m = regEx_dynamicSubCategories.Match(data);
                    while (m.Success)
                    {
                        RssLink cat = new RssLink();
                        cat.Url = FormatDecodeAbsolutifyUrl(baseUrl, m.Groups["url"].Value, dynamicSubCategoryUrlFormatString, dynamicSubCategoryUrlDecoding);
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
                    if (parentCategory.SubCategories.Count > 0 && regEx_dynamicSubCategoriesNextPage != null)
                    {
                        m = regEx_dynamicSubCategoriesNextPage.Match(data);
                        if (m.Success)
                        {
                            string nextCatPageUrl = m.Groups["url"].Value;
                            string[] urlSubString = nextCatPageUrl.Split('&');
                            nextCatPageUrl = urlSubString[0] + '&' + urlSubString[2];
                            nextCatPageUrl = System.Web.HttpUtility.HtmlDecode(nextCatPageUrl);
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


        public virtual int ParseSubSubCategories(Category parentCategory, string data)
        {
            if (parentCategory is RssLink && regEx_dynamicSubSubCategories != null)
            {
                if (data == null)
                    data = GetWebData((parentCategory as RssLink).Url, GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);
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
    }
}=======
﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

using OnlineVideos.Hoster.Base;


namespace OnlineVideos.Sites.bw_fotoart
{
    public class MyTainmentUtil : GenericSiteUtil
    {
        public class MyTainmentVideoInfo : VideoInfo
        {
            public MyTainmentUtil Util { get; set; }

            public override string GetPlaybackOptionUrl(string option)
            {
                return getPlaybackUrl(PlaybackOptions[option], Util);
            }

            public static string getPlaybackUrl(string playerUrl, MyTainmentUtil Util)
            {       

                string data = GetWebData(playerUrl, Util.GetCookie(), forceUTF8: Util.forceUTF8Encoding, allowUnsafeHeader: Util.allowUnsafeHeaders, encoding: Util.encodingOverride);
                WebRequest request = WebRequest.Create(playerUrl);
                WebResponse response = request.GetResponse();
                string url = response.ResponseUri.ToString();
                Uri uri = new Uri(url);
                foreach (HosterBase hosterUtil in HosterFactory.GetAllHosters())
                    if (uri.Host.ToLower().Contains(hosterUtil.getHosterUrl().ToLower()))
                    {
                        Dictionary<string, string> options = hosterUtil.getPlaybackOptions(url);
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
                    data = GetWebData((parentCategory as RssLink).Url, GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);
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
            
            return new MyTainmentVideoInfo() { Util = this };
        }

        public override string getUrl(VideoInfo video)
        {
            string result = base.getUrl(video);
            if (video.PlaybackOptions == null && !string.IsNullOrEmpty(result))
                result = MyTainmentVideoInfo.getPlaybackUrl(result, this);
            return result;
        }
    }
}>>>>>>> .r2750

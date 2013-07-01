using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Net;
using System.Web;
using System.Linq;

namespace OnlineVideos.Sites
{
    public class UitzendingGemistUtil : GenericSiteUtil
    {

        public enum VideoFormat { Wmv_Sb, Mov_Sb, Wmv_Bb, Mov_Bb, Mov_Std, Wvc1_Std };

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Preferred Format"), Description("Prefer this format when there are more than one for the desired quality.")]
        VideoFormat preferredFormat = VideoFormat.Wvc1_Std;

        private enum UgType { None, Recent, Omroepen, Genres, AtoZ, Type1, AtoZSub, Search };
        private Regex savedRegEx_dynamicSubCategoriesNextPage;

        string matchaz = @"(?<=<ol\sclass=""letters"">.*)<li[^>]*>\s*<a\shref=""(?<url>[^""]*)""(?:\sclass="""")?\stitle=""(?<description>[^""]*)"">(?<title>[^<]*)</a>\s*</li>";

        public override int DiscoverDynamicCategories()
        {
            savedRegEx_dynamicSubCategoriesNextPage = regEx_dynamicSubCategoriesNextPage;

            Settings.Categories.Add(new RssLink() { Name = "Afgelopen 7 dagen", Url = @"weekarchief/vandaag", Other = UgType.Recent });
            Settings.Categories.Add(new RssLink() { Name = "Omroepen", Url = @"omroepen", Other = UgType.Omroepen });
            Settings.Categories.Add(new RssLink() { Name = "Genres", Url = @"genres", Other = UgType.Genres });
            Settings.Categories.Add(new RssLink() { Name = "Programma’s A-Z", Url = @"programmas", Other = UgType.Type1 });
            foreach (RssLink cat in Settings.Categories)
            {
                cat.HasSubCategories = true;
                cat.Url = baseUrl + cat.Url;
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            switch ((UgType)parentCategory.Other)
            {
                case UgType.Recent: return getSubcats(parentCategory, UgType.None, @"<li><a\shref=""(?<url>[^""]*)"">(?<title>[^<]*)</a></li>");
                case UgType.Omroepen: return getSubcats(parentCategory, UgType.Type1, @"<li\sclass=""broadcaster\sknav""\sid=""broadcaster[^""]*"">\s*<a[^>]*><img\salt=""[^""]*""\sclass=""thumbnail""\ssrc=""(?<thumb>[^""]*)""\s/></a>\s*<h3><a\shref=""(?<url>[^""]*)""\sclass=""broadcaster\sknav_link""\stitle=""[^""]*"">(?<title>[^<]*)</a></h3>");
                case UgType.Genres: return getSubcats(parentCategory, UgType.Type1, @"<li\sclass=""genre\sknav""\sid=""genre[^""]*"">\s*<a[^>]*><img\salt=""[^""]*""\sclass=""thumbnail""\ssrc=""(?<thumb>[^""]*)""\s/></a>\s*<h3><a\shref=""(?<url>[^""]*)""\sclass=""genre\sknav_link""\stitle=""[^""]*"">(?<title>[^<]*)</a></h3>");
                case UgType.AtoZ: return getSubcats(parentCategory, UgType.Type1, matchaz);
                case UgType.Type1: return getType1Subcats(parentCategory);
                case UgType.AtoZSub: return getSubcats(parentCategory, UgType.None, @"<li\sclass=""series\sknav""\sid=""series_(?<seriesid>[^""]*)"">\s*<a[^>]*><img\salt=""[^""]*""\sclass=""thumbnail""\sdata-images=""\[(?:&quot;(?<thumb>[^&]*)&)?[^""]*""\sheight=""[^""]*""\ssrc=""[^""]*""[^>]*></a>\s*<h3><a\shref=""(?<url>[^""]*)""\sclass=""series\sknav_link""\stitle=""[^""]*"">(?<title>[^<]*)</a>\s<span\sclass=""broadcasters"">\([^\)]*\)</span></h3>\s*</li>");
                case UgType.Search: return getSubcats(parentCategory, UgType.None, @"<li\sclass=""series\sknav""\sid=""series_(?<seriesid>[^""]*)"">\s*<div\sclass=""wrapper"">\s*<div\sclass=""img"">\s*<a[^>]*><img\salt=""[^""]*""\sclass=""thumbnail""\sdata-images=""\[(?:&quot;(?<thumb>[^&]*)&)?[^>]*></a>\s*</div>\s*<h3><a\shref=""(?<url>[^""]*)""\sclass=""series\sknav_link""\stitle=""[^""]*"">(?<title>[^<]*)</a>\s<span\sclass=""broadcasters"">\([^\)]*\)</span></h3>\s*<div\sclass=""episodes-count"">[^<]*</div>\s*</div>\s*<div\sclass=""small-wrapper"">");
            }
            return 0;
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            int result = base.DiscoverNextPageCategories(category);
            if ((UgType)category.ParentCategory.Other == UgType.Search)
                foreach (RssLink npCat in category.ParentCategory.SubCategories)
                    if (npCat is NextPageCategory)
                        npCat.Url = HttpUtility.HtmlDecode(npCat.Url) + "&_pjax=true";

            return result;
        }

        private int getType1Subcats(Category parentCat)
        {
            if (parentCat.SubCategories == null) parentCat.SubCategories = new List<Category>();

            //string webData = GetWebData(((RssLink)parentCat).Url, userAgent: UgUserAgent, cc: SiteConsent);

            RssLink cat = new RssLink()
            {
                Name = "Alle Programma's",
                //Other = UgType.AtoZSub, will be set by call to getsubcats
                ParentCategory = parentCat,
                HasSubCategories = true
            };

            /*
            For now: no details
            Match m = Regex.Match(webData, @"<li>\s*<a\shref=""(?<url>[^""]*)""\srel=""detail""\stitle=""[^""]*"">");
            if (m.Success)
                cat.Url = new Uri(new Uri(baseUrl), m.Groups["url"].Value).AbsoluteUri;
            else*/
            cat.Url = ((RssLink)parentCat).Url;

            parentCat.SubCategories.Add(cat);

            getSubcats(parentCat, UgType.AtoZSub, matchaz);
            if (parentCat.SubCategories.Count > 0) parentCat.SubCategoriesDiscovered = true;
            return parentCat.SubCategories.Count;
        }

        private int getSubcats(Category parentCat, UgType subType, string regexString)
        {
            Regex r = new Regex(regexString, defaultRegexOptions);
            regEx_dynamicSubCategories = r;
            if ((UgType)parentCat.Other == UgType.Search)
            {
                regEx_dynamicSubCategoriesNextPage = new Regex(@"<div\sid=""right-arrow""\sclass=""arrow"">\s*<a\shref=""(?<url>[^""]*)""><img\salt=""Search_arrow_right""", defaultRegexOptions);
            }
            else
                if ((UgType)parentCat.Other == UgType.AtoZSub)
                {
                    regEx_dynamicSubCategoriesNextPage = savedRegEx_dynamicSubCategoriesNextPage;
                }
                else
                {
                    regEx_dynamicSubCategoriesNextPage = null;
                }
            string data = GetWebData(((RssLink)parentCat).Url, cc: GetCookie());
            int res = ParseSubCategories(parentCat, data);
            if (res > 0)
                foreach (RssLink cat in parentCat.SubCategories)
                {
                    if ((UgType)parentCat.Other == UgType.Search && cat is NextPageCategory)
                        cat.Url = HttpUtility.HtmlDecode(cat.Url) + "&_pjax=true";

                    if (subType != UgType.None)
                    {
                        cat.HasSubCategories = true;
                        if (subType == UgType.AtoZSub)
                        {
                            if (parentCat.ParentCategory == null)
                                // this is the az under ug/programmas
                                cat.Url = cat.Url + @"?display_mode=images-selected";
                        }
                        cat.Other = subType;
                    }
                }
            return res;
        }

        public override string getUrl(VideoInfo video)
        {
            string res = base.getUrl(video);
            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 1)
            {
                foreach (KeyValuePair<string, string> kv in video.PlaybackOptions)
                {
                    VideoFormat fmt;
                    try
                    {
                        fmt = (VideoFormat)Enum.Parse(typeof(VideoFormat), kv.Key.Replace(' ', '_'), true);
                        if (Enum.IsDefined(typeof(VideoFormat), fmt) && fmt.Equals(preferredFormat))
                            return kv.Value;
                    }
                    catch (ArgumentException)
                    {
                    }
                }
                res = video.PlaybackOptions.Last().Value;
            }
            return res;
        }

        protected override List<VideoInfo> Parse(string url, string data)
        {
            if (data == null)
                data = GetWebData(url, cc: GetCookie());
            bool fromSearch = url.Contains(".nl/zoek/");
            data = Regex.Unescape(data);
            if (fromSearch)
                data = data.Replace("<em>", String.Empty).Replace("</em>", String.Empty);// for search results
            List<VideoInfo> res = base.Parse(url, data);
            if (nextPageAvailable && fromSearch)
                nextPageUrl = HttpUtility.HtmlDecode(nextPageUrl) + "&_pjax=true";
            return res;
        }

        public override List<ISearchResultItem> DoSearch(string query)
        {
            List<ISearchResultItem> result = new List<ISearchResultItem>();
            RssLink cat = new RssLink()
            {
                Name = "Programmas",
                Other = UgType.Search,
                Url = String.Format(@"http://www.uitzendinggemist.nl/zoek/programmas?id={0}&series_page=1&_pjax=true", query),
                HasSubCategories = true
            };
            result.Add(cat);

            cat = new RssLink()
            {
                Name = "Uitzendingen",
                Url = String.Format(@"http://www.uitzendinggemist.nl/zoek/uitzendingen?id={0}&page=1&_pjax=true", query)
            };
            result.Add(cat);
            return result;
        }

    }

}

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class UitzendingGemistUtil : GenericSiteUtil
    {
        private enum UgType { None, Recent, Omroepen, Genres, AtoZ, Type1, AtoZSub };
        private RegexOptions defaultRegexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture;
        private Regex savedRegEx_dynamicSubCategoriesNextPage;

        string matchaz = @"(?<=<ol\sclass=""letters"">.*)<li[^>]*>\s*<a\shref=""(?<url>[^""]*)""\stitle=""(?<description>[^""]*)"">(?<title>[^<]*)</a>\s*</li>";

        public override void Initialize(OnlineVideos.SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
        }

        public override int DiscoverDynamicCategories()
        {
            savedRegEx_dynamicSubCategoriesNextPage = regEx_dynamicSubCategoriesNextPage;

            Settings.Categories.Add(new RssLink() { Name = "Afgelopen 7 dagen", Url = @"7dagen", Other = UgType.Recent });
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
                case UgType.Omroepen: return getSubcats(parentCategory, UgType.Type1, @"<li\sclass=""[^""]*"">\s*<a\shref=""(?<url>[^""]*)""><img\salt=""[^""]*""\sclass=""broadcaster-image""\srel=""broadcaster""\ssrc=""(?<thumb>[^""]*)""\stitle=""[^""]*""\s/></a>\s*<h4><a[^>]*>(?<title>[^<]*)</a></h4>\s*</li>");
                case UgType.Genres: return getSubcats(parentCategory, UgType.Type1, @"<li\sclass=""[^""]*"">\s*<a\shref=""(?<url>[^""]*)""><img\salt=""[^""]*""\sclass=""genre-image""\ssrc=""(?<thumb>[^""]*)""\s/></a>\s*<h2><a\shref=""[^""]*"">(?<title>[^<]*)</a></h2>\s*</li>");
                case UgType.AtoZ: return getSubcats(parentCategory, UgType.Type1, matchaz);
                case UgType.Type1: return getType1Subcats(parentCategory);
                case UgType.AtoZSub: return getSubcats(parentCategory, UgType.None, @"<li\sclass=""series[^""]*""\sid=""series_(?<url>[^""]*)"">\s*(?:<a[^>]*><img\salt=""[^""]*""\sclass=""thumbnail""\ssrc=""(?<thumb>[^""]*)""\s/></a>)?\s*<div\sclass=""info"">\s*<h3>\s*<a\shref=""(?<fullurl>[^""]*)""\sclass=""series""\srel=""series""\stitle=""[^""]*"">(?<title>[^<]*)</a>\s*<span\sclass=""broadcasters"">\([^\)]*\)</span>\s*</h3>\s*</div>");
            }
            return 0;
        }

        private int getType1Subcats(Category parentCat)
        {
            if (parentCat.SubCategories == null) parentCat.SubCategories = new List<Category>();

            string webData = GetWebData(((RssLink)parentCat).Url);

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
            if ((UgType)parentCat.Other == UgType.AtoZSub)
            {
                dynamicSubCategoryUrlFormatString = baseUrl + @"quicksearch/episodes?series_id={0}";
                regEx_dynamicSubCategoriesNextPage = savedRegEx_dynamicSubCategoriesNextPage;
            }
            else
            {
                regEx_dynamicSubCategoriesNextPage = null;
                dynamicSubCategoryUrlFormatString = null;
            }
            int res = base.DiscoverSubCategories(parentCat);
            if (res > 0)
                foreach (Category cat in parentCat.SubCategories)
                {
                    if (subType != UgType.None)
                    {
                        cat.HasSubCategories = true;
                        cat.Other = subType;
                    }
                }
            return res;
        }

        protected override List<VideoInfo> Parse(string url, string data)
        {
            if (data == null)
                data = GetWebData(url);
            data = Regex.Unescape(data);
            List<VideoInfo> res = base.Parse(url, data);

            foreach (VideoInfo video in res)
                if (!String.IsNullOrEmpty(video.Length))
                {
                    video.Title += ' ' + video.Length;
                    video.Length = null;
                }
            return res;
        }

    }

}

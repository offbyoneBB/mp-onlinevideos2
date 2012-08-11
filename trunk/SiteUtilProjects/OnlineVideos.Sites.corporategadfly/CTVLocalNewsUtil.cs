using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class CTVLocalNewsUtil : CTVNewsUtil
    {
        private static Regex subCategoriesRegex = new Regex(@"<dt\sclass=""videoPlaylistCategories""\sid=""(?<binId>[^""]*)""><a\shref[^>]*>(?<title>[^<]*)</a></dt>",
                                                            RegexOptions.Compiled);
        
        private static readonly OrderedDictionary localChannels = new OrderedDictionary()
        {
            {"Atlantic",                "atlantic.ctvnews.ca"},
            {"British Columbia",        "bc.ctvnews.ca"},
            {"Calgary",                 "calgary.ctvnews.ca"},
            {"Edmonton",                "edmonton.ctvnews.ca"},
            {"Kitchener",               "kitchener.ctvnews.ca"},
            {"Montreal",                "montreal.ctvnews.ca"},
            {"Northern Ontario",        "northernontario.ctvnews.ca"},
            {"Ottawa",                  "ottawa.ctvnews.ca"},
            {"Regina",                  "regina.ctvnews.ca"},
            {"Saskatoon",               "saskatoon.ctvnews.ca"},
            {"Toronto",                 "toronto.ctvnews.ca"},
            {"Winnipeg",                "winnipeg.ctvnews.ca"}
        };

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            foreach (DictionaryEntry channel in localChannels)
            {
                Log.Debug(@"{0}: {1}", channel.Key, channel.Value);

                RssLink cat = new RssLink() {
                    Name = (string) channel.Key,
                    Url = (string) channel.Value,
                    HasSubCategories = true
                };

                Settings.Categories.Add(cat);
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }
        
        public override int DiscoverSubCategories(Category parentCategory)
        {
            baseUrl = string.Format(@"http://{0}", ((RssLink) parentCategory).Url);
            parentCategory.SubCategories = new List<Category>();

            string webData = GetWebData(string.Format(@"{0}/video", baseUrl));

            if (!string.IsNullOrEmpty(webData))
            {
                foreach (Match m in subCategoriesRegex.Matches(webData))
                {
                    string binId = m.Groups["binId"].Value;

                    RssLink cat = new RssLink() {
                        ParentCategory = parentCategory,
                        Name = m.Groups["title"].Value,
                        Url = string.Format(videoListUrlFormat, baseUrl, binId, "1"),
                        HasSubCategories = false
                    };

                    parentCategory.SubCategories.Add(cat);
                }
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Site Utility for Global News.
    /// </summary>
    public class GlobalNewsUtil : CanWestUtil
    {
        private static readonly OrderedDictionary localChannels = new OrderedDictionary()
        {
            {"Global News",            "z/Global News Player - Main"},
            {"Global National",        "z/Global Player - The National VC"},
            {"British Columbia",       "z/Global BC Player - Video Center"},
            {"Calgary",                "z/Global CGY Player - Video Center"},
            {"Edmonton",               "z/Global EDM Player - Video Center"},
            {"Lethbridge",             "z/Global LTH Player - Video Center"},
            {"Maritimes",              "z/Global MAR Player - Video Center"},
            {"Montreal",               "z/Global QC Player - Video Center"},
            {"Regina",                 "z/Global REG Player - Video Center"},
            {"Saskatoon",              "z/Global SAS Player - Video Center"},
            {"Toronto",                "z/Global ON Player - Video Center"},
            {"Winnipeg",               "z/Global WIN Player - Video Center"}
        };
        
        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            string webData = GetWebData(feedPIDUrl);

            if (!string.IsNullOrEmpty(webData))
            {
                Match match = feedPIDRegex.Match(webData);
                if (match.Success)
                {
                    feedPID = match.Groups["feedPID"].Value;

                    Log.Debug(@"Feed PID: {0}", feedPID);
                }
            }

            foreach (DictionaryEntry channel in localChannels)
            {
                Log.Debug(@"{0}: {1}", channel.Key, channel.Value);

                RssLink cat = new RssLink() {
                    Name = (string) channel.Key,
                    Other = (string) channel.Value,
                    HasSubCategories = true
                };

                Settings.Categories.Add(cat);
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }
        
        public override int DiscoverSubCategories(Category parentCategory)
        {
            playerTag = parentCategory.Other as string;
            parentCategory.SubCategories = new List<Category>();
            
            DiscoverDynamicCategoriesUsingJson(parentCategory);

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }
    }
}

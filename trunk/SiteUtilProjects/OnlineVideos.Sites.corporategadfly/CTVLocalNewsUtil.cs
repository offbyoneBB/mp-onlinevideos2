using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class CTVLocalNewsUtil : CTVUtilBase
    {
        public override string BaseUrl { get { return @"http://watch.ctv.ca"; } }
        public override int StartingPanelLevel { get { return 2; } }

        private Regex videoListRegex = new Regex(
            @"<a\shref=""javascript:changeVideo\('[^']*','[^']*','[^']*','(?<length>[^']*)','(?<clip>[^']*)','[^']*'\)"">       # changeVideo javascript link
            (
                \s(?<title>[^<]*)</a>\ </p>                                                                                     # ctvbc
                |
                <div[^>]*>\s+(?<title>[^<]*)</div></a>\s+<div\ id=""blurb[^>]*>\s(?<description>.*?)\s</div>                    # calgary|edmonton|montreal|northerontario|ottawa|toronto|winnipeg
                |
                \s(?<title>.*?)\s+</div>\s+</a>\s+<<div\ id=""blurb[^>]*>\s(?<description>.*?)\s<script>                        # regina|saskatoon|swo
            )",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

        private static readonly OrderedDictionary localChannels = new OrderedDictionary()
        {
            {"British Columbia",        "www.ctvbc.ctv.ca"},
            {"Calgary",                 "calgary.ctv.ca"},
            {"Edmonton",                "edmonton.ctv.ca"},
            {"Montreal",                "montreal.ctv.ca"},
            {"Northern Ontario",        "northernontario.ctv.ca"},
            {"Ottawa",                  "ottawa.ctv.ca"},
            {"Regina",                  "regina.ctv.ca"},
            {"Saskatoon",               "saskatoon.ctv.ca"},
            {"Southwestern Ontario",    "swo.ctv.ca"},
            {"Toronto",                 "toronto.ctv.ca"},
            {"Winnipeg",                "winnipeg.ctv.ca"}
        };

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            foreach (DictionaryEntry channel in localChannels)
            {
                Log.Debug(@"{0}: {1}", channel.Key, channel.Value);
                RssLink cat = new RssLink();
                cat.Name = (string) channel.Key;
                cat.Url = String.Format(@"http://{0}/", (string) channel.Value);
                cat.HasSubCategories = false;

                Settings.Categories.Add(cat);
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> result = new List<VideoInfo>();

            RssLink rssLink = (RssLink) category;
            string webData = GetWebData(rssLink.Url);

            if (!string.IsNullOrEmpty(webData))
            {
                foreach (Match m in videoListRegex.Matches(webData))
                {
                    VideoInfo info = new VideoInfo();
                    info.Length = m.Groups["length"].Value;
                    info.Title = m.Groups["title"].Value;
                    info.Other = m.Groups["clip"].Value;
                    if (!string.IsNullOrEmpty(m.Groups["description"].Value))
                    {
                        info.Description = m.Groups["description"].Value.Trim();
                    }
                    result.Add(info);
                }
            }

            return result;
        }
    }
}

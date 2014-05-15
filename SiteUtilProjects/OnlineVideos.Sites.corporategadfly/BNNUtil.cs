using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class BNNUtil : CTVDeprecatedUtil
    {
        private static Regex weekdayRegex = new Regex(@"Monday|Tuesday|Wednesday|Thursday|Friday",
            RegexOptions.Compiled);
        private static Regex weekdayEpisodeListRegex = new Regex(@"<dt><a[^>]*>(?<title>[^<]*)</a></dt>\s+<dd\sclass=""Thumbnail"">.*?ClipId:'(?<episode>[^']*)'.*?<img\ssrc=""(?<thumb>[^""]*)""\s/><span></span></a></dd>\s+<dd class=""Description"">(?<description>[^<]*)</dd>",
            RegexOptions.Compiled);

        /*
         * Contains special processing for weekday main categories
         */
        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> result = new List<VideoInfo>();

            string url = (string)((RssLink)category).Url;
            string webData = GetWebData(url);

            if (!string.IsNullOrEmpty(webData))
            {
                Match weekdayMatch = weekdayRegex.Match(category.Name);
                Regex regex = weekdayMatch.Success ? weekdayEpisodeListRegex : EpisodeListRegex;

                foreach (Match m in regex.Matches(webData))
                {
                    VideoInfo info = new VideoInfo();
                    info.Title = m.Groups["title"].Value;
                    info.ImageUrl = m.Groups["thumb"].Value;
                    info.Description = m.Groups["description"].Value;
                    // this episode ID will be used later on, so store it in the .Other property
                    info.Other = m.Groups["episode"].Value;

                    if (weekdayMatch.Success)
                    {
                        // Weekday episodes need to be looked up directly now
                        info.VideoUrl = CreateRTMPUrl(String.Format("http://cls.ctvdigital.net/cliplookup.aspx?id={0}", info.Other));
                    }

                    result.Add(info);
                }
            }

            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class FootballStreamingInfoUtil : SiteUtilBase
    {
        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            List<Category> cats = new List<Category>();
            string videoReg = @"<strong><font size=""4"" color=""#333333""><strong>(?<title>.*?)</strong></font>(&nbsp;)?\s?<font size=""2"" color=""#999999""><strong>(?<description>.*?)</strong></font><br />\s*<font color=""black""><font size=""4""><font face=""Tahoma"">KICK OFF//(?<startTime>\d\d[.]\d\d)(?<vidHtml>.*?)((<br />\s*<br />)|(<hr size=""1"" />))";
            foreach (Match match in new Regex(videoReg, RegexOptions.Singleline).Matches(GetWebData("http://www.footballstreaming.info/streams/todays-links")))
            {
                Category cat = new Category();
                cat.Name = match.Groups["title"].Value;
                cat.Description = string.Format("{0}\r\n{1}", match.Groups["description"], match.Groups["startTime"]);
                cat.Other = match.Groups["vidHtml"].Value;
                cats.Add(cat);
            }
            foreach (Category cat in cats)
                Settings.Categories.Add(cat);

            return Settings.Categories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> vids = new List<VideoInfo>();

            string channelReg = @"<strong>(?<title>.*?)\s-(?<links>.*?)</strong><br />";
            string urlReg = @"href=""(?<link>sop://[^""]*)""";

            foreach (Match channelMatch in new Regex(channelReg, RegexOptions.Singleline).Matches(category.Other as string))
            {
                int x = 1;
                string title = channelMatch.Groups["title"].Value;
                foreach (Match link in new Regex(urlReg).Matches(channelMatch.Groups["links"].Value))
                {
                    VideoInfo vid = new VideoInfo();
                    vid.Title = title;
                    if (x > 1)
                        vid.Title += string.Format(" ({0})", x);

                    vid.VideoUrl = link.Groups["link"].Value;
                    vids.Add(vid);
                    x++;
                }
            }

            return vids;
        }
    }
}

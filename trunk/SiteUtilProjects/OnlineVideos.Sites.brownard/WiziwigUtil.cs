using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class WiziwigUtil : SiteUtilBase
    {
        const string BASE_URL = "http://www.wiziwig.tv";

        public override int DiscoverDynamicCategories()
        {
            if (Settings.Categories.Count > 0)
            {
                for (int i = 0; i < Settings.Categories.Count; i++)
                    Settings.Categories[i].HasSubCategories = true;
            }
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string categoryRegex = @"<tr class="".*?"">\s*<td class=""logo"">.*?</td>\s*<td><a.*?>(?<comp>[^<]*)</a>.*?</td>\s*<td>\s*<div class=""date"".*?>(?<date>[^<]*)</div>\s*<span class=""time"".*?>(?<starttime>[^<]*)</span> -\s*<span class=""time"".*?>(?<endtime>[^<]*)</span>\s*</td>\s*<td class=""home"".*?><img class=""flag"" src=""(?<thumb>[^""]*)"".*?/>(?<hometeam>[^<]*)<img.*?/></td>\s*(<td>vs.</td>\s*<td class=""away""><img.*?>(?<awayteam>[^<]*)<img class=""flag"" src=""(?<awaythumb>[^""]*)"".*?></td>\s*)?<td class=""broadcast""><a class=""broadcast"" href=""(?<url>[^""]*)""";

            string url = (parentCategory as RssLink).Url;

            string html = GetWebData(url);

            //hack, too lazy to implement next page properly
            //Regex nextPage = new Regex(@"<a class=""paginate_pagelink"" href=""(?<nextpage>[^""]*)"">2</a></td>");
            //if (nextPage.IsMatch(html))
            //    html += GetWebData("http://myp2p.eu" + nextPage.Match(html).Groups["nextpage"].Value);

            Regex regex = new Regex(categoryRegex);
            List<Category> cats = new List<Category>();
            foreach (Match match in regex.Matches(html))
            {
                RssLink cat = new RssLink();
                string append = "";
                if (match.Groups["awayteam"].Value != "")
                    append = " v " + match.Groups["awayteam"].Value;
                cat.Name = match.Groups["hometeam"].Value + append;
                cat.Description = getTime(match.Groups["starttime"].Value + " - " + match.Groups["endtime"].Value) + "\n" + match.Groups["date"].Value + "\n" + match.Groups["comp"].Value;
                cat.Url = System.Web.HttpUtility.HtmlDecode(BASE_URL + match.Groups["url"].Value);
                cat.Thumb = BASE_URL + match.Groups["thumb"].Value;
                cat.ParentCategory = parentCategory;
                cats.Add(cat);
            }
            parentCategory.SubCategories = cats;
            parentCategory.SubCategoriesDiscovered = true;
            return cats.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string channelRegex = @"<tr class=""broadcast"">\s*<td class=""logo"".*?><img src=""(?<thumb>[^""]*)"".*?></td>\s*<td class=""stationname"">(?<name>[^<]*)</td>\s*(<td.*?>.*?</td>\s*){3}</tr>\s*(?<vidhtml><tr class=""streamrow.*?</tr>)";
            string videoRegex = @"<tr class=""streamrow (odd|even)"">\s*<td>.*?</td>\s*<td>\s*<a class=""broadcast go"" href=""(?<url>[^""]*)"".*?</td>\s*<td>(?<bitrate>[^<]*)</td>\s*<td><div class=""rating"" rel=""(?<rating>[^""]*)""";

            List<VideoInfo> vids = new List<VideoInfo>();

            //links page
            string html = GetWebData(((RssLink)category).Url);

            Regex regex = new Regex(channelRegex, RegexOptions.Singleline);

            //match the channel groups to get tile and logo for individual links
            foreach (Match match in regex.Matches(html))
            {
                string imageurl = BASE_URL + System.Web.HttpUtility.HtmlDecode(match.Groups["thumb"].Value);
                string channel = match.Groups["name"].Value;

                Regex vidregex = new Regex(videoRegex, RegexOptions.Singleline);

                //individual links
                foreach (Match vidmatch in vidregex.Matches(match.Groups["vidhtml"].Value))
                {
                    string url = vidmatch.Groups["url"].Value;
                    if (!url.StartsWith("sop://") && !url.StartsWith("http://www.hitsports.net"))
                        continue;
                    VideoInfo vid = new VideoInfo();
                    vid.ImageUrl = imageurl;
                    vid.VideoUrl = url;
                    vid.Title = channel + " - " + vidmatch.Groups["bitrate"].Value;
                    vid.Description = category.Description + "\n" + string.Format("Rating: {0}/100", vidmatch.Groups["rating"]);
                    vid.Length = getVidLength(category.Description);
                    vids.Add(vid);
                }
            }
            return vids;
        }

        public override string getUrl(VideoInfo video)
        {
            if (video.VideoUrl.StartsWith("sop://"))
                return base.getUrl(video);

            return getHitSportsUrl(video.VideoUrl);
        }

        private string getHitSportsUrl(string url)
        {
            string html = GetWebData(url);
            Match m = new Regex(@"http://hitsports.net/stream-\d+.php").Match(html);
            if (!m.Success)
                return "";
            html = GetWebData(m.Value);
            m = new Regex(@"fid='(.*?)'").Match(html);
            if (!m.Success)
                return "";

            return new MPUrlSourceFilter.RtmpUrl("rtmp://50.115.124.69/flashi")
            {
                PlayPath = m.Groups[1].Value,
                SwfUrl = "http://www.flashi.tv/player/player-licensed.swf",
                SwfVerify = true,
                PageUrl = "http://www.flashi.tv/embed.php?v=" + m.Groups[1].Value,
                Live = true
            }.ToString();
        }

        private string getVidLength(string description)
        {
            string[] olengths = description.Split('\n')[0].Split('-');
            TimeSpan x = DateTime.Parse(olengths[1].Trim()).Subtract(DateTime.Parse(olengths[0].Trim()));
            return string.Format("{0} hours {1} mins", x.Hours.ToString(), x.Minutes.ToString());
        }

        private string getTime(string time)
        {
            string[] split = time.Split('-');
            string otime = DateTime.Parse(split[0].Trim()).AddHours(-1).ToShortTimeString();
            otime = otime + " - " + DateTime.Parse(split[1].Trim()).AddHours(-1).ToShortTimeString();

            return otime;
        }
    }
}

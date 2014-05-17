using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TSEngine;

namespace OnlineVideos.Sites
{
    public class WiziwigUtil : SiteUtilBase
    {
        TSPlayer tsPlayer = null;
        const string BASE_URL = "http://www.wiziwig.tv";
        static Regex CAT_REG = new Regex(@"<tr class="".*?"">\s*<td class=""logo"">.*?</td>\s*<td><a.*?>(?<comp>[^<]*)</a>.*?</td>\s*<td>\s*<div class=""date"".*?>(?<date>[^<]*)</div>\s*<span class=""time"".*?>(?<starttime>[^<]*)</span> -\s*<span class=""time"".*?>(?<endtime>[^<]*)</span>\s*</td>\s*<td class=""home"".*?><img class=""flag"" src=""(?<thumb>[^""]*)"".*?/>(?<hometeam>[^<]*)<img.*?/></td>\s*(<td>vs.</td>\s*<td class=""away""><img.*?>(?<awayteam>[^<]*)<img class=""flag"" src=""(?<awaythumb>[^""]*)"".*?></td>\s*)?<td class=""broadcast""><a class=""broadcast"" href=""(?<url>[^""]*)""",
            RegexOptions.Compiled);
        static Regex CHANNEL_REG = new Regex(@"<tr class=""broadcast"">\s*<td class=""logo"".*?><img src=""(?<thumb>[^""]*)"".*?></td>\s*<td class=""stationname"">(?<name>[^<]*)</td>\s*(<td.*?>.*?</td>\s*){3}</tr>\s*(?<vidhtml>(<tr class=""streamrow.*?</tr>\s*)+)",
            RegexOptions.Singleline | RegexOptions.Compiled);
        static Regex VIDEO_REG = new Regex(@"<tr class=""streamrow (odd|even)"">\s*<td>.*?</td>\s*<td>\s*<a class=""broadcast go"" href=""(?<url>[^""]*)"".*?</td>\s*<td>(?<bitrate>[^<]*)</td>\s*<td><div class=""rating"" rel=""(?<rating>[^""]*)""",
            RegexOptions.Singleline | RegexOptions.Compiled);

        [Category("OnlineVideosConfiguration"), Description("Max time in ms to wait for AceStream prebuffering to complete, ensure that OnlineVideos' web request timeout is at least equal to this.")]
        int aceStreamTimeout = 20000;

        public override int DiscoverDynamicCategories()
        {
            foreach (Category cat in Settings.Categories)
                if (cat is RssLink)
                    cat.HasSubCategories = true;

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string html = GetWebData((parentCategory as RssLink).Url);List<Category> cats = new List<Category>();
            foreach (Match match in CAT_REG.Matches(html))
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
            Group group = category as Group;
            if (group != null)
                return group.Channels.Select(c => new VideoInfo()
                {
                    Title = c.StreamName,
                    ImageUrl = c.Thumb,
                    VideoUrl = c.Url
                }).ToList();

            List<VideoInfo> vids = new List<VideoInfo>();

            //links page
            string html = GetWebData(((RssLink)category).Url);            

            //match the channel groups to get tile and logo for individual links
            foreach (Match match in CHANNEL_REG.Matches(html))
            {
                string imageurl = BASE_URL + System.Web.HttpUtility.HtmlDecode(match.Groups["thumb"].Value);
                string channel = match.Groups["name"].Value;
                //individual links
                foreach (Match vidmatch in VIDEO_REG.Matches(match.Groups["vidhtml"].Value))
                {
                    string url = vidmatch.Groups["url"].Value;
                    if (!isUrlSupported(url))
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
            string url = video.VideoUrl;
            if (url.StartsWith("acestream://"))
                return getAceStreamUrl(url.Substring(12));
            else if (url.StartsWith("sop://"))
                return base.getUrl(video);
            return null;
        }

        string getAceStreamUrl(string pid)
        {
            if (tsPlayer != null)
                tsPlayer.Close();

            Log.Debug("Wiziwig: Starting acestream with PID '{0}'", pid);
            string url = null;
            try
            {
                tsPlayer = new TSPlayer();
                tsPlayer.OnMessage += (s, e) => Log.Debug("Wiziwig: " + e.Message);
                if (!tsPlayer.Connect() || !tsPlayer.WaitForReady())
                    return null;

                tsPlayer.StartPID(pid);
                url = tsPlayer.WaitForUrl(aceStreamTimeout);
            }
            catch (System.Threading.ThreadAbortException)
            {
                tsPlayer.Close();
                tsPlayer = null;
                Log.Warn("Wiziwig: Background thread was aborted by OnlineVideos, consider increasing OnlineVideos' web request timeout");
                throw;
            }

            if (string.IsNullOrEmpty(url))
            {
                tsPlayer.Close();
                tsPlayer = null;
            }
            return url;
        }

        public override void OnPlaybackEnded(VideoInfo video, string url, double percent, bool stoppedByUser)
        {
            if (tsPlayer != null)
            {
                tsPlayer.Stop();
                tsPlayer.Close();
                tsPlayer = null;
            }
        }

        bool isUrlSupported(string url)
        {
            return url.StartsWith("acestream://"); // || url.StartsWith("sop://");
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

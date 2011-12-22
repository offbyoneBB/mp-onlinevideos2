using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class ABCUtil : SiteUtilBase
    {
        private static string mainCategoriesUrl = @"http://cdn.abc.go.com/vp2/ws-supt/s/syndication/2000/rss/001/001/-1/-1/-1/-1/-1/-1";
        private static string ajaxUrlForShowId = @"http://abc.go.com/vp2/s/carousel?svc=season&showid={0}&bust=07000000_0";

        private Regex mainCategoriesRegex = new Regex(@"<item><description>.*?<link>(?<link>.*?)</link><title>(?<title>.*?)</title>",
            RegexOptions.Compiled);
        private Regex rssLinkRegex = new Regex(@"<link href=""(?<link>[^""]*)"" rel=""alternate"" type=""application/rss\+xml""",
            RegexOptions.Compiled);
        private Regex episodeListRegex = new Regex(@"<item><description>(?<description>[^<]*)</description>.*?<image>(?<image>[^<]*)</image>.*?<title>(?<title>[^<]*)</title>",
            RegexOptions.Compiled);
        private Regex thumbnailRegex = new Regex(@"http://cdn\.video\.abc\.com/abcvideo/video_fep/thumbnails/220x124/(?<episode>.*?)_220x124\.jpg",
            RegexOptions.Compiled);
        private Regex showIdRegex = new Regex(@"/(?<showId>SH.*?)/[0-9]*?/-1/-1",
            RegexOptions.Compiled);
        private Regex seasonIdRegex = new Regex(@"seasonid=""(?<seasonid>[^""]*)""",
            RegexOptions.Compiled);

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            // look for main categories
            string webData = GetWebData(mainCategoriesUrl);

            if (!string.IsNullOrEmpty(webData))
            {
                foreach (Match m in mainCategoriesRegex.Matches(webData))
                {
                    RssLink cat = new RssLink();

                    cat.Name = m.Groups["title"].Value;
                    cat.Url = m.Groups["link"].Value;
                    cat.HasSubCategories = false;

                    Settings.Categories.Add(cat);
                }
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> result = new List<VideoInfo>();

            string url = (string) ((RssLink) category).Url;
            string webData = GetWebData(url);

            if (!string.IsNullOrEmpty(webData))
            {
                // look for RSS link
                Match rssLinkMatch = rssLinkRegex.Match(webData);
                if (rssLinkMatch.Success)
                {
                    string rssLink = rssLinkMatch.Groups["link"].Value;

                    int currentYear = DateTime.Now.Year;

                    // RSS link ends with current year and /-1/-1, so we must find the real RSS link
                    if (rssLink.EndsWith(currentYear + @"/-1/-1"))
                    {
                        // first find the show id
                        Match showIdMatch = showIdRegex.Match(rssLink);

                        if (showIdMatch.Success)
                        {
                            string showId = showIdMatch.Groups["showId"].Value;

                            // make ajax call to URL which includes the show ID
                            webData = GetWebData(String.Format(ajaxUrlForShowId, showId));

                            Match seasonIdMatch = seasonIdRegex.Match(webData);

                            if (seasonIdMatch.Success)
                            {
                                // extract season id from response
                                String seasonId = seasonIdMatch.Groups["seasonid"].Value;

                                // override rssLink with 
                                rssLink = Regex.Replace(rssLink, "/" + currentYear + "/", "/" + seasonId + "/");
                            }
                        }
                    }

                    // follow the RSS link
                    webData = GetWebData(rssLink);

                    if (!string.IsNullOrEmpty(webData))
                    {
                        // look for list of episodes
                        foreach (Match m in episodeListRegex.Matches(webData))
                        {
                            VideoInfo info = new VideoInfo();
                            info.Title = m.Groups["title"].Value;
                            info.ImageUrl = m.Groups["image"].Value;
                            info.Description = m.Groups["description"].Value;
                            info.VideoUrl = info.ImageUrl;

                            result.Add(info);
                        }
                    }
                }
            }

            return result;
        }

        public override String getUrl(VideoInfo video)
        {
            string result = video.VideoUrl;

            // look for thumbnail
            Match episodeMatch = thumbnailRegex.Match(result);

            if (episodeMatch.Success)
            {
                string episode = episodeMatch.Groups["episode"].Value;
                Log.Debug(@"episode found: {0}", episode);

                string rtmpUrl = @"rtmp://abcondemandfs.fplive.net:1935/abcondemand";
                string playPath = String.Format(@"mp4:/abcvideo/video_fep/mov/{0}_768x432_700.mov", episode.ToLower());
                Log.Debug(@"playPath: {0}", playPath);

                result = new MPUrlSourceFilter.RtmpUrl(rtmpUrl) { PlayPath = playPath }.ToString();
                Log.Debug(@"Resulting MPUrl: {0}", result);
            }
            else
            {
                Log.Warn(@"could not extract RTMP Url from Thumbnail");
            }

            return result;
        }
    }
}

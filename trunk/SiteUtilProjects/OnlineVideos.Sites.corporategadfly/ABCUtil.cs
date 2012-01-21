using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class ABCUtil : SiteUtilBase
    {
        private static string mainCategoriesUrl = @"http://cdn.abc.go.com/vp2/ws-supt/s/syndication/2000/rss/001/001/-1/-1/-1/-1/-1/-1";
        private static string ajaxUrlForShowId = @"http://abc.go.com/vp2/s/carousel?service=seasons&parser=VP2_Data_Parser_Seasons&showid={0}&view=season";

        private Regex mainCategoriesRegex = new Regex(@"<item><description>.*?<image>(?<image>.*?)</image><link>(?<link>.*?)</link><title>(?<title>.*?)</title>",
            RegexOptions.Compiled);
        private Regex rssLinkRegex = new Regex(@"<link href=""(?<link>[^""]*)"" rel=""alternate"" type=""application/rss\+xml""",
            RegexOptions.Compiled);
        private Regex episodeListRegex = new Regex(@"<item><description>(?<description>[^<]*)</description>.*?<image>(?<image>[^<]*)</image><link>(?<link>[^<]*)</link>.*?<title>(?<title>[^<]*)</title>",
            RegexOptions.Compiled);
        private Regex thumbnailRegex = new Regex(@"http://cdn\.video\.abc\.com/abcvideo/video_fep/thumbnails/220x124/(?<episode>.*?)_220x124\.jpg",
            RegexOptions.Compiled);
        private Regex showIdRegex = new Regex(@"/(?<showId>SH\d+)",
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
                    cat.Thumb = m.Groups["image"].Value;
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

            // determine show ID from the url
            string showId = string.Empty;
            Match showIdMatch = showIdRegex.Match(url);

            if (showIdMatch.Success)
            {
                showId = showIdMatch.Groups["showId"].Value;
            }

            Log.Debug(@"Retrieved show ID: {0} from url: {1}", showId, url);

            if (!string.IsNullOrEmpty(webData))
            {
                // look for RSS link
                Match rssLinkMatch = rssLinkRegex.Match(webData);
                if (rssLinkMatch.Success)
                {
                    string rssLink = rssLinkMatch.Groups["link"].Value;

                    int currentYear = DateTime.Now.Year;

                    // RSS link ends with
                    //      current year and /-1/-1
                    //      or
                    //      -1/-1/-1
                    // so we must find the real RSS link by finding season ID
                    if (rssLink.EndsWith(currentYear + @"/-1/-1") || rssLink.EndsWith(@"-1/-1/-1"))
                    {
                        // make ajax call to URL which includes the show ID
                        webData = GetWebData(String.Format(ajaxUrlForShowId, showId));

                        Match seasonIdMatch = seasonIdRegex.Match(webData);
                        if (seasonIdMatch.Success)
                        {
                            // extract season id from response
                            String seasonId = seasonIdMatch.Groups["seasonid"].Value;

                            // override rssLink
                            if (rssLink.EndsWith(currentYear + @"/-1/-1"))
                            {
                                // replace currentYear with season ID
                                rssLink = Regex.Replace(rssLink, "/" + currentYear + "/", "/" + seasonId + "/");
                            }
                            else if (rssLink.EndsWith(@"-1/-1/-1"))
                            {
                                // replace first occurence of -1 with season ID
                                rssLink = Regex.Replace(rssLink, "/-1/-1/-1", "/" + seasonId + "/-1/-1");
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
                            info.Other = m.Groups["link"].Value;
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
            string rtmpUrl = @"rtmp://abcondemandfs.fplive.net:1935/abcondemand";

            string result = video.VideoUrl;

            // look for thumbnail
            Match episodeMatch = thumbnailRegex.Match(result);

            if (episodeMatch.Success)
            {
                string episode = episodeMatch.Groups["episode"].Value;
                Log.Debug(@"episode found: {0}", episode);

                // convert thumbnail URL to the playPath
                string playPath = String.Format(@"mp4:/abcvideo/video_fep/mov/{0}_768x432_700.mov", episode.ToLower());
                Log.Debug(@"playPath: {0}", playPath);

                result = new MPUrlSourceFilter.RtmpUrl(rtmpUrl) { PlayPath = playPath }.ToString();
                Log.Debug(@"Resulting MPUrl: {0}", result);
            }
            else
            {
                // start with empty result
                result = string.Empty;

                // find the conviva content
                Log.Warn(@"Could not extract RTMP Url from Thumbnail Url: {0}. Reverting to conviva content", result);

                video.PlaybackOptions = new Dictionary<string, string>();
                // keep track of bitrates and URLs
                Dictionary<int, string> urlsDictionary = new Dictionary<int, string>();

                // find the conviva content
                // split on /
                string[] parts = ((string) video.Other).Split('/');
                string convivaId = parts[parts.Length - 2];     // 2nd last part
                string convivaUrl = String.Format(@"http://cdn.abc.go.com/vp2/ws/s/contents/2003/utils/video/mov/13/9024/{0}/432?v=06000007_3", convivaId);

                string webData = GetWebData(convivaUrl);
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(webData);

                foreach (XmlNode node in xml.SelectNodes("//videos/video"))
                {
                    string playPath = node.Attributes["src"].Value;

                    // do not bother unless src is non-empty
                    if (string.IsNullOrEmpty(playPath)) continue;

                    Log.Debug(@"Found video playPath: {0}", playPath);
                    int bitrate = int.Parse(node.Attributes["bitrate"].Value);

                    urlsDictionary.Add(bitrate, new MPUrlSourceFilter.RtmpUrl(rtmpUrl) { PlayPath = playPath }.ToString());
                }

                // sort the URLs ascending by bitrate
                foreach (var item in urlsDictionary.OrderBy(u => u.Key))
                {
                    video.PlaybackOptions.Add(string.Format("{0} kbps", item.Key), item.Value);
                    // return last URL as the default (will be the highest bitrate)
                    result = item.Value;
                }
            }

            return result;
        }
    }
}

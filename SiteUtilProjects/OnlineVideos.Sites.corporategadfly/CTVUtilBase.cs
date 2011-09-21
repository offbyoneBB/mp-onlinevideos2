using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public abstract class CTVUtilBase : SiteUtilBase
    {
        private Regex _episodeListRegex = new Regex(@"<dd(\sid=""[^""]*"")?\sclass=""Thumbnail""><a\shref=""javascript:Interface\.PlayEpisode\((?<episode>[^,]*),\strue\s\)""\stitle=""(?<title>[^""]*)""><img\ssrc=""(?<thumb>[^""]*)""\s/><span></span></a></dd>.*?<dd class=""Description"">(?<description>[^<]*)</dd>",
            RegexOptions.Compiled | RegexOptions.Singleline);

        public abstract string BaseUrl { get; }

        public virtual int StartingPanelLevel { get { return 3; } }

        public virtual string VideoLibraryParameter { get { return @"SeasonId"; } }

        public virtual string Swf { get { return @"http://watch.ctv.ca/Flash/player.swf?themeURL=http://watch.ctv.ca/themes/CTV/player/theme.aspx"; } }

        public virtual Regex EpisodeListRegex { get { return _episodeListRegex; } }

        public virtual Boolean IsMainCategoryContainsSubCategories { get { return true; } }

        public virtual Boolean IsVideoListStartsFromStartingPanelLevel { get { return true; } }

        public static string videoLibraryUri = @"/AJAX/VideoLibraryWithFrame.aspx";
        public static Regex mainCategoriesRegex = new Regex(@"<li[^>]*>\s*<a\sid=""(?<id>[^""]*)""\sonclick=""[^""]*""\shref=""(?<url>[^""]*)""\stitle=""[^""]*"">\s*(?<title>[^<]*)<span></span>\s*</a>\s*</li>",
            RegexOptions.Compiled);

        private Regex subcategoriesRegex = new Regex(@"<li[^>]*>\s+<a\s+id=""(?<id>[^""]*)""\s+onclick=""return\s+Interface\.GetChildPanel\('Season'[^""]*""\s+href=""(?<url>[^""]*)""\s+title=""[^""]*"">\s*(?<title>[^<]*)<span></span>\s*</a>\s*</li>",
            RegexOptions.Compiled);
        private Regex clipListRegex = new Regex(@"<dt><a\shref=""[^#]*#clip(?<clip>[^""]*)""\sonclick=""return\sPlaylist\.GetInstance.*?</a></dt>",
            RegexOptions.Compiled);
        private Regex clipUrlRegex = new Regex(@"Video\.Load\({url:'(?<url>[^']*)'.*?",
            RegexOptions.Compiled);
        // capture group <params> is optional
        private Regex rtmpUrlRegex = new Regex(@"rtmpe://(?<host>[^/]*)/ondemand/(?<file>[^?]*)\??(?<params>.*)?",
            RegexOptions.Compiled);
        private Regex rtmpUrlSecondaryRegex = new Regex(@"rtmpe://(?<host>[^/]*)/(?<app>[^/]*)/(?<file>[^?]*)\??(?<params>.*)",
            RegexOptions.Compiled);

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            string webData = GetWebData(BaseUrl + videoLibraryUri);

            if (!string.IsNullOrEmpty(webData))
            {
                foreach (Match m in mainCategoriesRegex.Matches(webData))
                {
                    RssLink cat = new RssLink();

                    cat.Name = m.Groups["title"].Value;
                    cat.Url = m.Groups["url"].Value;
                    cat.HasSubCategories = IsMainCategoryContainsSubCategories;

                    Settings.Categories.Add(cat);
                }
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();

            RssLink parentRssLink = (RssLink) parentCategory;
            string webData = GetWebData(parentRssLink.Url);

            if (!string.IsNullOrEmpty(webData))
            {
                foreach (Match m in subcategoriesRegex.Matches(webData))
                {
                    RssLink cat = new RssLink();

                    cat.ParentCategory = parentCategory;
                    cat.Name = m.Groups["title"].Value;
                    cat.Url = m.Groups["url"].Value;
                    // this id will be used later on, so store it in the .Other property
                    cat.Other = m.Groups["id"].Value;
                    cat.HasSubCategories = false;

                    parentCategory.SubCategories.Add(cat);
                }
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> result = new List<VideoInfo>();

            string url;

            if (IsVideoListStartsFromStartingPanelLevel)
            {
                string parentId = (string)((RssLink)category).Other;

                // StartingPanelLevel shows list of episodes
                url = BaseUrl
                    + @"/AJAX/VideoLibraryContents.aspx?GetChildOnly=true&PanelID="
                    + StartingPanelLevel
                    + @"&" + VideoLibraryParameter + @"="
                    + parentId;
            }
            else
            {
                url = (string) ((RssLink) category).Url;
            }

            string webData = GetWebData(url);

            if (!string.IsNullOrEmpty(webData))
            {
                foreach (Match m in EpisodeListRegex.Matches(webData))
                {
                    VideoInfo info = new VideoInfo();
                    info.Title = m.Groups["title"].Value;
                    info.ImageUrl = m.Groups["thumb"].Value;
                    info.Description = m.Groups["description"].Value;
                    // this episode ID will be used later on, so store it in the .Other property
                    info.Other = m.Groups["episode"].Value;

                    result.Add(info);
                }
            }

            return result;
        }

        public override List<string> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            List<string> result = new List<string>();

            if (StartingPanelLevel.Equals(2))
            {
                video.VideoUrl = String.Format("http://cls.ctvdigital.net/cliplookup.aspx?id={0}", video.Other);
                result.Add(CreateRTMPUrl(video));
            }
            else
            {
                // Level 4 shows clips
                string webData = GetWebData(BaseUrl
                    + @"/AJAX/VideoLibraryContents.aspx?GetChildOnly=true&PanelID="
                    + (StartingPanelLevel + 1)
                    + @"&EpisodeID="
                    + video.Other);

                if (!string.IsNullOrEmpty(webData))
                {
                    foreach (Match m in clipListRegex.Matches(webData))
                    {
                        string clipId = m.Groups["clip"].Value;
                        // this is the URL which will eventually reveal the rtmpe locations
                        result.Add(String.Format("http://cls.ctvdigital.net/cliplookup.aspx?id={0}", clipId));
                    }
                }
                Log.Debug(@"Found {0} episodes at level {1}", result.Count, StartingPanelLevel + 1);

                if (result.Count.Equals(1))
                {
                    // if there was only one result, we should translate it to the RTMP URL now
                    video.VideoUrl = result[0];
                    result = new List<string>();
                    result.Add(CreateRTMPUrl(video));
                }
            }

            return result;
        }

        public override string getPlaylistItemUrl(VideoInfo clonedVideoInfo, string chosenPlaybackOption, bool inPlaylist = false)
        {
            return CreateRTMPUrl(clonedVideoInfo);
        }

        private string CreateRTMPUrl(VideoInfo clonedVideoInfo)
        {
            Log.Debug(@"Video URL (before): {0}", clonedVideoInfo.VideoUrl);

            string result = clonedVideoInfo.VideoUrl;

            // must specify referer (or we will get 403 Forbidden from cls.ctvdigital.net)
            string webData = GetWebData(clonedVideoInfo.VideoUrl, null, BaseUrl);

            if (!string.IsNullOrEmpty(webData))
            {
                Match urlMatch = clipUrlRegex.Match(webData);
                if (urlMatch.Success)
                {
                    string rtmpFromScraper = urlMatch.Groups["url"].Value;

                    Log.Debug("RTMP URL found: {0}", rtmpFromScraper);

                    Match m = rtmpUrlRegex.Match(rtmpFromScraper);
                    if (m.Success)
                    {
                        string rtmpUrl = String.Format(@"rtmpe://{0}/ondemand?{1}", m.Groups["host"], m.Groups["params"]);
                        string playPath = String.Format(@"mp4:{0}", m.Groups["file"]);

                        string url = String.Format(@"http://127.0.0.1/stream.flv?rtmpurl={0}&playpath={1}&swfVfy={2}",
                            HttpUtility.UrlEncode(rtmpUrl),
                            HttpUtility.UrlEncode(playPath),
                            HttpUtility.UrlEncode(Swf));

                        Log.Debug(@"RTMP URL (after): {0}", url);
                        result = ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance, url);
                    }
                    else
                    {
                        
                        m = rtmpUrlSecondaryRegex.Match(rtmpFromScraper);

                        if (m.Success)
                        {
                            string rtmpUrl = String.Format(@"rtmpe://{0}/{1}?{2}", m.Groups["host"], m.Groups["app"], m.Groups["params"]);
                            string playPath = String.Format(@"mp4:{0}?{1}", m.Groups["file"], m.Groups["params"]);

                            string url = String.Format(@"http://127.0.0.1/stream.flv?rtmpurl={0}&playpath={1}&swfUrl={2}",
                                HttpUtility.UrlEncode(rtmpUrl),
                                HttpUtility.UrlEncode(playPath),
                                HttpUtility.UrlEncode(Swf));

                            Log.Debug(@"RTMP URL Secondary Option (after): {0}", url);
                            result = ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance, url);
                        }
                        else
                        {
                            Log.Error(@"Unknown RTMP URL: {0}", rtmpFromScraper);
                        }
                    }
                }
            }
            return result;
        }
    }
}

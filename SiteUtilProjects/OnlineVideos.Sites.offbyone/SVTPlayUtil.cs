using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using RssToolkit.Rss;
using System.Web;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// SVTPlayUtil gets streams from www.svtplay.se. It gets its categories 
    /// dynamicly and then uses rssfeeds to get the videos for each category
    /// </summary>
    public class SVTPlayUtil : SiteUtilBase
    {
        [Category("OnlineVideosConfiguration")]
        protected string kategorierRegEx;
        [Category("OnlineVideosConfiguration")]
        protected string kategorierSubRegEx;
        [Category("OnlineVideosConfiguration")]
        protected string kategorierSubPagingRegEx;
        [Category("OnlineVideosConfiguration")]
        protected string alfabetiskRegEx;
        [Category("OnlineVideosConfiguration")]
        protected string dynamicCategoryUrlFormatString;
        [Category("OnlineVideosConfiguration"), Description("Url used for prepending relative links.")]
        protected string baseUrl;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to find the current start index and total amount of items and page size. Groups should be named 'max', 'start' and 'pageSize'.")]
        protected string nextPageRegEx;

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Preferred Kbps"), Description("Chose your desired bitrate that will be preselected.")]
        protected int preferredKbps = 1400;

        protected uint currentPageSize = 20;
        protected uint currentStartIndex = 1;
        protected uint currentMaxVideos = 0;
        protected string currentCategoryUrl = "";

        protected Regex regEx_Alfabetisk, regEx_Kategorier, regEx_SubKategorier, regEx_SubPagingKategorier, regEx_NextPage;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            RegexOptions defaultRegexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture;

            if (!string.IsNullOrEmpty(alfabetiskRegEx)) regEx_Alfabetisk = new Regex(alfabetiskRegEx, defaultRegexOptions);
            if (!string.IsNullOrEmpty(kategorierRegEx)) regEx_Kategorier = new Regex(kategorierRegEx, defaultRegexOptions);
            if (!string.IsNullOrEmpty(kategorierSubRegEx)) regEx_SubKategorier = new Regex(kategorierSubRegEx, defaultRegexOptions);
            if (!string.IsNullOrEmpty(kategorierSubPagingRegEx)) regEx_SubPagingKategorier = new Regex(kategorierSubPagingRegEx, defaultRegexOptions);
            if (!string.IsNullOrEmpty(nextPageRegEx)) regEx_NextPage = new Regex(nextPageRegEx, defaultRegexOptions);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.ToList().ForEach(c => c.HasSubCategories = true);
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            currentCategoryUrl = (category as RssLink).Url;
            currentPageSize = 20;
            currentStartIndex = 1;
            currentMaxVideos = 0;
            return VideosForCurrentCategory();
        }

        private List<VideoInfo> VideosForCurrentCategory()
        {
            List<VideoInfo> videoList = new List<VideoInfo>();
            string data = GetWebData(currentCategoryUrl + (currentStartIndex > 1 ? "?start=" + currentStartIndex : ""));
            if (data.Length > 0)
            {
                Match m = regEx_NextPage.Match(data);
                if (m.Success)
                {
                    currentPageSize = uint.Parse(m.Groups["pageSize"].Value);
                    currentStartIndex = uint.Parse(m.Groups["start"].Value);
                    currentMaxVideos = uint.Parse(m.Groups["max"].Value);
                }
                foreach (RssItem rssItem in RssToolkit.Rss.RssDocument.Load(data).Channel.Items)
                {
                    VideoInfo video = VideoInfo.FromRssItem(rssItem, false, new Predicate<string>(isPossibleVideo));
                    // only if a video url was set, add this Video to the list
                    if (!string.IsNullOrEmpty(video.VideoUrl))
                    {
                        if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 1)
                        {
                            video.Other = "PlaybackOptions://\n" + Utils.DictionaryToString(video.PlaybackOptions);
                        }
                        videoList.Add(video);
                    }
                }
            }
            return videoList;
        }

        public override bool HasNextPage
        {
            get
            {
                return currentStartIndex + currentPageSize < currentMaxVideos;
            }
            protected set
            {
                base.HasNextPage = value;
            }
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            currentStartIndex += currentPageSize;
            return VideosForCurrentCategory();
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string data = GetWebData((parentCategory as RssLink).Url);
            if (!string.IsNullOrEmpty(data))
            {
                parentCategory.SubCategories = new List<Category>();
                if (parentCategory.ParentCategory == null && (parentCategory as RssLink).Url.Contains("alfabetisk"))
                {
                    Match m = regEx_Alfabetisk.Match(data);
                    while (m.Success)
                    {
                        RssLink cat = new RssLink();
                        cat.Url = m.Groups["url"].Value;
                        cat.Url = string.Format(dynamicCategoryUrlFormatString, cat.Url);
                        cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim().Replace('\n', ' '));
                        cat.Thumb = m.Groups["thumb"].Value;
                        if (!String.IsNullOrEmpty(cat.Thumb) && !Uri.IsWellFormedUriString(cat.Thumb, System.UriKind.Absolute)) cat.Thumb = new Uri(new Uri(baseUrl), cat.Thumb).AbsoluteUri;

                        string id = cat.Url.Substring(cat.Url.LastIndexOf('/') + 1);
                        string path = "http://material.svtplay.se/content/2/c6/" + id.Substring(0, 2) + "/" + id.Substring(2, 2) + "/" + id.Substring(4, 2);
                        cat.Thumb = cat.Thumb.Substring(cat.Thumb.LastIndexOf("/"));
                        cat.Thumb =
                            path + cat.Thumb + "_a.jpg" + "|" +
                            path + cat.Thumb + "4.jpg" + "|" +
                            path + cat.Thumb.Replace("_", "").Replace("-", "") + "_a.jpg" + "|" +
                            path + "/a_" + cat.Thumb.Substring(1).Replace("_", "") + "_168.jpg";

                        cat.Description = m.Groups["description"].Value;
                        cat.ParentCategory = parentCategory;
                        parentCategory.SubCategories.Add(cat);
                        m = m.NextMatch();
                    }
                }
                else if (parentCategory.ParentCategory == null && (parentCategory as RssLink).Url.Contains("kategorier"))
                {
                    Match m = regEx_Kategorier.Match(data);
                    while (m.Success)
                    {
                        RssLink cat = new RssLink();
                        cat.Url = m.Groups["url"].Value;
                        if (!string.IsNullOrEmpty(cat.Url) && !Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(baseUrl), cat.Url).AbsoluteUri;
                        cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim().Replace('\n', ' '));
                        cat.Thumb = m.Groups["thumb"].Value;
                        if (!string.IsNullOrEmpty(cat.Thumb) && !Uri.IsWellFormedUriString(cat.Thumb, System.UriKind.Absolute)) cat.Thumb = new Uri(new Uri(baseUrl), cat.Thumb).AbsoluteUri;
                        cat.HasSubCategories = true;
                        cat.ParentCategory = parentCategory;
                        parentCategory.SubCategories.Add(cat);
                        m = m.NextMatch();
                    }
                }
                else
                {
                    Match m = regEx_SubKategorier.Match(data);
                    while (m.Success)
                    {
                        MatchToCategory(m, parentCategory);
                        m = m.NextMatch();
                    }

                    // categories are spread among pages which are retieved using ajax requests, get them all parallel
                    List<string> additionalPageUrls = new List<string>();
                    Match mPage = regEx_SubPagingKategorier.Match(data);
                    while (mPage.Success)
                    {
                        additionalPageUrls.Add(mPage.Groups["url"].Value);
                        mPage = mPage.NextMatch();
                    }
                    if (additionalPageUrls.Count > 0)
                    {
                        System.Threading.ManualResetEvent[] threadWaitHandles = new System.Threading.ManualResetEvent[additionalPageUrls.Count];
                        for (int i = 0; i < additionalPageUrls.Count; i++)
                        {
                            threadWaitHandles[i] = new System.Threading.ManualResetEvent(false);
                            new System.Threading.Thread(delegate(object o)
                                {
                                    int o_i = (int)o;
                                    string addDataPage = GetWebData((parentCategory as RssLink).Url + "?ajax,pb/" + additionalPageUrls[o_i]);
                                    Match addM = regEx_SubKategorier.Match(addDataPage);
                                    if (o_i > 0) System.Threading.WaitHandle.WaitAny(new System.Threading.ManualResetEvent[] { threadWaitHandles[o_i - 1] });
                                    while (addM.Success)
                                    {
                                        MatchToCategory(addM, parentCategory);
                                        addM = addM.NextMatch();
                                    }
                                    threadWaitHandles[o_i].Set();
                                }) { IsBackground = true }.Start(i);
                        }
                        System.Threading.WaitHandle.WaitAll(threadWaitHandles);
                    }
                }

                parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0; // only set to true if actually discovered (forces re-discovery until found)

                return parentCategory.SubCategories.Count; // return the number of discovered categories
            }
            return 0;
        }

        void MatchToCategory(Match m, Category parentCategory)
        {
            RssLink cat = new RssLink();
            cat.Url = m.Groups["url"].Value;
            cat.Url = string.Format(dynamicCategoryUrlFormatString, cat.Url);
            cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim().Replace('\n', ' '));
            cat.Thumb = m.Groups["thumb"].Value;
            if (!string.IsNullOrEmpty(cat.Thumb) && !Uri.IsWellFormedUriString(cat.Thumb, System.UriKind.Absolute)) cat.Thumb = new Uri(new Uri(baseUrl), cat.Thumb).AbsoluteUri;
            parentCategory.SubCategories.Add(cat);
            cat.ParentCategory = parentCategory;
            cat.Description = m.Groups["description"].Value;
        }

        public override List<String> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            List<String> result = new List<string>();

            // deserialize PlaybackOptions from Other object
            if (video.Other is string && (video.Other as string).StartsWith("PlaybackOptions://"))
                video.PlaybackOptions = Utils.DictionaryFromString((video.Other as string).Substring("PlaybackOptions://".Length));
            
            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
            {
                // sort by bitrate descending
                string[] keys = new string[video.PlaybackOptions.Count];
                video.PlaybackOptions.Keys.CopyTo(keys, 0);
                var keysList = keys.ToList();
                keysList.Sort((s1, s2) => 
                {
                    int kbps1 = 0;
                    int kbps2 = 0;
                    int.TryParse(Regex.Match(s1, @"\((?<kbps>\d+)\skbps\)").Groups["kbps"].Value, out kbps1);
                    int.TryParse(Regex.Match(s2, @"\((?<kbps>\d+)\skbps\)").Groups["kbps"].Value, out kbps2);
                    return kbps2.CompareTo(kbps1);
                });
                Dictionary<string, string> sortedPlaybackoptions = new Dictionary<string, string>();
                string bestkbpsMatch = "";
                foreach (string key in keysList)
                {
                    // fix rtmp links
                    sortedPlaybackoptions.Add(key, video.PlaybackOptions[key].StartsWith("rtmp") ? video.PlaybackOptions[key].Replace("_definst_", "?slist=") : video.PlaybackOptions[key]);
                    // select the one that is equal or the first below the configured bitrate
                    int kbps = 0;
                    int.TryParse(Regex.Match(key, @"\((?<kbps>\d+)\skbps\)").Groups["kbps"].Value, out kbps);
                    if (string.IsNullOrEmpty(bestkbpsMatch) && kbps <= preferredKbps) bestkbpsMatch = sortedPlaybackoptions[key];
                }
                video.PlaybackOptions = sortedPlaybackoptions;
                if (!string.IsNullOrEmpty(bestkbpsMatch)) result.Add(bestkbpsMatch);
                else result.Add(video.PlaybackOptions.First().Value);
                if (inPlaylist) video.PlaybackOptions = null;
            }

            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.IO;
using System.Collections.ObjectModel;

namespace OnlineVideos.Sites.georgius
{
    public sealed class CsfdCzUtil : SiteUtilBase
    {
        private class CsfdCzVideo
        {
            public CsfdCzVideo()
            {
                this.Section = String.Empty;
                this.Url = String.Empty;
            }

            public String Section { get; set; }

            public String Url { get; set; }
        }

        private class CsfdCzVideoCollection : KeyedCollection<String, CsfdCzVideo>
        {
            protected override string GetKeyForItem(CsfdCzVideo item)
            {
                return item.Url;
            }
        }

        #region Private fields

        private static String baseUrl = "http://www.csfd.cz/videa/";

        private static String dynamicCategoryStart = @"<select name=""filter""";
        private static String dynamicCategoryEnd = @"<noscript>";
        private static String dynamicCategoryRegex = @"(<option value=""(?<dynamicCategoryUrl>[^""]+)"" selected=""selected"">(?<dynamicCategoryTitle>[^<]+))|(<option value=""(?<dynamicCategoryUrl>[^""]+)"">(?<dynamicCategoryTitle>[^<]+))";

        private static String dynamicCategoryUrlFormat = "http://www.csfd.cz/videa/filtr-{0}";

        private static String showsBlockStart = @"<ul class=""ui-image-list"">";
        private static String showsBlockEnd = @"<div class=""footer"">";

        private static String showsNextPageRegex = @"<a class=""button"" href=""(?<nextPageUrl>[^""]+)"" rel=""nofollow"">starší &gt;</a>";
        
        private static String showBlockStart = @"<li>";
        private static String showBlockEnd = @"</li>";

        private static String showThumbUrl = @"<img src=""(?<showThumbUrl>[^""]+)";
        private static String showUrl = @"<a href=""(?<showUrl>[^""]+)"" class=""[^""]*"">(?<showTitle>[^<]+)";

        //private static String showEpisodesBlockStart = @"Vyber video:";
        //private static String showEpisodesBlockEnd = @"<input";

        //private static String showEpisodeBlockStart = @"<option";
        //private static String showEpisodeBlockEnd = @"</option";

        //private static String showEpisodeUrlAndTitleRegex = @"<option value=""(?<showUrl>[^""]+)"">(?<showTitle>[^<]+)";

        private static String showEpisodesUrlBlockStart = @"<h2 class=""header"">";
        private static String showEpisodesUrlBlockEnd = @"</div>";

        private static String showEpisodesUrlCategoryNameStart = @"<span class=""selected"">";
        private static String showEpisodesUrlCategoryNameEnd = @"<";

        private static String showEpisodesUrlCategoryUrlRegex = @"<a href=""(?<showEpisodesUrlCategoryUrl>[^""]*)"">(?<showEpisodesUrlCategoryName>[^<]*)";

        private static String videoUrlBlockStart = @"new CSFD.VideoPlayer";
        private static String videoUrlBlockEnd = @"player.create()";
        private static String videoUrlRegex = @"""src"":""(?<videoUrl>[^""]+)";
        private static String subtitleUrlRegex = @"<track src=""/subtitles-proxy/\?url=(?<subtitleUrl>[^""]+)";

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        #endregion

        #region Constructors

        public CsfdCzUtil()
            : base()
        {
        }

        #endregion

        #region Methods

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
        }

        public override int DiscoverDynamicCategories()
        {
            int dynamicCategoriesCount = 0;
            String baseWebData = SiteUtilBase.GetWebData(CsfdCzUtil.baseUrl, null, null, null, true);

            int startIndex = baseWebData.IndexOf(CsfdCzUtil.dynamicCategoryStart);
            if (startIndex >= 0)
            {
                int endIndex = baseWebData.IndexOf(CsfdCzUtil.dynamicCategoryEnd, startIndex);
                if (endIndex >= 0)
                {
                    baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                    Match match = Regex.Match(baseWebData, CsfdCzUtil.dynamicCategoryRegex);
                    while (match.Success)
                    {
                        String dynamicCategoryUrl = match.Groups["dynamicCategoryUrl"].Value;
                        String dynamicCategoryTitle = match.Groups["dynamicCategoryTitle"].Value;

                        this.Settings.Categories.Add(
                            new RssLink()
                            {
                                Name = dynamicCategoryTitle,
                                HasSubCategories = true,
                                Url = String.Format(CsfdCzUtil.dynamicCategoryUrlFormat, dynamicCategoryUrl)
                            });

                        dynamicCategoriesCount++;
                        match = match.NextMatch();
                    }
                }
            }

            this.Settings.DynamicCategoriesDiscovered = true;
            return dynamicCategoriesCount;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            int showsCount = 0;
            String url = ((RssLink)parentCategory).Url;
            if (parentCategory.ParentCategory != null)
            {
                parentCategory = parentCategory.ParentCategory;
                // last category is next category, remove it
                parentCategory.SubCategories.RemoveAt(parentCategory.SubCategories.Count - 1);
            }
            if (parentCategory.SubCategories == null)
            {
                parentCategory.SubCategories = new List<Category>();
            }
            RssLink category = (RssLink)parentCategory;

            Hashtable titles = new Hashtable();
            foreach (var cat in category.SubCategories)
            {
                titles.Add(cat.Name, null);
            }

            String baseWebData = SiteUtilBase.GetWebData(url, null, null, null, true);

            int startIndex = baseWebData.IndexOf(CsfdCzUtil.showsBlockStart);
            if (startIndex >= 0)
            {
                int endIndex = baseWebData.IndexOf(CsfdCzUtil.showsBlockEnd, startIndex);
                if (endIndex >= 0)
                {
                    baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                    while (true)
                    {
                        startIndex = baseWebData.IndexOf(CsfdCzUtil.showBlockStart);
                        if (startIndex >= 0)
                        {
                            endIndex = baseWebData.IndexOf(CsfdCzUtil.showBlockEnd, startIndex);
                            if (endIndex >= 0)
                            {
                                String showData = baseWebData.Substring(startIndex, endIndex - startIndex);

                                String showThumbUrl = String.Empty;
                                String showUrl = String.Empty;
                                String showTitle = String.Empty;

                                Match match = Regex.Match(showData, CsfdCzUtil.showThumbUrl);
                                if (match.Success)
                                {
                                    showThumbUrl = Utils.FormatAbsoluteUrl(match.Groups["showThumbUrl"].Value, url);
                                }

                                match = Regex.Match(showData, CsfdCzUtil.showUrl);
                                if (match.Success)
                                {
                                    showUrl = Utils.FormatAbsoluteUrl("videa", Utils.FormatAbsoluteUrl(match.Groups["showUrl"].Value, url));
                                    showTitle = OnlineVideos.Utils.PlainTextFromHtml(match.Groups["showTitle"].Value);
                                }

                                if (!String.IsNullOrEmpty(showUrl))
                                {
                                    if (!titles.ContainsKey(showTitle))
                                    {
                                        category.SubCategories.Add(new RssLink()
                                        {
                                            Name = showTitle,
                                            Thumb = showThumbUrl,
                                            Url = showUrl,
                                            HasSubCategories = false
                                        });
                                        showsCount++;
                                        titles.Add(showTitle, null);
                                    }
                                }

                                baseWebData = baseWebData.Substring(endIndex);
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    Match nextPageMatch = Regex.Match(baseWebData, CsfdCzUtil.showsNextPageRegex);
                    if (nextPageMatch.Success)
                    {
                        String nextPageUrl = Utils.FormatAbsoluteUrl(nextPageMatch.Groups["nextPageUrl"].Value, url);
                        parentCategory.SubCategories.Add(new NextPageCategory() { Url = nextPageUrl, ParentCategory = parentCategory });
                        showsCount++;
                    }
                }
            }

            if (showsCount > 0)
            {
                parentCategory.SubCategoriesDiscovered = true;
            }

            return showsCount;
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            return this.DiscoverSubCategories(category);
        }

        private List<VideoInfo> GetPageVideos(String pageUrl)
        {
            List<VideoInfo> pageVideos = new List<VideoInfo>();

            if (!String.IsNullOrEmpty(pageUrl))
            {
                this.nextPageUrl = String.Empty;
                String baseWebData = CsfdCzUtil.GetWebData(pageUrl, null, null, null, true);

                int startIndex = baseWebData.IndexOf(CsfdCzUtil.showEpisodesUrlBlockStart);
                if (startIndex >= 0)
                {
                    int endIndex = baseWebData.IndexOf(CsfdCzUtil.showEpisodesUrlBlockEnd, startIndex);
                    if (endIndex >= 0)
                    {
                        baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                        startIndex = baseWebData.IndexOf(CsfdCzUtil.showEpisodesUrlCategoryNameStart);
                        if (startIndex >= 0)
                        {
                            endIndex = baseWebData.IndexOf(CsfdCzUtil.showEpisodesUrlCategoryNameEnd, startIndex + CsfdCzUtil.showEpisodesUrlCategoryNameStart.Length);
                            if (endIndex >= 0)
                            {
                                pageVideos.Add(new VideoInfo() { Title = baseWebData.Substring(startIndex + CsfdCzUtil.showEpisodesUrlCategoryNameStart.Length, endIndex - startIndex - CsfdCzUtil.showEpisodesUrlCategoryNameStart.Length), VideoUrl = pageUrl });
                            }
                        }

                        Match match = Regex.Match(baseWebData, CsfdCzUtil.showEpisodesUrlCategoryUrlRegex);
                        while (match.Success)
                        {
                            String catName = match.Groups["showEpisodesUrlCategoryName"].Value;
                            String catUrl = Utils.FormatAbsoluteUrl(match.Groups["showEpisodesUrlCategoryUrl"].Value, pageUrl);

                            pageVideos.Add(new VideoInfo() { Title = catName, VideoUrl = catUrl });

                            match = match.NextMatch();
                        }
                    }
                }

                //// serach for videos in each category
                //foreach (var internalCategory in categories)
                //{
                //    baseWebData = CsfdCzUtil.GetWebData(internalCategory.Url, null, null, null, true);

                //    startIndex = baseWebData.IndexOf(CsfdCzUtil.showEpisodesBlockStart);
                //    if (startIndex >= 0)
                //    {
                //        endIndex = baseWebData.IndexOf(CsfdCzUtil.showEpisodesBlockEnd, startIndex);
                //        if (endIndex >= 0)
                //        {
                //            baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

                //            while (true)
                //            {
                //                startIndex = baseWebData.IndexOf(CsfdCzUtil.showEpisodeBlockStart);
                //                if (startIndex >= 0)
                //                {
                //                    endIndex = baseWebData.IndexOf(CsfdCzUtil.showEpisodeBlockEnd, startIndex);
                //                    if (endIndex >= 0)
                //                    {
                //                        String episodeData = baseWebData.Substring(startIndex, endIndex - startIndex);
                //                        String episodeTitle = String.Empty;
                //                        String episodeUrl = String.Empty;

                //                        Match match = Regex.Match(episodeData, CsfdCzUtil.showEpisodeUrlAndTitleRegex);
                //                        if (match.Success)
                //                        {
                //                            episodeTitle = OnlineVideos.Utils.PlainTextFromHtml(match.Groups["showTitle"].Value);
                //                            episodeUrl = Utils.FormatAbsoluteUrl(String.Format("filtr-{0}", match.Groups["showUrl"].Value), pageUrl);
                //                        }

                //                        if ((!String.IsNullOrEmpty(episodeUrl)) && (!String.IsNullOrEmpty(episodeTitle)))
                //                        {
                //                            VideoInfo videoInfo = new VideoInfo()
                //                            {
                //                                Title = episodeTitle,
                //                                VideoUrl = episodeUrl
                //                            };

                //                            pageVideos.Add(videoInfo);
                //                        }

                //                        baseWebData = baseWebData.Substring(endIndex);
                //                    }
                //                    else
                //                    {
                //                        break;
                //                    }
                //                }
                //                else
                //                {
                //                    break;
                //                }
                //            }
                //        }
                //    }
                //}
            }

            return pageVideos;
        }

        private List<VideoInfo> GetVideoList(Category category)
        {
            hasNextPage = false;
            String baseWebData = String.Empty;
            RssLink parentCategory = (RssLink)category;
            List<VideoInfo> videoList = new List<VideoInfo>();

            if (parentCategory.Name != this.currentCategory.Name)
            {
                this.currentStartIndex = 0;
                this.nextPageUrl = parentCategory.Url;
                this.loadedEpisodes.Clear();
            }

            this.currentCategory = parentCategory;
            this.loadedEpisodes.AddRange(this.GetPageVideos(this.nextPageUrl));
            while (this.currentStartIndex < this.loadedEpisodes.Count)
            {
                videoList.Add(this.loadedEpisodes[this.currentStartIndex++]);
            }

            if (!String.IsNullOrEmpty(this.nextPageUrl))
            {
                hasNextPage = true;
            }

            return videoList;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            this.currentStartIndex = 0;
            return this.GetVideoList(category);
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return this.GetVideoList(this.currentCategory);
        }

        public override bool HasNextPage
        {
            get
            {
                return this.hasNextPage;
            }
            protected set
            {
                this.hasNextPage = value;
            }
        }

        public override List<string> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            List<String> resultUrls = new List<string>();

            CsfdCzVideoCollection videos = new CsfdCzVideoCollection();

            String baseWebData = SiteUtilBase.GetWebData(video.VideoUrl, null, null, null, true);
            int index = 0;

            while (true)
            {
                int startIndex = baseWebData.IndexOf(CsfdCzUtil.videoUrlBlockStart);
                if (startIndex >= 0)
                {
                    int endIndex = baseWebData.IndexOf(CsfdCzUtil.videoUrlBlockEnd, startIndex);
                    if (endIndex >= 0)
                    {
                        String section = baseWebData.Substring(startIndex, endIndex - startIndex);
                        String url = Utils.FormatAbsoluteUrl(index.ToString(), video.VideoUrl);

                        videos.Add(new CsfdCzVideo() { Section = section, Url = url });
                        resultUrls.Add(url);

                        baseWebData = baseWebData.Substring(endIndex + CsfdCzUtil.videoUrlBlockEnd.Length);
                        index++;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            // remember all videos with their quality
            video.Other = videos;

            if (resultUrls.Count == 1)
            {
                resultUrls.Clear();

                String oldUrl = video.VideoUrl;
                video.VideoUrl = videos[0].Url;

                resultUrls.Add(this.getPlaylistItemUrl(video, "", false));

                video.VideoUrl = oldUrl;
            }

            return resultUrls;
        }

        public override string getPlaylistItemUrl(VideoInfo clonedVideoInfo, string chosenPlaybackOption, bool inPlaylist = false)
        {
            CsfdCzVideoCollection videos = (CsfdCzVideoCollection)clonedVideoInfo.Other;
            CsfdCzVideo keyVideo = videos[clonedVideoInfo.VideoUrl];

            if (clonedVideoInfo.PlaybackOptions == null)
            {
                clonedVideoInfo.PlaybackOptions = new Dictionary<string, string>();
            }
            clonedVideoInfo.PlaybackOptions.Clear();

            MatchCollection matches = Regex.Matches(keyVideo.Section, CsfdCzUtil.videoUrlRegex);
            for (int i = 0; i < matches.Count; i++)
            {
                String url = matches[i].Groups["videoUrl"].Value.Replace("\\/", "/");
                String extension = Path.GetExtension(url).Replace(".", "");
                String fileName = Path.GetFileNameWithoutExtension(url);

                if (!url.Contains("/ads/"))
                {
                    clonedVideoInfo.PlaybackOptions.Add(String.Format("{0}p ({1})", fileName, extension), url);
                }
            }

            Match subtitleMatch = Regex.Match(keyVideo.Section, CsfdCzUtil.subtitleUrlRegex);
            if (subtitleMatch.Success)
            {
                clonedVideoInfo.SubtitleUrl = System.Web.HttpUtility.HtmlDecode(subtitleMatch.Groups["subtitleUrl"].Value).Replace(@"\/", "/");
            }

            if (clonedVideoInfo.PlaybackOptions.Count > 0)
            {
                var enumer = clonedVideoInfo.PlaybackOptions.GetEnumerator();
                enumer.MoveNext();
                return enumer.Current.Value;
            }

            return clonedVideoInfo.VideoUrl;
        }

        //public override string getUrl(VideoInfo video)
        //{
        //    String baseWebData = SiteUtilBase.GetWebData(video.VideoUrl, null, null, null, true);

        //    if (video.PlaybackOptions == null)
        //    {
        //        video.PlaybackOptions = new Dictionary<string, string>();
        //    }
        //    video.PlaybackOptions.Clear();

        //    int startIndex = baseWebData.IndexOf(CsfdCzUtil.videoUrlBlockStart);
        //    if (startIndex >= 0)
        //    {
        //        int endIndex = baseWebData.IndexOf(CsfdCzUtil.videoUrlBlockEnd, startIndex);
        //        if (endIndex >= 0)
        //        {
        //            baseWebData = baseWebData.Substring(startIndex, endIndex - startIndex);

        //            MatchCollection matches = Regex.Matches(baseWebData, CsfdCzUtil.videoUrlRegex);
        //            for (int i = 0; i < matches.Count; i++)
        //            {
        //                String url = matches[i].Groups["videoUrl"].Value.Replace("\\/", "/");
        //                String extension = Path.GetExtension(url).Replace(".", "");
        //                String fileName = Path.GetFileNameWithoutExtension(url);

        //                if (!url.Contains("/ads/"))
        //                {
        //                    video.PlaybackOptions.Add(String.Format("{0}p ({1})", fileName, extension), url);
        //                }
        //            }

        //            Match subtitleMatch = Regex.Match(baseWebData, CsfdCzUtil.subtitleUrlRegex);
        //            if (subtitleMatch.Success)
        //            {
        //                video.SubtitleUrl = System.Web.HttpUtility.HtmlDecode(subtitleMatch.Groups["subtitleUrl"].Value).Replace(@"\/", "/");
        //            }
        //        }
        //    }

        //    if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
        //    {
        //        var enumer = video.PlaybackOptions.GetEnumerator();
        //        enumer.MoveNext();
        //        return enumer.Current.Value;
        //    }

        //    return String.Empty;
        //}

        #endregion
    }
}

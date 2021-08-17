using Newtonsoft.Json.Linq;
using OnlineVideos.Helpers;
using OnlineVideos.Hoster;
using OnlineVideos.Sites.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{

    public class SvtPlayUtil : LatestVideosSiteUtilBase, IChoice
    {
        #region Svt classes

        public class SvtCategory : RssLink
        {
            public List<VideoInfo> Videos
            {
                get;
                set;
            }
        }

        #endregion

        #region Configuration


        #endregion

        #region vars and const

        private string currentVideosUrl = "";
        private uint currentVideosPage = 1;

        #endregion

        #region Categories

        public override int DiscoverDynamicCategories()
        {
            Category programCategory = new Category()
            {
                Name = "Program",
                HasSubCategories = true
            };
            programCategory.Other = (Func<List<Category>>)(() => GetProgramCategories(programCategory));
            Settings.Categories.Add(programCategory);
            Category channelCategory = new Category()
            {
                Name = "Kanaler",
                Other = "isChannels"
            };
            Settings.Categories.Add(channelCategory);
            RssLink popularCategory = new RssLink()
            {
                Name = "Populärt",
                SubCategories = new List<Category>(),
                HasSubCategories = true
            };
            popularCategory.Other = (Func<List<Category>>)(() => GetCategoriesFromArray(popularCategory, "http://www.svtplay.se/api/popular/?page={0}&pageSize=48", 1));
            Settings.Categories.Add(popularCategory);
            RssLink latestCategory = new RssLink()
            {
                Name = "Senaste program",
                Url = "http://www.svtplay.se/api/latest?page={0}",
                HasSubCategories = false
            };
            Settings.Categories.Add(latestCategory);
            RssLink lastChanceCategory = new RssLink()
            {
                Name = "Sista chansen",
                Url = "http://www.svtplay.se/api/last_chance?page={0}",
                HasSubCategories = false
            };
            Settings.Categories.Add(lastChanceCategory);
            RssLink liveCategory = new RssLink()
            {
                Name = "Livesändningar",
                Url = "http://www.svtplay.se/api/live?page={0}",
                Other = "isLive",
                HasSubCategories = false
            };
            Settings.Categories.Add(liveCategory);
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            int count = 0;
            Func<List<Category>> method = parentCategory.Other as Func<List<Category>>;
            if (method != null)
            {
                parentCategory.SubCategories = method();
                parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
                count = parentCategory.SubCategories.Count;
            }
            return count;
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            int count = 0;
            Func<List<Category>> method = category.Other as Func<List<Category>>;
            category.ParentCategory.SubCategories.Remove(category);
            if (method != null)
            {
                List<Category> cats = method();
                category.ParentCategory.SubCategories.AddRange(cats);
                count = cats.Count;
            }
            return count;
        }

        private List<Category> GetCategoriesFromArray(Category parentCategory, string url, int page)
        {
            JArray array;
            JObject json = null;
            string data = GetWebData<string>(string.Format(url, page));
            if (data.StartsWith("[") && data.EndsWith("]"))
            {
                array = JArray.Parse(data);
            }
            else
            {
                json = JObject.Parse(data);
                array = json["data"].Value<JArray>();
            }
            List<Category> cats = new List<Category>();
            foreach (JToken token in array)
            {
                RssLink popSubCategory;
                bool isVideoEpisode = token["contentType"] != null && token["contentType"].Type == JTokenType.String && token["contentType"].Value<string>() == "videoEpisod";
                if (!isVideoEpisode)
                    popSubCategory = new RssLink();
                else
                    popSubCategory = new SvtCategory();
                popSubCategory.ParentCategory = parentCategory;
                popSubCategory.Name = token["programTitle"].Value<string>();
                if (token["description"] != null && token["description"].Type != JTokenType.Null)
                    popSubCategory.Description = token["description"].Value<string>();
                if (token["poster"] != null && token["poster"].Type != JTokenType.Null)
                    popSubCategory.Thumb = token["poster"].Value<string>().Replace("{format}", "medium");
                popSubCategory.HasSubCategories = !isVideoEpisode;
                if (!isVideoEpisode)
                    popSubCategory.Url = token["contentUrl"].Value<string>().Replace("/", "");
                else if (popSubCategory is SvtCategory)
                    (popSubCategory as SvtCategory).Videos = GetVideos(new JArray() { token });
                if (!isVideoEpisode)
                    popSubCategory.Other = (Func<List<Category>>)(() => GetProgramCategoriesAndVideos(popSubCategory));
                cats.Add(popSubCategory);
            }
            if (json != null && json["totalPages"] != null && json["totalPages"].Type == JTokenType.Integer && json["totalPages"].Value<int>() > page)
            {
                NextPageCategory next = new NextPageCategory() { ParentCategory = parentCategory };
                next.Other = (Func<List<Category>>)(() => GetCategoriesFromArray(parentCategory, url, page + 1));
                cats.Add(next);
            }
            return cats;
        }

        private List<Category> GetProgramCategories(Category parentCategory)
        {
            List<Category> cats = new List<Category>();
            Category aToZCategory = new Category()
            {
                Name = "Program A-Ö",
                HasSubCategories = true,
                ParentCategory = parentCategory
            };
            aToZCategory.Other = (Func<List<Category>>)(() => GetAZProgramCatsFromJson(aToZCategory));
            cats.Add(aToZCategory);
            Category allTags = new Category()
            {
                Name = "Genrer",
                HasSubCategories = true,
                SubCategoriesDiscovered = true,
                SubCategories = new List<Category>(),
                ParentCategory = parentCategory
            };
            cats.Add(allTags);
            string data = GetWebData("https://www.svtplay.se/api/clusters");
            RssLink oppetArkivCategory = new RssLink()
            {
                Name = "Öppet arkiv",
                Url = "oppetarkiv",
                HasSubCategories = true,
                ParentCategory = allTags
            };
            oppetArkivCategory.Other = (Func<List<Category>>)(() => GetTagCategories(oppetArkivCategory));
            allTags.SubCategories.Add(oppetArkivCategory);
            foreach (JToken clusterToken in JArray.Parse(data))
            {
                string url = clusterToken["slug"].Value<string>();
                string title = clusterToken["name"].Value<string>();
                RssLink tagCategory = new RssLink()
                {
                    Name = title,
                    Url = url,
                    HasSubCategories = true,
                    ParentCategory = allTags
                };
                tagCategory.Other = (Func<List<Category>>)(() => GetTagCategories(tagCategory));
                if (tagCategory.Url != "barn")
                    allTags.SubCategories.Add(tagCategory);
                else
                    allTags.SubCategories.Insert(1, tagCategory);
            }
            return cats;
        }

        private List<Category> GetProgramCategoriesAndVideos(Category parentCategory)
        {
            string slug = (parentCategory as RssLink).Url;
            int articleId = GetWebData<JObject>(string.Format("http://www.svtplay.se/api/title?slug={0}", slug))["articleId"].Value<int>();
            List<Category> cats = new List<Category>();
            SvtCategory programs = new SvtCategory()
            {
                Name = "Avsnitt",
                Videos = new List<VideoInfo>(),
                ParentCategory = parentCategory,
                HasSubCategories = false,
                Url = string.Format("http://www.svtplay.se/api/title_episodes_by_article_id?articleId={0}", articleId)
            };
            cats.Add(programs);
            SvtCategory clips = new SvtCategory()
            {
                Name = "Klipp",
                Videos = new List<VideoInfo>(),
                ParentCategory = parentCategory,
                HasSubCategories = false,
                Url = string.Format("http://www.svtplay.se/api/title_clips_by_title_article_id?articleId={0}", articleId)
            };
            cats.Add(clips);
            RssLink related = new RssLink()
            {
                Name = "Liknande program",
                ParentCategory = parentCategory,
                SubCategories = new List<Category>(),
                HasSubCategories = true
            };
            related.Other = (Func<List<Category>>)(() => GetCategoriesFromArray(related, string.Format("http://www.svtplay.se/api/similar_content_for_title?slug={0}", slug) + "&dummy={0}", 0));
            cats.Add(related);
            foreach (SvtCategory cat in cats.Where(c => !c.HasSubCategories))
            {
                string data = GetWebData<string>(cat.Url);
                if (data.StartsWith("[") && data.EndsWith("]"))
                {
                    cat.Videos = GetVideos(JArray.Parse(data));
                }
                else
                {
                    cat.Videos = new List<VideoInfo>();
                }
            }
            if (programs.Videos.Count == 0)
                cats.Remove(programs);
            if (clips.Videos.Count == 0)
                cats.Remove(clips);
            return cats;
        }

        private List<Category> GetTagCategories(Category parentCategory)
        {
            JArray jArray;
            string data;
            List<Category> cats = new List<Category>();

            Category contents = new Category()
            {
                Name = "Program A-Ö",
                ParentCategory = parentCategory,
                SubCategories = new List<Category>(),
                HasSubCategories = true,
                SubCategoriesDiscovered = true
            };
            if ((parentCategory as RssLink).Url == "oppetarkiv")
            {
                data = GetWebData("http://www.oppetarkiv.se/program");
                Regex r = new Regex(@"<a\s+class=""svtoa-anchor-list-link""\s+href=""(?<url>[^""]*).*?>(?<title>[^<]*)", RegexOptions.Singleline);
                foreach (Match m in r.Matches(data))
                {
                    if (m.Success)
                    {
                        RssLink programCategoriesAndVideos = new RssLink()
                        {
                            Name = HttpUtility.HtmlDecode(m.Groups["title"].Value),
                            Url = string.Concat("http://www.oppetarkiv.se", m.Groups["url"].Value, "?sida={0}&sort=tid_stigande&embed=true"),
                            HasSubCategories = false,
                            ParentCategory = parentCategory
                        };
                        programCategoriesAndVideos.Other = (Func<List<Category>>)(() => GetProgramCategoriesAndVideos(programCategoriesAndVideos));
                        cats.Add(programCategoriesAndVideos);
                    }
                }
            }
            else
            {
                string progData = GetWebData<string>(string.Concat("http://www.svtplay.se/api/cluster_titles_and_episodes/?cluster=", (parentCategory as RssLink).Url));
                jArray = JArray.Parse(progData);
                foreach (JToken token in jArray)
                {
                    RssLink genreCategory;
                    bool isVideoEpisode = token["contentType"] != null && token["contentType"].Type == JTokenType.String && token["contentType"].Value<string>() == "videoEpisod";
                    if (!isVideoEpisode)
                        genreCategory = new RssLink();
                    else
                        genreCategory = new SvtCategory();
                    genreCategory.ParentCategory = contents;
                    genreCategory.Name = token["programTitle"].Value<string>();
                    if (token["description"] != null && token["description"].Type != JTokenType.Null)
                        genreCategory.Description = token["description"].Value<string>();
                    if (token["poster"] != null && token["poster"].Type != JTokenType.Null)
                        genreCategory.Thumb = token["poster"].Value<string>().Replace("{format}", "medium");
                    genreCategory.HasSubCategories = !isVideoEpisode;
                    if (!isVideoEpisode)
                        genreCategory.Url = token["contentUrl"].Value<string>().Replace("/", "");
                    else if (genreCategory is SvtCategory)
                        (genreCategory as SvtCategory).Videos = GetVideos(new JArray() { token });
                    if (!isVideoEpisode)
                        genreCategory.Other = (Func<List<Category>>)(() => GetProgramCategoriesAndVideos(genreCategory));
                    contents.SubCategories.Add(genreCategory);
                }
                cats.Add(contents);
                data = GetWebData<string>(string.Concat("http://www.svtplay.se/api/cluster_popular?cluster=", (parentCategory as RssLink).Url));
                if (data.StartsWith("[") && data.EndsWith("]"))
                {
                    jArray = JArray.Parse(data);
                    if (jArray.Count > 0)
                    {
                        SvtCategory popular = new SvtCategory()
                        {
                            Name = "Populärt",
                            ParentCategory = parentCategory,
                            HasSubCategories = true,
                            SubCategories = new List<Category>(),
                            SubCategoriesDiscovered = true
                        };
                        foreach (JToken token in jArray)
                        {
                            RssLink popSubCategory;
                            bool isVideoEpisode = token["contentType"] != null && token["contentType"].Type == JTokenType.String && token["contentType"].Value<string>() == "videoEpisod";
                            if (!isVideoEpisode)
                                popSubCategory = new RssLink();
                            else
                                popSubCategory = new SvtCategory();
                            popSubCategory.ParentCategory = popular;
                            popSubCategory.Name = token["programTitle"].Value<string>();
                            if (token["description"] != null && token["description"].Type != JTokenType.Null)
                                popSubCategory.Description = token["description"].Value<string>();
                            if (token["poster"] != null && token["poster"].Type != JTokenType.Null)
                                popSubCategory.Thumb = token["poster"].Value<string>().Replace("{format}", "medium");
                            popSubCategory.HasSubCategories = !isVideoEpisode;
                            if (!isVideoEpisode)
                                popSubCategory.Url = token["contentUrl"].Value<string>().Replace("/", "");
                            else if (popSubCategory is SvtCategory)
                                (popSubCategory as SvtCategory).Videos = GetVideos(new JArray() { token });
                            if (!isVideoEpisode)
                                popSubCategory.Other = (Func<List<Category>>)(() => GetProgramCategoriesAndVideos(popSubCategory));
                            popular.SubCategories.Add(popSubCategory);
                        }
                        cats.Add(popular);
                    }
                }
                data = GetWebData<string>(string.Concat("http://www.svtplay.se/api/cluster_latest?cluster=", (parentCategory as RssLink).Url));
                if (data.StartsWith("[") && data.EndsWith("]"))
                {
                    jArray = JArray.Parse(data);
                    if (jArray.Count > 0)
                    {
                        SvtCategory latest = new SvtCategory()
                        {
                            Name = "Senaste program",
                            ParentCategory = parentCategory,
                            Videos = GetVideos(jArray)
                        };
                        cats.Add(latest);
                    }
                }
                data = GetWebData<string>(string.Concat("http://www.svtplay.se/api/cluster_last_chance?cluster=", (parentCategory as RssLink).Url));
                if (data.StartsWith("[") && data.EndsWith("]"))
                {
                    jArray = JArray.Parse(data);
                    if (jArray.Count > 0)
                    {
                        SvtCategory lastChance = new SvtCategory()
                        {
                            Name = "Sista chansen",
                            ParentCategory = parentCategory,
                            Videos = GetVideos(jArray)
                        };
                        cats.Add(lastChance);
                    }
                }
                data = GetWebData<string>(string.Concat("http://www.svtplay.se/api/cluster_clips?cluster=", (parentCategory as RssLink).Url));
                if (data.StartsWith("[") && data.EndsWith("]"))
                {
                    jArray = JArray.Parse(data);
                    if (jArray.Count > 0)
                    {
                        SvtCategory clips = new SvtCategory()
                        {
                            Name = "Senaste klipp",
                            ParentCategory = parentCategory,
                            Videos = GetVideos(jArray)
                        };
                        cats.Add(clips);
                    }
                }
            }
            return cats;
        }

        private List<Category> GetAZProgramCatsFromJson(Category parentCategory)
        {
            string data = GetWebData<string>("https://www.svtplay.se/api/all_titles_and_singles");
            JArray array = JArray.Parse(data);

            List<Category> categories = new List<Category>();
            foreach (JToken element in array.Where(e => !e["contentUrl"].Value<string>().StartsWith("/video/")))
            {
                RssLink category = new RssLink()
                {
                    Name = element["programTitle"].Value<string>(),
                    Url = element["contentUrl"].Value<string>().Replace("/",""),
                    ParentCategory = parentCategory,
                    SubCategories = new List<Category>(),
                    HasSubCategories = true
                };
                category.Other = (Func<List<Category>>)(() => GetProgramCategoriesAndVideos(category));
                categories.Add(category);
            }
            categories = categories.OrderBy(c => c.Name).ToList<Category>();
            return categories;
        }

        #endregion

        #region Videos

        public override List<VideoInfo> GetVideos(Category category)
        {
            HasNextPage = false;
            List<VideoInfo> videos;
            if (category is SvtCategory)
            {
                videos = (category as SvtCategory).Videos;
            }
            else if (category.GetOtherAsString() != "isChannels")
            {
                currentVideosUrl = (category as RssLink).Url;
                currentVideosPage = 1;
                bool hasNext = false;
                videos = GetVideos(currentVideosUrl, currentVideosPage, out hasNext);
                HasNextPage = hasNext;
            }
            else
            {
                videos = GetChannelVideos();
            }
            videos.ForEach(v => v.HasDetails = (category.GetOtherAsString() == "isLive"));
            return videos;
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            currentVideosPage = currentVideosPage + 1;
            bool hasNext = false;
            List<VideoInfo> videos = GetVideos(currentVideosUrl, currentVideosPage, out hasNext);
            HasNextPage = hasNext;
            return videos;
        }

        private List<VideoInfo> GetVideos(string url, uint page, out bool hasNext)
        {
            hasNext = false;
            if (!currentVideosUrl.Contains("oppetarkiv"))
            {
                JObject json = GetWebData<JObject>(string.Format(url, page));
                hasNext = (json["totalPages"] != null && json["totalPages"].Type == JTokenType.Integer && json["totalPages"].Value<int>() > page);
                return GetVideos(json["data"].Value<JArray>());
            }
            else
            {
                Regex r = new Regex(@"<article.*?srcset=""(?<ImageUrl>.*?jpg).*?alt=""(?<Title>[^""]*).*?href=""(?<VideoUrl>[^""]*)", RegexOptions.Singleline);
                string data = GetWebData(string.Format(url, page));
                List<VideoInfo> videos = new List<VideoInfo>();
                foreach (Match m in r.Matches(data))
                {
                    if (m.Success)
                    {
                        VideoInfo videoInfo = new VideoInfo()
                        {
                            Title = m.Groups["Title"].Value,
                            Thumb = m.Groups["ImageUrl"].Value,
                            VideoUrl = string.Concat("http://www.oppetarkiv.se", m.Groups["VideoUrl"].Value)
                        };
                        videos.Add(videoInfo);
                    }
                }
                hasNext = data.Contains("Visa fler");
                return videos;
            }
        }

        private List<VideoInfo> GetVideos(JArray jVideos)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            if (jVideos != null)
            {
                foreach (JToken jVideo in jVideos)
                {
                    string contentUrl = jVideo["contentUrl"].Value<string>();
                    if (!string.IsNullOrWhiteSpace(contentUrl))
                    {
                        Match m = (new Regex(@"/(?:klipp|video)/(?<id>\d+)/")).Match(contentUrl);
                        string url = contentUrl;
                        if (m.Success)
                        {
                            url = string.Format("http://www.svtplay.se/api/episode?id={0}", m.Groups["id"].Value);
                        }
                        if (url.StartsWith("//")) url = url.Replace("//", "http://");
                        VideoInfo video = new VideoInfo();
                        video.VideoUrl = url;
                        string programTitle = "";
                        if (jVideo["programTitle"] != null && jVideo["programTitle"].Type == JTokenType.String)
                            programTitle = jVideo["programTitle"].Value<string>();
                        else if (jVideo["homeSectionName"] != null && jVideo["homeSectionName"].Type == JTokenType.String)
                            programTitle = jVideo["homeSectionName"].Value<string>();
                        string title = "";
                        if (jVideo["title"] != null && jVideo["title"].Type == JTokenType.String)
                            title = jVideo["title"].Value<string>();
                        int season = 0;
                        if (jVideo["season"] != null && jVideo["season"].Type == JTokenType.Integer)
                            season = jVideo["season"].Value<int>();
                        int episodeNumber = 0;
                        if (jVideo["episodeNumber"] != null && jVideo["episodeNumber"].Type == JTokenType.Integer)
                            episodeNumber = jVideo["episodeNumber"].Value<int>();
                        if (season > 0 && episodeNumber == 0 && title.ToLowerInvariant().Contains("avsnitt"))
                        {
                            m = (new Regex(@"[A|a]vsnitt\s+(?<episode>\d+)")).Match(title);
                            if (m.Success)
                            {
                                string episodeString = m.Groups["episode"].Value;
                                int.TryParse(episodeString, out episodeNumber);
                            }
                        }
                        if (season > 0 && episodeNumber > 0)
                        {
                            TrackingInfo trackingInfo = new TrackingInfo()
                            {
                                VideoKind = VideoKind.TvSeries,
                                Title = programTitle,
                                Season = (uint)season,
                                Episode = (uint)episodeNumber
                            };
                            video.Title = string.Format("{0} - {1}x{2}{3}{4}", programTitle, season, episodeNumber > 9 ? string.Empty : "0", episodeNumber, (string.IsNullOrWhiteSpace(title) ? string.Empty : " - " + title));
                            video.Other = trackingInfo;
                        }
                        else if (programTitle.ToLowerInvariant().Contains(title.ToLowerInvariant()))
                            video.Title = programTitle;
                        else if (!string.IsNullOrWhiteSpace(programTitle))
                            video.Title = programTitle + (string.IsNullOrWhiteSpace(title) ? string.Empty : " - " + title);
                        else
                            video.Title = title;
                        if (jVideo["description"] != null && jVideo["description"].Type == JTokenType.String)
                            video.Description = jVideo["description"].Value<string>();
                        if (jVideo["thumbnail"] != null && jVideo["thumbnail"].Type == JTokenType.String)
                            video.Thumb = jVideo["thumbnail"].Value<string>().Replace("{format}", "medium");
                        if (string.IsNullOrEmpty(video.Thumb) && jVideo["imageMedium"] != null && jVideo["imageMedium"].Type == JTokenType.String)
                            video.Thumb = jVideo["imageMedium"].Value<string>();
                        if (string.IsNullOrEmpty(video.Thumb) && jVideo["poster"] != null && jVideo["poster"].Type == JTokenType.String)
                            video.Thumb = jVideo["poster"].Value<string>().Replace("{format}", "medium");
                        int seconds = 0;
                        if (jVideo["materialLength"] != null && jVideo["materialLength"].Type == JTokenType.Integer)
                            seconds = jVideo["materialLength"].Value<int>();
                        if (seconds > 0)
                            video.Length = TimeUtils.TimeFromSeconds(seconds.ToString());
                        string date = "";
                        if (jVideo["broadcastDate"] != null && jVideo["broadcastDate"].Type != JTokenType.Null)
                            date = jVideo["broadcastDate"].Value<string>();
                        else if (jVideo["publishDate"] != null && jVideo["publishDate"].Type != JTokenType.Null)
                            date = jVideo["publishDate"].Value<string>();
                        if (!string.IsNullOrEmpty(date))
                        {
                            long t = 0;
                            DateTime dateTime;
                            if (long.TryParse(date, out t))
                                dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(t);
                            else
                                dateTime = DateTime.Parse(date);
                            video.Airdate = dateTime.ToString(OnlineVideoSettings.Instance.Locale);
                        }
                        videos.Add(video);
                    }
                }
            }
            return videos;
        }

        private List<VideoInfo> GetChannelVideos()
        {
            List<VideoInfo> videos = new List<VideoInfo>()
            {
                new VideoInfo() { Title = "SVT1", Other = "https://api.svt.se/videoplayer-api/video/ch-svt1"},
                new VideoInfo() { Title = "SVT2", Other = "https://api.svt.se/videoplayer-api/video/ch-svt2"},
                new VideoInfo() { Title = "SVT24", Other = "https://api.svt.se/videoplayer-api/video/ch-svt24"},
                new VideoInfo() { Title = "Barnkanalen", Other = "https://api.svt.se/videoplayer-api/video/ch-barnkanalen"},
                new VideoInfo() { Title = "Kunskapskanalen", Other = "https://api.svt.se/videoplayer-api/video/ch-kunskapskanalen"},
            };

            foreach (VideoInfo channel in videos)
            {
                JObject json = GetWebData<JObject>(channel.GetOtherAsString());
                JToken videoReference = json["videoReferences"].FirstOrDefault<JToken>((JToken vr) => vr["format"].Value<string>() == "hls");
                if (videoReference != null)
                    channel.VideoUrl = videoReference["url"].Value<string>();
            }
            return videos;
        }

        public override List<string> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            video.PlaybackOptions = new Dictionary<string, string>();
            string videoUrl = video.VideoUrl;
            if (videoUrl.EndsWith(".m3u8"))
            {
                video.PlaybackOptions = HlsPlaylistParser.GetPlaybackOptions(GetWebData(videoUrl), videoUrl);
            }
            else
            {
                HosterBase hoster = HosterFactory.GetAllHosters().FirstOrDefault<HosterBase>((HosterBase h) => videoUrl.ToLower().Contains(h.GetHosterUrl().ToLower()));
                if (hoster != null)
                {
                    video.PlaybackOptions = hoster.GetPlaybackOptions(videoUrl);
                    if (hoster is ISubtitle)
                        video.SubtitleText = (hoster as ISubtitle).SubtitleText;
                }
            }
            if (video.PlaybackOptions.Count == 0)
                return new List<string>();
            if (video.HasDetails && (video as DetailVideoInfo).Title2 == "Live")
            {
                video.PlaybackOptions = video.PlaybackOptions.ToDictionary(p => p.Key, p => p.Value.Replace("start=", "dummy="));
            }
            string url = video.PlaybackOptions.First<KeyValuePair<string, string>>().Value;
            if (inPlaylist)
                video.PlaybackOptions.Clear();
            return new List<string>() { url };
        }

        public override string GetFileNameForDownload(VideoInfo video, Category category, string url)
        {
            string ext = (url.Contains(".m3u8") ? ".mp4" : ".f4m");
            return string.Concat(FileUtils.GetSaveFilename(video.Title), ext);
        }

        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            return video.Other is ITrackingInfo ? video.Other as ITrackingInfo : base.GetTrackingInfo(video);
        }


        #endregion

        #region Search

        public override bool CanSearch
        {
            get
            {
                return true;
            }
        }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            List<SearchResultItem> searchResultItems = new List<SearchResultItem>();
            List<Category> categories = new List<Category>();
            JObject json = GetWebData<JObject>(string.Format("https://www.svtplay.se/api/search?q={0}", HttpUtility.UrlEncode(query)));
            if (json["categories"] != null && json["categories"].Type == JTokenType.Array)
            {
                Category genres = new Category()
                {
                    Name = "Genrer",
                    HasSubCategories = true,
                    SubCategoriesDiscovered = true,
                    SubCategories = new List<Category>()
                };
                foreach (JToken item in json["categories"].Value<JArray>())
                {
                    genres.SubCategories.Add(new RssLink()
                    {
                        Name = item["name"].Value<string>(),
                        Url = item["contentUrl"].Value<string>().Replace("genre/", "").Replace("/", ""),
                        Thumb = item["thumbnailImage"] != null && item["thumbnailImage"].Type != JTokenType.Null ? item["thumbnailImage"].Value<string>() : "",
                        HasSubCategories = true,
                        SubCategories = new List<Category>()
                    });

                }
                genres.SubCategories.ForEach(c => c.Other = (Func<List<Category>>)(() => GetTagCategories(c)));
                if (genres.SubCategories.Count > 0)
                    categories.Add(genres);
            }
            if (json["videosAndTitles"] != null && json["videosAndTitles"].Type == JTokenType.Array)
            {
                Category titles = new Category()
                {
                    Name = "Program",
                    HasSubCategories = true,
                    SubCategoriesDiscovered = true,
                    SubCategories = new List<Category>()
                };
                foreach (JToken item in json["videosAndTitles"].Value<JArray>().Where(i => i["titleType"] == null))
                {
                    titles.SubCategories.Add(new RssLink()
                    {
                        Name = item["programTitle"].Value<string>(),
                        Url = item["contentUrl"].Value<string>().Replace("/", ""),
                        Thumb = item["poster"] != null && item["poster"].Type != JTokenType.Null ? item["poster"].Value<string>().Replace("{format}", "medium") : "",
                        Description = item["description"].Value<string>(),
                        HasSubCategories = true,
                        SubCategories = new List<Category>()
                    });

                }
                titles.SubCategories.ForEach(t => t.Other = (Func<List<Category>>)(() => GetProgramCategoriesAndVideos(t)));
                if (titles.SubCategories.Count > 0)
                    categories.Add(titles);
                SvtCategory videos = new SvtCategory()
                {
                    Name = "Videos",
                    HasSubCategories = false
                };
                JArray filtered = new JArray();
                foreach (JToken to in json["videosAndTitles"].Value<JArray>().Where(t => t["titleType"] != null))
                {
                    filtered.Add(to);
                }
                videos.Videos = GetVideos(filtered);
                if (videos.Videos.Count > 0)
                    categories.Add(videos);

            }
            if (json["openArchive"] != null && json["openArchive"].Type == JTokenType.Array)
            {
                SvtCategory openArchive = new SvtCategory()
                {
                    Name = "Öppet arkiv",
                    HasSubCategories = false
                };
                openArchive.Videos = GetVideos(json["openArchive"].Value<JArray>());
                if (openArchive.Videos.Count > 0)
                    categories.Add(openArchive);
            }
            categories.ForEach(c => searchResultItems.Add(c));
            return searchResultItems;
        }

        #endregion

        #region Latest

        public override List<VideoInfo> GetLatestVideos()
        {
            bool dummy = false;
            List<VideoInfo> videos = GetVideos("http://www.svtplay.se/api/latest?page={0}", 1, out dummy);
            return (videos.Count >= LatestVideosCount ? videos.GetRange(0, (int)LatestVideosCount) : new List<VideoInfo>());
        }

        #endregion

        #region IChoice

        List<DetailVideoInfo> IChoice.GetVideoChoices(VideoInfo video)
        {
            List<DetailVideoInfo> videos = new List<DetailVideoInfo>();
            DetailVideoInfo live = new DetailVideoInfo(video);
            live.Title2 = "Live";
            videos.Add(live);
            DetailVideoInfo fromStart = new DetailVideoInfo(video);
            fromStart.Title2 = "Spela upp sändningen från början";
            videos.Add(fromStart);
            return videos;
        }

        #endregion
    }
}

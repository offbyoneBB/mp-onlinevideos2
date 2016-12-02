using Newtonsoft.Json.Linq;
using OnlineVideos;
using OnlineVideos.CrossDomain;
using OnlineVideos.Helpers;
using OnlineVideos.Hoster;
using OnlineVideos.Sites.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{

    public class SvtPlayUtil : LatestVideosSiteUtilBase
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
                Url = "http://www.svtplay.se/api/popular_page?page={0}",
                HasSubCategories = false
            };
            Settings.Categories.Add(popularCategory);
            RssLink latestCategory = new RssLink()
            {
                Name = "Senaste program",
                Url = "http://www.svtplay.se/api/latest_page?page={0}",
                HasSubCategories = false
            };
            Settings.Categories.Add(latestCategory);
            RssLink lastChanceCategory = new RssLink()
            {
                Name = "Sista chansen",
                Url = "http://www.svtplay.se/api/last_chance_page?page={0}",
                HasSubCategories = false
            };
            Settings.Categories.Add(lastChanceCategory);
            RssLink liveCategory = new RssLink()
            {
                Name = "Livesändningar",
                Url = "http://www.svtplay.se/api/live_page?page={0}",
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

        private List<Category> GetProgramCategories(Category parentCategory)
        {
            List<Category> cats = new List<Category>();
            Category aToZCategory = new Category()
            {
                Name = "Program A-Ö",
                HasSubCategories = true,
                ParentCategory = parentCategory
            };
            aToZCategory.Other = (Func<List<Category>>)(() => GetCategoriesFromRecomendationJson(aToZCategory, false));
            cats.Add(aToZCategory);
            string data = GetWebData("http://www.svtplay.se/ajax/sok/forslag.json");
            foreach (JToken recomendationToken in JArray.Parse(data))
            {
                if ((recomendationToken["isGenre"] != null && recomendationToken["isGenre"].Type != JTokenType.Null && recomendationToken["isGenre"].Value<string>() == "genre"))
                {
                    string url = recomendationToken["url"].Value<string>().Replace("/genre/", string.Empty).Replace("/", string.Empty);
                    string title = recomendationToken["title"].Value<string>();
                    string thumbnail = recomendationToken["thumbnail"].Value<string>();
                    RssLink tagCategory = new RssLink()
                    {
                        Name = title,
                        Url = url,
                        Thumb = thumbnail,
                        HasSubCategories = true,
                        ParentCategory = parentCategory
                    };
                    tagCategory.Other = (Func<List<Category>>)(() => GetTagCategories(tagCategory));
                    if (tagCategory.Url != "oppetarkiv"  && tagCategory.Url != "barn")
                        cats.Add(tagCategory);
                    else
                        cats.Insert(1, tagCategory);
                }
                else
                    break;
            }
            Category allTags = new Category()
            {
                Name = "Visa alla genrer",
                HasSubCategories = true,
                ParentCategory = parentCategory
            };
            allTags.Other = (Func<List<Category>>)(() => GetCategoriesFromRecomendationJson(allTags, true));
            cats.Add(allTags);
            return cats;
        }

        private List<Category> GetProgramCategoriesAndVideos(Category parentCategory)
        {
            List<Category> cats = new List<Category>();
            SvtCategory programs = new SvtCategory()
            {
                Name = "Avsnitt",
                Videos = new List<VideoInfo>(),
                ParentCategory = parentCategory,
                HasSubCategories = false,
                Url = "episodes"
            };
            cats.Add(programs);
            SvtCategory clips = new SvtCategory()
            {
                Name = "Klipp",
                Videos = new List<VideoInfo>(),
                ParentCategory = parentCategory,
                HasSubCategories = false,
                Url = "clipsResult"
            };
            cats.Add(clips);
            SvtCategory related = new SvtCategory()
            {
                Name = "Liknande program",
                Videos = new List<VideoInfo>(),
                ParentCategory = parentCategory,
                HasSubCategories = false,
                Url = "videosInSameCategory"
            };
            cats.Add(related);
            JToken json = GetWebData<JObject>(string.Format("http://www.svtplay.se/api/title_page?title={0}", (parentCategory as RssLink).Url))["relatedVideos"];
            foreach (SvtCategory cat in cats)
            {
                JToken jCat = json[cat.Url];
                JArray jVideos = null;
                if (jCat.Type == JTokenType.Array)
                    jVideos = jCat.Value<JArray>();
                else if (jCat["entries"] != null && jCat["entries"].Type != JTokenType.Null && jCat["entries"].Type == JTokenType.Array)
                    jVideos = jCat["entries"].Value<JArray>();
                cat.Videos = GetVideos(jVideos);
            }
            if (programs.Videos.Count == 0)
                cats.Remove(programs);
            if (clips.Videos.Count == 0)
                cats.Remove(clips);
            if (related.Videos.Count == 0)
                cats.Remove(related);
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
            if ((parentCategory as RssLink).Url != "barn" && (parentCategory as RssLink).Url != "oppetarkiv")
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
            }
            else if ((parentCategory as RssLink).Url == "oppetarkiv")
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
                data = GetWebData("http://www.svtplay.se/barn");
                Regex r = new Regex(@"<article(?:(?!data-title).)*data-title=""(?<title>[^""]*)(?:(?!data-description).)*data-description=""(?<description>[^""]*)(?:(?!<a\s+href).)*<a\s+href=""/(?!video)(?<url>[^""]*)(?:(?!src).)*src=""(?<thumb>[^""]*)", RegexOptions.Singleline);
                foreach (Match match in r.Matches(data))
                {
                    if (match.Success)
                    {
                        RssLink programCategoriesAndVideos = new RssLink()
                        {
                            Name = HttpUtility.HtmlDecode(match.Groups["title"].Value),
                            Url = match.Groups["url"].Value,
                            Description = HttpUtility.HtmlDecode(match.Groups["description"].Value),
                            Thumb = match.Groups["thumb"].Value,
                            HasSubCategories = true,
                            ParentCategory = contents
                        };
                        programCategoriesAndVideos.Other = (Func<List<Category>>)(() => GetProgramCategoriesAndVideos(programCategoriesAndVideos));
                        contents.SubCategories.Add(programCategoriesAndVideos);
                    }
                }
                cats.Add(contents);
            }
            if ((parentCategory as RssLink).Url != "oppetarkiv")
            {
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
                            Videos = GetVideos(jArray)
                        };
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

        private List<Category> GetCategoriesFromJArray(Category parentCategory, JArray array, bool getGenres)
        {
            List<Category> categories = new List<Category>();
            foreach (JToken element in array.Where(e => (!getGenres && e["isGenre"].Value<string>() != "genre" && !e["url"].Value<string>().StartsWith("/video/")) || (getGenres && e["isGenre"].Value<string>() == "genre")))
            {
                RssLink category = new RssLink()
                {
                    Name = element["title"].Value<string>(),
                    Thumb = element["thumbnail"].Value<string>(),
                    Url = element["url"].Value<string>().Replace("genre/", "").Replace("/", ""),
                    ParentCategory = parentCategory,
                    SubCategories = new List<Category>(),
                    HasSubCategories = true
                };
                category.Thumb = (category.Thumb.StartsWith("/") ? string.Concat("http://www.svtplay.se", category.Thumb) : category.Thumb);
                category.Other = (getGenres ? (Func<List<Category>>)(() => GetTagCategories(category)) : (Func<List<Category>>)(() => GetProgramCategoriesAndVideos(category)));
                categories.Add(category);
            }
            categories = categories.OrderBy(c => c.Name).ToList<Category>();
            return categories;
        }
        
        private List<Category> GetCategoriesFromRecomendationJson(Category parentCategory, bool getGenres)
        {
            string data = GetWebData<string>("http://www.svtplay.se/ajax/sok/forslag.json");
            JArray array = JArray.Parse(data);
            return GetCategoriesFromJArray(parentCategory, array, getGenres);
        }

        #endregion

        #region Videos

        public override List<VideoInfo> GetVideos(Category category)
        {
            HasNextPage = false;
            if (category is SvtCategory)
            {
                return (category as SvtCategory).Videos;
            }
            else if (category.GetOtherAsString() != "isChannels")
            {
                currentVideosUrl = (category as RssLink).Url;
                currentVideosPage = 1;
                bool hasNext = false;
                List<VideoInfo> videos = GetVideos(currentVideosUrl, currentVideosPage, out hasNext);
                HasNextPage = hasNext;
                return videos;
            }
            else
            {
                return GetChannelVideos();
            }
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
                hasNext = (json["paginationData"] != null && json["paginationData"].Type != JTokenType.Null && json["paginationData"]["totalPages"] != null && json["paginationData"]["totalPages"].Type == JTokenType.Integer && json["paginationData"]["totalPages"].Value<int>() > page);
                return GetVideos(json["videos"].Value<JArray>());
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
                            url = string.Format("http://www.svtplay.se/api/episodeIds?ids={0}", m.Groups["id"].Value);
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
                            DateTime dateTime = DateTime.Parse(date);
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
            DateTime dateTime;
            List<VideoInfo> videos = new List<VideoInfo>();
            JObject json = GetWebData<JObject>("http://www.svtplay.se/api/channel_page");
            foreach (JToken channel in (IEnumerable<JToken>)json["channels"])
            {
                VideoInfo video = new VideoInfo();
                video.Title = channel["name"].Value<string>();
                
                if (channel["videoReferences"] != null && channel["videoReferences"].Type == JTokenType.Array && channel["videoReferences"].Any<JToken>((JToken vr) => vr["playerType"].Value<string>() == "ios"))
                    video.VideoUrl = channel["videoReferences"].First<JToken>((JToken vr) => vr["playerType"].Value<string>() == "ios")["url"].Value<string>();
                if (channel["title"] != null && channel["title"].Type == JTokenType.String)
                    video.Thumb = string.Format("http://www.svtplay.se/public/images/channels/posters/{0}.png", channel["title"].Value<string>());
                video.Description = "";
                bool isFirst = true;
                foreach (JToken show in channel["schedule"])
                {
                    string start = "";
                    if (show["broadcastStartTime"] != null && show["broadcastStartTime"].Type != JTokenType.Null)
                    {
                        dateTime = DateTime.Parse(show["broadcastStartTime"].Value<string>());
                        start = dateTime.ToString("t", OnlineVideoSettings.Instance.Locale);
                    }
                    string end = "";
                    if (show["broadcastEndTime"] != null && show["broadcastEndTime"].Type != JTokenType.Null)
                    {
                        dateTime = DateTime.Parse(show["broadcastEndTime"].Value<string>());
                        end = dateTime.ToString("t", OnlineVideoSettings.Instance.Locale);
                    }
                    string title = "";
                    if (show["title"] != null && show["title"].Type == JTokenType.String)
                        title = show["title"].Value<string>();
                    string description = "";
                    if (show["description"] != null  && show["description"].Type == JTokenType.String)
                        description = show["description"].Value<string>();
                    video.Description += string.Format("{0}-{1}\n{2}\n{3}\n\n", start, end, title, description);
                    if (isFirst)
                    {
                        video.Title = string.Format("{0} - {1}", video.Title, title);
                        if (show["broadcastStartTime"] != null && show["broadcastStartTime"].Type != JTokenType.Null)
                        {
                            dateTime = DateTime.Parse(show["broadcastStartTime"].Value<string>());
                            video.Airdate = dateTime.ToString(OnlineVideoSettings.Instance.Locale);
                        }
                        if (show["titlePage"] != null && show["titlePage"].Type != JTokenType.Null &&  show["titlePage"]["thumbnailMedium"] != null && show["titlePage"]["thumbnailMedium"].Type == JTokenType.String)
                            video.Thumb = show["titlePage"]["thumbnailMedium"].Value<string>();
                        isFirst = false;
                    }
                }
                videos.Add(video);
            }
            return videos;
        }

        public override List<string> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            video.PlaybackOptions = new Dictionary<string, string>();
            string videoUrl = video.VideoUrl;
            if (videoUrl.EndsWith(".m3u8"))
            {
                MyHlsPlaylistParser parser = new MyHlsPlaylistParser(GetWebData(videoUrl), videoUrl);
                foreach (MyHlsStreamInfo streamInfo in parser.StreamInfos)
                    video.PlaybackOptions.Add(string.Format("{0}x{1} ({2}kbps)", streamInfo.Width, streamInfo.Height, streamInfo.Bandwidth), streamInfo.Url);
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
            string  url = video.PlaybackOptions.First<KeyValuePair<string, string>>().Value;
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
            JObject json = GetWebData<JObject>(string.Format("http://www.svtplay.se/api/search_page?q={0}", HttpUtility.UrlEncode(query)));
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
                        Url = item["urlPart"].Value<string>().Replace("genre/", "").Replace("/", ""),
                        Thumb = item["posterImageUrl"].Value<string>(),
                        HasSubCategories = true,
                        SubCategories = new List<Category>()
                    });

                }
                genres.SubCategories.ForEach(c => c.Other = (Func<List<Category>>)(() => GetTagCategories(c)));
                if (genres.SubCategories.Count > 0)
                    categories.Add(genres);
            }
            if (json["titles"] != null && json["titles"].Type == JTokenType.Array)
            {
                Category titles = new Category()
                {
                    Name = "Program",
                    HasSubCategories = true,
                    SubCategoriesDiscovered = true,
                    SubCategories = new List<Category>()
                };
                foreach (JToken item in json["titles"].Value<JArray>())
                {
                    titles.SubCategories.Add(new RssLink()
                    {
                        Name = item["programTitle"].Value<string>(),
                        Url = item["contentUrl"].Value<string>().Replace("/", ""),
                        Thumb = item["imageMedium"].Value<string>(),
                        Description = item["description"].Value<string>(),
                        HasSubCategories = true,
                        SubCategories = new List<Category>()
                    });

                }
                titles.SubCategories.ForEach(t => t.Other = (Func<List<Category>>)(() => GetProgramCategoriesAndVideos(t)));
                if (titles.SubCategories.Count > 0)
                    categories.Add(titles);
            }
            
            List<SvtCategory> videoCategories = new List<SvtCategory>();
            SvtCategory episodes = new SvtCategory()
            {
                Name = "Avsnitt",
                Videos = new List<VideoInfo>(),
                HasSubCategories = false,
                Url = "episodes"
            };
            videoCategories.Add(episodes);
            SvtCategory live = new SvtCategory()
            {
                Name = "Livesändningar",
                Videos = new List<VideoInfo>(),
                HasSubCategories = false,
                Url = "live"
            };
            videoCategories.Add(live);
            SvtCategory clips = new SvtCategory()
            {
                Name = "Klipp",
                Videos = new List<VideoInfo>(),
                HasSubCategories = false,
                Url = "clips"
            };
            videoCategories.Add(clips);
            SvtCategory openArchive = new SvtCategory()
            {
                Name = "Öppet arkiv",
                Videos = new List<VideoInfo>(),
                HasSubCategories = false,
                Url = "openArchive"
            };
            videoCategories.Add(openArchive);
            videoCategories.ForEach((cat) => ((SvtCategory)cat).Videos = GetVideos(json[cat.Url].Value<JArray>()));
            if (episodes.Videos.Count == 0)
                videoCategories.Remove(episodes);
            if (clips.Videos.Count == 0)
                videoCategories.Remove(clips);
            if (live.Videos.Count == 0)
                videoCategories.Remove(live);
            if (openArchive.Videos.Count == 0)
                videoCategories.Remove(openArchive);
            categories.AddRange(videoCategories);
            categories.ForEach(c => searchResultItems.Add(c));
            return searchResultItems;
        }

        #endregion

        #region Latest

        public override List<VideoInfo> GetLatestVideos()
        {
            bool dummy = false;
            List<VideoInfo> videos = GetVideos("http://www.svtplay.se/api/latest_page?page={0}", 1, out dummy);
            return (videos.Count >= LatestVideosCount ? videos.GetRange(0, (int)LatestVideosCount) : new List<VideoInfo>());
        }

        #endregion

    }
}
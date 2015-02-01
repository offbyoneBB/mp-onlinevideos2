using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Specialized;
using System.Net;
using OnlineVideos.Sites.Utils;

namespace OnlineVideos.Sites
{
    public class HboNordicWebUtil : SiteUtilBase, IBrowserSiteUtil
    {
        #region Config

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("E-mail"), Description("HBO Nordic username e-mail")]
        protected string username = null;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Password"), Description("HBO Nordic password"), PasswordPropertyText(true)]
        protected string password = null;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("In English please"), Description("Get titles and descriptions in english (does not affect subtitles).")]
        protected bool useEnglish = false;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Enable HD on 1st play/pause"), Description("(Try to) Enable HD the first time play/pause is pressed when playing video, let the video play for 20-30 seconds first.")]
        protected bool enableHd = false;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Show forum category"), Description("Enable or disable forum category (Link to forum - http://tinyurl.com/olv-hbonordic)")]
        protected bool showHelpCategory = true;

        #endregion

        #region Vars

        private const string baseUrl = "http://hbonordic.com/rest-services-hook";
        private const string moviesUrl = "/thin/movies";
        private const string seriesUrl = "/series";

        #endregion

        #region BrowserSiteUtil

        public string UserName
        {
            get
            {
                return username;
            }
        }

        public string Password
        {
            get
            {
                return password;
            }
        }

        public string ConnectorEntityTypeName
        {
            get
            {
                return "OnlineVideos.Sites.BrowserUtilConnectors.HboNordicConnector";
            }
        }

        #endregion

        #region Localization

        protected string SeriesCategoryName
        {
            get
            {
                string series = "Series";
                if (!useEnglish)
                {
                    switch (Settings.Language)
                    {
                        case "sv":
                            series = "Serier";
                            break;
                        case "da":
                            series = "Serier";
                            break;
                        case "fi":
                            series = "TV-Sarjat";
                            break;
                        case "no":
                            series = "Serier";
                            break;
                        default:
                            break;
                    }
                }
                return series;
            }
        }

        protected string SeasonCategoryNamePrefix
        {
            get
            {
                string series = "Season";
                if (!useEnglish)
                {
                    switch (Settings.Language)
                    {
                        case "sv":
                            series = "Säsong";
                            break;
                        case "da":
                            series = "Sæson";
                            break;
                        case "fi":
                            series = "Kausi";
                            break;
                        case "no":
                            series = "Sesong";
                            break;
                        default:
                            break;
                    }
                }
                return series;
            }
        }

        protected string MoviesCategoryName
        {
            get
            {
                string movies = "Movies";
                if (!useEnglish)
                {
                    switch (Settings.Language)
                    {
                        case "sv":
                            movies = "Film";
                            break;
                        case "da":
                            movies = "Film";
                            break;
                        case "fi":
                            movies = "Elokuvat";
                            break;
                        case "no":
                            movies = "Filmer";
                            break;
                        default:
                            break;
                    }
                }
                return movies;
            }
        }

        protected string Language
        {
            get
            {
                string lang = "en_US";
                if (!useEnglish)
                {
                    switch (Settings.Language)
                    {
                        case "sv":
                            lang = "sv_SE";
                            break;
                        case "da":
                            lang = "da_DK";
                            break;
                        case "fi":
                            lang = "fi_FI";
                            break;
                        case "no":
                            lang = "nb_NO";
                            break;
                        default:
                            break;
                    }
                }
                return lang;
            }
        }

        #endregion

        #region Helpers

        protected T MyGetWebData<T>(string url)
        {
            CookieContainer cc = new CookieContainer();
            CookieCollection ccol = new CookieCollection();
            Cookie langcookie = new Cookie("GUEST_LANGUAGE_ID", Language, "/", "hbonordic.com");
            cc.Add(langcookie);
            return GetWebData<T>(url, cookies: cc);
        }

        #endregion

        #region SiteUtil

        #region Category

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            RssLink seriesCat = new RssLink()
            {
                Name = SeriesCategoryName,
                HasSubCategories = true,
                SubCategories = new List<Category>()
            };

            string data = MyGetWebData<string>(baseUrl + seriesUrl);
            JArray series = (JArray)JsonConvert.DeserializeObject(data);
            foreach (JToken serie in series)
            {
                seriesCat.SubCategories.Add(new RssLink()
                {
                    Name = serie["title"].Value<string>(),
                    Thumb = serie["poster"].Value<string>(),
                    Description = serie["info"].Value<string>(),
                    HasSubCategories = true,
                    SubCategories = new List<Category>(),
                    ParentCategory = seriesCat,
                    Url = baseUrl + seriesUrl + "/" + serie["id"].Value<string>()
                });
            }
            seriesCat.EstimatedVideoCount = (uint)seriesCat.SubCategories.Count();
            seriesCat.SubCategoriesDiscovered = seriesCat.EstimatedVideoCount > 0;
            Settings.Categories.Add(seriesCat);

            RssLink moviesCat = new RssLink()
            {
                Name = MoviesCategoryName,
                HasSubCategories = false
            };
            Settings.Categories.Add(moviesCat);

            if (showHelpCategory)
            {
                Settings.Categories.Add(new RssLink()
                {
                    Name = "Forum",
                    Other = "Forum"
                });
            }

            Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > 0;
            return Settings.Categories.Count();
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            JObject series = MyGetWebData<JObject>((parentCategory as RssLink).Url);
            foreach (JToken seasonInfo in series["season_info"])
            {
                var season = seasonInfo.First().Value<JToken>();
                parentCategory.SubCategories.Add(new RssLink()
                {
                    Name = SeasonCategoryNamePrefix + " " + season["season_number"].Value<string>(),
                    Thumb = season["season_image_url"].Value<string>(),
                    ParentCategory = parentCategory,
                    HasSubCategories = false,
                    Other = new List<VideoInfo>()
                });
            }
            (parentCategory as RssLink).EstimatedVideoCount = (uint)parentCategory.SubCategories.Count();
            foreach (JToken episode in series["episodes"])
            {
                (parentCategory.SubCategories.First(c => c.Name == SeasonCategoryNamePrefix + " " + episode["season"].Value<string>()).Other as List<VideoInfo>).Add(new VideoInfo()
                {
                    Title = string.Format("{0}. {1}", episode["episode_number"].Value<string>(), episode["title"].Value<string>()),
                    ImageUrl = episode["thumb"].Value<string>(),
                    Description = episode["info"].Value<string>(),
                    VideoUrl = episode["url"].Value<string>()
                });
            }
            parentCategory.SubCategories.ForEach(c => (c as RssLink).EstimatedVideoCount = (uint)(c.Other as List<VideoInfo>).Count());
            parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count() > 0;
            return parentCategory.SubCategories.Count();
        }
        #endregion

        #region Videos

        public override List<VideoInfo> GetVideos(Category category)
        {
            if (category.Other is string && (category.Other as string) == "Forum")
                throw new OnlineVideosException("Forum http://tinyurl.com/olv-hbonordic");
            List<VideoInfo> videos = new List<VideoInfo>();
            if (category.Name == MoviesCategoryName)
            {
                JObject movieGroups = MyGetWebData<JObject>(baseUrl + moviesUrl);
                foreach (JToken movieGroup in movieGroups["entry"].First())
                {
                    foreach (JToken movie in movieGroup.First())
                    {
                        videos.Add(new VideoInfo()
                        {
                            Title = movie["title"].Value<string>(),
                            ImageUrl = movie["thumb"].Value<string>(),
                            Description = movie["description"].Value<string>(),
                            VideoUrl = movie["uri"].Value<string>()
                        });
                    }
                }
                (category as RssLink).EstimatedVideoCount = (uint)videos.Count;
            }
            else if (category.Other != null && category.Other is List<VideoInfo>)
            {
                videos = category.Other as List<VideoInfo>;
            }
            return videos;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || !HelperUtils.IsValidEmail(username))
                throw new OnlineVideosException("Please enter username and password");
            return video.VideoUrl + ((enableHd) ? "DOENABLEHD" : "");
        }

        #endregion

        #endregion
    }
}
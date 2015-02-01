using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;

namespace OnlineVideos.Sites
{
    //todo:
    //discoversubs of tvseasonlist

    public class TVLinksccUtil : GenericSiteUtil
    {
        private enum CatType
        {
            NewMovies, HotMovies, MovieGenres, MoviesAlph,
            NewTvshows, BestTvshows, TvshowGenres, TvshowsAlph, TvshowList, TvSeasonList,
            FreeNewMovies, FreeHotMovies, /*MovieGenres,*/ FreeMoviesAlph
        };

        [Category("OnlineVideosUserConfiguration"), Description("Your login name")]
        string username = string.Empty;
        [Category("OnlineVideosUserConfiguration"), Description("Your login password"), PasswordPropertyText(true)]
        string password = string.Empty;

        private RegexOptions defaultDataRegexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.ExplicitCapture;

        private string sRegexMovieDataAirdate = @"<div class=""tit"">{0}\s*?(?:&nbsp;)?\s*?\((?<Year>\d\d\d\d)\).*?</div>";

        private CookieContainer siteCookies = null;

        Regex regexNewMovies;
        Regex regexHotMovies;
        Regex regexDefaultVideoList;
        Regex regexMovieGenres;
        Regex regexTvShowGenres;
        Regex regexMoviesAlph;
        Regex regexTvShowsAlph;
        Regex regexTvShowList;
        Regex regexNewTvShows;
        Regex regexBestTvShows;

        Regex regexTvShowDataThumb;
        Regex regexTvShowDataPlot;
        Regex regexTvShowDataAirdate;
        Regex regexMovieDataAirdate;

        public override void Initialize(SiteSettings siteSettings)
        {
            regexNewMovies = new Regex(@"(?<!class=""title_h"".*)((<div[^>]*>\s*<div\sclass=""pic""><a\shref=""(?<VideoUrl>[^""]*)""[^>]*><img\ssrc=""(?<ImageUrl>[^""]*)""[^>]*></a></div>\s*<div\sclass=""show""><a[^>]*>(?<Title>[^<]*)</a>&nbsp;<font\scolor=""\#FF6600""><sup></sup></font></div>)|(<li><a\shref=""(?<VideoUrl>[^""]*)""[^>]*?title=""(?<Title>[^""]*)""[^>]*?>[^<]*?\((?<Airdate>[^\)]*)\)</a>(?:&nbsp;)?<font\scolor=""\#FF6600""><sup>.*?</sup></font></li>))", defaultRegexOptions);
            regexHotMovies = new Regex(@"(?<=class=""title_h"".*)((<div[^>]*>\s*<div\sclass=""pic""><a\shref=""(?<VideoUrl>[^""]*)""[^>]*><img\ssrc=""(?<ImageUrl>[^""]*)""[^>]*></a></div>\s*<div\sclass=""show""><a[^>]*>(?<Title>[^<]*)</a>&nbsp;<font\scolor=""\#FF6600""><sup></sup></font></div>)|(<li><a\shref=""(?<VideoUrl>[^""]*)""[^>]*?title=""(?<Title>[^""]*)""[^>]*?>[^<]*?\((?<Airdate>[^\)]*)\)</a>(?:&nbsp;)?<font\scolor=""\#FF6600""><sup>.*?</sup></font></li>))", defaultRegexOptions);
            regexDefaultVideoList = new Regex(@"<li><a\shref=""(?<VideoUrl>[^""]*)""\stitle=""[^>]*>(?<Title>[^<]*)</a>\s*?\((?<Airdate>[^\)]*)\)\s*?</a>&nbsp;<font\scolor=""\#FF6600""><sup></sup></font></li>", defaultRegexOptions);
            regexMovieGenres = new Regex(@"<td\s[^>]*>(?:&nbsp;)?\s<a\shref=""(?<url>[^""]*)"">(?<title>[^<]*)</a></td>", defaultRegexOptions);
            regexTvShowGenres = new Regex(@"<td\s[^>]*>(?:&nbsp;)?\s<a\shref=""(?<url>[^""]*)"">(?<title>[^<]*)</a></td>", defaultRegexOptions);
            regexMoviesAlph = new Regex(@"<a\shref='(?<url>[^']*)'>(?<title>[^<]*)</a>", defaultRegexOptions);
            regexTvShowsAlph = new Regex(@"<a\shref='(?<url>[^']*)'>(?<title>[^<]*)</a>", defaultRegexOptions);
            regexTvShowList = new Regex(@"<li><a\shref=""(?<url>[^""]*)""\stitle=""[^""]*"">(?<title>[^<]*)</a>\s*(?:</a>)?</li>", defaultRegexOptions);
            regexNewTvShows = new Regex(@"(?<!class=""title_h"".*)((<div[^>]*>\s*<div\sclass=""pic""><a\shref=""(?<url>[^""]*)""\stitle=""[^""]*""><img\ssrc=""(?<thumb>[^""]*)""[^>]*></a></div>\s*<div\sclass=""show""><a[^>]*>(?<title>[^<]*)</a></div>\s*</div>\s*)|(<li><a\shref=""(?<url>[^""]*)""\stitle=""[^""]*"">(?<title>[^<]*)</a></li>))", defaultRegexOptions);
            regexBestTvShows = new Regex(@"(?<=class=""title_h"".*)((<div[^>]*>\s*<div\sclass=""pic""><a\shref=""(?<url>[^""]*)""\stitle=""[^""]*""><img\ssrc=""(?<thumb>[^""]*)""[^>]*></a></div>\s*<div\sclass=""show""><a[^>]*>(?<title>[^<]*)</a></div>\s*</div>\s*)|(<li><a\shref=""(?<url>[^""]*)""\stitle=""[^""]*"">(?<title>[^<]*)</a></li>))", defaultRegexOptions);

            regexTvShowDataThumb = new Regex(@"<div\sclass=""left"">\s*?<img src=""(?<Thumb>[^""]*)""\s*?(width=""[^""]*"")?\s*?(height=""[^""]*"")?\s*?/>\s*?</div>", defaultDataRegexOptions);
            regexTvShowDataPlot = new Regex(@"<div\sclass=""plot"">\s*([Pp]lot)?:?\s*(?<Plot>.*?)\s*?</div>", defaultDataRegexOptions);
            regexTvShowDataAirdate = new Regex(@"<div>[Dd]ate:\s*?(?<Year>\d\d\d\d)\s*?</div>", defaultDataRegexOptions);


            base.Initialize(siteSettings);
        }

        public override int DiscoverDynamicCategories()
        {
            //movies
            RssLink cat = new RssLink();
            cat.Name = "Movies";
            cat.Url = @"http://www.tvlinks.cc/movie.htm";
            Settings.Categories.Add(cat);
            addSubcat("Newly added", cat, false).Other = CatType.NewMovies;
            addSubcat("Hot movies", cat, false).Other = CatType.HotMovies;
            addSubcat("Genres", cat, true).Other = CatType.MovieGenres;
            addSubcat("Alphabetical", cat, true).Other = CatType.MoviesAlph;
            cat.SubCategoriesDiscovered = true;

            //tv shows
            cat = new RssLink();
            cat.Name = "TV Shows";
            cat.Url = @"http://www.tvlinks.cc/tv.htm";
            Settings.Categories.Add(cat);
            addSubcat("Newly added", cat, true).Other = CatType.NewTvshows;
            addSubcat("Best rated", cat, true).Other = CatType.BestTvshows;
            addSubcat("Genres", cat, true).Other = CatType.TvshowGenres;
            addSubcat("Alphabetical", cat, true).Other = CatType.TvshowsAlph;
            cat.SubCategoriesDiscovered = true;

            //free movies
            cat = new RssLink();
            cat.Name = "Free Movies";
            cat.Url = @"http://www.tvlinks.cc/freemovies/freemovie.htm";
            Settings.Categories.Add(cat);
            addSubcat("Newly added", cat, false).Other = CatType.FreeNewMovies;
            addSubcat("Hot movies", cat, false).Other = CatType.FreeHotMovies;
            //addSubcat("Genres", cat, true).Other = CatType.MovieGenres;
            addSubcat("Alphabetical", cat, true).Other = CatType.FreeMoviesAlph;
            cat.SubCategoriesDiscovered = true;

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }


        private RssLink addSubcat(string name, Category parentCat, bool hasSubs)
        {
            RssLink cat = new RssLink();
            cat.Name = name;
            cat.Url = ((RssLink)parentCat).Url;
            cat.ParentCategory = parentCat;
            cat.HasSubCategories = hasSubs;
            if (parentCat.SubCategories == null)
                parentCat.SubCategories = new List<Category>();
            parentCat.SubCategories.Add(cat);
            parentCat.HasSubCategories = true;
            return cat;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            if (category.Other is string) return GetTvSeasonVideos(category);
            else if (category.Other is List<VideoInfo>) return category.Other as List<VideoInfo>;
            bool replaceBadLinks = false;
            switch (category.Other as CatType?)
            {
                case CatType.NewMovies:
                    regEx_VideoList = regexNewMovies; break;
                case CatType.FreeNewMovies:
                    regEx_VideoList = regexNewMovies; replaceBadLinks = true; break;
                case CatType.HotMovies:
                    regEx_VideoList = regexHotMovies; break;
                case CatType.FreeHotMovies:
                    regEx_VideoList = regexHotMovies; replaceBadLinks = true; break;
                default:
                    regEx_VideoList = regexDefaultVideoList; break;
            }
            List<VideoInfo> result = base.GetVideos(category);
            if (replaceBadLinks && result != null && result.Count > 0)
            {
                foreach (VideoInfo vid in result)
                {
                    vid.VideoUrl = Regex.Replace(vid.VideoUrl, "/freemovies", "", RegexOptions.IgnoreCase);
                }
            }
            return result;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            switch (parentCategory.Other as CatType?)
            {
                case null: return 0;
                case CatType.MovieGenres:
                    {
                        regEx_dynamicSubCategories = regexMovieGenres;
                        return base.DiscoverSubCategories(parentCategory);
                    }
                case CatType.TvshowGenres:
                    return GetTvShowSubcats(parentCategory, CatType.TvshowList, regexTvShowGenres);

                case CatType.MoviesAlph:
                case CatType.FreeMoviesAlph:
                    {
                        regEx_dynamicSubCategories = regexMoviesAlph;
                        return base.DiscoverSubCategories(parentCategory);
                    }
                case CatType.TvshowsAlph:
                    return GetTvShowSubcats(parentCategory, CatType.TvshowList, regexTvShowsAlph);

                case CatType.TvshowList:
                    return GetTvShowSubcats(parentCategory, CatType.TvSeasonList, regexTvShowList);

                case CatType.NewTvshows:
                    return GetTvShowSubcats(parentCategory, CatType.TvSeasonList, regexNewTvShows);

                case CatType.BestTvshows:
                    return GetTvShowSubcats(parentCategory, CatType.TvSeasonList, regexBestTvShows);

                case CatType.TvSeasonList:
                    return GetTvSeasons((RssLink)parentCategory);
            }
            return 0;
        }

        private int GetTvShowSubcats(Category parentCategory, CatType newCatType, Regex regEx)
        {
            regEx_dynamicSubCategories = regEx;
            int res = base.ParseSubCategories(parentCategory, parentCategory.Other as string);
            foreach (Category cat in parentCategory.SubCategories)
            {
                cat.HasSubCategories = true;
                cat.Other = newCatType;
            }
            return res;
        }

        private List<VideoInfo> GetTvSeasonVideos(Category category)
        {
            string s = category.Other as string;
            List<VideoInfo> result = new List<VideoInfo>();
            string[] vids = s.Split(new[] { "%=+=%" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string vid in vids)
                if (vid.StartsWith("/"))
                {
                    string[] parts = vid.Split(new[] { "*|=*" }, StringSplitOptions.RemoveEmptyEntries);
                    VideoInfo video = new VideoInfo();

                    string sName = category.ParentCategory.Name;
                    string sSeason = category.Name.ToLower().Replace("season", "").Trim();
                    uint iSeason;
                    if (!uint.TryParse(sSeason, out iSeason))
                        iSeason = 0;
                    uint iEpisode;
                    if (!uint.TryParse(parts[2], out iEpisode))
                        iEpisode = 0;

                    video.Title = string.Format("{0}x{1:00} {2}", iSeason, iEpisode, parts[3]);
                    video.VideoUrl = new Uri(new Uri(baseUrl), parts[0]).AbsoluteUri;

                    video.ImageUrl = category.ParentCategory.Thumb;
                    video.Description = category.ParentCategory.Description;
                    //video.Airdate = //

                    if (!string.IsNullOrEmpty(sName) && iEpisode > 0 && iSeason > 0)
                    {
                        video.Other = new TrackingInfo()
                        {
                            Episode = iEpisode,
                            Season = iSeason,
                            Title = sName,
                            VideoKind = VideoKind.TvSeries
                        };
                    }

                    result.Add(video);
                }
            return result;
        }

        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            if (video.Other is ITrackingInfo) return video.Other as ITrackingInfo;
            else return base.GetTrackingInfo(video);
        }

        private static string GetSubString(string s, string start, string until)
        {
            int p = s.IndexOf(start);
            if (p == -1) return String.Empty;
            p += start.Length;
            if (until == null) return s.Substring(p);
            int q = s.IndexOf(until, p);
            if (q == -1) return s.Substring(p);
            return s.Substring(p, q - p);
        }

        private void SetCookies()
        {
            if (siteCookies != null)
            {
                bool bExpired = false;
                foreach (Cookie cook in siteCookies.GetCookies(new Uri(baseUrl)))
                {
                    if (cook.Expired)
                    {
                        bExpired = true;
                        break;
                    }
                }
                if (bExpired || siteCookies.Count < 1)
                    siteCookies = null;
            }

            if (siteCookies == null)
            {
                CookieContainer tempCookies = new CookieContainer();
                GetWebData("http://www.tvlinks.cc/checkin.php?action=login", string.Format("username={0}&password={1}&submit_button=Login", username, password), tempCookies);

                siteCookies = new CookieContainer();

                foreach (Cookie cook in tempCookies.GetCookies(new Uri(baseUrl)))
                {
                    siteCookies.Add(new Uri(baseUrl), cook);
                }
            }
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            SetCookies();

            string code;
            if (!video.VideoUrl.EndsWith(".htm", StringComparison.InvariantCultureIgnoreCase))
            {
                //here we can set more video info - description, airdate, plot
                if (string.IsNullOrEmpty(video.ThumbnailImage) || string.IsNullOrEmpty(video.Description) || string.IsNullOrEmpty(video.Airdate))
                {
                    string sData = GetWebData(video.VideoUrl);
                    if (!string.IsNullOrEmpty(sData))
                    {
                        Match m;
                        if (string.IsNullOrEmpty(video.ImageUrl))
                        {
                            m = regexTvShowDataThumb.Match(sData);
                            if (m.Success)
                            {
                                video.ImageUrl = m.Groups["Thumb"].Value;
                                video.ThumbnailImage = video.ImageUrl; //so it shows in OSD
                            }
                        }
                        if (string.IsNullOrEmpty(video.Description))
                        {
                            m = regexTvShowDataPlot.Match(sData);
                            if (m.Success)
                                video.Description = m.Groups["Plot"].Value;
                        }
                        if (string.IsNullOrEmpty(video.Airdate))
                        {
                            regexMovieDataAirdate = new Regex(string.Format(sRegexMovieDataAirdate, video.Title), defaultDataRegexOptions);
                            m = regexMovieDataAirdate.Match(sData);
                            if (m.Success)
                                video.Airdate = m.Groups["Year"].Value;
                        }
                    }
                }

                uint year;
                if (!uint.TryParse(video.Airdate, out year))
                    year = 0;

                if (!string.IsNullOrEmpty(video.Title) && year > 1900)
                {
                    video.Other = new TrackingInfo()
                    {
                        Title = video.Title,
                        Year = year,
                        VideoKind = OnlineVideos.VideoKind.Movie
                    };
                }

                string webData = GetWebData(video.VideoUrl + "/play.htm");
                code = GetSubString(webData, "code=", "|");
            }
            else
                code = Path.GetFileNameWithoutExtension(video.VideoUrl).Substring(4);

            string part1 = GetWebData(@"http://www.tvlinks.cc/checkpts.php?code=" + code, cookies: siteCookies).Trim();
            if (part1.Length < 5) //try to re-login
            {
                siteCookies = null;
                SetCookies();
                part1 = GetWebData(@"http://www.tvlinks.cc/checkpts.php?code=" + code, cookies: siteCookies).Trim();
            }

            long ticks = DateTime.UtcNow.Ticks;
            long t3 = (ticks - 621355968000000000) / 10000;
            string time = t3.ToString();
            string token = part1.Substring(part1.LastIndexOf('/') + 1);

            string hash = "kekute**$%2009";
            hash = Utils.GetMD5Hash(hash);
            string s1 = Utils.GetMD5Hash(hash.Substring(0, 16));
            string s2 = Utils.GetMD5Hash(hash.Substring(16, 16));
            string s3 = Utils.GetMD5Hash(token);
            string key = Utils.GetMD5Hash(time + s1 + s3 + s2).Substring(5, 27);

            return String.Format("{0}?start=0&key={1}&time={2}", part1, key, time);
        }

        private int GetTvSeasons(RssLink parentCategory)
        {
            //if (string.IsNullOrEmpty(parentCategory.Thumb) || string.IsNullOrEmpty(parentCategory.Description))
            //{
            string sData = GetWebData(parentCategory.Url);
            if (!string.IsNullOrEmpty(sData))
            {
                Match m;
                if (string.IsNullOrEmpty(parentCategory.Thumb))
                {
                    m = regexTvShowDataThumb.Match(sData);
                    if (m.Success)
                        parentCategory.Thumb = m.Groups["Thumb"].Value;
                }
                if (string.IsNullOrEmpty(parentCategory.Description))
                {
                    m = regexTvShowDataPlot.Match(sData);
                    if (m.Success)
                        parentCategory.Description = m.Groups["Plot"].Value;
                }
                if (string.IsNullOrEmpty(parentCategory.Other as string))
                {
                    m = regexTvShowDataAirdate.Match(sData);
                    if (m.Success)
                        parentCategory.Other = m.Groups["Year"].Value;
                }
            }
            //}


            string data = GetWebData(parentCategory.Url + "/url.js");
            string[] seasons = data.Split(new[] { "urlarray" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in seasons)
            {
                if (s.StartsWith("["))
                {
                    int p = s.IndexOf(']');
                    string seasonNr = s.Substring(1, p - 1);
                    addSubcat("Season " + seasonNr, parentCategory, false).Other = s.Substring(p + 1);
                }
            }
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        public override bool CanSearch { get { return true; } }

        public override Dictionary<string, string> GetSearchableCategories()
        {
            Dictionary<string, string> result = Settings.Categories.Select(a => a.Name).Where(a => a != "Free Movies").ToDictionary<string, string>(a => a);
            return result;
        }

        List<ISearchResultItem> DoSearch(string query)
        {
            List<ISearchResultItem> result = new List<ISearchResultItem>();

            string searchData = GetWebData(searchUrl, string.Format(searchPostString, query), allowUnsafeHeader: allowUnsafeHeaders);
            if (!string.IsNullOrEmpty(searchData))
            {
                RssLink catMovies = new RssLink();
                catMovies.Name = "Movies";
                catMovies.Url = @"";

                List<VideoInfo> movies;
                Regex regEx_VideoList_Copy = regEx_VideoList;
                regEx_VideoList = regexNewMovies;
                try
                {
                    movies = Parse(searchUrl, searchData);
                    if (movies != null && movies.Count > 0)
                    {
                        catMovies.EstimatedVideoCount = movies.Count < 0 ? 0 : (uint)movies.Count;
                        catMovies.Other = movies;
                        result.Add(catMovies);
                    }
                }
                finally
                {
                    regEx_VideoList = regEx_VideoList_Copy;
                }

                RssLink catTVShows = new RssLink();
                catTVShows.Name = "TV Shows";
                catTVShows.Url = @"";
                catTVShows.HasSubCategories = true;
                catTVShows.Other = searchData;

                GetTvShowSubcats(catTVShows, CatType.TvSeasonList, regexTvShowList);

                if (catTVShows.SubCategories != null && catTVShows.SubCategories.Count > 0)
                {
                    catTVShows.EstimatedVideoCount = catTVShows.SubCategories.Count < 0 ? 0 : (uint)catTVShows.SubCategories.Count;
                    result.Add(catTVShows);
                }
            }

            return result;
        }

        public override List<ISearchResultItem> Search(string query, string category = null)
        {
            List<ISearchResultItem> result = DoSearch(query);
            if (!string.IsNullOrEmpty(category))
            {
                if (result != null && result.Count > 0)
                {
                    foreach (Category cat in result)
                    {
                        if (cat.Name.Equals(category))
                        {
                            if (cat.Other is List<VideoInfo>) //movies
                            {
                                return (cat.Other as List<VideoInfo>).ConvertAll<ISearchResultItem>(v => v as ISearchResultItem);
                            }
                            else //tvshows
                            {
                                return (cat.SubCategories).ConvertAll<ISearchResultItem>(v => v as ISearchResultItem);
                            }
                        }
                    }
                }
            }
            return new List<ISearchResultItem>();
        }
    }
}
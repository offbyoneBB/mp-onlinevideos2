using OnlineVideos.Hoster;
using OnlineVideos.Subtitles;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{

    public class LosMoviesUtil : LatestVideosSiteUtilBase
    {
        public class LosMoviesVideoInfo : VideoInfo
        {
            public string LatestOption { get; private set; }
            public ITrackingInfo TrackingInfo { get; set; }
            public override string GetPlaybackOptionUrl(string option)
            {
                string u = this.PlaybackOptions[option];
                this.LatestOption = option;
                Hoster.HosterBase hoster = Hoster.HosterFactory.GetAllHosters().FirstOrDefault(h => u.ToLowerInvariant().Contains(h.GetHosterUrl().ToLowerInvariant()));
                if (hoster != null)
                {
                    string theUrl = hoster.GetVideoUrl(u);
                    if (hoster is ISubtitle && string.IsNullOrWhiteSpace(SubtitleText))
                        this.SubtitleText = (hoster as ISubtitle).SubtitleText;
                    return theUrl;
                }
                return "";
            }
        }

        [Category("OnlineVideosUserConfiguration"), Description("Select subtitle source, for example: TvSubtitles")]
        [TypeConverter(typeof(SubtitleSourceConverter))]
        protected string subtitleSource = "";
        [Category("OnlineVideosUserConfiguration"), Description("Select subtitle language preferences (; separated and ISO 639-2), for example: eng;ger")]
        protected string subtitleLanguages = "";

        private const string baseUrl = "http://los-movies.com";
        private string nextPageUrl = "";
        private string currentCategoryThumb = "";

        private SubtitleHandler sh = null;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            sh = new SubtitleHandler(subtitleSource, subtitleLanguages);
        }

        public override int DiscoverDynamicCategories()
        {
            Category movies = new Category()
            {
                Name = "Movies",
                HasSubCategories = true,
                SubCategoriesDiscovered = true,
            };
            movies.SubCategories = new List<Category>()
            {
                new RssLink()
                {
                    Name = "Genres",
                    HasSubCategories = true,
                    Url = "/movie-genres",
                    ParentCategory = movies,
                    Other = "movies"
                },
                new RssLink()
                {
                    Name = "Countries",
                    HasSubCategories = true,
                    Url = "/countries",
                    ParentCategory = movies,
                    Other = "movies"
                },
                new RssLink()
                {
                    Name = "Popular",
                    HasSubCategories = false,
                    Url = "/",
                    ParentCategory = movies
                },
                new RssLink()
                {
                    Name = "Latest",
                    HasSubCategories = false,
                    Url = "/latest-movies/",
                    ParentCategory = movies
                },
                new RssLink()
                {
                    Name = "HD Movies",
                    HasSubCategories = false,
                    Url = "/watch-movies-in-hd/",
                    ParentCategory = movies
                },
                new RssLink()
                {
                    Name = "3D Movies",
                    HasSubCategories = false,
                    Url = "/watch-3D-movies/",
                    ParentCategory = movies
                },
                new RssLink()
                {
                    Name = "New Releases",
                    HasSubCategories = false,
                    Url = "/watch-new-release-movies/",
                    ParentCategory = movies
                }
            };
            Settings.Categories.Add(movies);

            Category shows = new Category()
            {
                Name = "TV Shows",
                HasSubCategories = true,
                SubCategoriesDiscovered = true,
            };
            shows.SubCategories = new List<Category>()
            {
                 new RssLink()
                {
                    Name = "Genres",
                    HasSubCategories = true,
                    Url = "/movie-genres",
                    ParentCategory = shows,
                    Other = "shows"
                },
                new RssLink()
                {
                    Name = "Countries",
                    HasSubCategories = true,
                    Url = "/countries",
                    ParentCategory = shows,
                    Other = "shows"
                },
                new RssLink()
                {
                    Name = "Popular",
                    HasSubCategories = true,
                    Url = "/watch-popular-tv-shows/",
                    ParentCategory = shows
                },
                new RssLink()
                {
                    Name = "Latest",
                    HasSubCategories = true,
                    Url = "/watch-latest-tv-shows/",
                    ParentCategory = shows
                },
                new RssLink()
                {
                    Name = "HD TV Shows",
                    HasSubCategories = true,
                    Url = "/watch-tv-shows-in-hd/",
                    ParentCategory = shows
                },
                new RssLink()
                {
                    Name = "3D TV Shows",
                    HasSubCategories = true,
                    Url = "/watch-3D-tv-shows/",
                    ParentCategory = shows
                },
                new RssLink()
                {
                    Name = "New Releases",
                    HasSubCategories = true,
                    Url = "/watch-new-release-tv-shows/",
                    ParentCategory = shows
                }
            };
            Settings.Categories.Add(shows);

            Settings.DynamicCategoriesDiscovered = true;
            return 2;
        }

        private string GetImdbId(string data)
        {
            Regex r = new Regex(@"https{0,1}://www.imdb.com/title/(?<imdb>tt\d+)");
            Match m = r.Match(data);
            if (!m.Success)
                return "";
            return m.Groups["imdb"].Value;
        }

        private uint GetRelesaseYear(string data)
        {
            Regex r = new Regex(@"showValueRelease"">(?<y>\d\d\d\d)<");
            uint y = 0;
            Match m = r.Match(data);
            if (m.Success)
            {
                uint.TryParse(m.Groups["y"].Value, out y);
            }
            return y;
        }

        private string GetNextLink(string data)
        {
            Regex r = new Regex(@"<a href=""(?<u>[^""]*)"" class=""nextLink"">");
            Match match = r.Match(data);
            if (match.Success)
            {
                return baseUrl + match.Groups["u"].Value;
            }
            return "";
        }

        private List<Category> DiscoverSubCategoriesFromListing(string url)
        {
            List<Category> cats = new List<Category>();
            string data = GetWebData(url);
            Regex r = new Regex(@"<div id=""movie-.*?""movieQuality[^>]*?>(?<q>[^<]*)<.*?<a href=""(?<u>[^""]*)"".*?<img src=""(?<i>[^""]*)"".*?>(?<n>[^<]*)</h4", RegexOptions.Singleline);
            foreach (Match m in r.Matches(data))
            {
                RssLink cat = new RssLink()
                {
                    Name = m.Groups["n"].Value,
                    Thumb = m.Groups["i"].Value,
                    Url = m.Groups["u"].Value,
                    Description = "Quality: " + m.Groups["q"].Value.Trim(),
                    HasSubCategories = false
                };
                cats.Add(cat);
            }
            string nextUrl = GetNextLink(data);
            if (!string.IsNullOrEmpty(nextUrl))
            {
                NextPageCategory next = new NextPageCategory();
                next.Url = nextUrl;
                cats.Add(next);
            }
            return cats;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string other = parentCategory.GetOtherAsString();
            string url = baseUrl + (parentCategory as RssLink).Url;
            if (string.IsNullOrEmpty(other))
            {
                parentCategory.SubCategories = DiscoverSubCategoriesFromListing(url);
            }
            else
            {
                string data = GetWebData(url);
                Regex r = new Regex(@"showRowText"">(?<n>.*?)</div>.*?<a href=""(?<u>[^""]*?-" + other + @")""", RegexOptions.Singleline);
                parentCategory.SubCategories = new List<Category>();
                foreach (Match m in r.Matches(data))
                {
                    RssLink cat = new RssLink()
                    {
                        Name = m.Groups["n"].Value,
                        Url = m.Groups["u"].Value,
                        HasSubCategories = other == "shows"
                    };
                    parentCategory.SubCategories.Add(cat);
                }
            }
            parentCategory.SubCategories.ForEach(c => c.ParentCategory = parentCategory);
            parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count() > 0;
            return parentCategory.SubCategories.Count();
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            Category parent = category.ParentCategory;
            parent.SubCategories.Remove(category);
            List<Category> cats = DiscoverSubCategoriesFromListing(category.Url);
            cats.ForEach(c => c.ParentCategory = parent);
            parent.SubCategories.AddRange(cats);
            return cats.Count;
        }

        private List<VideoInfo> GetVideos(string url, bool checkForNext, Category category = null)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            string data = GetWebData(url);
            if (category != null)
            {
                currentCategoryThumb = category.Thumb;
            }
            Regex r = new Regex(@"<h3>Watch Online:(?<n>.*?)Season (?<s>\d*)? Serie (?<e>\d*)?.*?</h3.*?(?<t><table.*?</table>)", RegexOptions.Singleline);
            Match match = r.Match(data);
            if (match.Success)
            {
                // Show page:
                string imdb = GetImdbId(data);
                uint year = GetRelesaseYear(data);

                foreach (Match m in r.Matches(data))
                {
                    LosMoviesVideoInfo video = new LosMoviesVideoInfo()
                    {
                        Title = m.Groups["n"].Value.Trim() + " " + m.Groups["s"].Value + "x" + m.Groups["e"].Value,
                        Other = m.Groups["t"].Value,
                        Thumb = currentCategoryThumb
                    };
                    video.TrackingInfo = new TrackingInfo()
                    {
                        ID_IMDB = imdb,
                        Title = m.Groups["n"].Value.Trim(),
                        Season = uint.Parse(m.Groups["s"].Value),
                        Episode = uint.Parse(m.Groups["e"].Value),
                        VideoKind = VideoKind.TvSeries,
                        Year = year
                    };
                    videos.Add(video);
                }
            }
            else
            {
                if (category != null)
                {
                    r = new Regex(@"<tr class=""linkTr"">.*?""linkQuality[^>]*>(?<q>[^<]*)<.*?linkHiddenUrl[^>]*>(?<u>[^<]*)<.*?</tr", RegexOptions.Singleline);
                    match = r.Match(data);
                    if (match.Success)
                    {
                        //Movie page
                        LosMoviesVideoInfo video = new LosMoviesVideoInfo()
                        {
                            Title = category.Name,
                            Thumb = currentCategoryThumb,
                            VideoUrl = url,
                            Description = category.Description,
                            TrackingInfo = null
                        };
                        video.TrackingInfo = new TrackingInfo()
                        {
                            ID_IMDB = GetImdbId(data),
                            Title = video.Title,
                            VideoKind = VideoKind.Movie,
                            Year = GetRelesaseYear(data)
                        };
                        videos.Add(video);
                    }
                }
                if (videos.Count == 0)
                {
                    //Movie listing
                    r = new Regex(@"<div id=""movie-.*?""movieQuality[^>]*?>(?<q>[^<]*)<.*?<a href=""(?<u>[^""]*)"".*?<img src=""(?<i>[^""]*)"".*?>(?<n>[^<]*)</h4", RegexOptions.Singleline);
                    foreach (Match m in r.Matches(data))
                    {
                        LosMoviesVideoInfo video = new LosMoviesVideoInfo()
                        {
                            Title = m.Groups["n"].Value,
                            Thumb = m.Groups["i"].Value,
                            VideoUrl = baseUrl + m.Groups["u"].Value,
                            Description = "Quality: " + m.Groups["q"].Value.Trim(),
                            TrackingInfo = new TrackingInfo()
                            {
                                Title = m.Groups["n"].Value.Trim(),
                                VideoKind = VideoKind.Movie
                            }
                        };
                        videos.Add(video);
                    }
                }
            }
            if (checkForNext)
            {
                nextPageUrl = GetNextLink(data);
                HasNextPage = !string.IsNullOrEmpty(nextPageUrl);
            }
            return videos;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            HasNextPage = false;
            List<VideoInfo> videos = GetVideos(baseUrl + (category as RssLink).Url, true, category);
            return videos;
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            HasNextPage = false;
            List<VideoInfo> videos = GetVideos(nextPageUrl, true);
            videos.ForEach((v) =>
            {
                if (string.IsNullOrEmpty(v.Thumb))
                    v.Thumb = currentCategoryThumb;
            });
            return videos;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string data;
            if (video.Other is string && !string.IsNullOrWhiteSpace(video.GetOtherAsString()))
                data = video.GetOtherAsString();
            else
                data = GetWebData(video.VideoUrl);
            Regex r = new Regex(@"<tr class=""linkTr"">.*?""linkQuality[^>]*>(?<q>[^<]*)<.*?linkHiddenUrl[^>]*>(?<u>[^<]*)<.*?</tr", RegexOptions.Singleline);
            Dictionary<string, string> d = new Dictionary<string, string>();
            List<Hoster.HosterBase> hosters = Hoster.HosterFactory.GetAllHosters();
            foreach (Match m in r.Matches(data))
            {
                string u = m.Groups["u"].Value;
                if (u.StartsWith("//"))
                    u = "http:" + u;
                Hoster.HosterBase hoster = hosters.FirstOrDefault(h => u.ToLowerInvariant().Contains(h.GetHosterUrl().ToLowerInvariant()));
                if (hoster != null)
                {
                    string format = hoster.GetHosterUrl() + " [" + m.Groups["q"].Value + "] " + "({0})";
                    int count = 1;
                    while (d.ContainsKey(string.Format(format, count)))
                    {
                        count++;
                    }
                    d.Add(string.Format(format, count), u);
                }
            }
            d = d.OrderBy((p) =>
            {
                return p.Key;
            }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            video.PlaybackOptions = d;
            if (d.Count == 0)
                return "";
            if ((video as LosMoviesVideoInfo).TrackingInfo == null)
                (video as LosMoviesVideoInfo).TrackingInfo = new TrackingInfo();
            var ti = (video as LosMoviesVideoInfo).TrackingInfo;
            if (string.IsNullOrEmpty(ti.ID_IMDB))
            {
                ti.VideoKind = VideoKind.Movie;
                ti.Title = video.Title;
                ti.Year = GetRelesaseYear(data);
                ti.ID_IMDB = GetImdbId(data);
            }
            sh.SetSubtitleText(video, GetTrackingInfo, false);
            string latestOption = (video is LosMoviesVideoInfo) ? (video as LosMoviesVideoInfo).LatestOption : "";
            if (string.IsNullOrEmpty(latestOption))
                return d.First().Value;
            if (d.ContainsKey(latestOption))
                return d[latestOption];
            return d.First().Value;
        }

        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            if (video is LosMoviesVideoInfo && (video as LosMoviesVideoInfo).TrackingInfo != null)
            {
                return (video as LosMoviesVideoInfo).TrackingInfo;
            }
            return base.GetTrackingInfo(video);
        }

        public override bool CanSearch { get { return true; } }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            string url = baseUrl + "/search?type=movies&q=" + HttpUtility.UrlEncode(query);
            List<SearchResultItem> result = new List<SearchResultItem>();
            GetVideos(url, false).ForEach(v => result.Add(v));
            return result;
        }

        public override List<VideoInfo> GetLatestVideos()
        {
            List<VideoInfo> videos = GetVideos(baseUrl + "/latest-movies", false);
            return videos.Count >= LatestVideosCount ? videos.GetRange(0, (int)LatestVideosCount) : new List<VideoInfo>();
        }
    }
}

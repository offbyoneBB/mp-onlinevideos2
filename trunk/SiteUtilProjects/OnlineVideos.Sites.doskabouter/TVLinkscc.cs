using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace OnlineVideos.Sites
{
    //todo:
    //discoversubs of tvseasonlist

    public class TVLinksccUtil : GenericSiteUtil
    {
        private enum CatType
        {
            NewMovies, HotMovies, MovieGenres, MoviesAlph,
            NewTvshows, BestTvshows, TvshowGenres, TvshowsAlph, TvshowList, TvSeasonList
        };


        private RegexOptions defaultRegexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture;
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

        public override void Initialize(SiteSettings siteSettings)
        {
            regexNewMovies = new Regex(@"(?<!class=""title_h"".*)((<div[^>]*>\s*<div\sclass=""pic""><a\shref=""(?<VideoUrl>[^""]*)""[^>]*><img\ssrc=""(?<ImageUrl>[^""]*)""[^>]*></a></div>\s*<div\sclass=""show""><a[^>]*>(?<Title>[^<]*)</a>&nbsp;<font\scolor=""\#FF6600""><sup></sup></font></div>)|(<li><a\shref=""(?<VideoUrl>[^""]*)""[^>]*>(?<Title>[^&]*)&nbsp;\((?<Airdate>[^\)]*)\)</a>&nbsp;<font\scolor=""\#FF6600""><sup></sup></font></li>))", defaultRegexOptions);
            regexHotMovies = new Regex(@"(?<=class=""title_h"".*)((<div[^>]*>\s*<div\sclass=""pic""><a\shref=""(?<VideoUrl>[^""]*)""[^>]*><img\ssrc=""(?<ImageUrl>[^""]*)""[^>]*></a></div>\s*<div\sclass=""show""><a[^>]*>(?<Title>[^<]*)</a>&nbsp;<font\scolor=""\#FF6600""><sup></sup></font></div>)|(<li><a\shref=""(?<VideoUrl>[^""]*)""[^>]*>(?<Title>[^&]*)&nbsp;\((?<Airdate>[^\)]*)\)</a>&nbsp;<font\scolor=""\#FF6600""><sup></sup></font></li>))", defaultRegexOptions);
            regexDefaultVideoList = new Regex(@"<li><a\shref=""(?<VideoUrl>[^""]*)""\stitle=""[^>]*>(?<Title>[^<]*)</a>\s(?<Airdate>[^<]*)</a>&nbsp;<font\scolor=""\#FF6600""><sup></sup></font></li>", defaultRegexOptions);
            regexMovieGenres = new Regex(@"<td\s[^>]*>(?:&nbsp;)?\s<a\shref=""(?<url>[^""]*)"">(?<title>[^<]*)</a></td>", defaultRegexOptions);
            regexTvShowGenres = new Regex(@"<td\s[^>]*>(?:&nbsp;)?\s<a\shref=""(?<url>[^""]*)"">(?<title>[^<]*)</a></td>", defaultRegexOptions);
            regexMoviesAlph = new Regex(@"<a\shref='(?<url>[^']*)'>(?<title>[^<]*)</a>", defaultRegexOptions);
            regexTvShowsAlph = new Regex(@"<a\shref='(?<url>[^']*)'>(?<title>[^<]*)</a>", defaultRegexOptions);
            regexTvShowList = new Regex(@"<li><a\shref=""(?<url>[^""]*)""\stitle=""[^""]*"">(?<title>[^<]*)</a>\s*</a></li>", defaultRegexOptions);
            regexNewTvShows = new Regex(@"(?<!class=""title_h"".*)((<div[^>]*>\s*<div\sclass=""pic""><a\shref=""(?<url>[^""]*)""\stitle=""[^""]*""><img\ssrc=""(?<thumb>[^""]*)""[^>]*></a></div>\s*<div\sclass=""show""><a[^>]*>(?<title>[^<]*)</a></div>\s*</div>\s*)|(<li><a\shref=""(?<url>[^""]*)""\stitle=""[^""]*"">(?<title>[^<]*)</a></li>))", defaultRegexOptions);
            regexBestTvShows = new Regex(@"(?<=class=""title_h"".*)((<div[^>]*>\s*<div\sclass=""pic""><a\shref=""(?<url>[^""]*)""\stitle=""[^""]*""><img\ssrc=""(?<thumb>[^""]*)""[^>]*></a></div>\s*<div\sclass=""show""><a[^>]*>(?<title>[^<]*)</a></div>\s*</div>\s*)|(<li><a\shref=""(?<url>[^""]*)""\stitle=""[^""]*"">(?<title>[^<]*)</a></li>))", defaultRegexOptions);
            base.Initialize(siteSettings);
        }

        public override int DiscoverDynamicCategories()
        {
            RssLink cat = new RssLink();
            cat.Name = "Movies";
            cat.Url = @"http://www.tvlinks.cc/movie.htm";
            Settings.Categories.Add(cat);

            addSubcat("Newly added", cat, false).Other = CatType.NewMovies;
            addSubcat("Hot movies", cat, false).Other = CatType.HotMovies;

            addSubcat("Genres", cat, true).Other = CatType.MovieGenres;
            addSubcat("Alphabetical", cat, true).Other = CatType.MoviesAlph;

            cat.SubCategoriesDiscovered = true;

            cat = new RssLink();
            cat.Name = "TV Shows";
            cat.Url = @"http://www.tvlinks.cc/tv.htm";
            Settings.Categories.Add(cat);

            addSubcat("Newly added", cat, true).Other = CatType.NewTvshows;
            addSubcat("Best rated", cat, true).Other = CatType.BestTvshows;

            addSubcat("Genres", cat, true).Other = CatType.TvshowGenres;

            addSubcat("Alphabetical", cat, true).Other = CatType.TvshowsAlph;

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

        public override List<VideoInfo> getVideoList(Category category)
        {
            if (category.Other is string) return GetTvSeasonVideos(category);
            switch (category.Other as CatType?)
            {
                case CatType.NewMovies:
                    regEx_VideoList = regexNewMovies; break;
                case CatType.HotMovies:
                    regEx_VideoList = regexHotMovies; break;
                default:
                    regEx_VideoList = regexDefaultVideoList; break;
            }
            return base.getVideoList(category);
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
            int res = base.DiscoverSubCategories(parentCategory);
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

                    video.Title = "Episode " + parts[2] + " " + parts[3];
                    video.VideoUrl = new Uri(new Uri(baseUrl), parts[0]).AbsoluteUri;

                    try
                    {
                        video.Other = new TrackingInfo() 
                        { 
                            Episode = uint.Parse(parts[2]), 
                            Season = uint.Parse(category.Name.ToLower().Replace("season", "").Trim()), 
                            Title = category.ParentCategory.Name, 
                            VideoKind = VideoKind.TvSeries 
                        };
                    }
                    catch { }

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

        public override string getUrl(VideoInfo video)
        {
            string code;
            if (!video.VideoUrl.EndsWith(".htm", StringComparison.InvariantCultureIgnoreCase))
            {
                string webData = GetWebData(video.VideoUrl + "/play.htm");
                code = GetSubString(webData, "code=", "|");
            }
            else
                code = Path.GetFileNameWithoutExtension(video.VideoUrl).Substring(4);

            string part1 = GetWebData(@"http://www.tvlinks.cc/checkpts.php?code=" + code).Trim();

            long ticks = DateTime.UtcNow.Ticks;
            long t3 = (ticks - 621355968000000000) / 10000;
            string time = t3.ToString();
            string token = part1.Substring(part1.LastIndexOf('/') + 1);

            string hash = "kekute**$%2009";
            hash = GetMD5Hash(hash);
            string s1 = GetMD5Hash(hash.Substring(0, 16));
            string s2 = GetMD5Hash(hash.Substring(16, 16));
            string s3 = GetMD5Hash(token);
            string key = GetMD5Hash(time + s1 + s3 + s2).Substring(5, 27);

            return String.Format("{0}?start=0&key={1}&time={2}", part1, key, time);
        }

        private string GetMD5Hash(string input)
        {
            System.Security.Cryptography.MD5 md5Hasher;
            byte[] data;
            int count;
            StringBuilder result;

            md5Hasher = System.Security.Cryptography.MD5.Create();
            data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));

            // Loop through each byte of the hashed data and format each one as a hexadecimal string.
            result = new StringBuilder();
            for (count = 0; count < data.Length; count++)
            {
                result.Append(data[count].ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
            }

            return result.ToString();
        }

        private int GetTvSeasons(RssLink parentCategory)
        {
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
    }
}
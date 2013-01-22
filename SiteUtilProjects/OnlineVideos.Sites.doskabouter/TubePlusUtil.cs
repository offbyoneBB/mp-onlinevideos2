using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Web;
using System.IO;

namespace OnlineVideos.Sites
{
    public class TubePlusUtil : DeferredResolveUtil
    {

        [Category("OnlineVideosConfiguration"), Description("")]
        protected string genresRegEx;

        [Category("OnlineVideosConfiguration"), Description("")]
        protected string aZSubCategoriesRegEx;

        [Category("OnlineVideosConfiguration"), Description("")]
        protected string seriesSubCategoriesRegEx;

        [Category("OnlineVideosConfiguration"), Description("")]
        protected string seasonCategoriesRegEx;

        [Category("OnlineVideosConfiguration"), Description("")]
        protected string showVideoListRegEx;

        [Category("OnlineVideosConfiguration"), Description("")]
        protected string moviesVideoListRegEx;

        private Regex regex_Genres;
        private Regex regex_AZSubCategories;
        private Regex regex_SeriesSubCategories;
        private Regex regex_SeasonCategories;
        private Regex regex_ShowVideoList;
        private Regex regex_MoviesVideoList;

        private enum Mode
        {
            Show,
            ShowGenres,
            ShowAZ,
            Series,
            Season,
            ShowTop10,

            Movies,
            MovieGenres,
            MoviesAZ,
            MoviesTop10
        };
        private const string allGenresName = "All Genres";

        private string userAgent = @"Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.0; Trident/4.0; SLCC1; .NET CLR 2.0.50727; Media Center PC 5.0; .NET CLR 3.5.30729; .NET CLR 3.0.30729; .NET4.0C; .NET4.0E)";

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regex_Genres = new Regex(genresRegEx, defaultRegexOptions);
            regex_AZSubCategories = new Regex(aZSubCategoriesRegEx, defaultRegexOptions);
            regex_SeriesSubCategories = new Regex(seriesSubCategoriesRegEx, defaultRegexOptions);
            regex_SeasonCategories = new Regex(seasonCategoriesRegEx, defaultRegexOptions);
            regex_ShowVideoList = new Regex(showVideoListRegEx, defaultRegexOptions);
            regex_MoviesVideoList = new Regex(moviesVideoListRegEx, defaultRegexOptions);
        }

        public override int DiscoverDynamicCategories()
        {

            int res = base.DiscoverDynamicCategories();
            foreach (Category cat in Settings.Categories)
            {
                cat.HasSubCategories = true;
                if (cat.Name.ToLower().Contains("movies"))
                    cat.Other = Mode.Movies;
                else
                    cat.Other = Mode.Show;
            }
            return res;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string parentUrl = (parentCategory as RssLink).Url;
            string data = GetWebData(parentUrl, userAgent: userAgent);
            Mode parentMode = (Mode)parentCategory.Other;

            switch (parentMode)
            {
                case Mode.Show:
                case Mode.Movies:
                    {
                        regEx_dynamicSubCategories = regex_Genres;
                        parentCategory.SubCategories = new List<Category>();

                        RssLink top10 = new RssLink()
                        {
                            Name = "Top 10",
                            Url = baseUrl,
                            ParentCategory = parentCategory,
                            Other = parentMode == Mode.Show ? Mode.ShowTop10 : Mode.MoviesTop10,
                            HasSubCategories = parentMode == Mode.Show
                        };
                        parentCategory.SubCategories.Add(top10);
                        RssLink latest = new RssLink()
                        {
                            Name = "Latest",
                            Url = parentUrl,
                            ParentCategory = parentCategory,
                            Other = parentMode == Mode.Show ? Mode.ShowAZ : Mode.MoviesAZ,
                            HasSubCategories = parentMode == Mode.Show
                        };
                        parentCategory.SubCategories.Add(latest);

                        RssLink allGenres = new RssLink()
                        {
                            Name = allGenresName,
                            Url = parentUrl,
                            ParentCategory = parentCategory,
                            HasSubCategories = true,
                            Other = parentMode == Mode.Show ? Mode.ShowGenres : Mode.MovieGenres
                        };
                        parentCategory.SubCategories.Add(allGenres);

                        int result = base.ParseSubCategories(parentCategory, data);
                        for (int i = 3; i < parentCategory.SubCategories.Count; i++)
                        {
                            RssLink cat = (RssLink)parentCategory.SubCategories[i];
                            cat.Url = parentUrl.Replace(@"/Last/", '/' + cat.Name + '/');
                            cat.Other = parentMode == Mode.Show ? Mode.ShowGenres : Mode.MovieGenres;
                            cat.HasSubCategories = true;
                        }
                        return result;
                    }
                case Mode.ShowGenres:
                case Mode.MovieGenres:
                    {
                        regEx_dynamicSubCategories = regex_AZSubCategories;
                        parentCategory.SubCategories = new List<Category>();

                        if (parentCategory.Name != allGenresName)
                        {
                            RssLink all = new RssLink()
                            {
                                Name = "All",
                                Url = parentUrl,
                                ParentCategory = parentCategory,
                            };
                            parentCategory.SubCategories.Add(all);
                        }

                        int result = base.ParseSubCategories(parentCategory, data);
                        foreach (RssLink cat in parentCategory.SubCategories)
                        {
                            if (parentMode == Mode.ShowGenres)
                            {
                                cat.Other = Mode.ShowAZ;
                                cat.HasSubCategories = true;
                            }
                            else
                            {
                                cat.Other = Mode.MoviesAZ;
                            }
                        }
                        return result;
                    }
                case Mode.ShowAZ:
                    {
                        regEx_dynamicSubCategories = regex_SeriesSubCategories;
                        int result = base.ParseSubCategories(parentCategory, data);
                        foreach (RssLink cat in parentCategory.SubCategories)
                        {
                            cat.HasSubCategories = true;
                            cat.Other = Mode.Series;
                        }
                        return result;
                    }
                case Mode.Series:
                    {
                        parentCategory.SubCategories = new List<Category>();
                        Match m = regex_SeasonCategories.Match(data);
                        while (m.Success)
                        {
                            RssLink cat = new RssLink();
                            cat.Url = m.Groups["url"].Value;
                            if (!Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(baseUrl), cat.Url).AbsoluteUri;
                            cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim().Replace('\n', ' '));
                            cat.Thumb = m.Groups["thumb"].Value;
                            if (!String.IsNullOrEmpty(cat.Thumb) && !Uri.IsWellFormedUriString(cat.Thumb, System.UriKind.Absolute)) cat.Thumb = new Uri(new Uri(baseUrl), cat.Thumb).AbsoluteUri;
                            cat.Description = m.Groups["description"].Value;

                            string[] airdates = m.Groups["airdates"].Value.Replace(",", "||").Split(new[] { @"||" }, StringSplitOptions.RemoveEmptyEntries);

                            List<VideoInfo> videoList = GetSeriesVideoList(baseUrl, m.Groups["urls"].Value, parentCategory);

                            for (int i = 0; i < videoList.Count && i + 1 < airdates.Length; i++)
                            {
                                string airdate = airdates[i + 1].Trim('"');
                                int p = airdate.LastIndexOf('_');
                                if (p < 0) p = 0;
                                airdate = airdate.Substring(p + 1);
                                if (!String.IsNullOrEmpty(airdate))
                                    videoList[i].Length = '|' + Translation.Instance.Airdate + ": " + airdate;
                            }
                            cat.EstimatedVideoCount = (uint)videoList.Count;
                            cat.Other = videoList;
                            cat.ParentCategory = parentCategory;
                            parentCategory.SubCategories.Add(cat);
                            m = m.NextMatch();
                        }
                        parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
                        return parentCategory.SubCategories.Count;
                    }
                case Mode.ShowTop10:
                    {
                        regEx_dynamicSubCategories = regex_SeriesSubCategories;
                        data = data.Split(new[] { @"<h1 id=""list_head"" class=""short"">" }, StringSplitOptions.None)[2];
                        int result = base.ParseSubCategories(parentCategory, data);
                        foreach (RssLink cat in parentCategory.SubCategories)
                        {
                            cat.HasSubCategories = true;
                            cat.Other = Mode.Series;
                        }
                        return result;
                    }

            }
            return 0;
        }

        private List<VideoInfo> GetSeriesVideoList(string url, string data, Category parentCategory)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();
            if (data.Length > 0)
            {
                Match m = regex_ShowVideoList.Match(data);
                while (m.Success)
                {
                    VideoInfo videoInfo = CreateVideoInfo();
                    videoInfo.Title = HttpUtility.HtmlDecode(m.Groups["Title"].Value);
                    // get, format and if needed absolutify the video url
                    videoInfo.VideoUrl = FormatDecodeAbsolutifyUrl(url, m.Groups["VideoUrl"].Value, videoListRegExFormatString, videoListUrlDecoding);

                    try
                    {
                        string id = m.Groups["id"].Value;
                        Match mid = Regex.Match(id, "s(?<season>[^e]*)e(?<episode>.*)");
                        if (mid.Success)
                        {
                            TrackingInfo tInfo = new TrackingInfo();
                            tInfo.Title = parentCategory.Name;
                            tInfo.Season = Convert.ToUInt32(mid.Groups["season"].Value);
                            tInfo.Episode = Convert.ToUInt32(mid.Groups["episode"].Value);
                            tInfo.VideoKind = VideoKind.TvSeries;
                            videoInfo.Other = tInfo;
                        }
                    }
                    catch { };
                    videoList.Add(videoInfo);

                    m = m.NextMatch();
                }
            }

            return videoList;
        }



        public override List<VideoInfo> getVideoList(Category category)
        {
            if (category.Other is List<VideoInfo>) //tvshows
                return (List<VideoInfo>)category.Other;
            regEx_VideoList = regex_MoviesVideoList;
            return base.getVideoList(category);
        }

        public override string FormatHosterName(string name)
        {
            if (name.StartsWith("glink "))
                return name.Substring(5) + " on google";
            else
                return name.Substring(4);
        }

        public override string FormatHosterUrl(string name)
        {

            // on tubeplus it's always http://<hoster>/<id>
            //source for conversion: http://www.tubeplus.me/resources/js/player2.js 
            // doesn't really work, putlocker&sockshare needs file instead of embed, and gorillavid needs nothing
            string[] parts = name.Split('/');
            string id = parts[parts.Length - 1];
            string hoster = parts[parts.Length - 2].ToLowerInvariant();

            if (hoster == "youtube") return "http://youtube.com/v/" + id;
            if (hoster == "putlocker.com" || hoster == "sockshare.com")
                return String.Format(@"http://www.{0}/embed/{1}", hoster, id);
            if (hoster == "videoweed.es")
                return @"http://embed.videoweed.es/embed.php?v=" + id;
            if (hoster == "movshare.net")
                return @"http://embed.movshare.net/embed.php?v=" + id;
            if (hoster == "novamov.com")
                return @"http://embed.novamov.com/embed.php?width=653&height=525&px=1&v=" + id;
            if (hoster == "vidbull.com")
                return String.Format(@"http://vidbull.com/embed-{0}-650x328.html", id);
            if (hoster == "divxstage.eu")
                return @"http://embed.divxstage.eu/embed.php?&width=653&height=438&v=" + id;
            return name;
        }

        public override string GetFileNameForDownload(VideoInfo video, Category category, string url)
        {
            if (string.IsNullOrEmpty(url)) // called for adding to favorites
                return video.Title;
            else // called for downloading
            {
                string name = base.GetFileNameForDownload(video, category, url);
                string extension = Path.GetExtension(name);
                if (String.IsNullOrEmpty(extension) || !OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(extension))
                    name += ".flv";
                if (category.ParentCategory != null && category.ParentCategory.Other.Equals(Mode.Series))
                {
                    string season = category.Name.Split('(')[0];
                    name = category.ParentCategory.Name + ' ' + season + ' ' + name;
                    int l;
                    do
                    {
                        l = name.Length;
                        name = name.Replace("  ", " ");
                    } while (l != name.Length);

                }
                return Utils.GetSaveFilename(name);
            }
        }


        #region search
        public override bool CanSearch
        {
            get { return true; }
        }

        public override List<ISearchResultItem> DoSearch(string query)
        {
            List<ISearchResultItem> result = new List<ISearchResultItem>();
            RssLink cat = new RssLink()
            {
                Name = "Movies",
                Other = Mode.MoviesAZ,
                Url = String.Format(@"http://www.tubeplus.me/search/movies/{0}/0/", encodeQuery(query))
            };
            result.Add(cat);

            cat = new RssLink()
            {
                Name = "TV",
                Other = Mode.ShowAZ,
                Url = String.Format(@"http://www.tubeplus.me/search/tv-shows/{0}/0/", encodeQuery(query)),
                HasSubCategories = true
            };
            result.Add(cat);
            return result;
        }

        private string encodeQuery(string query)
        {
            return HttpUtility.UrlEncode(query.Replace(' ', '_'));
        }
        #endregion

        public static bool GotTrackingInfoData(string name, int season, int episode, int year)
        {
            return (!string.IsNullOrEmpty(name) && ((season > -1 && episode > -1) || (year > 1900)));
        }

        public static void FillTrackingInfoData(Match trackingInfoMatch, ref string name, ref int season, ref int episode, ref int year)
        {
            if (trackingInfoMatch != null && trackingInfoMatch.Success)
            {
                name = trackingInfoMatch.Groups["name"].Value.Trim();
                if (!int.TryParse(trackingInfoMatch.Groups["season"].Value, out season))
                {
                    season = -1;
                }
                if (!int.TryParse(trackingInfoMatch.Groups["episode"].Value, out episode))
                {
                    episode = -1;
                }
                if (!int.TryParse(trackingInfoMatch.Groups["year"].Value, out year))
                {
                    year = -1;
                }
            }
        }
    }
}

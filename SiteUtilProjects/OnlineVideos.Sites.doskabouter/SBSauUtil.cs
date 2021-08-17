using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json.Linq;
using OnlineVideos.Helpers;

namespace OnlineVideos.Sites
{
    public class SBSauUtil : GenericSiteUtil
    {
        private DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        private enum Kind { MoviesRoot, SeriesRoot, OneSerie, MovieGenre };

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Add(new RssLink() { Name = "Series", Url = @"https://www.sbs.com.au/api/video_programs/all", HasSubCategories = true, Other = Kind.SeriesRoot });
            Settings.Categories.Add(new RssLink() { Name = "Movies", Url = "https://www.sbs.com.au/ondemandcms/sitenav", HasSubCategories = true, Other = Kind.MoviesRoot });
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        private void AddMovieCat(Category parentCat, string name)
        {
            parentCat.SubCategories = new List<Category>();
            Category cat = new RssLink()
            {
                Name = name,
                ParentCategory = parentCat,
                HasSubCategories = true,
                Url = String.Format(
                    @"https://www.sbs.com.au/api/video_feed/f/Bgtm9B/sbs-section-programs/?form=json&count=true&sort=metrics.viewCount.last7Days%7Cdesc&range=1-16&byCategories=Section%2FPrograms,Film,Film%2F{0}&byRatings=&facets=1",
                    name)
            };
            parentCat.SubCategories.Add(cat);
            parentCat.SubCategoriesDiscovered = true;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            switch ((Kind)parentCategory.Other)
            {
                case Kind.MoviesRoot: return GetMovieSubcats(parentCategory);
                case Kind.SeriesRoot: return GetSeriesSubcats(parentCategory);
                case Kind.OneSerie:
                    {
                        var data = GetWebData<JObject>(((RssLink)parentCategory).Url);
                        SortedList<int, SortedList<int, VideoInfo>> list = new SortedList<int, SortedList<int, VideoInfo>>();

                        foreach (var vid in data["entries"])
                        {
                            VideoInfo video = parseVideo(vid);

                            int season = vid.Value<int>("pl1$season");
                            if (!list.ContainsKey(season))
                                list.Add(season, new SortedList<int, VideoInfo>());
                            list[season].Add(vid.Value<int>("pl1$episodeNumber"), video);

                        }

                        foreach (var item in list)
                            parentCategory.SubCategories.Add(new Category()
                            {
                                Name = "Season " + item.Key.ToString(),
                                Other = new List<VideoInfo>(item.Value.Values),
                                ParentCategory = parentCategory
                            });
                        parentCategory.SubCategoriesDiscovered = true;
                        return parentCategory.SubCategories.Count;
                    }
            }
            return 0;
        }

        private int GetSeriesSubcats(Category parentCat)
        {
            var data = GetWebData<JObject>(((RssLink)parentCat).Url);
            SortedDictionary<char, Category> list = new SortedDictionary<char, Category>();

            foreach (var entry in data["entries"])
            {
                if (entry.Value<String>("type") == "program_series")
                {
                    RssLink categ = new RssLink()
                    {
                        Name = entry.Value<String>("name"),
                        Description = entry.Value<String>("descrption"),
                        Thumb = entry["thumbnails"].First.First.Value<String>(),
                        Url = entry.Value<String>("url"),
                        SubCategories = new List<Category>(),
                        HasSubCategories = true,
                        Other = Kind.OneSerie
                    };
                    if (!String.IsNullOrEmpty(categ.Url))
                    {
                        Char first = categ.Name[0];
                        if (Char.IsDigit(first))
                            first = '#';
                        else
                            first = Char.ToUpper(first);
                        if (!list.ContainsKey(first))
                        {
                            Category cat = new Category()
                            {
                                Name = first.ToString(),
                                SubCategories = new List<Category>(),
                                HasSubCategories = true,
                                SubCategoriesDiscovered = true,
                                ParentCategory = parentCat
                            };
                            list.Add(first, cat);
                        }
                        categ.ParentCategory = list[first];
                        list[first].SubCategories.Add(categ);
                    }
                }
            }
            parentCat.SubCategories = new List<Category>();
            foreach (var item in list)
                parentCat.SubCategories.Add(item.Value);
            parentCat.SubCategoriesDiscovered = true;
            return parentCat.SubCategories.Count;
        }

        private int GetMovieSubcats(Category parentCat)
        {
            var data = GetWebData(((RssLink)parentCat).Url);
            var match = Regex.Match(data, @"{""title"":""(?<title>[^""]*)"",""href"":(?<url>""movies\\[^}]*)}", defaultRegexOptions);
            parentCat.SubCategories = new List<Category>();
            while (match.Success)
            {
                parentCat.SubCategories.Add(new RssLink()
                {
                    Name = match.Groups["title"].Value,
                    Url = String.Format(@"https://www.sbs.com.au/ondemandcms/sections/{0}",
                        Newtonsoft.Json.JsonConvert.DeserializeObject<string>(match.Groups["url"].Value)),
                    ParentCategory = parentCat,
                    Other = Kind.MovieGenre
                });
                match = match.NextMatch();
            }
            parentCat.SubCategoriesDiscovered = true;
            return parentCat.SubCategories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            if (category.Other is List<VideoInfo>)
                return (List<VideoInfo>)category.Other;
            else
                return MovieVideos(category);
        }

        private List<VideoInfo> MovieVideos(Category category)
        {
            var data = GetWebData(((RssLink)category).Url);
            Match m = Regex.Match(data, @"data-filter=""(?<url>[^""]*)""", defaultRegexOptions);
            List<VideoInfo> result = new List<VideoInfo>();
            if (m.Success)
            {
                var url = String.Format(@"https://www.sbs.com.au/api/video_feed/f/Bgtm9B/sbs-section-programs/?form=json&count=true&sort=metrics.viewCount.last7Days%7Cdesc&" +
                    @"byCategories=Section%2FPrograms,Film,{0}&byRatings=&facets=1", HttpUtility.UrlEncode(m.Groups["url"].Value));
                var jData = GetWebData<JObject>(url);
                foreach (var entry in jData["entries"])
                {
                    result.Add(parseVideo(entry));
                }
            }
            return result;

        }
        public override string GetVideoUrl(VideoInfo video)
        {
            var webData = GetWebData(video.VideoUrl);
            var data = JArray.Parse(webData);
            var ff = data[0]["releaseUrls"].Value<String>("html");
            webData = GetWebData(ff);
            var match = Regex.Match(webData, @"<video\ssrc=""(?<url>[^""]*)""", defaultRegexOptions);
            if (match.Success)
            {
                webData = GetWebData(match.Groups["url"].Value);
                video.PlaybackOptions = HlsPlaylistParser.GetPlaybackOptions(webData, match.Groups["url"].Value, HlsStreamInfoFormatter.VideoDimensionAndBitrate);
            }
            return video.GetPreferredUrl(true);
        }

        private VideoInfo parseVideo(JToken vid)
        {
            VideoInfo video = new VideoInfo()
            {
                Title = vid.Value<string>("title"),
                Description = vid.Value<string>("description") +
                    " Expires " + epoch.AddSeconds(vid.Value<long>("media$expirationDate") / 1000).ToString(),
                Airdate = epoch.AddSeconds(vid.Value<long>("pubDate") / 1000).ToString(),
                VideoUrl = vid.Value<string>("id").Replace(@"http://data.media.theplatform.com/media/data/Media/",
                @"https://www.sbs.com.au/api/video_pdkvars/playlist/")
            };

            JArray thumbs = vid.Value<JArray>("media$thumbnails");
            if (thumbs != null)
                video.Thumb = thumbs[0].Value<string>("plfile$downloadUrl");
            JArray contents = vid.Value<JArray>("media$content");
            if (contents != null)
                video.Length = TimeSpan.FromSeconds(contents[0].Value<double>("plfile$duration")).ToString();
            return video;
        }

        protected override List<VideoInfo> Parse(string url, string data)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            if (data == null) data = GetWebData(url);
            var jData = JObject.Parse(data);
            foreach (var vid in jData["entries"])
            {
                VideoInfo video = parseVideo(vid);
                result.Add(video);
            }
            return result;
        }
    }
}

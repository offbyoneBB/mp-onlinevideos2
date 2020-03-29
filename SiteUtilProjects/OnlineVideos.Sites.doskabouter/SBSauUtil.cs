using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using OnlineVideos.Helpers;

namespace OnlineVideos.Sites
{
    public class SBSauUtil : GenericSiteUtil
    {

        private DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        public override int DiscoverDynamicCategories()
        {
            var data = GetWebData<JObject>(baseUrl);
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
                        HasSubCategories = true
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
                                SubCategoriesDiscovered = true
                            };
                            list.Add(first, cat);
                        }
                        categ.ParentCategory = list[first];
                        list[first].SubCategories.Add(categ);
                    }
                }
            }
            foreach (var item in list)
                Settings.Categories.Add(item.Value);
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            var data = GetWebData<JObject>(((RssLink)parentCategory).Url);
            SortedList<int, SortedList<int,VideoInfo>> list = new SortedList<int, SortedList<int,VideoInfo>>();

            foreach (var vid in data["entries"])
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

                int season = vid.Value<int>("pl1$season");
                if (!list.ContainsKey(season))
                    list.Add(season, new SortedList<int,VideoInfo>());
                list[season].Add(vid.Value<int>("pl1$episodeNumber"),video);
                
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

        public override List<VideoInfo> GetVideos(Category category)
        {
            return (List<VideoInfo>)category.Other;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            var webData = GetWebData(video.VideoUrl);
            var data=JArray.Parse(webData);
            var ff = data[0]["releaseUrls"].Value<String>("html");
            webData = GetWebData(ff);
            var match = Regex.Match(webData,@"<video\ssrc=""(?<url>[^""]*)""", defaultRegexOptions);
            if (match.Success)
            {
                webData = GetWebData(match.Groups["url"].Value);
                video.PlaybackOptions = HlsPlaylistParser.GetPlaybackOptions(webData, match.Groups["url"].Value, (x, y) => y.Bandwidth.CompareTo(x.Bandwidth), (x) => x.Width + "x" + x.Height);
            }
            return video.GetPreferredUrl(true);
        }
    }
}

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class ZDFMediathekUtil : SiteUtilBase
    {
        string m3u8Regex = @"#EXT-X-STREAM-INF:PROGRAM-ID=\d,BANDWIDTH=(?<bitrate>\d+),RESOLUTION=(?<resolution>\d+x\d+),?CODECS=""(?<codecs>[^""]+)"".*?\n(?<url>.*)";

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Video Quality", TranslationFieldName = "VideoQuality"), Description("Defines the maximum quality for the video to be played.")]
        string videoQuality = "veryhigh";

        private static readonly NameValueCollection headers = new NameValueCollection { { "Accept-Encoding", "gzip" }, { "Accept", "*/*" }, { "User-Agent", "ZDF UWP" }, { "Api-Auth", "Bearer 6ed877e78b8e63771ca91df6cd232c3ef35609c1" } };

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            Settings.Categories.Add(new Category { Name = "Live" });
            Settings.Categories.Add(new Category { Name = "Sendung Verpasst", HasSubCategories = true, Description = "Sendungen der letzten 7 Tage." });
            Settings.Categories.Add(new Category { Name = "Rubriken", HasSubCategories = true });
            Settings.Categories.Add(new Category { Name = "Sendungen A-Z", HasSubCategories = true });
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (parentCategory.ParentCategory == null)
            {
                switch (parentCategory.Name)
                {
                    case "Sendung Verpasst":
                        if (parentCategory.SubCategories != null &&
                            parentCategory.SubCategories.Count > 0 &&
                            parentCategory.SubCategories[0].Name == DateTime.Today.ToString("dddd, d.M.yyy"))
                        { /* no need to rediscover if day hasn't changed */ }
                        else
                        {
                            parentCategory.SubCategories = new List<Category>();
                            for (int i = 0; i <= 7; i++)
                            {
                                parentCategory.SubCategories.Add(new RssLink()
                                {
                                    Name = i == 0 ? "Heute" : i == 1 ? "Gestern" : DateTime.Today.AddDays(-i).ToString("ddd, d.M."),
                                    Url = string.Format("https://api.zdf.de/content/documents/sendung-verpasst-100.json?profile=default&airtimeDate={0}", DateTime.SpecifyKind(DateTime.Today.AddHours(12).AddDays(-i), DateTimeKind.Utc).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK")),
                                    ParentCategory = parentCategory
                                });
                            }
                        }
                        break;
                    case "Sendungen A-Z":
                        parentCategory.SubCategories = new List<Category>();
                        var showsUrl = "http://api.zdf.de/content/documents/sendungen-100.json?profile=default";
                        foreach (var show in GetWebData<JObject>(showsUrl, headers: headers)["brand"].SelectMany(l => l["teaser"] ?? Enumerable.Empty<JToken>()))
                        {
                            var category = CategoryFromJson(show, parentCategory, false);
                            category.Url = string.Format("http://api.zdf.de/search/documents{0}?q=*&contentTypes=episode&sortOrder=desc&sortBy=date", show["http://zdf.de/rels/target"].Value<string>("structureNodePath"));
                            parentCategory.SubCategories.Add(category);
                        }
                        parentCategory.SubCategoriesDiscovered = true;
                        break;
                    case "Rubriken":
                        parentCategory.SubCategories = new List<Category>();
                        var catUrl = "http://api.zdf.de/search/documents?q=*&types=page-index&contentTypes=category";
                        foreach (var cat in GetWebData<JObject>(catUrl, headers: headers)["http://zdf.de/rels/search/results"])
                        {
                            var category = CategoryFromJson(cat, parentCategory, true);
                            category.Url = string.Format("http://api.zdf.de/search/documents{0}?q=*&contentTypes=brand&sortOrder=desc&sortBy=relevance", cat["http://zdf.de/rels/target"].Value<string>("structureNodePath"));
                            parentCategory.SubCategories.Add(category);
                        }
                        parentCategory.SubCategoriesDiscovered = true;
                        break;
                }
            }
            else
            {
                parentCategory.SubCategories = new List<Category>();
                var json = GetWebData<JObject>(((RssLink)parentCategory).Url, headers: headers);
                foreach (var show in json["http://zdf.de/rels/search/results"])
                {
                    var category = CategoryFromJson(show, parentCategory, false);
                    category.Url = string.Format("http://api.zdf.de/search/documents{0}?q=*&contentTypes=episode&sortOrder=desc&sortBy=date", show["http://zdf.de/rels/target"].Value<string>("structureNodePath"));
                    parentCategory.SubCategories.Add(category);
                }
                parentCategory.SubCategoriesDiscovered = true;
            }
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> list = new List<VideoInfo>();

            if (category.Name == "Live")
            {
                var json = GetWebData<JObject>("https://api.zdf.de/content/documents/epg-livetv-100.json?profile=default", headers: headers);
                var teasers = (json["livestreams"] as JArray)?.First?["teaser"] as JArray;
                foreach(var teaser in teasers)
                {
                    var title = teaser["http://zdf.de/rels/target"].Value<string>("tvService");
                    var img = teaser["http://zdf.de/rels/target"]["teaserImageRef"]["layouts"].Value<string>("384x216");
                    var url = "http://api.zdf.de" + teaser["http://zdf.de/rels/target"]["mainVideoContent"]["http://zdf.de/rels/target"].Value<string>("http://zdf.de/rels/streams/ptmd-template").Replace("{playerId}", "portal");

                    list.Add(new VideoInfo
                    {
                        Title = title,
                        Thumb = img,
                        VideoUrl = url
                    });
                }
            }
            else if (category.ParentCategory.Name == "Sendung Verpasst")
            {
                
                var json = GetWebData<JObject>((category as RssLink).Url, headers: headers);
                foreach (var broadcast in json["http://zdf.de/rels/broadcasts-page"]["http://zdf.de/rels/cmdm/broadcasts"])
                {
                    var video_page_teaser = broadcast["http://zdf.de/rels/content/video-page-teaser"];
                    if (video_page_teaser == null)
                        continue;
                    var mainVideoContent = video_page_teaser?["mainVideoContent"];
                    if (mainVideoContent == null)
                        continue;

                    var start = broadcast.Value<DateTime>("airtimeBegin");
                    var length = TimeSpan.FromSeconds(broadcast.Value<int>("duration"));
                    var title = broadcast.Value<string>("title");
                    var subtitle = broadcast.Value<string>("subtitle");
                    var tvStation = broadcast.Value<string>("tvService");
                    var desc = broadcast.Value<string>("text");
                    var img = video_page_teaser["teaserImageRef"]?["layouts"]?.Value<string>("384x216");
                    var url = "http://api.zdf.de" + mainVideoContent["http://zdf.de/rels/target"].Value<string>("http://zdf.de/rels/streams/ptmd-template").Replace("{playerId}", "portal");

                    list.Add(new VideoInfo
                    {
                        Title = title + (subtitle != null ? " [" + subtitle + "]" : ""),
                        Description = Helpers.StringUtils.PlainTextFromHtml(desc),
                        Length = length.TotalMinutes <= 60 ? length.TotalMinutes.ToString() + " min" : length.ToString("h\\h\\ m\\ \\m\\i\\n"),
                        Thumb = img,
                        Airdate = start.ToString("g", OnlineVideoSettings.Instance.Locale),
                        VideoUrl = url
                    });
                }
            }
            else
            {
                var json = GetWebData<JObject>((category as RssLink).Url, headers: headers);
                foreach (var result in json["http://zdf.de/rels/search/results"])
                {
                    var obj = result["http://zdf.de/rels/target"];
                    var videoContent = obj["mainContent"].First["videoContent"].First["http://zdf.de/rels/target"];

                    if (!obj.Value<bool>("hasVideo")) continue;

                    var title = obj.Value<string>("teaserHeadline");
                    var start = obj.Value<DateTime>("editorialDate");
                    var img = obj["teaserImageRef"]["layouts"]?.Value<string>("384x216");

                    var length = TimeSpan.FromSeconds(videoContent.Value<int>("duration"));
                    var url = "http://api.zdf.de" + videoContent.Value<string>("http://zdf.de/rels/streams/ptmd-template").Replace("{playerId}", "portal");
                    list.Add(new VideoInfo
                    {
                        Title = title,
                        Length = length.TotalMinutes <= 60 ? Math.Round(length.TotalMinutes).ToString() + " min" : length.ToString("h\\h\\ m\\ \\m\\i\\n"),
                        Thumb = img,
                        Airdate = start.ToString("g", OnlineVideoSettings.Instance.Locale),
                        VideoUrl = url
                    });
                }
            }
            return list;
        }

        public override String GetVideoUrl(VideoInfo video)
        {
            if (video.PlaybackOptions == null)
            {
                var json = GetWebData<JObject>(video.VideoUrl, headers: headers);
                var sortedPlaybackOptions = new SortedDictionary<string, Dictionary<string, string>>();
                foreach (var formitaet in json["priorityList"].SelectMany(l=>l["formitaeten"]))
                {
                    if (formitaet["facets"].Any(f => f.ToString() == "restriction_useragent"))
                        continue;
    
                    var type = formitaet.Value<string>("type");
                    foreach(var vid in formitaet["qualities"])
                    {
                        var quality = vid.Value<string>("quality");
                        if (quality == "auto")
                            continue;
                        var url = vid["audio"]["tracks"].First.Value<string>("uri");

                        if (url.EndsWith(".m3u8") || url.EndsWith(".webm"))
                            continue;

                        if (!sortedPlaybackOptions.ContainsKey(quality))
                            sortedPlaybackOptions[quality] = new Dictionary<string, string>();

                        if (url.Contains("master.m3u8"))
                        {
                            var m3u8Data = GetWebData(url);
                            foreach (Match match in Regex.Matches(m3u8Data, m3u8Regex))
                            {
                                sortedPlaybackOptions[quality]
                                    [string.Format("HLS - {0} - {1} kbps", match.Groups["resolution"].Value, int.Parse(match.Groups["bitrate"].Value) / 1000)]
                                    = match.Groups["url"].Value;
                            }
                        }
                        else
                            sortedPlaybackOptions[quality][type] = url;
                    }
                }
                
                video.PlaybackOptions = new Dictionary<string, string>();
                foreach (var e in sortedPlaybackOptions)
                {
                    foreach (var f in e.Value)
                        video.PlaybackOptions.Add(string.Format("{0}-{1}", e.Key, f.Key), f.Value);
                }
            }

            if (video.PlaybackOptions == null || video.PlaybackOptions.Count == 0)
                return string.Empty;
            if (video.PlaybackOptions.Count == 1)
                return video.PlaybackOptions.First().Value;
            
            string qualitytoMatch = videoQuality.ToString();
            string firstUrl = video.PlaybackOptions.FirstOrDefault(p => p.Key.Contains(qualitytoMatch)).Value;
            return !string.IsNullOrEmpty(firstUrl) ? firstUrl : video.PlaybackOptions.First().Value;
        }

        static RssLink CategoryFromJson(JToken result, Category parent, bool hasSubCategories)
        {
            var obj = result["http://zdf.de/rels/target"];
            var title = obj.Value<string>("teaserHeadline");
            var desc = obj.Value<string>("teasertext");
            var thumb = obj["teaserImageRef"]["layouts"].Value<string>("384x216");

            var videoCounterObj = obj["http://zdf.de/rels/search/page-video-counter-with-video"];
            uint? videoCount = !hasSubCategories && videoCounterObj != null ? videoCounterObj.Value<uint?>("totalResultsCount") : null;

            return new RssLink
            {
                Name = title,
                Thumb = thumb,
                Description = desc,
                EstimatedVideoCount = videoCount,
                ParentCategory = parent,
                HasSubCategories = hasSubCategories
            };
        }
    }
}


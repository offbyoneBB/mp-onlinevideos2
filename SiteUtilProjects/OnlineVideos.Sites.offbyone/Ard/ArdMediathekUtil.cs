using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Caching;

using Newtonsoft.Json.Linq;

using OnlineVideos.Helpers;
using OnlineVideos.Sites.Ard;

namespace OnlineVideos.Sites
{
    public class ArdConstants
    {

        public static Uri API_URL { get; } = new Uri("https://api.ardmediathek.de");
        public static string ITEM_URL { get; } = API_URL + "/page-gateway/pages/ard/item/";
        public static string DAY_PAGE_URL { get; } = API_URL + "/page-gateway/compilations/{0}/pastbroadcasts?startDateTime={1}T00:00:00.000Z&endDateTime={2}T23:59:59.000Z&pageNumber=0&pageSize={3}";

        public static int DAY_PAGE_SIZE { get; } = 100;
    }

    public class ArdMediathekUtil : SiteUtilBase
    {
        private const string CATEGORYNAME_MISSED_BROADCASTS = "Was lief";

        public static readonly string PLACEHOLDER_IMAGE_WIDTH = "{width}";
        public static readonly string IMAGE_WIDTH = "1024";

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            Settings.Categories.Add(new RssLink {Name = ArdLiveStreamsDeserializer.Name, Url = ArdLiveStreamsDeserializer.EntryUrl.AbsoluteUri, HasSubCategories = ArdLiveStreamsDeserializer .HasCategories, Other = new Context(new ArdLiveStreamsDeserializer(WebCache.Instance), default) });

            Settings.Categories.Add(new RssLink {Name = ArdTopicsPageDeserializer.Name, Url = ArdTopicsPageDeserializer.EntryUrl.AbsoluteUri , HasSubCategories = ArdTopicsPageDeserializer.HasCategories, Other = new Context(new ArdTopicsPageDeserializer(WebCache.Instance), default) });
            Settings.Categories.Add(new RssLink
            {
                Name = CATEGORYNAME_MISSED_BROADCASTS,
                HasSubCategories = true,
                Description = "Sendungen der letzten 7 Tage."
            });

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public delegate ArdCategoryInfoDto GetCategories(JToken json);

        public override int DiscoverSubCategories(Category selectedCategory)
        {

            if (selectedCategory is RssLink rssCategory && !string.IsNullOrWhiteSpace(rssCategory.Url) && rssCategory.Other is Context context)
            {
                var subCategories = new List<Category>();
                var page = context.Page;
                var result = page.GetCategories(rssCategory.Url, context.Token);
                foreach (var item in result.Value)
                {
                    var subCategory = new RssLink()
                    {
                        Name = item.Title,
                        Description = item.Description,
                        Thumb = item.ImageUrl,
                        Url = item.TargetUrl,
                        ParentCategory = selectedCategory,
                        HasSubCategories = item.HasSubCategories,
                        SubCategories = new List<Category>(),
                        Other = new Context(page, result.ContinuationToken)
                    };
                    subCategories.Add(subCategory);
                }
                selectedCategory.SubCategories = subCategories;
            }
            else if (selectedCategory.ParentCategory == null)
            {
                switch (selectedCategory.Name)
                {
                    case CATEGORYNAME_MISSED_BROADCASTS:
                        {
                            const string DAY_PAGE_DATE_FORMAT = "yyyy-MM-dd";
                            selectedCategory.SubCategories = new List<Category>();
                            for (var i = 0; i <= 7; i++)
                            {
                                var day = DateTime.Today.AddDays(-i);
                                var url = string.Format(ArdConstants.DAY_PAGE_URL, "daserste",
                                    day.ToString(DAY_PAGE_DATE_FORMAT), day.ToString(DAY_PAGE_DATE_FORMAT),
                                    ArdConstants.DAY_PAGE_SIZE);
                                // string.Format("https://api.zdf.de/content/documents/sendung-verpasst-100.json?profile=default&airtimeDate={0}", DateTime.SpecifyKind(DateTime.Today.AddHours(12).AddDays(-i), DateTimeKind.Utc).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK"))
                                selectedCategory.SubCategories.Add(new RssLink()
                                {
                                    Name = i == 0 ? "Heute" : i == 1 ? "Gestern" : day.ToString("ddd, d.M."),
                                    Url = url,
                                    ParentCategory = selectedCategory
                                });
                            }

                            //TODO Url returns an array with a single widget element
                        }
                        break;
                }
            }

            return selectedCategory.SubCategories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            var list = new List<VideoInfo>();

            if (category is RssLink rssCategory && !string.IsNullOrWhiteSpace(rssCategory.Url) && rssCategory.Other is Context context)
            {
                var page = context.Page;
                var result = page.GetVideos(rssCategory.Url, context.Token);
                foreach (var filmInfoDto in result.Value)
                {
                    list.Add(new VideoInfo
                    {
                        Title = filmInfoDto.Title,
                        Description = filmInfoDto.Description ?? string.Empty,
                        //Length = length.TotalMinutes <= 60 ? length.TotalMinutes.ToString() + " min" : length.ToString("h\\h\\ m\\ \\m\\i\\n"),
                        Thumb = filmInfoDto.ImageUrl,
                        Airdate = filmInfoDto.AirDate.ToLocalTime().ToString("g", OnlineVideoSettings.Instance.Locale),
                        VideoUrl = filmInfoDto.TargetUrl,
                        Other = new Context(page, result.ContinuationToken)
                    });
                }
            }
            else
            {
                var json = GetWebData<JToken>((category as RssLink).Url, cache: false);
                var parser = new ArdDayPageDeserializer();
                var b = parser.Deserialize(json);

                foreach (var filmInfoDto in b)
                {
                    list.Add(new VideoInfo
                    {
                        Title = filmInfoDto.Title,
                        Description = filmInfoDto.Description ?? string.Empty,
                        Thumb = filmInfoDto.ImageUrl,
                        Airdate = filmInfoDto.AirDate.ToLocalTime().ToString("g", OnlineVideoSettings.Instance.Locale),
                        VideoUrl = filmInfoDto.TargetUrl,
                        Other = filmInfoDto
                    });
                }
            }

            return list;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            if (string.IsNullOrWhiteSpace(video.VideoUrl))
            {
                throw new OnlineVideosException("Video not available!");
            }
            else if (video.Other is Context context)
            {
                var page = context.Page;
                var result = page.GetStreams(video.VideoUrl, context.Token);


                var orderedPlaybackOptions = result.Value.OrderBy(i => (int)i.Quality)
                    .ToLookup(s => s.Quality)
                    .ToDictionary(i => i.Key, i => i.Select(x => x.Url));
                video.PlaybackOptions = orderedPlaybackOptions?.ToDictionary(e => e.Key.ToString(), e => e.Value.First());


                var streamUrl = video.PlaybackOptions.FirstOrDefault().Value;
                //// TODO already handled in DownloadDetailsDto
                //if (streamUrl.StartsWith("//"))
                //{
                //    streamUrl = $"https:{streamUrl}";
                //}

                if (streamUrl?.Contains("master.m3u8") ?? false)
                {
                    var m3u8Data = GetWebData<string>(streamUrl, cache: false);
                    var m3u8PlaybackOptions = HlsPlaylistParser.GetPlaybackOptions(m3u8Data, streamUrl);
                    video.PlaybackOptions = m3u8PlaybackOptions;
                    streamUrl = video.PlaybackOptions.FirstOrDefault().Value;
                }
                return streamUrl;
            }

            {
                var json = GetWebData<JObject>(video.VideoUrl, cache: false);
                var listLiveStream = json["widgets"]?.FirstOrDefault()?["mediaCollection"]["embedded"];
                var livestreamPlaylistUrl = listLiveStream["_mediaArray"].FirstOrDefault()["_mediaStreamArray"].FirstOrDefault().Value<string>("_stream");
                //TODO Workaround for url without leading https:
                if (livestreamPlaylistUrl.StartsWith("//"))
                {
                    livestreamPlaylistUrl = $"https:{livestreamPlaylistUrl}";
                }

                //var newUrl = WebCache.Instance.GetRedirectedUrl(livestreamPlaylistUrl);
                var m3u8Data = GetWebData<string>(livestreamPlaylistUrl, cache: false);
                var m3u8PlaybackOptions = HlsPlaylistParser.GetPlaybackOptions(m3u8Data, livestreamPlaylistUrl);
                video.PlaybackOptions = m3u8PlaybackOptions;
            }

            return video.PlaybackOptions.FirstOrDefault().Value;
        }
    }

    class Context
    {
        public Context(PageDeserializerBase page, ContinuationToken token)
        {
            Page = page;
            Token = token;
        }

        public PageDeserializerBase Page { get; set; }
        public ContinuationToken Token { get; set; }
    }

    /// <summary>
    /// teasers target link auf item https://api.ardmediathek.de/page-gateway/pages/daserste/item/...
    /// </summary>
    public class ArdDayPageDeserializer : ArdTeasersDeserializer
    {
        private static readonly string ELEMENT_WIDGETS = "widgets";
        private static readonly string ELEMENT_TEASERS = "teasers";

        public override HashSet<ArdFilmInfoDto> Deserialize(JToken widgetElement)
        {
            if(widgetElement?.Type == JTokenType.Object && widgetElement?[ELEMENT_WIDGETS] != null)
            {
                widgetElement = widgetElement[ELEMENT_WIDGETS];
            }
            if (widgetElement?.Type == JTokenType.Array)
            {
                //DayPage returns an array with a single widget
                widgetElement = widgetElement?.First;
            }
            var teasers = widgetElement?[ELEMENT_TEASERS] as JArray;

            if (teasers == null)
            {
                return new HashSet<ArdFilmInfoDto>(); ;
            }

            return ParseTeasers(teasers);
        }
    }


    /// <summary>
    /// teasers target link auf nicht item https://api.ardmediathek.de/page-gateway/pages/daserste/item/...
    /// </summary>
    public class ArdXyzDeserializer : ArdDayPageDeserializer
    {
        private static readonly string ELEMENT_WIDGETS = "widgets";
        private static readonly string ELEMENT_TEASERS = "teasers";

        public override HashSet<ArdFilmInfoDto> Deserialize(JToken jsonElement)
        {
            throw new NotImplementedException();
            var widgets = jsonElement?[ELEMENT_WIDGETS] as JArray;

            if (widgets == null)
            {
                return new HashSet<ArdFilmInfoDto>(); ;
            }

            return base.Deserialize(widgets);
        }

        public IEnumerable<ArdCategoryInfoDto> DeserializeNeuWidgetsToCategory(JToken jsonElement)
        {
            var widgets = jsonElement?[ELEMENT_WIDGETS] as JArray;

            if (widgets == null)
            {
                yield break;
            }

            foreach (var workaround in base.Deserialize(widgets))
            {
                yield return new ArdCategoryInfoDto(workaround.Id, workaround.TargetUrl)
                             {
                                 Title = workaround.Title,
                                 //Description =,
                                 ImageUrl = workaround.ImageUrl,
                                 //NavigationUrl = workaround.Url,
                                 //Pagination = ,
                             };
            }
        }

        public IEnumerable<ArdCategoryInfoDto> DeserializeNeuTeasersToCategory(JToken jsonElement)
        {
            var widgetElement = jsonElement; //?[ELEMENT_TEASERS] as JArray

            //if (teasers == null)
            //{
            //    yield break;
            //}

            foreach (var workaround in base.Deserialize(widgetElement))
            {
                yield return new ArdCategoryInfoDto(workaround.Id, workaround.TargetUrl)
                {
                    Title = workaround.Title,
                    //Description =,
                    ImageUrl = workaround.ImageUrl,
                    //NavigationUrl = workaround.Url,
                    //Pagination = ,
                };
            }
        }
    }


    public class ArdEditorialDeserializer : ArdTeasersDeserializer //TODO Workaround
    {
        private static readonly string ATTRIBUTE_COMPILATIONTYPE = "compilationType";
        private static readonly string ELEMENT_WIDGETS = "widgets";
        private static readonly string ELEMENT_TEASERS = "teasers";

        private static readonly string ELEMENT_LINKS = "links";
        private static readonly string ELEMENT_SELF = "self";

        private static readonly string ATTRIBUTE_ID = "id";
        private static readonly string ATTRIBUTE_HREF = "href";
        private static readonly string ATTRIBUTE_TITLE = "title";

        public string SupportedCompilationType { get; } = "editorial";


        /// <inheritdoc />
        public override HashSet<ArdFilmInfoDto> Deserialize(JToken jsonElement) => throw new NotImplementedException();


        public IEnumerable<ArdCategoryInfoDto> DeserializeNeuWidgetsToCategory(JToken jsonElement)
        {
            var widgets = jsonElement?[ELEMENT_WIDGETS] as JArray;

            if (widgets == null)
            {
                yield break;
            }

            foreach (var widget in widgets)
            {
                var compilationType = widget.Value<string>(ATTRIBUTE_COMPILATIONTYPE);

                var id = //widgetElement[ELEMENT_LINKS]?[ELEMENT_TARGET]?.Value<string>(ATTRIBUTE_ID) ??
                    widget.Value<string>(ATTRIBUTE_ID);

                var selfUrl = widget[ELEMENT_LINKS]?[ELEMENT_SELF]?.Value<string>(ATTRIBUTE_HREF);

                yield return new ArdCategoryInfoDto(id, selfUrl)
                             {
                                Title = widget.Value<string>(ATTRIBUTE_TITLE),
                                //Description =,
                                //ImageUrl = ,
                                //NavigationUrl = selfUrl, //string.Format(ArdConstants.EDITORIAL_URL, id),
                                                       //Pagination = ,
                };
            }
        }

    }

    public abstract class ArdTeasersDeserializer
    {

        private static readonly string ELEMENT_LINKS = "links";
        private static readonly string ELEMENT_TARGET = "target";
        private static readonly string ELEMENT_MEDIACOLLECTION = "mediaCollection";
        private static readonly string ELEMENT_EMBEDDED = "embedded";
        private static readonly string ELEMENT_IMAGES = "images";
        private static readonly string ELEMENT_ASPECT_16X9 = "aspect16x9";
        private static readonly string ELEMENT_ASPECT_4X3 = "aspect16x9";

        private static readonly string ATTRIBUTE_ID = "id";
        private static readonly string ATTRIBUTE_HREF = "href";
            private static readonly string ATTRIBUTE_TITLE = "shortTitle";
            private static readonly string ATTRIBUTE_DATETIME = "broadcastedOn";
            private static readonly string ATTRIBUTE_DURATION = "_duration";
            private static readonly string ATTRIBUTE_DESCRIPTION = "synopsis";
            private static readonly string ATTRIBUTE_IMAGESRC = "src";

            private static readonly string ATTRIBUTE_NUMBER_OF_CLIPS = "numberOfClips";

            public abstract HashSet<ArdFilmInfoDto> Deserialize(JToken jsonElement);

        protected HashSet<ArdFilmInfoDto> ParseTeasers(JArray teasers)
        {
            var results = new HashSet<ArdFilmInfoDto>();
            foreach (var teaserElement in teasers)
            {
                //var teaserObject = teaserElement;
                var id = teaserElement[ELEMENT_LINKS]?[ELEMENT_TARGET]?.Value<string>(ATTRIBUTE_ID) ??
                         teaserElement.Value<string>(ATTRIBUTE_ID);
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }
                var url = teaserElement[ELEMENT_LINKS]?[ELEMENT_TARGET]?.Value<string>(ATTRIBUTE_HREF);
                var numberOfClips = teaserElement.Value<int?>(ATTRIBUTE_NUMBER_OF_CLIPS) ?? 0;
                var imageUrl = teaserElement[ELEMENT_IMAGES]?[ELEMENT_ASPECT_16X9]?.Value<string>(ATTRIBUTE_IMAGESRC) ??
                               teaserElement[ELEMENT_IMAGES]?[ELEMENT_ASPECT_4X3]?.Value<string>(ATTRIBUTE_IMAGESRC);

                var filmInfo = new ArdFilmInfoDto(id, numberOfClips, url)
                {
                    Title       = teaserElement.Value<string>(ATTRIBUTE_TITLE),
                    AirDate     = teaserElement.Value<DateTime>(ATTRIBUTE_DATETIME),
                    Description = teaserElement.Value<string>(ATTRIBUTE_DESCRIPTION),
                    Duration    = teaserElement[ELEMENT_MEDIACOLLECTION]?[ELEMENT_EMBEDDED]?.Value<int>(ATTRIBUTE_DURATION),
                    ImageUrl    = imageUrl?.Replace(ArdMediathekUtil.PLACEHOLDER_IMAGE_WIDTH, ArdMediathekUtil.IMAGE_WIDTH)
                };

                results.Add(filmInfo);
            }

            return results;
        }
    }

}

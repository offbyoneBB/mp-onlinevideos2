using Newtonsoft.Json.Linq;

using OnlineVideos.Sites.Zdf;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineVideos.Sites.Ard
{

    /// <summary>
    ///                     case CATEGORYNAME_BROADCASTS_AZ:
    /// https://api.ardmediathek.de/page-gateway/pages/ard/editorial/experiment-a-z
    /// https://api.ardmediathek.de/page-gateway/pages/ard/editorial/experiment-a-z?embedded=false
    /// https://api.ardmediathek.de/page-gateway/pages/ard/editorial/experiment-a-z");
    /// </summary>
    public class ArdTopicsPageDeserializer : PageDeserializerBase
    {
        private static readonly string _level = "Level";

        private readonly WebCache _webClient;

        public static string Name { get; } = "Sendungen A-Z";
        public static bool HasCategories { get; } = true;
        public static Uri EntryUrl { get; } = new Uri("https://api.ardmediathek.de/page-gateway/pages/ard/editorial/experiment-a-z");

        public ArdTopicsPageDeserializer(WebCache webClient) => _webClient = webClient;

        private ArdCategoryDeserializer _categoryDeserializer = new ArdCategoryDeserializer();
        private ArdFilmInfoDeserializerNeu _videoDeserializer = new ArdFilmInfoDeserializerNeu();
        private ArdMediaArrayConverter _mediaArrayConverter = new ArdMediaArrayConverter();

        public override Result<IEnumerable<ArdCategoryInfoDto>> GetCategories(string targetUrl, ContinuationToken continuationToken = null)
        {
            continuationToken ??= new ContinuationToken() { { _level, 0 } };

            var currentLevel = continuationToken.GetValueOrDefault(_level) as int? ?? 0;
            //var currentLevel = string.Equals(EntryUrl.AbsoluteUri, targetUrl) ? 0 : 1;

            var json = _webClient.GetWebData<JObject>(targetUrl);
            var categoryInfoDtos = currentLevel switch
            {
                0 => _categoryDeserializer.ParseWidgets(json, hasSubCategories: true), // load A - Z
                1 => LoadDetails(json), // load e.g. Abendschau - skip level, (load infos from nextlevel) for each category load url and read synopsis
                //2 => categoryDeserializer.ParseTeasers(json), // videos...
                _ => throw new ArgumentOutOfRangeException(),
            };

            var newToken = new ContinuationToken(continuationToken);
            newToken[_level] = currentLevel + 1;
            return new Result<IEnumerable<ArdCategoryInfoDto>>
            {
                ContinuationToken = newToken,
                Value = categoryInfoDtos
            };
        }

        private IEnumerable<ArdCategoryInfoDto> LoadDetails(JObject json)
        {
            var categories = _categoryDeserializer.ParseTeasers(json);

            foreach (var category in categories)
            {
                //TODO workaround
                var details = _webClient.GetWebData<JObject>(category.TargetUrl);
                var newCategory = _categoryDeserializer.ParseTeaser(details);
                category.Title = newCategory.Title;
                category.Description = newCategory.Description;
                yield return category;
                // sub item sind videos, aber ...
            }

            //return categories;
        }

        public override Result<IEnumerable<ArdFilmInfoDto>> GetVideos(string url, ContinuationToken continuationToken = null)
        {
            var json = _webClient.GetWebData<JToken>(url, cache: false);
            var filmInfoDtos = _videoDeserializer.ParseTeasers(json);

            return new Result<IEnumerable<ArdFilmInfoDto>>()
            {
                ContinuationToken = continuationToken,
                Value = filmInfoDtos
            };
        }

        public override Result<IEnumerable<DownloadDetailsDto>> GetStreams(string url, ContinuationToken continuationToken = null)
        {
            continuationToken ??= new ContinuationToken() { { _level, 0 } };

            var json = _webClient.GetWebData<JObject>(url, cache: false);
            //var json = GetWebData<JObject>(video.VideoUrl, cache: false);
            var collectionEmbedded = json["widgets"]?.FirstOrDefault()?["mediaCollection"]["embedded"];
            //var livestreamPlaylistUrl = listLiveStream["_mediaArray"].FirstOrDefault()["_mediaStreamArray"].FirstOrDefault().Value<string>("_stream"); "_stream");

            var streamInfoDtos = _mediaArrayConverter.ParseVideoUrls(collectionEmbedded as JObject);

            return new Result<IEnumerable<DownloadDetailsDto>>()
            {
                ContinuationToken = continuationToken,
                Value = streamInfoDtos
            };
        }

    }

    public class ContinuationToken : Dictionary<string, object>
    {
        public ContinuationToken()
        {
        }

        public ContinuationToken(ContinuationToken otherToken) : base(otherToken)
        {
        }
    }

    public abstract class PageDeserializerBase
    {
        public abstract Result<IEnumerable<ArdCategoryInfoDto>> GetCategories(string url, ContinuationToken continuationToken = null);

        public abstract Result<IEnumerable<ArdFilmInfoDto>> GetVideos(string url, ContinuationToken continuationToken = null);

        public abstract Result<IEnumerable<DownloadDetailsDto>> GetStreams(string url, ContinuationToken continuationToken = null);
    }

    public class Result<T> //where T : ArdInformationDtoBase
    {
        public T Value { get; set; }
        public ContinuationToken ContinuationToken { get; set; }
    }


    public class ArdMediaArrayConverter
    {
        private static readonly string ELEMENT_MEDIA_ARRAY = "_mediaArray";
        private static readonly string ELEMENT_STREAM = "_stream";
        private static readonly string ELEMENT_MEDIA_STREAM_ARRAY = "_mediaStreamArray";

        private static readonly string ELEMENT_HEIGHT = "_height";
        private static readonly string ELEMENT_PLUGIN = "_plugin";
        private static readonly string ELEMENT_QUALITY = "_quality";
        private static readonly string ELEMENT_SERVER = "_server";
        private static readonly string ELEMENT_SORT_ARRAY = "_sortierArray";
        private static readonly string ELEMENT_WIDTH = "_width";



        public IEnumerable<DownloadDetailsDto> ParseVideoUrls(/*DownloadDto dto,*/ JObject jsonElement)
        {
            var pluginValue = GetPluginValue(jsonElement);
            var mediaArray = jsonElement?[ELEMENT_MEDIA_ARRAY] as JArray;
            return ParseMediaArray(pluginValue, mediaArray);
        }

        private static int GetPluginValue(JObject jsonElement)
        {
            var pluginArray = jsonElement?[ELEMENT_SORT_ARRAY] as JArray;
            return pluginArray?.Values<int>()?.FirstOrDefault() ?? 1;
        }

        private IEnumerable<DownloadDetailsDto> ParseMediaArray(int pluginValue, JArray mediaArray)
        {
            foreach (var element in mediaArray.Where(item => item.Value<int>(ELEMENT_PLUGIN) == pluginValue).Select(item => item[ELEMENT_MEDIA_STREAM_ARRAY]))
            {
                foreach (var downloadDetail in ParseMediaStreamArray(element as JArray))
                {
                    yield return downloadDetail;
                }
            }
        }

        private IEnumerable<DownloadDetailsDto> ParseMediaStreamArray(JArray mediaStreamArray)
        {
            foreach (var videoElement in mediaStreamArray)
            {
                var quality = ParseVideoQuality(videoElement);
                foreach (var downloadDetail in ParseMediaStreamStream(videoElement, quality))
                {
                    yield return downloadDetail;
                }
            }
        }

        private Qualities ParseVideoQuality(JToken quality)
        {
            string ardQuality = quality?[ELEMENT_QUALITY].ToString();
            var qualityValue = ardQuality switch
            {
                "0" => Qualities.Small,
                "1" => Qualities.Small,
                "2" => Qualities.Normal,
                "3" => Qualities.High,
                "4" => Qualities.HD,
                _ => Qualities.Small,
            };
            return qualityValue;
        }

        private IEnumerable<DownloadDetailsDto> ParseMediaStreamStream(JToken videoElement, Qualities quality)
        {
            var videoObject = videoElement as JObject;
            var streamObject = videoObject?[ELEMENT_STREAM];
            if (streamObject != null)
            {
                if (streamObject.Type == JTokenType.String)
                {
                    var url = streamObject.Value<string>();
                    yield return new DownloadDetailsDto(quality, url);
                }
                else if (streamObject.Type == JTokenType.Array)
                {
                    // TODO: Take first of same quality
                    var url = streamObject.First.Value<string>();
                    yield return new DownloadDetailsDto(quality, url);
                }
            }
        }
    }


    internal abstract class ArdWidgetTeaserDeserializerBase
    {
        protected static readonly string ELEMENT_WIDGETS = "widgets";
        protected static readonly string ELEMENT_TEASERS = "teasers";

        protected static readonly string ELEMENT_LINKS = "links";
        protected static readonly string ELEMENT_SELF = "self";
        protected static readonly string ELEMENT_TARGET = "target";

        protected static readonly string ELEMENT_IMAGES = "images";
        protected static readonly string ELEMENT_ASPECT_16X9 = "aspect16x9";
        protected static readonly string ELEMENT_ASPECT_4X3 = "aspect16x9";


        protected static readonly string ATTRIBUTE_ID = "id";
        protected static readonly string ATTRIBUTE_HREF = "href";
        protected static readonly string ATTRIBUTE_SRC = "src";

        protected static readonly string ATTRIBUTE_TITLE = "title";
        protected static readonly string ATTRIBUTE_TITLE_SHORT = "shortTitle";
        protected static readonly string ATTRIBUTE_DESCRIPTION = "synopsis";
    }

    internal class ArdCategoryDeserializer : ArdWidgetTeaserDeserializerBase
    {
        protected static readonly string ELEMENT_MEDIACOLLECTION = "mediaCollection";
        protected static readonly string ELEMENT_EMBEDDED = "embedded";

        protected static readonly string WIDGET_ATTRIBUTE_COMPILATIONTYPE = "compilationType";

        public IEnumerable<ArdCategoryInfoDto> ParseWidgets(JToken jsonElement, bool hasSubCategories = false)
        {
            var widgets = jsonElement?[ELEMENT_WIDGETS] as JArray;
            return ParseWidgets(widgets, hasSubCategories);
        }

        public IEnumerable<ArdCategoryInfoDto> ParseWidgets(JArray widgets, bool hasSubCategories = false)
        {
            if (widgets == null)
            {
                yield break;
            }

            foreach (var widget in widgets)
            {
                var category = ParseWidget(widget, hasSubCategories);
                if (category == null)
                {
                    yield break;
                }
                else
                {
                    yield return category;
                }
            }
        }

        protected ArdCategoryInfoDto ParseWidget(JToken widgetElement, bool hasSubCategories = false)
        {
            if (widgetElement == null)
            {
                return null;
            }

            var compilationType = widgetElement.Value<string>(WIDGET_ATTRIBUTE_COMPILATIONTYPE);

            var id = //widgetElement[ELEMENT_LINKS]?[ELEMENT_TARGET]?.Value<string>(ATTRIBUTE_ID) ??
                     widgetElement.Value<string>(ATTRIBUTE_ID);


            var selfUrl = widgetElement[ELEMENT_LINKS]?[ELEMENT_SELF]?.Value<string>(ATTRIBUTE_HREF);

            return new ArdCategoryInfoDto(id, selfUrl)
            {
                Title = widgetElement.Value<string>(ATTRIBUTE_TITLE),
                //Description =,
                //ImageUrl = ,
                //NavigationUrl = selfUrl, //string.Format(ArdConstants.EDITORIAL_URL, id),
                //Pagination = ,
                HasSubCategories = hasSubCategories,
            };
        }


        public IEnumerable<ArdCategoryInfoDto> ParseTeasers(JToken jsonElement, bool hasSubCategories = false, int widgetsToUse = 1)
        {
            var widgetsElement = jsonElement;
            if (jsonElement?.Type == JTokenType.Object && jsonElement?[ELEMENT_WIDGETS] != null)
            {
                widgetsElement = jsonElement[ELEMENT_WIDGETS];
            }
            IEnumerable<JObject> selectedWidgetElements = new List<JObject>() { jsonElement as JObject };
            if (widgetsElement?.Type == JTokenType.Array)
            {
                selectedWidgetElements = widgetsElement.Children().Take(widgetsToUse).OfType<JObject>();
            }

            // ToDo SelectMany do we need the widget category, which is now omitted?
            var teasers = selectedWidgetElements.Select(jObj => jObj?[ELEMENT_TEASERS] as JArray).First();
            return ParseTeasers(teasers, hasSubCategories);
        }

        public IEnumerable<ArdCategoryInfoDto> ParseTeasers(JArray teasers, bool hasSubCategories = false)
        {
            if (teasers == null)
            {
                yield break;
            }

            foreach (var teaser in teasers)
            {
                var category = ParseTeaser(teaser, hasSubCategories);
                if (category == null)
                {
                    yield break;
                }
                else
                {
                    yield return category;
                }
            }
        }


        public ArdCategoryInfoDto ParseTeaser(JToken teaserElement, bool hasSubCategories = false)
        {
            var id = teaserElement[ELEMENT_LINKS]?[ELEMENT_TARGET]?.Value<string>(ATTRIBUTE_ID) ??
                        teaserElement.Value<string>(ATTRIBUTE_ID);
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            var url = teaserElement[ELEMENT_LINKS]?[ELEMENT_TARGET]?.Value<string>(ATTRIBUTE_HREF);
            var imageUrl = teaserElement[ELEMENT_IMAGES]?[ELEMENT_ASPECT_16X9]?.Value<string>(ATTRIBUTE_SRC) ??
                            teaserElement[ELEMENT_IMAGES]?[ELEMENT_ASPECT_4X3]?.Value<string>(ATTRIBUTE_SRC);

            return new ArdCategoryInfoDto(id, url)
            {
                Title = teaserElement.Value<string>(ATTRIBUTE_TITLE),
                //AirDate = teaserElement.Value<DateTime>(ATTRIBUTE_DATETIME),
                Description = teaserElement.Value<string>(ATTRIBUTE_DESCRIPTION),
                //Duration = teaserElement[ELEMENT_MEDIACOLLECTION]?[ELEMENT_EMBEDDED]?.Value<int>(ATTRIBUTE_DURATION),
                ImageUrl = imageUrl?.Replace(ArdMediathekUtil.PLACEHOLDER_IMAGE_WIDTH, ArdMediathekUtil.IMAGE_WIDTH),
                HasSubCategories = hasSubCategories,
            };
        }
    }

    internal class ArdFilmInfoDeserializerNeu : ArdWidgetTeaserDeserializerBase
    {
        protected static readonly string ATTRIBUTE_UNTIL_DATETIME = "availableTo";
        protected static readonly string ATTRIBUTE_AIR_DATETIME = "broadcastedOn";
        protected static readonly string ATTRIBUTE_DURATION = "duration";

        protected static readonly string ATTRIBUTE_NUMBER_OF_CLIPS = "numberOfClips";

        public IEnumerable<ArdFilmInfoDto> ParseTeasers(JToken jsonElement, int widgetsToUse = 1)
        {
            JToken widgetsElement = jsonElement;
            if (jsonElement?.Type == JTokenType.Object && jsonElement?[ELEMENT_WIDGETS] != null)
            {
                widgetsElement = jsonElement[ELEMENT_WIDGETS];
            }
            var selectedWidgetElements = Enumerable.Empty<JObject>();
            if (widgetsElement?.Type == JTokenType.Array)
            {
                selectedWidgetElements = widgetsElement.Children().Take(widgetsToUse).OfType<JObject>();
            }

            // ToDo SelectMany do we need the widget category, which is now omitted?
            var teasers = selectedWidgetElements.Select(jObj => jObj[ELEMENT_TEASERS] as JArray).First();
            return ParseTeasers(teasers);
        }

        public IEnumerable<ArdFilmInfoDto> ParseTeasers(JArray teasers)
        {
            if (teasers == null)
            {
                yield break;
            }

            foreach (var teaser in teasers)
            {
                var category = ParseTeaser(teaser);
                if (category == null)
                {
                    yield break;
                }
                else
                {
                    yield return category;
                }
            }
        }

        protected ArdFilmInfoDto ParseTeaser(JToken teaserElement)
        {
            var id = teaserElement[ELEMENT_LINKS]?[ELEMENT_TARGET]?.Value<string>(ATTRIBUTE_ID) ??
                        teaserElement.Value<string>(ATTRIBUTE_ID);
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            var url = teaserElement[ELEMENT_LINKS]?[ELEMENT_TARGET]?.Value<string>(ATTRIBUTE_HREF);
            var imageUrl = teaserElement[ELEMENT_IMAGES]?[ELEMENT_ASPECT_16X9]?.Value<string>(ATTRIBUTE_SRC) ??
                            teaserElement[ELEMENT_IMAGES]?[ELEMENT_ASPECT_4X3]?.Value<string>(ATTRIBUTE_SRC);

            var numberOfClips = teaserElement.Value<int?>(ATTRIBUTE_NUMBER_OF_CLIPS) ?? 0;

            return new ArdFilmInfoDto(id, numberOfClips, url)
            {
                Title = teaserElement.Value<string>(ATTRIBUTE_TITLE),
                AirDate = teaserElement.Value<DateTime>(ATTRIBUTE_AIR_DATETIME),
                AvailableUntilDate = teaserElement.Value<DateTime>(ATTRIBUTE_UNTIL_DATETIME),
                //TODO To get Description we need to navigate to items url (should be the "url")
                Description = teaserElement.Value<string>(ATTRIBUTE_DESCRIPTION),
                Duration = teaserElement.Value<int>(ATTRIBUTE_DURATION),
                ImageUrl = imageUrl?.Replace(ArdMediathekUtil.PLACEHOLDER_IMAGE_WIDTH, ArdMediathekUtil.IMAGE_WIDTH)
            };
        }
    }


    public static class CollectionExtensions
    {
        public static TValue? GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            return dictionary.GetValueOrDefault(key, default!);
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            TValue? value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }

        public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, value);
                return true;
            }

            return false;
        }

        public static bool Remove<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, out TValue value)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            if (dictionary.TryGetValue(key, out value))
            {
                dictionary.Remove(key);
                return true;
            }

            value = default;
            return false;
        }
    }
}

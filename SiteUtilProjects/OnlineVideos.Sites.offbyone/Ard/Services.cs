using Newtonsoft.Json.Linq;

using OnlineVideos.Sites.Zdf;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OnlineVideos.Sites.Ard
{

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
        protected ArdMediaArrayConverter MediaArrayConverter { get; } = new ArdMediaArrayConverter();

        //public abstract ArdCategoryInfoDto RootCategory { get; }
        protected WebCache WebClient { get; }

        protected PageDeserializerBase(WebCache webClient) => WebClient = webClient;

        public abstract Result<IEnumerable<ArdCategoryInfoDto>> GetCategories(string targetUrl, ContinuationToken continuationToken = null);

        public abstract Result<IEnumerable<ArdFilmInfoDto>> GetVideos(string targetUrl, ContinuationToken continuationToken = null);



        public virtual Result<IEnumerable<DownloadDetailsDto>> GetStreams(string url, ContinuationToken continuationToken = null)
        {
            //continuationToken ??= new ContinuationToken() { { _level, 0 } };

            var json = WebClient.GetWebData<JObject>(url, cache: false);
            //var json = GetWebData<JObject>(video.VideoUrl, cache: false);
            var collectionEmbedded = json["widgets"]?.FirstOrDefault()?["mediaCollection"]["embedded"];
            //var livestreamPlaylistUrl = listLiveStream["_mediaArray"].FirstOrDefault()["_mediaStreamArray"].FirstOrDefault().Value<string>("_stream"); "_stream");

            var streamInfoDtos = MediaArrayConverter.ParseVideoUrls(collectionEmbedded as JObject);

            return new Result<IEnumerable<DownloadDetailsDto>>()
                   {
                       ContinuationToken = continuationToken,
                       Value = streamInfoDtos
                   };
        }
    }

    public class Result<T> //where T : ArdInformationDtoBase
    {
        public T Value { get; set; }
        public ContinuationToken ContinuationToken { get; set; }
    }


    public class ArdLiveStreamsDeserializer : PageDeserializerBase
    {
        public static string Name { get; } = "Live TV";
        public static bool HasCategories { get; } = false;
        public static Uri EntryUrl { get; } = new Uri("https://api.ardmediathek.de/page-gateway/widgets/ard/editorials/4hEeBDgtx6kWs6W6sa44yY");

        private ArdCategoryDeserializer _categoryDeserializer = new ArdCategoryDeserializer();
        private ArdFilmInfoDeserializerNeu _videoDeserializer = new ArdFilmInfoDeserializerNeu();
        //private ArdMediaArrayConverter _mediaArrayConverter = new ArdMediaArrayConverter();

        public ArdLiveStreamsDeserializer(WebCache webClient) : base(webClient) { }

        /// <inheritdoc />
        public override Result<IEnumerable<ArdCategoryInfoDto>> GetCategories(string targetUrl, ContinuationToken continuationToken = null) => throw new NotImplementedException();


        /// <inheritdoc />
        public override Result<IEnumerable<ArdFilmInfoDto>> GetVideos(string targetUrl, ContinuationToken continuationToken = null)
        {
            var json = WebClient.GetWebData<JObject>(targetUrl);
            var filmInfoDtos = LoadDetails(json);
            return new Result<IEnumerable<ArdFilmInfoDto>>()
                   {
                       ContinuationToken = continuationToken,
                       Value = filmInfoDtos
                   };
        }

        private IEnumerable<ArdFilmInfoDto> LoadDetails(JObject json)
        {
            var categories = _videoDeserializer.ParseTeasers(json);

            //TODO bringt nichts, weil ich von TargetUrl FirstWidget nehmen muss und nicht teasers
            foreach (var category in categories)
            {
                //TODO workaround
                var details = WebClient.GetWebData<JObject>(category.TargetUrl);
                var newFilmInfo = _videoDeserializer.ParseWidgets(details).SingleOrDefault();
                category.Title = newFilmInfo?.Title;
                category.Description = newFilmInfo?.Description;
                yield return category;
                // sub item sind videos, aber ...
            }

            //return categories;
        }

        ///// <inheritdoc />
        //public override Result<IEnumerable<DownloadDetailsDto>> GetStreams(string url, ContinuationToken continuationToken = null)
        //{
        //    throw new NotImplementedException();
        //}
        /// <inheritdoc />
    }

    /// <summary>
    ///                     case CATEGORYNAME_BROADCASTS_AZ:
    /// https://api.ardmediathek.de/page-gateway/pages/ard/editorial/experiment-a-z
    /// https://api.ardmediathek.de/page-gateway/pages/ard/editorial/experiment-a-z?embedded=false
    /// https://api.ardmediathek.de/page-gateway/pages/ard/editorial/experiment-a-z");
    /// </summary>
    public class ArdTopicsPageDeserializer : PageDeserializerBase
    {
        private static readonly string _level = "Level";

        public static string Name { get; } = "Sendungen A-Z";
        public static bool HasCategories { get; } = true;
        public static Uri EntryUrl { get; } = new Uri("https://api.ardmediathek.de/page-gateway/pages/ard/editorial/experiment-a-z");

        public ArdTopicsPageDeserializer(WebCache webClient) : base(webClient) { }

        private ArdCategoryDeserializer _categoryDeserializer = new ArdCategoryDeserializer();
        private ArdFilmInfoDeserializerNeu _videoDeserializer = new ArdFilmInfoDeserializerNeu();
        //private ArdMediaArrayConverter _mediaArrayConverter = new ArdMediaArrayConverter();

        public override Result<IEnumerable<ArdCategoryInfoDto>> GetCategories(string targetUrl, ContinuationToken continuationToken = null)
        {
            continuationToken ??= new ContinuationToken() { { _level, 0 } };

            var currentLevel = continuationToken.GetValueOrDefault(_level) as int? ?? 0;
            //var currentLevel = string.Equals(EntryUrl.AbsoluteUri, targetUrl) ? 0 : 1;

            var json = WebClient.GetWebData<JObject>(targetUrl);
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
                var details = WebClient.GetWebData<JObject>(category.TargetUrl);
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
            var json = WebClient.GetWebData<JToken>(url, cache: false);
            var filmInfoDtos = _videoDeserializer.ParseTeasers(json);

            return new Result<IEnumerable<ArdFilmInfoDto>>()
            {
                ContinuationToken = continuationToken,
                Value = filmInfoDtos
            };
        }

        //public override Result<IEnumerable<DownloadDetailsDto>> GetStreams(string url, ContinuationToken continuationToken = null)
        //{
        //    continuationToken ??= new ContinuationToken() { { _level, 0 } };

        //    var json = _webClient.GetWebData<JObject>(url, cache: false);
        //    //var json = GetWebData<JObject>(video.VideoUrl, cache: false);
        //    var collectionEmbedded = json["widgets"]?.FirstOrDefault()?["mediaCollection"]["embedded"];
        //    //var livestreamPlaylistUrl = listLiveStream["_mediaArray"].FirstOrDefault()["_mediaStreamArray"].FirstOrDefault().Value<string>("_stream"); "_stream");

        //    var streamInfoDtos = _mediaArrayConverter.ParseVideoUrls(collectionEmbedded as JObject);

        //    return new Result<IEnumerable<DownloadDetailsDto>>()
        //    {
        //        ContinuationToken = continuationToken,
        //        Value = streamInfoDtos
        //    };
        //}

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


        public IEnumerable<T> EnumerateItems<T>(IEnumerable<JToken> elements, Func<JToken, T> converter) where T : ArdInformationDtoBase
        {
            return elements.AsEmptyIfNull()
                           .ExceptDefault()
                           .Select(converter)
                           .Where(result => result != null);
        }


        protected static JToken TryGetWidgetsTokenOrInput(JToken jsonElement)
        {
            return jsonElement?.Type == JTokenType.Object && jsonElement?[ELEMENT_WIDGETS] != null 
                       ? jsonElement[ELEMENT_WIDGETS] 
                       : jsonElement;
        }


        protected static IEnumerable<JToken> TryGetTeasersTokenOrInput(JToken widgetsElement, int widgetsToUse = 1)
        {

            var selectedWidgetElements = widgetsElement?.Type == JTokenType.Array 
                                             ? widgetsElement.Children()
                                                             .Take(widgetsToUse)
                                                             .OfType<JObject>() 
                                             : new List<JObject>()
                                               {
                                                   widgetsElement as JObject
                                               };


            // ToDo SelectMany do we need the widget category, which is now omitted?
            return selectedWidgetElements.ExceptDefault()
                                         .Select(jObj => jObj[ELEMENT_TEASERS] as JArray)
                                         .First();
        }
    }

    internal class ArdCategoryDeserializer : ArdWidgetTeaserDeserializerBase
    {
        protected static readonly string ELEMENT_MEDIACOLLECTION = "mediaCollection";
        protected static readonly string ELEMENT_EMBEDDED = "embedded";

        protected static readonly string WIDGET_ATTRIBUTE_COMPILATIONTYPE = "compilationType";

        public IEnumerable<ArdCategoryInfoDto> ParseWidgets(JToken jsonElement, bool hasSubCategories = false)
        {
            var widgetsElement = TryGetWidgetsTokenOrInput(jsonElement);

            return EnumerateItems(widgetsElement as JArray, widget => ParseWidget(widget, hasSubCategories));
        }

        protected ArdCategoryInfoDto ParseWidget(JToken widgetElement, bool hasSubCategories = false)
        {
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
            var widgetsElement = TryGetWidgetsTokenOrInput(jsonElement);

            var teasers = TryGetTeasersTokenOrInput(widgetsElement);
            return EnumerateItems(teasers, teaser => ParseTeaser(teaser, hasSubCategories));
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
        public IEnumerable<ArdFilmInfoDto> ParseWidgets(JToken jsonElement)
        {
            var widgetsElement = TryGetWidgetsTokenOrInput(jsonElement);


            return EnumerateItems(widgetsElement, ParseVideoInfo);
        }

        public IEnumerable<ArdFilmInfoDto> ParseTeasers(JToken jsonElement, int widgetsToUse = 1)
        {
            var widgetsElement = TryGetWidgetsTokenOrInput(jsonElement);

            var teasers = TryGetTeasersTokenOrInput(widgetsElement);

            return EnumerateItems(teasers, ParseVideoInfo);
        }

        public ArdFilmInfoDto ParseVideoInfo(JToken teaserElement)
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
                AvailableUntilDate = teaserElement.Value<DateTime?>(ATTRIBUTE_UNTIL_DATETIME) ?? DateTime.MaxValue,
                Description = teaserElement.Value<string>(ATTRIBUTE_DESCRIPTION),
                Duration = teaserElement.Value<int>(ATTRIBUTE_DURATION),
                ImageUrl = imageUrl?.Replace(ArdMediathekUtil.PLACEHOLDER_IMAGE_WIDTH, ArdMediathekUtil.IMAGE_WIDTH)
            };
        }
    }
}

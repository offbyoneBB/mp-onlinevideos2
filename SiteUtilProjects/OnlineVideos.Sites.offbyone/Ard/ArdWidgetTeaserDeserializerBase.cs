using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;


namespace OnlineVideos.Sites.Ard
{
    internal abstract class ArdWidgetTeaserDeserializerBase
    {
        protected static readonly string ELEMENT_WIDGETS = "widgets";
        protected static readonly string ELEMENT_TEASERS = "teasers";

        protected static readonly string ELEMENT_LINKS = "links";
        protected static readonly string ELEMENT_SELF = "self";
        protected static readonly string ELEMENT_TARGET = "target";

        protected static readonly string ELEMENT_IMAGE = "image";
        protected static readonly string ELEMENT_IMAGES = "images";
        protected static readonly string ELEMENT_ASPECT_16X9 = "aspect16x9";
        protected static readonly string ELEMENT_ASPECT_4X3 = "aspect16x9";


        protected static readonly string ATTRIBUTE_ID = "id";
        protected static readonly string ATTRIBUTE_HREF = "href";
        protected static readonly string ATTRIBUTE_SRC = "src";

        protected static readonly string ATTRIBUTE_TITLE = "title";
        protected static readonly string ATTRIBUTE_TITLE_SHORT = "shortTitle";
        protected static readonly string ATTRIBUTE_DESCRIPTION = "synopsis";


        protected IEnumerable<T> EnumerateItems<T>(IEnumerable<JToken> elements, Func<JToken, T> converter)
        {
            return elements.AsEmptyIfNull()
                           .ExceptDefault()
                           .Select(converter)
                           .ExceptDefault();
        }


        protected static JToken TryGetWidgetsTokenOrInput(JToken jsonElement)
        {
            return jsonElement?.Type == JTokenType.Object && jsonElement?[ELEMENT_WIDGETS] != null 
                       ? jsonElement[ELEMENT_WIDGETS] 
                       : jsonElement;
        }


        /// <summary>
        /// extract the teasers of the first #<paramref name="takeWidgets"/> widgets
        /// </summary>
        /// <param name="widgetsElement"></param>
        /// <param name="takeWidgets"></param>
        /// <returns></returns>
        protected static IEnumerable<JToken> TryGetTeasersTokenOrInput(JToken widgetsElement, int takeWidgets = 1)
        {
            IEnumerable<JObject> selectedWidgetElements = TrySelectWidgetElements(widgetsElement, takeWidgets);

            // ToDo SelectMany do we need the widget category, which is now omitted?
            return selectedWidgetElements.ExceptDefault()
                                         .Select(jObj => jObj[ELEMENT_TEASERS] as JArray)
                                         .First();
        }


        /// <summary>
        /// get first #<paramref name="takeWidgets"/> widgets
        /// </summary>
        /// <param name="widgetsElement"></param>
        /// <param name="takeWidgets"></param>
        /// <returns></returns>
        protected static IEnumerable<JObject> TrySelectWidgetElements(JToken widgetsElement, int takeWidgets)
        {
            var selectedWidgetElements = widgetsElement?.Type == JTokenType.Array
                                             ? widgetsElement.Children()
                                                             .Take(takeWidgets)
                                                             .OfType<JObject>()
                                             : new List<JObject>()
                                               {
                                                   widgetsElement as JObject
                                               };
            return selectedWidgetElements;
        }
    }


    internal class ArdCategoryDeserializer : ArdWidgetTeaserDeserializerBase
    {
        protected static readonly string WIDGET_ATTRIBUTE_COMPILATIONTYPE = "compilationType";

        public IEnumerable<ArdCategoryInfoDto> ParseWidgets(JToken jsonElement, bool hasSubCategories = false)
        {
            var widgetsElement = TryGetWidgetsTokenOrInput(jsonElement);

            return EnumerateItems(widgetsElement as JArray, widget => ParseWidget(widget, hasSubCategories));
        }

        public IEnumerable<ArdCategoryInfoDto> ParseTeasers(JToken jsonElement, bool hasSubCategories = false, int widgetsToUse = 1)
        {
            var widgetsElement = TryGetWidgetsTokenOrInput(jsonElement);

            var teasers = TryGetTeasersTokenOrInput(widgetsElement);
            return EnumerateItems(teasers, teaser => ParseTeaser(teaser, hasSubCategories));
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

        public ArdCategoryInfoDto ParseTeaser(JToken teaserElement, bool hasSubCategories = false)
        {
            //var teaserObject = teaserElement;
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

    internal class ArdVideoInfoDeserializer : ArdWidgetTeaserDeserializerBase
    {
        protected static readonly string ATTRIBUTE_UNTIL_DATETIME = "availableTo";
        protected static readonly string ATTRIBUTE_AIR_DATETIME = "broadcastedOn";
        protected static readonly string ATTRIBUTE_DURATION = "duration";

        protected static readonly string ATTRIBUTE_NUMBER_OF_CLIPS = "numberOfClips";

        public IEnumerable<ArdVideoInfoDto> ParseWidgets(JToken jsonElement, int takeWidgets = int.MaxValue)
        {
            var widgetsElement = TryGetWidgetsTokenOrInput(jsonElement);
            var selectedWidgetElements = TrySelectWidgetElements(widgetsElement, takeWidgets);

            Func<JToken, ArdVideoInfoDto> videoInfoParser = IsVideoElement(selectedWidgetElements.FirstOrDefault()) ? ParseWidgetVideoInfo : ParseTeaserVideoInfo;
            return EnumerateItems(selectedWidgetElements, videoInfoParser);
        }


        private static bool IsVideoElement(JObject widgetElement)
        {
            return widgetElement?[ATTRIBUTE_AIR_DATETIME] != null;
        }


        public IEnumerable<ArdVideoInfoDto> ParseTeasers(JToken jsonElement, int widgetsToUse = 1)
        {
            var teasers = ParseTeasersInternal(jsonElement, widgetsToUse);


            return EnumerateItems(teasers, ParseTeaserVideoInfo);
        }

        public IEnumerable<string> ParseTeasersUrl(JToken jsonElement, int widgetsToUse = 1)
        {
            var teasers = ParseTeasersInternal(jsonElement, widgetsToUse);

            return EnumerateItems(teasers, GetTeaserTargetUrl);
        }


        private static IEnumerable<JToken> ParseTeasersInternal(JToken jsonElement, int widgetsToUse)
        {
            var widgetsElement = TryGetWidgetsTokenOrInput(jsonElement);

            var teasers = TryGetTeasersTokenOrInput(widgetsElement);

            return teasers;
        }

        private static string GetTeaserTargetUrl(JToken teaserElement)
        {
            return teaserElement[ELEMENT_LINKS]?[ELEMENT_TARGET]?.Value<string>(ATTRIBUTE_HREF);
        }

        public ArdVideoInfoDto ParseTeaserVideoInfo(JToken teaserElement)
        {
            var id = teaserElement[ELEMENT_LINKS]?[ELEMENT_TARGET]?.Value<string>(ATTRIBUTE_ID) ??
                        teaserElement.Value<string>(ATTRIBUTE_ID);
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            var url = GetTeaserTargetUrl(teaserElement);
            var imageUrl = teaserElement[ELEMENT_IMAGES]?[ELEMENT_ASPECT_16X9]?.Value<string>(ATTRIBUTE_SRC) ??
                            teaserElement[ELEMENT_IMAGES]?[ELEMENT_ASPECT_4X3]?.Value<string>(ATTRIBUTE_SRC);

            var numberOfClips = teaserElement.Value<int?>(ATTRIBUTE_NUMBER_OF_CLIPS) ?? 0;

            return new ArdVideoInfoDto(id, numberOfClips, url)
            {
                Title = teaserElement.Value<string>(ATTRIBUTE_TITLE),
                AirDate = teaserElement.Value<DateTime?>(ATTRIBUTE_AIR_DATETIME),
                AvailableUntilDate = teaserElement.Value<DateTime?>(ATTRIBUTE_UNTIL_DATETIME),
                Description = teaserElement.Value<string>(ATTRIBUTE_DESCRIPTION),
                Duration = teaserElement.Value<int>(ATTRIBUTE_DURATION),
                ImageUrl = imageUrl?.Replace(ArdMediathekUtil.PLACEHOLDER_IMAGE_WIDTH, ArdMediathekUtil.IMAGE_WIDTH)
            };
        }

        public ArdVideoInfoDto ParseWidgetVideoInfo(JToken itemElement)
        {
            var id = itemElement.Value<string>(ATTRIBUTE_ID);
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            // fskRating
            // tracking.atiCustomVars.clipLength
            // widgets[0].mediaCollection.embedded._duration
            var url = itemElement[ELEMENT_LINKS]?[ELEMENT_SELF]?.Value<string>(ATTRIBUTE_HREF);
            var imageUrl = itemElement[ELEMENT_IMAGE]?.Value<string>(ATTRIBUTE_SRC);

            var numberOfClips = itemElement.Value<int?>(ATTRIBUTE_NUMBER_OF_CLIPS) ?? 0;

            return new ArdVideoInfoDto(id, numberOfClips, url)
                   {
                       Title = itemElement.Value<string>(ATTRIBUTE_TITLE),
                       AirDate = itemElement.Value<DateTime?>(ATTRIBUTE_AIR_DATETIME),
                       AvailableUntilDate = itemElement.Value<DateTime?>(ATTRIBUTE_UNTIL_DATETIME),
                       Description = itemElement.Value<string>(ATTRIBUTE_DESCRIPTION),
                       Duration = ArdMediaStreamsDeserializer.GetDuration(itemElement),
                       ImageUrl = imageUrl?.Replace(ArdMediathekUtil.PLACEHOLDER_IMAGE_WIDTH, ArdMediathekUtil.IMAGE_WIDTH)
                   };
        }
    }

    internal class ArdMediaStreamsDeserializer : ArdWidgetTeaserDeserializerBase
    {
        protected static readonly string ELEMENT_MEDIACOLLECTION = "mediaCollection";
        protected static readonly string ELEMENT_EMBEDDED = "embedded";

        protected static readonly string ATTRIBUTE_DURATION = "_duration";
        protected static readonly string ATTRIBUTE_TYPE = "_type"; //"video"
        protected static readonly string ATTRIBUTE_IS_LIVE = "_isLive";
        protected static readonly string ATTRIBUTE_PREVIEW_IMAGE = "_previewImage";

        protected ArdMediaArrayConverter MediaArrayConverter { get; } = new ArdMediaArrayConverter();

        public IEnumerable<DownloadDetailsDto> ParseWidgets(JToken jsonElement)
        {
            var widgetsElement = TryGetWidgetsTokenOrInput(jsonElement);
            var firstWidget = TrySelectWidgetElements(widgetsElement, 1).FirstOrDefault();
            var collectionEmbedded = firstWidget?[ELEMENT_MEDIACOLLECTION][ELEMENT_EMBEDDED];

            return MediaArrayConverter.ParseVideoUrls(collectionEmbedded as JObject);
        }


        public static int? GetDuration(JToken jsonElement)
        {
            return jsonElement?[ELEMENT_MEDIACOLLECTION][ELEMENT_EMBEDDED]?.Value<int>(ATTRIBUTE_DURATION);
        }
    }
}

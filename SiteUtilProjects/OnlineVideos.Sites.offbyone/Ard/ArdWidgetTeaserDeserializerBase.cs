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

        protected static readonly string ELEMENT_IMAGES = "images";
        protected static readonly string ELEMENT_ASPECT_16X9 = "aspect16x9";
        protected static readonly string ELEMENT_ASPECT_4X3 = "aspect16x9";


        protected static readonly string ATTRIBUTE_ID = "id";
        protected static readonly string ATTRIBUTE_HREF = "href";
        protected static readonly string ATTRIBUTE_SRC = "src";

        protected static readonly string ATTRIBUTE_TITLE = "title";
        protected static readonly string ATTRIBUTE_TITLE_SHORT = "shortTitle";
        protected static readonly string ATTRIBUTE_DESCRIPTION = "synopsis";


        protected IEnumerable<T> EnumerateItems<T>(IEnumerable<JToken> elements, Func<JToken, T> converter) where T : ArdInformationDtoBase
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
                AirDate = teaserElement.Value<DateTime?>(ATTRIBUTE_AIR_DATETIME),
                AvailableUntilDate = teaserElement.Value<DateTime?>(ATTRIBUTE_UNTIL_DATETIME),
                Description = teaserElement.Value<string>(ATTRIBUTE_DESCRIPTION),
                Duration = teaserElement.Value<int>(ATTRIBUTE_DURATION),
                ImageUrl = imageUrl?.Replace(ArdMediathekUtil.PLACEHOLDER_IMAGE_WIDTH, ArdMediathekUtil.IMAGE_WIDTH)
            };
        }
    }

}

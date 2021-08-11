using Newtonsoft.Json.Linq;

using OnlineVideos.Sites.Zdf;

using System;
using System.Collections.Generic;
using System.Linq;

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

    internal abstract class PageDeserializerBase
    {
        protected ArdMediaArrayConverter MediaArrayConverter { get; } = new ArdMediaArrayConverter();
        protected ArdCategoryDeserializer CategoryDeserializer { get; } = new ArdCategoryDeserializer();
        protected ArdFilmInfoDeserializerNeu VideoDeserializer { get; } = new ArdFilmInfoDeserializerNeu();
        protected WebCache WebClient { get; }

        protected PageDeserializerBase(WebCache webClient) => WebClient = webClient;

        public abstract ArdCategoryInfoDto RootCategory { get; }

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


    internal class ArdLiveStreamsDeserializer : PageDeserializerBase
    {
        public static string Name { get; } = "Live TV";
        public static bool HasCategories { get; } = false;
        public static Uri EntryUrl { get; } = new Uri("https://api.ardmediathek.de/page-gateway/widgets/ard/editorials/4hEeBDgtx6kWs6W6sa44yY");

        public override ArdCategoryInfoDto RootCategory { get; } = new ArdCategoryInfoDto("", EntryUrl.AbsoluteUri)
                                                                   {
                                                                       Title = Name,
                                                                       //Description = "",
                                                                       HasSubCategories = false,
                                                                       //ImageUrl = ,
                                                                       //TargetUrl = 
                                                                   };

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
            var categories = VideoDeserializer.ParseTeasers(json);

            foreach (var category in categories)
            {
                //TODO workaround
                var details = WebClient.GetWebData<JObject>(category.TargetUrl);
                var newFilmInfo = VideoDeserializer.ParseWidgets(details).SingleOrDefault();
                category.Title = newFilmInfo?.Title;
                category.Description = newFilmInfo?.Description;
                yield return category;
                // sub item sind videos, aber ...
            }
        }
    }

    /// <summary>
    ///                     case CATEGORYNAME_BROADCASTS_AZ:
    /// https://api.ardmediathek.de/page-gateway/pages/ard/editorial/experiment-a-z
    /// https://api.ardmediathek.de/page-gateway/pages/ard/editorial/experiment-a-z?embedded=false
    /// https://api.ardmediathek.de/page-gateway/pages/ard/editorial/experiment-a-z");
    /// </summary>
    internal class ArdTopicsPageDeserializer : PageDeserializerBase
    {
        private static readonly string _categoryLevel = "Level";

        public static string Name { get; } = "Sendungen A-Z";
        public static bool HasCategories { get; } = true;
        public static Uri EntryUrl { get; } = new Uri("https://api.ardmediathek.de/page-gateway/pages/ard/editorial/experiment-a-z");

        public override ArdCategoryInfoDto RootCategory { get; } = new ArdCategoryInfoDto("", EntryUrl.AbsoluteUri)
                                                                   {
                                                                       Title = Name,
                                                                       //Description = "",
                                                                       HasSubCategories = false,
                                                                       //ImageUrl = ,
                                                                       //TargetUrl = 
                                                                   };

        public ArdTopicsPageDeserializer(WebCache webClient) : base(webClient) { }


        public override Result<IEnumerable<ArdCategoryInfoDto>> GetCategories(string targetUrl, ContinuationToken continuationToken = null)
        {
            continuationToken ??= new ContinuationToken() { { _categoryLevel, 0 } };

            var currentLevel = continuationToken.GetValueOrDefault(_categoryLevel) as int? ?? 0;
            //var currentLevel = string.Equals(EntryUrl.AbsoluteUri, targetUrl) ? 0 : 1;

            var json = WebClient.GetWebData<JObject>(targetUrl);
            var categoryInfos = currentLevel switch
            {
                0 => CategoryDeserializer.ParseWidgets(json, hasSubCategories: true), // load A - Z
                1 => LoadDetails(json), // load e.g. Abendschau - skip level, (load infos from nextlevel) for each category load url and read synopsis
                //2 => categoryDeserializer.ParseTeasers(json), // videos...
                _ => throw new ArgumentOutOfRangeException(),
            };

            var newToken = new ContinuationToken(continuationToken);
            newToken[_categoryLevel] = currentLevel + 1;
            return new Result<IEnumerable<ArdCategoryInfoDto>>
            {
                ContinuationToken = newToken,
                Value = categoryInfos
            };
        }

        private IEnumerable<ArdCategoryInfoDto> LoadDetails(JObject json)
        {
            var categories = CategoryDeserializer.ParseTeasers(json);

            foreach (var category in categories)
            {
                //TODO workaround
                var details = WebClient.GetWebData<JObject>(category.TargetUrl);
                var newCategory = CategoryDeserializer.ParseTeaser(details);
                category.Title = newCategory.Title;
                category.Description = newCategory.Description;
                yield return category;
                // sub item sind videos, aber ...
            }
        }

        public override Result<IEnumerable<ArdFilmInfoDto>> GetVideos(string url, ContinuationToken continuationToken = null)
        {
            var json = WebClient.GetWebData<JToken>(url, cache: false);
            var filmInfoDtos = VideoDeserializer.ParseTeasers(json);

            return new Result<IEnumerable<ArdFilmInfoDto>>()
            {
                ContinuationToken = continuationToken,
                Value = filmInfoDtos
            };
        }
    }


    internal class ArdDayPageDeserializer : PageDeserializerBase
    {
        private static readonly string _categoryLevel = "Level";

        private static readonly string DAY_PAGE  = "https://api.ardmediathek.de//page-gateway/compilations/{0}/pastbroadcasts?startDateTime={1}T00:00:00.000Z&endDateTime={2}T23:59:59.000Z&pageNumber=0&pageSize={3}";

        /// <inheritdoc />
        public override ArdCategoryInfoDto RootCategory { get; } = new ArdCategoryInfoDto(nameof(ArdDayPageDeserializer), string.Empty)
                                                                   {
                                                                       Title = "Was lief",
                                                                       Description = "Sendungen der letzten 7 Tage.",
                                                                       HasSubCategories = true,
                                                                       //ImageUrl = ,
                                                                       //TargetUrl = 
                                                                   };

        public ArdDayPageDeserializer(WebCache webClient) : base(webClient) { }


        /// <inheritdoc />
        public override Result<IEnumerable<ArdCategoryInfoDto>> GetCategories(string targetUrl, ContinuationToken continuationToken = null)
        {
            continuationToken ??= new ContinuationToken() { { _categoryLevel, 0 } };
            var currentLevel = continuationToken.GetValueOrDefault(_categoryLevel) as int? ?? 0;

            var categoryInfos = LastSevenDays();

            var newToken = new ContinuationToken(continuationToken);
            newToken[_categoryLevel] = currentLevel + 1;
            return new Result<IEnumerable<ArdCategoryInfoDto>>
                   {
                       ContinuationToken = newToken,
                       Value = categoryInfos
                   };

        }


        private IEnumerable<ArdCategoryInfoDto> LastSevenDays()
        {
            const string DAY_PAGE_DATE_FORMAT = "yyyy-MM-dd";
            static string CreateDayUrl(string partnerName, DateTime day)
                => $"https://api.ardmediathek.de//page-gateway/compilations/{partnerName}/pastbroadcasts" +
                   $"?startDateTime={day.ToString(DAY_PAGE_DATE_FORMAT)}T00:00:00.000Z" +
                   $"&endDateTime={day.ToString(DAY_PAGE_DATE_FORMAT)}T23:59:59.000Z" +
                   $"&pageNumber=0" +
                   $"&pageSize={ArdConstants.DAY_PAGE_SIZE}";

            for (var i = 0; i <= 7; i++)
            {
                var day = DateTime.Today.AddDays(-i);
                var url = CreateDayUrl("daserste", day);
                yield return new ArdCategoryInfoDto(nameof(ArdDayPageDeserializer) + i, url) 
                             { 
                                 Title = i switch
                                 {
                                     0 => "Heute",
                                     1 => "Gestern",
                                     _ => day.ToString("ddd, d.M.")
                                 },
                                 //Url = url,
                                 //HasSubCategories = true,
                                 //ImageUrl = 
                             };
            }
        }

        /// <inheritdoc />
        public override Result<IEnumerable<ArdFilmInfoDto>> GetVideos(string url, ContinuationToken continuationToken = null)
        {
            var json = WebClient.GetWebData<JToken>(url, cache: false);
            var filmInfos = VideoDeserializer.ParseTeasers(json);

            return new Result<IEnumerable<ArdFilmInfoDto>>()
                   {
                       ContinuationToken = continuationToken,
                       Value = filmInfos
                   };
        //            return filmInfo;
        }
    }

    //ArdFilmDeserialize (ArdFilmInfoDto) -> ArdVideoInfoJsonDeserializer (ArdVideoDTO) --> ArdMediaArrayToDownloadUrlsConverter ( Map<Qualities, URL> )

    internal class ArdMediaArrayConverter
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
}

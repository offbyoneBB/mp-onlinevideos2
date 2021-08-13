using Newtonsoft.Json.Linq;

using OnlineVideos.Sites.Zdf;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

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
        protected ArdCategoryDeserializer CategoryDeserializer { get; } = new ArdCategoryDeserializer();
        protected ArdVideoInfoDeserializer VideoDeserializer { get; } = new ArdVideoInfoDeserializer();
        protected ArdMediaStreamsDeserializer VideoStreamsDeserializer { get; } = new ArdMediaStreamsDeserializer();

        protected WebCache WebClient { get; }

        protected PageDeserializerBase(WebCache webClient) => WebClient = webClient;

        public abstract ArdCategoryInfoDto RootCategory { get; }

        public abstract Result<IEnumerable<ArdCategoryInfoDto>> GetCategories(string url, ContinuationToken continuationToken = null);

        public virtual Result<IEnumerable<ArdVideoInfoDto>> GetVideos(string url, ContinuationToken continuationToken = null)
        {
            var json = WebClient.GetWebData<JToken>(url, cache: false, proxy: WebRequest.GetSystemWebProxy());
            var detailUrls = VideoDeserializer.ParseTeasersUrl(json);
            var filmInfos = LoadVideosWithDetails(detailUrls);

            return new Result<IEnumerable<ArdVideoInfoDto>>()
                   {
                       ContinuationToken = continuationToken,
                       Value = filmInfos
            };
        }

        private IEnumerable<ArdVideoInfoDto> LoadVideosWithDetails(IEnumerable<string> urls)
        {
            foreach (var url in urls)
            {
                yield return GetVideoDetails(url);
            }
        }


        private ArdVideoInfoDto GetVideoDetails(string url)
        {
            var details = WebClient.GetWebData<JObject>(url, proxy: WebRequest.GetSystemWebProxy());
            var filmInfo = VideoDeserializer.ParseWidgets(details, takeWidgets: 1).FirstOrDefault();
            return filmInfo;
        }


        public virtual Result<IEnumerable<DownloadDetailsDto>> GetStreams(string url, ContinuationToken continuationToken = null)
        {
            //continuationToken ??= new ContinuationToken() { { _level, 0 } };

            var json = WebClient.GetWebData<JToken>(url, cache: false, proxy: WebRequest.GetSystemWebProxy());
            var streamInfos = VideoStreamsDeserializer.ParseWidgets(json);

            return new Result<IEnumerable<DownloadDetailsDto>>()
                   {
                       ContinuationToken = continuationToken,
                       Value = streamInfos
                   };
        }
    }

    public class Result<T> //where T : ArdInformationDtoBase
    {
        public T Value { get; set; }
        public ContinuationToken ContinuationToken { get; set; }
    }

    internal class ArdHomeDeserializer : PageDeserializerBase
    {
        private static readonly string _categoryLevel = "Level";

        public static Uri EntryUrl { get; } = new Uri("https://api.ardmediathek.de/page-gateway/pages/ard/home?embedded=true");

        public override ArdCategoryInfoDto RootCategory { get; } = new ArdCategoryInfoDto(nameof(Ard), EntryUrl.AbsoluteUri)
        {
            Title = "Home", //"Highlights",
            //Description = "",
            HasSubCategories = true,
            //ImageUrl = ,
        };

        public ArdHomeDeserializer(WebCache webClient) : base(webClient) { }

        public override Result<IEnumerable<ArdCategoryInfoDto>> GetCategories(string url, ContinuationToken continuationToken = null)
        {
            continuationToken ??= new ContinuationToken() { { _categoryLevel, 0 } };

            var currentLevel = continuationToken.GetValueOrDefault(_categoryLevel) as int? ?? 0;
            Log.Debug($"GetCategories current Level: {currentLevel}");

            var json = WebClient.GetWebData<JObject>(url, proxy: WebRequest.GetSystemWebProxy());
            var categoryInfos = currentLevel switch
            {
                0 => CategoryDeserializer.ParseWidgets(json, hasSubCategories: true), // load A - Z
                //1 => LoadCategoriesWithDetails(json), // load e.g. Abendschau - skip level, (load infos from nextlevel) for each category load url and read synopsis
                ////2 => categoryDeserializer.ParseTeasers(json), // videos...
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
    }

    internal class ArdLiveStreamsDeserializer : PageDeserializerBase
    {
        public static Uri EntryUrl { get; } = new Uri("https://api.ardmediathek.de/page-gateway/widgets/ard/editorials/4hEeBDgtx6kWs6W6sa44yY");

        public override ArdCategoryInfoDto RootCategory { get; } = new ArdCategoryInfoDto("", EntryUrl.AbsoluteUri)
                                                                   {
                                                                       Title = "Live TV",
                                                                       //Description = "",
                                                                       HasSubCategories = false,
                                                                       //ImageUrl = ,
                                                                       //TargetUrl =
                                                                   };

        public ArdLiveStreamsDeserializer(WebCache webClient) : base(webClient) { }

        /// <inheritdoc />
        public override Result<IEnumerable<ArdCategoryInfoDto>> GetCategories(string targetUrl, ContinuationToken continuationToken = null) => throw new NotImplementedException();
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

        public static Uri EntryUrl { get; } = new Uri("https://api.ardmediathek.de/page-gateway/pages/ard/editorial/experiment-a-z");

        public override ArdCategoryInfoDto RootCategory { get; } = new ArdCategoryInfoDto("", EntryUrl.AbsoluteUri)
                                                                   {
                                                                       Title = "Sendungen A-Z",
                                                                       //Description = "",
                                                                       HasSubCategories = true,
                                                                       //ImageUrl = ,
                                                                       //TargetUrl =
                                                                   };

        public ArdTopicsPageDeserializer(WebCache webClient) : base(webClient) { }


        public override Result<IEnumerable<ArdCategoryInfoDto>> GetCategories(string targetUrl, ContinuationToken continuationToken = null)
        {
            continuationToken ??= new ContinuationToken() { { _categoryLevel, 0 } };

            var currentLevel = continuationToken.GetValueOrDefault(_categoryLevel) as int? ?? 0;
            Log.Debug($"GetCategories current Level: {currentLevel}");

            var json = WebClient.GetWebData<JObject>(targetUrl, proxy: WebRequest.GetSystemWebProxy());
            var categoryInfos = currentLevel switch
            {
                0 => CategoryDeserializer.ParseWidgets(json, hasSubCategories: true), // load A - Z
                1 => LoadCategoriesWithDetails(json), // load e.g. Abendschau - skip level, (load infos from nextlevel) for each category load url and read synopsis
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

        private IEnumerable<ArdCategoryInfoDto> LoadCategoriesWithDetails(JObject json)
        {
            var categories = CategoryDeserializer.ParseTeasers(json);

            foreach (var category in categories)
            {
                //TODO workaround
                var details = WebClient.GetWebData<JObject>(category.TargetUrl, proxy: WebRequest.GetSystemWebProxy());
                var newCategory = CategoryDeserializer.ParseTeaser(details);
                category.Title = newCategory.Title;
                category.Description = newCategory.Description;
                yield return category;
                // sub item sind videos, aber ...
            }
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

            var categoryInfos = currentLevel switch
            {
                0 => LastSevenDays(),
                1 => PartnerNames(targetUrl),
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


        private static readonly string PLACEHOLDER_PARTNERNAME = "{{partnerName}}";

        private IEnumerable<ArdCategoryInfoDto> LastSevenDays()
        {
            const string DAY_PAGE_DATE_FORMAT = "yyyy-MM-dd";
            static string CreateDayUrl(DateTime day)
                => $"https://api.ardmediathek.de//page-gateway/compilations/{PLACEHOLDER_PARTNERNAME}/pastbroadcasts" +
                   $"?startDateTime={day.ToString(DAY_PAGE_DATE_FORMAT)}T00:00:00.000Z" +
                   $"&endDateTime={day.ToString(DAY_PAGE_DATE_FORMAT)}T23:59:59.000Z" +
                   $"&pageNumber=0" +
                   $"&pageSize={ArdConstants.DAY_PAGE_SIZE}";

            for (var i = 0; i <= 7; i++)
            {
                var day = DateTime.Today.AddDays(-i);
                var url = CreateDayUrl(day);
                yield return new ArdCategoryInfoDto(nameof(ArdDayPageDeserializer) + i, url)
                             {
                                 Title = i switch
                                 {
                                     0 => "Heute",
                                     1 => "Gestern",
                                     _ => day.ToString("ddd, d.M.")
                                 },
                                 //Url = url,
                                 HasSubCategories = true,
                                 //ImageUrl =
                             };
            }
        }

        private IEnumerable<ArdCategoryInfoDto> PartnerNames(string placeholderUrl)
        {
            static string CreatePartnerUrl(string placeholderUrl, string partnerName)
                => placeholderUrl.Replace(PLACEHOLDER_PARTNERNAME, partnerName);

            foreach (var partner in ArdPartner.Values)
            {
                var url = CreatePartnerUrl(placeholderUrl, partner);
                yield return new ArdCategoryInfoDto(nameof(ArdDayPageDeserializer) + partner, url)
                {
                    Title = partner.DisplayName,
                    //Url = url,
                    //HasSubCategories = true,
                    //ImageUrl =
                };
            }
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

        private static IEnumerable<DownloadDetailsDto> ParseMediaStreamStream(JToken videoElement, Qualities quality)
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

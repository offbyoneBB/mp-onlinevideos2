using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net;

using Google.Apis.Util;
using HtmlAgilityPack;

using Newtonsoft.Json.Linq;


//using static OnlineVideos.Sites.FilmStartsUtil;

namespace OnlineVideos.Sites.Zdf
{
    public class ZDFMediathekUtil : SiteUtilBase
    {
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Video Quality", TranslationFieldName = "VideoQuality"), Description("Defines the maximum quality for the video to be played (" + nameof(Qualities.HD) + "/" + nameof(Qualities.High) + "/" + nameof(Qualities.Normal) + "/" + nameof(Qualities.Small) + ").")]
        protected string videoQuality = "HD";

        [Category("OnlineVideosUserConfiguration"), Description("MIME Type that is preferred, if several exist."), LocalizableDisplayName("MIME type preferred")]
        protected string preferredMimeType = "video/webm";

        [Category("OnlineVideosUserConfiguration"), Description("MIME Types (comma seperated) priority order (left highest prio)"), LocalizableDisplayName("MIME types priority")]
        protected string prioOrderMimeTypes = "video/webm, video/mp4, application/x-mpegURL";

        private IEnumerable<string> GetMimeTypesPrioOrder()
            => prioOrderMimeTypes.Split(new [] {',', ';'}, StringSplitOptions.RemoveEmptyEntries).ToList();
            //{ "video/webm", "video/mp4", "application/x-mpegURL"};
        //private static readonly string RELEVANT_MIME_TYPE = "video/mp4";
        //private static readonly string RELEVANT_MIME_TYPE = "video/webm";

        private static readonly string RELEVANT_TEASERIMAGE_LAYOUT = "384x216";
        //private static readonly string RELEVANT_TEASERIMAGE_LAYOUT = "768x432";


        private static readonly NameValueCollection _defaultHeaders = new NameValueCollection { { "Accept-Encoding", "gzip" }, { "Accept", "*/*" } };

        private const string CATEGORYNAME_LIVESTREAM = "Live TV";
        private const string CATEGORYNAME_MISSED_BROADCAST = "Sendung Verpasst";
        private const string CATEGORYNAME_RUBRICS = "Rubriken";
        private const string CATEGORYNAME_BROADCASTS_AZ = "Sendungen A-Z";

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            Settings.Categories.Add(new Category { Name = CATEGORYNAME_LIVESTREAM });
            Settings.Categories.Add(new Category { Name = CATEGORYNAME_MISSED_BROADCAST, HasSubCategories = true, Description = "Sendungen der letzten 7 Tage." });
            Settings.Categories.Add(new Category { Name = CATEGORYNAME_RUBRICS, HasSubCategories = true });
            Settings.Categories.Add(new Category { Name = CATEGORYNAME_BROADCASTS_AZ, HasSubCategories = true });
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            // .net 4.0 SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)(0xc0 | 0x300 | 0xc00);

            var document = GetWebData<HtmlDocument>("https://www.zdf.de", headers: _defaultHeaders);
            _searchBearer = ParseBearerIndexPage(document.DocumentNode.Descendants("head").Single(), "script", "'");
            _videoBearer = ParseBearerIndexPage(document.DocumentNode.Descendants("body").Single(), "script", "\"");
        }


        private NameValueCollection HeadersWithSearchBearer()
        {
            return new NameValueCollection(_defaultHeaders)
            {
                { "Api-Auth", $"Bearer {_searchBearer}" }
            };
        }

        private NameValueCollection HeadersWithVideohBearer()
        {
            return new NameValueCollection(_defaultHeaders)
            {
                { "Api-Auth", $"Bearer {_videoBearer}" }
            };
        }

        private static readonly string PLACEHOLDER_PLAYER_ID = "{playerId}";
        private static readonly string PLAYER_ID = "ngplayer_2_4";
        //private static readonly string PLAYER_ID = "portal";

        private static readonly string JSON_API_TOKEN = "apiToken";
        private string _searchBearer;
        private string _videoBearer;

        private string ParseBearerIndexPage(HtmlNode aDocumentNode, string aQuery, string aStringQuote)
        {

            var scriptElements = aDocumentNode.Descendants(aQuery);
            foreach (var scriptElement in scriptElements)
            {
                var script = scriptElement.InnerHtml;

                var value = ParseBearer(script, aStringQuote);
                if (!value.IsNullOrEmpty())
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private string ParseBearer(string aJson, string aStringQuote)
        {
            var bearer = "";

            var indexToken = aJson.IndexOf(JSON_API_TOKEN);

            if (indexToken <= 0)
            {
                return bearer;
            }
            var indexStart = aJson.IndexOf(aStringQuote, indexToken + JSON_API_TOKEN.Length + 1) + 1;
            var indexEnd = aJson.IndexOf(aStringQuote, indexStart);

            if (indexStart > 0)
            {
                bearer = aJson.Substring(indexStart, indexEnd - indexStart);
            }

            return bearer;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (parentCategory.ParentCategory == null)
            {
                switch (parentCategory.Name)
                {
                    case CATEGORYNAME_MISSED_BROADCAST:
                        if (parentCategory.SubCategories != null &&
                            parentCategory.SubCategories.Count > 0 &&
                            parentCategory.SubCategories[0].Name == DateTime.Today.ToString("dddd, d.M.yyy"))
                        { /* no need to rediscover if day hasn't changed */ }
                        else
                        {
                            parentCategory.SubCategories = new List<Category>();
                            for (var i = 0; i <= 7; i++)
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
                    case CATEGORYNAME_BROADCASTS_AZ:
                        parentCategory.SubCategories = new List<Category>();
                        var showsUrl = "https://api.zdf.de/content/documents/sendungen-100.json?profile=default";
                        foreach (var show in GetWebData<JObject>(showsUrl, headers: HeadersWithSearchBearer())["brand"].SelectMany(l => l["teaser"] ?? Enumerable.Empty<JToken>()))
                        {
                            var category = CategoryFromJson(show, parentCategory, false);
                            category.Url = string.Format("https://api.zdf.de/search/documents{0}?q=*&contentTypes=episode&sortOrder=desc&sortBy=date", show["http://zdf.de/rels/target"].Value<string>("structureNodePath"));
                            parentCategory.SubCategories.Add(category);
                        }
                        parentCategory.SubCategoriesDiscovered = true;
                        break;
                    case CATEGORYNAME_RUBRICS:
                        parentCategory.SubCategories = new List<Category>();
                        var catUrl = "https://api.zdf.de/search/documents?q=*&types=page-index&contentTypes=category";
                        foreach (var cat in GetWebData<JObject>(catUrl, headers: HeadersWithSearchBearer())["http://zdf.de/rels/search/results"])
                        {
                            var category = CategoryFromJson(cat, parentCategory, true);
                            category.Url = string.Format("https://api.zdf.de/search/documents{0}?q=*&contentTypes=brand&sortOrder=desc&sortBy=relevance", cat["http://zdf.de/rels/target"].Value<string>("structureNodePath"));
                            parentCategory.SubCategories.Add(category);
                        }
                        parentCategory.SubCategoriesDiscovered = true;
                        break;
                }
            }
            else
            {
                parentCategory.SubCategories = new List<Category>();
                var json = GetWebData<JObject>(((RssLink)parentCategory).Url, headers: HeadersWithSearchBearer());
                foreach (var show in json["http://zdf.de/rels/search/results"])
                {
                    var category = CategoryFromJson(show, parentCategory, false);
                    category.Url = string.Format("https://api.zdf.de/search/documents{0}?q=*&contentTypes=episode&sortOrder=desc&sortBy=date", show["http://zdf.de/rels/target"].Value<string>("structureNodePath"));
                    parentCategory.SubCategories.Add(category);
                }
                parentCategory.SubCategoriesDiscovered = true;
            }
            return parentCategory.SubCategories.Count;
        }

        class TvServiceDto
        {
            public string Title { get; set; }
            public string LogoUrl { get; set; }
            public string VideoUrl { get; set; }
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            var list = new List<VideoInfo>();
            var headers = HeadersWithSearchBearer();

            if (category.Name == CATEGORYNAME_LIVESTREAM)
            {
                // TODO nice, but downloads a 5MB Json, just to dynamically retrieve some data for more or less static streams
                //var json = GetWebData<JObject>("https://api.zdf.de/content/documents/epg-livetv-100.json?profile=default", headers: headers, cache: false);
                //var teasers = (json["livestreams"] as JArray)?.First?["teaser"] as JArray;
                //foreach (var teaser in teasers)
                //{
                //    var title = teaser["http://zdf.de/rels/target"].Value<string>("tvService");
                //    var img = teaser["http://zdf.de/rels/target"]["teaserImageRef"]["layouts"].Value<string>(RELEVANT_TEASERIMAGE_LAYOUT);
                //    var url = "https://api.zdf.de" + teaser["http://zdf.de/rels/target"]["mainVideoContent"]["http://zdf.de/rels/target"].Value<string>("http://zdf.de/rels/streams/ptmd-template").Replace(PLACEHOLDER_PLAYER_ID, PLAYER_ID);

                //    list.Add(new VideoInfo
                //    {
                //        Title = title,
                //        Thumb = img,
                //        VideoUrl = url
                //    });
                //}
                var baseUri = new Uri("https://api.zdf.de");
                var tvServices = new List<TvServiceDto>()
                {
                    new TvServiceDto() { Title = "ZDF", LogoUrl = @"https://www.zdf.de/assets/2400-zdf-100~768x432?cb=1619595262367", VideoUrl = @"/tmd/2/{playerId}/live/ptmd/247onAir-201" },
                    new TvServiceDto() { Title = "ZDFneo", LogoUrl = @"https://www.zdf.de/assets/2400-zdfneo-100~768x432?cb=1599052578312", VideoUrl = @"/tmd/2/{playerId}/live/ptmd/247onAir-202" },
                    new TvServiceDto() { Title = "ZDFinfo", LogoUrl = @"https://www.zdf.de/assets/2400-zdfinfo-100~768x432?cb=1599052544190", VideoUrl = @"/tmd/2/{playerId}/live/ptmd/247onAir-203" },
                    new TvServiceDto() { Title = "3sat", LogoUrl = @"https://www.zdf.de/assets/2400-3sat-100~768x432?cb=1549376862930", VideoUrl = @"/tmd/2/{playerId}/live/ptmd/247onAir-204" },
                    new TvServiceDto() { Title = "PHOENIX", LogoUrl = @"https://www.zdf.de/assets/2400-phoenix-100~768x432?cb=1624941994702", VideoUrl = @"/tmd/2/{playerId}/live/ptmd/247onAir-205" },
                    new TvServiceDto() { Title = "KI.KA", LogoUrl = @"https://www.zdf.de/assets/2400_kika-100~768x432?cb=1479309336008", VideoUrl = @"/tmd/2/{playerId}/live/ptmd/247onAir-207" },
                    new TvServiceDto() { Title = "arte", LogoUrl = @"https://www.zdf.de/assets/2400-arte-100~768x432?cb=1537858145018", VideoUrl = @"/tmd/2/{playerId}/live/ptmd/247onAir-208" },
                }.Select(tvService =>
                    new VideoInfo
                    {
                        Title = tvService.Title,
                        Thumb = tvService.LogoUrl,
                        VideoUrl = new Uri(baseUri, tvService.VideoUrl.Replace(PLACEHOLDER_PLAYER_ID, PLAYER_ID)).ToString()
                    }
                );

                list.AddRange(tvServices);
            }
            else if (category.ParentCategory.Name == CATEGORYNAME_MISSED_BROADCAST)
            {

                var json = GetWebData<JObject>((category as RssLink).Url, headers: headers, cache: false);
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
                    var img = video_page_teaser["teaserImageRef"]?["layouts"]?.Value<string>(RELEVANT_TEASERIMAGE_LAYOUT);
                    var url = mainVideoContent["http://zdf.de/rels/target"].Value<string>("http://zdf.de/rels/streams/ptmd-template")?.Replace(PLACEHOLDER_PLAYER_ID, PLAYER_ID)?.Insert(0, "https://api.zdf.de");

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

                    if (!obj.Value<bool>("hasVideo")) continue;

                    var videoContent = obj["mainVideoContent"]["http://zdf.de/rels/target"];

                    var title = obj.Value<string>("teaserHeadline");
                    var start = obj.Value<DateTime>("editorialDate");
                    var img = obj["teaserImageRef"]["layouts"]?.Value<string>(RELEVANT_TEASERIMAGE_LAYOUT);

                    var length = TimeSpan.FromSeconds(videoContent.Value<int>("duration"));
                    var url = "https://api.zdf.de" + videoContent.Value<string>("http://zdf.de/rels/streams/ptmd-template").Replace(PLACEHOLDER_PLAYER_ID, PLAYER_ID);
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

        public override string GetVideoUrl(VideoInfo video)
        {
            var bestVideoQualityUrl = string.Empty;
            var headers = HeadersWithVideohBearer();

            if (video.PlaybackOptions == null)
            {
                if (string.IsNullOrWhiteSpace(video.VideoUrl))
                    throw new OnlineVideosException("Video nicht verfügbar!");

                var json = GetWebData<JObject>(video.VideoUrl, headers: headers);
                parseVideoUrls(json);
                //var playbackOptions = new HashSet<KeyValuePair<string, string>>(KeyValuePairComparer.KeyOrdinalIgnoreCase);
                //foreach (var formitaet in json["priorityList"].SelectMany(l => l["formitaeten"]))
                //{
                //    if (formitaet["facets"].Any(f => f.ToString() == "restriction_useragent"))
                //        continue;

                //    var type = formitaet.Value<string>("type");
                //    foreach (var vid in formitaet["qualities"])
                //    {
                //        var quality = vid.Value<string>("quality");
                //        if (quality == "auto")
                //            continue;
                //        var url = vid["audio"]["tracks"].First.Value<string>("uri");

                //        if (url.EndsWith(".m3u8") || url.EndsWith(".webm"))
                //            continue;

                //        if (url.Contains("master.m3u8"))
                //        {
                //            var m3u8Data = GetWebData(url);
                //            var m3u8PlaybackOptions = HlsPlaylistParser.GetPlaybackOptions(m3u8Data, video.VideoUrl);
                //            playbackOptions.UnionWith(m3u8PlaybackOptions);
                //            bestVideoQualityUrl = m3u8PlaybackOptions.FirstOrDefault().Value; //Default, if m3u8 playlist cannot be collected, e.g. geoblocking
                //        }
                //        else
                //        {
                //            playbackOptions.Add(new KeyValuePair<string, string>(string.Format("{0}-{1}", quality, type), url));
                //        }
                //    }
                //}

                //video.PlaybackOptions = playbackOptions.ToDictionary(e => e.Key, e => e.Value);
                //var orderedPlaybackOptions = playbackOptionsWorkaround.OrderBy(i => (int) i.Key).ToList();
                var xxx = videoStreamsWorkaround.Where(s => string.Equals("deu", s.Language, StringComparison.OrdinalIgnoreCase))
                    .ToLookup(s => s.MimeType)
                    .ToDictionary(grp => grp.Key, StringComparer.OrdinalIgnoreCase);
                //                        grp => grp.OrderBy(i => (int) i.Quality).ToDictionary(i => i.Quality, i => i.Url));

                var mimeTypes = new List<string>(new string[] { preferredMimeType }.Concat(GetMimeTypesPrioOrder()));
                var selectedMimeType = mimeTypes.FirstOrDefault(mime => xxx.ContainsKey(mime));

                var dls = string.IsNullOrEmpty(selectedMimeType) ? xxx.FirstOrDefault().Value : xxx[selectedMimeType];
                var orderedPlaybackOptions = dls?.OrderBy(i => (int)i.Quality).ToDictionary(i => i.Quality, i => i.Url);

                video.PlaybackOptions = orderedPlaybackOptions?.ToDictionary(e => e.Key.ToString(), e => e.Value);
            }

            //return !string.IsNullOrWhiteSpace(bestVideoQualityUrl) ? bestVideoQualityUrl : video.PlaybackOptions.LastOrDefault().Value;
            return video.PlaybackOptions.FirstOrDefault().Value;
        }

        static RssLink CategoryFromJson(JToken result, Category parent, bool hasSubCategories)
        {
            var obj = result["http://zdf.de/rels/target"];
            var title = obj.Value<string>("teaserHeadline");
            var desc = obj.Value<string>("teasertext");
            var thumb = obj["teaserImageRef"]["layouts"]?.Value<string>(RELEVANT_TEASERIMAGE_LAYOUT);

            var videoCounterObj = obj["http://zdf.de/rels/search/page-video-counter-with-video"];
            var videoCount = !hasSubCategories && videoCounterObj != null ? videoCounterObj.Value<uint?>("totalResultsCount") : null;

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


        private const string ZDF_QUALITY_VERYHIGH = "veryhigh";
        private const string ZDF_QUALITY_HIGH = "high";
        private const string ZDF_QUALITY_MED = "med";
        private const string ZDF_QUALITY_LOW = "low";

        private static readonly string JSON_ELEMENT_ATTRIBUTES = "attributes";
        private static readonly string JSON_ELEMENT_AUDIO = "audio";
        private static readonly string JSON_ELEMENT_CAPTIONS = "captions";
        private static readonly string JSON_ELEMENT_CLASS = "class";
        private static readonly string JSON_ELEMENT_FORMITAET = "formitaeten";
        private static readonly string JSON_ELEMENT_GEOLOCATION = "geoLocation";
        private static readonly string JSON_ELEMENT_HD = "hd";
        private static readonly string JSON_ELEMENT_LANGUAGE = "language";
        private static readonly string JSON_ELEMENT_MIMETYPE = "mimeType";
        private static readonly string JSON_ELEMENT_PRIORITYLIST = "priorityList";
        private static readonly string JSON_ELEMENT_QUALITY = "quality";
        private static readonly string JSON_ELEMENT_TRACKS = "tracks";
        private static readonly string JSON_ELEMENT_URI = "uri";

        private static readonly String CLASS_AD = "ad";

        //private static readonly string RELEVANT_MIME_TYPE = "video/mp4";
        private static readonly string RELEVANT_SUBTITLE_TYPE = ".xml";
        private static readonly string JSON_ELEMENT_QUALITIES = "qualities";

        private void parseVideoUrls(/*DownloadDto dto,*/ JObject rootNode)
        {
            //TODO Reset workaround
            videoStreamsWorkaround = new HashSet<DownloadDetailsDto>();

            // array priorityList
            var priorityList = rootNode[JSON_ELEMENT_PRIORITYLIST];
            foreach (var priority in priorityList)
            {
                parsePriority(/*dto,*/ priority);
            }
        }

        private void parsePriority(/*DownloadDto dto,*/ JToken priority)
        {
            if (priority == null)
            {
                return;
            }

            // array formitaeten
            var formitaetList = priority[JSON_ELEMENT_FORMITAET];
            foreach (var formitaet in formitaetList)
            {
                parseFormitaet(/*dto,*/ formitaet);
            }
        }

        private void parseFormitaet(/*DownloadDto dto,*/ JToken formitaet)
        {
            var mimeType = formitaet[JSON_ELEMENT_MIMETYPE];
            if (mimeType == null
                //|| !string.Equals(RELEVANT_MIME_TYPE, mimeType.ToString(), StringComparison.OrdinalIgnoreCase)
                )
            {
                return;
            }
            // array Resolution
            var qualityList = formitaet[JSON_ELEMENT_QUALITIES];
            foreach (var quality in qualityList)
            {

                var qualityValue = parseVideoQuality(quality);

                // subelement audio
                var audio = quality[JSON_ELEMENT_AUDIO];
                if (audio == null)
                {
                    continue;
                }
                // array tracks
                var tracks = audio[JSON_ELEMENT_TRACKS];

                foreach (var trackElement in tracks)
                {
                    extractTrack(/*dto,*/ mimeType.ToString(), qualityValue, trackElement);
                }
            }
        }

        private void extractTrack(/*DownloadDto aDto,*/ string mimeType, Qualities qualityValue, JToken aTrackElement)
        {
            var trackObject = aTrackElement;
            var classValue = trackObject[JSON_ELEMENT_CLASS].ToString();
            var language = trackObject[JSON_ELEMENT_LANGUAGE].ToString();
            var uri = trackObject[JSON_ELEMENT_URI].ToString();

            // films with audiodescription are handled as a language
            if (string.Equals(CLASS_AD, classValue, StringComparison.OrdinalIgnoreCase))
            {
                language += "-ad";
            }
            if (uri != null)
            {
                //aDto.addUrl(language, qualityValue, uri);
                var dl = new DownloadDetailsDto(mimeType: mimeType, language: language, qualityValue, uri);
                videoStreamsWorkaround.Add(dl);
            }
            else
            {
                //throw new RuntimeException("either quality or uri is null");
            }
        }

        //private HashSet<KeyValuePair<Qualities, string>> playbackOptionsWorkaround = new HashSet<KeyValuePair<Qualities, string>>();
        private HashSet<DownloadDetailsDto> videoStreamsWorkaround = new HashSet<DownloadDetailsDto>();

        private Qualities parseVideoQuality(JToken quality)
        {
            Qualities qualityValue;
            var hd = quality[JSON_ELEMENT_HD];
            if (hd != null && (bool)hd)
            {
                qualityValue = Qualities.HD;
            }
            else
            {
                string zdfQuality = quality[JSON_ELEMENT_QUALITY].ToString();
                switch (zdfQuality)
                {
                    case ZDF_QUALITY_LOW:
                        qualityValue = Qualities.Small;
                        break;
                    case ZDF_QUALITY_MED:
                        qualityValue = Qualities.Small;
                        break;
                    case ZDF_QUALITY_HIGH:
                        qualityValue = Qualities.Normal;
                        break;
                    case ZDF_QUALITY_VERYHIGH:
                        qualityValue = Qualities.High;
                        break;
                    default:
                        qualityValue = Qualities.Small;
                        break;
                }
            }
            return qualityValue;
        }
    }


    internal class DownloadDetailsDto : IEquatable<DownloadDetailsDto>
    {
        public DownloadDetailsDto(string mimeType, string language, Qualities quality, string url)
        {
            MimeType = mimeType;
            Language = language;
            Quality = quality;
            Url = url;
        }

        public string MimeType { get; }
        public string Language { get; }
        public Qualities Quality { get; }
        public string Url { get; }

        public bool Equals(DownloadDetailsDto other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Language, other.Language, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(MimeType, other.MimeType, StringComparison.OrdinalIgnoreCase)
                   && Quality == other.Quality;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals((DownloadDetailsDto)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (MimeType != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(MimeType) : 0);
                hashCode = (hashCode * 397) ^ (Language != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Language) : 0);
                hashCode = (hashCode * 397) ^ (int)Quality;
                return hashCode;
            }
        }

        public static bool operator ==(DownloadDetailsDto left, DownloadDetailsDto right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DownloadDetailsDto left, DownloadDetailsDto right)
        {
            return !Equals(left, right);
        }
    }

    internal class DownloadDto : HashSet<DownloadDetailsDto>
    {
        //private HashSet<DownloadDetailsDto> downloadDetails;

        //public DownloadDto()
        //{
        //    downloadDetails = new HashSet<DownloadDetailsDto>();
        //}


        //public void addUrl(string language, Qualities quality, String url)
        //{
        //    if (!downloadUrls.containsKey(language))
        //    {
        //        downloadUrls.put(language, new EnumMap<>(Qualities.class));
        //    }

        //    Map<Qualities, String> urlMap = downloadUrls.get(language);
        //    urlMap.put(quality, url);
        //}

        //public IEnumerable<DownloadDetailsDto> Prefered(String language)
        //{
        //    if (downloadUrls.containsKey(language))
        //    {
        //        return downloadUrls.get(language);
        //    }

        //    return new EnumMap<>(Qualities.class);
        //}


        //public string getUrl(String language, Qualities resolution)
        //{
        //    if (downloadUrls.containsKey(language))
        //    {
        //        Map<Qualities, String> urlMap = downloadUrls.get(language);
        //        if (urlMap.containsKey(resolution))
        //        {
        //            return Optional.of(urlMap.get(resolution));
        //        }
        //    }
        //    return Optional.empty();
        //}



        //public Set<String> getLanguages()
        //{
        //    return downloadUrls.keySet();
        //}



        ////public GeoLocation GeoLocation { get; set; }
        //public string SubTitleUrl { get; set; }
    }


    internal enum Qualities
    {
        HD,
        High,
        Normal,
        Small
    }
}


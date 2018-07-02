using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using OnlineVideos.Helpers;

namespace OnlineVideos.Sites
{
    public static class Extensions
    {
        public static bool Contains(this string source, string value, StringComparison comparison)
        {
            return source?.IndexOf(value, comparison) >= 0;
        }
    }
    public class JsonResponse
    {
        [JsonProperty("_type")]
        public string Type { get; set; }
        [JsonProperty("_isLive")]
        public bool IsLive { get; set; }
        [JsonProperty("_defaultQuality")]
        public object[] DefaultQuality { get; set; }
        public string _previewImage { get; set; }
        public int _subtitleOffset { get; set; }
        [JsonProperty("_mediaArray")]
        public Mediaarray[] MediaArray { get; set; }
    }

    public class Mediaarray
    {
        public int _plugin { get; set; }
        [JsonProperty("_mediaStreamArray")]
        public Mediastreamarray[] MediaStreamArray { get; set; }
    }

    public class Mediastreamarray
    {
        [JsonProperty("_quality")]
        public string Quality { get; set; }
        [JsonProperty("_server")]
        public string Server { get; set; }
        [JsonProperty("_cdn")]
        public string Cdn { get; set; }
        [JsonProperty("_stream")]
        public object Stream { get; set; }
    }
    public class DasErsteMediathekUtil : SiteUtilBase
    {
        public enum VideoQuality
        {
            Low = 0,
            Med = 1,
            High = 2,
            VeryHigh = 3,
            VeryHigh2 = 4,
        };

        private const string CATEGORYNAME_LIVESTREAM = "Livestreams";
        private const string CATEGORYNAME_SENDUNG_VERPASST = "Sendung verpasst?";
        private const string CATEGORYNAME_SENDUNGEN_AZ = "Sendungen A-Z";
        private const string CATEGORYNAME_RUBRIKEN = "Rubriken";

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Video Quality", TranslationFieldName = "VideoQuality"), Description("Choose your preferred quality for the videos according to bandwidth.")]
        VideoQuality videoQuality = VideoQuality.HD;

        string nextPageUrl;

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Add(new RssLink() { Name = CATEGORYNAME_LIVESTREAM, Url = "https://www.ardmediathek.de/tv/live" });
            Settings.Categories.Add(new RssLink() { Name = CATEGORYNAME_SENDUNG_VERPASST, HasSubCategories = true, Url = "https://www.ardmediathek.de/tv/sendungVerpasst" });
            Settings.Categories.Add(new RssLink() { Name = CATEGORYNAME_SENDUNGEN_AZ, HasSubCategories = true, Url = "https://www.ardmediathek.de/tv/sendungen-a-z" });

            Uri baseUri = new Uri("https://www.ardmediathek.de/tv");
            var baseDoc = GetWebData<HtmlDocument>(baseUri.AbsoluteUri);
            foreach (var category in ExtractCategoriesFromHeadlines(baseDoc.DocumentNode, baseUri))
            {
                Settings.Categories.Add(category);
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        private IEnumerable<RssLink> ExtractCategoriesFromHeadlines(HtmlNode document, RssLink parentCategory)
        {
            return ExtractCategoriesFromHeadlines(document, new Uri(parentCategory.Url), parentCategory);
        }

        private IEnumerable<RssLink> ExtractCategoriesFromHeadlines(HtmlNode document, Uri baseUri, Category parentCategory = null)
        {
			//Create Function Delegate in order to find a certain category section on a page again, especially if a headline category has multiple pages
            Func<HtmlNode, IList<HtmlNode>> modHeadlinesFunc = (doc) => doc.Descendants("h2").Where(h2 => h2.GetAttributeValue("class", "") == "modHeadline").ToList();
            var modHeadlines = modHeadlinesFunc.Invoke(document);
            if (modHeadlines.Count == 1)
            {
                // Skip if only one Category, treat this as Video Links Only
                yield break;
            }

            foreach (var modHeadline in modHeadlines.Where(modHeadline => !modHeadline.InnerText.Contains(CATEGORYNAME_LIVESTREAM, StringComparison.OrdinalIgnoreCase)))
            {
                var categoryName = HttpUtility.HtmlDecode(string.Join("",modHeadline.Elements("#text").Select(t => t.InnerText.Trim())));
                var categorySection = modHeadline.ParentNode;
                var moreLink = categorySection.Descendants("a").FirstOrDefault(a => a.GetAttributeValue("class", "") == "more");
                //TODO:instead of MoreLink paging on site
                var pages = categorySection.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "") == "controls paging")?.Descendants("a").Select(a => new Uri(baseUri, HttpUtility.HtmlDecode(a.GetAttributeValue("href", "")))).Distinct();

                var category = new RssLink()
                {
                    Url = moreLink == null ? baseUri.AbsoluteUri : new Uri(baseUri, moreLink.GetAttributeValue("href", "")).AbsoluteUri,
                    Name = categoryName,
                    ParentCategory = parentCategory,
                    HasSubCategories = !SubItemsAreMedias(categorySection),
                };

                if (moreLink == null)
                {
					
                    //TODO: Ignore Livestream Category links of Rubrik Nachrichten
                    category.SubCategories = ExtractSubcategoriesFromDiv(categorySection, category).Cast<Category>().ToList();
                    category.SubCategoriesDiscovered = category.SubCategories.Any();
                    //now create concrete Function for this section and presever in Other information for paging
					HtmlNode CategorySectionFunc(HtmlNode doc) => modHeadlinesFunc.Invoke(doc).Single(headline => headline.InnerText.Equals(modHeadline.InnerText)).ParentNode;
                    category.Other = (Func<HtmlNode, HtmlNode>) CategorySectionFunc;
                }

                yield return category;
            }
        }


        private static bool SubItemsAreMedias(HtmlNode htmlNode)
        {
			//check for any kind of media, not only video
            var mediaLinkTypes = new [] {"/Video?", "/Audio?"};

            var teasers = htmlNode.DescendantsAndSelf("div").Where(d => d.GetAttributeValue("class", "") == "teaser").ToArray();
            if (!teasers.Any())
            {
                return false;
            }
            var allTeaserLinks = teasers.SelectMany( teaser => teaser.Descendants("a")).Select(a => a.GetAttributeValue("href", "")).Distinct().ToArray();
            //TODO: All Contains("/Video?") or Any Contains("/Video?") ????
            return allTeaserLinks.Any() && allTeaserLinks.All(link => mediaLinkTypes.Any(link.Contains));
        }

        private static IEnumerable<RssLink> ExtractSubcategoriesFromDiv(HtmlNode mainDiv, RssLink parentCategory)
        {
            var baseUri = new Uri(parentCategory.Url);
            foreach (var teaser in mainDiv.Descendants("div").Where(div => div.GetAttributeValue("class", "") == "teaser").Where(div => !SubItemsAreMedias(div)))
            {
                var headline = teaser.Descendants("h4").FirstOrDefault(h4 => h4.GetAttributeValue("class", "") == "headline");
                if (headline == null || headline.InnerText.Contains(parentCategory.Name))
                {
                    continue;
                }

                RssLink subCategory = new RssLink()
                {
                    ParentCategory = parentCategory,
                    Name = HttpUtility.HtmlDecode(headline.InnerText.Trim()),
                };

                var img = teaser.Descendants("img").FirstOrDefault();
                if (img != null) subCategory.Thumb = new Uri(baseUri, JObject.Parse(HttpUtility.HtmlDecode(img.GetAttributeValue("data-ctrl-image", ""))).Value<string>("urlScheme").Replace("##width##", "256")).AbsoluteUri;

                var link = teaser.Descendants("a").FirstOrDefault();
                if (link != null) subCategory.Url = new Uri(baseUri, HttpUtility.HtmlDecode(link.GetAttributeValue("href", ""))).AbsoluteUri;

                var textWrapper = teaser.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "") == "textWrapper");
                var subtitle = textWrapper?.Descendants("p").FirstOrDefault(div => div.GetAttributeValue("class", "") == "subtitle");
                if (subtitle != null) subCategory.Description = subtitle.InnerText;

                yield return subCategory;
            }
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            var currentCategory = parentCategory as RssLink;
            currentCategory.SubCategories = new List<Category>();
            var myBaseUri = new Uri(currentCategory.Url);
            var baseDoc = GetWebData<HtmlDocument>(myBaseUri.AbsoluteUri);

            if (currentCategory.Name == CATEGORYNAME_SENDUNGEN_AZ)
            {
                foreach (HtmlNode entry in baseDoc.DocumentNode.Descendants("ul").FirstOrDefault(ul => ul.GetAttributeValue("class", "") == "subressorts raster").Elements("li"))
                {
                    var a = entry.Descendants("a").FirstOrDefault();
                    RssLink letter = new RssLink() { Name = a.InnerText.Trim(), ParentCategory = currentCategory, HasSubCategories = true, SubCategories = new List<Category>() };
                    if (!string.IsNullOrEmpty(a.GetAttributeValue("href", "")))
                    {
                        letter.Url = new Uri(myBaseUri, a.GetAttributeValue("href", "")).AbsoluteUri;
                        currentCategory.SubCategories.Add(letter);
                    }
                }
                currentCategory.SubCategoriesDiscovered = currentCategory.SubCategories.Count > 0;
            }
            else if (currentCategory.Name == CATEGORYNAME_SENDUNG_VERPASST)
            {
                var senderDiv = baseDoc.DocumentNode.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "").Contains("modSender"))
                    .Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "").Contains("controls"));
                foreach (HtmlNode entry in senderDiv.Descendants("div").Where(div => div.GetAttributeValue("class", "") == "entry" || div.GetAttributeValue("class", "") == "entry active"))
                {
                    var a = entry.Descendants("a").FirstOrDefault();
                    if (a != null && a.GetAttributeValue("href", "") != "#")
                    {
                        var tvStation = CreateRssLinkFromAnchor(a, myBaseUri, currentCategory);
                        tvStation.HasSubCategories = true;
                        tvStation.SubCategories = new List<Category>();
                        currentCategory.SubCategories.Add(tvStation);
                    }
                }
                currentCategory.SubCategoriesDiscovered = currentCategory.SubCategories.Count > 0;
            }
            else if (currentCategory.ParentCategory != null && currentCategory.ParentCategory.Name == CATEGORYNAME_SENDUNG_VERPASST)
            {
                var programmDiv = baseDoc.DocumentNode.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "").Contains("modProgramm"))
                    .Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "").Contains("controls"));
                foreach (HtmlNode entry in programmDiv.Descendants("div").Where(div => div.GetAttributeValue("class", "") == "entry" || div.GetAttributeValue("class", "") == "entry active").Skip(1))
                {
                    var a = entry.Descendants("a").FirstOrDefault();
                    var dayLink = CreateRssLinkFromAnchor(a, myBaseUri, currentCategory);
                    var j = HttpUtility.HtmlDecode(entry.GetAttributeValue("data-ctrl-programmloader-source", ""));
                    if (!string.IsNullOrWhiteSpace(j))
                    {
                        var f = JObject.Parse(j);
                        dayLink.Name += " " + HttpUtility.UrlDecode(f.Value<string>("pixValue")).Split('/')[1];
                    }
                    currentCategory.SubCategories.Add(dayLink);
                }

                currentCategory.SubCategories.Reverse();
            }
            else if (currentCategory.Name == CATEGORYNAME_RUBRIKEN)
            {
                var pages = baseDoc.DocumentNode.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "") == "controls paging")?.Descendants("a").Select(a => new Uri(myBaseUri, HttpUtility.HtmlDecode(a.GetAttributeValue("href", "")))).Distinct();

                foreach (var page in pages)//.Except(new [] {myBaseUri}))
                {
                    baseDoc = GetWebData<HtmlDocument>(page.AbsoluteUri);
                    var mainDiv = baseDoc.DocumentNode.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "") == "elementWrapper").Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "") == "boxCon");
                    foreach (var subCategory in ExtractSubcategoriesFromDiv(mainDiv, currentCategory))
                    {
                        subCategory.HasSubCategories = true;
                        currentCategory.SubCategories.Add(subCategory);
                    }
                }
                //var mainDiv = baseDoc.DocumentNode.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "") == "elementWrapper").Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "") == "boxCon");
                //foreach (var subCategory in ExtractSubcategoriesFromDiv(mainDiv, currentCategory))
                //{
                //    subCategory.HasSubCategories = true;
                //    currentCategory.SubCategories.Add(subCategory);
                //}
                currentCategory.SubCategoriesDiscovered = currentCategory.SubCategories.Count > 0;
            }
            else
            {
                //TODO: SubCategories with pages (rubriken)
                //TODO: Workaround
                var subCategories = ExtractCategoriesFromHeadlines(baseDoc.DocumentNode, currentCategory).ToList();
                if (!subCategories.Any())
                {
                    //sendungen-a-z?buchstabe=A
                    var mainDiv = baseDoc.DocumentNode.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "") == "elementWrapper").Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "") == "boxCon");
                    subCategories = ExtractSubcategoriesFromDiv(mainDiv, currentCategory).ToList();
                }
                currentCategory.SubCategories.AddRange(subCategories);
                currentCategory.SubCategoriesDiscovered = currentCategory.SubCategories.Count > 0;
            }

            return currentCategory.SubCategories.Count;
        }

        private static RssLink CreateRssLinkFromAnchor(HtmlNode a, Uri myBaseUri, Category parentCategory)
        {
            var name = string.Join("", a.ChildNodes.Where(elem => elem.GetAttributeValue("class", "") != "hidden").Select(elem => elem.InnerHtml.Trim()));
            var rssLink = new RssLink()
            {
                Name = name,
                Url = new Uri(myBaseUri, HttpUtility.HtmlDecode(a.GetAttributeValue("href", ""))).AbsoluteUri,
                ParentCategory = parentCategory
            };
            return rssLink;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            HasNextPage = false;

            var myBaseUri = new Uri((category as RssLink).Url);
            var baseDoc = GetWebData<HtmlDocument>(myBaseUri.AbsoluteUri);

            var result = new List<VideoInfo>();
            if (category.Name == CATEGORYNAME_LIVESTREAM)
            {
                //var result2 = new List<VideoInfo>();
                //var programmDivNew = baseDoc.DocumentNode.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "").Contains("modMini"));
                //foreach (HtmlNode entry in programmDivNew.Descendants("div").Where(div => div.GetAttributeValue("class", "") == "teaser"))
                //{
                //    var img = entry.Descendants("img").Select(a => HttpUtility.HtmlDecode(a.GetAttributeValue("src", ""))).First(src => !string.IsNullOrWhiteSpace(src)); ;
                //    var url = entry.Descendants("a").Select(a => HttpUtility.HtmlDecode(a.GetAttributeValue("href", ""))).First(href => !string.IsNullOrWhiteSpace(href));
                //    var title = entry.Descendants("h4").First().InnerText.Trim();
                //    var subtitle = entry.Descendants("p").FirstOrDefault(p => p.GetAttributeValue("class", "").Contains("subtitle"));
                //    var timeline = entry.Descendants("div").First(div => div.GetAttributeValue("class", "").Contains("timeline"));

                //    result.Add(new VideoInfo()
                //    {
                //        Title = title,
                //        VideoUrl = new Uri(myBaseUri, url).AbsoluteUri,
                //        Thumb = new Uri(myBaseUri, img).AbsoluteUri,

                //    });
                //}

                var programmDiv = baseDoc.DocumentNode.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "").Contains("modSender"))
                    .Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "").Contains("controls"));
                foreach (HtmlNode entry in programmDiv.Descendants("div").Where(div => div.GetAttributeValue("class", "") == "entry" || div.GetAttributeValue("class", "") == "entry active").Skip(1))
                {
                    var a = entry.Descendants("a").FirstOrDefault();
                    if (a != null && a.GetAttributeValue("href", "").Length > 1)
                    {
                        result.Add(new VideoInfo()
                        {
                            Title = a.InnerText.Trim(),
                            VideoUrl = new Uri(myBaseUri, HttpUtility.HtmlDecode(a.GetAttributeValue("href", ""))).AbsoluteUri
                        });
                    }
                }
            }
            else if (myBaseUri.AbsoluteUri.Contains("sendungVerpasst"))
            {
                var programmDiv = baseDoc.DocumentNode.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "").Contains("modProgramm"));
                foreach (var boxDiv in programmDiv.Descendants("div").Where(div => div.GetAttributeValue("class", "") == "box"))
                {
                    foreach (var entryDiv in boxDiv.Elements("div").FirstOrDefault().Elements("div").Where(div => div.GetAttributeValue("class", "") == "entry"))
                    {
                        var start = entryDiv.Descendants("span").FirstOrDefault(span => span.GetAttributeValue("class", "") == "date").InnerText;
                        var title = entryDiv.Descendants("span").FirstOrDefault(span => span.GetAttributeValue("class", "") == "titel").InnerText;
                        foreach (var teaser in entryDiv.Descendants("div").Where(div => div.GetAttributeValue("class", "") == "teaser"))
                        {
                            var video = new VideoInfo();
                            var img = teaser.Descendants("img").FirstOrDefault();
                            if (img != null) video.Thumb = new Uri(myBaseUri, JObject.Parse(HttpUtility.HtmlDecode(img.GetAttributeValue("data-ctrl-image", ""))).Value<string>("urlScheme").Replace("##width##", "256")).AbsoluteUri;

                            var textWrapper = teaser.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "") == "textWrapper");
                            if (textWrapper != null)
                            {
                                video.VideoUrl = new Uri(myBaseUri, HttpUtility.HtmlDecode(textWrapper.Element("a").GetAttributeValue("href", ""))).AbsoluteUri;
                                video.Title = textWrapper.Descendants("h4").FirstOrDefault().InnerText.Trim();
                                if (video.Title != title) video.Title = title + " - " + video.Title;
                                video.Length = textWrapper.Descendants("p").FirstOrDefault(div => div.GetAttributeValue("class", "") == "subtitle").InnerText.Split('|')[0].Trim();
                                video.Airdate = start + " Uhr";
                                result.Add(video);
                            }
                        }
                    }
                }
            }
            else if (myBaseUri.AbsoluteUri.Contains("/Video?"))
            {
                return new List<VideoInfo>() { new VideoInfo() { Title = category.Name, VideoUrl = myBaseUri.AbsoluteUri, Description = category.Description, Thumb = category.Thumb } };
            }
            else
            {
                HtmlNode mainDiv;
                if (category.Other is Func<HtmlNode, HtmlNode> getCategoryDiv)
                {
					//special handling for multiple categories on same page, need to find matching DIV for getting videos
                    mainDiv = getCategoryDiv.Invoke(baseDoc.DocumentNode);
                }
                else
                {
                    mainDiv = baseDoc.DocumentNode.Descendants("div").LastOrDefault(div => div.GetAttributeValue("class", "").Contains("modMini")).ParentNode;
                }
                result = GetVideosFromDiv(mainDiv, myBaseUri);
            }
            return result;
        }

        List<VideoInfo> GetVideosFromDiv(HtmlNode mainDiv, Uri myBaseUri)
        {
            var result = new List<VideoInfo>();
            foreach (var teaser in mainDiv.Descendants("div").Where(div => div.GetAttributeValue("class", "") == "teaser"))
            {
                var link = teaser.Descendants("a").FirstOrDefault();
                if (link != null)
                {
                    var video = new VideoInfo();
                    video.VideoUrl = new Uri(myBaseUri, HttpUtility.HtmlDecode(link.GetAttributeValue("href", ""))).AbsoluteUri;
                    if (!video.VideoUrl.Contains("/Video?"))
                        continue;

                    var img = teaser.Descendants("img").FirstOrDefault();
                    if (img != null) video.Thumb = new Uri(myBaseUri, JObject.Parse(HttpUtility.HtmlDecode(img.GetAttributeValue("data-ctrl-image", ""))).Value<string>("urlScheme").Replace("##width##", "256")).AbsoluteUri;

                    var headline = teaser.Descendants("h4").FirstOrDefault(h4 => h4.GetAttributeValue("class", "") == "headline");
                    if (headline != null) video.Title = HttpUtility.HtmlDecode(headline.InnerText.Trim());

                    var textWrapper = teaser.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "") == "textWrapper");
                    if (textWrapper != null)
                    {
                        var dachzeile = HttpUtility.HtmlDecode(textWrapper.Descendants("p").FirstOrDefault(div => div.GetAttributeValue("class", "") == "dachzeile").InnerText);
                        video.Description = dachzeile;

                        var teaserParagraph = textWrapper.Descendants("p").FirstOrDefault(div => div.GetAttributeValue("class", "") == "teasertext");
                        if (teaserParagraph != null)
                            video.Description += (string.IsNullOrEmpty(video.Description) ? "" : "\n") + teaserParagraph.InnerText;

                        var subtitleNode = textWrapper.Descendants("p").FirstOrDefault(div => div.GetAttributeValue("class", "") == "subtitle");
                        string subtitle = (subtitleNode != null && subtitleNode.ChildNodes.Count > 0) ? subtitleNode.ChildNodes[0].InnerText : "";
                        if (subtitle.Contains('|'))
                        {

                            foreach (var subtitleSplit in subtitle.Split('|'))
                            {
                                if (subtitleSplit.Contains("min"))
                                    video.Length = subtitleSplit.Trim();
                                else if (subtitleSplit.Count(c => c == '.') == 2)
                                    video.Airdate = subtitleSplit.Trim();
                            }
                        }
                        else
                        {
                            video.Length = subtitle;
                            video.Airdate = dachzeile;
                        }
                    }
                    result.Add(video);
                }
            }

            // paging
            var nextPageLink = mainDiv.Descendants("a").FirstOrDefault(a => a.GetAttributeValue("href", "") != "" && a.InnerText.Trim() == HttpUtility.HtmlEncode(">"));
            HasNextPage = nextPageLink != null;
            if (HasNextPage)
                nextPageUrl = new Uri(myBaseUri, HttpUtility.HtmlDecode(nextPageLink.GetAttributeValue("href", ""))).AbsoluteUri;

            return result;
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            var myBaseUri = new Uri(nextPageUrl);
            var doc = GetWebData<HtmlDocument>(nextPageUrl);
            var mainDiv = doc.DocumentNode.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "").Contains("modList") || div.GetAttributeValue("class", "").Contains("modMini")).ParentNode;
            return GetVideosFromDiv(mainDiv, myBaseUri);
        }

        public override bool CanSearch { get { return true; } }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            var searchUrl = string.Format("http://www.ardmediathek.de/tv/suche?searchText={0}", HttpUtility.UrlEncode(query));
            var myBaseUri = new Uri(searchUrl);
            var doc = GetWebData<HtmlDocument>(searchUrl);
            var mainDiv = doc.DocumentNode.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "").Contains("modList")).ParentNode;
            return GetVideosFromDiv(mainDiv, myBaseUri).ConvertAll(v => v as SearchResultItem);
        }

        public override String GetVideoUrl(VideoInfo video)
        {
            var baseDoc = GetWebData<HtmlDocument>(video.VideoUrl);
            var mediaDiv = baseDoc.DocumentNode.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("data-ctrl-player", "") != "");
            if (mediaDiv != null)
            {
                var configUrl = new Uri(new Uri(video.VideoUrl), JObject.Parse(HttpUtility.HtmlDecode(mediaDiv.GetAttributeValue("data-ctrl-player", ""))).Value<string>("mcUrl")).AbsoluteUri;
                var mediaString = GetWebData<string>(configUrl);
                var mediaJson = JsonConvert.DeserializeObject<JsonResponse>(mediaString);
                var playbackOptions = new HashSet<KeyValuePair<string, string>>(KeyValuePairComparer.KeyOrdinalIgnoreCase);
                int qualityNumber;
                foreach (var media in mediaJson.MediaArray.SelectMany(m => m.MediaStreamArray).Select(streamArray => new
                {
                        Quality = int.TryParse(streamArray.Quality, out qualityNumber) ? ((VideoQuality)qualityNumber).ToString() : "HD",
                        Url = streamArray.Stream is JArray ? ((JArray)streamArray.Stream).Values<string>().OrderByDescending(item => item, StringComparer.OrdinalIgnoreCase).First() : streamArray.Stream as string,
                        Server = streamArray.Server
                    }).Distinct())
                {
                    string url = media.Url;
                    if (url.EndsWith(".smil"))
                    {
                        url = GetStreamUrlFromSmil(url);
                    }

                    if (Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
                    {
                        if (Uri.IsWellFormedUriString(url, UriKind.Relative))
                        {
                            var absoluteUri = new Uri(new Uri(video.VideoUrl), url);
                            url = absoluteUri.ToString();
                        }

                        if (url.Contains("master.m3u8"))
                        {
                            var m3u8Data = GetWebData(url);
                            var m3u8PlaybackOptions = HlsPlaylistParser.GetPlaybackOptions(m3u8Data, video.VideoUrl);
                            playbackOptions.UnionWith(m3u8PlaybackOptions);
                        }
                        else
                        {
                            if (url.EndsWith("f4m"))
                            {
                                url += "?g=" + StringUtils.GetRandomLetters(12) + "&hdcore=3.8.0";
                            }
                            playbackOptions.Add(new KeyValuePair<string, string>(media.Quality, url));
                        }
                    }
                    else if (mediaJson.IsLive)
                    {
                        url = string.Empty;
                        if (string.IsNullOrEmpty(media.Url))
                        {
                            string guessedStream = media.Server.Substring(media.Server.LastIndexOf('/') + 1);
                            url = new MPUrlSourceFilter.RtmpUrl(media.Server) { Live = true, LiveStream = true, Subscribe = guessedStream, PageUrl = video.VideoUrl }.ToString();
                        }
                        else if (media.Url.Contains('?'))
                        {
                            var tcUrl = media.Server.TrimEnd('/') + media.Url.Substring(media.Url.IndexOf('?'));
                            var app = new Uri(media.Server).AbsolutePath.Trim('/') + media.Url.Substring(media.Url.IndexOf('?'));
                            var playPath = media.Url;
                            url = new MPUrlSourceFilter.RtmpUrl(tcUrl) { App = app, PlayPath = playPath, Live = true, PageUrl = video.VideoUrl, Subscribe = playPath }.ToString();
                        }
                        else
                        {
                            url = new MPUrlSourceFilter.RtmpUrl(media.Server + "/" + media.Url) { Live = true, LiveStream = true, Subscribe = media.Url, PageUrl = video.VideoUrl }.ToString();
                        }

                        playbackOptions.Add(new KeyValuePair<string, string>(media.Quality, url));
                    }
                }

                video.PlaybackOptions = playbackOptions.ToDictionary(e => e.Key, e => e.Value);
            }

            string qualitytoMatch = videoQuality.ToString();
            string firstUrl = video.PlaybackOptions.FirstOrDefault(p => p.Key.Contains(qualitytoMatch)).Value;
            return !string.IsNullOrEmpty(firstUrl) ? firstUrl : video.PlaybackOptions.LastOrDefault().Value;
        }

        string GetStreamUrlFromSmil(string smilUrl)
        {
            var doc = GetWebData<System.Xml.Linq.XDocument>(smilUrl);
            return doc.Descendants("meta").FirstOrDefault().Attribute("base").Value + doc.Descendants("video").FirstOrDefault().Attribute("src").Value;
        }

    }
}


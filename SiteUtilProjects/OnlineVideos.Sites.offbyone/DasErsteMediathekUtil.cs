using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace OnlineVideos.Sites
{
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
        public enum VideoQuality { Low, Med, High, HD };
        string m3u8Regex1 = @"#EXT-X-STREAM-INF:CODECS=""(?<codecs>[^""]+)"",BANDWIDTH=(?<bitrate>\d+).*?\n(?<url>.*)";
        string m3u8Regex2 = @"#EXT-X-STREAM-INF:PROGRAM-ID=\d,BANDWIDTH=(?<bitrate>\d+),?RESOLUTION=(?<resolution>\d+x\d+),?CODECS=""(?<codecs>[^""]+)"".*?\n(?<url>.*)";


        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Video Quality", TranslationFieldName = "VideoQuality"), Description("Choose your preferred quality for the videos according to bandwidth.")]
        VideoQuality videoQuality = VideoQuality.HD;

        string nextPageUrl;

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Add(new RssLink() { Name = "TV-Livestreams", Url = "http://www.ardmediathek.de/tv/live" });
            Settings.Categories.Add(new RssLink() { Name = "Sendung verpasst?", HasSubCategories = true, Url = "http://www.ardmediathek.de/tv/sendungVerpasst" });
            Settings.Categories.Add(new RssLink() { Name = "Sendungen A-Z", HasSubCategories = true, Url = "http://www.ardmediathek.de/tv/sendungen-a-z" });

            Uri baseUri = new Uri("http://www.ardmediathek.de/tv");
            var baseDoc = GetWebData<HtmlDocument>(baseUri.AbsoluteUri);
            foreach (var modHeadline in baseDoc.DocumentNode.Descendants("h2").Where(h2 => h2.GetAttributeValue("class", "") == "modHeadline"))
            {
                var title = HttpUtility.HtmlDecode(string.Join("", modHeadline.Elements("#text").Select(t => t.InnerText.Trim()).ToArray()));
                if (!title.ToLower().Contains("live"))
                {
                    var moreLink = modHeadline.ParentNode.Descendants("a").FirstOrDefault(a => a.GetAttributeValue("class", "") == "more");
                    if (moreLink != null)
                    {
                        Settings.Categories.Add(new RssLink() { Name = title, Url = new Uri(baseUri, moreLink.GetAttributeValue("href", "")).AbsoluteUri, HasSubCategories = !SubItemsAreVideos(modHeadline.ParentNode) });
                    }
                    else
                    {
                        var cat = new RssLink() { Name = title, Url = baseUri.AbsoluteUri, HasSubCategories = true, SubCategoriesDiscovered = true, SubCategories = new List<Category>() };
                        GetSubcategoriesFromDiv(cat, modHeadline.ParentNode);
                        Settings.Categories.Add(cat);
                    }
                }
            }
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        bool SubItemsAreVideos(HtmlNode parentNode)
        {
            var firstTeaser = parentNode.Descendants("div").FirstOrDefault(d => d.GetAttributeValue("class", "") == "teaser");
            if (firstTeaser != null)
            {
                var firstTeaserLink = firstTeaser.Descendants("a").FirstOrDefault();
                if (firstTeaserLink != null)
                {
                    return firstTeaserLink.GetAttributeValue("href", "").Contains("/Video?");
                }
            }
            return false;
        }

        void GetSubcategoriesFromDiv(RssLink parentCategory, HtmlNode mainDiv)
        {
            var myBaseUri = new Uri((parentCategory as RssLink).Url);
            foreach (var teaser in mainDiv.Descendants("div").Where(div => div.GetAttributeValue("class", "") == "teaser"))
            {
                RssLink subCategory = new RssLink() { ParentCategory = parentCategory };
                var img = teaser.Descendants("img").FirstOrDefault();
                if (img != null) subCategory.Thumb = new Uri(myBaseUri, JObject.Parse(HttpUtility.HtmlDecode(img.GetAttributeValue("data-ctrl-image", ""))).Value<string>("urlScheme").Replace("##width##", "256")).AbsoluteUri;
                var headline = teaser.Descendants("h4").FirstOrDefault(h4 => h4.GetAttributeValue("class", "") == "headline");
                if (headline != null) subCategory.Name = HttpUtility.HtmlDecode(headline.InnerText.Trim());
                var link = teaser.Descendants("a").FirstOrDefault();
                if (link != null) subCategory.Url = new Uri(myBaseUri, HttpUtility.HtmlDecode(link.GetAttributeValue("href", ""))).AbsoluteUri;

                var textWrapper = teaser.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "") == "textWrapper");
                if (textWrapper != null)
                {
                    var subtitle = textWrapper.Descendants("p").FirstOrDefault(div => div.GetAttributeValue("class", "") == "subtitle");
                    if (subtitle != null) subCategory.Description = subtitle.InnerText;
                }

                parentCategory.SubCategories.Add(subCategory);
            }
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            var myBaseUri = new Uri((parentCategory as RssLink).Url);
            var baseDoc = GetWebData<HtmlDocument>(myBaseUri.AbsoluteUri);

            if (parentCategory.Name == "Sendungen A-Z")
            {
                foreach (HtmlNode entry in baseDoc.DocumentNode.Descendants("ul").FirstOrDefault(ul => ul.GetAttributeValue("class", "") == "subressorts raster").Elements("li"))
                {
                    var a = entry.Descendants("a").FirstOrDefault();
                    RssLink letter = new RssLink() { Name = a.InnerText.Trim(), ParentCategory = parentCategory, HasSubCategories = true, SubCategories = new List<Category>() };
                    if (!string.IsNullOrEmpty(a.GetAttributeValue("href", "")))
                    {
                        letter.Url = new Uri(myBaseUri, a.GetAttributeValue("href", "")).AbsoluteUri;
                        parentCategory.SubCategories.Add(letter);
                    }
                }
                parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
            }
            else if (parentCategory.Name == "Sendung verpasst?")
            {
                var senderDiv = baseDoc.DocumentNode.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "").Contains("modSender"))
                    .Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "").Contains("controls"));
                foreach (HtmlNode entry in senderDiv.Descendants("div").Where(div => div.GetAttributeValue("class", "") == "entry" || div.GetAttributeValue("class", "") == "entry active"))
                {
                    var a = entry.Descendants("a").FirstOrDefault();
                    if (a != null && a.GetAttributeValue("href", "") != "#")
                    {
                        var tvStation = CreateRssLinkFromAnchor(a, myBaseUri, parentCategory);
                        tvStation.HasSubCategories = true;
                        tvStation.SubCategories = new List<Category>();
                        parentCategory.SubCategories.Add(tvStation);
                    }
                }
                parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
            }
            else if (parentCategory.ParentCategory == null || parentCategory.ParentCategory.Name == "Sendungen A-Z")
            {
                var mainDivs = baseDoc.DocumentNode.Descendants("div").Where(div => div.GetAttributeValue("class", "") == "elementWrapper").Select(elem => elem.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "") == "boxCon")).ToList();
                var mainDiv = baseDoc.DocumentNode.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "") == "elementWrapper")
                    .Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "") == "boxCon");
                GetSubcategoriesFromDiv(parentCategory as RssLink, mainDiv);
                parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
            }
            else if (parentCategory.ParentCategory.Name == "Sendung verpasst?")
            {
                var programmDiv = baseDoc.DocumentNode.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "").Contains("modProgramm"))
                    .Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "").Contains("controls"));
                foreach (HtmlNode entry in programmDiv.Descendants("div").Where(div => div.GetAttributeValue("class", "") == "entry" || div.GetAttributeValue("class", "") == "entry active").Skip(1))
                {
                    var a = entry.Descendants("a").FirstOrDefault();
                    var dayLink = CreateRssLinkFromAnchor(a, myBaseUri, parentCategory);
                    var j = HttpUtility.HtmlDecode(entry.GetAttributeValue("data-ctrl-programmloader-source", ""));
                    if (!string.IsNullOrWhiteSpace(j))
                    {
                        var f = JObject.Parse(j);
                        dayLink.Name += " " + HttpUtility.UrlDecode(f.Value<string>("pixValue")).Split('/')[1];
                    }
                    parentCategory.SubCategories.Add(dayLink);
                }

                parentCategory.SubCategories.Reverse();
            }

            return parentCategory.SubCategories.Count;
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
            if (category.Name == "TV-Livestreams")
            {
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
                var mainDiv = baseDoc.DocumentNode.Descendants("div").LastOrDefault(div => div.GetAttributeValue("class", "").Contains("modMini")).ParentNode;
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
            var cache = new List<string>();
            var baseDoc = GetWebData<HtmlDocument>(video.VideoUrl);
            var mediaDiv = baseDoc.DocumentNode.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("data-ctrl-player", "") != "");
            if (mediaDiv != null)
            {
                var configUrl = new Uri(new Uri(video.VideoUrl), JObject.Parse(HttpUtility.HtmlDecode(mediaDiv.GetAttributeValue("data-ctrl-player", ""))).Value<string>("mcUrl")).AbsoluteUri;
                var mediaJson = GetWebData<JsonResponse>(configUrl);
                var playbackOptionsByUrl = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var media in mediaJson.MediaArray.SelectMany(m => m.MediaStreamArray).Select(streamArray => new
                    {
                        Quality = int.TryParse(streamArray.Quality, out int qualityNumber) ? ((VideoQuality)qualityNumber).ToString() : "HD",
                        Url = streamArray.Stream is JArray urlArray ? urlArray.Values<string>().OrderByDescending(item => item, StringComparer.OrdinalIgnoreCase).First() : streamArray.Stream as string,
                        Server = streamArray.Server
                    }).Distinct())
                {
                    string url = media.Url;
                    cache.Add(url);
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
                            foreach (Match match in Regex.Matches(m3u8Data, m3u8Regex2))
                            {
                                playbackOptionsByUrl[match.Groups["url"].Value] =
                                    string.Format("HLS - {0} - {1} kbps", match.Groups["resolution"].Value, int.Parse(match.Groups["bitrate"].Value) / 1000);
                                cache.Add(match.Groups["url"].Value);
                            }

                            foreach (Match match in Regex.Matches(m3u8Data, m3u8Regex1))
                            {
                                playbackOptionsByUrl[match.Groups["url"].Value] =
                                    string.Format("HLS - {0} - {1} kbps", match.Groups["codecs"].Value, int.Parse(match.Groups["bitrate"].Value) / 1000);
                                cache.Add(match.Groups["url"].Value);
                            }
                        }
                        else if (url.EndsWith("f4m"))
                        {
                            url += "?g=" + Helpers.StringUtils.GetRandomLetters(12) + "&hdcore=3.8.0";
                            playbackOptionsByUrl[url] = media.Quality;
                        }
                        else
                        {
                            playbackOptionsByUrl[url] = media.Quality;
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

                        playbackOptionsByUrl[url] = media.Quality;
                    }
                }

                video.PlaybackOptions = new Dictionary<string, string>();
                foreach (var lookup in playbackOptionsByUrl.ToLookup(kvp => kvp.Value))
                {
                    var i = 0;
                    foreach(var optionByUrl in lookup)
                    {
                        video.PlaybackOptions.Add(string.Format("{0} - {1}", optionByUrl.Value, i++), optionByUrl.Key);
                    }
                }

            }

            string qualitytoMatch = videoQuality.ToString();
            string firstUrl = video.PlaybackOptions.FirstOrDefault(p => p.Key.Contains(qualitytoMatch)).Value;
            return !string.IsNullOrEmpty(firstUrl) ? firstUrl : video.PlaybackOptions.Select(kvp => kvp.Value).LastOrDefault();
        }

        string GetStreamUrlFromSmil(string smilUrl)
        {
            var doc = GetWebData<System.Xml.Linq.XDocument>(smilUrl);
            return doc.Descendants("meta").FirstOrDefault().Attribute("base").Value + doc.Descendants("video").FirstOrDefault().Attribute("src").Value;
        }

    }
}


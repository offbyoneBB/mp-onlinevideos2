using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Xml;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class TV4Play : SiteUtilBase
    {
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Download Subtitles"), Description("Chose if you want to download available subtitles or not.")]
        protected bool retrieveSubtitles = true;

        [Category("OnlineVideosConfiguration"), Description("Url used for prepending relative links.")]
        protected string baseUrl;

        protected string playbackMetadataUrl = "http://prima.tv4play.se/api/web/asset/{0}/play";
        protected string playbackSwfUrl = "http://www.tv4play.se/flash/tv4play_sa.swf";

        protected string videosUrl = "http://www.tv4play.se/videos/search?node_nids={0}&page=1&per_page=999&sort_order=desc";
        protected string nextPageUrl = "";

        public override int DiscoverDynamicCategories()
        {
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(GetWebData(baseUrl));
            var ul = htmlDoc.DocumentNode.SelectSingleNode("//ul[@id = 'filter-categories']");
            Settings.Categories.Clear();
            foreach (var li in ul.Elements("li"))
            {
                var a = li.Element("a");
                RssLink category = new RssLink()
                {
                    Name = HttpUtility.HtmlDecode(a.InnerText),
                    Url = GetUrlForCategory(a.GetAttributeValue("href", "")),
                    SubCategories = new List<Category>(),
                    HasSubCategories = true
                };
                Settings.Categories.Add(category);
            }
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        string GetUrlForCategory(string category)
        {
            category = HttpUtility.HtmlDecode(category);
            Uri uri = null;
            if (!Uri.IsWellFormedUriString(category, UriKind.Absolute))
            {
                // workaround for .net bug when combining uri with a query only
                if (category.StartsWith("?"))
                {
                    uri = new UriBuilder(baseUrl) { Query = category.Substring(1) }.Uri;
                }
                else
                {
                    if (!Uri.TryCreate(new Uri(baseUrl), category, out uri))
                    {
                        return string.Empty;
                    }
                }
            }
            // use : &is_geo_restricted=false to hide geoblocked

            return baseUrl + "/search" + (string.IsNullOrEmpty(uri.Query) ? "?" : uri.Query + "&") + "per_page=999&page=1&is_premium=false&content-type=a-o";
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string data = GetWebData((parentCategory as RssLink).Url);
            parentCategory.SubCategories = new List<Category>();
            if (!string.IsNullOrEmpty(data))
            {
                var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(data);
                var ul = htmlDoc.DocumentNode.SelectSingleNode("//ul[@class = 'row js-show-more-content']");
                if (ul != null)
                {
                    foreach (var li in ul.Elements("li"))
                    {
                        var url = li.Descendants("a").Select(i => i.GetAttributeValue("href", "")).FirstOrDefault();
                        var idIndex = url.LastIndexOf("/");
                        if (idIndex < url.Length)
                        {
                            RssLink cat = new RssLink();
                            cat.Url = HttpUtility.HtmlDecode(string.Format(videosUrl, url.Substring(idIndex + 1)));
                            if (!Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(baseUrl), cat.Url).AbsoluteUri;
                            cat.Thumb = li.Descendants("img").Select(i => i.GetAttributeValue("src", "")).FirstOrDefault();
                            cat.Name = li.Descendants("img").Select(i => HttpUtility.HtmlDecode(i.GetAttributeValue("alt", ""))).FirstOrDefault();
                            var span = li.Descendants("span").First(s => s.GetAttributeValue("class", "") == "more");
                            if (span != null)
                            {
                                cat.Description = HttpUtility.HtmlDecode(span.InnerText);
                            }
                            parentCategory.SubCategories.Add(cat);
                            cat.ParentCategory = parentCategory;
                        }
                    }
                }
                (parentCategory as RssLink).EstimatedVideoCount = (uint)parentCategory.SubCategories.Count;
            }
            parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return getVideoList(new RssLink() { Url = nextPageUrl });
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(GetWebData(((RssLink)category).Url));
            var ul = htmlDoc.DocumentNode.SelectSingleNode("//ul[@class = 'row js-show-more-content']");
            foreach (var li in ul.Elements("li").Where(lid => lid.GetAttributeValue("class", "").Contains("free-episode")))
            {
                VideoInfo video = new VideoInfo();
                video.Title = HttpUtility.HtmlDecode(li.Element("div").Element("h3").Element("a").InnerText);
                video.Airdate = HttpUtility.HtmlDecode(li.Element("div").Element("div").Element("p").InnerText);
                video.ImageUrl = HttpUtility.ParseQueryString(new Uri(HttpUtility.HtmlDecode(HttpUtility.HtmlDecode(li.Element("p").Element("a").Element("img").GetAttributeValue("src", "")))).Query)["source"];
                video.VideoUrl = HttpUtility.HtmlDecode(li.Element("div").Element("h3").Element("a").GetAttributeValue("href", ""));
                if (!Uri.IsWellFormedUriString(video.VideoUrl, System.UriKind.Absolute)) video.VideoUrl = new Uri(new Uri(baseUrl), video.VideoUrl).AbsoluteUri;
                video.Description = HttpUtility.HtmlDecode(li.Element("div").Elements("p").First(p => p.GetAttributeValue("class", "") == "video-description").InnerText);
                videos.Add(video);
            }
            HasNextPage = false;
            nextPageUrl = "";
            return videos;
        }

        public override string getUrl(VideoInfo video)
        {
            string result = string.Empty;
            video.PlaybackOptions = new Dictionary<string, string>();
            XmlDocument xDoc = GetWebData<XmlDocument>(string.Format(playbackMetadataUrl, HttpUtility.ParseQueryString(new Uri(video.VideoUrl).Query)["video_id"]));
            var errorElements = xDoc.SelectNodes("//meta[@name = 'error']");
            if (errorElements != null && errorElements.Count > 0)
            {
                throw new OnlineVideosException(((XmlElement)errorElements[0]).GetAttribute("content"));
            }
            else
            {
                List<KeyValuePair<int, string>> urls = new List<KeyValuePair<int, string>>();
                string mediaformat;
                string urlbase;
                foreach (XmlElement videoElem in xDoc.SelectNodes("//items/item"))
                {
                    mediaformat = videoElem.GetElementsByTagName("mediaFormat")[0].InnerText.ToLower();
                    if (mediaformat.StartsWith("mp4"))
                    {
                        urlbase = videoElem.GetElementsByTagName("base")[0].InnerText.ToLower().Trim();
                        if (urlbase.StartsWith("rtmp"))
                        {
                            urls.Add(new KeyValuePair<int, string>(
                                int.Parse(videoElem.GetElementsByTagName("bitrate")[0].InnerText),
                                new MPUrlSourceFilter.RtmpUrl(videoElem.GetElementsByTagName("base")[0].InnerText)
                                {
                                    PlayPath = videoElem.GetElementsByTagName("url")[0].InnerText.Replace(".mp4", ""),
                                    SwfUrl = playbackSwfUrl,
                                    SwfVerify = true
                                }.ToString()));
                        }
                        else if (urlbase.EndsWith(".f4m"))
                        {
                            urls.Add(new KeyValuePair<int, string>(
                                int.Parse(videoElem.GetElementsByTagName("bitrate")[0].InnerText),
                                videoElem.GetElementsByTagName("url")[0].InnerText + "?hdcore=2.11.3&g=" + OnlineVideos.Sites.Utils.HelperUtils.GetRandomChars(12)));
                        }

                    } /* Can not play wvm format, drm'ed (widevine?) ? */
                    else if (mediaformat.StartsWith("wvm"))
                    {
                        urls.Add(new KeyValuePair<int, string>(
                            int.Parse(videoElem.GetElementsByTagName("bitrate")[0].InnerText),
                            videoElem.GetElementsByTagName("url")[0].InnerText));
                    }
                    else if (retrieveSubtitles && mediaformat.StartsWith("smi"))
                    {
                        video.SubtitleText = GetWebData(videoElem.GetElementsByTagName("url")[0].InnerText, null, null, null, false, false, null, System.Text.Encoding.Default);
                    }
                }
                foreach (var item in urls.OrderBy(u => u.Key))
                {
                    video.PlaybackOptions.Add(string.Format("{0} kbps", item.Key), item.Value);
                    result = item.Value;
                }
                return result;
            }
        }
    }
}

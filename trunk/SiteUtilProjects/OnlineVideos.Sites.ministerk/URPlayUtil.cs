using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Xml;
using HtmlAgilityPack;

namespace OnlineVideos.Sites
{
    public class URPlayUtil : LatestVideosSiteUtilBase
    {
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Download Subtitles"), Description("Chose if you want to download available subtitles or not.")]
        protected bool retrieveSubtitles = true;

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Subtitle Language"), Description("Chose the language of the subtitle to download, if no match the first subtitle will be downloaded")]
        protected string subtitlesLanguage = "Svenska";

        protected string baseUrl = "http://urplay.se/";
        protected string senasteUrl = "Senaste?product_type=programtv";
        protected string swfUrl = "http://urplay.se/design/ur/javascript/jwplayer-5.10.swf";

        protected string[] ignoreCategories = { "Aktuellt" , "Start", "Tablå" };
        protected string[] noSubCategories = { "Senaste", "Mest spelade", "Mest delade", "Sista chansen" };

        protected int currentVideoIndex;
        protected List<string> videoPages;

        public override int DiscoverDynamicCategories()
        {
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(GetWebData(baseUrl));
            var div = htmlDoc.DocumentNode.SelectSingleNode("//div[@id = 'navigering-content']");
            var a_s = div.Elements("ul").SelectMany(u => u.Descendants("a")).ToList();
            Settings.Categories.Clear();
            foreach (var a in a_s.Where(elt => !ignoreCategories.Contains(elt.InnerText.Trim())))
            {
                var span = a.Element("span");
                if (span != null)
                {
                    a.RemoveChild(span);
                }
                var name = HttpUtility.HtmlDecode((a.InnerText)).Trim();
                RssLink category = new RssLink()
                {
                    HasSubCategories = !noSubCategories.Contains(name),
                    SubCategories = new List<Category>(),
                    Url = HttpUtility.HtmlDecode(a.GetAttributeValue("href", "")) + "?product_type=programtv",
                    Name = name,
                };
                Settings.Categories.Add(category);
            }
            Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > 0;
            return Settings.Categories.Count;
        }


        public override int DiscoverSubCategories(Category parentCategory)
        {
            string data = GetWebData(baseUrl + (parentCategory as RssLink).Url);
            parentCategory.SubCategories = new List<Category>();
            if (!string.IsNullOrEmpty(data))
            {
                var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(data);
                if (parentCategory.Name == "A-Ö")
                {
                    var a_s = htmlDoc.DocumentNode.SelectSingleNode("//section[@id = 'alphabet']").Descendants("a");
                    foreach (var a in a_s.Where(a => (a.Descendants("span").Where(s => s.GetAttributeValue("class", "") == "product-type tv")).Any()))
                    {
                        var span = a.Element("span");
                        a.RemoveChild(span);
                        RssLink category = new RssLink()
                        {
                            Name = HttpUtility.HtmlDecode(a.InnerHtml),
                            Url = HttpUtility.HtmlDecode(a.GetAttributeValue("href", "")) + "?product_type=programtv",
                            HasSubCategories = false,
                            ParentCategory = parentCategory
                        };
                        parentCategory.SubCategories.Add(category);
                    }
                }
                else
                {
                    var a_s = htmlDoc.DocumentNode.SelectSingleNode("//nav[@class = 'sort-options']").Descendants("a");
                    foreach (var a in a_s)
                    {
                        var span = a.Element("span");
                        uint estimatedVideoCount = 0;
                        if (span != null)
                        {
                            a.RemoveChild(span);
                            uint.TryParse(span.InnerText, out estimatedVideoCount);
                        }
                        RssLink category = new RssLink()
                        {
                            HasSubCategories = false,
                            Url = HttpUtility.HtmlDecode(a.GetAttributeValue("href", "")),
                            Name = HttpUtility.HtmlDecode((a.InnerText ?? "")).Trim()
                        };
                        category.ParentCategory = parentCategory;
                        if (span == null)
                        {
                            parentCategory.SubCategories.Add(category);
                        }
                        else if (span != null && estimatedVideoCount > 0)
                        {
                            category.EstimatedVideoCount = estimatedVideoCount;
                            parentCategory.SubCategories.Add(category);
                        }
                    }
                    if (parentCategory.Other == null)
                    {
                        a_s = htmlDoc.DocumentNode.SelectSingleNode("//ul[@id = 'underkategori']").Descendants("a");
                        foreach (var a in a_s.Where(elt => !(elt.InnerText ?? "").Trim().StartsWith("Alla ämnen")))
                        {
                            RssLink category = new RssLink()
                            {
                                HasSubCategories = true,
                                SubCategories = new List<Category>(),
                                Url = HttpUtility.HtmlDecode(a.GetAttributeValue("href", "")),
                                Name = HttpUtility.HtmlDecode((a.InnerText ?? "")).Trim(),
                                Other = "Ämne",
                            };
                            category.ParentCategory = parentCategory;
                            parentCategory.SubCategories.Add(category);
                        }
                    }
                }
            }
            parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            HasNextPage = false;
            currentVideoIndex = 0;
            videoPages = new List<string>();
            List<VideoInfo> videos = new List<VideoInfo>();
            string url = (category as RssLink).Url;
            string data = GetWebData(baseUrl +  url);
            if (data.Length > 0)
            {
                var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(data);
                var pagination = htmlDoc.DocumentNode.SelectSingleNode("//nav[@class = 'pagination']");
                if (pagination != null)
                {
                    var a_s = pagination.Descendants("a").Where(a => !a.InnerText.Contains(HttpUtility.HtmlEncode(">")));
                    if (a_s != null && a_s.Count() > 0)
                    {
                        foreach (var a in a_s)
                        {
                            videoPages.Add(HttpUtility.HtmlDecode(a.GetAttributeValue("href", "")));
                        }
                        HasNextPage = videoPages.Count() > 1;
                    }
                    
                }
                videos = VideosForCategory(htmlDoc.DocumentNode);
            }
            return videos;
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            currentVideoIndex++;
            List<VideoInfo> videos = new List<VideoInfo>();
            var countPages = videoPages.Count();
            if (countPages > currentVideoIndex)
            {
                string data = GetWebData(baseUrl + videoPages[currentVideoIndex]);
                if (data.Length > 0)
                {
                    var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                    htmlDoc.LoadHtml(data);
                    videos = VideosForCategory(htmlDoc.DocumentNode);
                }
            }
            HasNextPage = countPages > currentVideoIndex + 1;
            return videos;
        }

        private List<VideoInfo> VideosForCategory(HtmlAgilityPack.HtmlNode node)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();
            var sections = node.Descendants("section").Where(s => s.GetAttributeValue("class", "") == "tv");
            if (sections != null)
            {
                foreach (var section in sections)
                {
                    var a = section.SelectSingleNode("a");
                    if (a != null)
                    {
                        VideoInfo video = new VideoInfo();
                        video.VideoUrl = baseUrl + a.GetAttributeValue("href", "");
                        var img = a.Descendants("img");
                        if (img != null && img.First() != null)
                        {
                            video.ImageUrl = img.First().GetAttributeValue("src", "");
                            video.Title = img.First().GetAttributeValue("alt", "");
                        }
                        var dd = a.Descendants("dd");
                        if (dd != null && dd.FirstOrDefault() != null)
                            video.Length = dd.FirstOrDefault().InnerText;
                        var h1 = a.Descendants("h1");
                        videoList.Add(video);
                        var descP = section.Descendants("p").Where(p => p.GetAttributeValue("class", "") == "ellipsis-lastline");
                        if (descP != null && descP.FirstOrDefault() != null)
                            video.Description = descP.FirstOrDefault().InnerText;
                    }
                }
            }
            return videoList;
        }

        public override string getUrl(VideoInfo video)
        {
            var data = GetWebData(video.VideoUrl);
            var url = string.Empty;
            Regex rgx = new Regex(@"^urPlayer.init\((.*)\);", RegexOptions.Multiline);
            Match m = rgx.Match(data);
            if (m != null)
            {
                var json = JObject.Parse(m.Groups[1].Value);
                /*
                var format = "http://{0}/{1}/{2}";
                var file = (string)json["file_html5"];
                var domain = (string)json["streaming_config"]["streamer"]["redirect"];
                var manifest = (string)json["streaming_config"]["http_streaming"]["hds_file"];
                url = string.Format(format, domain, file, manifest);
                */
                var format = "rtmp://{0}/{1}/{2}";
                var domain = (string)json["streaming_config"]["streamer"]["redirect"];
                var file = (string)json["file_flash"];
                var app = (string)json["streaming_config"]["rtmp"]["application"];
                url = string.Format(format, domain, app, file);
                url = new MPUrlSourceFilter.RtmpUrl(url)
                {
                    SwfVerify = true,
                    SwfUrl = swfUrl
                }.ToString();
                var subtitle = (string)json["subtitles"];
                var lable = (string)json["subtitle_labels"];
                if (!string.IsNullOrEmpty(subtitle) && !string.IsNullOrEmpty(subtitle))
                {
                    string[] subtitles = subtitle.Split(',');
                    string[] lables = lable.Split(',');
                    Dictionary<string, string> subDictionary = new Dictionary<string, string>();
                    int noOfSubs = subtitles.Count();
                    for (int i = 0; i < lables.Count() && i < noOfSubs; i++)
                    {
                        subDictionary.Add(lables[i], subtitles[i]);
                    }
                    var subUrl = "";
                    if (subDictionary.TryGetValue(subtitlesLanguage, out subUrl))
                    {
                        video.SubtitleText = GetSubtitle(subUrl);
                    }
                    else if (subtitles.Count() > 0)
                    {
                        video.SubtitleText = GetSubtitle(subtitles[0]);
                    }
                }
            }
            return url;
        }

        string GetSubtitle(string url)
        {
            XmlDocument xDoc = GetWebData<XmlDocument>(url);
            string srt = string.Empty;
            var errorElements = xDoc.SelectNodes("//meta[@name = 'error']");
            if (errorElements == null || errorElements.Count <= 0)
            {
                string srtFormat = "{0}\r\n{1}0 --> {2}0\r\n{3}\r\n\r\n";
                string begin;
                string end;
                string text;
                string textPart;
                int line = 1;
                foreach (XmlElement p in xDoc.GetElementsByTagName("p"))
                {
                    text = string.Empty;
                    begin = p.GetAttribute("begin");
                    end = p.GetAttribute("end");
                    XmlNodeList textNodes = p.SelectNodes(".//text()");
                    foreach (XmlNode textNode in textNodes)
                    {
                        textPart = textNode.InnerText;
                        textPart.Trim();
                        text += string.IsNullOrEmpty(textPart) ? "" : textPart + "\r\n";
                    }
                    srt += string.Format(srtFormat, line++, begin, end, text);
                }
            }
            return srt;
        }
        #region search
        public override bool CanSearch
        {
            get
            {
                return true;
            }
        }

        public override List<ISearchResultItem> DoSearch(string query)
        {
            RssLink cat = new RssLink()
            {
                Name = "Sök",
                Url = "/Produkter?product_type=programtv&q=" + HttpUtility.UrlEncode(query)
            };
            List<ISearchResultItem> results = new List<ISearchResultItem>();
            foreach (VideoInfo vi in getVideoList(cat))
                results.Add(vi);
            return results;
        }
        #endregion

        #region LatestVideos

        public override List<VideoInfo> GetLatestVideos()
        {
            RssLink latest = new RssLink() { Name = "Latest videos", Url = senasteUrl };
            List<VideoInfo> videos = getVideoList(latest);
            return videos.Count >= LatestVideosCount ? videos.GetRange(0, (int)LatestVideosCount): new List<VideoInfo>();
        }

        #endregion
    }
}
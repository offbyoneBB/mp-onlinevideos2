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
        public enum JaNej { Ja, Nej };
        public enum Subtitle { Svenska, Förvald_URPlay, Endast_manuellt_val, Ingen_textning };

        protected const string _amnesord = "Ämnesord";
        protected const string _textningssprak = "Textningsspråk";
        protected const string _ingenTextning = "Ingen textning";

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Textning"), Description("Svenska; Förvalt av URPlay; Manuellt val från kontextmenyn (F9 eller info-knapp) för filmen; Ingen textning.")]
        protected Subtitle subtitleChoice = Subtitle.Svenska;
        protected bool AutomaticallyRetrieveSubtitles { get { return subtitleChoice == Subtitle.Svenska || subtitleChoice == Subtitle.Förvald_URPlay; } }
        protected bool ManuallyRetrieveSubtitles { get { return subtitleChoice == Subtitle.Endast_manuellt_val; } }

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Föredra alltid HD-kvalité"), Description("Välj om du automatiskt vill se filmerna i HD-kvalité om det finns tillgängligt, annars får du manuellt välja.")]
        protected JaNej preferHD = JaNej.Nej;
        protected bool PreferHD { get { return preferHD == JaNej.Ja; } }

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Aktivera ämnesordsök"), Description("Välj om du vill kunna göra ämnesordsök från kontextmenyn (F9 eller info-knapp) för varje enskild film/video.")]
        protected JaNej enableAOSearch = JaNej.Ja;
        protected bool EnableAOSearch { get { return enableAOSearch == JaNej.Ja; } }

        protected string baseUrl = "http://urplay.se/";
        protected string senasteUrl = "Senaste?product_type=programtv";

        protected string[] ignoreCategories = { "Aktuellt", "Start", "Tablå" };
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

        public override List<VideoInfo> GetVideos(Category category)
        {
            HasNextPage = false;
            currentVideoIndex = 0;
            videoPages = new List<string>();
            List<VideoInfo> videos = new List<VideoInfo>();
            string url = (category as RssLink).Url;
            string data = GetWebData(baseUrl + url);
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

        public override List<VideoInfo> GetNextPageVideos()
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
                        var descP = section.Descendants("p").Where(p => p.GetAttributeValue("class", "") == "ellipsis-lastline");
                        if (descP != null && descP.FirstOrDefault() != null)
                            video.Description = descP.FirstOrDefault().InnerText;
                        if (EnableAOSearch)
                        {
                            SerializableDictionary<string, string> other = new SerializableDictionary<string, string>();
                            var ul = section.SelectSingleNode("div[@class = 'details']/ul[@class = 'keywords']");
                            if (ul != null)
                            {
                                IEnumerable<HtmlNode> keyAs = ul.Descendants("a");
                                foreach (HtmlNode keyA in keyAs)
                                {
                                    other.Add(keyA.GetAttributeValue("data-keyword", ""), keyA.GetAttributeValue("href", ""));
                                }
                            }
                            video.Other = other;
                        }
                        videoList.Add(video);
                    }
                }
            }
            return videoList;
        }

        public override List<string> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            var data = GetWebData(video.VideoUrl);
            var urlSD = string.Empty;
            var urlHD = string.Empty;
            Regex rgx = new Regex(@"^urPlayer.init\((.*)\);", RegexOptions.Multiline);
            Match m = rgx.Match(data);
            video.PlaybackOptions = new Dictionary<string, string>();
            if (m != null)
            {
                var json = JObject.Parse(m.Groups[1].Value);
                var format = "http://{0}/ondemand/_definst_/mp4:{1}/{2}";
                var fileHD = (string)json["file_hd"];
                var fileSD = (string)json["file_flash"];
                var domain = (string)json["streaming_config"]["streamer"]["redirect"];
                var manifest = (string)json["streaming_config"]["http_streaming"]["hds_file"];
                if (!string.IsNullOrEmpty(fileHD))
                {
                    urlHD = string.Format(format, domain, fileHD, manifest);
                    video.PlaybackOptions.Add("HD", urlHD);
                }
                if (!string.IsNullOrEmpty(fileSD))
                {
                    urlSD = string.Format(format, domain, fileSD, manifest);
                    video.PlaybackOptions.Add("SD", urlSD);
                }

                if (AutomaticallyRetrieveSubtitles)
                {
                    video.SubtitleText = GetSubtitle(json, subtitleChoice == Subtitle.Förvald_URPlay ? (string)json["subtitle_default"] : subtitleChoice.ToString());
                }
            }
            if (inPlaylist || PreferHD)
                video.PlaybackOptions.Clear();

            return new List<string>() { string.IsNullOrEmpty(urlHD) ? urlSD : urlHD };
        }

        private string GetSubtitle(JObject json, string language)
        {
            var subtitle = (string)json["subtitles"];
            var label = (string)json["subtitle_labels"];
            if (!string.IsNullOrEmpty(subtitle) && !string.IsNullOrEmpty(label))
            {
                string[] subtitles = subtitle.Split(',');
                string[] lables = label.Split(',');
                Dictionary<string, string> subDictionary = new Dictionary<string, string>();
                for (int i = 0; i < lables.Count() && i < subtitles.Count(); i++)
                {
                    subDictionary.Add(lables[i], subtitles[i]);
                }
                var url = "";
                if (subDictionary.TryGetValue(language, out url))
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
            }
            return "";
        }

        #region Context menu
        public override List<ContextMenuEntry> GetContextMenuEntries(Category selectedCategory, VideoInfo selectedItem)
        {
            List<ContextMenuEntry> entries = new List<ContextMenuEntry>();
            if (selectedItem != null && EnableAOSearch && selectedItem.Other is SerializableDictionary<string, string>)
            {
                var other = selectedItem.Other as SerializableDictionary<string, string>;

                ContextMenuEntry amnesord = new ContextMenuEntry();
                amnesord.Action = ContextMenuEntry.UIAction.ShowList;
                amnesord.DisplayText = _amnesord;
                foreach (var ao in other)
                {
                    ContextMenuEntry entry = new ContextMenuEntry();
                    entry.DisplayText = ao.Key;
                    entry.Other = ao.Value;
                    amnesord.SubEntries.Add(entry);
                }
                if (amnesord.SubEntries.Count > 0)
                    entries.Add(amnesord);
            }
            if (selectedItem != null && ManuallyRetrieveSubtitles)
            {
                string data = GetWebData(selectedItem.VideoUrl);
                Regex rgx = new Regex(@"^urPlayer.init\((.*)\);", RegexOptions.Multiline);
                Match m = rgx.Match(data);
                if (m != null)
                {
                    var json = JObject.Parse(m.Groups[1].Value);
                    var label = (string)json["subtitle_labels"];
                    string[] labels = label.Split(',');
                    if (labels.Count() > 0)
                    {
                        ContextMenuEntry textningssprak = new ContextMenuEntry();
                        textningssprak.Action = ContextMenuEntry.UIAction.ShowList;
                        textningssprak.DisplayText = _textningssprak;
                        textningssprak.Other = json.ToString();
                        ContextMenuEntry entry = new ContextMenuEntry();
                        entry.DisplayText = string.Format(_ingenTextning);
                        textningssprak.SubEntries.Add(entry);
                        for (int i = 0; i < labels.Count(); i++)
                        {
                            entry = new ContextMenuEntry();
                            entry.DisplayText = string.Format(labels[i]);
                            textningssprak.SubEntries.Add(entry);
                        }
                        entries.Add(textningssprak);
                    }
                }
            }
            return entries;
        }

        public override ContextMenuExecutionResult ExecuteContextMenuEntry(Category selectedCategory, VideoInfo selectedItem, ContextMenuEntry choice)
        {
            if (selectedItem != null && ManuallyRetrieveSubtitles && choice.ParentEntry != null && choice.ParentEntry.DisplayText == _textningssprak && choice.ParentEntry.Other is string)
            {
                ContextMenuExecutionResult result = new ContextMenuExecutionResult();
                if (choice.DisplayText == _ingenTextning)
                    selectedItem.SubtitleText = "";
                else
                    selectedItem.SubtitleText = GetSubtitle(JObject.Parse(choice.ParentEntry.Other as string), choice.DisplayText);
                result.ExecutionResultMessage = selectedItem.Title + " - " + (string.IsNullOrEmpty(selectedItem.SubtitleText) && choice.DisplayText != _ingenTextning ? "Fel vid hämtning av textning!" : _textningssprak + ": " + choice.DisplayText);
                result.RefreshCurrentItems = false;
                return result;
            }
            else if (selectedItem != null && EnableAOSearch && choice.ParentEntry != null && choice.ParentEntry.DisplayText == _amnesord && choice.Other is string)
            {
                ContextMenuExecutionResult result = new ContextMenuExecutionResult();
                RssLink cat = new RssLink()
                {
                    Name = choice.DisplayText,
                    Url = choice.Other as string
                };
                List<SearchResultItem> results = new List<SearchResultItem>();
                foreach (VideoInfo vi in GetVideos(cat))
                    results.Add(vi);
                result.ResultItems = results;
                return result;
            }
            else
                return base.ExecuteContextMenuEntry(selectedCategory, selectedItem, choice);
        }

        #endregion
        #region search
        public override bool CanSearch
        {
            get
            {
                return true;
            }
        }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            RssLink cat = new RssLink()
            {
                Name = "Sök",
                Url = "/Produkter?product_type=programtv&q=" + HttpUtility.UrlEncode(query)
            };
            List<SearchResultItem> results = new List<SearchResultItem>();
            foreach (VideoInfo vi in GetVideos(cat))
                results.Add(vi);
            return results;
        }
        #endregion

        #region LatestVideos

        public override List<VideoInfo> GetLatestVideos()
        {
            RssLink latest = new RssLink() { Name = "Latest videos", Url = senasteUrl };
            List<VideoInfo> videos = GetVideos(latest);
            return videos.Count >= LatestVideosCount ? videos.GetRange(0, (int)LatestVideosCount) : new List<VideoInfo>();
        }

        #endregion
    }
}
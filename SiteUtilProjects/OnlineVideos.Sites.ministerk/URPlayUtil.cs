using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Xml;
using HtmlAgilityPack;
using System.Web;

namespace OnlineVideos.Sites
{
    public class URPlayUtil : LatestVideosSiteUtilBase
    {
        #region Enums

        public enum JaNej { Ja, Nej };
        public enum Subtitle { Svenska, Förvald_URPlay, Endast_manuellt_val, Ingen_textning };

        #endregion

        #region Configuration

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Textning"), Description("Svenska; Förvalt av URPlay; Manuellt val från kontextmenyn (F9 eller info-knapp) för filmen; Ingen textning.")]
        protected Subtitle subtitleChoice = Subtitle.Svenska;
        protected bool AutomaticallyRetrieveSubtitles { get { return subtitleChoice == Subtitle.Svenska || subtitleChoice == Subtitle.Förvald_URPlay; } }
        protected bool ManuallyRetrieveSubtitles { get { return subtitleChoice == Subtitle.Endast_manuellt_val; } }

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Föredra alltid HD-kvalité"), Description("Välj om du automatiskt vill se filmerna i HD-kvalité om det finns tillgängligt, annars får du manuellt välja.")]
        protected JaNej preferHD = JaNej.Nej;
        protected bool PreferHD { get { return preferHD == JaNej.Ja; } }

        #endregion

        #region Consts and vars

        protected const string _textningssprak = "Textningsspråk";
        protected const string _ingenTextning = "Ingen textning";
        private const string episodeVideosState = "EPISODES_STATE";
        private string currentVideoUrl = "";
        private int currentVideoOffset = 0;

        #endregion

        #region Category dictionaries

        private Dictionary<string, string> tvCategories = new Dictionary<string, string>()
        {
            { "Alla", "" },
            { "Dokumentärfilmer", "Dokument%C3%A4rfilmer" },
            { "Föreläsningar", "F%C3%B6rel%C3%A4sningar" },
            { "Vetenskap", "Vetenskap" },
            { "Kultur och historia", "Kultur+och+historia" },
            { "Samhälle", "Samh%C3%A4lle" },
            { "Reality och livsstil", "Reality+och+livsstil" },
            { "Barn", "Barn" }
        };

        private Dictionary<string, string> programSorteringar = new Dictionary<string, string>()
        {
            { "Relevans", "default" },
            { "Mest spelade", "most_viewed" },
            { "Sista chansen", "last_chance" },
            { "Mest delat", "most_shared" },
            { "Senaste", "latest" },
            { "A - Ö", "title" }
        };

        #endregion

        #region Categories

        public override int DiscoverDynamicCategories()
        {
            Category series = new Category()
            {
                Name = "Serier",
                SubCategories = new List<Category>(),
                SubCategoriesDiscovered = true,
                HasSubCategories = true,
            };
            Category programs = new Category()
            {
                Name = "Program",
                SubCategories = new List<Category>(),
                SubCategoriesDiscovered = true,
                HasSubCategories = true,
            };
            RssLink spelistor = new RssLink()
            {
                Name = "Spellistor",
                Url = "http://urplay.se/sok?age=&product_type=playlists&query=&type=programtv",
                HasSubCategories = true,
                Other = episodeVideosState
            };

            foreach(KeyValuePair<string,string> genre in tvCategories)
            {
                series.SubCategories.Add(new RssLink()
                {
                    Name = genre.Key,
                    Url = string.Format("http://urplay.se/sok?age=&product_type=series&query=&rows=1000&type=programtv&view=title&play_category={0}", genre.Value),
                    HasSubCategories = true,
                    ParentCategory = series
                });
                string programurl = "http://urplay.se/sok?age=&product_type=program&query=&rows=20&type=programtv&play_category={0}&view={1}&start=";
                RssLink program = new RssLink()
                {
                    Name = genre.Key,
                    HasSubCategories = true,
                    ParentCategory = programs,
                    SubCategories = new List<Category>(),
                    SubCategoriesDiscovered = true
                };
                foreach (KeyValuePair<string, string> programSortering in programSorteringar)
                {
                    program.SubCategories.Add(new RssLink()
                    {
                        Name = programSortering.Key,
                        Url = string.Format(programurl, genre.Value, programSortering.Value) + "{0}",
                        HasSubCategories = false,
                        ParentCategory = program
                    });
                }
                programs.SubCategories.Add(program);

            }
            Settings.Categories.Add(series);
            Settings.Categories.Add(programs);
            Settings.Categories.Add(spelistor);
            Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > 0;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string url = (parentCategory as RssLink).Url;
            HtmlDocument doc = GetWebData<HtmlDocument>(url);
            HtmlNodeCollection items = doc.DocumentNode.SelectNodes("//li[@class='series' or @class='user-collection']");
            parentCategory.SubCategories = new List<Category>();
            if (items != null)
            {
                foreach(HtmlNode item in items)
                {
                    HtmlNode cat = item.SelectSingleNode(".//p[@class='category']");
                    string desc = "";
                    if (cat != null)
                        desc = HttpUtility.HtmlDecode(cat.InnerText.Trim()) + "\r\n";
                    cat = item.SelectSingleNode(".//p[@class='description']");
                    if (cat != null)
                        desc += HttpUtility.HtmlDecode(item.SelectSingleNode(".//p[@class='description']").InnerText.Trim());
                    string thumb = item.SelectSingleNode(".//img").GetAttributeValue("data-src", "").Replace("_t.","_l.");
                    if (string.IsNullOrWhiteSpace(thumb))
                        thumb = item.SelectSingleNode(".//img").GetAttributeValue("src", "").Replace("_t.","_l.");
                    if (!string.IsNullOrWhiteSpace(thumb) && thumb.StartsWith("/"))
                        thumb = "http://urplay.se" + thumb;
                    string link = item.SelectSingleNode("a").GetAttributeValue("href", "");
                    if (!string.IsNullOrWhiteSpace(link) && link.StartsWith("/"))
                        link = "http://urplay.se" + link;
                    parentCategory.SubCategories.Add(new RssLink()
                    {
                        Name = HttpUtility.HtmlDecode(item.SelectSingleNode(".//h3").InnerText.Trim()),
                        Url = link,
                        Thumb = thumb,
                        Description = desc,
                        ParentCategory = parentCategory,
                        Other = episodeVideosState
                    });
                }
            }
            parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
            return parentCategory.SubCategories.Count;
        }

        #endregion

        #region Videos

        private List<VideoInfo> GetVideos(string url)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            HtmlDocument doc = GetWebData<HtmlDocument>(url);
            HtmlNode docNode = doc.DocumentNode;
            HtmlNodeCollection items;
            HtmlNode episodes = docNode.SelectSingleNode("//section[@id='episodes']");
            if (episodes != null)
                items = episodes.SelectNodes("div/ul/li");
            else
                items = docNode.SelectNodes("//li[@class='program']");
            
            if (items != null)
            {
                foreach (HtmlNode item in items)
                {
                    List<string> descriptions = new List<string>();
                    HtmlNode categoryNode = item.SelectSingleNode(".//p[@class='category']");
                    if (categoryNode != null && !string.IsNullOrWhiteSpace(categoryNode.InnerText))
                        descriptions.Add(HttpUtility.HtmlDecode(categoryNode.InnerText.Trim()));
                    HtmlNode descNode = item.SelectSingleNode(".//p[@class='description']");
                    if (descNode != null && !string.IsNullOrWhiteSpace(descNode.InnerText))
                        descriptions.Add(HttpUtility.HtmlDecode(descNode.InnerText.Trim()));

                    List<string> titleInfos = new List<string>();
                    HtmlNode seriesTitle = item.SelectSingleNode(".//p[@class='series-title']");
                    if (seriesTitle != null && !string.IsNullOrWhiteSpace(seriesTitle.InnerText))
                        titleInfos.Add(HttpUtility.HtmlDecode(seriesTitle.InnerText.Trim()));
                    HtmlNode episodeNum = item.SelectSingleNode(".//span[@class='episode-number']");
                    if (episodeNum != null && !string.IsNullOrWhiteSpace(episodeNum.InnerText))
                        titleInfos.Add(HttpUtility.HtmlDecode(episodeNum.InnerText.Trim()));

                    string description = descriptions.Count > 0 ? string.Join("\r\n", descriptions) : "";
                    string titleInfo = titleInfos.Count > 0 ? string.Format(" ({0})", string.Join(" ", titleInfos)) : "";

                    string thumb = item.SelectSingleNode(".//img").GetAttributeValue("data-src", "").Replace("_t.", "_l.");
                    if (string.IsNullOrWhiteSpace(thumb))
                        thumb = item.SelectSingleNode(".//img").GetAttributeValue("src", "").Replace("_t.","_l.");
                    if (!string.IsNullOrWhiteSpace(thumb) && thumb.StartsWith("/"))
                        thumb = "http://urplay.se" + thumb;

                    string videoUrl = item.SelectSingleNode("a").GetAttributeValue("href", "");
                    if (!string.IsNullOrWhiteSpace(videoUrl) && videoUrl.StartsWith("/"))
                        videoUrl = "http://urplay.se" + videoUrl;

                    videos.Add(new VideoInfo()
                    {
                        Title = HttpUtility.HtmlDecode(item.SelectSingleNode(".//h3").InnerText.Trim()) + titleInfo,
                        VideoUrl = videoUrl ,
                        Thumb = thumb,
                        Description = description,
                        Length = HttpUtility.HtmlDecode(item.SelectSingleNode(".//span[@class='duration']").InnerText.Trim())
                    });
                }
            }
            return videos;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            HasNextPage = false;
            List<VideoInfo> videos;
            if (category.Other is string && category.GetOtherAsString() == episodeVideosState)
            {
                currentVideoUrl = "";
                currentVideoOffset = 0;
                videos = GetVideos((category as RssLink).Url);
            }
            else
            {
                currentVideoUrl = (category as RssLink).Url;
                currentVideoOffset = 0;
                videos = GetVideos(string.Format(currentVideoUrl, currentVideoOffset));
                HasNextPage = videos.Count > 19;
            }
            return videos;
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            HasNextPage = false;
            currentVideoOffset += 20;
            List<VideoInfo> videos = GetVideos(string.Format(currentVideoUrl, currentVideoOffset));
            HasNextPage = videos.Count > 19;
            return videos;
        }

        public override List<string> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            string data = GetWebData(video.VideoUrl);
            string urlSD = string.Empty;
            string urlHD = string.Empty;
            Regex rgx = new Regex(@"urPlayer.init\((.*)\);", RegexOptions.Multiline);
            Match m = rgx.Match(data);
            video.PlaybackOptions = new Dictionary<string, string>();
            if (m != null)
            {
                JObject json = JObject.Parse(m.Groups[1].Value);
                string format = "http://{0}/{1}{2}";
                string fileHD = json["file_http_hd"].Value<string>();
                string fileSD = json["file_http"].Value<string>();
                string domain = GetWebData<JObject>(json["streaming_config"]["loadbalancer"].Value<string>())["redirect"].Value<string>();
                string manifest = json["streaming_config"]["http_streaming"]["hds_file"].Value<string>();
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
                    video.SubtitleText = GetSubtitle(json);
                }
            }
            if (inPlaylist || PreferHD)
                video.PlaybackOptions.Clear();

            return new List<string>() { string.IsNullOrEmpty(urlHD) ? urlSD : urlHD };
        }

        private string GetSubtitle(JObject json, string language = null)
        {
            JArray subtitles = (JArray)json["tracks"];
            JToken subtitle = null;
            string srt = string.Empty;
            if (language != null)
            {
                subtitle = subtitles.FirstOrDefault(s => s["label"] != null && s["label"].Value<string>() == language);
            }
            else if (subtitleChoice == Subtitle.Förvald_URPlay)
            {
                subtitle = subtitles.FirstOrDefault(s => s["default"] != null && s["default"].Value<bool>());
            }
            else if (subtitleChoice == Subtitle.Svenska)
            {
                subtitle = subtitles.FirstOrDefault(s => s["label"] != null && s["label"].Value<string>() == Subtitle.Svenska.ToString());
            }
            if (subtitle != null)
            {
                XmlDocument xDoc = GetWebData<XmlDocument>(subtitle["file"].Value<string>());
                XmlNodeList errorElements = xDoc.SelectNodes("//meta[@name = 'error']");
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
            }
            return srt;
        }

        #endregion

        #region Context menu
        
        public override List<ContextMenuEntry> GetContextMenuEntries(Category selectedCategory, VideoInfo selectedItem)
        {
            List<ContextMenuEntry> entries = new List<ContextMenuEntry>();
            if (selectedItem != null && ManuallyRetrieveSubtitles)
            {
                string data = GetWebData(selectedItem.VideoUrl);
                Regex rgx = new Regex(@"urPlayer.init\((.*)\);", RegexOptions.Multiline);
                Match m = rgx.Match(data);
                if (m != null)
                {
                    var json = JObject.Parse(m.Groups[1].Value);
                    JArray subtitles = (JArray)json["tracks"];
                    if (subtitles != null && subtitles.Count() > 0)
                    {
                        ContextMenuEntry textningssprak = new ContextMenuEntry();
                        textningssprak.Action = ContextMenuEntry.UIAction.ShowList;
                        textningssprak.DisplayText = _textningssprak;
                        textningssprak.Other = json.ToString();
                        ContextMenuEntry entry = new ContextMenuEntry();
                        entry.DisplayText = string.Format(_ingenTextning);
                        textningssprak.SubEntries.Add(entry);
                        foreach (JToken subtitle in subtitles)
                        {
                            entry = new ContextMenuEntry();
                            entry.DisplayText = string.Format(subtitle["label"].Value<string>());
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
            else
                return base.ExecuteContextMenuEntry(selectedCategory, selectedItem, choice);
        }

        #endregion

        #region Search

        public override bool CanSearch
        {
            get
            {
                return true;
            }
        }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            List<SearchResultItem> results = new List<SearchResultItem>();
            HasNextPage = false;
            currentVideoUrl = string.Format("http://urplay.se/sok?age=&product_type=program&query=&rows=20&type=programtv&play_category=&view=default&query={0}", HttpUtility.UrlEncode(query)) +  "&start={0}";
            currentVideoOffset = 0;
            GetVideos(string.Format(currentVideoUrl, currentVideoOffset)).ForEach(v => results.Add(v));
            HasNextPage = results.Count > 19;
            return results;
        }

        #endregion

        #region LatestVideos

        public override List<VideoInfo> GetLatestVideos()
        {
            string latest = string.Format("http://urplay.se/sok?age=&product_type=program&query=&rows={0}&type=programtv&play_category=&view=latest&start=0", LatestVideosCount);
            List<VideoInfo> videos = GetVideos(latest);
            return videos.Count >= LatestVideosCount ? videos.GetRange(0, (int)LatestVideosCount) : new List<VideoInfo>();
        }

        #endregion
    }
}
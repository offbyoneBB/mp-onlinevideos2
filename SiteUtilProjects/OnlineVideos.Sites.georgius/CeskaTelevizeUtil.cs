using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Net;
using System.IO;

namespace OnlineVideos.Sites.georgius
{
    public sealed class CeskaTelevizeUtil : SiteUtilBase
    {
        #region Private fields

        private static String baseUrl = "http://www.ceskatelevize.cz";
        private static String dynamicCategoryBaseUrl = "http://www.ceskatelevize.cz/ivysilani/podle-abecedy";

        private static String showEpisodePostStart = @"getPlaylistUrl([";
        private static String showEpisodePostEnd = @"]";

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        #endregion

        #region Constructors

        public CeskaTelevizeUtil()
            : base()
        {
        }

        #endregion

        #region Methods

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
        }

        public override int DiscoverDynamicCategories()
        {
            int dynamicCategoriesCount = 0;

            this.Settings.Categories.Add(
                new RssLink()
                {
                    Name = "Živě",
                    HasSubCategories = false,
                    Url = "live"
                });
            dynamicCategoriesCount++;

            String baseWebData = GetWebData(CeskaTelevizeUtil.dynamicCategoryBaseUrl, forceUTF8: true).Replace("\r", "").Replace("\n", "");

            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            document.LoadHtml(baseWebData);

            HtmlAgilityPack.HtmlNodeCollection categories = document.DocumentNode.SelectSingleNode(".//ul[@id='programmeGenre']").SelectNodes("li");

            foreach (var category in categories)
            {
                String categoryUrl = category.SelectSingleNode("a").Attributes["href"].Value;
                String categoryTitle = category.SelectSingleNode(".//span").InnerText;

                this.Settings.Categories.Add(
                        new RssLink()
                        {
                            Name = categoryTitle,
                            HasSubCategories = true,
                            Url = Utils.FormatAbsoluteUrl(categoryUrl, CeskaTelevizeUtil.baseUrl)
                        });

                dynamicCategoriesCount++;
            }

            this.Settings.DynamicCategoriesDiscovered = true;
            return dynamicCategoriesCount;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            int dynamicSubCategoriesCount = 0;
            RssLink category = (RssLink)parentCategory;

            String baseWebData = GetWebData(category.Url, forceUTF8: true);

            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            document.LoadHtml(baseWebData);

            HtmlAgilityPack.HtmlNodeCollection shows = document.DocumentNode.SelectSingleNode("//div[@id='programmeAlphabetContent']").SelectNodes(".//li[not(span[@class='labelBonus'])]");

            if (shows.Count > 0)
            {
                category.SubCategories = new List<Category>();

                foreach (var show in shows)
                {
                    HtmlAgilityPack.HtmlNode link = show.SelectSingleNode("a");

                    String showUrl = link.Attributes["href"].Value;
                    String showTitle = link.InnerText;
                    String showDescription = link.Attributes.Contains("title") ? link.Attributes["title"].Value : String.Empty;

                    category.SubCategoriesDiscovered = true;
                    category.SubCategories.Add(
                        new RssLink()
                        {
                            Name = showTitle,
                            HasSubCategories = false,
                            Url = Utils.FormatAbsoluteUrl(showUrl, CeskaTelevizeUtil.baseUrl),
                            Description = OnlineVideos.Helpers.StringUtils.PlainTextFromHtml(showDescription)
                        });

                    dynamicSubCategoriesCount++;
                }
            }

            return dynamicSubCategoriesCount;
        }

        private List<VideoInfo> GetPageVideos(String pageUrl, Boolean first)
        {
            // in first run we must check site number, if we are really on first site

            List<VideoInfo> pageVideos = new List<VideoInfo>();

            if (!String.IsNullOrEmpty(pageUrl))
            {
                this.nextPageUrl = String.Empty;

                if (this.currentCategory.Name == "Živě")
                {
                    System.Collections.Specialized.NameValueCollection headers = new System.Collections.Specialized.NameValueCollection();

                    headers.Add("Accept", "*/*"); // accept any content type
                    headers.Add("User-Agent", OnlineVideoSettings.Instance.UserAgent); // set the default OnlineVideos UserAgent when none specified
                    headers.Add("X-Requested-With", "XMLHttpRequest");
                    headers.Add("x-addr", "127.0.0.1");

                    String baseWebData = GetWebData("http://www.ceskatelevize.cz/ivysilani/ajax/live-box", null, null, "http://www.ceskatelevize.cz/ivysilani/podle-abecedy", null, true, false, null, null, headers, false);

                    HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
                    document.LoadHtml(baseWebData);

                    HtmlAgilityPack.HtmlNodeCollection liveChannels = document.DocumentNode.SelectNodes(".//div[contains(@class, 'channel')]");

                    foreach (var liveChannel in liveChannels)
                    {
                        HtmlAgilityPack.HtmlNode titleLink = liveChannel.SelectSingleNode("./p[@class='title']/a");
                        HtmlAgilityPack.HtmlNode thumbLink = liveChannel.SelectSingleNode(".//img");

                        String showUrl = (titleLink != null) ? Utils.FormatAbsoluteUrl(titleLink.Attributes["href"].Value, CeskaTelevizeUtil.baseUrl) : String.Empty;
                        String showThumbUrl = ((thumbLink != null) && thumbLink.Attributes.Contains("src")) ? thumbLink.Attributes["src"].Value : String.Empty;
                        String showTitle = (titleLink != null) ? titleLink.InnerText.Trim() : String.Empty;

                        if (String.IsNullOrEmpty(showTitle) || String.IsNullOrEmpty(showUrl))
                        {
                            continue;
                        }

                        VideoInfo videoInfo = new VideoInfo()
                        {
                            Thumb = showThumbUrl,
                            Title = showTitle,
                            VideoUrl = showUrl
                        };

                        pageVideos.Add(videoInfo);
                    }
                }
                else
                {
                    String baseWebData = GetWebData(pageUrl, forceUTF8: true);

                    HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
                    document.LoadHtml(baseWebData);

                    if (first)
                    {
                        HtmlAgilityPack.HtmlNode firstPageNode = document.DocumentNode.SelectSingleNode(".//a[@class='first' and @data-page='1']");
                        String firstPageUrl = Utils.FormatAbsoluteUrl(firstPageNode.Attributes["href"].Value, CeskaTelevizeUtil.baseUrl);

                        // reload first page and continue in parsing
                        baseWebData = GetWebData(firstPageUrl, forceUTF8: true);
                        document.LoadHtml(baseWebData);
                    }

                    HtmlAgilityPack.HtmlNodeCollection episodes = document.DocumentNode.SelectNodes(".//ul[@class='clearfix content']/li");

                    foreach (var episode in episodes)
                    {
                        HtmlAgilityPack.HtmlNode link = episode.SelectSingleNode("a[@class='itemImage']");

                        if (link != null)
                        {
                            HtmlAgilityPack.HtmlNode thumbLink = link.SelectSingleNode("img");
                            HtmlAgilityPack.HtmlNode title = episode.SelectSingleNode(".//h3");
                            HtmlAgilityPack.HtmlNodeCollection descriptions = episode.SelectNodes(".//p[not(*)]");

                            String showUrl = Utils.FormatAbsoluteUrl(link.Attributes["href"].Value, CeskaTelevizeUtil.baseUrl);
                            String showThumbUrl = ((thumbLink != null) && thumbLink.Attributes.Contains("src")) ? thumbLink.Attributes["src"].Value : String.Empty;
                            String showTitle = (title != null) ? title.InnerText.Trim() : String.Empty;

                            StringBuilder showDescription = new StringBuilder();

                            if (descriptions != null)
                            {
                                foreach (var descriptionItem in episode.SelectNodes(".//p[not(*)]"))
                                {
                                    showDescription.AppendLine(OnlineVideos.Helpers.StringUtils.PlainTextFromHtml(descriptionItem.InnerText.Replace('\t', ' ').Trim()));
                                }
                            }

                            if (String.IsNullOrEmpty(showTitle) || String.IsNullOrEmpty(showUrl) || String.IsNullOrEmpty(showThumbUrl))
                            {
                                continue;
                            }

                            VideoInfo videoInfo = new VideoInfo()
                            {
                                Thumb = showThumbUrl,
                                Title = showTitle,
                                VideoUrl = showUrl,
                                Description = showDescription.ToString()
                            };

                            pageVideos.Add(videoInfo);
                        }
                    }
                    
                    HtmlAgilityPack.HtmlNode nextPageLink = document.DocumentNode.SelectSingleNode(".//a[@class='next']");
                    this.nextPageUrl = (nextPageLink != null) ? Utils.FormatAbsoluteUrl(nextPageLink.Attributes["href"].Value, CeskaTelevizeUtil.baseUrl) : String.Empty;
                }
            }

            return pageVideos;
        }

        private List<VideoInfo> GetVideoList(Category category)
        {
            this.hasNextPage = false;
            String baseWebData = String.Empty;
            RssLink parentCategory = (RssLink)category;
            List<VideoInfo> videoList = new List<VideoInfo>();

            if (parentCategory.Name != this.currentCategory.Name)
            {
                this.currentStartIndex = 0;
                this.nextPageUrl = parentCategory.Url;
                this.loadedEpisodes.Clear();
            }

            this.currentCategory = parentCategory;
            this.loadedEpisodes.AddRange(this.GetPageVideos(this.nextPageUrl, (this.loadedEpisodes.Count == 0)));
            while (this.currentStartIndex < this.loadedEpisodes.Count)
            {
                videoList.Add(this.loadedEpisodes[this.currentStartIndex++]);
            }

            if (!String.IsNullOrEmpty(this.nextPageUrl))
            {
                this.hasNextPage = true;
            }

            return videoList;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            this.currentStartIndex = 0;
            return this.GetVideoList(category);
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            return this.GetVideoList(this.currentCategory);
        }

        public override bool HasNextPage
        {
            get
            {
                return this.hasNextPage;
            }
            protected set
            {
                this.hasNextPage = value;
            }
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            if (video.PlaybackOptions == null)
            {
                video.PlaybackOptions = new Dictionary<string, string>();
            }
            video.PlaybackOptions.Clear();

            System.Net.CookieContainer container = new System.Net.CookieContainer();
            String baseWebData = GetWebData(video.VideoUrl, cookies: container, forceUTF8: true);

            String playlistSerializedUrl = String.Empty;
            if (this.currentCategory.Name == "Živě")
            {
                String serializedDataForPost = "playlist%5B0%5D%5Btype%5D=channel&playlist%5B0%5D%5Bid%5D=24&requestUrl=%2Fivysilani%2Fembed%2FiFramePlayerCT24.php&requestSource=iVysilani&addCommercials=1&type=flash";
                System.Collections.Specialized.NameValueCollection headers = new System.Collections.Specialized.NameValueCollection();

                headers.Add("Accept", "*/*"); // accept any content type
                headers.Add("User-Agent", OnlineVideoSettings.Instance.UserAgent); // set the default OnlineVideos UserAgent when none specified
                headers.Add("X-Requested-With", "XMLHttpRequest");
                headers.Add("x-addr", "127.0.0.1");

                playlistSerializedUrl = GetWebData("http://www.ceskatelevize.cz/ivysilani/ajax/get-client-playlist", serializedDataForPost, container, video.VideoUrl, null, false, false, null, null, headers, false);
            }
            else
            {
                int start = baseWebData.IndexOf(CeskaTelevizeUtil.showEpisodePostStart);
                if (start >= 0)
                {
                    int end = baseWebData.IndexOf(CeskaTelevizeUtil.showEpisodePostEnd, start + CeskaTelevizeUtil.showEpisodePostStart.Length);
                    if (end >= 0)
                    {
                        String postData = baseWebData.Substring(start + CeskaTelevizeUtil.showEpisodePostStart.Length, end - start - CeskaTelevizeUtil.showEpisodePostStart.Length);
                        Newtonsoft.Json.Linq.JContainer playlistData = (Newtonsoft.Json.Linq.JContainer)Newtonsoft.Json.JsonConvert.DeserializeObject(postData);

                        StringBuilder builder = new StringBuilder();
                        foreach (Newtonsoft.Json.Linq.JProperty child in playlistData.Children())
                        {
                            builder.AppendFormat("&playlist[0][{0}]={1}", child.Name, child.Value.ToString());
                        }
                        builder.AppendFormat("&requestUrl={0}&requestSource=iVysilani&addCommercials=1&type=flash", video.VideoUrl.Remove(0, CeskaTelevizeUtil.baseUrl.Length));

                        String serializedDataForPost = HttpUtility.UrlEncode(builder.ToString()).Replace("%3d", "=").Replace("%26", "&");
                        System.Collections.Specialized.NameValueCollection headers = new System.Collections.Specialized.NameValueCollection();

                        headers.Add("Accept", "*/*"); // accept any content type
                        headers.Add("User-Agent", OnlineVideoSettings.Instance.UserAgent); // set the default OnlineVideos UserAgent when none specified
                        headers.Add("X-Requested-With", "XMLHttpRequest");
                        headers.Add("x-addr", "127.0.0.1");

                        playlistSerializedUrl = GetWebData("http://www.ceskatelevize.cz/ivysilani/ajax/get-client-playlist", serializedDataForPost, container, video.VideoUrl, null, false, false, null, null, headers, false);
                    }
                }
            }

            if (!String.IsNullOrWhiteSpace(playlistSerializedUrl))
            {
                Newtonsoft.Json.Linq.JContainer playlistJson = (Newtonsoft.Json.Linq.JContainer)Newtonsoft.Json.JsonConvert.DeserializeObject(playlistSerializedUrl);

                String videoDataUrl = String.Empty;
                foreach (Newtonsoft.Json.Linq.JProperty child in playlistJson.Children())
                {
                    if (child.Name == "url")
                    {
                        videoDataUrl = child.Value.ToString().Replace("%26", "&");
                    }
                }

                String videoConfigurationSerialized = GetWebData(videoDataUrl);
                Newtonsoft.Json.Linq.JContainer videoConfiguration = (Newtonsoft.Json.Linq.JContainer)Newtonsoft.Json.JsonConvert.DeserializeObject(videoConfigurationSerialized);

                String qualityUrl = (String)((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JProperty)((Newtonsoft.Json.Linq.JArray)videoConfiguration["playlist"])[0]["streamUrls"].First).Value).Value;
                String qualityData = GetWebData(qualityUrl);

                String[] lines = qualityData.Split(new Char[] { '\n' });
                int lastBadwidth = -1;

                for (int i = 0; i < lines.Length; i++)
                {
                    String line = lines[i];

                    if (line == "#EXTM3U")
                    {
                        continue;
                    }
                    else
                    {
                        int bandwidthIndex = line.IndexOf("BANDWIDTH=");

                        if (bandwidthIndex == (-1))
                        {
                            // url line
                            video.PlaybackOptions.Add(String.Format("Bandwidth (quality) {0}", lastBadwidth), line);
                        }
                        else
                        {
                            lastBadwidth = int.Parse(line.Substring(bandwidthIndex + "BANDWIDTH=".Length));
                        }
                    }
                }
            }

            if (video.PlaybackOptions.Count > 0)
            {
                var enumer = video.PlaybackOptions.GetEnumerator();
                enumer.MoveNext();
                return enumer.Current.Value;
            }

            return String.Empty;
        }

        #endregion
    }
}
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Xml;
using System.Web;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class MuzuTVUtil : GenericSiteUtil, IFilter
    {
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Videos per Page"), Description("Defines the default number of videos to display per page.")]
        int pageSize = 26;
        private Regex regExSearchChannel;
        private Regex regexPlayList;
        private Regex regExGenres;
        private string apiKey = "WCqz1q0T1d";
        private List<int> steps = new List<int>() { 10, 20, 30, 40, 50 };
        private Dictionary<String, String> orderByList;
        private Dictionary<String, String> timeFrameList = new Dictionary<string, string>();
        private int pageNr = 0;
        private bool fromHtml = false;
        private enum MuzuType { None, Genres, PlayList, SearchChannel, SearchVideo, NewReleases };
        public override int DiscoverDynamicCategories()
        {
            regExGenres = new Regex(@"<a\sclass=""ajax-load""\shref="".*?/(?<url>[^/]*)/"">\s*<div\sclass=""browse-heading-title-wrap"">\s*<div\sclass=""browse-area-title\swhite-text\sgreen-gradient"">(?<title>[^<]*)</div>", defaultRegexOptions);
            regExSearchChannel = new Regex(@"<li>\s*<a\sclass=""ch-item""\s*href=""/(?<url>[^/]*)/""\stitle=""[^""]*"">\s*<div\sclass=""v-thumb"">\s*<img\ssrc=""(?<thumb>[^""]*)""[^>]*>\s*</div><!--/\.v-thumb-->\s*<div\sclass=""v-details-small"">\s*<h2>(?<title>[^<]*)</h2>\s*<span>(?<description>[^<]*)</span>\s*</div>\s*</a>\s*</li>", defaultRegexOptions);
            regexPlayList = new Regex(@"<li[^>]*>\s*<a\sclass=""ajax-load""\shref=""(?<url>[^""]*)""[^>]*>\s*<ul\sclass=""browse-title-row\sgrid-row\smuzu-rollover-text"">\s*<li\sclass=""grid-column-12"">\s*<img\sclass=""browse-playlist-image""\salt=""[^""]*""\ssrc=""(?<thumb>[^""]*)""/>\s*</li>\s*<li\sclass=""grid-column-12\sbrowse-content-text-area"">\s*<ul\sclass=""grid-row"">\s*<li\sclass=""grid-column-12"">\s*<div\sclass=""browse-area-title-wrap"">\s*<div\sclass=""browse-top-line"">(?<title>[^<]*)</div>\s*</div>\s*</li>\s*<li\sclass=""grid-column-12\sright-align-column"">\s*<div\sclass=""browse-playlist-video-count-area"">\s*<div\sclass=""browse-playlist-video-count"">[^<]*</div>\s*<div\sclass=""small-play-title"">Play\sAll</div>\s*<div\sclass=""play-arrow""><div\sclass=""plus-icon"">\+</div></div>\s*</div>\s*</li>\s*<li\sclass=""grid-column-12\sbrowse-second-line"">(?<description>[^<]*)</li>\s*</ul>\s*</li>\s*</ul>", defaultRegexOptions);
            orderByList = new Dictionary<String, String>() {{"Views", "views"},
                                                            {"Recent", "recent"},
                                                            {"Alphabetical", "alpha"}};

            timeFrameList = new Dictionary<String, String>() {{"All Time", "0"},
                                                            {"Last Day", "1"},
                                                            {"Last Week", "7"},
                                                            {"Last Month", "31"}};

            Category genres = new RssLink() { Name = "Genres", Url = baseUrl + "music-videos/", HasSubCategories = true, Other = MuzuType.Genres };
            Settings.Categories.Add(genres);

            Category newReleases = new RssLink()
            {
                Name = "New Releases",
                Url = baseUrl + "new-releases/",
                Other = MuzuType.NewReleases
            };
            Settings.Categories.Add(newReleases);

            Category AtoZ = new RssLink()
            {
                Name = "A to Z",
                HasSubCategories = true,
                SubCategoriesDiscovered = true,
                SubCategories = new List<Category>()
            };
            for (char c = 'A'; c <= 'Z'; c++) AddToAZ(AtoZ, c);
            for (char c = '0'; c <= '9'; c++) AddToAZ(AtoZ, c);
            Settings.Categories.Add(AtoZ);

            Category playLists = new RssLink()
            {
                Name = "Featured PlayLists",
                HasSubCategories = true,
                Url = baseUrl + "browse-playlists/",
                SubCategories = new List<Category>(),
                Other = MuzuType.PlayList
            };
            /*AddToPlayList(playLists, "Featured", "featured");
            AddToPlayList(playLists, "Festivals", "festivals");
            AddToPlayList(playLists, "Popular", "views");
            AddToPlayList(playLists, "Recent", "recent");*/
            Settings.Categories.Add(playLists);

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        private void AddToAZ(Category aToZ, char name)
        {
            aToZ.SubCategories.Add(
                new RssLink()
                {
                    Name = name.ToString(),
                    Url = baseUrl + String.Format("api/browse?af={0}&", name.ToString().ToLowerInvariant()),
                    ParentCategory = aToZ
                }
                );
        }

        /*private void AddToPlayList(Category playLists, string name, string urlob)
        {
            playLists.SubCategories.Add(new RssLink()
            {
                Name = name,
                Url = @"http://www.muzu.tv/browse/loadPlaylistsByCategory?ob=" + urlob,
                Other = MuzuType.PlayList,
                ParentCategory = playLists,
                HasSubCategories = true
            }
            );
        }*/

        public override int ParseSubCategories(Category parentCategory, string data)
        {
            int res;
            switch ((MuzuType)parentCategory.Other)
            {
                case MuzuType.SearchChannel:
                    {
                        string url;
                        regEx_dynamicSubCategories = regExSearchChannel;
                        url = ((RssLink)parentCategory).Url;
                        dynamicSubCategoryUrlFormatString = baseUrl + "api/artist/details?aname={0}&";

                        data = GetWebData(url, forceUTF8: true);
                        res = base.ParseSubCategories(parentCategory, data);
                        if (parentCategory.SubCategories.Count > 0)
                        {
                            NextPageCategory c = parentCategory.SubCategories[parentCategory.SubCategories.Count - 1] as NextPageCategory;
                            if (c != null)
                                c.Url = HttpUtility.HtmlDecode(c.Url);
                        }
                        break;
                    }
                case MuzuType.Genres:
                    {
                        regEx_dynamicSubCategories = regExGenres;
                        dynamicSubCategoryUrlFormatString = baseUrl + @"api/browse?g={0}&";
                        res = base.ParseSubCategories(parentCategory, data);
                        parentCategory.SubCategories.Sort();
                        break;
                    }
                case MuzuType.PlayList:
                    {
                        regEx_dynamicSubCategories = regexPlayList;
                        dynamicSubCategoryUrlFormatString = "{0}";
                        res = base.ParseSubCategories(parentCategory, data);
                        foreach (Category cat in parentCategory.SubCategories)
                            cat.Other = MuzuType.PlayList;
                        break;
                    }
                default: throw new NotImplementedException("unhandled muzutype " + (MuzuType)parentCategory.Other);
            }
            return res;
        }

        public override bool CanSearch
        {
            get { return true; }
        }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            pageNr = 0;

            List<SearchResultItem> result = new List<SearchResultItem>();
            RssLink cat = new RssLink()
            {
                Name = "Artists",
                Url = baseUrl + String.Format("search?mySearch={0}&", HttpUtility.UrlEncode(query)),
                HasSubCategories = true,
                Other = MuzuType.SearchChannel
            };
            result.Add(cat);

            cat = new RssLink()
            {
                Name = "Videos",
                Other = MuzuType.SearchVideo,
                Url = baseUrl + String.Format("api/search?mySearch={0}&", HttpUtility.UrlEncode(query))
            };
            result.Add(cat);
            return result;
        }

        public override VideoInfo CreateVideoInfo()
        {
            return new MuzuTVVideoInfo();
        }

        private List<VideoInfo> lowGetVideoList(string url, string orderBy, string timeFrame, bool isFromHtml)
        {
            List<VideoInfo> res;
            fromHtml = isFromHtml;
            if (fromHtml)
            {
                string oldnextUrl = nextPageUrl;
                res = base.Parse(url, GetWebData(url, forceUTF8: true));
                foreach (VideoInfo video in res)
                {
                    Dictionary<string, string> playbackOptions = new Dictionary<string, string>();
                    playbackOptions.Add("lq", video.VideoUrl + "&qv=480");
                    playbackOptions.Add("hq", video.VideoUrl + "&qv=720");
                    video.Other = "PlaybackOptions://\n" + Helpers.CollectionUtils.DictionaryToString(playbackOptions);
                }
                if (nextPageUrl.EndsWith("?vo=0") || nextPageUrl.Equals(oldnextUrl))
                {
                    nextPageUrl = String.Empty;
                    nextPageAvailable = false;
                }
            }
            else
            {
                nextPageUrl = url;
                url = url + String.Format("muzuid={0}&l={1}&vd={2}&ob={3}&of={4}&format=xml", apiKey, pageSize, timeFrame, orderBy, pageNr * pageSize);
                string webData = GetWebData(url, forceUTF8: true);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(webData);
                res = new List<VideoInfo>();
                foreach (XmlNode node in doc.SelectNodes("//videos/video"))
                {
                    Dictionary<string, string> playbackOptions = new Dictionary<string, string>();
                    VideoInfo video = new MuzuTVVideoInfo();
                    video.Title = node.SelectSingleNode("artistname").InnerText + ":" + node.SelectSingleNode("title").InnerText;
                    video.Description = node.SelectSingleNode("description").InnerText;
                    video.Length = node.Attributes["duration"].Value;
                    video.Airdate = node.Attributes["releasedate"].Value;
                    if (video.Length == "0") video.Length = null;
                    string turl = @"http://player.muzu.tv/player/requestVideo?qv={0}&viewhash=ur4uJCOszp6vEJnIEXLUmxWMo&ai=" + node.Attributes["id"].Value;

                    playbackOptions.Add("lq", String.Format(turl, "480"));
                    playbackOptions.Add("hq", String.Format(turl, "720"));
                    video.VideoUrl = playbackOptions["hq"];

                    XmlNode thumb = node.SelectSingleNode("thumbnails/image[@type='6']");
                    if (thumb != null)
                        video.Thumb = thumb.SelectSingleNode("url").InnerText;
                    video.Other = "PlaybackOptions://\n" + Helpers.CollectionUtils.DictionaryToString(playbackOptions);
                    res.Add(video);
                }
                if (res.Count == pageSize)
                {
                    nextPageAvailable = true;
                    pageNr++;
                }
                else
                {
                    nextPageAvailable = false;
                }
            }
            return res;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            pageNr = 0;
            return lowGetVideoList(((RssLink)category).Url, "views", "7",
                 MuzuType.PlayList.Equals(category.Other) || MuzuType.NewReleases.Equals(category.Other));
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            return lowGetVideoList(nextPageUrl, "views", "7", fromHtml);
        }

        public List<int> GetResultSteps()
        {
            return steps;
        }

        public Dictionary<string, string> GetOrderByOptions()
        {
            return orderByList;
        }

        public Dictionary<string, string> GetTimeFrameOptions()
        {
            return timeFrameList;
        }

        private class MuzuTVVideoInfo : VideoInfo
        {
            public override string GetPlaybackOptionUrl(string option)
            {
                string webData = WebCache.Instance.GetWebData(PlaybackOptions[option]);
                JObject contentData = (JObject)JObject.Parse(webData);
                return contentData.Value<string>("url");
            }
        }

        #region IFilter
        public List<VideoInfo> FilterVideos(Category category, int maxResult, string orderBy, string timeFrame)
        {
            pageSize = maxResult;
            pageNr = 0;
            return lowGetVideoList(((RssLink)category).Url, orderBy, timeFrame, false);
        }

        public List<VideoInfo> FilterSearchResults(string query, int maxResult, string orderBy, string timeFrame)
        {
            pageNr = 0;
            pageSize = maxResult;
            string url = baseUrl + String.Format("api/search?mySearch={0}&", HttpUtility.UrlEncode(query));
            return lowGetVideoList(url, orderBy, timeFrame, false);
        }

        public List<VideoInfo> FilterSearchResults(string query, string category, int maxResult, string orderBy, string timeFrame)
        {
            pageSize = maxResult;
            string url = baseUrl + String.Format("api/search?mySearch={0}&g={1}&", HttpUtility.UrlEncode(query), category);
            return lowGetVideoList(url, orderBy, timeFrame, false);
        }
        #endregion

    }
}

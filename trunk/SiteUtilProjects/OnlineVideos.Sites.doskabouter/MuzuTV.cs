using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Xml;
using System.Web;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class MuzuTVUtil : GenericSiteUtil, IFilter
    {
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Videos per Page"), Description("Defines the default number of videos to display per page.")]
        int pageSize = 26;
        private RegexOptions defaultRegexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture;
        private Regex savedRegEx_dynamicSubCategories;
        private Regex regExChannel;
        private Regex regExHtmlVideoList;
        private Regex regexPlayList;
        private string apiKey = "WCqz1q0T1d";
        private List<int> steps = new List<int>() { 10, 20, 30, 40, 50 };
        private Dictionary<String, String> orderByList;
        private Dictionary<String, String> timeFrameList = new Dictionary<string, string>();
        private int pageNr = 0;
        private bool fromHtml = false;
        private enum MuzuType { None, Genres, AtoZ, Channels, Channel, PlayList };
        public override int DiscoverDynamicCategories()
        {
            savedRegEx_dynamicSubCategories = regEx_dynamicSubCategories;
            regExChannel = new Regex(@"<li>\s*<a\shref=""/[^/]*/(?<url>[^/]*)/[^""]*""\stitle=""[^""]*"">\s*<div\sclass=""v-thumb"">\s*<img\salt=""[^""]*""\ssrc=""(?<thumb>[^""]*)""[^>]*>\s*</div><!--/\.v-thumb-->\s*<div\sclass=""v-details"">\s*<h2>(?<title>[^<]*)</h2>\s*<h3>(?<description>[^<]*)</h3>\s*</div><!--/\.v-details\s-->\s*</a>\s*</li>", defaultRegexOptions);
            regExHtmlVideoList = new Regex(@"<li>\s*<a\stitle=""[^""]*""\shref=""/[^/]*/[^/]*/[^/]*/(?<VideoUrl>[^/]*)/"">\s*<img\salt=""[^""]*""\stitle=""[^""]*""\ssrc=""(?<ImageUrl>[^""]*)""\s/>\s*<span>(?<Title>[^<]*)</span>\s*</a>\s*</li>", defaultRegexOptions);
            regexPlayList = new Regex(@"<li\sclass=""note""\sdata-id=""(?<url>[^""]*)""\sdata-network-id=""(?<networkid>[^""]*)""\stitle=""[^""]*"">\s*<img\sheight=""42""\ssrc=""(?<thumb>[^""]*)""\swidth=""79""\s/>\s*<!--<h3>(?<description>[^<]*)</h3>\s*<h4>(?<title>[^<]*)</h4>\s*<h5><span\sclass=""count"">[^<]*</span>\svideos</h5>-->\s*</li>", defaultRegexOptions);
            orderByList = new Dictionary<String, String>() {{"Views", "views"},
                                                            {"Recent", "recent"},
                                                            {"Alphabetical", "alpha"}};

            timeFrameList = new Dictionary<String, String>() {{"All Time", "0"},
                                                            {"Last Day", "1"},
                                                            {"Last Week", "7"},
                                                            {"Last Month", "31"}};

            Category genres = new RssLink() { Name = "Genres", Url = baseUrl, HasSubCategories = true, Other = MuzuType.Genres };
            Settings.Categories.Add(genres);

            Category channels = new RssLink()
            {
                Name = "Channels",
                Url = baseUrl + "channels",
                HasSubCategories = true,
                Other = MuzuType.Channels
            };
            Settings.Categories.Add(channels);

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
                Name = "PlayLists",
                HasSubCategories = true,
                SubCategoriesDiscovered = true,
                SubCategories = new List<Category>()
            };
            AddToPlayList(playLists, "Featured", "featured");
            AddToPlayList(playLists, "Festivals", "festivals");
            AddToPlayList(playLists, "Popular", "views");
            AddToPlayList(playLists, "Recent", "recent");
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
                    Other = MuzuType.AtoZ,
                    ParentCategory = aToZ
                }
                );
        }

        private void AddToPlayList(Category playLists, string name, string urlob)
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
        }

        public override int ParseSubCategories(Category parentCategory, string data)
        {
            int res;
            switch ((MuzuType)parentCategory.Other)
            {
                case MuzuType.Channels:
                    {
                        regEx_dynamicSubCategories = regExChannel;
                        dynamicSubCategoryUrlFormatString = baseUrl + @"{0}/music-videos/";
                        res = base.ParseSubCategories(parentCategory, data);
                        if (parentCategory.SubCategories.Count > 0)
                        {
                            NextPageCategory c = parentCategory.SubCategories[parentCategory.SubCategories.Count - 1] as NextPageCategory;
                            if (c != null)
                                c.Url = HttpUtility.HtmlDecode(c.Url);
                        }
                        foreach (Category cat in parentCategory.SubCategories)
                            cat.Other = MuzuType.Channel;

                        break;
                    }
                case MuzuType.Genres:
                case MuzuType.AtoZ:
                    {
                        regEx_dynamicSubCategories = savedRegEx_dynamicSubCategories;
                        dynamicSubCategoryUrlFormatString = baseUrl + @"api/browse?g={0}&";
                        res = base.ParseSubCategories(parentCategory, data);
                        parentCategory.SubCategories.Sort();
                        break;
                    }
                case MuzuType.PlayList:
                    {
                        regEx_dynamicSubCategories = regexPlayList;
                        dynamicSubCategoryUrlFormatString = baseUrl + @"browse/loadPlaylistContents?pi={0}";
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

        public override List<ISearchResultItem> DoSearch(string query)
        {
            pageNr = 0;
            string url = baseUrl + String.Format("api/search?mySearch={0}&", HttpUtility.UrlEncode(query));
            return lowGetVideoList(url, "views", "7", false).ConvertAll<ISearchResultItem>(v => v as ISearchResultItem);
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
                res = base.Parse(url, GetWebData(url));
                foreach (VideoInfo video in res)
                {
                    video.PlaybackOptions = new Dictionary<string, string>();
                    video.PlaybackOptions.Add("lq", video.VideoUrl + "&videoType=1");
                    video.PlaybackOptions.Add("hq", video.VideoUrl + "&videoType=2");
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
                string webData = GetWebData(url);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(webData);
                res = new List<VideoInfo>();
                foreach (XmlNode node in doc.SelectNodes("videos/video"))
                {
                    VideoInfo video = new MuzuTVVideoInfo();
                    video.Title = node.SelectSingleNode("artistname").InnerText + ":" + node.SelectSingleNode("title").InnerText;
                    video.Description = node.SelectSingleNode("description").InnerText;
                    video.Length = node.Attributes["duration"].Value;
                    video.Airdate = node.Attributes["releasedate"].Value;
                    if (video.Length == "0") video.Length = null;
                    video.PlaybackOptions = new Dictionary<string, string>();
                    string turl = baseUrl + "player/playAsset?assetId=" + node.Attributes["id"].Value + "&videoType=";
                    video.PlaybackOptions.Add("lq", turl + "1");
                    video.PlaybackOptions.Add("hq", turl + "2");
                    video.VideoUrl = turl + "2";

                    XmlNode thumb = node.SelectSingleNode("thumbnails/image[@type='6']");
                    if (thumb != null)
                        video.ImageUrl = thumb.SelectSingleNode("url").InnerText;

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

        public override List<VideoInfo> getVideoList(Category category)
        {
            pageNr = 0;
            return lowGetVideoList(((RssLink)category).Url, "views", "7",
                MuzuType.Channel.Equals(category.Other) || MuzuType.PlayList.Equals(category.Other));
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return lowGetVideoList(nextPageUrl, "views", "7", fromHtml);
        }

        public List<int> getResultSteps()
        {
            return steps;
        }

        public Dictionary<string, string> getOrderbyList()
        {
            return orderByList;
        }

        public Dictionary<string, string> getTimeFrameList()
        {
            return timeFrameList;
        }

        private class MuzuTVVideoInfo : VideoInfo
        {
            public override string GetPlaybackOptionUrl(string option)
            {
                string webData = GetWebData(PlaybackOptions[option]);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(webData);
                return doc.SelectSingleNode("smil/body/video").Attributes["src"].Value;
            }
        }

        #region IFilter
        public List<VideoInfo> filterVideoList(Category category, int maxResult, string orderBy, string timeFrame)
        {
            pageSize = maxResult;
            pageNr = 0;
            return lowGetVideoList(((RssLink)category).Url, orderBy, timeFrame, false);
        }

        public List<VideoInfo> filterSearchResultList(string query, int maxResult, string orderBy, string timeFrame)
        {
            pageNr = 0;
            pageSize = maxResult;
            string url = baseUrl + String.Format("api/search?mySearch={0}&", HttpUtility.UrlEncode(query));
            return lowGetVideoList(url, orderBy, timeFrame, false);
        }

        public List<VideoInfo> filterSearchResultList(string query, string category, int maxResult, string orderBy, string timeFrame)
        {
            pageSize = maxResult;
            string url = baseUrl + String.Format("api/search?mySearch={0}&g={1}&", HttpUtility.UrlEncode(query), category);
            return lowGetVideoList(url, orderBy, timeFrame, false);
        }
        #endregion

    }
}

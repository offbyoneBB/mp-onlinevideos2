using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.ComponentModel;
using OnlineVideos.Hoster;
using OnlineVideos.Sites.Brownard;
using Newtonsoft.Json.Linq;
using HtmlAgilityPack;
using System.Xml;
using System.Web;

namespace OnlineVideos.Sites
{
    public class FourodUtil : SiteUtilBase
    {
        [Category("OnlineVideosUserConfiguration"), Description("Proxy to use for WebRequests (must be in the UK). Define like this: 83.84.85.86:8116")]
        string proxy = null;
        [Category("OnlineVideosUserConfiguration"), Description("If your proxy requires a username, set it here.")]
        string proxyUsername = null;
        [Category("OnlineVideosUserConfiguration"), Description("If your proxy requires a password, set it here.")]
        string proxyPassword = null;
        [Category("OnlineVideosUserConfiguration"), Description("Whether to download subtitles")]
        protected bool RetrieveSubtitles = false;
        [Category("OnlineVideosConfiguration"), Description("Url of the 4od swf object")]
        string swfObjectUrl = "http://www.channel4.com/static/programmes/asset/flash/swf/4odplayer-11.31.1.swf";

        const string BASE_URL = "http://m.channel4.com";
        const string SEARCH_URL = "http://m.channel4.com/4od/search/ajax/";

        static readonly Regex catchupDaysRegex = new Regex(@"<li[^>]*>\s*<a href=""([^""]*)"">([^\s]+)\s*<span");
        static readonly Regex previousCatchupDaysRegex = new Regex(@"<li[^>]*>\s*<a href=""([^""]*)"">\s*<span>Forward");
        static readonly Regex dateFromUrlRegex = new Regex(@"\d\d\d\d/(\d+)/\d+");

        static readonly Regex aToZRegex = new Regex(@"<a href=""(/4od/atoz/[^""]*)[^>]*>([^<]*)");

        static readonly Regex trimRegex = new Regex(@"\s\s+");
        static readonly Regex seasonRegex = new Regex(@"<li[^>]*>\s*<a href=""([^""]*)"">(\d+)<", RegexOptions.Singleline);
        static readonly Regex episodeRegex = new Regex(@"data-preselectasseturl=""([^""]*)"".*?<img class=""screenShot"" src=""([^""]*)"".*?<a[^>]*>([^<]*)</a>(.*?)\(\d", RegexOptions.Singleline);
        static readonly Regex episodeInfoRegex = new Regex(@"<p>(.*?)</p>", RegexOptions.Singleline);

        static readonly string[] months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        string defaultLogo;
        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            defaultLogo = string.Format(@"{0}\Icons\{1}.png", OnlineVideoSettings.Instance.ThumbsDir, siteSettings.Name);
        }

        DateTime lastRefesh = DateTime.MinValue;
        public override int DiscoverDynamicCategories()
        {
            if ((DateTime.Now - lastRefesh).TotalMinutes > 15)
            {
                BindingList<Category> dynamicCats = new BindingList<Category>();
                foreach (Category cat in Settings.Categories)
                {
                    if (cat is RssLink)
                    {
                        cat.HasSubCategories = true;
                        cat.SubCategoriesDiscovered = false;
                        if (string.IsNullOrEmpty(cat.Thumb))
                            cat.Thumb = defaultLogo;
                        dynamicCats.Add(cat);
                    }
                }
                Settings.Categories = dynamicCats;
                lastRefesh = DateTime.Now;
            }
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            bool isNextPage = false;
            if (parentCategory is NextPageCategory)
            {
                isNextPage = true;
                parentCategory.ParentCategory.Other = parentCategory.Other;
                parentCategory = parentCategory.ParentCategory;
            }

            string url = (parentCategory as RssLink).Url;
            List<Category> subCats;
            if (url == "http://m.channel4.com/4od/atoz")
                subCats = getAtoZCategories(url, parentCategory);
            else if (url == "http://m.channel4.com/4od/catchup")
                subCats = getCatchupDays(url, parentCategory);
            else if (url.StartsWith("http://m.channel4.com/4od/tags") || url.StartsWith("http://m.channel4.com/4od/atoz"))
                subCats = getShows(url, parentCategory);
            else
                subCats = getSeasons(url, parentCategory);

            if (isNextPage)
            {
                parentCategory.SubCategories.RemoveAt(parentCategory.SubCategories.Count - 1);
                parentCategory.SubCategories.AddRange(subCats);
            }
            else
            {
                parentCategory.SubCategories = subCats;
                parentCategory.SubCategoriesDiscovered = true;
            }
            return parentCategory.SubCategories.Count;
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            return this.DiscoverSubCategories(category);
        }

        List<Category> getAtoZCategories(string url, Category parentCategory)
        {
            List<Category> subCats = new List<Category>();
            string html = GetWebData(url);
            foreach (Match m in aToZRegex.Matches(html))
            {
                subCats.Add(new RssLink()
                {
                    Url = BASE_URL + m.Groups[1].Value,
                    Name = m.Groups[2].Value.Trim(),
                    HasSubCategories = true
                });
            }
            return subCats;
        }

        List<Category> getCatchupDays(string url, Category parentCategory)
        {
            List<Category> subCats = new List<Category>();
            while (true)
            {
                string html = GetWebData(url);
                MatchCollection matches = catchupDaysRegex.Matches(html);
                for (int x = matches.Count - 1; x >= 0; x--)
                {
                    Match m = matches[x];
                    string catUrl = m.Groups[1].Value;
                    string day = stripTags(m.Groups[2].Value);
                    if (day != "Today" && day != "Yesterday")
                    {
                        string month = months[int.Parse(dateFromUrlRegex.Match(catUrl).Groups[1].Value) - 1];
                        day += " " + month;
                    }

                    subCats.Add(new RssLink()
                    {
                        Url = BASE_URL + catUrl,
                        Name = day,
                        ParentCategory = parentCategory
                    });
                }

                Match previousDays = previousCatchupDaysRegex.Match(html);
                if (!previousDays.Success)
                    break;
                url = BASE_URL + previousDays.Groups[1].Value;
            }
            return subCats;
        }

        List<Category> getShows(string url, Category parentCategory)
        {
            List<Category> subCats = new List<Category>();
            int pageNumber = (parentCategory == null || parentCategory.Other == null) ? 1 : (int)parentCategory.Other;

            JObject jsonResponse = GetWebData<JObject>(url + "/page-" + pageNumber);
            foreach (JObject result in (JArray)jsonResponse["results"])
            {
                subCats.Add(new RssLink()
                {
                    Name = cleanString((string)result["title"]),
                    Thumb = (string)result["img"],
                    Url = BASE_URL + (string)result["url"],
                    ParentCategory = parentCategory,
                    HasSubCategories = true
                });
            }

            if ((int)jsonResponse["nextPageCount"] > 0)
            {
                subCats.Add(new NextPageCategory()
                {
                    Url = url,
                    Other = pageNumber + 1,
                    ParentCategory = parentCategory
                });
            }

            return subCats;
        }

        List<Category> getSeasons(string url, Category parentCategory)
        {
            List<Category> subCats = new List<Category>();
            string html = GetWebData(url);
            MatchCollection ms = seasonRegex.Matches(html);
            if (ms.Count > 0)
            {
                foreach (Match m in ms)
                {
                    subCats.Add(new RssLink()
                    {
                        Url = BASE_URL + m.Groups[1].Value,
                        Name = "Series " + m.Groups[2].Value,
                        Thumb = parentCategory.Thumb,
                        ParentCategory = parentCategory
                    });
                }
            }
            else
            {
                subCats.Add(new RssLink()
                {
                    Url = url,
                    Name = "Series 1",
                    Thumb = parentCategory.Thumb,
                    ParentCategory = parentCategory
                });
            }

            parentCategory.SubCategoriesDiscovered = true;
            return subCats;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();

            string html = GetWebData(((RssLink)category).Url);
            foreach (Match m in episodeRegex.Matches(html))
            {
                VideoInfo video = new VideoInfo()
                {
                    VideoUrl = m.Groups[1].Value,
                    ImageUrl = m.Groups[2].Value
                };

                string episodeTitle = cleanString(m.Groups[3].Value);
                MatchCollection info = episodeInfoRegex.Matches(m.Groups[4].Value);
                if (info.Count == 2)
                {
                    video.Title = cleanString(trim(info[0].Groups[1].Value)) + (episodeTitle != category.ParentCategory.Name ? " - " + episodeTitle : "");
                    video.Airdate = cleanString(trim(info[1].Groups[1].Value));
                }
                else
                {
                    video.Title = episodeTitle;
                    if (info.Count == 1)
                        video.Airdate = cleanString(trim(info[0].Groups[1].Value));
                }

                videos.Add(video);
            }

            return videos;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            XmlDocument xml = GetWebData<XmlDocument>(video.VideoUrl, proxy: getProxy());
            if (RetrieveSubtitles)
            {
                XmlNode subtitle = xml.SelectSingleNode("//subtitlesFileUri");
                if (subtitle != null && !string.IsNullOrEmpty(subtitle.InnerText))
                    video.SubtitleText = Utils.SubtitleReader.SAMI2SRT(GetWebData("http://ais.channel4.com" + subtitle.InnerText));
            }

            string token = xml.SelectSingleNode("//token").InnerText;
            string decryptedToken = FourodDecrypter.Decode4odToken(token);
            string cdn = xml.SelectSingleNode("//cdn").InnerText;
            string auth;
            if (cdn == "ll")
            {
                string e = xml.SelectSingleNode("//e").InnerText;
                auth = string.Format("e={0}&h={1}", e, decryptedToken);
            }
            else
            {
                string fingerprint = xml.SelectSingleNode("//fingerprint").InnerText;
                string slist = xml.SelectSingleNode("//slist").InnerText;
                auth = string.Format("auth={0}&aifp={1}&slist={2}", decryptedToken, fingerprint, slist);
            }

            string streamUri = xml.SelectSingleNode("//streamUri").InnerText;
            string playUrl = new Regex("(.*?)mp4:", RegexOptions.Singleline).Match(streamUri).Groups[1].Value;
            playUrl = playUrl.Replace(".com/", ".com:1935/");
            string playPath = new Regex("(mp4:.*)", RegexOptions.Singleline).Match(streamUri).Groups[1].Value;
            playPath = playPath + "?" + auth;

            return new MPUrlSourceFilter.RtmpUrl(playUrl + "?ovpfv=1.1&" + auth)
            {
                PlayPath = playPath,
                SwfUrl = swfObjectUrl,
                SwfVerify = true
            }.ToString();
        }

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
            string searchUrl = SEARCH_URL + urlEncode(query);
            return getShows(searchUrl, null).Select(c => (SearchResultItem)c).ToList();
        }

        #endregion

        System.Net.WebProxy getProxy()
        {
            if (string.IsNullOrEmpty(proxy))
                return null;

            System.Net.WebProxy proxyObj = new System.Net.WebProxy(proxy);
            if (!string.IsNullOrEmpty(proxyUsername) && !string.IsNullOrEmpty(proxyPassword))
                proxyObj.Credentials = new System.Net.NetworkCredential(proxyUsername, proxyPassword);
            return proxyObj;
        }

        static string trim(string s)
        {
            return trimRegex.Replace(s.Trim(), " ");
        }

        static string stripTags(string s)
        {
            s = cleanString(s);
            return s.Replace("<br>", " ").Replace("<p>", "").Replace("</p>", "\r\n");
        }

        static string cleanString(string s)
        {
            return s.Replace("&amp;", "&").Replace("&pound;", "£").Replace("&hellip;", "...").Trim();
        }

        static string urlEncode(string s)
        {
            //The search string has to be fully url encoded, then the %s encoded
            return HttpUtility.UrlEncode(s).Replace("+", "%20").Replace("!", "%21").Replace("'", "%27").Replace("(", "%28").Replace(")", "%29").Replace("%", "%25");
        }
    }
}
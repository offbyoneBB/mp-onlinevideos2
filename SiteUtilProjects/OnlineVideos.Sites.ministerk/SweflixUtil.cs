using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Web;
using System.Net;

namespace OnlineVideos.Sites
{
    public class SweflixUtil : LatestVideosSiteUtilBase
    {
        #region Enums

        public enum Languages { Svenska, Engelska };
        public enum VideoSort { Tillagd, IMDBbetyg, Titel_Ö_till_A, Lanseringsår, Popularitet };

        #endregion

        #region OnlineVideosUserConfiguration

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Username"), Description("Sweflix username")]
        protected string username = null;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Password"), Description("Sweflix password"), PasswordPropertyText(true)]
        protected string password = null;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Prefered subtitle language"), Description("Pick your prefered subtitle language")]
        Languages preferedLanguage = Languages.Svenska;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Sort videos by"), Description("Sort videos by, not for Latest/Popular categories")]
        VideoSort videoSort = VideoSort.Tillagd;
        string VideoSortId
        {
            get
            {
                switch (videoSort)
                {
                    case VideoSort.Tillagd: return "id";
                    case VideoSort.IMDBbetyg: return "imdbrate";
                    case VideoSort.Titel_Ö_till_A: return "titel";
                    case VideoSort.Lanseringsår: return "year";
                    case VideoSort.Popularitet: return "hits";
                    default: return "id";
                }
            }
        }
        #endregion
  
        #region OnlineVideosConfiguration

        [Category("OnlineVideosConfiguration"), Description("Url used for prepending relative links.")]
        protected string baseUrl;
        [Category("OnlineVideosConfiguration"), Description("Url for movies")]
        protected string filmFooterApiUrl;
        [Category("OnlineVideosConfiguration"), Description("Url for series")]
        protected string filmBodyApiUrl;
        [Category("OnlineVideosConfiguration"), Description("Url for search")]
        protected string searchUrl;
        [Category("OnlineVideosConfiguration"), Description("Url for latest videos")]
        protected string latestUrl;
        [Category("OnlineVideosConfiguration"), Description("Url for login")]
        protected string loginPostUrl;
        [Category("OnlineVideosConfiguration"), Description("Data to post when logging in")]
        protected string loginPostDataFormatString;
        [Category("OnlineVideosConfiguration"), Description("Referer url when logging in")]
        protected string loginRefererUrl;
        
        #endregion

        #region LogInHelpers

        private CookieContainer cc = null;
        private bool isLoggedIn = false;

        private bool HaveCredentials()
        {
            return !string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(password);
        }

        private bool NeedLogIn(string s)
        {
            return s.Contains(loginRefererUrl);
        }

        private bool IsLoggedIn<T>(T t)
        {
            if (t is HtmlDocument)
            {
                return !NeedLogIn((t as HtmlDocument).DocumentNode.OuterHtml);
            }
            else if (t is string)
            {
                return !NeedLogIn(t as string);
            }
            else
                return false;
        }

        private T MyGetWebData<T>(string url)
        {
            if (HaveCredentials() && cc == null)
                cc = new CookieContainer();

            if (!url.Contains("sort=") && !url.Contains(latestUrl) && !url.Contains("act=pop"))
            {
                if (url.Contains("?"))
                    url += "&sort=" + VideoSortId;
                else
                    url += "?sort=" + VideoSortId;
            }

            T t = GetWebData<T>(url, cookies: cc);
            if (HaveCredentials())
            {
                isLoggedIn = IsLoggedIn(t);
                if (!isLoggedIn)
                {
                    GetWebData(baseUrl + loginPostUrl, string.Format(loginPostDataFormatString, username, password), cc);
                    t = GetWebData<T>(url, cookies: cc);
                    isLoggedIn = IsLoggedIn(t);
                    if (!isLoggedIn)
                    {
                        cc = null;
                        throw new OnlineVideosException("Sweflix: Wrong username or password, unable to log in.");
                    }
                }
            }
            return t;
        }

        #endregion

        #region Categories

        public override int DiscoverDynamicCategories()
        {
            HtmlDocument doc = MyGetWebData<HtmlDocument>(baseUrl);
            HtmlNode sidebar = doc.DocumentNode.SelectSingleNode("//div[@id = 'sidebar']");
            Settings.Categories.Clear();
            string[] ignoredCategories = { "Premium", "TV serier" };
            foreach (HtmlNode item in sidebar.Descendants("a").Where(a => !string.IsNullOrEmpty(a.InnerText.Trim()) && (isLoggedIn || !ignoredCategories.Contains(a.InnerText.Trim()))))
            {
                Settings.Categories.Add(new RssLink() { Name = item.InnerText.Trim(), Url = item.GetAttributeValue("href", ""), SubCategories = new List<Category>(), HasSubCategories = true });
            }

            Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > 0;
            return Settings.Categories.Count;
        }

        private List<Category> GetSubCategories(RssLink parentCategory, bool skipLogIn = false)
        {
            HtmlDocument doc = MyGetWebData<HtmlDocument>(baseUrl + parentCategory.Url);
            List<Category> videos = new List<Category>();
            foreach (HtmlNode item in doc.DocumentNode.Descendants("a").Where(a => a.SelectNodes("div[@class = 'image-wrapper']") != null && !a.Descendants("i").Any()))
            {
                videos.Add(new RssLink() { Name = HttpUtility.HtmlDecode(item.Descendants("h5").FirstOrDefault().InnerText.Trim()), Url = item.GetAttributeValue("data-movieid", ""), Thumb = item.SelectSingleNode("div/img").GetAttributeValue("src", ""), Description = HttpUtility.HtmlDecode(item.SelectSingleNode("div/div/p").InnerText.Trim().Replace("Releasedatum:", "\nReleasedatum:").Replace("IMDB betyg:", "\nIMDB betyg:")), ParentCategory = parentCategory is NextPageCategory ? parentCategory.ParentCategory : parentCategory });
            }
            if (videos.Count > 0)
            {
                HtmlNode baut = doc.DocumentNode.Descendants("div").FirstOrDefault(d => d.GetAttributeValue("class", "") == "baut");
                if (baut != null)
                {
                    HtmlNode next = baut.Descendants("a").FirstOrDefault(a => a.InnerText.Contains("Nästa sida"));
                    if (next != null)
                    {
                        videos.Add(new NextPageCategory() { Url = next.GetAttributeValue("href", ""), ParentCategory = parentCategory is NextPageCategory ? parentCategory.ParentCategory : parentCategory });
                    }
                }
            }
            return videos;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = GetSubCategories(parentCategory as RssLink);
            parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
            return parentCategory.SubCategories.Count;
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            category.ParentCategory.SubCategories.Remove(category);
            List<Category> categories = GetSubCategories(category);
            category.ParentCategory.SubCategories.AddRange(categories);
            return categories.Count;
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
            RssLink searchCategory = new RssLink()
            {
                SubCategories = new List<Category>(),
                Url = searchUrl + HttpUtility.UrlEncode(query)
            };
            List<Category> categories = GetSubCategories(searchCategory);
            searchCategory.SubCategories = categories;
            searchCategory.SubCategoriesDiscovered = searchCategory.SubCategories.Count > 0;
            searchCategory.SubCategories.ForEach(c => results.Add(c));
            return results;
        }

        #endregion

        #region Videos

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            HtmlDocument footerDoc = MyGetWebData<HtmlDocument>(baseUrl + filmFooterApiUrl + (category as RssLink).Url);
            HtmlNode movieAnchor = footerDoc.DocumentNode.Descendants("a").FirstOrDefault(a => a.InnerText.ToLower().Contains("spela film"));
            if (movieAnchor != null)
            {
                VideoInfo video = new VideoInfo() { Title = category.Name, Description = category.Description, Thumb = category.Thumb, VideoUrl = baseUrl + movieAnchor.GetAttributeValue("href", "") };
                TrackingInfo ti = new TrackingInfo() { VideoKind = VideoKind.Movie, Title = category.Name.Replace("1080P", "").Replace("1080p", "") };
                Regex rgx = new Regex(@"Releasedatum:\s(\d{4})");
                Match m = rgx.Match(video.Description);
                uint y = 0;
                if (m.Success)
                {
                    uint.TryParse(m.Groups[1].Value, out y);
                    ti.Year = y;
                }
                video.Other = ti;
                videos.Add(video);
            }
            else
            {
                HtmlDocument bodyDoc = MyGetWebData<HtmlDocument>(baseUrl + filmBodyApiUrl + (category as RssLink).Url);
                IEnumerable<HtmlNode> rows = bodyDoc.DocumentNode.Descendants("tr").Where(r => r.GetAttributeValue("class", "").StartsWith("spaceUnder"));
                foreach (HtmlNode row in rows)
                {
                    VideoInfo video = new VideoInfo();
                    video.Title = category.Name;
                    video.Thumb = category.Thumb;
                    HtmlNode descP = row.Descendants("p").FirstOrDefault();
                    string desc = "";
                    if (descP != null)
                    {
                        Regex rgx = new Regex(@"Releasedatum:\s(\d{4})");
                        Match m = rgx.Match(video.Description);
                        uint y = 0;
                        if (m.Success)
                        {
                            uint.TryParse(m.Groups[1].Value, out y);
                        }
                        desc = descP.InnerText;
                        rgx = new Regex(@"(?<Title>[^-]+)-.*S(?<Season>\d+)E(?<Episode>\d+)[^-]*-(?<Description>.*)");
                        m = rgx.Match(desc);
                        uint s = 0;
                        uint e = 0;
                        if (m.Success)
                        {
                            TrackingInfo ti = new TrackingInfo() { VideoKind = VideoKind.TvSeries, Title = video.Title.Replace("1080P", "").Replace("1080p", ""), Year = y };
                            uint.TryParse(m.Groups["Season"].Value, out s);
                            uint.TryParse(m.Groups["Episode"].Value, out e);
                            video.Title = string.Format("{0} {1}x{2} {3}", video.Title, s, e, m.Groups["Title"].Value);
                            video.Description = m.Groups["Description"].Value;
                            ti.Season = s;
                            ti.Episode = e;
                            video.Other = ti;
                        }
                        HtmlNode a = row.Descendants("a").FirstOrDefault();
                        if (a != null)
                            video.VideoUrl = baseUrl + a.GetAttributeValue("href", "");
                        videos.Add(video);
                    }
                }
            }
            return videos;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            HtmlDocument data = MyGetWebData<HtmlDocument>(video.VideoUrl);
            HtmlNode doc = data.DocumentNode;

            string url = doc.SelectSingleNode("//source").GetAttributeValue("src", "");
            if (!string.IsNullOrEmpty(url))
            {
                Languages secondLang = preferedLanguage == Languages.Svenska ? Languages.Engelska : Languages.Svenska;

                HtmlNode subNode = doc.SelectSingleNode("//track[@label = '" + preferedLanguage.ToString() + "']");
                if (subNode == null)
                {
                    subNode = doc.SelectSingleNode("//track[@label = '" + secondLang.ToString() + "']");
                }
                if (subNode == null)
                {
                    subNode = doc.SelectSingleNode("//track");
                }
                if (subNode != null)
                {
                    string subUrl = subNode.GetAttributeValue("src", "");
                    if (!string.IsNullOrEmpty(subUrl))
                    {
                        video.SubtitleText = GetWebData(subUrl);
                        int newLineIndex = video.SubtitleText.IndexOf("\r\n");
                        if (newLineIndex > -1 && !video.SubtitleText.StartsWith("1") && video.SubtitleText.Count() - newLineIndex >= 2)
                        {
                            video.SubtitleText = video.SubtitleText.Substring(newLineIndex + 2);
                        }
                        //Only allow ASCII + Extended ASCII (many subtitles contain crap characters -> fail to load)
                        video.SubtitleText = Regex.Replace(video.SubtitleText, @"[^\u0000-\u00FF]", "");
                    }
                }
            }
            return url;
        }

        public override string GetFileNameForDownload(VideoInfo video, Category category, string url)
        {
            //Extension always .mp4
            return OnlineVideos.Utils.GetSaveFilename(video.Title) + ".mp4";
        }

        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            if (video.Other is TrackingInfo)
                return video.Other as TrackingInfo;
            return base.GetTrackingInfo(video);
        }

        #endregion

        #region LatestVideos

        public override List<VideoInfo> GetLatestVideos()
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            try
            {
                List<VideoInfo> temp;
                RssLink latest = new RssLink() { Name = "Senast inlagda", Url = latestUrl };
                List<Category> cats = GetSubCategories(latest);
                foreach (Category cat in cats)
                {
                    temp = GetVideos(cat);
                    if (temp.Count == 1)
                    {
                        videos.Add(temp.First());
                        if (videos.Count == LatestVideosCount) break;
                    }
                }
            }
            catch { /* if failed log in*/ }
            return videos;

        }

        #endregion
    }
}

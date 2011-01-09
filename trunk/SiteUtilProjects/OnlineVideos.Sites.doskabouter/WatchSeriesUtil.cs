using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Net;
using System.IO;
using System.Threading;
using OnlineVideos.Hoster.Base;

namespace OnlineVideos.Sites
{
    public class WatchSeriesUtil : GenericSiteUtil
    {
        private enum Depth { MainMenu = 0, Alfabet = 1, Series = 2, Seasons = 3, BareList = 4 };
        public CookieContainer cc = null;
        private bool isWatchMovies = false;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            isWatchMovies = baseUrl.StartsWith(@"http://watch-movies");
            //ReverseProxy.AddHandler(this);
        }

        public void GetBaseCookie()
        {
            WebCache.Instance[baseUrl] = null;
            CookieContainer tmpcc = new CookieContainer();
            GetWebData(baseUrl, tmpcc);

            cc = new CookieContainer();
            CookieCollection ccol = tmpcc.GetCookies(new Uri(baseUrl));
            foreach (Cookie c in ccol)
                cc.Add(c);
        }

        public override int DiscoverDynamicCategories()
        {
            GetBaseCookie();

            base.DiscoverDynamicCategories();
            int i = 0;
            do
            {
                RssLink cat = (RssLink)Settings.Categories[i];
                if (cat.Url.Equals(baseUrl) ||
                    (isWatchMovies && (cat.Name == "HOW TO WATCH" || cat.Name == "CONTACT"))
                   )
                    Settings.Categories.Remove(cat);
                else
                {
                    bool isMain;
                    if (isWatchMovies)
                        isMain = cat.Url.Contains(@"/year/") || cat.Url.Contains(@"/genres/") ||
                            cat.Url.EndsWith("/A");
                    else
                        isMain = cat.Url.EndsWith("/A");
                    if (isMain)
                        cat.Other = Depth.MainMenu;
                    else
                    {
                        cat.Other = Depth.BareList;
                        cat.HasSubCategories = false;
                    }
                    i++;
                }
            }
            while (i < Settings.Categories.Count);
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string url = ((RssLink)parentCategory).Url;
            string webData;
            int p = url.IndexOf('#');
            if (p >= 0)
            {
                string nm = url.Substring(p + 1);
                webData = GetWebData(url.Substring(0, p), cc);
                webData = @"class=""listbig"">" + GetSubString(webData, @"class=""listbig""><a name=""" + nm + @"""", @"class=""listbig""");
            }
            else
                webData = GetWebData(url, cc);

            parentCategory.SubCategories = new List<Category>();
            Match m = null;
            switch ((Depth)parentCategory.Other)
            {
                case Depth.MainMenu:
                    if (!isWatchMovies)
                    {
                        webData = GetSubString(webData, @"class=""pagination""", @"class=""listbig""");
                        m = regEx_dynamicCategories.Match(webData);
                    }
                    else
                        m = regEx_dynamicSubCategories.Match(webData);
                    break;
                case Depth.Alfabet:
                    webData = GetSubString(webData, @"class=""listbig""", @"class=""clear""");
                    m = regEx_dynamicSubCategories.Match(webData);
                    break;
                case Depth.Series:
                    webData = GetSubString(webData, @"class=""lists""", @"class=""clear""");
                    string[] tmp = { @"class=""lists""" };
                    string[] seasons = webData.Split(tmp, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string s in seasons)
                    {
                        RssLink cat = new RssLink();
                        cat.Name = HttpUtility.HtmlDecode(GetSubString(s, ">", "<"));
                        cat.Url = s;
                        cat.SubCategoriesDiscovered = true;
                        cat.HasSubCategories = false;
                        cat.Other = ((Depth)parentCategory.Other) + 1;

                        parentCategory.SubCategories.Add(cat);
                        cat.ParentCategory = parentCategory;
                    }
                    break;
                default:
                    m = null;
                    break;
            }

            while (m != null && m.Success)
            {
                RssLink cat = new RssLink();
                cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                cat.Url = m.Groups["url"].Value;
                cat.Description = HttpUtility.HtmlDecode(m.Groups["description"].Value);
                cat.HasSubCategories = !isWatchMovies && !parentCategory.Other.Equals(Depth.Series);
                cat.Other = ((Depth)parentCategory.Other) + 1;

                if (cat.Name == "NEW")
                {
                    cat.HasSubCategories = false;
                    cat.Other = Depth.BareList;
                }

                parentCategory.SubCategories.Add(cat);
                cat.ParentCategory = parentCategory;
                m = m.NextMatch();
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string webData = ((RssLink)category).Url;
            if (category.Other.Equals(Depth.BareList))
            {
                webData = GetWebData(webData, cc);
                if (isWatchMovies)
                    webData = GetSubString(webData, @"class=""listings""", @"class=""clear""");
                else
                    webData = GetSubString(webData, @"class=""listbig""", @"class=""clear""");
            }
            else
                if (isWatchMovies)
                    webData = GetWebData(webData, cc);

            List<VideoInfo> videos = new List<VideoInfo>();
            if (!string.IsNullOrEmpty(webData))
            {
                Match m = regEx_VideoList.Match(webData);
                while (m.Success)
                {
                    SeriesVideoInfo video = new SeriesVideoInfo();
                    video.parent = this;

                    video.Title = HttpUtility.HtmlDecode(m.Groups["Title"].Value);
                    video.VideoUrl = m.Groups["VideoUrl"].Value.Replace("..", baseUrl);
                    videos.Add(video);
                    m = m.NextMatch();
                }

            }
            return videos;
        }

        public override string getUrl(VideoInfo video)
        {
            string tmp = base.getUrl(video);
            List<PlaybackElement> lst = new List<PlaybackElement>();
            if (video.PlaybackOptions == null) // just one
                lst.Add(new PlaybackElement("100%justone", tmp));
            else
                foreach (string name in video.PlaybackOptions.Keys)
                {
                    PlaybackElement element = new PlaybackElement(name, video.PlaybackOptions[name]);

                    if (element.server.StartsWith("youtube.com"))
                    {
                        Dictionary<string, string> savOptions = video.PlaybackOptions;
                        video.GetPlaybackOptionUrl(name);
                        foreach (string nm in video.PlaybackOptions.Keys)
                        {
                            PlaybackElement el = new PlaybackElement();
                            el.server = element.server;
                            el.extra = nm;
                            el.percentage = element.percentage;
                            lst.Add(el);
                        }
                        video.PlaybackOptions = savOptions;
                    }
                    else
                    {
                        element.status = "ns";
                        if (element.server.Equals("videoclipuri") ||
                            HosterFactory.ContainsName(element.server.ToLower().Replace("google", "googlevideo")))
                            element.status = String.Empty;
                        lst.Add(element);
                    }
                }


            Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (PlaybackElement el in lst)
            {
                if (counts.ContainsKey(el.server))
                    counts[el.server]++;
                else
                    counts.Add(el.server, 1);
            }
            Dictionary<string, int> counts2 = new Dictionary<string, int>();
            foreach (string name in counts.Keys)
                if (counts[name] != 1)
                    counts2.Add(name, counts[name]);

            lst.Sort(PlaybackComparer);

            for (int i = lst.Count - 1; i >= 0; i--)
                if (counts2.ContainsKey(lst[i].server))
                {
                    lst[i].dupcnt = counts2[lst[i].server];
                    counts2[lst[i].server]--;
                }

            video.PlaybackOptions = new Dictionary<string, string>();
            foreach (PlaybackElement el in lst)
            {
                if (!Uri.IsWellFormedUriString(el.url, System.UriKind.Absolute))
                    el.url = new Uri(new Uri(baseUrl), el.url).AbsoluteUri;
                video.PlaybackOptions.Add(el.GetName(), el.url);
            }

            if (lst.Count == 1)
            {
                video.VideoUrl = video.GetPlaybackOptionUrl(lst[0].GetName());
                video.PlaybackOptions = null;
                return video.VideoUrl;
            }

            if (lst.Count > 0)
                tmp = lst[0].url;
            return tmp;
        }


        public class SeriesVideoInfo : VideoInfo
        {
            public WatchSeriesUtil parent;

            public override string GetPlaybackOptionUrl(string url)
            {
                string newUrl = base.PlaybackOptions[url];
                if (newUrl.StartsWith(@"http://youtube.com/get_video?video_id=")) return newUrl; //already handled youtube link

                parent.GetBaseCookie();
                string webData = GetWebData(newUrl, parent.cc);
                string savUrl = url;

                string vidId = GetSubString(webData, @"FlashVars=""input=", @"""");
                if (String.IsNullOrEmpty(vidId) && newUrl.IndexOf("deschide.php") >= 0)
                {
                    string docId = GetSubString(webData, "docid=", "&"); // for documentaries
                    if (!String.IsNullOrEmpty(docId))
                        url = @"http://video.google.com/videofeed?fgvns=1&fai=1&docid=" + docId + "&hl=undefined";
                    else
                    {
                        docId = GetSubString(webData, @"videoFile: '", @"'"); // for shows
                        if (!String.IsNullOrEmpty(docId))
                            return docId;
                    }
                }
                else
                {
                    if (newUrl.StartsWith(@"http://watch-movies"))
                        url = GetRedirectedUrl(@"http://www.watch-movies.ro/open_link.php?input=" + vidId);
                    else
                        url = GetRedirectedUrl(@"http://www.watch-series.com/open_link.php?vari=" + vidId);
                }
                return GetVideoUrl(url);
            }
        }

        private static string GetSubString(string s, string start, string until)
        {
            int p = s.IndexOf(start);
            if (p == -1) return String.Empty;
            p += start.Length;
            if (until == null) return s.Substring(p);
            int q = s.IndexOf(until, p);
            if (q == -1) return s.Substring(p);
            return s.Substring(p, q - p);
        }

        private int IntComparer(int i1, int i2)
        {
            if (i1 == i2) return 0;
            if (i1 > i2) return -1;
            return 1;
        }

        private int PlaybackComparer(PlaybackElement e1, PlaybackElement e2)
        {
            int res = IntComparer(e1.percentage, e2.percentage);
            if (res != 0)
                return res;
            else
                return String.Compare(e1.server, e2.server);
        }

    }

    internal class PlaybackElement
    {
        public int percentage;
        public string server;
        public string url;
        public string status;
        public string extra;
        public int dupcnt;

        public PlaybackElement()
        {
        }

        public string GetName()
        {
            string res = server;
            if (dupcnt != 0)
                res += " (" + dupcnt.ToString() + ')';
            if (!String.IsNullOrEmpty(extra))
                res += ' ' + extra;
            res += ' ' + percentage.ToString() + '%';
            if (!String.IsNullOrEmpty(status))
                res += " - " + status;
            return res;
        }

        public PlaybackElement(string aPlaybackName, string aUrl)
        {
            string[] tmp = aPlaybackName.Split('%');
            percentage = int.Parse(tmp[0]);
            server = tmp[1].TrimEnd(new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }).Trim();
            int i = server.IndexOf(".");
            if (i >= 0)
                server = server.Substring(0, i);
            url = aUrl;
        }

    }
}

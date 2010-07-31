using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Net;
using System.IO;
using System.Threading;

namespace OnlineVideos.Sites
{
    public class WatchSeriesUtil : GenericSiteUtil, ISimpleRequestHandler
    {
        private enum Depth { MainMenu = 0, Alfabet = 1, Series = 2, Seasons = 3, BareList = 4 };
        public CookieContainer cc = null;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            ReverseProxy.AddHandler(this);
        }

        public override int DiscoverDynamicCategories()
        {
            CookieContainer tmpcc = new CookieContainer();
            GetWebData(baseUrl, tmpcc);

            cc = new CookieContainer();
            CookieCollection ccol = tmpcc.GetCookies(new Uri(baseUrl));
            foreach (Cookie c in ccol)
                cc.Add(c);

            base.DiscoverDynamicCategories();
            int i = 0;
            do
            {
                RssLink cat = (RssLink)Settings.Categories[i];
                if (cat.Url.Equals(baseUrl))
                    Settings.Categories.Remove(cat);
                else
                {
                    if (cat.Url.EndsWith("/A"))
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
                    webData = GetSubString(webData, @"class=""pagination""", @"class=""listbig""");
                    m = regEx_dynamicCategories.Match(webData);
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
                cat.HasSubCategories = !parentCategory.Other.Equals(Depth.Series);
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
                webData = GetSubString(webData, @"class=""listbig""", @"class=""clear""");
            }
            List<VideoInfo> videos = new List<VideoInfo>();
            if (!string.IsNullOrEmpty(webData))
            {
                Match m = regEx_VideoList.Match(webData);
                while (m.Success)
                {
                    SeriesVideoInfo video = new SeriesVideoInfo();
                    video.reqHandler = this;
                    video.cc = cc;

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

                    if (element.server == "youtube.com")
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
                        if (element.server == "movshare.net" || element.server == "playmyvid.com" ||
                            element.server == "divxden.com" || element.server == "smotri.com" ||
                            element.server == "wisevid.com" || element.server == "megavideo.com" ||
                            element.server == "zshare.net" || element.server == "vureel.com" ||
                            element.server == "stagevu.com" || element.server == "56.com" ||
                            element.server == "loombo.com" || element.server == "ufliq.com" ||
                            element.server == "tudou.com" || element.server == "google.ca")
                            element.status = String.Empty;
                        else
                            if (element.server == "livevideo.com" || element.server == "veehd.com" ||
                                element.server == "myspace.com" || element.server == "cinshare.com" ||
                                element.server == "2gb-hosting.com" || element.server == "gigabyteupload.com")
                                element.status = "wip";

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
            public ISimpleRequestHandler reqHandler;
            public CookieContainer cc;

            public override string GetPlaybackOptionUrl(string url)
            {
                string newUrl = base.GetPlaybackOptionUrl(url);
                if (newUrl.StartsWith(@"http://youtube.com/get_video?video_id=")) return newUrl; //already handled youtube link

                string webData = GetWebData(newUrl, cc);
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
                    url = GetRedirectedUrl(@"http://www.watch-series.com/open_link.php?vari=" + vidId);
                if (savUrl.StartsWith("youtube.com"))
                    return UrlTricks.YoutubeTrick(url, this);
                if (url.StartsWith("http://www2.movshare.net"))
                    return UrlTricks.MovShareTrick(url);
                if (url.StartsWith("http://www.vureel.com"))
                    return UrlTricks.VureelTrick(url);
                if (url.StartsWith("http://www.56.com"))
                    return UrlTricks.FiftySixComTrick(url);
                if (url.StartsWith("http://www.livevideo.com"))
                    return UrlTricks.LiveVideoTrick(url);
                if (url.StartsWith("http://www.divxden.com"))
                    return UrlTricks.DivxDenTrick(url);
                if (url.StartsWith("http://smotri.com"))
                    return UrlTricks.SmotriTrick(url);
                if (url.StartsWith("http://video.google"))
                    return UrlTricks.GoogleCaTrick(url);
                if (url.StartsWith("http://www.megavideo.com"))
                    return UrlTricks.MegaVideoTrick(url);
                if (url.StartsWith("http://www.myspace.com"))
                    return UrlTricks.MyspaceTrick(url);

                webData = GetWebData(url);

                if (url.StartsWith("http://loombo.com"))
                {
                    string postData = String.Empty;
                    Match m = Regex.Match(webData, @"<input\stype=""hidden""\sname=""(?<m0>[^""]*)""\svalue=""(?<m1>[^""]*)");
                    while (m.Success)
                    {
                        if (!String.IsNullOrEmpty(postData))
                            postData += "&";
                        postData += m.Groups["m0"].Value + "=" + m.Groups["m1"].Value;
                        m = m.NextMatch();
                    }
                    if (String.IsNullOrEmpty(postData))
                        return null;

                    Thread.Sleep(5000);

                    webData = GetWebDataFromPost(url, postData);
                    string packed = GetSubString(webData, @"return p}", @"</script>");
                    packed = packed.Replace(@"\'", @"'");
                    string unpacked = UrlTricks.UnPack(packed);
                    return GetSubString(unpacked, @"'file','", @"'");
                }

                if (url.StartsWith("http://www.2gb-hosting.com"))
                {
                    string postData = String.Empty;
                    string post = GetSubString(webData, @"<form>", @"</form>");
                    Match m = Regex.Match(webData, @"<input\stype=""[^""]*""\sname=""(?<m0>[^""]*)""\svalue=""(?<m1>[^""]*)");
                    while (m.Success)
                    {
                        if (!String.IsNullOrEmpty(postData))
                            postData += "&";
                        postData += m.Groups["m0"].Value + "=" + m.Groups["m1"].Value;
                        m = m.NextMatch();
                    }
                    webData = GetWebDataFromPost(url, postData);
                    string res = GetSubString(webData, @"embed", @">");
                    res = GetSubString(res, @"src=""", @"""");
                    return res;
                }

                if (url.StartsWith("http://www.ufliq.com"))
                {
                    string postData = String.Empty;
                    Match m = Regex.Match(webData, @"<input\stype=""hidden""\sname=""(?<m0>[^""]*)""\svalue=""(?<m1>[^""]*)");
                    while (m.Success)
                    {
                        if (!String.IsNullOrEmpty(postData))
                            postData += "&";
                        postData += m.Groups["m0"].Value + "=" + m.Groups["m1"].Value;
                        m = m.NextMatch();
                    }
                    if (String.IsNullOrEmpty(postData))
                        return null;

                    Thread.Sleep(5000);

                    webData = GetWebDataFromPost(url, postData);
                    string packed = GetSubString(webData, @"return p}", @"</script>");
                    packed = packed.Replace(@"\'", @"'");
                    string unpacked = UrlTricks.UnPack(packed);
                    return GetSubString(unpacked, @"'file','", @"'");
                }

                if (url.StartsWith("http://www.cinshare.com"))
                {
                    string tmp = GetSubString(webData, @"<iframe src=""", @"""");
                    webData = GetWebData(tmp);
                    tmp = GetSubString(webData, @"<param name=""src"" value=""", @"""");
                    return GetRedirectedUrl(tmp);
                }
                if (url.StartsWith("http://veehd.com"))
                    return GetSubString(webData, @"name=""src"" value=""", @"""");

                if (url.StartsWith("http://www.tudou.com"))
                {  //babylon 5
                    string iid = GetSubString(webData, @"var iid = ", "\n");
                    url = @"http://v2.tudou.com/v?it=" + iid;
                    return UrlTricks.TudouTrick(url, reqHandler);
                }

                if (url.StartsWith("http://stagevu.com"))
                {
                    url = GetSubString(webData, @"url[", @"';");
                    return GetSubString(url, @"'", @"'");
                }

                if (url.StartsWith("http://www.zshare.net"))
                {
                    url = GetSubString(webData, @"<iframe src=""", @"""");
                    return UrlTricks.ZShareTrick(url);
                }

                if (url.StartsWith("http://www.playmyvid.com"))
                {
                    url = GetSubString(webData, @"flv=", @"&");
                    return @"http://www.playmyvid.com/files/videos/" + url;
                }

                if (url.StartsWith("http://www.wisevid.com"))
                {
                    // (with age confirm)
                    url = @"http://www.wisevid.com/play?v=" + GetSubString(webData,
                        @"play?v=", @"""");
                    string tmp2 = GetWebDataFromPost(url, "a=1");
                    url = GetSubString(tmp2, "getF('", "'");
                    return UrlTricks.WiseVidTrick(url);
                }

                return null;
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


        #region ISimpleRequestHandler Members

        public void UpdateRequest(HttpWebRequest request)
        {
            request.UserAgent = OnlineVideoSettings.USERAGENT;
        }

        #endregion
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
            server = tmp[1].TrimEnd(new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' });
            url = aUrl;
        }

    }
}

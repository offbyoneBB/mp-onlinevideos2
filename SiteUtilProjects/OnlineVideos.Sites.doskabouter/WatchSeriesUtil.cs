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
            CookieContainer tmpcc = new CookieContainer();
            GetWebData(baseUrl, tmpcc);

            cc = new CookieContainer();
            CookieCollection ccol = tmpcc.GetCookies(new Uri(baseUrl));
            foreach (Cookie c in ccol)
                cc.Add(c);
        }

        public override int DiscoverDynamicCategories()
        {
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
            Dictionary<string, string> newPlaybackOptions = new Dictionary<string, string>();
            string firstName = null;
            foreach (string name in video.PlaybackOptions.Keys)
            {
                if (name.StartsWith("youtube.com"))
                {
                    Dictionary<string, string> savOptions = video.PlaybackOptions;
                    video.GetPlaybackOptionUrl(name);
                    foreach (string nm in video.PlaybackOptions.Keys)
                        newPlaybackOptions.Add(nm, video.PlaybackOptions[nm]);
                    video.PlaybackOptions = savOptions;
                }
                else
                    if (name.StartsWith("movshare.net") || name.StartsWith("playmyvid.com") ||
                        name.StartsWith("divxden.com") || name.StartsWith("smotri.com") ||
                        name.StartsWith("wisevid.com") || name.StartsWith("megavideo.com") ||
                        name.StartsWith("zshare.net") || name.StartsWith("vureel.com") ||
                        name.StartsWith("stagevu.com") || name.StartsWith("56.com") ||
                        name.StartsWith("loombo.com") || name.StartsWith("ufliq.com") ||
                        name.StartsWith("tudou.com") || (name.StartsWith("google.ca")))
                        newPlaybackOptions.Add(name, video.PlaybackOptions[name]);
                    else
                        if (name.StartsWith("livevideo.com") || name.StartsWith("veehd.com") ||
                            name.StartsWith("myspace.com") || name.StartsWith("cinshare.com") ||
                            name.StartsWith("2gb-hosting.com") || name.StartsWith("gigabyteupload.com"))
                            newPlaybackOptions.Add(name + " wip", video.PlaybackOptions[name]);
                        else
                            newPlaybackOptions.Add(name + " ns", video.PlaybackOptions[name]);
                if (firstName == null)
                    firstName = name;
            }
            video.PlaybackOptions = newPlaybackOptions;
            if (video.PlaybackOptions.Count == 1)
            {
                video.VideoUrl = video.GetPlaybackOptionUrl(firstName);
                video.PlaybackOptions = null;
                return video.VideoUrl;
            }
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

                webData = GetSubString(webData, @"FlashVars=""input=", @"""");
                string tmpUrl = null;

                tmpUrl = GetRedirectedUrl(@"http://www.watch-series.com/open_link.php?vari=" + webData);
                if (url.StartsWith("youtube.com"))
                    return UrlTricks.YoutubeTrick(tmpUrl, this);
                if (url.StartsWith("movshare.net"))
                    return UrlTricks.MovShareTrick(tmpUrl);
                if (url.StartsWith("vureel.com"))
                    return UrlTricks.VureelTrick(tmpUrl);
                if (url.StartsWith("56.com"))
                    return UrlTricks.FiftySixComTrick(tmpUrl);
                if (url.StartsWith("livevideo.com"))
                    return UrlTricks.LiveVideoTrick(tmpUrl);
                if (url.StartsWith("divxden.com"))
                    return UrlTricks.DivxDenTrick(tmpUrl);
                if (url.StartsWith("smotri.com"))
                    return UrlTricks.SmotriTrick(tmpUrl);
                if (url.StartsWith("google.ca"))
                    return UrlTricks.GoogleCaTrick(tmpUrl);
                if (url.StartsWith("megavideo.com"))
                    return UrlTricks.MegaVideoTrick(tmpUrl);
                if (url.StartsWith("myspace.com"))
                    return UrlTricks.MyspaceTrick(tmpUrl);

                webData = GetWebData(tmpUrl);

                if (url.StartsWith("loombo.com"))
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

                    webData = GetWebDataFromPost(tmpUrl, postData);
                    string packed = GetSubString(webData, @"return p}", @"</script>");
                    packed = packed.Replace(@"\'", @"'");
                    string unpacked = UrlTricks.UnPack(packed);
                    return GetSubString(unpacked, @"'file','", @"'");
                }

                if (url.StartsWith("2gb-hosting.com"))
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
                    webData = GetWebDataFromPost(tmpUrl, postData);
                    string res = GetSubString(webData, @"embed", @">");
                    res = GetSubString(res, @"src=""", @"""");
                    return res;
                }

                if (url.StartsWith("ufliq.com"))
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

                    webData = GetWebDataFromPost(tmpUrl, postData);
                    string packed = GetSubString(webData, @"return p}", @"</script>");
                    packed = packed.Replace(@"\'", @"'");
                    string unpacked = UrlTricks.UnPack(packed);
                    return GetSubString(unpacked, @"'file','", @"'");
                }

                if (url.StartsWith("cinshare.com"))
                {
                    string tmp = GetSubString(webData, @"<iframe src=""", @"""");
                    webData = GetWebData(tmp);
                    tmp = GetSubString(webData, @"<param name=""src"" value=""", @"""");
                    return GetRedirectedUrl(tmp);
                }
                if (url.StartsWith("veehd.com"))
                    return GetSubString(webData, @"name=""src"" value=""", @"""");

                if (url.StartsWith("tudou.com"))
                {  //babylon 5
                    string iid = GetSubString(webData, @"var iid = ", "\n");
                    tmpUrl = @"http://v2.tudou.com/v?it=" + iid;
                    return UrlTricks.TudouTrick(tmpUrl, reqHandler);
                }

                if (url.StartsWith("stagevu.com"))
                {
                    tmpUrl = GetSubString(webData, @"url[", @"';");
                    return GetSubString(tmpUrl, @"'", @"'");
                }

                if (url.StartsWith("zshare.net"))
                {
                    tmpUrl = GetSubString(webData, @"<iframe src=""", @"""");
                    return UrlTricks.ZShareTrick(tmpUrl);
                }

                if (url.StartsWith("playmyvid.com"))
                {
                    tmpUrl = GetSubString(webData, @"flv=", @"&");
                    return @"http://www.playmyvid.com/files/videos/" + tmpUrl;
                }

                if (url.StartsWith("wisevid.com"))
                {
                    // (with age confirm)
                    tmpUrl = @"http://www.wisevid.com/play?v=" + GetSubString(webData,
                        @"play?v=", @"""");
                    string tmp2 = GetWebDataFromPost(tmpUrl, "a=1");
                    tmpUrl = GetSubString(tmp2, "getF('", "'");
                    return UrlTricks.WiseVidTrick(tmpUrl);
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


        #region ISimpleRequestHandler Members

        public void UpdateRequest(HttpWebRequest request)
        {
            request.UserAgent = OnlineVideoSettings.USERAGENT;
        }

        #endregion
    }
}

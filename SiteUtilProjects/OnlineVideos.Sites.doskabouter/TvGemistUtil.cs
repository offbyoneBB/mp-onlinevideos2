using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Web;
using System.Xml;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class TvGemistUtil : SiteUtilBase
    {
        private SortedList<string, object> rtlBlackList;
        private enum Source { Net5Gemist, SBSGemist, VeronicaGemist, UitzendingGemist, RtlGemist, RtlGemistOpDag };
        private RssLink baseCategory;

        public override void Initialize(OnlineVideos.SiteSettings siteSettings)
        {
            string[] lst = { "Een", "Films Bij RTL", "Publieksservice", "RTL Forum", "RTL Gemist FAQ", "RTL Gids", 
                               "RTL News Agent","RTL Shop", "RTL Tickets","RTL Video", "RTL Weer", 
                               "Sex: How To Do Everything", "Spelsalon" ,"Teletext"};
            rtlBlackList = new SortedList<string, object>(lst.Length);
            foreach (string s in lst) rtlBlackList.Add(s, null);

            base.Initialize(siteSettings);
        }

        public override int DiscoverDynamicCategories()
        {
            RssLink cat;
            //======================= uitzending gemist ===================
            cat = new RssLink();
            cat.Name = "Uitzending Gemist";
            cat.Thumb = OnlineVideoSettings.Instance.ThumbsDir == null ? null :
                System.IO.Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, @"\Icons\Tvgemist\uitzendinggemist.png");
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);
            string[] cats = { "Op alfabet", "Op dag", "Op zender" };
            AddSubcats(cats, cat);

            CookieContainer cc = new CookieContainer();
            cat.SubCategories[0].Other = new UitzendingGemistSpecifics(cc, @"<div id=""nav_letter"">", 0);
            cat.SubCategories[1].Other = new UitzendingGemistSpecifics(cc, @"<div id=""nav_dag"">", 0);
            cat.SubCategories[2].Other = new UitzendingGemistSpecifics(cc, @"<div id=""nav_net"">", 0);

            //======================= RTL gemist ===================
            cat = new RssLink();
            cat.Name = "Rtl Gemist";
            cat.Thumb = OnlineVideoSettings.Instance.ThumbsDir == null ? null :
                System.IO.Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, @"\OnlineVideos\Icons\Tvgemist\rtlgemist.png");
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);
            string[] cats2 = { "Op alfabet", "Op dag" };
            AddSubcats(cats2, cat);
            cat.SubCategories[0].Other = new RtlGemistSpecifics(false);
            ((RssLink)cat.SubCategories[0]).Url = @"http://rtl.nl/experience/rtlnl/";
            cat.SubCategories[1].Other = new RtlGemistSpecifics(true);

            //======================= NET5 gemist ===================
            cat = new RssLink();
            cat.Name = "Net5 Gemist";
            cat.Thumb = OnlineVideoSettings.Instance.ThumbsDir == null ? null :
                System.IO.Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, @"\OnlineVideos\Icons\Tvgemist\net5gemist.png");
            cat.HasSubCategories = true;
            cat.Url = @"http://www.net5.nl/web/show/id=95681/langid=43";
            cat.Other = new Specifics(Source.Net5Gemist, @"http://www.net5.nl");
            Settings.Categories.Add(cat);

            //======================= SBS6 gemist ===================
            cat = new RssLink();
            cat.Name = "SBS6 Gemist";
            cat.Thumb = OnlineVideoSettings.Instance.ThumbsDir == null ? null :
                System.IO.Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, @"\OnlineVideos\Icons\Tvgemist\sbsgemist.png");
            cat.HasSubCategories = true;
            cat.Url = @"http://www.sbs6.nl/web/show/id=73863/langid=43";
            cat.Other = new Specifics(Source.SBSGemist, @"http://www.sbs6.nl");
            Settings.Categories.Add(cat);

            //======================= Veronica gemist ===================
            cat = new RssLink();
            cat.Name = "Veronica Gemist";
            cat.Thumb = OnlineVideoSettings.Instance.ThumbsDir == null ? null :
                System.IO.Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, @"\OnlineVideos\Icons\Tvgemist\veronicagemist.png");
            cat.HasSubCategories = true;
            cat.Url = @"http://www.veronicatv.nl/web/show/id=96520/langid=43";
            cat.Other = new Specifics(Source.VeronicaGemist, @"http://www.veronicatv.nl");
            Settings.Categories.Add(cat);

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            switch (((Specifics)parentCategory.Other).source)
            {
                case Source.UitzendingGemist: return UitzendingGemistDiscoverSubCategories((RssLink)parentCategory);
                case Source.RtlGemist: return RtlGemistDiscoverSubCategories((RssLink)parentCategory);
                case Source.Net5Gemist: return Net5SbsVeronicaGemistDiscoverSubCategories((RssLink)parentCategory);
                case Source.SBSGemist: return Net5SbsVeronicaGemistDiscoverSubCategories((RssLink)parentCategory);
                case Source.VeronicaGemist: return Net5SbsVeronicaGemistDiscoverSubCategories((RssLink)parentCategory);
            }
            throw new NotImplementedException();
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            baseCategory = (RssLink)category;
            switch (((Specifics)category.Other).source)
            {
                case Source.UitzendingGemist:
                    {
                        UitzendingGemistSpecifics specifics = (UitzendingGemistSpecifics)category.Other;
                        specifics.pageNr = 0;
                        return UitzendingGemistGetVideoList(baseCategory, specifics);
                    }
                case Source.RtlGemist: return RtlGemistGetVideoList(baseCategory);
                case Source.Net5Gemist: return Net5SbsVeronicaGemistGetVideoList(baseCategory);
                case Source.SBSGemist: return Net5SbsVeronicaGemistGetVideoList(baseCategory);
                case Source.VeronicaGemist: return Net5SbsVeronicaGemistGetVideoList(baseCategory);
            }
            throw new NotImplementedException();
        }

        public override string getUrl(VideoInfo video)
        {
            string s = video.Other as string;
            if (s != null)
            {
                if (Enum.IsDefined(typeof(Source), s))
                    video.Other = Enum.Parse(typeof(Source), s, true);
                else
                    video.Other = null;
            }

            if (Source.UitzendingGemist.Equals(video.Other))
                return GenericSiteUtil.GetVideoUrl(video.VideoUrl, video);
            if (Source.VeronicaGemist.Equals(video.Other) || Source.SBSGemist.Equals(video.Other) || Source.Net5Gemist.Equals(video.Other))
            {
                string webData = GetWebData(video.VideoUrl);
                return GetSubString(webData, @"class=""wmv-player-holder"" href=""", @"""");
            }
            if (Source.RtlGemistOpDag.Equals(video.Other) || Source.RtlGemist.Equals(video.Other))
            {
                Regex regex_RtlDetails = Specifics.getRegex(@"bandwidth:\s*'(?<bandwidth>[^']*)(?:(?!file).)*file:\s*'(?<url>[^']*)");
                string webData = GetWebData(video.VideoUrl);
                Match detm = regex_RtlDetails.Match(webData);
                int highest = 0;
                string result = null;
                while (detm.Success)
                {
                    int bw;
                    if (!int.TryParse(detm.Groups["bandwidth"].Value, out bw))
                        bw = 0;
                    if (bw >= highest)
                    {
                        result = detm.Groups["url"].Value;
                        highest = bw;
                    }
                    detm = detm.NextMatch();
                }

                if (result != null && result.IndexOf("http://av.rtl.nl/") >= 0 && result.IndexOf(@"rtlboulevard") == 0)
                {
                    string exp = result.Replace("http://av.rtl.nl/web/", "http://www.rtl.nl/system/video/wvx/");
                    int ind = exp.LastIndexOf('/');
                    ind = exp.LastIndexOf('/', ind - 1);
                    exp = exp.Insert(ind, "/miMedia");
                    exp = exp.Replace(".MiMedia_WM_1500K_V9.wmv", ".xml/1500.wvx");

                    // transform 
                    // http://av.rtl.nl/web/components/soaps/gtst/203350/203939.s4m.29707557.Goede_Tijden_Slechte_Tijden_s24_a4020.MiMedia_WM_1500K_V9.wmv
                    // into
                    // http://www.rtl.nl/system/video/wvx/components/soaps/gtst/miMedia/203350/203939.s4m.29707557.Goede_Tijden_Slechte_Tijden_s24_a4020.xml/1500.wvx

                    result = exp;
                }

                return result;
            }

            return video.VideoUrl;
        }

        public override bool HasNextPage
        {
            get
            {
                if (baseCategory.Other is UitzendingGemistSpecifics)
                    return ((UitzendingGemistSpecifics)baseCategory.Other).hasNextPage;
                else
                    return false;
            }
        }

        public override bool HasPreviousPage
        {
            get
            {
                if (baseCategory.Other is UitzendingGemistSpecifics)
                    return ((UitzendingGemistSpecifics)baseCategory.Other).pageNr >= 1;
                else
                    return false;
            }
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            UitzendingGemistSpecifics specifics = (UitzendingGemistSpecifics)baseCategory.Other;
            specifics.pageNr++;
            return UitzendingGemistGetVideoList(baseCategory, specifics);
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            UitzendingGemistSpecifics specifics = (UitzendingGemistSpecifics)baseCategory.Other;
            specifics.pageNr--;
            return UitzendingGemistGetVideoList(baseCategory, specifics);
        }


        #region UitzendingGemist
        private int UitzendingGemistDiscoverSubCategories(RssLink parentCategory)
        {
            UitzendingGemistSpecifics specifics = (UitzendingGemistSpecifics)parentCategory.Other;
            if (specifics.depth == 0)
            {
                //a..z and maandag..zondag
                string webData = GetWebData(specifics.baseUrl, specifics.cc);
                CookieCollection ccol = specifics.cc.GetCookies(new Uri(specifics.baseUrl.Insert(7, "tmp.") + '/'));
                CookieContainer newcc = new CookieContainer();
                foreach (Cookie cook in ccol)
                    newcc.Add(cook);

                Regex regex_SubCatDepth0 = Specifics.getRegex(@"<a(?:(?!href).)*href=""(?<url>[^""]+)""[^>]*>(?<title>[^<]+)<");
                webData = GetSubString(webData, specifics.startParse, @"</div>");

                parentCategory.SubCategories = new List<Category>();
                Match m = regex_SubCatDepth0.Match(webData);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Url = specifics.baseUrl + m.Groups["url"].Value;
                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim());
                    cat.ParentCategory = parentCategory;
                    cat.HasSubCategories = specifics.startParse.IndexOf("nav_letter") >= 0;
                    cat.Other = new UitzendingGemistSpecifics(newcc, String.Empty, 1);
                    parentCategory.SubCategories.Add(cat);
                    m = m.NextMatch();
                }
                parentCategory.SubCategoriesDiscovered = true;
                return parentCategory.SubCategories.Count;
            }
            else
            {
                // get series cats
                parentCategory.SubCategories = new List<Category>();
                Regex regex_SubCatDepth1 = Specifics.getRegex(@"<div\sstyle=""overflow[^<]*<a(?:(?!href).)*href=""(?<url>[^""]+)""[^>]*>(?<title>[^<]+)<");

                bool hasNextPage = false;
                int pageNr = 0;
                do
                {
                    pageNr++;
                    string webData = GetWebData(pageNr == 1 ? parentCategory.Url : parentCategory.Url + @"&pgNum=" + pageNr.ToString(), specifics.cc);
                    hasNextPage = webData.Contains(@"populair_bottom_volgende");
                    Match m = regex_SubCatDepth1.Match(webData);
                    while (m.Success)
                    {
                        RssLink cat = new RssLink();
                        cat.Url = specifics.baseUrl + m.Groups["url"].Value;
                        cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim());
                        cat.ParentCategory = parentCategory;
                        cat.Other = new UitzendingGemistSpecifics(specifics.cc, String.Empty, -1);
                        parentCategory.SubCategories.Add(cat);
                        m = m.NextMatch();
                    }
                } while (hasNextPage);

                parentCategory.SubCategoriesDiscovered = true;
                return parentCategory.SubCategories.Count;
            }
        }

        private string getUrlForPage(string url, int pageNr)
        {
            if (pageNr > 1)
                return url.Replace("&md5=", String.Format("&pgNum={0}&md5=", pageNr.ToString()));
            else
                return url;
        }

        private List<VideoInfo> UitzendingGemistGetVideoList(RssLink category, UitzendingGemistSpecifics specifics)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            string url = HttpUtility.HtmlDecode(category.Url);
            string referer = getUrlForPage(url, specifics.pageNr - 1);
            if (specifics.pageNr >= 1)
                url = url.Replace("serie?", "serie2?");
            url = getUrlForPage(url, specifics.pageNr);

            string webData;
            webData = GetWebData(specifics.pageNr == 0 ? url : url + @"/page=" + specifics.pageNr.ToString(), specifics.cc, referer);
            if (specifics.pageNr == 0)
                specifics.hasNextPage = webData.Contains(@"alt=""meer afleveringen""");
            else
                specifics.hasNextPage = webData.Contains(@"title=""Pagina #" + (specifics.pageNr + 1).ToString() + @"""");

            if (!string.IsNullOrEmpty(webData))
            {

                Match m;
                if (specifics.depth == -1)
                {
                    if (specifics.pageNr == 0)
                        m = UitzendingGemistSpecifics.videoListPage0Regex.Match(webData);
                    else
                        m = UitzendingGemistSpecifics.videoListPage1Regex.Match(webData);
                }
                else
                    m = UitzendingGemistSpecifics.videolistOpDagRegex.Match(webData);

                while (m.Success)
                {
                    VideoInfo video = new VideoInfo();
                    video.VideoUrl = HttpUtility.HtmlDecode(m.Groups["url"].Value);
                    if (specifics.depth == -1)
                        video.VideoUrl = specifics.baseUrl + video.VideoUrl;
                    video.Title = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    string airdate = HttpUtility.HtmlDecode(m.Groups["airdate"].Value);
                    video.Description = HttpUtility.HtmlDecode(m.Groups["descr"].Value);
                    video.Other = specifics.source;

                    if (!String.IsNullOrEmpty(video.VideoUrl))
                    {
                        if (String.IsNullOrEmpty(video.Title))
                            fillFromNed(video.VideoUrl, video, specifics);
                        if (String.IsNullOrEmpty(video.Title))
                            video.Title = "Aflevering van " + airdate;
                        if (!String.IsNullOrEmpty(airdate))
                            video.Length = video.Length + '|' + Translation.Airdate + ": " + airdate;
                        videos.Add(video);
                    }
                    m = m.NextMatch();
                }
            }
            return videos;
        }

        private void fillFromNed(string url, VideoInfo video, UitzendingGemistSpecifics specifics)
        {
            try
            {
                string tmp = GetWebData(HttpUtility.HtmlDecode(video.VideoUrl), specifics.cc, url);
                Match detm = UitzendingGemistSpecifics.detailsRegex.Match(tmp);
                if (detm.Success)
                {
                    string airdate = detm.Groups["airdate"].Value;
                    if (String.IsNullOrEmpty(airdate))
                        video.Title = GetSubString(tmp, @"<b class=""btitle"">", "</b>");
                    else
                        video.Title = "Aflevering van " + airdate;

                    video.VideoUrl = detm.Groups["url"].Value;
                    string tmpdes = HttpUtility.HtmlDecode(detm.Groups["descr"].Value);
                    if (!String.IsNullOrEmpty(tmpdes))
                        video.Description = tmpdes;
                }
            }
            catch
            {
                video.VideoUrl = String.Empty;
            }
        }

        #endregion

        #region RtlGemist
        private int RtlGemistDiscoverSubCategories(RssLink parentCategory)
        {
            if (parentCategory.SubCategoriesDiscovered)
                return parentCategory.SubCategories.Count;
            RtlGemistSpecifics specifics = (RtlGemistSpecifics)parentCategory.Other;

            if (specifics.opDag) return DiscoverRtlOpDag(parentCategory);

            string url = parentCategory.Url;
            IDictionary<string, Category> subCats = new SortedDictionary<string, Category>();

            string webData = GetWebData(url);
            webData = GetSubString(webData, @"portalProgrammas", @"this.");
            if (!string.IsNullOrEmpty(webData))
            {
                Regex regex_SubCat = Specifics.getRegex(@"\[""(?<title>[^""]+)"",""(?<url>[^""]+)"",""[^""]+""\]");

                Match m = regex_SubCat.Match(webData);
                while (m.Success)
                {
                    RssLink cat = new RtlRssLink();
                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    cat.Url = m.Groups["url"].Value;
                    if (!cat.Url.StartsWith(@"http://"))
                        cat.Url = specifics.baseUrl + cat.Url;

                    cat.Other = specifics;
                    cat.ParentCategory = parentCategory;

                    if (!rtlBlackList.ContainsKey(cat.Name))
                        if (!subCats.ContainsKey(cat.Name))
                            subCats.Add(cat.Name, cat);

                    m = m.NextMatch();
                }
            }
            parentCategory.SubCategories = new List<Category>(subCats.Values);
            return parentCategory.SubCategories.Count;
        }

        private int DiscoverRtlOpDag(RssLink parentCategory)
        {
            XmlDocument doc = new XmlDocument();
            string data = GetWebData(@"http://www.rtl.nl/service/gemist/dataset_xml.xml");
            doc.LoadXml(data);
            XmlNamespaceManager nsmRequest = new XmlNamespaceManager(doc.NameTable);
            nsmRequest.AddNamespace("a", @"http://s4mns.rtl.nl/system/xmlns/s4m");
            XmlNodeList list = doc.SelectNodes(@"//a:episodes/a:episode", nsmRequest);
            SortedDictionary<string, List<VideoInfo>> cats = new SortedDictionary<string, List<VideoInfo>>();
            foreach (XmlNode node in list)
            {
                string date = node.SelectSingleNode("broadcast_start").InnerText;
                date = date.Split(' ')[0];
                VideoInfo videoInfo = GetRtlOpDagVideo(node);
                if (videoInfo != null)
                {
                    if (!cats.ContainsKey(date))
                        cats.Add(date, new List<VideoInfo>());
                    cats[date].Add(videoInfo);
                }
            }
            List<string> t = new List<string>(cats.Keys);
            t.Reverse();
            parentCategory.SubCategories = new List<Category>();
            foreach (string s in t)
            {
                RssLink cat = new RssLink();
                cat.Name = s;
                cat.HasSubCategories = false;
                cat.Other = new RtlGemistSpecifics(cats[s]);
                cat.ParentCategory = parentCategory;
                parentCategory.SubCategories.Add(cat);
            }

            return parentCategory.SubCategories.Count;
        }

        private VideoInfo GetRtlOpDagVideo(XmlNode node)
        {
            VideoInfo result = new VideoInfo();
            result.VideoUrl = node.SelectSingleNode("component_uri").InnerText;
            if (String.IsNullOrEmpty(result.VideoUrl))
                return null;
            result.VideoUrl = @"http://www.rtl.nl" + result.VideoUrl;
            result.Title = node.SelectSingleNode("name").InnerText;
            // start of imageurl from http://data.rtl.nl/_rtl-internal/js/5494466d434556a34b5be354e0a96817.js
            result.ImageUrl = @"http://data.rtl.nl/system/img/477623qchsb4rdxh6wai4ofb7/" + node.SelectSingleNode("thumbnail_id").InnerText;
            result.Description = "afl. " + node.SelectSingleNode("episode_number").InnerText + " uitgezonden:" + node.SelectSingleNode("broadcast_start").InnerText + " op " +
                node.SelectSingleNode("station").InnerText;
            result.Other = Source.RtlGemistOpDag;
            return result;
        }

        private List<VideoInfo> RtlGemistGetVideoList(RssLink category)
        {
            RtlGemistSpecifics specifics = (RtlGemistSpecifics)category.Other;
            if (specifics.opDag) return specifics.videos;

            List<VideoInfo> videos = new List<VideoInfo>();
            string url = category.Url;
            string webData = GetWebData(url);
            if (!string.IsNullOrEmpty(webData) && webData.StartsWith(@"<?xml"))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(webData);
                XmlNodeList programs = doc.SelectNodes(("//li[@class='video']"));
                string trimmed = specifics.baseUrl.TrimEnd('/');
                foreach (XmlNode program in programs)
                {
                    VideoInfo video = new VideoInfo();
                    video.Title = program.InnerText;
                    XmlNode thumb = program.Attributes["thumb"];
                    if (thumb != null)
                        video.ImageUrl = trimmed + thumb.Value;
                    int ctime = Int32.Parse(program.Attributes["ctime"].Value);
                    string airdate = DateTime.FromFileTime(10000000 * (long)ctime + 116444736000000000).ToString();
                    if (String.IsNullOrEmpty(video.Title))
                        video.Title = "Aflevering van " + airdate;
                    video.Length = '|' + Translation.Airdate + ": " + airdate;
                    video.Other = specifics.source;
                    video.VideoUrl = trimmed + program.Attributes["rel"].Value;
                    videos.Add(video);
                }
            }
            return videos;
        }



        private class RtlRssLink : RssLink
        {
            private bool hasSubCats = false;
            private static string rtlSubRegex = @"menu_prefix[^']*'(?<part1>[^']*)'(?:(?!menu_prefix).)*menu_prefix[^']*'(?<part2>[^']*)'";
            private static Regex regex_RtlSub = new Regex(rtlSubRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
            private string prefix = String.Empty;
            private bool GetSubCats()
            {

                Specifics specifics = Other as Specifics;
                SubCategories = new List<Category>();

                string url;
                SubCategoriesDiscovered = true;

                if (!Url.EndsWith(".xml"))
                {
                    url = GetRedirectedUrl(Url);
                    prefix = "http://www.rtl.nl/system/video/menu";
                    url = url.Replace("http://www.rtl.nl", prefix) + "videomenu.xml";
                    url = url.Replace("/home/", "/");
                }
                else
                    url = Url;
                if (url.Equals(String.Empty)) return false;
                XmlDocument doc = new XmlDocument();
                try
                {
                    doc.Load(url);
                }
                catch
                {
                    return false;
                }

                foreach (XmlNode node in doc.SelectNodes("//li[@class='folder']"))
                {
                    RtlRssLink cat = new RtlRssLink();
                    cat.Name = HttpUtility.HtmlDecode(node.InnerText);
                    cat.Url = prefix + node.Attributes["rel"].Value;
                    cat.prefix = prefix;
                    cat.ParentCategory = this;
                    cat.Other = Other;
                    SubCategories.Add(cat);
                }
                if (SubCategories.Count == 0)
                    return false;
                if (SubCategories.Count == 1)
                {
                    Url = ((RssLink)SubCategories[0]).Url;
                    return GetSubCats();
                }

                return true;

            }

            public override bool HasSubCategories
            {
                get
                {
                    if (!SubCategoriesDiscovered) hasSubCats = GetSubCats();
                    return hasSubCats;
                }
            }
        }

        #endregion

        #region Net5SbsVeronicaGemist
        private int Net5SbsVeronicaGemistDiscoverSubCategories(RssLink parentCategory)
        {
            Specifics specifics = (Specifics)parentCategory.Other;
            string data = GetWebData(parentCategory.Url);
            parentCategory.SubCategories = new List<Category>();
            Regex regex_SubCat = Specifics.getRegex(@"<li\sclass=""[^""]*""><span\sclass=""title""><a\s*href=""(?<url>[^""]*)""\sclass=""navigation""\stitle=""[^""]*"">(?<title>[^<]*)(?:<sup>[^<]*</sup>)?</a></span></li>");
            Match m = regex_SubCat.Match(data);
            while (m.Success)
            {
                RssLink cat = new RssLink();
                cat.Url = specifics.baseUrl + m.Groups["url"].Value;
                cat.Name = m.Groups["title"].Value.Trim();
                cat.ParentCategory = parentCategory;
                cat.Other = specifics;
                parentCategory.SubCategories.Add(cat);
                m = m.NextMatch();
            }
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        private List<VideoInfo> Net5SbsVeronicaGemistGetVideoList(RssLink category)
        {
            Specifics specifics = (Specifics)category.Other;
            List<VideoInfo> videos = new List<VideoInfo>();
            string url = category.Url;
            if (specifics.source == Source.VeronicaGemist)
                url = GetRedirectedUrl(url);

            bool hasNextPage = false;
            Regex regex_VideoList = Specifics.getRegex(@"<div\sclass=""item[^>]*>\s*<div\sclass=""thumb""><a\s*href=""(?<VideoUrl>[^""]*)""[^<]*<img\ssrc=""(?<ImageUrl>[^""]*)""[^<]*</a></div>\s*<div\sclass=""title""><a[^<]*<span>(?<Title>[^<]*)</span></a></div>\s*<div\sclass=""airtime""><a\s*href=""[^>]*><span>(?<Airdate>[^<]*)</span>");

            int pageNr = 0;
            do
            {
                pageNr++;
                string webData;
                webData = GetWebData(pageNr == 1 ? url : url + @"/page=" + pageNr.ToString());
                hasNextPage = webData.Contains(@"class=""next""");
                if (!string.IsNullOrEmpty(webData))
                {
                    Match m = regex_VideoList.Match(webData);
                    while (m.Success)
                    {
                        VideoInfo video = new VideoInfo();
                        video.Title = HttpUtility.HtmlDecode(m.Groups["Title"].Value);
                        video.ImageUrl = m.Groups["ImageUrl"].Value;
                        if (!String.IsNullOrEmpty(video.ImageUrl))
                            video.ImageUrl = specifics.baseUrl + video.ImageUrl;
                        video.VideoUrl = specifics.baseUrl + m.Groups["VideoUrl"].Value;
                        string airdate = HttpUtility.HtmlDecode(m.Groups["Airdate"].Value);
                        video.Other = specifics.source;

                        if (!String.IsNullOrEmpty(video.VideoUrl))
                        {
                            video.Length = video.Length + '|' + Translation.Airdate + ": " + airdate;
                            videos.Add(video);
                        }
                        m = m.NextMatch();
                    }
                }
            }
            while (hasNextPage);

            return videos;
        }

        #endregion


        private void AddSubcats(string[] names, RssLink parentCat)
        {
            parentCat.SubCategories = new List<Category>();

            foreach (string name in names)
            {
                RssLink cat = new RssLink();
                cat.Name = name;
                cat.HasSubCategories = true;
                cat.Url = parentCat.Url;
                cat.ParentCategory = parentCat;
                parentCat.SubCategories.Add(cat);
            }
            parentCat.SubCategoriesDiscovered = true;
        }


        #region Specifics
        private class Specifics
        {
            public Specifics(Source source, string baseUrl)
            {
                this.source = source;
                this.baseUrl = baseUrl;
            }

            public Source source;
            public string baseUrl;

            public static Regex getRegex(string s)
            {
                return new Regex(s, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
            }
        }

        private class UitzendingGemistSpecifics : Specifics
        {
            public UitzendingGemistSpecifics(CookieContainer cc, string startParse, int depth)
                : base(Source.UitzendingGemist, @"http://www.uitzendinggemist.nl")
            {
                this.cc = cc;
                this.startParse = startParse;
                this.depth = depth;
            }
            public CookieContainer cc;
            public string startParse;
            public int depth;
            public bool hasNextPage;
            public int pageNr;
            public static Regex videoListPage0Regex = Specifics.getRegex(@"<tr\sclass=""bg(odd|even)[^<]*<[^>]*>\s*(?<airdate>[^<]*)<[^>]*>[^>]*>\s*(?<descr>[^\(]*)\(<a\shref=""(?<url>[^""]*)""");
            public static Regex videoListPage1Regex = Specifics.getRegex(@"<tr\sclass=""bg(odd|even)[^>]*>[^>]*>\s*(?<airdate>[^<]*)<(?:(?!href).)*href=""(?<url>[^""]*)"">(?<descr>[^<]*)<");
            public static Regex detailsRegex = Specifics.getRegex(@"(<p>Datum\suitzending[^>]*>(?<airdate>[^<]*).*?)?(Deze\saflevering:(?<descr>[^<]*).*?)?<a\shref=""(?<url>[^""]*)""\s*target=""player""");
            public static Regex videolistOpDagRegex = Specifics.getRegex(@"<div\sstyle=""overflow[^<]*<a(?:(?!href).)*href=""(?<nourl>[^""]+)""[^>]*>(?<title>[^<]+)<(?:(?!<td).)*<td[^>]*>(?<airdate>[^<]*)<(?:(?!http://player.omroep.nl).)*(?<url>[^""]*)""");
        }

        private class RtlGemistSpecifics : Specifics
        {
            public RtlGemistSpecifics(bool opDag)
                : base(Source.RtlGemist, @"http://www.rtl.nl/")
            {
                this.opDag = opDag;
            }

            public RtlGemistSpecifics(List<VideoInfo> videos)
                : base(Source.RtlGemist, @"http://www.rtl.nl/")
            {
                this.opDag = true;
                this.videos = videos;
            }
            public bool opDag;
            public List<VideoInfo> videos; // only valid for opdag
        }
        #endregion

        private static string GetSubString(string s, string start, string until)
        {
            int p = s.IndexOf(start);
            if (p == -1) return String.Empty;
            p += start.Length;
            int q = s.IndexOf(until, p);
            if (q == -1) return s.Substring(p);
            return s.Substring(p, q - p);
        }
    }

}

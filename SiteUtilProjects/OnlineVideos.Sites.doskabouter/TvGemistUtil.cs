using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Net;
using MediaPortal.Configuration;
using System.ComponentModel;

namespace OnlineVideos.Sites
{
    public class TvGemistUtil : SiteUtilBase
    {
        [Category("OnlineVideosConfiguration"), Description("Boolean used for testing rtlgemist")]
        protected bool experimental;

        private string subCategoryRegexSbsVeronica = @"<span\s*class=""title"">\s*<a\s*href=""(?<url>[^""]+)""[^>]*>(?<title>[^<]+)<";
        private string videoListRegexNet5SbsVeronica = @"<a\s*href=""(?<url>[^""]+)"".*?<img\s*src=""(?<thumb>[^""]+)"".*?<span>(?<episode>[^<]*)<.*?<span>(?<airdate>[^<]*)<.*?<span>(?<descr>[^<]*)<";
        private SortedList<string, object> rtlBlackList;
        private Regex regex_NedSub;
        private Regex regex_NedVidList;
        private Regex regex_NedDetails;
        private Regex regex_RtlDetails;
        private enum Source { UitzendingGemist = 0, RtlGemist = 1, Veronica = 2, SBS = 3, Rest = 4 };
        private enum Misc { RtlOpDagUrl };

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
            Specifics specifics;

            cat = new RssLink();
            cat.Name = "Uitzending Gemist";
            cat.Url = @"http://www.uitzendinggemist.nl/";
            cat.Thumb = Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\Icons\Tvgemist\uitzendinggemist.png";
            cat.HasSubCategories = true;
            regex_NedSub = Specifics.getRegex(@"style=""overflow.*?<a\s.*?href=""(?<url>[^""]+)""[^>]*>(?<title>[^<]+)<.*?<td[^>]*>(?<airdate>[^<]*)<");
            regex_NedVidList = Specifics.getRegex(@"<tr.*?height[^>]*>(?<airdate>[^<]*)<.*?href=""(?<url>[^""]*)"">(?<descr>[^<]+)<.*?</span>.*?href=""(?<vidurl>[^""]+)""");
            regex_NedDetails = Specifics.getRegex(@"(<p>Datum\suitzending[^>]*>(?<airdate>[^<]*).*?)?(Deze\saflevering:(?<descr>[^<]*).*?)?<a\shref=""(?<url>[^""]*)""\s*target=""player""");

            Settings.Categories.Add(cat);
            Add2Subcats("Op alfabet", "Op dag", cat);
            CookieContainer cc = new CookieContainer();
            cat.SubCategories[0].Other = GetNlSpecifics(@"<div id=""nav_letter"">", cc);
            cat.SubCategories[1].Other = GetNlSpecifics(@"<div id=""nav_dag"">", cc);
            ((Specifics)cat.SubCategories[1].Other).doSort = false;
            ((Specifics)cat.SubCategories[1].Other).isDay = true;
            ((Specifics)cat.SubCategories[1].Other).videoListStart = @"bekijk</td>";
            ((Specifics)cat.SubCategories[1].Other).regex_VideoList = Specifics.getRegex(@"<tr\sclass.*?class=""title""[^>]*>(?<title>[^<]*)<.*?<td\salign[^>]*>(?<airdate>[^<]*)<.*?<a\shref=""(?<url>[^""]*)""");

            cat = new RssLink();
            cat.Name = "Rtl Gemist";
            cat.Url = @"http://rtl.nl/experience/rtlnl/";
            cat.Thumb = Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\Icons\Tvgemist\rtlgemist.png";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            Add2Subcats("Op alfabet", "Op dag", cat);

            specifics = new Specifics(Source.RtlGemist);
            specifics.baseUrl = @"http://www.rtl.nl/";
            specifics.subCatStart = @"portalProgrammas";
            specifics.subCatEnd = @"this.";
            specifics.regex_SubCat = Specifics.getRegex(@"\[""(?<title>[^""]+)"",""(?<url>[^""]+)"",""[^""]+""\]");

            specifics.videoListStart = String.Empty;
            specifics.regex_VideoList = Specifics.getRegex(@"<li\sclass=""video""\s*(thumb=""(?<thumb>[^""]+)"")?.*?rel=""(?<url>[^""]+)""[^>]*>(?<title>[^<]+)<");
            regex_RtlDetails = Specifics.getRegex(@"bandwidth:\s*'(?<bandwidth>[^']*).*?file:\s*'(?<url>[^']*)");
            cat.SubCategories[0].Other = specifics;
            cat.SubCategories[1].Other = Source.RtlGemist;

            cat = new RssLink();
            cat.Name = "Net5 Gemist";
            cat.Url = @"http://www.net5.nl/web/show/id=95681/langid=43";
            cat.Thumb = Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\Icons\Tvgemist\net5gemist.png";
            cat.HasSubCategories = true;
            specifics = new Specifics(Source.Rest);
            specifics.baseUrl = @"http://www.net5.nl";
            specifics.subCatStart = @"class=""mo-a";
            specifics.subCatEnd = @"class=""clearer""";
            specifics.regex_SubCat = Specifics.getRegex(@"<a\s*href=""(?<url>[^""]+)""[^>]*>(?<title>[^<]+)<");

            specifics.videoListStart = @"class=""mo-c double""";
            specifics.regex_VideoList = Specifics.getRegex(videoListRegexNet5SbsVeronica);

            cat.Other = specifics;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Name = "SBS6 Gemist";
            cat.Url = @"http://www.sbs6.nl/web/show/id=73863/langid=43";
            cat.Thumb = Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\Icons\Tvgemist\sbsgemist.png";
            cat.HasSubCategories = true;
            specifics = new Specifics(Source.SBS);
            specifics.baseUrl = @"http://www.sbs6.nl";
            specifics.subCatStart = @"<span>Programma gemist overzicht";
            specifics.subCatEnd = @"class=""bottom""";
            specifics.regex_SubCat = Specifics.getRegex(subCategoryRegexSbsVeronica);

            specifics.videoListStart = @"class=""mo-c double""";
            specifics.regex_VideoList = Specifics.getRegex(videoListRegexNet5SbsVeronica);

            cat.Other = specifics;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Name = "Veronica Gemist";
            cat.Url = @"http://www.veronicatv.nl/web/show/id=96520/langid=43";
            cat.Thumb = Config.GetFolder(Config.Dir.Thumbs) + @"\OnlineVideos\Icons\Tvgemist\veronicagemist.png";
            cat.HasSubCategories = true;
            specifics = new Specifics(Source.Veronica);
            specifics.baseUrl = @"http://www.veronicatv.nl";
            specifics.subCatStart = @"class=""mo-a"; // was: mo-b
            specifics.subCatEnd = @"class=""bottom""";
            specifics.regex_SubCat = Specifics.getRegex(subCategoryRegexSbsVeronica);

            specifics.videoListStart = @"class=""mo-c double""";
            specifics.regex_VideoList = Specifics.getRegex(videoListRegexNet5SbsVeronica);

            cat.Other = specifics;
            Settings.Categories.Add(cat);

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        private Specifics GetNlSpecifics(string subCatStart, CookieContainer cc)
        {
            Specifics specifics = new pagedTest(Source.UitzendingGemist);
            specifics.baseUrl = @"http://www.uitzendinggemist.nl";
            specifics.subCatStart = subCatStart;
            specifics.subCatEnd = @"</div>";
            specifics.regex_SubCat = Specifics.getRegex(@"<a\s.*?href=""(?<url>[^""]+)""[^>]*>(?<title>[^<]+)<");

            specifics.videoListStart = @"<tbody id=""afleveringen"">";
            specifics.regex_VideoList = Specifics.getRegex(@"<tr.*?height=[^>]*>\s*(?<airdate>[^<]*)<.*?onclick=""document[^>]*>\s*(?<descr>.*?)\(<a\shref=""(?<url>[^""]+)""");
            specifics.cc = cc;
            return specifics;
        }

        private Specifics GetSbsExtraSpecifics()
        {
            Specifics specifics = new Specifics(Source.SBS);
            specifics.baseUrl = String.Empty;
            specifics.subCatStart = null;
            specifics.subCatEnd = null;
            specifics.regex_SubCat = null;

            specifics.videoListStart = @"class=""mo-c"">";
            specifics.regex_VideoList = Specifics.getRegex(@"<div\sclass=""item\s*[^\s]*\s*archief.*?<a\shref=""(?<url>[^""]*).*?<img\ssrc=""(?<thumb>[^""]*).*?<div\sclass=""airtime""><span>(?<airdate>[^<]*).*?<div\sclass=""title""><span>[^>]*>(?<title>[^<]*).*?<div\sclass=""text""><span>[^>]*>(?<desrc>[^<]*)");
            return specifics;
        }

        private void Add2Subcats(string cat1, string cat2, RssLink parentCat)
        {
            parentCat.SubCategories = new List<Category>();

            RssLink cat = new RssLink();
            cat.Name = cat1;
            cat.HasSubCategories = true;
            cat.Url = parentCat.Url;
            cat.ParentCategory = parentCat;
            parentCat.SubCategories.Add(cat);

            cat = new RssLink();
            cat.Name = cat2;
            cat.HasSubCategories = true;
            cat.Url = parentCat.Url;
            cat.ParentCategory = parentCat;
            parentCat.SubCategories.Add(cat);

            parentCat.SubCategoriesDiscovered = true;
        }

        private int DiscoverRtlSubCategories(RssLink parentCategory, Specifics bareSpecifics)
        {
            return parentCategory.SubCategories.Count;
        }

        private int DiscoverBareSubCategories(RssLink parentCategory, Specifics specifics)
        {
            string url = parentCategory.Url;
            IDictionary<string, Category> subCats;

            if (specifics.doSort)
                subCats = new SortedDictionary<string, Category>();
            else
                subCats = new Dictionary<string, Category>();

            bool hasNextPage = false;
            int pageNr = 0;
            do
            {
                pageNr++;
                string webData = GetWebData(pageNr == 1 ? url : url + @"/page=" + pageNr.ToString(), specifics.cc);
                if (specifics.cc != null)
                {
                    CookieCollection ccol = specifics.cc.GetCookies(new Uri(specifics.baseUrl.Insert(7, "tmp.") + '/'));
                    CookieContainer newcc = new CookieContainer();
                    foreach (Cookie cook in ccol)
                        newcc.Add(cook);
                    specifics.cc = newcc;
                }

                hasNextPage = webData.Contains(@"class=""next""");
                webData = GetSubString(webData, specifics.subCatStart, specifics.subCatEnd);


                if (!string.IsNullOrEmpty(webData))
                {

                    Match m = specifics.regex_SubCat.Match(webData);
                    while (m.Success)
                    {
                        RssLink cat;
                        if (specifics.source == Source.RtlGemist)
                            cat = new RtlRssLink();
                        else
                            cat = new RssLink();

                        cat.Other = parentCategory.Other;

                        cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                        cat.Url = m.Groups["url"].Value;
                        if (!cat.Url.StartsWith(@"http://"))
                            cat.Url = specifics.baseUrl + cat.Url;
                        else
                            if (specifics.source == Source.SBS)
                            {
                                cat.Url = cat.Url + "archief/";
                                cat.Other = GetSbsExtraSpecifics();
                            }

                        cat.Thumb = m.Groups["thumb"].Value;
                        if (cat.Thumb != String.Empty) cat.Thumb = specifics.baseUrl + cat.Thumb;

                        cat.HasSubCategories = (specifics.source == Source.UitzendingGemist && !specifics.isDay);
                        cat.ParentCategory = parentCategory;

                        if (specifics.source != Source.RtlGemist || !rtlBlackList.ContainsKey(cat.Name))
                            if (!subCats.ContainsKey(cat.Name))
                                subCats.Add(cat.Name, cat);

                        m = m.NextMatch();
                    }
                }
            } while (hasNextPage);

            parentCategory.SubCategories = new List<Category>(subCats.Values);
            return parentCategory.SubCategories.Count;
        }

        private int DiscoverNEDSubCategories(RssLink parentCategory, Specifics specifics)
        {
            string url = parentCategory.Url;
            parentCategory.SubCategories = new List<Category>();

            bool hasNextPage = false;
            int pageNr = 0;
            do
            {
                pageNr++;
                string webData = GetWebData(pageNr == 1 ? url : url + @"&sort=&pgNum=" + pageNr.ToString(), specifics.cc);

                hasNextPage = webData.Contains(@"populair_bottom_volgende");
                webData = GetSubString(webData, @"<thead id=""tooltip_selectie"">", "</html>");

                if (!string.IsNullOrEmpty(webData))
                {
                    Match m = regex_NedSub.Match(webData);
                    while (m.Success)
                    {
                        RssLink cat = new RssLink();
                        cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                        cat.Url = specifics.baseUrl + HttpUtility.HtmlDecode(m.Groups["url"].Value);
                        cat.HasSubCategories = false;
                        cat.Other = parentCategory.Other;
                        cat.ParentCategory = parentCategory;
                        parentCategory.SubCategories.Add(cat);
                        m = m.NextMatch();
                    }
                }
            } while (hasNextPage);

            return parentCategory.SubCategories.Count;
        }

        private VideoInfo GetRtlOpDagVideo(XmlNode node)
        {
            VideoInfo result = new VideoInfo();
            result.VideoUrl = node.SelectSingleNode("component_uri").InnerText;
            if (String.IsNullOrEmpty(result.VideoUrl))
                return null;
            result.VideoUrl = @"http://data.rtl.nl" + result.VideoUrl;
            result.Title = node.SelectSingleNode("name").InnerText;
            // start of imageurl from http://data.rtl.nl/_rtl-internal/js/5494466d434556a34b5be354e0a96817.js
            result.ImageUrl = @"http://data.rtl.nl/system/img/477623qchsb4rdxh6wai4ofb7/" + node.SelectSingleNode("thumbnail_id").InnerText;
            result.Description = "afl. " + node.SelectSingleNode("episode_number").InnerText + " uitgezonden:" + node.SelectSingleNode("broadcast_start").InnerText + " op " +
                node.SelectSingleNode("station").InnerText;
            result.Other = Misc.RtlOpDagUrl;
            return result;
        }

        private int DiscoverRtlOpDag(RssLink parentCategory)
        {
            XmlDocument doc = new XmlDocument();
            string data = GetWebData(@"http://www.rtl.nl/service/gemist/dataset_xml.xml");
            doc.LoadXml(data);
            XmlNamespaceManager nsmRequest = new XmlNamespaceManager(doc.NameTable);
            nsmRequest.AddNamespace("a", @"http://interactief.rtl.nl/system/xmlns/s4m");
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
                cat.Other = cats[s];
                cat.ParentCategory = parentCategory;
                parentCategory.SubCategories.Add(cat);
            }

            return parentCategory.SubCategories.Count;
        }


        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategoriesDiscovered = true;
            if (parentCategory.Other == null) return parentCategory.SubCategories.Count;
            if (parentCategory.Other.Equals(Source.RtlGemist))
                return DiscoverRtlOpDag((RssLink)parentCategory);
            Specifics specifics = parentCategory.Other as Specifics;
            if (parentCategory.ParentCategory == null || parentCategory.ParentCategory.Other == null)
                return DiscoverBareSubCategories((RssLink)parentCategory, specifics);
            else
                switch (specifics.source)
                {
                    case Source.RtlGemist: return DiscoverRtlSubCategories((RssLink)parentCategory, specifics);
                    case Source.UitzendingGemist: return DiscoverNEDSubCategories((RssLink)parentCategory, specifics);
                    default:
                        return DiscoverBareSubCategories((RssLink)parentCategory, specifics);
                }
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            baseCategory = (RssLink)category;
            Specifics specifics = category.Other as Specifics;
            if (specifics is pagedTest && ((pagedTest)specifics).pageNr != 1)
                return getNedVideoList(baseCategory, ((pagedTest)specifics));
            else
            {
                List<VideoInfo> test = category.Other as List<VideoInfo>;
                if (test != null)
                    return test;
                return getBareVideoList(baseCategory, specifics);
            }
        }

        public override bool HasNextPage
        {
            get
            {
                if (baseCategory.Other is pagedTest)
                    return ((pagedTest)baseCategory.Other).hasNextPage;
                else
                    return false;
            }
        }

        public override bool HasPreviousPage
        {
            get
            {
                if (baseCategory.Other is pagedTest)
                    return ((pagedTest)baseCategory.Other).pageNr > 1;
                else
                    return false;
            }
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            pagedTest specifics = ((pagedTest)baseCategory.Other);
            specifics.pageNr++;
            return getNedVideoList(baseCategory, specifics);
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            pagedTest specifics = ((pagedTest)baseCategory.Other);
            specifics.pageNr--;
            if (specifics.pageNr > 2)
                return getNedVideoList(baseCategory, specifics);
            else
                return getBareVideoList(baseCategory, specifics);
        }

        private string getUrlForPage(string url, int pageNr)
        {
            if (pageNr > 2)
                return url.Replace("&md5=", String.Format("&pgNum={0}&md5=", (pageNr - 1).ToString()));
            else
                return url;
        }

        private List<VideoInfo> getNedVideoList(RssLink category, pagedTest specifics)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            string url = category.Url.Replace("serie?", "serie2?");
            string referer = getUrlForPage(url, specifics.pageNr - 1);
            url = getUrlForPage(url, specifics.pageNr);

            string webData = GetWebData(url, specifics.cc, referer);
            specifics.hasNextPage = webData.Contains(@"title=""Pagina #" + specifics.pageNr.ToString() + @"""");
            webData = GetSubString(webData, specifics.videoListStart, @"class=""pages""");
            if (!string.IsNullOrEmpty(webData))
            {
                Match m = regex_NedVidList.Match(webData);
                while (m.Success)
                {
                    VideoInfo video = new VideoInfo();
                    video.VideoUrl = specifics.baseUrl + m.Groups["url"].Value;
                    video.Description = HttpUtility.HtmlDecode(m.Groups["descr"].Value);
                    video.Other = Source.UitzendingGemist;

                    fillFromNed(url, video, specifics);

                    if (!String.IsNullOrEmpty(video.VideoUrl))
                        videos.Add(video);
                    m = m.NextMatch();
                }
            }

            return videos;
        }

        private void fillFromNed(string url, VideoInfo video, Specifics specifics)
        {
            try
            {
                string tmp = GetWebData(HttpUtility.HtmlDecode(video.VideoUrl), specifics.cc, url);
                Match detm = regex_NedDetails.Match(tmp);
                if (detm.Success)
                {
                    string airdate = detm.Groups["airdate"].Value;
                    if (String.IsNullOrEmpty(airdate))
                        video.Title = GetSubString(tmp, @"<b class=""btitle"">", "</b>");
                    else
                        video.Title = "Aflevering van " + airdate;

                    video.VideoUrl = detm.Groups["url"].Value;
                    video.Description = HttpUtility.HtmlDecode(detm.Groups["descr"].Value);
                }
            }
            catch
            {
                video.VideoUrl = String.Empty;
            }
        }

        private void SetRtlUrl(VideoInfo video, ref string airdate)
        {
            try
            {
                string tmp = GetWebData(video.VideoUrl);
                airdate = GetSubString(tmp, @"date:'", "'");
                if (String.IsNullOrEmpty(airdate))
                    airdate = GetSubString(tmp, @"date: '", "'");
                Match detm = regex_RtlDetails.Match(tmp);
                int highest = 0;
                while (detm.Success)
                {
                    int bw;
                    if (!int.TryParse(detm.Groups["bandwidth"].Value, out bw))
                        bw = 0;
                    if (bw >= highest)
                    {
                        video.VideoUrl = detm.Groups["url"].Value;
                        highest = bw;
                    }
                    detm = detm.NextMatch();
                }
            }
            catch
            {
                //Console.WriteLine(" no video found at " + video.VideoUrl);
                video.VideoUrl = String.Empty;
            }
            if (experimental)
            {
                string exp = video.VideoUrl.Replace("http://av.rtl.nl/web/", "http://www.rtl.nl/system/video/wvx/");
                int ind = exp.LastIndexOf('/');
                ind = exp.LastIndexOf('/', ind - 1);
                exp = exp.Insert(ind, "/miMedia");
                exp = exp.Replace(".MiMedia_WM_1500K_V9.wmv", ".xml/1500.wvx");

                // transform 
                // http://av.rtl.nl/web/components/soaps/gtst/203350/203939.s4m.29707557.Goede_Tijden_Slechte_Tijden_s24_a4020.MiMedia_WM_1500K_V9.wmv
                // into
                // http://www.rtl.nl/system/video/wvx/components/soaps/gtst/miMedia/203350/203939.s4m.29707557.Goede_Tijden_Slechte_Tijden_s24_a4020.xml/1500.wvx

                video.VideoUrl = exp;
            }
        }

        private List<VideoInfo> getBareVideoList(RssLink category, Specifics specifics)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            string url = category.Url;

            bool hasNextPage = false;

            if (specifics.source == Source.Veronica)
                url = GetRedirectedUrl(url);
            int pageNr = 0;
            do
            {
                pageNr++;
                string webData;
                //Log.Info("getting page {0} of category {1}", pageNr, category.Name);
                if (specifics.source == Source.SBS && String.IsNullOrEmpty(specifics.baseUrl))
                    webData = GetWebData(pageNr == 1 ? url : url + @"page/" + pageNr.ToString() + "/", specifics.cc);
                else
                    webData = GetWebData(pageNr == 1 ? url : url + @"/page=" + pageNr.ToString(), specifics.cc);
                switch (specifics.source)
                {
                    case Source.UitzendingGemist:
                        {
                            ((pagedTest)specifics).hasNextPage = webData.Contains(@"alt=""meer afleveringen""");
                            hasNextPage = false;
                            break;
                        }
                    case Source.SBS: hasNextPage = webData.Contains(@">Ouder<"); break;
                    default:
                        hasNextPage = webData.Contains(@"class=""next"""); break;
                }
                webData = GetSubString(webData, specifics.videoListStart, @"class=""pages""");
                if (!string.IsNullOrEmpty(webData))
                {
                    Match m = specifics.regex_VideoList.Match(webData);
                    while (m.Success)
                    {
                        VideoInfo video = new VideoInfo();
                        video.Title = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                        video.ImageUrl = m.Groups["thumb"].Value;
                        if (!String.IsNullOrEmpty(video.ImageUrl))
                            video.ImageUrl = specifics.baseUrl + video.ImageUrl;
                        if (specifics.isDay)
                            video.VideoUrl = m.Groups["url"].Value;
                        else
                            video.VideoUrl = specifics.baseUrl + m.Groups["url"].Value;
                        string airdate = HttpUtility.HtmlDecode(m.Groups["airdate"].Value);
                        video.Description = HttpUtility.HtmlDecode(m.Groups["descr"].Value);
                        video.Other = specifics.source;
                        if (specifics.source == Source.UitzendingGemist && !specifics.isDay)
                        {
                            fillFromNed(url, video, specifics);

                        }

                        if (specifics.source == Source.RtlGemist)
                            SetRtlUrl(video, ref airdate);

                        if (!String.IsNullOrEmpty(video.VideoUrl))
                        {
                            if (String.IsNullOrEmpty(video.Title))
                                video.Title = "Aflevering van " + airdate;
                            else
                                video.Description = video.Description + " " + airdate;
                            videos.Add(video);
                        }
                        m = m.NextMatch();
                    }
                }
            }
            while (hasNextPage);

            return videos;
        }

        public override string getUrl(VideoInfo video)
        {

            string s = video.Other as string;
            if (s != null)
            {
                if (Enum.IsDefined(typeof(Misc), s))
                    video.Other = Enum.Parse(typeof(Misc), s, true);
                else
                    if (Enum.IsDefined(typeof(Source), s))
                        video.Other = Enum.Parse(typeof(Source), s, true);
                    else
                        video.Other = null;
            }

            if (Misc.RtlOpDagUrl.Equals(video.Other))
            {
                string dummy = null;
                SetRtlUrl(video, ref dummy);
                return video.VideoUrl;
            }
            if (Source.RtlGemist.Equals(video.Other))
            {
                return video.VideoUrl;
            }
            if (Source.UitzendingGemist.Equals(video.Other))
                return UrlTricks.PlayerOmroepTrick(video.VideoUrl);

            string webData = GetWebData(video.VideoUrl);
            string url = GetSubString(webData, @"class=""wmv-player-holder"" href=""", @"""");
            if (!String.IsNullOrEmpty(url))
                return url;
            url = GetSubString(webData, @"file=", @"""");
            if (!String.IsNullOrEmpty(url))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(GetWebData(url));
                XmlNamespaceManager nsmRequest = new XmlNamespaceManager(doc.NameTable);
                nsmRequest.AddNamespace("a", "http://xspf.org/ns/0/");

                XmlNode node = doc.SelectSingleNode(@"//a:location", nsmRequest);
                if (node != null)
                    url = node.InnerText;
                if (url.StartsWith(@"rtmp://"))
                {
                    node = doc.SelectSingleNode(@"//a:identifier", nsmRequest);

                    return ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                        string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&swfurl={1}",                        
                            System.Web.HttpUtility.UrlEncode(url + node.InnerText + ".flv"),
                            @"http://www.veronicatv.nl/design/channel/veronicatv/swf/mediaplayer.swf"));
                    //vb: hotshots op veronica: this is not working, connection closed by server
                }
                if (!String.IsNullOrEmpty(url))
                    return url;
                else
                    return video.VideoUrl;
            }
            return video.VideoUrl;

        }

        public override string GetFileNameForDownload(VideoInfo video, Category category, string url)
        {
            return category.Name + " " + video.Title;
        }

        private static string GetSubString(string s, string start, string until)
        {
            int p = s.IndexOf(start);
            if (p == -1) return String.Empty;
            p += start.Length;
            int q = s.IndexOf(until, p);
            if (q == -1) return s.Substring(p);
            return s.Substring(p, q - p);
        }

        private class Specifics
        {
            public Specifics(Source source)
            {
                this.source = source;
            }

            public static Regex getRegex(string s)
            {
                return new Regex(s, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
            }

            public Regex regex_SubCat;
            public Regex regex_VideoList;
            public string baseUrl;
            public string subCatStart;
            public string subCatEnd;
            public string videoListStart;
            public CookieContainer cc = null;
            public Source source;
            public bool doSort = true;
            public bool isDay = false;
        }

        private class pagedTest : Specifics
        {
            public pagedTest(Source source) : base(source) { }
            public bool hasNextPage;
            public int pageNr = 1;
        }

        private class RtlRssLink : RssLink
        {
            private bool hasSubCats = false;
            private static string rtlSubRegex = @"menu_prefix[^']*'(?<part1>[^']*)'.*?menu_prefix[^']*'(?<part2>[^']*)'";
            private static Regex regex_RtlSub = new Regex(rtlSubRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
            private string prefix = String.Empty;
            private bool GetSubCats()
            {

                Specifics specifics = Other as Specifics;

                string url = String.Empty;
                SubCategoriesDiscovered = true;

                if (!Url.EndsWith(".xml"))
                {
                    string extra;
                    string trimmed = specifics.baseUrl.TrimEnd('/');
                    if (Url.Equals("http://www.rtl.nl/shows/hoewordt2010/"))
                        extra = @"""http://www.rtl.nl/components/shows/hoewordt2010/index_video.xml""";
                    else
                        if (Url.Equals("http://www.rtl.nl/huistuinkeuken/thetasteoflife/"))
                            extra = String.Empty;
                        else
                            extra = GetWebData(Url);


                    int p = extra.IndexOf(@"index_video.xml""");
                    if (p == -1)
                    {
                        string tmp4 = GetSubString(extra, @"src=""", @"""");
                        if (tmp4.StartsWith(@"http://"))
                        {
                            extra = GetWebData(tmp4);
                            p = extra.IndexOf(@"index_video.xml""");
                        }
                        else
                        {
                            prefix = "http://www.rtl.nl/system/video/menu";
                            url = Url.Replace("http://www.rtl.nl", prefix) + "videomenu.xml";
                            url = url.Replace("/home/", "/");
                        }

                    }
                    if (p != -1)
                    {
                        int q = extra.LastIndexOf('"', p);
                        if (q != -1)
                        {
                            url = extra.Substring(q + 1, p - q + 14);
                            if (!url.StartsWith(@"http://"))
                                url = trimmed + url;
                            string vidxml = GetWebData(url);
                            Match m = regex_RtlSub.Match(vidxml);
                            if (m.Success)
                            {
                                prefix = trimmed + m.Groups["part1"].Value;
                                string tmp = prefix + m.Groups["part2"].Value;
                                url = tmp;
                            }
                        }
                    }
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

                SortedDictionary<string, Category> subCats = new SortedDictionary<string, Category>();
                foreach (XmlNode node in doc.SelectNodes("//li[@class='folder']"))
                {
                    RtlRssLink cat = new RtlRssLink();
                    cat.Name = HttpUtility.HtmlDecode(node.InnerText);
                    cat.Url = prefix + node.Attributes["rel"].Value;
                    cat.prefix = prefix;
                    cat.ParentCategory = this;
                    cat.Other = Other;
                    if (!subCats.ContainsKey(cat.Name))
                        subCats.Add(cat.Name, cat);
                }
                if (subCats.Count == 0)
                    return false;
                if (subCats.Count == 1)
                {
                    foreach (Category tmp in subCats.Values)
                        Url = ((RssLink)tmp).Url;
                    return GetSubCats();
                }

                SubCategories = new List<Category>(subCats.Values);
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

    }
}

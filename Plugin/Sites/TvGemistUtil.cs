using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using MediaPortal.GUI.Library;

namespace OnlineVideos.Sites
{
    public class TvGemistUtil : SiteUtilBase
    {

        private string subCategoryRegexSbsVeronica = @"<div\s*class=""thumb"">\s*<a\s*href=""(?<url>[^""]+)"".*?<img\s*src=""(?<thumb>[^""]+)"".*?<span>(?<title>[^<]+)<";
        private string videoListRegexNet5SbsVeronica = @"<a\s*href=""(?<url>[^""]+)"".*?<img\s*src=""(?<thumb>[^""]+)"".*?<span>(?<episode>[^<]*)<.*?<span>(?<airtime>[^<]*)<.*?<span>(?<descr>[^<]*)<";

        private SortedList<string, object> rtlBlackList;
        //private enum Source { UitzendingGemist = 0, RtlGemist = 1, Rest = 2 };

        public override void Initialize(OnlineVideos.SiteSettings siteSettings)
        {
            string[] lst = { "Films Bij RTL", "RTL Gemist FAQ", "RTL Gids", "RTL Tickets", "RTL Weer", "Spelsalon" };
            rtlBlackList = new SortedList<string, object>(lst.Length);
            foreach (string s in lst) rtlBlackList.Add(s, null);

            base.Initialize(siteSettings);
        }

        public override int DiscoverDynamicCategories()
        {
            RssLink cat;
            test specifics;

            cat = new RssLink();
            cat.Name = "Uitzending Gemist";
            cat.Url = @"http://www.uitzendinggemist.nl/";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Name = "Rtl Gemist";
            cat.Url = @"http://rtl.nl/experience/rtlnl/";
            cat.HasSubCategories = true;
            specifics = new test();
            specifics.baseUrl = @"http://www.rtl.nl/";
            specifics.subCatStart = @"portalProgrammas";
            specifics.subCatEnd = @"this.";
            specifics.regex_SubCat = specifics.getRegex(@"\[""(?<title>[^""]+)"",""(?<url>[^""]+)"","".""\]");

            specifics.videoListStart = String.Empty;
            specifics.regex_VideoList = specifics.getRegex(@"<li\sclass=""video""\s*(thumb=""(?<thumb>[^""]+)"")?.*?rel=""(?<url>[^""]+)""[^>]*>(?<title>[^<]+)<");
            specifics.extraGetWeb = true;

            cat.Other = specifics;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Name = "Net5 Gemist";
            cat.Url = @"http://www.net5.nl/web/show/id=95681/langid=43";
            cat.HasSubCategories = true;
            specifics = new test();
            specifics.baseUrl = @"http://www.net5.nl";
            specifics.subCatStart = @"class=""mo-a""";
            specifics.subCatEnd = @"class=""clearer""";
            specifics.regex_SubCat = specifics.getRegex(@"<a\s*href=""(?<url>[^""]+)""[^>]*>(?<title>[^<]+)<");

            specifics.videoListStart = @"class=""mo-c double""";
            specifics.regex_VideoList = specifics.getRegex(videoListRegexNet5SbsVeronica);
            cat.Other = specifics;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Name = "SBS6 Gemist";
            cat.Url = @"http://www.sbs6.nl/web/show/id=73863/langid=43";
            cat.HasSubCategories = true;
            specifics = new test();
            specifics.baseUrl = @"http://www.sbs6.nl";
            specifics.subCatStart = @"<span>Programma gemist overzicht";
            specifics.subCatEnd = @"class=""bottom""";
            specifics.regex_SubCat = specifics.getRegex(subCategoryRegexSbsVeronica);

            specifics.videoListStart = @"class=""mo-c double""";
            specifics.regex_VideoList = specifics.getRegex(videoListRegexNet5SbsVeronica);
            cat.Other = specifics;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Name = "Veronica Gemist";
            cat.Url = @"http://www.veronicatv.nl/web/show/id=96520/langid=43";
            cat.HasSubCategories = true;
            specifics = new test();
            specifics.baseUrl = @"http://www.veronicatv.nl";
            specifics.subCatStart = @"class=""mo-b""";
            specifics.subCatEnd = @"class=""bottom""";
            specifics.regex_SubCat = specifics.getRegex(subCategoryRegexSbsVeronica);

            specifics.videoListStart = @"class=""mo-c double""";
            specifics.regex_VideoList = specifics.getRegex(videoListRegexNet5SbsVeronica);
            cat.Other = specifics;
            Settings.Categories.Add(cat);

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (parentCategory is RtlRssLink) return parentCategory.SubCategories.Count;

            string url = ((RssLink)parentCategory).Url;
            SortedDictionary<string, Category> subCats = new SortedDictionary<string, Category>();
            if (parentCategory.Other == null) return 0;
            test specifics = parentCategory.Other as test;

            bool hasNextPage = false;
            int pageNr = 0;
            do
            {
                pageNr++;
                string webData = GetWebData(pageNr == 1 ? url : url + @"/page=" + pageNr.ToString());

                hasNextPage = webData.Contains(@"class=""next""");
                webData = GetSubString(webData, specifics.subCatStart, specifics.subCatEnd);


                if (!string.IsNullOrEmpty(webData))
                {

                    Match m = specifics.regex_SubCat.Match(webData);
                    while (m.Success)
                    {
                        RssLink cat;
                        if (specifics.extraGetWeb)
                        {
                            cat = new RtlRssLink();
                        }
                        else
                            cat = new RssLink();

                        cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                        cat.Url = specifics.baseUrl + m.Groups["url"].Value;

                        cat.Thumb = m.Groups["thumb"].Value;
                        if (cat.Thumb != String.Empty) cat.Thumb = specifics.baseUrl + cat.Thumb;

                        cat.HasSubCategories = false;
                        cat.Other = parentCategory.Other;
                        cat.ParentCategory = parentCategory;

                        /*
                         if (specifics.extraGetWeb)
                        {
                            try
                            {
                                //string tmp = GetWebData(cat.Url);
                                bool add = cat.HasSubCategories; takes too much time!
                                if (!add && false)
                                {
                                    string tmp2 = GetWebData(cat.Url);
                                    tmp2 = GetSubString(tmp2, specifics.videoListStart, @"class=""pages""");
                                    Match m2 = specifics.regex_VideoList.Match(tmp2);
                                    add = m2.Success;
                                }
                                if (add)
                                {
                                    parentCategory.SubCategories.Add(cat);
                                }
                                else
                                    Console.WriteLine(cat.Name);
                            }
                            catch
                            {
                                Console.WriteLine("notfound!! " + cat.Name);
                            }

                        }
                        else */
                        if (!specifics.extraGetWeb || !rtlBlackList.ContainsKey(cat.Name))
                            if (!subCats.ContainsKey(cat.Name))
                                subCats.Add(cat.Name, cat);
                        //bool b = cat.HasSubCategories;

                        m = m.NextMatch();
                    }
                }
            } while (hasNextPage);


            parentCategory.SubCategoriesDiscovered = true;
            parentCategory.SubCategories = new List<Category>(subCats.Values);
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            string url = ((RssLink)category).Url;
            if (category.Other == null) return videos;
            test specifics = category.Other as test;

            bool hasNextPage = false;

            //url = GetRedirectedUrl(url); // for veronica multi pages
            int pageNr = 0;
            do
            {
                pageNr++;
                string webData;
                Log.Info("getting page {0} of category {1}", pageNr, category.Name);
                webData = GetWebData(pageNr == 1 ? url : url + @"/page=" + pageNr.ToString());
                hasNextPage = webData.Contains(@"class=""next""");
                webData = GetSubString(webData, specifics.videoListStart, @"class=""pages""");
                if (!string.IsNullOrEmpty(webData))
                {
                    Match m = specifics.regex_VideoList.Match(webData);
                    while (m.Success)
                    {
                        VideoInfo video = new VideoInfo();
                        video.Title = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                        video.ImageUrl = specifics.baseUrl + m.Groups["thumb"].Value;
                        video.VideoUrl = specifics.baseUrl + m.Groups["url"].Value;
                        video.Description = m.Groups["descr"].Value + " " +
                            m.Groups["episode"].Value + " " + m.Groups["airtime"].Value;

                        try
                        {
                            if (specifics.extraGetWeb)
                            {
                                string tmp = GetWebData(video.VideoUrl);
                                video.Description = video.Description + GetSubString(tmp, @"date:'", "'");
                                video.VideoUrl = GetSubString(tmp, @"file:'", "'");
                                video.Other = true;
                            }

                            videos.Add(video);
                        }
                        catch
                        {
                            Console.WriteLine(" no video found at " + video.VideoUrl);
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
            if (true.Equals(video.Other)) return video.VideoUrl;

            string webData = GetWebData(video.VideoUrl);
            webData = GetSubString(webData, @"class=""wmv-player-holder"" href=""", @"""");
            return webData;
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

        private class test
        {
            public Regex getRegex(string s)
            {
                return new Regex(s, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
            }

            public Regex regex_SubCat;
            public Regex regex_VideoList;
            public string baseUrl;
            public string subCatStart;
            public string subCatEnd;
            public string videoListStart;
            public bool extraGetWeb;
        }

        private class RtlRssLink : RssLink
        {
            private bool hasSubCats = false;
            private static string rtlVideoListRegex = @"menu_prefix[^']*'(?<part1>[^']*)'.*?menu_prefix[^']*'(?<part2>[^']*)'";
            private static Regex regex_RtlVideoList = new Regex(rtlVideoListRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
            private string prefix = String.Empty;
            private bool GetSubCats()
            {

                test specifics = Other as test;

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
                            Match m = regex_RtlVideoList.Match(vidxml);
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

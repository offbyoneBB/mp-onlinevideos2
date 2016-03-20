using HtmlAgilityPack;
using Jurassic;
using OnlineVideos.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace OnlineVideos.Sites
{
    public class SwefilmUtil : SiteUtilBase
    {
        private CookieContainer cc = new CookieContainer();
        private const string baseUrl = "http://swefilm.tv/";
        private T MyGetWebData<T>(string orgurl)
        {
            string webData = GetWebData(orgurl, cookies: cc);

            //Check cloudflare...
            Regex rgx = new Regex(@"var t,r,a,f,(?<g1>[^;]*;).*challenge-form.*?;[^;]*(?<g2>;.*)?a.value[^=]*=(?<g3>[^+]*)", RegexOptions.Singleline);
            Match m = rgx.Match(webData);
            if (m.Success)
            {
                string url = orgurl;
                if (url != baseUrl)
                {
                    //calculations dependent on this url...
                    url = baseUrl;
                    cc = new CookieContainer();
                    webData = GetWebData(url, cookies: cc);
                    m = rgx.Match(webData);
                }
                string js = "function answer() {";
                js += "var " + m.Groups["g1"].Value;
                js += m.Groups["g2"].Value;
                js += " return " + m.Groups["g3"].Value;
                js += " + 10; }";

                var engine = new Jurassic.ScriptEngine();
                engine.Execute(js);
                string answer = engine.CallGlobalFunction("answer").ToString();

                rgx = new Regex(@"jschl_vc.*?value=""(?<jschl_vc>[^""]*).*?pass.*?value=""(?<pass>[^""]*)", RegexOptions.Singleline);
                m = rgx.Match(webData);
                if (m.Success)
                {
                    url = baseUrl + "cdn-cgi/l/chk_jschl";
                    url += "?jschl_vc=" + m.Groups["jschl_vc"].Value;
                    url += "&pass=" + m.Groups["pass"].Value;
                    url += "&jschl_answer=" + answer;
                    //need to sleep...
                    Thread.Sleep(4000);
                    webData = GetWebData(url, cookies: cc, referer: baseUrl);
                    if (url != orgurl)
                        webData = GetWebData(orgurl, cookies: cc);
                }
            }

            if (typeof(T) == typeof(string))
            {
                return (T)(object)webData;
            }
            else if (typeof(T) == typeof(Newtonsoft.Json.Linq.JToken))
            {
                return (T)(object)Newtonsoft.Json.Linq.JToken.Parse(webData);
            }
            else if (typeof(T) == typeof(Newtonsoft.Json.Linq.JObject))
            {
                return (T)(object)Newtonsoft.Json.Linq.JObject.Parse(webData);
            }
            else if (typeof(T) == typeof(RssToolkit.Rss.RssDocument))
            {
                return (T)(object)RssToolkit.Rss.RssDocument.Load(webData);
            }
            else if (typeof(T) == typeof(System.Xml.XmlDocument))
            {
                var xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.LoadXml(webData);
                return (T)(object)xmlDoc;
            }
            else if (typeof(T) == typeof(System.Xml.Linq.XDocument))
            {
                return (T)(object)System.Xml.Linq.XDocument.Parse(webData);
            }
            else if (typeof(T) == typeof(HtmlAgilityPack.HtmlDocument))
            {
                HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(webData);
                return (T)(object)htmlDoc;
            }

            return default(T);
        }

        public override int DiscoverDynamicCategories()
        {
            string data = MyGetWebData<string>(baseUrl);
            Settings.Categories.Clear();
            RssLink hdFilmer = new RssLink() { Name = "HD-filmer", Url = "http://swefilm.tv/list/film/", HasSubCategories = true };
            RssLink tvSerier = new RssLink() { Name = "TV-serier", Url = "http://swefilm.tv/list/tvseries/", HasSubCategories = true };
            Category kategori = new Category() { Name = "Kategori", HasSubCategories = true, SubCategories = new List<Category>() };
            Category land = new Category() { Name = "Land", HasSubCategories = true, SubCategories = new List<Category>() };

            Regex rgx = new Regex(@"href=""(?<url>http://swefilm.tv/genre/[^/]*/)?"">(?<name>[^<]*)");
            foreach (Match match in rgx.Matches(data))
            {
                RssLink genre = new RssLink() { Name = match.Groups["name"].Value, Url = match.Groups["url"].Value, ParentCategory = kategori, HasSubCategories = true };
                kategori.SubCategories.Add(genre);
            }

            rgx = new Regex(@"href=""(?<url>http://swefilm.tv/country/[^/]*/)?"">(?<name>[^<]*)");
            foreach (Match match in rgx.Matches(data))
            {
                RssLink genre = new RssLink() { Name = match.Groups["name"].Value, Url = match.Groups["url"].Value, ParentCategory = land, HasSubCategories = true };
                land.SubCategories.Add(genre);
            }
            Settings.Categories.Add(hdFilmer);
            Settings.Categories.Add(tvSerier);
            kategori.SubCategoriesDiscovered = kategori.SubCategories.Count > 0;
            if (kategori.SubCategoriesDiscovered)
                Settings.Categories.Add(kategori);
            land.SubCategoriesDiscovered = land.SubCategories.Count > 0;
            if (land.SubCategoriesDiscovered)
                Settings.Categories.Add(land);
            Settings.DynamicCategoriesDiscovered = Settings.Categories.Count == 4;
            return Settings.Categories.Count;
        }

        private List<Category> DiscoverSubCategories(Category parentCategory, string url)
        {
            List<Category> cats = new List<Category>();
            HtmlDocument doc = MyGetWebData<HtmlDocument>(url);

            HtmlNode html = doc.DocumentNode.SelectSingleNode("//div[@class='html']");

            if (html != null)
            {
                HtmlNodeCollection items = html.SelectNodes(".//li");
                if (items != null)
                {
                    foreach (HtmlNode item in items)
                    {
                        HtmlNode nameTop = item.SelectSingleNode(".//div[@class='name_top']");
                        if (nameTop != null)
                        {
                            RssLink cat = new RssLink();
                            cat.Name = HttpUtility.HtmlDecode(nameTop.InnerText.Replace("\r", "").Replace("\n", "").Trim()).Replace("&#39","'");
                            cat.Url = nameTop.SelectSingleNode("a").GetAttributeValue("href", "");
                            //Not able to get thumbs - behind cloudflare...
                            //HtmlNode img = item.SelectSingleNode("a/img");
                            //string thumb = img != null ? ("http://swefilm.tv" + img.GetAttributeValue("src", "")) : "";
                            //cat.Thumb = thumb; 
                            cat.ParentCategory = parentCategory;
                            cats.Add(cat);
                        }
                    }
                }
            }
            HtmlNode nextNode =  doc.DocumentNode.SelectSingleNode("//a[@title='Next']");
            if (nextNode != null)
            {
                string nextUrl = nextNode.GetAttributeValue("href", "");
                if(!string.IsNullOrEmpty(nextUrl))
                {
                    cats.Add(new NextPageCategory() { Url = baseUrl + nextUrl, ParentCategory = parentCategory});
                }
            }
            return cats;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = DiscoverSubCategories(parentCategory, (parentCategory as RssLink).Url);
            parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
            return parentCategory.SubCategories.Count;
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            Category parentCategory = category.ParentCategory;
            parentCategory.SubCategories.Remove(category);
            List<Category> nextCats = DiscoverSubCategories(parentCategory, category.Url);
            parentCategory.SubCategories.AddRange(nextCats);
            return nextCats.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            string data = MyGetWebData<string>((category as RssLink).Url);
            Regex rgx = new Regex(@"(?<url>http://swefilm.tv/watch/[^""]*)");
            Match m = rgx.Match(data);
            SerializableDictionary<string, SerializableDictionary<string, string>> urlsDictionary = new SerializableDictionary<string, SerializableDictionary<string, string>>();
            if (m.Success)
            {
                data = MyGetWebData<string>(m.Groups["url"].Value);
                rgx = new Regex(@"<span class=""svname"">(?<cdn>[^<]*)</span><span class=""svep"">(?<urls>.*?)</span>");
                foreach (Match match in rgx.Matches(data))
                {
                    string cdnName = match.Groups["cdn"].Value;
                    Regex uRgx = new Regex(@"href=""(?<url>[^""]*)"">(?<name>[^<]*)");
                    foreach (Match uMatch in uRgx.Matches(match.Groups["urls"].Value))
                    {
                        string vName = uMatch.Groups["name"].Value;
                        string vUrl = uMatch.Groups["url"].Value;
                        if (!urlsDictionary.ContainsKey(vName))
                            urlsDictionary.Add(vName, new SerializableDictionary<string, string>());
                        if (!urlsDictionary[vName].ContainsKey(cdnName))
                            urlsDictionary[vName].Add(cdnName, vUrl);
                    }
                }
            }
            foreach (KeyValuePair<string, SerializableDictionary<string, string>> episodes in urlsDictionary)
            {
                foreach (KeyValuePair<string, string> cdn in episodes.Value /*.Where(c => !c.Key.ToLower().StartsWith("flash"))*/)
                {

                    string cdnInfo = cdn.Key;
                    string title = episodes.Key;
                    string desc = "";
                    if (title.StartsWith("Avsnitt HD") || title.StartsWith("Avsnitt Full HD"))
                    {
                        title = category.Name;
                        desc = title;
                    }
                    else
                    {
                        desc = category.Name + " " + title;
                    }
                    bool isOriginal = cdnInfo.ToLower().StartsWith("original");
                    cdnInfo = " [" + cdnInfo.Replace(":","") + ( isOriginal ? " - SLOW" : "" ) + "]";
                    videos.Add(new VideoInfo() { Title = title + cdnInfo, VideoUrl = cdn.Value, Description = desc + (isOriginal ? ("\r\n" + cdn.Key + " Original CDN starts very slow, please set a high timeout in OV settings") : ""), Other = cdn.Key });
                }
            }
            return videos;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string url = video.VideoUrl;
            string cdn = video.GetOtherAsString();
            string data = MyGetWebData<string>(url);
            Regex rgx = new Regex(@"(?<iframe>http://player.swefilm.tv/player/player.php[^""]*)");
            Match m = rgx.Match(data);
            if (m.Success)
            {
                url = m.Groups["iframe"].Value;
                data = MyGetWebData<string>(url);
                rgx = new Regex(@"\(\('(?<base>[^']*).*?.replace\('(?<replace>[^']*)");
                m = rgx.Match(data);
                if (m.Success)
                {
                    string base64 = m.Groups["base"].Value;
                    string replace = m.Groups["replace"].Value;
                    base64 = base64.Replace(replace, "");
                    base64 = Encoding.ASCII.GetString(Convert.FromBase64String(base64));
                    base64 = Encoding.ASCII.GetString(Convert.FromBase64String(base64));
                    base64 = Encoding.ASCII.GetString(Convert.FromBase64String(base64));
                    if (cdn.ToLower().StartsWith("original"))
                    {
                        rgx = new Regex(@"src=""(?<url>[^""]*)");
                        m = rgx.Match(base64);
                        if (m.Success)
                        {
                            string orgCdnUrl = m.Groups["url"].Value;
                            Hoster.HosterBase host = Hoster.HosterFactory.GetAllHosters().FirstOrDefault(h => orgCdnUrl.ToLowerInvariant().Contains(h.GetHosterUrl().ToLowerInvariant()));
                            if (host == null)
                                return "";
                            return host.GetVideoUrl(orgCdnUrl);
                        }
                    }
                    else if (cdn.ToLower().StartsWith("global") || cdn.ToLower().StartsWith("fast"))
                    {
                        rgx = new Regex(@"<source.+?src='(?<url>[^']*)[^>]*?type=""video");
                        m = rgx.Match(base64);
                        if (m.Success)
                        {
                            return m.Groups["url"].Value;
                        }
                    }
                    else if (cdn.ToLower().StartsWith("flash"))
                    {
                        rgx = new Regex(@"iframe.*?src=""(?<url>[^""]*)");
                        m = rgx.Match(base64);
                        if (m.Success)
                        {
                            string flashCdnUrl = m.Groups["url"].Value;
                            Hoster.HosterBase host = Hoster.HosterFactory.GetAllHosters().FirstOrDefault(h => flashCdnUrl.ToLowerInvariant().Contains(h.GetHosterUrl().ToLowerInvariant()));
                            if (host == null)
                                return "";
                            return host.GetVideoUrl(flashCdnUrl);


                        }
                    }
                }
            }
            return "";
        }

        public override bool CanSearch
        {
            get
            {
                return true;
            }
        }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            List<SearchResultItem> res = new List<SearchResultItem>();
            RssLink cat = new RssLink() { Name = "Search", Url = "http://swefilm.tv/search/" + HttpUtility.UrlDecode(query) };
            DiscoverSubCategories(cat);
            cat.SubCategories.ForEach(c => res.Add(c));
            return res;
        }

        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            string desc = video.Description;
            if (desc.Contains("vsnitt") && desc.Contains("eason")) //[Aa]vsnitt && [Ss]eason
            {
                Regex rgx = new Regex(@"(?<VideoKind>TvSeries)(?<Title>.*)?\s[Ss]eason\s*?(?<Season>\d+).*?[Aa]vsnitt.*?(?<Episode>\d+)");
                Match m = rgx.Match("TvSeries" + video.Description);
                ITrackingInfo ti = new TrackingInfo() { Regex = m };
                return ti;
            }
            else
            {
                Regex rgx = new Regex(@"(?<VideoKind>Movie)(?<Title>.*)?\s\((?<Year>\d{4})");
                Match m = rgx.Match("Movie" + video.Description);
                ITrackingInfo ti = new TrackingInfo() { Regex = m };
                return ti;
            }
        }


    }
}

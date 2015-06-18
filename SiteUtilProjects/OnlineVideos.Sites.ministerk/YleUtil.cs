using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using OnlineVideos.Helpers;
using OnlineVideos.Sites.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public class YleAppInfo
    {
        public string AppId { get; set; }
        public string AppKey { get; set; }
    }

    public enum Kieli
    {
        Kaikki,
        Suomi,
        Ruotsi
    }

    public class YleUtil : LatestVideosSiteUtilBase
    {
        #region OnlineVideosUserConfiguration

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Vain ulkomailla katsottavat"), Description("Rajaa tarkemmin: Vain ulkomailla katsottavat")]
        protected bool onlyNonGeoblockedContent = false;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Kieli"), Description("Rajaa tarkemmin: Kieli")]
        protected Kieli contentLanguage = Kieli.Kaikki;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("På svenska"), Description("Arenan: På svenska")]
        protected bool inSwedish = false;

        #endregion

        #region Variables and constants

        private YleAppInfo appInfo = null;

        //Translations
        private Dictionary<string, string> dictionary;
        
        //NextPage videos
        private string CurrentSeriesVideoUrl = string.Empty;
        private int CurrentSeriesVideoOffset = 0;
        private string CurrentSeriesCategoryName = string.Empty;

        //Api keys and reference
        private List<string> apiCategories;
        private List<string> apiChannelCategories;
        private List<string> apiSortOrders;
        private const string cApiClip = "clip";
        private const string cApiProgram = "program";
        private const string cApiContentTypeTvSeries = "TVSeries";
        private const string cApiContentTypeProgram = "TVProgram";
        private const string cApiContentTypeTvLive = "TVLive";
        private const int cApiLimit = 100;
        private const string cChannelCategory = "ChannelCategory";

        //Non API Categories 
        private const string cLiveCategory = "LiveCategory";
        private const string cAllProgramsCategory = "AllProgramsCategory";
        private const string cArchiveCategory = "ArchiveCategory";

        //Urls
        //Api urls
        private const string cUrlCategoryFormat = @"{0}api/v1/programs/tv?app_id={1}&app_key={2}&service=tv&category={3}&o={4}&region={5}&olang={6}{7}&limit={8}&offset={9}";
        private const string cUrlEpisodesFormat = @"{0}api/programs/v1/items.json?series={1}&type={2}&availability=ondemand&order=ondemand.publication.starttime%253Adesc&app_id={3}&app_key={4}&limit={5}&offset={6}";
        private const string cUrlProgramFormat = @"{0}api/programs/v1/id/{1}.json?app_id={2}&app_key={3}";
        private const string cUrlSearchFormat = @"{0}api/v1/search?language={1}&service=tv&query={2}";
        //Image url
        private const string cUrlImageFormat = @"http://a4.images.cdn.yle.fi/image/upload/{0}.jpg";
        //HTML urls
        private const string cUrlAllProgramsFormat = @"{0}tv/a-o";
        private const string cUrlArchive = @"http://areena.yle.fi/elava-arkisto/a-o";
        //Player api urls
        private const string cUrlLiveServiceFormat = @"http://player.yle.fi/api/v1/services.jsonp?id={0}&region={1}";
        private const string cUrlArchiveEmbedFormat = @"http://yle.fi/elavaarkisto/embed/{0}.jsonp?callback=yleEmbed.eaJsonpCallback&instance=1&id={1}&lang=fi";
        private const string cUrlHdsFormat = @"http://player.yle.fi/api/v1/media.jsonp?protocol=HDS&client=areena-flash-player&id={0}";
        #endregion

        #region Properties

        //Api app info
        private YleAppInfo AppInfo
        {
            get
            {
                if (appInfo == null)
                {
                    string data = GetWebData(AppUrl);
                    Regex r = new Regex(@"window.ohjelmat.*?(?<json>{[^<]*)");
                    Match m = r.Match(data);
                    if (m.Success)
                    {
                        JToken json = JConstructor.Parse(m.Groups["json"].Value);
                        appInfo = new YleAppInfo()
                        {
                            AppId = json["api"]["applicationId"].Value<string>(),
                            AppKey = json["api"]["applicationKey"].Value<string>()
                        };
                    }
                }
                return appInfo;
            }
        }

        //Localization
        private string BaseUrl
        {
            get
            {
                return inSwedish ? "http://arenan.yle.fi/" : "http://areena.yle.fi/";
            }
        }

        private string AppUrl
        {
            get
            {
                return BaseUrl + "tv";
            }
        }

        private string LiveUrl
        {
            get
            {
                return inSwedish ? "http://arenan.yle.fi/tv/direkt" : "http://areena.yle.fi/tv/suorat";
            }
        }

        private string ApiRegion
        {
            get
            {
                return onlyNonGeoblockedContent ? "world" : "fi";
            }
        }

        private string ApiLanguage
        {
            get
            {
                return inSwedish ? "sv" : "fi";
            }
        }

        private string ApiOtherLanguage
        {
            get
            {
                return inSwedish ? "fi" : "sv";
            }
        }

        private string ApiContentLanguage
        {
            get
            {
                string langParam = string.Empty;
                switch (contentLanguage)
                {
                    case Kieli.Ruotsi:
                        langParam = "&l=sv";
                        break;
                    case Kieli.Suomi:
                        langParam = "&l=fi";
                        break;
                }
                return langParam;
            }
        }

        #endregion

        #region LatestVideosSiteUtilBase implementation

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            if (inSwedish)
            {
                apiCategories = new List<string>() { "serier-och-film", "kultur-och-underhallning", "dokumentarer-och-fakta", "nyheter", "sport", "barn" };
                apiSortOrders = new List<string>() { "", "popularaste", "sistachansen", "ao" };
            }
            else
            {
                apiCategories = new List<string>() { "sarjat-ja-elokuvat", "viihde-ja-kulttuuri", "dokumentit-ja-fakta", "uutiset", "urheilu", "lapset" };
                apiSortOrders = new List<string>() { "", "suosituimmat", "vielaehdit", "ao" };
            }
            apiChannelCategories = new List<string>() { "*&k=yle-tv1", "*&k=yle-tv2", "*&k=yle-fem", "*&k=yle-teema", "*&k=yle-areena", "*&k=ylex" };

            dictionary = new Dictionary<string, string>();
            if (inSwedish)
            {
                dictionary.Add("serier-och-film", "Serier och film");
                dictionary.Add("kultur-och-underhallning", "Kultur och underhållning");
                dictionary.Add("dokumentarer-och-fakta", "Dokumentärer och fakta");
                dictionary.Add("nyheter", "Nyheter");
                dictionary.Add("sport", "Sport");
                dictionary.Add("barn", "Barn");
                dictionary.Add("", "Nyaste");
                dictionary.Add("popularaste", "Mest sedda");
                dictionary.Add("sistachansen", "Sista chansen");
                dictionary.Add("ao", "A-Ö");
                dictionary.Add(cApiClip, "Klipp");
                dictionary.Add(cApiProgram, "Avsnitt");
                dictionary.Add(cLiveCategory, "Direkt");
                dictionary.Add(cAllProgramsCategory, "Program A-Ö");
                dictionary.Add(cChannelCategory, "Program enligt kanal");
            }
            else
            {
                dictionary.Add("sarjat-ja-elokuvat", "Sarjat ja elokuvat");
                dictionary.Add("viihde-ja-kulttuuri", "Viihde ja kulttuuri");
                dictionary.Add("dokumentit-ja-fakta", "Dokumentit ja fakta");
                dictionary.Add("uutiset", "Uutiset");
                dictionary.Add("urheilu", "Urheilu");
                dictionary.Add("lapset", "Lapset");
                dictionary.Add("", "Uusimmat");
                dictionary.Add("suosituimmat", "Katsotuimmat");
                dictionary.Add("vielaehdit", "Vielä ehdit");
                dictionary.Add("ao", "A-Ö");
                dictionary.Add(cApiClip, "Klipit");
                dictionary.Add(cApiProgram, "Jaksot");
                dictionary.Add(cLiveCategory, "Suorat");
                dictionary.Add(cAllProgramsCategory, "Ohjelmat A–Ö");
                dictionary.Add(cChannelCategory, "Ohjelmat kanavittain");
                dictionary.Add(cArchiveCategory, "Elävä arkisto");
            }
            dictionary.Add("*&k=yle-tv1", "Yle TV1");
            dictionary.Add("*&k=yle-tv2", "Yle TV2");
            dictionary.Add("*&k=yle-fem", "Yle Fem");
            dictionary.Add("*&k=yle-teema", "Yle Teema");
            dictionary.Add("*&k=yle-areena", "Yle Areena");
            dictionary.Add("*&k=ylex", "YleX");
        }

        #region Categories

        public override int DiscoverDynamicCategories()
        {
            // API Categories
            foreach (string cat in apiCategories)
            {
                RssLink apiCat = new RssLink() { Name = dictionary[cat], Url = cat, HasSubCategories = true, SubCategories = new List<Category>() };
                foreach (string sortOrder in apiSortOrders)
                {
                    RssLink sortCat = new RssLink() { Name = dictionary[sortOrder], Url = sortOrder, ParentCategory = apiCat, HasSubCategories = true};
                    sortCat.Other = (Func<List<Category>>)(() => GetApiSubCategories(sortCat,0));
                    apiCat.SubCategories.Add(sortCat);
                }
                Settings.Categories.Add(apiCat);
            }

            // All programs category
            Category allProgramsCatergory = new Category() { Name = dictionary[cAllProgramsCategory], HasSubCategories = true };
            allProgramsCatergory.Other = (Func<List<Category>>)(() => GetAllProgramsSubCategories(allProgramsCatergory));
            Settings.Categories.Add(allProgramsCatergory);
            
            // Channel categories
            Category channelsCatergory = new Category() { Name = dictionary[cChannelCategory], HasSubCategories = true, SubCategories = new List<Category>() };
            foreach (string cat in apiChannelCategories)
            {
                RssLink channelCat = new RssLink() { Name = dictionary[cat], Url = cat, ParentCategory = channelsCatergory, HasSubCategories = true, SubCategories = new List<Category>() };
                foreach (string sortOrder in apiSortOrders)
                {
                    RssLink sortCat = new RssLink() { Name = dictionary[sortOrder], Url = sortOrder, ParentCategory = channelCat, HasSubCategories = true };
                    sortCat.Other = (Func<List<Category>>)(() => GetApiSubCategories(sortCat, 0));
                    channelCat.SubCategories.Add(sortCat);
                }
                channelsCatergory.SubCategories.Add(channelCat);
            }
            Settings.Categories.Add(channelsCatergory);

            // Live categories
            Category liveCatergory = new Category() { Name = dictionary[cLiveCategory], HasSubCategories = false, Other = cApiContentTypeTvLive };
            Settings.Categories.Add(liveCatergory);

            //Archive Category
            if (!inSwedish)
            {
                Category archiveCategory = new Category() { Name = dictionary[cArchiveCategory], HasSubCategories = true };
                archiveCategory.Other = (Func<List<Category>>)(() => GetArchiveProgramsSubCategories(archiveCategory));
                Settings.Categories.Add(archiveCategory);
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;

        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            var method = parentCategory.Other as Func<List<Category>>;
            if (method != null)
            {
                parentCategory.SubCategories = method.Invoke();
                parentCategory.SubCategoriesDiscovered = true;
                return parentCategory.SubCategories.Count;
            }
            return 0;
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            category.ParentCategory.SubCategories.Remove(category);
            var method = category.Other as Func<List<Category>>;
            if (method != null)
            {
                List<Category> cats = method.Invoke();
                category.ParentCategory.SubCategories.AddRange(cats);
                return cats.Count;
            }
            return 0;
        }

        private List<Category> GetApiSubCategories(Category parentCategory, int offset)
        {
            List<Category> subCategories = new List<Category>();
            string categoryKey = (parentCategory.ParentCategory as RssLink).Url;
            string sortOrder = (parentCategory as RssLink).Url;
            string url = string.Format(cUrlCategoryFormat, BaseUrl, AppInfo.AppId, AppInfo.AppKey, categoryKey, sortOrder, ApiRegion, ApiLanguage, ApiContentLanguage, cApiLimit, offset);
            JObject json = GetWebData<JObject>(url);
            JArray data = json["data"].Value<JArray>();
            foreach (JToken item in data)
            {
                if (!onlyNonGeoblockedContent || item["region"].Value<string>() == "World")
                {
                    string title = item["title"][ApiLanguage] == null ? item["title"][ApiOtherLanguage].Value<string>() : item["title"][ApiLanguage].Value<string>();
                    string id = item["id"].Value<string>();
                    string type = item["type"].Value<string>();
                    bool hasSubCats = type == cApiContentTypeTvSeries;
                    string image = (item["imageId"] != null) ? string.Format(cUrlImageFormat, item["imageId"].Value<string>()) : string.Empty;
                    uint clipCount = 0;
                    uint programCount = 0;
                    uint? estimatedVideoCount = null;
                    if (hasSubCats)
                    {
                        clipCount = (item["clipCount"] != null) ? item["clipCount"].Value<uint>() : 0;
                        programCount = (item["programCount"] != null) ? item["programCount"].Value<uint>() : 0;
                        estimatedVideoCount = clipCount + programCount;
                    }
                    RssLink category = new RssLink()
                    {
                        Name = title,
                        Url = hasSubCats ? string.Empty : id,
                        Thumb = image,
                        ParentCategory = parentCategory,
                        EstimatedVideoCount = estimatedVideoCount,
                        HasSubCategories = hasSubCats,
                        SubCategoriesDiscovered = hasSubCats,
                        SubCategories = new List<Category>()
                    };
                    if (hasSubCats)
                    {
                        if (programCount > 0)
                        {
                            category.SubCategories.Add(new RssLink()
                            {
                                Name = dictionary[cApiProgram],
                                Url = id,
                                ParentCategory = category,
                                EstimatedVideoCount = programCount,
                                HasSubCategories = false,
                                Other = type,
                                Thumb = image
                            });
                        }
                        if (clipCount > 0)
                        {
                            category.SubCategories.Add(new RssLink()
                            {
                                Name = dictionary[cApiClip],
                                Url = id,
                                ParentCategory = category,
                                EstimatedVideoCount = clipCount,
                                HasSubCategories = false,
                                Other = type,
                                Thumb = image
                            });
                        }
                    }
                    subCategories.Add(category);
                }

            }
            JToken meta = json["meta"];
            int count = meta["count"].Value<int>();
            if (count > cApiLimit + offset)
            {
                NextPageCategory next = new NextPageCategory() { ParentCategory = parentCategory };
                next.Other = (Func<List<Category>>)(() => GetApiSubCategories(parentCategory, cApiLimit + offset));
                subCategories.Add(next);
            }
            return subCategories;
        }

        private List<Category> GetAllProgramsSubCategories(Category parentCategory)
        {
            List<Category> subCategories = new List<Category>();
            HtmlDocument doc = GetWebData<HtmlDocument>(string.Format(cUrlAllProgramsFormat, BaseUrl));
            HtmlNode ul = doc.DocumentNode.SelectSingleNode("//ul[@class='index']");
            foreach (HtmlNode li in ul.SelectNodes("li"))
            {
                string idAttribute = li.GetAttributeValue("id", "");
                if (string.IsNullOrEmpty(idAttribute))
                {
                    HtmlNode a = li.SelectSingleNode("a");
                    RssLink program = new RssLink()
                    {
                        Name = a.InnerText,
                        Url = a.GetAttributeValue("href", "").Replace("/", ""),
                        ParentCategory = subCategories.Last(),
                        HasSubCategories = true
                    };
                    program.Other = (Func<List<Category>>)(() => GetProgramSubCategories(program));
                    subCategories.Last().SubCategories.Add(program);
                }
                else
                {
                    subCategories.Add(new Category()
                    {
                        Name = idAttribute,
                        ParentCategory = parentCategory,
                        HasSubCategories = true,
                        SubCategories = new List<Category>(),
                        SubCategoriesDiscovered = true
                    });
                }
            }
            return subCategories;
        }

        private List<Category> GetProgramSubCategories(Category category)
        {
            List<Category> cats = new List<Category>();
            JObject json = GetWebData<JObject>(string.Format(cUrlProgramFormat, BaseUrl, (category as RssLink).Url, AppInfo.AppId, AppInfo.AppKey));
            string type = json["data"]["type"].Value<string>();
            if (cApiContentTypeTvSeries == type)
            {
                //Series episodes and clips
                json = GetWebData<JObject>(string.Format(cUrlEpisodesFormat, BaseUrl, (category as RssLink).Url, "", AppInfo.AppId, AppInfo.AppKey, 1, 0));
                JToken meta = json["meta"];
                uint clipCount = meta[cApiClip].Value<uint>();
                uint programCount = meta[cApiProgram].Value<uint>();
                if (programCount > 0)
                {
                    cats.Add(new RssLink()
                    {
                        Name = dictionary[cApiProgram],
                        Url = (category as RssLink).Url,
                        ParentCategory = category,
                        EstimatedVideoCount = programCount,
                        HasSubCategories = false,
                        Other = type
                    });
                }
                if (clipCount > 0)
                {
                    cats.Add(new RssLink()
                    {
                        Name = dictionary[cApiClip],
                        Url = (category as RssLink).Url,
                        ParentCategory = category,
                        EstimatedVideoCount = clipCount,
                        HasSubCategories = false,
                        Other = type
                    });
                }
            }
            else
            {
                //Programs/Movies
                cats.Add(new RssLink()
                {
                    Name = category.Name,
                    Url = (category as RssLink).Url,
                    ParentCategory = category,
                    HasSubCategories = false,
                    Other = type
                });
            }
            return cats;
        }

        private List<Category> GetArchiveProgramsSubCategories(Category category)
        {
            List<Category> subCategories = new List<Category>();
            HtmlDocument doc = GetWebData<HtmlDocument>(string.Format(cUrlArchive, BaseUrl));
            HtmlNodeCollection uls = doc.DocumentNode.SelectNodes("//ul[@class='index']");
            foreach (HtmlNode li in uls.Descendants("li"))
            {
                string dataLetterAttribute = li.GetAttributeValue("data-letter", "");
                if (!string.IsNullOrEmpty(dataLetterAttribute))
                {
                    subCategories.Add(new Category()
                    {
                        Name = dataLetterAttribute,
                        ParentCategory = category,
                        HasSubCategories = true,
                        SubCategories = new List<Category>(),
                        SubCategoriesDiscovered = true
                    });
                }
                HtmlNode a = li.SelectNodes("a").First(n => !n.GetAttributeValue("href", "").StartsWith("#"));
                RssLink program = new RssLink()
                {
                    Name = a.InnerText,
                    Url = a.GetAttributeValue("href", "").Replace("/",""),
                    ParentCategory = subCategories.Last(),
                    HasSubCategories = false,
                    Other = cArchiveCategory
                };
                subCategories.Last().SubCategories.Add(program);
            }
            return subCategories;
        }

        #endregion

        #region Videos

        public override List<VideoInfo> GetVideos(Category category)
        {
            HasNextPage = false;
            List<VideoInfo> videos;
            if (cApiContentTypeTvSeries == category.GetOtherAsString())
            {
                CurrentSeriesVideoUrl = (category as RssLink).Url;
                CurrentSeriesVideoOffset = 0;
                CurrentSeriesCategoryName = category.Name;
                videos = GetSeriesVideos();
            }
            else if (cApiContentTypeTvLive == category.GetOtherAsString())
            {
                videos = GetLiveVideos();
            }
            else if (cArchiveCategory == category.GetOtherAsString())
            {
                videos = GetArchiveVideos(category);
            }
            else
            {
                videos = new List<VideoInfo>();
                string url = string.Format(cUrlProgramFormat, BaseUrl, (category as RssLink).Url, AppInfo.AppId, AppInfo.AppKey);
                JObject json = GetWebData<JObject>(url);
                JToken data = json["data"];
                VideoInfo video = GetVideoInfoFromJsonData(data, false);
                if (video != null)
                {
                    videos.Add(video);
                }
            }
            return videos;
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            //Only TV-Series have paging
            return GetSeriesVideos();
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            bool isArchive = video.GetOtherAsString() == cArchiveCategory;
            string archiveUrl = "";
            if (isArchive)
            {
                Regex archiveRgx = new Regex(@".*(?<prefix>\d-)(?<id>\d*)");
                Match archiveMatch = archiveRgx.Match(video.VideoUrl);
                if (archiveMatch.Success)
                {
                    Regex mediakantaIdRegex = new Regex(@"""mediakantaId"":""(?<id>[^""]*)");
                    string id = archiveMatch.Groups["id"].Value;
                    Match mediakantaIdMatch = mediakantaIdRegex.Match(GetWebData(string.Format(cUrlArchiveEmbedFormat, id, id)));
                    if (mediakantaIdMatch.Success)
                    {
                        archiveUrl = archiveMatch.Groups["prefix"].Value + mediakantaIdMatch.Groups["id"].Value;
                    }
                }
            }
            string url = string.Format(cUrlHdsFormat, isArchive ? archiveUrl : video.VideoUrl);
            JObject json = GetWebData<JObject>(url, cache: false);
            JToken hdsStream = json["data"]["media"]["HDS"].FirstOrDefault(h => h["subtitles"] != null && h["subtitles"].Count() > 0);
            if (hdsStream == null)
            {
                hdsStream = json["data"]["media"]["HDS"].First;
            }
            else
            {
                JToken subtitle = hdsStream["subtitles"].FirstOrDefault(s => s["lang"].Value<string>() == ApiLanguage);
                if (subtitle == null) subtitle = hdsStream["subtitles"].FirstOrDefault(s => s["lang"].Value<string>() == ApiOtherLanguage);
                if (subtitle != null && subtitle["uri"] != null) video.SubtitleUrl = subtitle["uri"].Value<string>();
            }
            string data = hdsStream["url"].Value<string>();
            byte[] bytes = Convert.FromBase64String(data);
            RijndaelManaged rijndael = new RijndaelManaged();
            byte[] iv = new byte[16];
            Array.Copy(bytes, iv, 16);
            rijndael.IV = iv;
            rijndael.Key = Encoding.ASCII.GetBytes("yjuap4n5ok9wzg43");
            rijndael.Mode = CipherMode.CFB;
            rijndael.Padding = PaddingMode.Zeros;
            ICryptoTransform decryptor = rijndael.CreateDecryptor(rijndael.Key, rijndael.IV);
            int padLen = 16 - bytes.Length % 16;
            byte[] newbytes = new byte[bytes.Length - 16 + padLen];
            Array.Copy(bytes, 16, newbytes, 0, bytes.Length - 16);
            Array.Clear(newbytes, newbytes.Length - padLen, padLen);
            string result = null;
            using (MemoryStream msDecrypt = new MemoryStream(newbytes))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        result = srDecrypt.ReadToEnd();
                    }
                }
            }
            Regex r;
            if (video.GetOtherAsString() == cApiContentTypeTvLive)
                r = new Regex(@"(?<url>.*\.f4m)");
            else
                r = new Regex(@"(?<url>.*hmac=[a-z0-9]*)");
            Match m = r.Match(result);
            if (m.Success)
            {
                result = m.Groups["url"].Value;
            }
            if (video.GetOtherAsString() == cApiContentTypeTvLive)
            {
                result += "?g=" + HelperUtils.GetRandomChars(12) + "&hdcore=3.3.0&plugin=flowplayer-3.3.0.0";
                MPUrlSourceFilter.AfhsManifestUrl f4mUrl = new MPUrlSourceFilter.AfhsManifestUrl(result)
                {
                    LiveStream = true
                };
                result = f4mUrl.ToString();
            }
            else
            {
                result += "&g=" + HelperUtils.GetRandomChars(12) + "&hdcore=3.3.0&plugin=flowplayer-3.3.0.0";
            }
            return result;
        }

        public override List<VideoInfo> GetLatestVideos()
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            try
            {
                string url = string.Format(cUrlCategoryFormat, BaseUrl, AppInfo.AppId, AppInfo.AppKey, "*", "", ApiRegion, ApiLanguage, ApiContentLanguage, latestVideosCount, 0);
                JObject json = GetWebData<JObject>(url);
                foreach(JToken item in json["data"].Value<JArray>())
                {
                    string id = item["latestProgram"]["id"].Value<string>();
                    JObject programJson = GetWebData<JObject>(string.Format(cUrlProgramFormat, BaseUrl, id, AppInfo.AppId, AppInfo.AppKey));
                    videos.Add(GetVideoInfoFromJsonData(programJson["data"], item["type"].Value<string>() == cApiContentTypeTvSeries));
                }
            }
            catch { }
            return videos;
        }

        private List<VideoInfo> GetLiveVideos()
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            HtmlDocument doc = GetWebData<HtmlDocument>(LiveUrl, cache: false);
            HtmlNodeCollection divs = doc.DocumentNode.SelectNodes("//div[@class='channel-preview']");
            foreach (HtmlNode d in divs)
            {
                HtmlNode div = d.ParentNode;
                HtmlNode a = div.SelectSingleNode(".//a[@class='channel-live-link']");
                string id = a.GetAttributeValue("href", "");
                id = Path.GetFileName(id);
                id = id.Substring(0, id.IndexOf("#"));
                JToken outlet = null;
                VideoInfo video = null;
                if (!id.StartsWith("yle"))
                {
                    string url = string.Format(cUrlProgramFormat, BaseUrl, id, AppInfo.AppId, AppInfo.AppKey);
                    JObject json = GetWebData<JObject>(url);
                    JToken data = json["data"];
                    video = GetVideoInfoFromJsonData(data, false);
                }
                else
                {
                    JObject json = GetWebData<JObject>(string.Format(cUrlLiveServiceFormat, id, ApiRegion), cache: false);
                    outlet = json["data"]["outlets"].FirstOrDefault(o => o["outlet"]["language"] != null && o["outlet"]["language"].First.Value<string>() == ApiLanguage && (!onlyNonGeoblockedContent || o["outlet"]["region"].Value<string>() == "World"));
                    if (outlet == null)
                        outlet = json["data"]["outlets"].FirstOrDefault(o => !onlyNonGeoblockedContent || o["outlet"]["region"].Value<string>() == "World");
                }
                if (outlet != null || video != null)
                {
                    if (video == null)
                    {
                        video = new VideoInfo();
                        video.VideoUrl = outlet["outlet"]["media"]["id"].Value<string>();
                        video.Other = cApiContentTypeTvLive;
                    }
                    video.Title = a.InnerText;
                    HtmlNode img = div.SelectSingleNode(".//noscript/img");
                    string imgUrl = img.GetAttributeValue("src", "");
                    video.Thumb = imgUrl.StartsWith("//") ? "http:" + imgUrl : imgUrl;
                    string desc = "";
                    foreach (HtmlNode li in div.SelectNodes(".//li"))
                    {
                        desc += li.SelectSingleNode(".//time[@class='dtstart']").InnerText.Trim() + " ";
                        desc += li.SelectSingleNode(".//div[@class='program-title']").InnerText.Replace("\n", " ").Trim() + "\r\n";
                        desc += li.SelectSingleNode(".//div[@class='program-desc']").InnerText.Replace("\n", " ").Trim() + "\r\n";
                    }
                    desc = Regex.Replace(desc, @"[ \f\t\v]+", " ");
                    video.Description = desc;
                    videos.Add(video);
                }
            }
            return videos;
        }

        private List<VideoInfo> GetSeriesVideos()
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            string clipOrProgram = CurrentSeriesCategoryName == dictionary[cApiClip] ? cApiClip : cApiProgram;
            string url = string.Format(cUrlEpisodesFormat, BaseUrl, CurrentSeriesVideoUrl, clipOrProgram, AppInfo.AppId, AppInfo.AppKey, cApiLimit, CurrentSeriesVideoOffset);
            JObject json = GetWebData<JObject>(url);
            if (json["data"] != null && json["data"].Count() > 0)
            {
                foreach (JToken data in json["data"])
                {
                    VideoInfo video = GetVideoInfoFromJsonData(data, true);
                    if (video != null)
                    {
                        videos.Add(video);
                    }
                }
            }
            JToken meta = json["meta"];
            int count = meta[clipOrProgram].Value<int>();
            if (count > cApiLimit + CurrentSeriesVideoOffset)
            {
                CurrentSeriesVideoOffset += cApiLimit;
                HasNextPage = true;
            }
            else
            {
                HasNextPage = false;
            }
            return videos;
        }

        private VideoInfo GetVideoInfoFromJsonData(JToken data, bool isSeries)
        {
            VideoInfo video = null;
            JToken publicationEvents = data["publicationEvent"];
            JToken publicationEvent = publicationEvents.FirstOrDefault(pe => 
                    pe["type"].Value<string>() == "OnDemandPublication" && 
                    pe["temporalStatus"].Value<string>() == "currently" && 
                    pe["media"] != null && 
                    pe["media"]["available"] != null && 
                    pe["media"]["available"].Value<bool>() &&
                    (!onlyNonGeoblockedContent || pe["region"].Value<string>() == "World")
                    );
            if (publicationEvent != null)
            {
                string id = publicationEvent["media"]["id"].Value<string>();
                DateTime start = publicationEvent["startTime"].Value<DateTime>();
                string title = string.Empty;
                string episodeTitle = string.Empty;
                string type = data["type"].Value<string>();
                if (isSeries && type == cApiContentTypeProgram)
                {
                    JToken episode = data["episodeNumber"];
                    if (episode != null && episode.Value<uint?>() != null)
                    {
                        episodeTitle = dictionary[cApiProgram] + " " + episode.Value<uint>() + " ";
                    }
                }
                JToken itemTitle = data["itemTitle"];
                if (itemTitle != null && itemTitle.Count() > 0)
                {
                    JToken itemTitleLang = itemTitle[ApiLanguage];
                    JToken itemTitleOtherLang = itemTitle[ApiOtherLanguage];
                    if (itemTitleLang != null)
                    {
                        title = itemTitleLang.Value<string>();
                    }
                    else if (itemTitleOtherLang != null)
                    {
                        title = itemTitleOtherLang.Value<string>();
                    }
                }
                if (string.IsNullOrEmpty(title))
                {
                    JToken promotionTitle = data["promotionTitle"];
                    if (promotionTitle != null && promotionTitle.Count() > 0)
                    {
                        JToken titleLang = promotionTitle[ApiLanguage];
                        JToken titleOtherLang = promotionTitle[ApiOtherLanguage];
                        if (titleLang != null)
                        {
                            title = titleLang.Value<string>();
                        }
                        else if (titleOtherLang != null)
                        {
                            title = titleOtherLang.Value<string>();
                        }
                    }
                }
                if (string.IsNullOrEmpty(title))
                {
                    JToken titleToken = data["title"];
                    if (titleToken != null && titleToken.Count() > 0)
                    {
                        JToken titleLang = titleToken[ApiLanguage];
                        JToken titleOtherLang = titleToken[ApiOtherLanguage];
                        if (titleLang != null)
                        {
                            title = titleLang.Value<string>();
                        }
                        else if (titleOtherLang != null)
                        {
                            title = titleOtherLang.Value<string>();
                        }
                    }
                }
                title = episodeTitle + title;
                string description = string.Empty;
                JToken descriptionJson = data["description"];
                if (descriptionJson != null && descriptionJson.Count() > 0)
                {
                    description = descriptionJson[ApiLanguage] == null ? descriptionJson[ApiOtherLanguage].Value<string>() : descriptionJson[ApiLanguage].Value<string>();
                }
                JToken imageJson = data["image"];
                string image = string.Empty;
                if (imageJson != null && imageJson.Count() > 0 && imageJson["available"] != null && imageJson["available"].Value<bool>())
                {
                    image = (imageJson["id"] != null) ? string.Format(cUrlImageFormat, imageJson["id"].Value<string>()) : image;
                }

                video = new VideoInfo()
                {
                    Title = title,
                    Description = description,
                    Thumb = image,
                    Airdate = start.ToString(),
                    VideoUrl = id
                };
            }
            return video;
        }

        private List<VideoInfo> GetArchiveVideos(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            HtmlDocument doc = GetWebData<HtmlDocument>(BaseUrl + (category as RssLink).Url);
            HtmlNodeCollection episodeNodes = doc.DocumentNode.SelectNodes("//li[@itemprop='episode']");
            foreach (HtmlNode epNode in episodeNodes)
            {
                VideoInfo video = new VideoInfo();
                video.VideoUrl = epNode.GetAttributeValue("data-item-id", "");
                video.Title = epNode.SelectSingleNode(".//span[@class='program-title']").InnerText;
                video.Description = epNode.SelectSingleNode(".//span[@class='program-desc']").InnerText;
                HtmlNode img = epNode.SelectSingleNode(".//noscript/img");
                string imgUrl = img.GetAttributeValue("src", "");
                video.Thumb = imgUrl.StartsWith("//") ? "http:" + imgUrl : imgUrl;
                video.Other = cArchiveCategory;
                videos.Add(video);
            }
            return videos;
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
            JObject json = GetWebData<JObject>(string.Format(cUrlSearchFormat, BaseUrl, ApiLanguage, HttpUtility.UrlEncode(query)));
            JArray data = json["data"].Value<JArray>();
            foreach (JToken t in data)
            {
                RssLink program = new RssLink();
                program.Url = t["id"].Value<string>();
                program.Name = t["title"][ApiLanguage] == null ? t["title"][ApiOtherLanguage].Value<string>() : t["title"][ApiLanguage].Value<string>();
                program.Description = (t["description"] == null || !t["description"].HasValues) ? string.Empty : ( t["description"][ApiLanguage] == null ? t["description"][ApiOtherLanguage].Value<string>() : t["description"][ApiLanguage].Value<string>());
                JToken imageJson = t["image"];
                string image = string.Empty;
                if (imageJson != null && imageJson.Count() > 0 && imageJson["available"] != null && imageJson["available"].Value<bool>())
                {
                    program.Thumb = (imageJson["id"] != null) ? string.Format(cUrlImageFormat, imageJson["id"].Value<string>()) : image;
                }
                program.HasSubCategories = true;
                program.Other = (Func<List<Category>>)(() => GetProgramSubCategories(program));
                results.Add(program);
            }
            
            return results;
        }

        #endregion

        #endregion

    }
}

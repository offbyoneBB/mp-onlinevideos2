using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Net;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;


namespace OnlineVideos.Sites
{
    public class MiteleUtil : GenericSiteUtil
    {
        internal String miteleBaseUrl = "http://www.mitele.es";

        internal String seriesListRegex = "<a\\shref=\"(?<url>[^\"]*)\"\\stitle=\"(?<title>[^\"]*)\"\\sclass=\"ElementImageContainer\">\\s*<img\\swidth=\"195\"\\sheight=\"110\"\\salt=\"[^\"]*\"\\ssrc=\"(?<thumb>[^\"]*)\"\\s/>(?:(?!</a).)*</a>";
        internal String nextPageRegex = "<li\\sclass=\"next\"><a\\shref=\"javascript:void\\(0\\);\">Siguiente\\s&raquo;</a></li>";
        internal String nextPageProgramsARG = "pag=";
        internal String programsData = "";
        internal String temporadasBrowserRegex = "{\\s*temporadas:\\s\\[(?<temporadas>[^]]*)]\\s}";
        internal String temporadasRegex = "{(?<temporadas>[^}]*)}";
        internal String temporadasARG = "/temporadasbrowser/getCapitulos/";
        internal String episodiosPaginaRegex = "{\"episodes\":\\[(?<episodios>[^]]*)],\"hasNext\":(?<hayMasPaginas>[^}]*)}";
        internal String episodiosRegex = "{(?<episodios>[^}]*)}";
        internal String xmlURLRegex = "{\"host\":\"(?<url>[^\"]*)\"";
        internal String xmlDataRegex = "<duration>(?<Duration>[^<]*)</duration>\\s*<videoUrl\\sscrubbing=\"(?<Scrubbing>[^\"]*)\"\\smultipleDef=\"(?<MultipleDef>[^\"]*)\"\\srtmp=\"(?<rtmp>[^\"]*)\">\\s*<link\\sstart=\"(?<start>[^\"]*)\"\\send=\"(?<end>[^\"]*)\">(?<VideoURL>[^<]*)</link>\\s*</videoUrl>";
        internal String episodesData = "";
        internal String timeURL = "http://token.mitele.es/clock.php";
        internal String tokenizerURL = "http://token.mitele.es/";
        internal String finalVideoURLRegex = "<file[^>]*>(?<url>[^<]*)</file>";
        internal int i = 1;
        internal int j = 1;
        internal JArray episodios;

        internal enum CategoryType
        {
            None,
            Series,
            TVMovies,
            Programas,
            Infantil,
            VO,
            Temporadas
        }

        internal Regex regexSeriesList;
        internal Regex regexNextPage;
        internal Regex regexTemporadasBrowser;
        internal Regex regexTemporadas;
        internal Regex regexEpisodiosPagina;
        internal Regex regexEpisodios;
        internal Regex regexXmlURL;
        internal Regex regexXmlData;
        internal Regex regexFinalVideoURL;

        public override void Initialize(SiteSettings siteSettings)
        {
            InitializeRegex();
            siteSettings.DynamicCategoriesDiscovered = false;
            base.Initialize(siteSettings);
        }

        internal void InitializeRegex()
        {
            regexSeriesList = new Regex(seriesListRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            regexNextPage = new Regex(nextPageRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexTemporadasBrowser = new Regex(temporadasBrowserRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexTemporadas = new Regex(temporadasRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexEpisodiosPagina = new Regex(episodiosPaginaRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            regexEpisodios = new Regex(episodiosRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexXmlURL = new Regex(xmlURLRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
            regexXmlData = new Regex(xmlDataRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexFinalVideoURL = new Regex(finalVideoURLRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            Settings.Categories.Add(CreateCategory("Programas", miteleBaseUrl + "/programas-tv/", String.Empty, CategoryType.Programas, "", null));
            Settings.Categories.Add(CreateCategory("Series", miteleBaseUrl+"/series-online/", String.Empty, CategoryType.Series, "", null));
            Settings.Categories.Add(CreateCategory("TV Movies", miteleBaseUrl + "/tv-movies/", String.Empty, CategoryType.TVMovies, "", null));
            Settings.Categories.Add(CreateCategory("Infantil", miteleBaseUrl + "/tv-infantil/", String.Empty, CategoryType.Infantil, "", null));
            Settings.Categories.Add(CreateCategory("V.O.", miteleBaseUrl + "/mitele-vo/", String.Empty, CategoryType.VO, "", null));
            Settings.DynamicCategoriesDiscovered = true;

            return Settings.Categories.Count;
        }

        internal RssLink CreateCategory(String name, String url, String thumbUrl, CategoryType categoryType, String description, Category parentCategory)
        {
            RssLink category = new RssLink();

            category.Name = name;
            category.Url = url;
            category.Thumb = thumbUrl;
            category.Other = categoryType;
            category.Description = description;
            category.HasSubCategories = categoryType != CategoryType.None;
            category.SubCategoriesDiscovered = false;
            category.ParentCategory = parentCategory;

            return category;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {            
            List<Category> subCategories = null;

            switch ((CategoryType)parentCategory.Other)
            {
                case CategoryType.Series:
                    subCategories = DiscoverSeries(parentCategory as RssLink);
                    break;
                case CategoryType.TVMovies:
                    subCategories = DiscoverSeries(parentCategory as RssLink);
                    break;
                case CategoryType.Programas:
                    subCategories = DiscoverSeries(parentCategory as RssLink);
                    break;
                case CategoryType.Infantil:
                    subCategories = DiscoverSeries(parentCategory as RssLink);
                    break;
                case CategoryType.VO:
                    subCategories = DiscoverSeries(parentCategory as RssLink);
                    break;
                case CategoryType.Temporadas:
                    subCategories = DiscoverTemporadas(parentCategory as RssLink);
                    break;
            }

            parentCategory.SubCategories = subCategories;
            parentCategory.SubCategoriesDiscovered = true;
            parentCategory.HasSubCategories = subCategories == null ? false : subCategories.Count > 0;

            return parentCategory.HasSubCategories ? subCategories.Count : 0;
        }

        internal List<Category> DiscoverSeries(RssLink parentCategory)
        {
            List<Category> result = new List<Category>();
            programsData = GetWebData(parentCategory.Url);
            i = 1;
            getNextPages(programsData, parentCategory.Url);
            Match match = regexSeriesList.Match(programsData);
            while (match.Success)
            {
                String name = match.Groups["title"].Value;
                String url = miteleBaseUrl + match.Groups["url"].Value;
                String thumbUrl = match.Groups["thumb"].Value;
                result.Add(CreateCategory(name, url, thumbUrl, CategoryType.Temporadas, "", parentCategory));
                Log.Debug("mitele: serie {0} added.", name);
                match = match.NextMatch();
            }
            return result;
        }

        internal List<Category> DiscoverTemporadas(RssLink parentCategory)
        {
            List<Category> result = new List<Category>();
            String data = GetWebData(parentCategory.Url);
            Match content = regexTemporadasBrowser.Match(data);
            JObject jsonTemporadas = JObject.Parse(content.Value);
            JArray temporadas = (JArray)jsonTemporadas["temporadas"];
            for (int k = 0; k < temporadas.Count; k++)
            {
                JToken temporada = temporadas[k];
                String name = (String)temporada.SelectToken("post_title");
                String idTemporada = (String)temporada.SelectToken("ID");
                String url = miteleBaseUrl + temporadasARG + idTemporada;
                result.Add(CreateCategory(name, url, "", CategoryType.None, "", parentCategory));
                Log.Debug("Mitele: temporada {0} added.", name);
            }
            return result;
        }

        public void getNextPages(String data, String url)
        {
            String newData = "";
            if (!string.IsNullOrEmpty(data))
            {
                Match match = regexNextPage.Match(data);
                if (match.Success)
                {
                    fileUrlPostString = nextPageProgramsARG + ++i;
                    Log.Debug("Mitele: searching next pages categories {0} {1}", url, fileUrlPostString);
                    newData = GetWebDataFromPostMitele(url, fileUrlPostString);
                    programsData += newData;
                    match = regexNextPage.Match(newData);
                    if (match.Success)
                    {
                        getNextPages(newData, url);
                    }
                }
            }
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            Log.Debug("Mitele: getting video list from category {0}", category.Name);
            List<VideoInfo> videoList = new List<VideoInfo>();
            j = 1;
            String data = GetWebData((category as RssLink).Url + "/" + j + "/");
            Match episodiosPaginaMatch = regexEpisodiosPagina.Match(data);
            JObject jsonEpisodios = JObject.Parse(episodiosPaginaMatch.Value);
            episodios = (JArray)jsonEpisodios["episodes"];
            getNextPagesEpisodes(data, (category as RssLink).Url);
            for (int k = 0; k < episodios.Count; k++)
            {
                JToken episodio = episodios[k];
                VideoInfo video = new VideoInfo();
                video.Title = episodio.SelectToken("post_title") + " - " + episodio.SelectToken("post_subtitle");
                video.Description = (String)episodio.SelectToken("post_content");
                video.Airdate = (String)episodio.SelectToken("post_date");
                video.ImageUrl = (String)episodio.SelectToken("image");
                video.VideoUrl = miteleBaseUrl + (String)episodio.SelectToken("url");
                videoList.Add(video);
            }
            return videoList;
        }

        public void getNextPagesEpisodes(String data, String url)
        {
            String newData = "";
            if (!string.IsNullOrEmpty(data))
            {
                Match episodiosPaginaMatch = regexEpisodiosPagina.Match(data);
                if (episodiosPaginaMatch.Success && episodiosPaginaMatch.Groups["hayMasPaginas"].Value.Equals("true"))
                {
                    Log.Debug("Mitele: searching next pages episodes");
                    newData = GetWebData(url + "/" + ++j + "/");
                    Match episodiosPaginaMatchAUX = regexEpisodiosPagina.Match(newData);
                    JObject jsonEpisodios = JObject.Parse(episodiosPaginaMatchAUX.Value);
                    JArray episodiosAUX = (JArray)jsonEpisodios["episodes"];
                    for (int k = 0; k < episodiosAUX.Count; k++)
                    {
                        JToken episodio = episodiosAUX[k];
                        episodios.Add(episodio);
                    }
                    episodiosPaginaMatch = regexEpisodiosPagina.Match(data);
                    if (episodiosPaginaMatch.Success && episodiosPaginaMatch.Groups["hayMasPaginas"].Value.Equals("true"))
                    {
                        getNextPagesEpisodes(newData, url);
                    }
                }
            }
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            Log.Debug("Mitele: getting video URL from video {0} ", video.Title);
            String data = GetWebData(video.VideoUrl);
            String videoURL = "";
            Match xmlURLMatch = regexXmlURL.Match(data);
            if (xmlURLMatch.Success)
            {
                String xmlURL = xmlURLMatch.Groups["url"].Value;
                xmlURL = xmlURL.Replace("\\", "");
                String xmlData = GetWebData(xmlURL);
                Match xmlDataMatch = regexXmlData.Match(xmlData);
                if (xmlDataMatch.Success)
                {
                    String startTime = xmlDataMatch.Groups["start"].Value;
                    if (string.IsNullOrEmpty(startTime))
                    {
                        startTime = "0";
                    }
                    String endTime = xmlDataMatch.Groups["end"].Value;
                    if (string.IsNullOrEmpty(endTime))
                    {
                        endTime = "0";
                    }
                    String url = xmlDataMatch.Groups["VideoURL"].Value;
                    videoURL = getVideoURLMitele(url, startTime, endTime);
                    Log.Debug("Mitele: videoURL {0} added", videoURL);
                }
            }
            return videoURL;
        }

        public String getVideoURLMitele(String url, String startTime, String endTime)
        {
            String serverTime = GetWebData(timeURL);
            String toEncode = serverTime + ";" + url + ";" + startTime + ";" + endTime;
            String encodedParams = Flowplayer.Commercial.V3_1_5_17_002.Aes.Encrypt(toEncode, base64Encode(Flowplayer.Commercial.V3_1_5_17_002.Aes.Key), Flowplayer.Commercial.V3_1_5_17_002.Aes.KeyType.Key256);
            String hash = HttpUtility.UrlEncode(encodedParams);
            NameValueCollection headers = new NameValueCollection();
            headers.Add("Accept", "*/*");
            headers.Add("Accept-Charset", "ISO-8859-1,utf-8;q=0.7,*;q=0.3");
            headers.Add("Accept-Encoding", "gzip,deflate,sdch");
            headers.Add("Accept-Language", "es-ES,es;q=0.8");
            headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.1 (KHTML, like Gecko) Chrome/21.0.1180.89 Safari/537.1");
            headers.Add("Origin", "http://static1.tele-cinco.net");
            headers.Add("Referer", "http://static1.tele-cinco.net/comun/swf/playerMitele.swf");
            string data = GetWebData(tokenizerURL+"?hash=" + hash + "&id=" + url + "&startTime=0&endTime=0", headers: headers, cache: false);
            return data.Substring(data.IndexOf("tokenizedUrl\":\"") + "tokenizedUrl\":\"".Length).Split('\"')[0].Replace(" ", "").Replace("\\/", "/");
        }

        public string GetWebDataFromPostMitele(string url, string postData)
        {
            NameValueCollection headers = new NameValueCollection();
            headers.Add("X-Requested-With", "XMLHttpRequest");
            headers.Add("Accept", "*/*"); // accept any content type
            headers.Add("User-Agent", OnlineVideoSettings.Instance.UserAgent); // set the default OnlineVideos UserAgent when none specified
            return GetWebData(url, postData, headers: headers);
        }
        
        public static string base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}

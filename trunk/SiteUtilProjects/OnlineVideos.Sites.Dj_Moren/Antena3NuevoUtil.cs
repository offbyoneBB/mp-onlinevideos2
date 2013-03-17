using System;
using System.Collections.Generic;
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

namespace OnlineVideos.Sites
{
    public class Antena3NuevoUtil : GenericSiteUtil
    {
        internal String antena3BaseUrl = "http://www.antena3.com";
        internal String categoryContentRegex = "\\<ul class=\"subemnu\"\\>(?<content>.*?)</ul>";
        internal String categoryRegex = "\\<a.*?title=\"(?<title>.*?)\".*?href=\"(?<link>.*?)\".*?\\>(?<label>.*?)\\</a\\>";
        internal String videoLinksRegex = "\\<a.*?title=\"(?<title>.*?)\"\\s*?href=\"(?<url>.*?)\"\\s*?\\>\\s*?\\<img.*?title=\"(.*?)\"\\s*?src=\"(?<img>.*?)\"\\s*?alt=\"(.*?)\"\\s*?href=\"(.*?)\"\\s*?/\\>";
        //internal String seriesListRegex = "\\<img.*?title=\"(?<title>.*?)\"\\s*?href=\".*?\"\\s*?src=\"(?<img>.*?)\"\\s*?alt=\"(.*?)\"\\s*?/>\\s*?\\<a.*?title=\"(.*?)\"\\s*?href=\"(?<url>.*?)\"\\s*?\\>\\s*?\\<h2\\>\\s*?\\<p\\>(?<label>.*?)\\</p\\>\\s*?\\</h2\\>\\s*?\\</a\\>";
        internal String seriesListRegex = "<li>\\s*<div>\\s*<a\\stitle=\"[^\"]*\"\\shref=\"(?<url>[^\"]*)\">\\s*<img\\stitle=\"(?<description>[^\"]*)\"\\shref=\"[^\"]*\"\\s*src=\"(?<thumb>[^\"]*)\"\\s*alt=\"[^\"]*\"\\s*/>\\s*<h2><p>(?<title>[^<]*)</p></h2>\\s*</a>\\s*</div>\\s*</li>";
        internal String videoXmlUrlRegex = "player_capitulo.xml='(?<url>.*?)';";
        internal String seriesSeasonContentRegex = "\\<dd class=\"paginador\"\\>(?<content>(\\s|\\S)*?)\\</dd\\>";
        internal String seriesSeasonRegex = "\\<li.*?\\>\\s*?\\<a\\s*?title=\"(.*?)\"\\s*?href=\"(?<url>.*?)\"\\s*?\\>\\s*?(?<label>.*?)\\s*?\\</a\\>\\s*?\\</li\\>";
        internal String videoUrlContentRegex = "\\<multimedias\\>\\s*\\<multimedia.*?\\>(?<content>(\\s|\\S)*?)\\</multimedia\\>";
        internal String videoBaseUrlHttpRegex = "\\<urlHttpVideo\\>\\s*\\<!\\[CDATA\\[(?<url>.*?)\\]\\]\\>\\s*?\\</urlHttpVideo\\>";
        internal String videoBaseUrlMp4Regex = "\\<urlVideoMp4\\>\\s*\\<!\\[CDATA\\[(?<url>.*?)\\]\\]\\>\\s*?\\</urlVideoMp4\\>";
        internal String videoBaseUrlFlvRegex = "\\<urlVideoFlv\\>\\s*\\<!\\[CDATA\\[(?<url>.*?)\\]\\]\\>\\s*?\\</urlVideoFlv\\>";
        internal String videoMovieUrlRegex = "\\<archivoMultimedia\\>\\s*\\<archivo\\>\\s*?\\<!\\[CDATA\\[(?<url>.*?)\\]\\]\\>\\</archivo\\>";
        internal String videoThumbUrlRegex = "\\<archivoMultimediaMaxi\\>\\s*\\<archivo\\>\\s*?\\<!\\[CDATA\\[(?<url>.*?)\\]\\]\\>\\</archivo\\>";
        //internal String programsListContentRegex = "\\<ul class=\"carrusel\"\\>(?<content>(\\s|\\S)*?)\\</ul\\>";
        internal String programsListContentRegex = "<li>\\s*<div>\\s*<a\\s*title=\"[^\"]*\"\\shref=\"(?<url>[^\"]*)\"\\s>\\s*<img\\stitle=\"[^\"]*\"\\s*src=\"(?<thumb>[^\"]*)\"\\s*alt=\"[^>]*>\\s*<h2><p>(?<title>[^<]*)</p></h2>\\s*</a>\\s*</div>\\s*</li>";
        internal String programsListRegex = "\\<li\\>\\s*\\<div\\>\\s*\\<a\\s+title=\"(?<title>(.*?))\"\\s+href=\"(?<url>(.*?))\"\\s*\\>\\s*\\<img\\s+title=\"(?<imgtitle>(.*?))\"\\s+src=\"(?<img>(.*?))\"(\\s|\\S)*?href=\"(?<imglink>(.*?))\"(\\s|\\S)*?\\<h2\\>\\s*\\<p\\>((?<label>.*?))\\</p\\>\\s*\\</h2\\>";
        internal String programsYearsContentRegex = "\\<dd\\s+class=\"seleccion\"\\>(?<content>(\\s|\\S)*?)\\</dd\\>";
        internal String programsYearsRegex = "\\<li.*?\\>\\s*\\<a\\s+title=\"(?<title>(.*?))\"\\s+href=\"(?<url>(.*?))\"\\s*\\>(?<label>(.*?))\\</a\\>\\s*\\</li\\>";
        internal String programsMonthsContentRegex = "\\<dd\\s+class=\"paginador\"\\>(?<content>(\\s|\\S)*?)\\</dd\\>";
        internal String programsMonthsRegex = "\\<li.*?\\>\\s*\\<a\\s+title=\"(?<title>(.*?))\"\\s+href=\"(?<url>(.*?))\"(\\s|\\S)*?\\>(?<label>(.*?))\\</a\\>\\s*\\</li\\>";
        internal String programContentRegex = "\\<ul class=\"carrusel carruDetalle\"\\>(?<content>(\\s|\\S)*?)\\</ul\\>";
        internal String programRegex = "\\<li\\>\\s*\\<div\\>\\s*\\<a\\s+title=\"(?<title>(.*?))\"\\s+href=\"(?<url>(.*?))\"\\s*\\>\\s*\\<img\\s+title=\"(?<imgtitle>(.*?))\"\\s+src=\"(?<img>(.*?))\"(\\s|\\S)*?href=\"(?<imglink>(.*?))\"(\\s|\\S)*?\\<h2\\>\\s*\\<p\\>((?<label>.*?))\\</p\\>\\s*\\</h2\\>";

        internal Regex regexCategory;
        internal Regex regexCategoryContent;
        internal Regex regexVideoLinks;
        internal Regex regexSeriesList;
        internal Regex regexSeriesSeasonContent;
        internal Regex regexSeriesSeason;
        internal Regex regexVideoXmlUrl;
        internal Regex regexVideoUrlContent;
        internal Regex regexVideoBaseUrlHttp;
        internal Regex regexVideoBaseUrlMp4;
        internal Regex regexVideoBaseUrlFlv;
        internal Regex regexVideoMovieUrl;
        internal Regex regexProgramsListContent;
        internal Regex regexProgramsList;
        internal Regex regexProgramsYearsContent;
        internal Regex regexProgramsYears;
        internal Regex regexProgramsMonthsContent;
        internal Regex regexProgramsMonths;
        internal Regex regexProgramContent;
        internal Regex regexProgram;

        internal enum CategoryType
        {
            None,
            Series,
            SeriesSeason,
            Program,
            ProgramYear,
            ProgramMonth
        }

        public override void Initialize(SiteSettings siteSettings)
        {
            InitializeRegex();
            siteSettings.DynamicCategoriesDiscovered = false;
            base.Initialize(siteSettings);
        }

        /*
 
                    String data = WebUtil.GetWebData(baseUrl + url);
                    Match match = regexVideoXmlUrl.Match(data);
                    if (match.Success)
                    {
                        data = WebUtil.GetWebData(baseUrl + match.Groups["url"].Value);
                        match = regexVideoBaseUrl.Match(data);
                        if (match.Success)
                        {
                            String videoBaseUrl = match.Groups["url"].Value;
                            match = regexVideoMovieUrl.Match(data);
                            while (match.Success)
                            {
                                String videoUrl = match.Groups["url"].Value;
                                Console.WriteLine(videoBaseUrl + videoUrl);
                                match = match.NextMatch();
                            }
                       }
                    }  
  
        */

        public override List<String> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            List<String> result = new List<String>();
            String data = GetWebData(antena3BaseUrl + video.VideoUrl);
            Match match = regexVideoXmlUrl.Match(data);
            if (match.Success)
            {
                Log.Debug("antena3: getting xml file from {0}", antena3BaseUrl + match.Groups["url"].Value);
                data = GetWebData(antena3BaseUrl + match.Groups["url"].Value);
                match = regexVideoBaseUrlHttp.Match(data);
                if (match.Success)
                {
                    String videoBaseUrl = match.Groups["url"].Value;
                    match = regexVideoMovieUrl.Match(data);
                    while (match.Success)
                    {
                        Log.Debug("antena3: adding {0} to playlist", match.Groups["url"].Value);
                        result.Add(videoBaseUrl + match.Groups["url"].Value);
                        match = match.NextMatch();
                    }
                }
            }
            return result;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            Log.Debug("antena3: getting video list for {0}.", category.Name);
            List<VideoInfo> videoList = new List<VideoInfo>();
            String data = GetWebData(antena3BaseUrl + (category as RssLink).Url);
            Match videoMatch = regexVideoLinks.Match(data);
            while (videoMatch.Success)
            {
                VideoInfo video = new VideoInfo();
                video.Title = videoMatch.Groups["title"].Value.Replace("Vídeos de ", "");
                video.ImageUrl = antena3BaseUrl + videoMatch.Groups["img"].Value;
                video.VideoUrl = videoMatch.Groups["url"].Value;
                videoList.Add(video);
                videoMatch = videoMatch.NextMatch();
            }
            return videoList;
        }

        internal void InitializeRegex()
        {
            regexCategoryContent = new Regex(categoryContentRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexCategory = new Regex(categoryRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexVideoLinks = new Regex(videoLinksRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexSeriesList = new Regex(seriesListRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexSeriesSeasonContent = new Regex(seriesSeasonContentRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexSeriesSeason = new Regex(seriesSeasonRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexVideoXmlUrl = new Regex(videoXmlUrlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexVideoUrlContent = new Regex(videoUrlContentRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexVideoBaseUrlHttp = new Regex(videoBaseUrlHttpRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexVideoBaseUrlFlv = new Regex(videoBaseUrlFlvRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexVideoBaseUrlMp4 = new Regex(videoBaseUrlMp4Regex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexVideoMovieUrl = new Regex(videoMovieUrlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexProgramsListContent = new Regex(programsListContentRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexProgramsList = new Regex(programsListRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexProgramsYearsContent = new Regex(programsYearsContentRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexProgramsYears = new Regex(programsYearsRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexProgramsMonthsContent = new Regex(programsMonthsContentRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexProgramsMonths = new Regex(programsMonthsRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexProgramContent = new Regex(programContentRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
            regexProgram = new Regex(programRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
        }

        internal String FormatMonthLabel(String fromLabel)
        {
            switch (fromLabel.ToLowerInvariant())
            {
                case "ene":
                    return "Enero";
                case "feb":
                    return "Febrero";
                case "mar":
                    return "Marzo";
                case "abr":
                    return "Abril";
                case "may":
                    return "Mayo";
                case "jun":
                    return "Junio";
                case "jul":
                    return "Julio";
                case "ago":
                    return "Agosto";
                case "sep":
                    return "Septiembre";
                case "oct":
                    return "Octubre";
                case "nov":
                    return "Noviembre";
                case "dic":
                    return "Diciembre";
                default:
                    return fromLabel;
            }
        }

        internal List<Category> DiscoverSeriesSeasons(RssLink parentCategory)
        {
            List<Category> result = new List<Category>();
            String data = GetWebData(antena3BaseUrl + parentCategory.Url);
            Match content = regexSeriesSeasonContent.Match(data);
            if (content.Success)
            {
                MatchCollection matches = regexSeriesSeason.Matches(content.Groups["content"].Value);
                parentCategory.SubCategories = new List<Category>();
                foreach (Match match in matches)
                {
                    String label = match.Groups["label"].Value;
                    String url = match.Groups["url"].Value;
                    String name = label.Length < 3 ? "Temporada " + label : label;
                    result.Add(CreateCategory(name, url, String.Empty, CategoryType.None,"", parentCategory));
                    Log.Debug("antena3: season {0} added.", name);
                }
            }
            return result;
        }

        internal List<Category> DiscoverSeries(RssLink parentCategory)
        {
            List<Category> result = new List<Category>();
            String data = GetWebData(antena3BaseUrl + parentCategory.Url);
            Match match = regexSeriesList.Match(data);
            while (match.Success)
            {
                String name = match.Groups["title"].Value;
                String url = match.Groups["url"].Value;
                String thumbUrl = antena3BaseUrl + match.Groups["thumb"].Value;
                result.Add(CreateCategory(name, url, thumbUrl, CategoryType.SeriesSeason,"", parentCategory));
                Log.Debug("antena3: series {0} added.", name);
                match = match.NextMatch();
            }
            return result;
        }

        internal List<Category> DiscoverProgramMonths(RssLink parentCategory)
        {
            List<Category> result = new List<Category>();
            String data = GetWebData(antena3BaseUrl + parentCategory.Url);
            Match content = regexProgramsMonthsContent.Match(data);
            if (content.Success)
            {
                String programMonthsContent = content.Groups["content"].Value;
                MatchCollection matches = regexProgramsMonths.Matches(programMonthsContent);
                foreach (Match match in matches)
                {
                    String name = FormatMonthLabel(match.Groups["label"].Value);
                    String url = match.Groups["url"].Value;
                    String thumbUrl = antena3BaseUrl + match.Groups["img"].Value;
                    result.Add(CreateCategory(name, url, thumbUrl, CategoryType.None,"", parentCategory));
                    Log.Debug("antena3: program month {0} added.", name);
                }
            }
            return result;
        }

        internal List<Category> DicoverProgramYears(RssLink parentCategory)
        {
            List<Category> result = new List<Category>();
            String data = GetWebData(antena3BaseUrl + parentCategory.Url);
            Match content = regexProgramsYearsContent.Match(data);
            if (content.Success)
            {
                String programYearsContent = content.Groups["content"].Value;
                MatchCollection matches = regexProgramsYears.Matches(programYearsContent);
                foreach (Match match in matches)
                {
                    String name = match.Groups["label"].Value;
                    String url = match.Groups["url"].Value;
                    String thumbUrl = antena3BaseUrl + match.Groups["img"].Value;
                    result.Add(CreateCategory(name, url, thumbUrl, CategoryType.ProgramMonth,"", parentCategory));
                    Log.Debug("antena3: program year {0} added.", name);
                }
            }
            return result;
        }

        internal List<Category> DiscoverPrograms(RssLink parentCategory)
        {
            List<Category> result = new List<Category>();
            String data = GetWebData(antena3BaseUrl + parentCategory.Url);
            Match match = regexProgramsListContent.Match(data);
            while (match.Success)
            {
                String name = match.Groups["title"].Value;
                String url = match.Groups["url"].Value;
                String thumbUrl = antena3BaseUrl + match.Groups["thumb"].Value;
                String description = match.Groups["description"].Value;
                result.Add(CreateCategory(name, url, thumbUrl, CategoryType.ProgramYear, description, parentCategory));
                Log.Debug("antena3: program {0} added.", name);
                match = match.NextMatch();
            }
            return result;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            //Log.Debug("antena3: discovering subcategory {0}", (CategoryType)parentCategory.Other);
            List<Category> subCategories = null;

            switch ((CategoryType)parentCategory.Other)
            {
                case CategoryType.Series:
                    subCategories = DiscoverSeries(parentCategory as RssLink);
                    break;
                case CategoryType.SeriesSeason:
                    subCategories = DiscoverSeriesSeasons(parentCategory as RssLink);
                    break;
                case CategoryType.Program:
                    subCategories = DiscoverPrograms(parentCategory as RssLink);
                    break;
                case CategoryType.ProgramYear:
                    subCategories = DicoverProgramYears(parentCategory as RssLink);
                    break;
                case CategoryType.ProgramMonth:
                    subCategories = DiscoverProgramMonths(parentCategory as RssLink);
                    break;
            }

            parentCategory.SubCategories = subCategories;
            parentCategory.SubCategoriesDiscovered = true;
            parentCategory.HasSubCategories = subCategories == null ? false : subCategories.Count > 0;

            return parentCategory.HasSubCategories ? subCategories.Count : 0;
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

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            Settings.Categories.Add(CreateCategory("Series", "/videos/series.html", String.Empty, CategoryType.Series,"", null));
            Settings.Categories.Add(CreateCategory("Noticias", "/videos/noticias.html", String.Empty, CategoryType.Program,"", null));
            Settings.Categories.Add(CreateCategory("Programas", "/videos/programas.html", String.Empty, CategoryType.Program,"", null));
            Settings.DynamicCategoriesDiscovered = true;

            return Settings.Categories.Count;
        }
    }
}

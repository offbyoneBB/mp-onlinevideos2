using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Web;

namespace OnlineVideos.Sites
{
    public class TVEUtil : SiteUtilBase
    {
        private string baseUrl = "http://www.rtve.es";
        private string subCategoryRegex = @"<li[^>]*>\s*<a\shref=""(?<url>[^""]+)""[^>]*>(?<title>[^<]*)<|<span>(?<title>[^<]*)|<div>(?<title>[^<]*)";
        private string videoListRegex = @"<li\sid=""video-(?<videoid>[^""]*).*?img\ssrc=""(?<thumb>[^""]*).*?<h3>[^>]*>(?<title>[^<]*)<.*?</h3>\s*<p>(?<descr>[^<]*)</p>.*?(Emitido:\s*(?<airdate>[^\[]*)\[(?<airtime>[^""]*))?""";
        private string urlRegex = @",file:""(?<url>[^""]+)""";
        private int pageNr = 1;
        private bool hasNextPage = false;
        string firstPageUrl;

        private Regex regEx_SubCategory;
        private Regex regEx_VideoList;
        private Regex regEx_GetUrl;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_SubCategory = new Regex(subCategoryRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_GetUrl = new Regex(urlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_VideoList = new Regex(videoListRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
        }
        public override int DiscoverDynamicCategories()
        {
            foreach (Category cat in Settings.Categories)
                cat.HasSubCategories = true;

            return base.DiscoverDynamicCategories();
        }
        public override int DiscoverSubCategories(Category parentCategory)
        {
            string url = ((RssLink)parentCategory).Url;
            List<Category> categories = new List<Category>();
            string webData = GetWebData(url);


            if (!string.IsNullOrEmpty(webData))
            {
                if (parentCategory.Other != null)
                    webData = GetSubString(webData, @"carta_herader", @"programas_carta");
                else
                    webData = GetSubString(webData, @"<div class=""menu_opciones"">", @"<ul class=""paginacion"">");

                Match m = regEx_SubCategory.Match(webData);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    cat.Url = m.Groups["url"].Value;
                    if (String.IsNullOrEmpty(cat.Url))
                        cat.Url = url;
                    else
                        cat.Url = baseUrl + cat.Url;

                    cat.HasSubCategories = (parentCategory.Other == null) && cat.Name != "Recomendados";
                    cat.ParentCategory = parentCategory;
                    cat.Other = 1;
                    categories.Add(cat);
                    m = m.NextMatch();
                }

                parentCategory.SubCategoriesDiscovered = true;
            }
            parentCategory.SubCategories = categories;
            return parentCategory.SubCategories.Count;
        }

        public override string getUrl(VideoInfo video)
        {
            string webData = GetWebData(video.VideoUrl);
            string url = GetSubString(webData, "<location>", "</location>");
            if (url.StartsWith("rtmp"))
                return string.Format("http://127.0.0.1:{0}/stream.flv?rtmpurl={1}",
                    OnlineVideoSettings.RTMP_PROXY_PORT, System.Web.HttpUtility.UrlEncode(url));
            else
                return url;

            /*if (url.StartsWith("rtmp"))
                return url.Replace(@"rtmp://stream.rtve.es/stream/resources", @"http://www.rtve.es/resources");
            return url;
             */
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            firstPageUrl = ((RssLink)category).Url;
            pageNr = 1;
            return getPagedVideoList(firstPageUrl);
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            pageNr++;
            return getPagedVideoList(firstPageUrl);
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            pageNr--;
            return getPagedVideoList(firstPageUrl);
        }

        private List<VideoInfo> getPagedVideoList(string url)
        {
            if (pageNr > 1)
                url = url + "?page=" + pageNr.ToString();
            string webData = GetWebData(url);
            hasNextPage = webData.Contains(@"<a title=""Adelante"" href=""");
            webData = GetSubString(webData, "programas_carta", @"<div id=""footer"">");
            List<VideoInfo> videos = new List<VideoInfo>();

            if (!string.IsNullOrEmpty(webData))
            {
                Match m = regEx_VideoList.Match(webData);
                while (m.Success)
                {
                    VideoInfo video = new VideoInfo();

                    video.Title = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    video.VideoUrl = baseUrl + "/alacarta/player/" + m.Groups["videoid"].Value + ".xml";
                    video.ImageUrl = baseUrl + m.Groups["thumb"].Value;
                    video.Description = HttpUtility.HtmlDecode(m.Groups["descr"].Value) + " " +
                        HttpUtility.HtmlDecode(m.Groups["airdate"].Value) + " " + HttpUtility.HtmlDecode(m.Groups["airtime"].Value);
                    video.Description = video.Description.Replace("\t", String.Empty);
                    video.Description = video.Description.Replace("\n", String.Empty);
                    videos.Add(video);
                    m = m.NextMatch();
                }
            }
            return videos;
        }

        public override bool HasPreviousPage
        {
            get { return pageNr > 1; }
        }

        public override bool HasNextPage
        {
            get { return hasNextPage; }
        }


        private string GetSubString(string s, string start, string until)
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

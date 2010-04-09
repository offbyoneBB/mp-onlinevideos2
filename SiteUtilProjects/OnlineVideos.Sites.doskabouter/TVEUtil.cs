using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Web;

namespace OnlineVideos.Sites
{
    public class TVEUtil : GenericSiteUtil
    {
        private string subCategoryRegex = @"<li[^>]*>\s*<a\shref=""(?<url>[^""]+)""[^>]*>(?<title>[^<]*)<|<span>(?<title>[^<]*)|<div>(?<title>[^<]*)";

        private Regex regEx_SubCategory;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_SubCategory = new Regex(subCategoryRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
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
                    cat.Name = Utils.PlainTextFromHtml(m.Groups["title"].Value);
                    cat.Url = m.Groups["url"].Value;
                    if (String.IsNullOrEmpty(cat.Url))
                        cat.Url = url;
                    else
                        cat.Url = baseUrl + cat.Url;

                    cat.HasSubCategories = (parentCategory.Other == null) && !cat.Url.Equals(url);
                        //cat.Name != "Recomendados";
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
            string videoName = baseUrl + "/alacarta/player/" + Path.ChangeExtension(Path.GetFileName(video.VideoUrl), ".html");
            string webData = GetWebData(videoName);
            string url = GetSubString(webData, "'url':'", "'");
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

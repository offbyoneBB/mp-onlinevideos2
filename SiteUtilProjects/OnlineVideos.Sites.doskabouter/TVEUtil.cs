using System;
using System.Collections.Generic;
using System.Text;
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

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> res = base.getVideoList(category);
            foreach (VideoInfo video in res)
                video.Length = Regex.Replace(video.Length, @"[\t\r\n]", String.Empty, RegexOptions.Multiline);
            return res;
        }
        public override string getUrl(VideoInfo video)
        {
            // copy from http://code.google.com/p/xbmc-tvalacarta/source/browse/trunk/tvalacarta/tvalacarta/channels/rtve.py
            string videoId = Path.ChangeExtension(Path.GetFileName(video.VideoUrl), String.Empty).TrimEnd('.');
            StringBuilder sb = new StringBuilder();
            for (int i = 0, j = videoId.Length - 1; i < 4; i++, j--)
            {
                sb.Append(videoId[j]);
                sb.Append('/');
            }
            sb.Append(videoId);
            sb.Append(".xml");
            string webData = GetWebData(baseUrl + @"/swf/data/es/videos/alacarta/" + sb.ToString());
            string res = GetSubString(webData, @"<file>", @"</file>");
            if (!String.IsNullOrEmpty(res))
                return res;

            webData = GetWebData(baseUrl + @"/swf/data/es/videos/video/" + sb.ToString());
            res = GetSubString(webData, @"<file>", @"</file>");
            if (!String.IsNullOrEmpty(res))
                return res;
            string assetID = GetSubString(webData, @"assetDataId::", @"""");

            sb = new StringBuilder();
            sb.Append(baseUrl);
            sb.Append(@"/scd/CONTENTS/ASSET_DATA_VIDEO/");
            for (int i = 0, j = assetID.Length - 1; i < 4; i++, j--)
            {
                sb.Append(assetID[j]);
                sb.Append('/');
            }
            sb.Append(@"ASSET_DATA_VIDEO-");
            sb.Append(assetID);
            sb.Append(".xml");
            webData = GetWebData(sb.ToString());
            Match m = Regex.Match(webData, @"<key>ASD_FILE</key>\s*<value>(?<url>[^<]*)<");
            if (m.Success)
                res = baseUrl + @"/resources/TE_NGVA" + m.Groups["url"].Value.Substring(@"/deliverty/demo/resources".Length);
            return res;
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

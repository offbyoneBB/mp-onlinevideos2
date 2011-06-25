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
        private Regex regEx_SubCategory;
        private Regex regEx_SubSubCategory;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            regEx_SubSubCategory = regEx_dynamicSubCategories;
            regEx_SubCategory = regEx_dynamicCategories;
            regEx_dynamicCategories = null;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (parentCategory.ParentCategory == null)
                regEx_dynamicSubCategories = regEx_SubCategory;
            else
                regEx_dynamicSubCategories = regEx_SubSubCategory;

            int res = base.DiscoverSubCategories(parentCategory);

            if (res != 0 && parentCategory.ParentCategory == null)
                foreach (Category cat in parentCategory.SubCategories)
                    cat.HasSubCategories = true;

            return res;
        }

        private string hash(string s)
        {
            int l = s.Length;
            if (l < 4)
                return s;
            return String.Format("{0}/{1}/{2}/{3}", s[l - 1], s[l - 2], s[l - 3], s[l - 4]);
        }

        public override string getUrl(VideoInfo video)
        {
            //http://www.rtve.es/alacarta/videos/amar-en-tiempos-revueltos/amar-tiempos-revueltos-t6-capitulos-211-212/1137920/
            string[] parts = video.VideoUrl.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            string url = String.Format(@"http://www.rtve.es/swf/data/es/videos/video/{0}/{1}.xml",
                hash(parts[parts.Length - 1]), parts[parts.Length - 1]);
            string webData = GetWebData(url);
            string assetId = GetSubString(webData, @"assetDataId::", @"""");

            string url2 = String.Format(@"http://www.rtve.es/scd/CONTENTS/ASSET_DATA_VIDEO/{0}/ASSET_DATA_VIDEO-{1}.xml",
                hash(assetId), assetId);

            webData = GetWebData(url2);
            Match m = Regex.Match(webData, @"<key>ASD_FILE</key>\s*<value>/deliverty/demo/resources/(?<url>[^<]*)</value>");
            if (m.Success)
                return baseUrl + @"/resources/TE_NGVA/" + m.Groups["url"].Value;
            return null;
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

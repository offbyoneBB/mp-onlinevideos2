using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Web;
using OnlineVideos.MPUrlSourceFilter;

namespace OnlineVideos.Sites
{
    public class TVEUtil : GenericSiteUtil
    {
        [Category("OnlineVideosConfiguration")]
        protected string generosRegEx;

        private Regex regEx_SubCategory;
        private Regex regEx_SubSubCategory;
        private Regex regEx_Generos;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            regEx_SubSubCategory = regEx_dynamicSubCategories;
            regEx_SubCategory = regEx_dynamicCategories;
            regEx_dynamicCategories = null;
            regEx_Generos = new Regex(generosRegEx, defaultRegexOptions);
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (parentCategory.Name == "Generos")
                regEx_dynamicSubCategories = regEx_Generos;
            else
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
            if ("livestream".Equals(video.Other))
            {
                string data = GetWebData(video.VideoUrl);
                Match m2 = Regex.Match(data, @"<param\sname=""flashvars""\svalue=""assetID=(?<assetid>[^_]*)_");
                if (m2.Success)
                {
                    data = GetWebData(String.Format(@"http://www.rtve.es/api/videos/{0}/config/portada.json", m2.Groups["assetid"].Value));
                    m2 = Regex.Match(data, @"""file"":""(?<url>[^""]*)""");
                    if (m2.Success)
                    {
                        RtmpUrl rtmpUrl = new RtmpUrl(m2.Groups["url"].Value)
                        {
                            Live = true,
                            SwfUrl = @"http://www.rtve.es/swf/4.0.32/RTVEPlayerVideo.swf"
                        };
                        return rtmpUrl.ToString();
                    }
                }
                return null;
            }

            //copied from http://code.google.com/p/pydowntv/:
            //http://www.rtve.es/alacarta/videos/amar-en-tiempos-revueltos/amar-tiempos-revueltos-t6-capitulos-211-212/1137920/
            string[] parts = video.VideoUrl.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            return GetRedirectedUrl(String.Format(@"http://www.rtve.es/ztnr/consumer/xl/video/alta/{0}_es_292525252525111", parts[parts.Length - 1]));
            /*string url = String.Format(@"http://www.rtve.es/swf/data/es/videos/video/{0}/{1}.xml",
                hash(parts[parts.Length - 1]), parts[parts.Length - 1]);
            string webData = GetWebData(url);
            string assetId = GetSubString(webData, @"assetDataId::", @"""");

            string url2 = String.Format(@"http://www.rtve.es/scd/CONTENTS/ASSET_DATA_VIDEO/{0}/ASSET_DATA_VIDEO-{1}.xml",
                hash(assetId), assetId);

            webData = GetWebData(url2);
            Match m = Regex.Match(webData, @"<key>ASD_FILE</key>\s*<value>/deliverty/demo/resources/(?<url>[^<]*)</value>");
            if (m.Success)
                return baseUrl + @"/resources/TE_NGVA/" + m.Groups["url"].Value;
            // get ipad url
            webData = GetWebData(video.VideoUrl, userAgent: "Mozilla/5.0 (iPad; U; CPU OS 3_2 like Mac OS X; en-us) AppleWebKit/531.21.10 (KHTML, like Gecko) Version/4.0.4 Mobile/7B334b Safari/531.21.10");
            m = Regex.Match(webData, @"<a\shref=""/usuarios/sharesend.shtml\?urlContent\=(?<url>[^""]+)"" target");
            if (m.Success)
            {
                return new Uri(new Uri(baseUrl), m.Groups["url"].Value).AbsoluteUri;
            }
            return null;
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
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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

        public override List<VideoInfo> GetVideos(Category category)
        {
            string url = ((RssLink)category).Url;
            if (url.StartsWith(@"http://www.rtve.es/infantil", StringComparison.InvariantCultureIgnoreCase))
            {
                string webData = GetWebData(url);
                Match m = Regex.Match(webData, @"data-attribute=""(?<url>[^""]*)""\stitle=""Episodios""");
                if (m.Success)
                    url = FormatDecodeAbsolutifyUrl(url, m.Groups["url"].Value, null, UrlDecoding.None) + @"&de=S&ll=N&stt=S&fakeImg=S";
            }
            return Parse(url, null);
        }

        public override string GetVideoUrl(VideoInfo video)
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

            //source: http://code.google.com/p/xbmc-tvalacarta/source/browse/trunk/tvalacarta/servers/rtve.py:
            // thanks to aabilio and tvalacarta
            string[] parts = video.VideoUrl.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            string data2 = GetWebData(String.Format(@"http://www.rtve.es/ztnr/movil/thumbnail/anubis/videos/{0}.png", parts[parts.Length - 1]), referer: @"http://www.rtve.es");
            data2 = Encoding.ASCII.GetString(Convert.FromBase64String(data2));
            Match mm = Regex.Match(data2, ".*tEXt(?<cypher>.*)#[\x00]*(?<key>[0-9]*).*");
            if (mm.Success)
            {
                string cypher = mm.Groups["cypher"].Value;
                string key = mm.Groups["key"].Value;
                string int_cypher = "";
                int inc = 1;
                int ti = 0;
                while (ti < cypher.Length)
                {
                    ti = ti + inc;
                    if (ti > 0 && ti <= cypher.Length)
                        int_cypher += cypher[ti - 1];
                    inc++;
                    if (inc == 5) inc = 1;
                }

                string plaintext = "";
                int key_ind = 0;
                inc = 4;
                while (key_ind < key.Length)
                {
                    key_ind++;
                    ti = ((byte)key[key_ind - 1] - 48) * 10;
                    key_ind += inc;
                    if (key_ind <= key.Length)
                        ti += (byte)key[key_ind - 1] - 48;
                    ti++;
                    inc++;
                    if (inc == 5) inc = 1;
                    if (ti > 0 && ti <= int_cypher.Length)
                        plaintext += int_cypher[ti - 1];
                }
                HttpUrl res = new HttpUrl(plaintext.Replace("www.rtve.es", "media5.rtve.es"));
                res.Cookies.Add(new Cookie("odin", "odin=banebdyede"));
                return res.ToString();
            }

            return null;
        }
    }
}
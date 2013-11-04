using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.ComponentModel;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Web;

namespace OnlineVideos.Sites
{
    public class AtresplayerUtil : GenericSiteUtil
    {

        [Category("OnlineVideosConfiguration")]
        protected string categoriasRegEx;

        [Category("OnlineVideosConfiguration")]
        protected string menuAZRegEx;

        [Category("OnlineVideosConfiguration")]
        protected string videoIDRegEx;

        [Category("OnlineVideosConfiguration")]
        protected string nombreMenuAZ;

        [Category("OnlineVideosConfiguration")]
        protected string todosURL;

        internal enum CategoryType
        {
            None,
            menuAZ,
            subMenuAZ,
            general
        }

        public override int DiscoverDynamicCategories()
        {
            foreach (Category c in Settings.Categories)
            {
                if (c.Name != nombreMenuAZ)
                {
                    c.Other = CategoryType.general;
                    c.HasSubCategories = true;
                }
                else
                {
                    c.Other = CategoryType.menuAZ;
                    c.HasSubCategories = true;
                }
            }
            Settings.DynamicCategoriesDiscovered = true;

            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            List<Category> subCategories = null;

            switch ((CategoryType)parentCategory.Other)
            {
                case CategoryType.general:
                    subCategories = DiscoverGeneral(parentCategory as RssLink);
                    break;
                case CategoryType.menuAZ:
                    subCategories = DiscoverMenuAZ(parentCategory as RssLink);
                    break;
                case CategoryType.subMenuAZ:
                    subCategories = DiscoverSubMenuAZ(parentCategory as RssLink);
                    break;
            }

            parentCategory.SubCategories = subCategories;
            parentCategory.SubCategoriesDiscovered = true;
            parentCategory.HasSubCategories = subCategories == null ? false : subCategories.Count > 0;

            return parentCategory.HasSubCategories ? subCategories.Count : 0;
        }

        internal List<Category> DiscoverMenuAZ(RssLink parentCategory)
        {
            List<Category> result = new List<Category>();
            String programsData = GetWebData(parentCategory.Url);
            regEx_dynamicSubCategories = new Regex(menuAZRegEx, defaultRegexOptions);
            Match match = regEx_dynamicSubCategories.Match(programsData);
            while (match.Success)
            {
                String name = match.Groups["title"].Value;
                String url = match.Groups["url"].Value;
                result.Add(CreateCategory(name, url, "", CategoryType.subMenuAZ, "", parentCategory));
                match = match.NextMatch();
            }
            return result;
        }

        internal List<Category> DiscoverSubMenuAZ(RssLink parentCategory)
        {
            List<Category> result = new List<Category>();
            String programsData = GetWebData(todosURL);
            JObject jsonProgramas = JObject.Parse("{\"a\":" + programsData + "}");
            JArray programas = (JArray)jsonProgramas["a"];
            int ignoreMe;
            for (int k = 0; k < programas.Count; k++)
            {
                JToken programa = programas[k];
                String letra = (String)programa.SelectToken("letra");
                if ("".Equals(parentCategory.Url) || (("?" + letra).ToUpper().Equals(parentCategory.Url.ToUpper()) || "?0".Equals(parentCategory.Url) && int.TryParse(letra, out ignoreMe)))
                {
                    String name = (String)programa.SelectToken("titulo");
                    String url = baseUrl + (String)programa.SelectToken("href") + "/carousel.json";
                    String thumbUrl = baseUrl + (String)programa.SelectToken("img");
                    result.Add(CreateCategory(name, url, thumbUrl, CategoryType.None, "", parentCategory));
                }
            }
            return result;
        }

        internal List<Category> DiscoverGeneral(RssLink parentCategory)
        {
            List<Category> result = new List<Category>();
            String programsData = GetWebData(parentCategory.Url);
            regEx_dynamicSubCategories = new Regex(categoriasRegEx, defaultRegexOptions);
            Match match = regEx_dynamicSubCategories.Match(programsData);
            while (match.Success)
            {
                String name = match.Groups["title"].Value;
                String url =  match.Groups["url"].Value + "/carousel.json";
                String thumbUrl = baseUrl + match.Groups["thumb"].Value;
                result.Add(CreateCategory(name, url, thumbUrl, CategoryType.None, "", parentCategory));
                match = match.NextMatch();
            }
            return result;
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

        public override List<VideoInfo> getVideoList(Category category)
        {
            RssLink parentCategory = category as RssLink;
            List<VideoInfo> videoList = new List<VideoInfo>();
            String data = GetWebData(parentCategory.Url);
            JObject jsonCapitulos = JObject.Parse("{\"a\":" + data + "}");
            JArray capitulos = (JArray)jsonCapitulos["a"];
            for (int k = 0; k < capitulos.Count; k++)
            {
                JToken capitulo = capitulos[k];
                String tipo = (String)capitulo.SelectToken("icono");
                if (tipo.Equals("preestreno"))
                {
                    tipo = "PREESTRENO (DE PAGO)";
                }
                else if (tipo.Equals("user"))
                {
                    tipo = "REQUIERE REGISTRO (GRATUITO)";
                }
                else if (tipo.Equals("premium"))
                {
                    tipo = "PREMIUM (DE PAGO)";
                }
                VideoInfo video = new VideoInfo();
                video.Title = (String)capitulo.SelectToken("title") + (tipo.Equals("") ? "" : " - " + tipo);
                video.VideoUrl = (String)capitulo.SelectToken("hrefHtml");
                video.ImageUrl = baseUrl + (String)capitulo.SelectToken("srcImage");
                video.Description = tipo;
                //video.Airdate = "";
                videoList.Add(video);

            }
            return videoList;
        }

        public override string getUrl(VideoInfo video)
        {
            String videoURL = "";
            String data = GetWebData(video.VideoUrl);
            regEx_dynamicSubCategories = new Regex(videoIDRegEx, defaultRegexOptions);
            Match match = regEx_dynamicSubCategories.Match(data);
            if (match.Success)
            {
                String videoID = match.Groups["videoID"].Value;
                String token = HttpUtility.UrlEncode(getToken(videoID, "puessepavuestramerced"));
                String auxURL = String.Format("https://servicios.atresplayer.com/api/urlVideo/{0}/{1}/{2}", videoID, "android_tablet", token);
                String datos = GetWebData(auxURL);
                JObject datosJSON = JObject.Parse(datos);
                videoURL = (String)datosJSON["resultObject"]["es"];
            }
            return videoURL;
        }

        public String getToken(String videoID, String password)
        {
            long l = 3000L + getApiTime();
            String hash = getHash(videoID + l, password);
            return videoID + ("|" + l + "|" + hash).ToLower();
            
        }

        public long getApiTime()
        {
            String stime = GetWebData("http://servicios.atresplayer.com/api/admin/time");
            return long.Parse(stime) / 1000L;
        }

        public String getHash(String s, String password)
        {
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            byte[] keyByte = encoding.GetBytes(password);
            HMACMD5 hmacmd5 = new HMACMD5(keyByte);
            byte[] messageBytes = encoding.GetBytes(s);
            byte[] hashmessage = hmacmd5.ComputeHash(messageBytes);
            string HMACMd5Value = ByteToString(hashmessage);
            return HMACMd5Value;
        }

        private static string ByteToString(byte[] buff)
        {
            string sbinary = "";
            for (int i = 0; i < buff.Length; i++)
            {
                sbinary += buff[i].ToString("X2");
            }
            return (sbinary);
        }

    }
}

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
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using OnlineVideos.AMF;
using OnlineVideos.MPUrlSourceFilter;

namespace OnlineVideos.Sites
{
    public class EITBUtil : GenericSiteUtil
    {
        [Category("OnlineVideosConfiguration")]
        protected string menu6;
        [Category("OnlineVideosConfiguration")]
        protected string menu7;

        [Category("OnlineVideosConfiguration")]
        protected string videoUrlService;
        [Category("OnlineVideosConfiguration")]
        protected string seasonUrlService;
        [Category("OnlineVideosConfiguration")]
        protected string playlistUrlService;

        [Category("OnlineVideosConfiguration")]
        protected string nameLanguage;
        [Category("OnlineVideosConfiguration")]
        protected string descriptionLanguage;
        
        internal enum CategoryType
        {
            None,
            submenu,
            seasons
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
            foreach (Category c in Settings.Categories)
            {
                if (c.Name == menu6 || c.Name == menu7)
                {
                    c.Other = CategoryType.None;
                    c.HasSubCategories = false;
                }
                else
                {
                    c.Other = CategoryType.submenu;
                    c.HasSubCategories = true;
                }
            }
            Settings.DynamicCategoriesDiscovered = true;

            return Settings.Categories.Count;
        }
               
        public override int DiscoverSubCategories(Category parentCategory)
        {
            List<Category> subCategories = DiscoverSubmenu(parentCategory as RssLink, regEx_dynamicSubCategories);
            parentCategory.SubCategories = subCategories;
            parentCategory.SubCategoriesDiscovered = true;
            parentCategory.HasSubCategories = subCategories == null ? false : subCategories.Count > 0;

            return parentCategory.HasSubCategories ? subCategories.Count : 0;
        }

        internal List<Category> DiscoverSubmenu(RssLink parentCategory, Regex regexp)
        {
            List<Category> result = new List<Category>();
            String data = GetWebData(parentCategory.Url);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(data);
            XmlNodeList elemList = xmlDoc.GetElementsByTagName("prog");
            foreach (XmlNode node in elemList)
            {
                String name = node.InnerText;
                CategoryType category = CategoryType.None;
                String url = playlistUrlService + node.Attributes["pls"].Value;
                if (parentCategory.Other.Equals(CategoryType.submenu) && node.Attributes["show_seasons"].Value.Equals("1"))
                {
                    category =  CategoryType.seasons;
                    url = seasonUrlService + node.Attributes["id"].Value;
                }
                String thumbUrl = node.Attributes["img"] != null ? node.Attributes["img"].Value : null;
                result.Add(CreateCategory(name, url, thumbUrl, category, "", parentCategory));
            }
            return result;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();
            String url = (category as RssLink).Url;
            String data = GetWebData(url);
            JObject jsonEpisodios = JObject.Parse(data);
            JArray episodios = (JArray)jsonEpisodios["web_media"];
            foreach (JToken episodio in episodios)
            {
                VideoInfo video = new VideoInfo();
                video.Title = (String)episodio.SelectToken(nameLanguage);
                video.Description = (String)episodio.SelectToken(descriptionLanguage);
                video.Airdate = (String)episodio.SelectToken("BROADCST_DATE");
                TimeSpan ts = TimeSpan.FromMilliseconds(double.Parse(episodio.SelectToken("LENGTH").ToString()));
                TimeSpan tsAux = TimeSpan.FromMilliseconds(ts.Milliseconds);
                video.Length = ts.Subtract(tsAux).ToString();
                video.Thumb = (String)episodio.SelectToken("THUMBNAIL_URL");
                video.VideoUrl = videoUrlService + episodio.SelectToken("ID").ToString();
                videoList.Add(video);
            }
            return videoList;
        }

        static DateTime ConvertFromUnixTimestamp(double timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return origin.AddMilliseconds(timestamp);
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string webdata = GetWebData(video.VideoUrl);
            return GetFileUrl(video, webdata);
        }

        protected string GetFileUrl(VideoInfo video, string data)
        {
            string mediaData = GetWebData(video.VideoUrl);
            JArray renditions = (JArray)JObject.Parse(mediaData).SelectToken("web_media")[0].SelectToken("RENDITIONS");
            video.PlaybackOptions = new Dictionary<string, string>();
            foreach (JObject rendition in renditions.OrderBy(u => int.Parse(u["ENCODING_RATE"].ToString())))
            {
                string nm = String.Format("{0}x{1} {2}K", rendition["FRAME_WIDTH"], rendition["FRAME_HEIGHT"], int.Parse(rendition["ENCODING_RATE"].ToString()) / 1024);
                string url = HttpUtility.UrlDecode(rendition["PMD_URL"].ToString());
                video.PlaybackOptions.Add(nm, url);
            }

            if (video.PlaybackOptions.Count == 0) return "";// if no match, return empty url -> error
            else
                if (video.PlaybackOptions.Count == 1)
                {
                    string resultUrl = video.PlaybackOptions.Last().Value;
                    video.PlaybackOptions = null;// only one url found, PlaybackOptions not needed
                    return resultUrl;
                }
                else
                {
                    return video.PlaybackOptions.Last().Value;
                }
        }

    }
    
    
}

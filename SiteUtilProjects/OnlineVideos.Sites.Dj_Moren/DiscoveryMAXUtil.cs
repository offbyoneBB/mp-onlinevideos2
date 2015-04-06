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
using OnlineVideos.AMF;
using OnlineVideos.MPUrlSourceFilter;

namespace OnlineVideos.Sites
{
    public class DiscoveryMAXUtil : BrightCoveUtil
    {

        [Category("OnlineVideosConfiguration")]
        protected string categoriasRegEx;

        [Category("OnlineVideosConfiguration")]
        protected string id_categoriasRegEx;

        [Category("OnlineVideosConfiguration")]
        protected string secciones_categoriasRegEx;

        [Category("OnlineVideosConfiguration")]
        protected string menuAZRegEx;

        [Category("OnlineVideosConfiguration")]
        protected string nombreMenuAZ;

        [Category("OnlineVideosConfiguration")]
        protected string videosRegEx;

        internal enum CategoryType
        {
            None,
            menuAZ,
            subMenuAZ,
            categorias,
            temporadas
        }

        public override int DiscoverDynamicCategories()
        {
            foreach (Category c in Settings.Categories)
            {
                if (c.Name == nombreMenuAZ)
                {
                    c.Other = CategoryType.menuAZ;
                    c.HasSubCategories = true;
                }
                else
                {
                    c.Other = CategoryType.categorias;
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
                case CategoryType.categorias:
                    subCategories = DiscoverCategorias(parentCategory as RssLink);
                    break;
                case CategoryType.menuAZ:
                    subCategories = DiscoverMenuAZ(parentCategory as RssLink);
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
                result.Add(CreateCategory(name, url, "", CategoryType.None, "", parentCategory));
                match = match.NextMatch();
            }
            return result;
        }

        internal List<Category> DiscoverCategorias(RssLink parentCategory)
        {
            List<Category> result = new List<Category>();
            String programsData = GetWebData(parentCategory.Url);
            regEx_dynamicSubCategories = new Regex(categoriasRegEx, defaultRegexOptions);
            Match match = regEx_dynamicSubCategories.Match(programsData);
            while (match.Success)
            {
                String name = match.Groups["title"].Value;
                String url = baseUrl + "?" + match.Groups["url"].Value;
                result.Add(CreateCategory(name, url, "", CategoryType.None, "", parentCategory));
                match = match.NextMatch();
            }
            return result;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            RssLink parentCategory = category as RssLink;
            List<VideoInfo> videoList = new List<VideoInfo>();
            String data = GetWebData(parentCategory.Url);
            string inicioUrl = parentCategory.Url;
            // Si estamos filtrando por categorías elegimos el div de la categoría
            if ((CategoryType)category.ParentCategory.Other == CategoryType.categorias)
            {
                inicioUrl = baseUrl;
                string id_categoria = parentCategory.Url.Substring(parentCategory.Url.LastIndexOf("/") + 2);
                regEx_dynamicSubCategories = new Regex(secciones_categoriasRegEx.Replace("ID_CATEGORIA", id_categoria), defaultRegexOptions);
                Match matchCat = regEx_dynamicSubCategories.Match(data);
                if (matchCat.Success)
                {
                    data = matchCat.Groups["contenido"].Value;
                }
            }
            regEx_dynamicSubCategories = new Regex(videosRegEx, defaultRegexOptions);
            Match match = regEx_dynamicSubCategories.Match(data);
            while (match.Success)
            {
                VideoInfo video = new VideoInfo();
                video.Title = match.Groups["title"].Value;
                video.VideoUrl = inicioUrl + match.Groups["url"].Value;
                video.Description = match.Groups["description"].Value;
                video.Thumb = match.Groups["thumb"].Value;
                videoList.Add(video);
                match = match.NextMatch();
            }
            return videoList;
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

        //********************************************************************************************************************************
        //****************************************************** Brightcove util *********************************************************
        //********************************************************************************************************************************

        public override string GetVideoUrl(VideoInfo video)
        {
            string webdata = GetWebData(video.VideoUrl);
            return GetFileUrl(video, webdata);
        }

        protected new string GetFileUrl(VideoInfo video, string data)
        {
            Match m = regEx_FileUrl.Match(data);

            if (!m.Success)
                return String.Empty;

            AMFArray renditions = GetResultsFromFindByMediaId(m, video.VideoUrl);

            return FillPlaybackOptions(video, renditions, m);
        }


        protected AMFArray GetResultsFromFindByMediaId(Match m, string videoUrl)
        {
            AMFSerializer ser = new AMFSerializer();
            object[] values = new object[4];
            values[0] = hashValue;
            values[1] = Convert.ToDouble(m.Groups["experienceId"].Value);
            values[2] = Convert.ToDouble(videoUrl.Substring(videoUrl.LastIndexOf("/") + 2));
            values[3] = Convert.ToDouble(array4);
            byte[] data = ser.Serialize2("com.brightcove.player.runtime.PlayerMediaFacade.findMediaById", values, AMFVersion.AMF3);
            AMFObject obj = AMFObject.GetResponse(requestUrl + m.Groups["playerKey"].Value, data);
            return obj.GetArray("renditions");
        }

        protected new string FillPlaybackOptions(VideoInfo video, AMFArray renditions, Match m)
        {
            video.PlaybackOptions = new Dictionary<string, string>();

            foreach (AMFObject rendition in renditions.OrderBy(u => u.GetIntProperty("encodingRate")))
            {
                string nm = String.Format("{0}x{1} {2}K",
                    rendition.GetIntProperty("frameWidth"), rendition.GetIntProperty("frameHeight"),
                    rendition.GetIntProperty("encodingRate") / 1024);
                string url = HttpUtility.UrlDecode(rendition.GetStringProperty("defaultURL"));
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

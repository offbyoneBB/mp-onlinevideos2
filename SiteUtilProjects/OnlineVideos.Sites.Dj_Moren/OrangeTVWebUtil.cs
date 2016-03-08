using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.ComponentModel;
using HtmlAgilityPack;
using System.Web;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Specialized;
using OnlineVideos.Helpers;

namespace OnlineVideos.Sites.BrowserUtilConnectors
{

    public class OrangeTVWebUtil : SiteUtilBase, IBrowserSiteUtil
    {

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Altura del vídeo"), Description("Altura en píxeles del cuadro del vídeo")]
        string alturaVideo = "1049";
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Tiempo delay lanzamiento script"), Description("Delay de lanzamiento de script")]
        string scriptDelayTime = "1000";

        public override int DiscoverDynamicCategories()
        {   
            RssLink cat = CreateCategory("Ahora", "https://orangetv.orange.es", null, CategoryType.None, "", null);
            Settings.Categories.Add(cat);            
            Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > 0;
            return Settings.Categories.Count;
            
        }

        internal enum CategoryType
        {
            None
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

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            String sessionIdData = GetWebData("https://orangetv.orange.es/atv/api/anonymous?appId=es.orange.pc&appVersion=1.0");
            String sessionId = (String)JObject.Parse(sessionIdData).SelectToken("sessionId");
            NameValueCollection headers = new NameValueCollection();
            headers.Add("Accept", "*/*");
            headers.Add("Accept-Charset", "ISO-8859-1,utf-8;q=0.7,*;q=0.3");
            headers.Add("Accept-Encoding", "gzip,deflate,sdch");
            headers.Add("Accept-Language", "es-ES,es;q=0.8");
            headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.1 (KHTML, like Gecko) Chrome/21.0.1180.89 Safari/537.1");
            headers.Add("X-Aspiro-TV-Session", sessionId);
            String canalesData = GetWebData("https://orangetv.orange.es/atv/api/epg?from=now&offset=-2h&duration=16h&view=epg&byTags=SmoothStreaming%40sourceType", headers: headers);
            JArray canalesJson = JArray.Parse(canalesData);
            for (int i = 0; i < canalesJson.Count; i++)
            {
                JToken canal = canalesJson[i];
                VideoInfo video = new VideoInfo();
                video.Title = (String)canal["title"];
                video.Thumb = (String)canal["images"]["LOGO"];
                video.VideoUrl = "https://orangetv.orange.es/#!channel/" + (String)canal["id"] + "/play";
                videos.Add(video);
            }
            return videos;
        }


        #region IBrowserSiteUtil
        string IBrowserSiteUtil.ConnectorEntityTypeName
        {
            get
            {
                return "OnlineVideos.Sites.BrowserUtilConnectors.OrangeTVConnector";
            }
        }

        string IBrowserSiteUtil.UserName
        {
            get
            {
                return alturaVideo;
            }
        }

        string IBrowserSiteUtil.Password
        {
            get
            {
                return scriptDelayTime;
            }
        }
        #endregion

    }
}

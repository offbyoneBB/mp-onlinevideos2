using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class Sat1GroupUtil : SiteUtilBase
    {
        //User Settings
        int past = 10;
        int future = 1;

        [Category("OnlineVideosConfiguration"), Description("Url used for Category json-Query.")]
        string baseUrl;
        [Category("OnlineVideosConfiguration"), Description("Url used for Videolist json-Query")]
        string categoryUrlFormatString;
        [Category("OnlineVideosConfiguration"), Description("Url to rtmp Server")]
        string rtmpBase;

        //Private Variables
        string start = "";
        string end = "";

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            DateTime startDate = DateTime.Today.Subtract(DateTime.Today.AddMonths(past).Subtract(DateTime.Today));
            DateTime endDate = DateTime.Today.AddMonths(future);
            start = string.Format("{0:00}{1:00}{2:00}", startDate.Year - 2000, startDate.Month, startDate.Day);
            end = string.Format("{0:00}{1:00}{2:00}", endDate.Year - 2000, endDate.Month, endDate.Day);
        }
        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            JObject contentData = GetWebDataAsJson(baseUrl);
            if (contentData != null)
            {
                foreach (var jObject in contentData)
                {
                    if (jObject.Key.Contains("categoryList"))
                    {
                        foreach (var jSubObject in jObject.Value)
                        {
                            RssLink cat = new RssLink();
                            cat.Name = jSubObject.Value<string>("name");
                            cat.Description = jSubObject.Value<string>("description");
                            if (jSubObject["thumbnail"] != null)
                                cat.Thumb = jSubObject["thumbnail"].Value<string>("thumb_url");
                            cat.Url = string.Format(categoryUrlFormatString, jSubObject.Value<string>("id"), start, end);

                            if (jSubObject["clipList"] as JArray != null)
                            {                                
                                cat.EstimatedVideoCount = (uint)(jSubObject["clipList"] as JArray).Count;
                            }

                            if (jSubObject["categoryList"] as JArray != null)
                            {                                
                                cat.SubCategories = new List<Category>();

                                foreach (var jSubCategoryObject in jSubObject["categoryList"] as JArray)
                                {
                                    RssLink subCategory = new RssLink();
                                    subCategory.ParentCategory = cat;

                                    subCategory.Name = jSubCategoryObject.Value<string>("name");
                                    subCategory.Description = jSubCategoryObject.Value<string>("description");
                                    if(jSubCategoryObject["thumbnail"] != null)
                                        subCategory.Thumb = jSubCategoryObject["thumbnail"].Value<string>("thumb_url");
                                    subCategory.Url = string.Format(categoryUrlFormatString, jSubCategoryObject.Value<string>("id"), start, end);

                                    if (jSubCategoryObject["clipList"] as JArray != null)
                                    {                                        
                                        subCategory.EstimatedVideoCount = (uint)(jSubCategoryObject["clipList"] as JArray).Count;
                                        cat.EstimatedVideoCount += subCategory.EstimatedVideoCount;
                                    }
                                    if(subCategory.EstimatedVideoCount > 0)
                                        cat.SubCategories.Add(subCategory);  
                                }

                                if (cat.SubCategories.Count > 0)
                                {
                                    cat.HasSubCategories = true;
                                    cat.SubCategoriesDiscovered = true;
                                }
                            }
                            if(cat.EstimatedVideoCount > 0)
                                Settings.Categories.Add(cat);
                        }
                    }
                }
            }
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override String getUrl(VideoInfo video)
        {
                string url = video.VideoUrl;
                string host = url.Substring(url.IndexOf(":") + 3, url.IndexOf("/", url.IndexOf(":") + 3) - (url.IndexOf(":") + 3));
                string app = url.Substring(host.Length + url.IndexOf(host) + 1, (url.IndexOf("/", url.IndexOf("/", (host.Length + url.IndexOf(host) + 1)) + 1)) - (host.Length + url.IndexOf(host) + 1));
                if (host.Contains(":")) host = host.Substring(0, host.IndexOf(":"));    
                string tcUrl = "rtmpe://" + host + ":1935" + "/" + app;
                string playpath = url.Substring(url.IndexOf(app) + app.Length + 1);

                string resultUrl = ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                    string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&hostname={1}&tcUrl={2}&app={3}&swfurl={4}&swfsize={5}&swfhash={6}&playpath={7}",
                        tcUrl, //rtmpUrl
                        host, //host
                        tcUrl, //tcUrl
                        app, //app
                        "http://www.sat1.de/php-bin/apps/VideoPlayer20/mediacenter/HybridPlayer.swf", //swfurl
                        "850680", //swfsize
                        "89b2c799c23569599472e3ed8b00a292a78de2ef7f181d4de64dccc99e43e1ff", //swfhash
                        playpath //playpath
                        ));
                return resultUrl;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            VideoInfo video = new VideoInfo();

            JObject contentData = GetWebDataAsJson((category as RssLink).Url);
            if (contentData != null)
            {
                foreach (var jObject in contentData)
                {
                    if (jObject.Key.ToString().Contains("clipList"))
                    {
                        foreach (var jSubObject in jObject.Value as JArray)
                        {
                            foreach (JProperty jEntry in jSubObject)
                            {
                                switch (jEntry.Name)
                                {
                                    case "id":
                                        break;
                                    case "external_id":
                                        break;
                                    case "metadata":
                                        if (jEntry.Value["flashdrm_url"] != null)
                                        {
                                            video.VideoUrl = jEntry.Value.Value<string>("flashdrm_url");
                                        }
                                        else
                                        {
                                            string filename = jEntry.Value.Value<string>("uploadFilename");
                                            filename = filename.Substring(0, filename.Length - 3);

                                            string geo = jEntry.Value.Value<string>("geoblocking");
                                            string geoblock = "";
                                            if (string.IsNullOrEmpty(geo))
                                                geoblock = "geo_d_at_ch/";
                                            else if (geo.Contains("ww"))
                                                geoblock = "geo_worldwide/";
                                            else if (geo.Contains("de_at_ch"))
                                                geoblock = "geo_d_at_ch/";
                                            else
                                                geoblock = "geo_d/";

                                            string suffix = jEntry.Value.Value<string>("flashSuffix");
                                            string videoType = jEntry.Value.Value<string>("video_type");
                                            string broadcast = jEntry.Value.Value<string>("broadcast_date");
                                            video.Title2 = videoType;

                                            if (suffix != null && suffix.Contains("mp4"))
                                                video.VideoUrl = rtmpBase + geoblock + "mp4:" + filename + "mp4";
                                            else
                                                video.VideoUrl = rtmpBase + geoblock + filename + "flv";
                                        }

                                        string cast, tags;

                                        cast = jEntry.Value.Value<string>("cast_1") + ",";
                                        cast += jEntry.Value.Value<string>("cast_2") + ",";
                                        cast += jEntry.Value.Value<string>("cast_3") + ",";
                                        cast += jEntry.Value.Value<string>("cast_4") + ",";
                                        cast += jEntry.Value.Value<string>("cast_5");
                                        while (cast.EndsWith(",")) cast = cast.Substring(0, cast.Length - 1);
                                        video.Description += string.IsNullOrEmpty(cast) ? "" : "\n" + Translation.Actors + ": " + cast;

                                        tags = jEntry.Value.Value<string>("tag_1") + ",";
                                        tags += jEntry.Value.Value<string>("tag_2") + ",";
                                        tags += jEntry.Value.Value<string>("tag_3");
                                        while (tags.EndsWith(",")) tags = tags.Substring(0, tags.Length - 1);
                                        video.Description += string.IsNullOrEmpty(tags) ? "" : "\n" + Translation.Tags + ": " + tags;
                                                                                
                                        break;
                                    case "name":
                                        video.Title = jEntry.Value.Value<string>();
                                        break;
                                    case "playback_duration":
                                        video.Length = jEntry.Value.Value<string>();                                        
                                        break;
                                    case "visibility":
                                        break;
                                    case "description":
                                        video.Description = jEntry.Value.Value<string>() + video.Description;
                                        break;
                                    case "thumbnail":
                                        video.ImageUrl = jEntry.Value.Value<string>("thumb_url");
                                        break;
                                }
                            }
                            if (!string.IsNullOrEmpty(video.Title) && !string.IsNullOrEmpty(video.VideoUrl))
                            {

                                if (!string.IsNullOrEmpty(video.Title2))
                                {
                                    video.Title2 = video.Title2.Replace("short", "clip");
                                    video.Title = video.Title + " (" + video.Title2 + ")";
                                }
                                videos.Add(video);
                                video = new VideoInfo();
                            }
                        }
                    }
                }
            }
            return videos;
        }
    }
}
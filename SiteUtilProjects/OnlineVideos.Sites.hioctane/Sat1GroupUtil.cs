using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Jayrock.Json;
using System.Collections;
using System.Text.RegularExpressions;
using System.ComponentModel;

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
            string json = GetWebData(baseUrl);

            JsonObject contentData = Jayrock.Json.Conversion.JsonConvert.Import(json) as JsonObject;
            if (contentData != null)
            {
                foreach (DictionaryEntry jObject in contentData)
                {
                    if (jObject.Key.ToString().Contains("categoryList"))
                    {
                        foreach (JsonObject jSubObject in jObject.Value as JsonArray)
                        {
                            RssLink cat = new RssLink();
                            cat.Name = jSubObject["name"].ToString();
                            cat.Description = jSubObject["description"].ToString();
                            if (jSubObject["thumbnail"] != null)
                                cat.Thumb = Regex.Match(jSubObject["thumbnail"].ToString(), @"""thumb_url"":""(?<thumb>[^""]+)""").Groups["thumb"].Value;
                            cat.Url = string.Format(categoryUrlFormatString, jSubObject["id"].ToString(), start, end);

                            if (jSubObject["clipList"].ToString().CompareTo("[]") != 0)
                            {
                                string cliplist = jSubObject["clipList"].ToString();
                                if (cliplist.StartsWith("[")) cliplist = cliplist.Substring(1, cliplist.Length - 2);
                                string[] vars = Utils.Tokenize(cliplist, true, ",");
                                cat.EstimatedVideoCount = (uint)vars.Length;
                            }

                            if (jSubObject["categoryList"].ToString().CompareTo("[]") != 0)
                            {
                                cat.HasSubCategories = true;
                                cat.SubCategoriesDiscovered = true;
                                cat.SubCategories = new List<Category>();

                                foreach (JsonObject jSubCategoryObject in jSubObject["categoryList"] as JsonArray)
                                {
                                    RssLink subCategory = new RssLink();
                                    subCategory.ParentCategory = cat;

                                    subCategory.Name = jSubCategoryObject["name"].ToString();
                                    subCategory.Description = jSubCategoryObject["description"].ToString();
                                    if(jSubCategoryObject["thumbnail"] != null)
                                        subCategory.Thumb = Regex.Match(jSubCategoryObject["thumbnail"].ToString(), @"""thumb_url"":""(?<thumb>[^""]+)""").Groups["thumb"].Value;
                                    subCategory.Url = string.Format(categoryUrlFormatString, jSubCategoryObject["id"].ToString(), start, end);

                                    if (jSubCategoryObject["clipList"].ToString().CompareTo("[]") != 0)
                                    {
                                        string cliplist = jSubCategoryObject["clipList"].ToString();
                                        if (cliplist.StartsWith("[")) cliplist = cliplist.Substring(1, cliplist.Length - 2);
                                        string[] vars = Utils.Tokenize(cliplist, true, ",");
                                        subCategory.EstimatedVideoCount = (uint)vars.Length;
                                        cat.EstimatedVideoCount += subCategory.EstimatedVideoCount;
                                    }
                                    if(subCategory.EstimatedVideoCount > 0)
                                        cat.SubCategories.Add(subCategory);  
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
            if (video.VideoUrl.Contains("token"))
            {
                string url = video.VideoUrl;
                string host = url.Substring(9, url.IndexOf("/", 9) - 9);
                string app = url.Substring(host.Length + url.IndexOf(host) + 1, (url.IndexOf("/", url.IndexOf("/", (host.Length + url.IndexOf(host) + 1)) + 1)) - (host.Length + url.IndexOf(host) + 1));
                string tcUrl = "rtmpe://" + host + ":1935" + "/" + app;
                string playpath = url.Substring(url.IndexOf(app) + app.Length + 1);

                string resultUrl = string.Format("http://127.0.0.1:{0}/stream.flv?rtmpurl={1}&hostname={2}&tcUrl={3}&app={4}&swfurl={5}&swfsize={6}&swfhash={7}&playpath={8}",
                        OnlineVideoSettings.RTMP_PROXY_PORT,
                        url, //rtmpUrl
                        host, //host
                        tcUrl, //tcUrl
                        app, //app
                        "http://www.sat1.de/php-bin/apps/VideoPlayer20/mediacenter/HybridPlayer.swf", //swfurl
                        "850680", //swfsize
                        "89b2c799c23569599472e3ed8b00a292a78de2ef7f181d4de64dccc99e43e1ff", //swfhash
                        playpath //playpath
                        );
                return resultUrl;
            }
            else
            {
                string url = video.VideoUrl;
                string resultUrl = string.Format("http://127.0.0.1:{0}/stream.flv?rtmpurl={1}", OnlineVideoSettings.RTMP_PROXY_PORT, System.Web.HttpUtility.UrlEncode(url));
                return resultUrl;
            }
           
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            VideoInfo video = new VideoInfo();

            string data = GetWebData((category as RssLink).Url);
            JsonObject contentData = Jayrock.Json.Conversion.JsonConvert.Import(data) as JsonObject;
            if (contentData != null)
            {
                foreach (DictionaryEntry jObject in contentData)
                {
                    if (jObject.Key.ToString().Contains("clipList"))
                    {
                        foreach (JsonObject jSubObject in jObject.Value as JsonArray)
                        {
                            foreach (DictionaryEntry jEntry in jSubObject)
                            {
                                switch (jEntry.Key.ToString())
                                {
                                    case "id":
                                        break;
                                    case "external_id":
                                        break;
                                    case "metadata":

                                        string drmUrl = Regex.Match(jEntry.Value.ToString(), @"""flashdrm_url"":\s*""(?<tag>[^""]+)""").Groups["tag"].Value;
                                        if (!string.IsNullOrEmpty(drmUrl))
                                        {
                                            video.VideoUrl = drmUrl;
                                        }
                                        else
                                        {
                                            string filename = Regex.Match(jEntry.Value.ToString(), @"""uploadFilename"":\s*""(?<tag>[^""]+)""").Groups["tag"].Value;
                                            filename = filename.Substring(0, filename.Length - 3);

                                            string geo = Regex.Match(jEntry.Value.ToString(), @"""geoblocking"":\s*""(?<tag>[^""]+)""").Groups["tag"].Value;
                                            string geoblock = "";
                                            if (string.IsNullOrEmpty(geo))
                                                geoblock = "geo_d_at_ch/";
                                            else if (geo.Contains("ww"))
                                                geoblock = "geo_worldwide/";
                                            else if (geo.Contains("de_at_ch"))
                                                geoblock = "geo_d_at_ch/";
                                            else
                                                geoblock = "geo_d/";

                                            string suffix = Regex.Match(jEntry.Value.ToString(), @"""flashSuffix"":\s*""(?<tag>[^""]+)""").Groups["tag"].Value;
                                            string videoType = Regex.Match(jEntry.Value.ToString(), @"""video_type"":\s*""(?<tag>[^""]+)""").Groups["tag"].Value;
                                            string broadcast = Regex.Match(jEntry.Value.ToString(), @"""broadcast_date"":\s*""(?<tag>[^""]+)""").Groups["tag"].Value;
                                            video.Title2 = videoType + " (" + broadcast + ")";

                                            if (suffix.Contains("mp4"))
                                                video.VideoUrl = rtmpBase + geoblock + "mp4:" + filename + "f4v";
                                            else
                                                video.VideoUrl = rtmpBase + geoblock + filename + "flv";
                                        }

                                        string cast, tags;

                                        cast = Regex.Match(jEntry.Value.ToString(), @"""cast_1"":\s*""(?<tag>[^""]+)""").Groups["tag"].Value + ",";
                                        cast += Regex.Match(jEntry.Value.ToString(), @"""cast_2"":\s*""(?<tag>[^""]+)""").Groups["tag"].Value + ",";
                                        cast += Regex.Match(jEntry.Value.ToString(), @"""cast_3"":\s*""(?<tag>[^""]+)""").Groups["tag"].Value + ",";
                                        cast += Regex.Match(jEntry.Value.ToString(), @"""cast_4"":\s*""(?<tag>[^""]+)""").Groups["tag"].Value + ",";
                                        cast += Regex.Match(jEntry.Value.ToString(), @"""cast_5"":\s*""(?<tag>[^""]+)""").Groups["tag"].Value;
                                        while (cast.EndsWith(",")) cast = cast.Substring(0, cast.Length - 1);

                                        tags = Regex.Match(jEntry.Value.ToString(), @"""tag_1"":\s*""(?<tag>[^""]+)""").Groups["tag"].Value + ",";
                                        tags += Regex.Match(jEntry.Value.ToString(), @"""tag_2"":\s*""(?<tag>[^""]+)""").Groups["tag"].Value + ",";
                                        tags += Regex.Match(jEntry.Value.ToString(), @"""tag_3"":\s*""(?<tag>[^""]+)""").Groups["tag"].Value;
                                        while (tags.EndsWith(",")) tags = tags.Substring(0, tags.Length - 1);

                                        video.Description += "\n" + Translation.Actors + ": " + cast + "\n" + Translation.Tags + ": " + tags;
                                        break;
                                    case "name":
                                        if (!string.IsNullOrEmpty(video.Title) && !string.IsNullOrEmpty(video.VideoUrl))
                                        {
                                            videos.Add(video);
                                            video = new VideoInfo();
                                        }
                                        video.Title = jEntry.Value.ToString();
                                        break;
                                    case "playback_duration":
                                        if (!string.IsNullOrEmpty(video.Length) && !string.IsNullOrEmpty(video.VideoUrl))
                                        {
                                            videos.Add(video);
                                            video = new VideoInfo();
                                        }
                                        if (!string.IsNullOrEmpty(jEntry.Value.ToString())){
                                            int duration = Convert.ToInt32(jEntry.Value.ToString().Substring(0,jEntry.Value.ToString().IndexOf(".")));
                                            video.Length = string.Format("{0:00}:{1:00}", duration / 60, duration % 60);
                                        }
                                        break;
                                    case "visibility":
                                        break;
                                    case "description":
                                        if (!string.IsNullOrEmpty(video.Description) && !string.IsNullOrEmpty(video.VideoUrl) && video.Description.Contains(jEntry.Value.ToString()))
                                        {
                                            videos.Add(video);
                                            video = new VideoInfo();
                                        }
                                        video.Description = jEntry.Value.ToString() + video.Description;
                                        break;
                                    case "thumbnail":
                                        if (!string.IsNullOrEmpty(video.ImageUrl) && !string.IsNullOrEmpty(video.VideoUrl))
                                        {
                                            videos.Add(video);
                                            video = new VideoInfo();
                                        }
                                        video.ImageUrl = Regex.Match(jEntry.Value.ToString(), @"""thumb_url"":\s*""(?<thumb>[^""]+)""").Groups["thumb"].Value;
                                        break;
                                }
                            }
                            
                        }
                        if (!string.IsNullOrEmpty(video.Title) && !string.IsNullOrEmpty(video.VideoUrl))
                        {
                            videos.Add(video);
                            video = new VideoInfo();
                        }
                    }
                }
            }
            return videos;
        }
    }
}
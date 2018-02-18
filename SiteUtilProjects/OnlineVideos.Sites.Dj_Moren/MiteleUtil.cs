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


namespace OnlineVideos.Sites
{
    public class MiteleUtil : GenericSiteUtil
    {   
        internal enum CategoryType
        {
            None,
            Programs
        }

        public override void Initialize(SiteSettings siteSettings)
        {
            siteSettings.DynamicCategoriesDiscovered = false;
            base.Initialize(siteSettings);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            Settings.Categories.Add(CreateCategory("Programas de TV", "https://cdn-search-mediaset.carbyne.ps.ooyala.com/search/v1/full/providers/104951/mini?q={%22types%22:%22tv_series%22,%22genres%22:[%22_ca_programas%22],%22sort%22:{%22field%22:%22ingestion_date%22,%22order%22:%22desc%22},%22page_size%22:1000,%22page_number%22:1,%22product_id%22:[%22Free_Web%22,%22Free_Web_Mobile%22,%22Register_Web%22,%22Free_Live_Web%22,%22Register_Live_Web%22,%22MITELTVOD_Web_Mobile_01%22]}&format=full&include_titles=Series,Season&&product_name=test&format=full", string.Empty, CategoryType.Programs, string.Empty, null));
            Settings.Categories.Add(CreateCategory("Series Online", "https://cdn-search-mediaset.carbyne.ps.ooyala.com/search/v1/full/providers/104951/mini?q={%22types%22:%22tv_series%22,%22genres%22:[%22_ca_series%22],%22sort%22:{%22field%22:%22ingestion_date%22,%22order%22:%22desc%22},%22page_size%22:1000,%22page_number%22:1,%22product_id%22:[%22Free_Web%22,%22Free_Web_Mobile%22,%22Register_Web%22,%22Free_Live_Web%22,%22Register_Live_Web%22,%22MITELTVOD_Web_Mobile_01%22]}&format=full&include_titles=Series,Season&&product_name=test&format=full", string.Empty, CategoryType.Programs, string.Empty, null));
            Settings.Categories.Add(CreateCategory("TV Movies", "https://cdn-search-mediaset.carbyne.ps.ooyala.com/search/v1/full/providers/104951/mini?q={%22types%22:%22tv_series%22,%22genres%22:[%22_ca_tv-movies%22],%22sort%22:{%22field%22:%22ingestion_date%22,%22order%22:%22desc%22},%22page_size%22:1000,%22page_number%22:1,%22product_id%22:[%22Free_Web%22,%22Free_Web_Mobile%22,%22Register_Web%22,%22Free_Live_Web%22,%22Register_Live_Web%22,%22MITELTVOD_Web_Mobile_01%22]}&format=full&include_titles=Series,Season&&product_name=test&format=full", string.Empty, CategoryType.Programs, string.Empty, null));
            Settings.Categories.Add(CreateCategory("Documentales", "https://cdn-search-mediaset.carbyne.ps.ooyala.com/search/v1/full/providers/104951/mini?q={%22genres%22:%22_ca_documentales%22,%22sort%22:{%22field%22:%22region%22,%22order%22:%22asc%22},%22page_size%22:1000,%22page_number%22:1,%22product_id%22:[%22Free_Web%22,%22Free_Web_Mobile%22,%22Register_Web%22,%22Free_Live_Web%22,%22Register_Live_Web%22,%22MITELTVOD_Web_Mobile_01%22]}&format=full&include_titles=Series,Season&&product_name=test&format=full", string.Empty, CategoryType.Programs, string.Empty, null));
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        internal RssLink CreateCategory(string name, string url, string thumbUrl, CategoryType categoryType, string description, Category parentCategory)
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

        public override int DiscoverSubCategories(Category parentCategory)
        {            
            List<Category> subCategories = null;

            switch ((CategoryType)parentCategory.Other)
            {
                case CategoryType.Programs:
                    subCategories = DiscoverPrograms(parentCategory as RssLink);
                    break;
            }

            parentCategory.SubCategories = subCategories;
            parentCategory.SubCategoriesDiscovered = true;
            parentCategory.HasSubCategories = subCategories == null ? false : subCategories.Count > 0;

            return parentCategory.HasSubCategories ? subCategories.Count : 0;
        }

        internal List<Category> DiscoverPrograms(RssLink parentCategory)
        {
            List<Category> result = new List<Category>();
            JArray programs = GetWebDataJArray(parentCategory.Url);
            string programsBaseUrl = "https://cdn-search-mediaset.carbyne.ps.ooyala.com/search/v1/full/providers/104951/docs/series?series_id={0}&include=Episode,Season,Series&size=2000&include_titles=Series,Season&product_name=test&format=full&onlinevideos_id={1}";
            foreach (JToken program in programs)
            {
                JToken source = program.SelectToken("_source");
                JToken localizable_titles = source.SelectToken("localizable_titles")[0];
                string name = (string)localizable_titles.SelectToken("episode_name");
                string externalId = (string)source.SelectToken("external_id");
                string id = (string)program.SelectToken("_id");
                string url = string.Format(programsBaseUrl, externalId, id);
                string thumbUrl = (string)source.SelectToken("thumbnail.url");
                string description = (string)localizable_titles.SelectToken("summary_long");
                result.Add(CreateCategory(name, url, thumbUrl, CategoryType.None, description, parentCategory));
            }
            result = result.OrderBy(o => o.Name).ToList();
            return result;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();
            JArray episodes = GetWebDataJArray((category as RssLink).Url);
            String videosBaseUrl = "http://player.ooyala.com/sas/player_api/v2/authorization/embed_code/FhcGUyOrTrXrKWC9kzg5OnqpZp3S/{0}?device=html5&domain=www.mitele.es";
            if (episodes.Count == 0)
            {
                string id = (category as RssLink).Url.Split(new string[] { "&onlinevideos_id=" }, StringSplitOptions.None)[1];
                string url = "https://cdn-search-mediaset.carbyne.ps.ooyala.com/search/v1/full/providers/104951/docs/{0}?&product_name=test&format=full";
                episodes = GetWebDataJArray(String.Format(url, id));
            }
            
            foreach (JToken episode in episodes)
            {
                try
                {
                    JToken source = episode.SelectToken("_source");
                    JToken localizable_titles = source.SelectToken("localizable_titles[0]");
                    string showType = (string)source.SelectToken("show_type");
                    if ("Episode".Equals(showType) || "Movie".Equals(showType))
                    {
                        VideoInfo video = new VideoInfo();
                        string season_number = (string)source.SelectToken("season_number");
                        string episode_number = (string)localizable_titles.SelectToken("title_sort_name");
                        string episode_name = (string)localizable_titles.SelectToken("title_medium");
                        if (season_number != null)
                        {
                            video.Title = string.Format("Temporada {0} - {1} - {2}", season_number, episode_number, episode_name);
                        }
                        else
                        {
                            video.Title = episode_name;
                        }
                        video.Description = (string)localizable_titles.SelectToken("summary_long");
                        video.Airdate = (source.SelectToken("linear_broadcast_date") == null? source.SelectToken("created_at") : source.SelectToken("linear_broadcast_date")).ToString();
                        video.Thumb = (string)source.SelectToken("thumbnail.url");
                        string embed_code = (string)source.SelectToken("offers[0].embed_codes[0]"); ;
                        video.VideoUrl = string.Format(videosBaseUrl, embed_code);
                        videoList.Add(video);
                    }
                }
                catch
                {
                    Log.Debug("mitele: error getting episodes {0}", episode);
                }
            } 
            videoList = videoList.OrderByDescending(o => Convert.ToDateTime(o.Airdate).Ticks).ToList();
            return videoList;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string data = GetWebData(video.VideoUrl);
            JObject jsonVideos = JObject.Parse(data);
            JArray streams = (JArray)jsonVideos.SelectToken("authorization_data").First().First().SelectToken("streams");
            string previous_master_m3u8_url = "";
            foreach (JToken stream in streams)
            {
                if ("akamai_hd2_vod_hls".Equals((string)stream.SelectToken("delivery_type")))
                {
                    previous_master_m3u8_url = Base64Decode((string)stream.SelectToken("url.data"));
                    break;
                }
            }
            if (!"".Equals(previous_master_m3u8_url))
            {
                string options_data = GetWebData(previous_master_m3u8_url);
                string[] options = options_data.Split('\n');
                SortedDictionary<string, string> playbackOptions = new SortedDictionary<string, string>();
                for (int i = 1; i < options.Length; i = i + 2)
                {
                    string[] features = options[i].Split(',');
                    string resolution = "";
                    int bandwidth = 0;
                    foreach (string feature in features)
                    {
                        if (feature.StartsWith("BANDWIDTH"))
                        {
                            bandwidth = int.Parse(feature.Split('=')[1]) / 1024;
                        }
                        else if (feature.StartsWith("RESOLUTION"))
                        {
                            resolution = feature.Split('=')[1];
                        }
                    }
                    string nm = string.Format("{0} {1}K", resolution, bandwidth);
                    string url = options[i + 1];
                    playbackOptions.Add(nm, url);
                }
                video.PlaybackOptions = new Dictionary<string, string>();
                foreach (var item in playbackOptions.OrderBy(i => int.Parse(i.Key.Split(' ')[1].Split('K')[0])))
                {
                    video.PlaybackOptions.Add(item.Key, item.Value);
                }
                    
            }
            if (video.PlaybackOptions.Count == 0)
            {
                return "";// if no match, return empty url -> error
            }
            else if (video.PlaybackOptions.Count == 1)
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

        public JArray GetWebDataJArray(string url)
        {
            string data = GetWebData(url);
            JObject jsonEpisodes = JObject.Parse(data);
            return (JArray)jsonEpisodes["hits"]["hits"];
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public static string base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}

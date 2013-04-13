using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml;

using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class M6ReplayUtil : SiteUtilBase
    {
        [Category("OnlineVideosConfiguration"), Description("site identifier")]
        protected string siteIdentifier = "m6";
        
        private static string catalogUrlFormat = @"http://static.m6replay.fr/catalog/m6group_web/{0}replay/catalogue.json";
        private static string imageUrlFormat = @"http://static.m6replay.fr/images/{0}";
        private static string videoListUrlFormat = @"http://static.m6replay.fr/catalog/m6group_web/{0}replay/program/getvideos-{1}.json";
        private static string videoUrlFormat = @"http://backstage-video.m6replay.fr/rest-replay-v2/?ws=get_video_info&service={0}replay&serviceref={0}replay&platform=m6group_web&idvideo={1}";
        
        private static List<JProperty> genres = new List<JProperty>();
        private static List<JProperty> programs = new List<JProperty>();

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            
            JObject json = GetWebData<JObject>(string.Format(catalogUrlFormat, siteIdentifier));
            if (json != null)
            {
                foreach (JProperty property in (json["gnrList"] as JObject).Properties())
                {
                    genres.Add(property);

                    JToken genre = property.Value;
                    string id = property.Name;
                    string idParent = genre.Value<string>("idParent");
                    string name = genre.Value<string>("name");

                    Log.Debug("id: {0} idParent: {1} name: {2}", id, idParent, name);

                    // only get main genres (which have no parent)
                    if (string.IsNullOrEmpty(idParent)) {
                        Settings.Categories.Add(new RssLink() {
                                                    Name = name,
                                                    HasSubCategories = true,
                                                    Thumb = string.Format(imageUrlFormat, genre["img"]["vignette"]),
                                                    Other = id
                                                });
                    }
                }
                
                // get programs
                foreach (JProperty property in (json["pgmList"] as JObject).Properties())
                {
                    programs.Add(property);
                }
            }
            
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            
            string genreId = (parentCategory.Other) as string;
            foreach (JProperty property in programs)
            {
                string programId = property.Name;
                JToken program = property.Value;
                if (genreId.Equals(program.Value<string>("idGnr")) || getGenresForId(genreId).Contains(program.Value<string>("idGnr")))
                {
                    parentCategory.SubCategories.Add(new RssLink() {
                                                         ParentCategory = parentCategory,
                                                         Name = program.Value<string>("name"),
                                                         Description = program.Value<string>("desc"),
                                                         Thumb = string.Format(imageUrlFormat, program["img"]["vignette"]),
                                                         Other = programId,
                                                         HasSubCategories = false
                                                     });
                }
            }
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }
        
        private List<string> getGenresForId(string id)
        {
            // traverse all genres to find all subgenres associated with this parent genre
            List<string> result = new List<string>();
            foreach (JProperty property in genres)
            {
                if (id.Equals(property.Value.Value<string>("idParent"))) { result.Add(property.Name); }
            }
            return result;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            JObject json = GetWebData<JObject>(string.Format(videoListUrlFormat, siteIdentifier, category.Other));
            if (json != null)
            {
                foreach (JProperty property in json.Properties())
                {
                    JToken video = property.Value;
                    result.Add(new VideoInfo() {
                                   Title = video.Value<string>("clpName"),
                                   Description = video.Value<string>("desc"),
                                   ImageUrl = string.Format(imageUrlFormat, video["img"]["vignette"]),
                                   Length = video.Value<string>("duration"),
                                   Other = property.Name
                               });
                }
            }
            return result;
        }

        public override string getUrl(VideoInfo video)
        {
            string result = string.Empty;
            video.PlaybackOptions = new Dictionary<string, string>();
            XmlDocument xml = GetWebData<XmlDocument>(string.Format(videoUrlFormat, siteIdentifier, video.Other));
            if (xml != null)
            {
                string url = string.Empty;
                XmlNode sd = xml.SelectSingleNode(@"//item/url_video_sd");
                if (sd != null)
                {
                    url = new MPUrlSourceFilter.HttpUrl(sd.InnerText).ToString();
                    video.PlaybackOptions.Add("SD", url);
                    result = url;
                }
                XmlNode hd = xml.SelectSingleNode(@"//item/url_video_hd");
                if (hd != null)
                {
                    url = new MPUrlSourceFilter.HttpUrl(hd.InnerText).ToString();
                    video.PlaybackOptions.Add("HD", url);
                    result = url;
                }
            }
            return result;
        }
    }
}

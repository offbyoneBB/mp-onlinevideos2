using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class VodTvpUtil : GenericSiteUtil
    {
        const string categoryUrl = @"http://www.api.v3.tvp.pl/shared/listing.php?dump=json&direct=true&count=150&parent_id={0}";

        public override int DiscoverDynamicCategories()
        {
            if (Settings.Categories == null) Settings.Categories = new BindingList<Category>();

            foreach (Category cat in Settings.Categories)
            {
                cat.HasSubCategories = true;
                cat.Other = true;
            }

            foreach (Category cat in getCats(String.Format(categoryUrl, "1785454"), null))
                Settings.Categories.Add(cat);
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            if (true.Equals(parentCategory.Other))
            {
                int res = base.DiscoverSubCategories(parentCategory);
                foreach (Category cat in parentCategory.SubCategories)
                    cat.Other = parentCategory.Other;
                return res;
            }

            parentCategory.SubCategories = getCats(((RssLink)parentCategory).Url, parentCategory);
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            if (true.Equals(category.Other))
                return base.GetVideos(category);
            string webData = GetWebData(((RssLink)category).Url);
            JObject contentData = JObject.Parse(webData);
            if (contentData != null)
            {
                JArray items = contentData["items"] as JArray;
                if (items != null)
                {
                    List<VideoInfo> result = new List<VideoInfo>();
                    foreach (JToken item in items)
                        if (!item.Value<bool>("payable") && item.Value<int>("play_mode") == 1)
                        {
                            VideoInfo video = new VideoInfo();
                            video.Title = item.Value<string>("title");
                            video.VideoUrl = String.Format(videoListRegExFormatString, item.Value<string>("_id"));
                            video.Description = item.Value<string>("description_root");
                            video.Thumb = getImageUrl(item);
                            video.Airdate = item.Value<string>("publication_start_dt") + ' ' + item.Value<string>("publication_start_hour");
                            result.Add(video);
                        }
                    return result;
                }
            }
            return null;
        }

        public override String GetVideoUrl(VideoInfo video)
        {
            string webData = GetWebData(video.VideoUrl);
            JObject content = JObject.Parse(webData);

            if (content != null)
            {
                JArray formats = content["formats"] as JArray;
                if (formats != null)
                {
                    SortedList<int, SortedList<string, string>> bitrateList = new SortedList<int, SortedList<string, string>>();
                    //inner sortedlist is extention->url
                    foreach (JToken format in formats)
                    {
                        int bitrate = format.Value<int>("totalBitrate");
                        string url = format.Value<string>("url");
                        string ext = System.IO.Path.GetExtension(url);

                        if (!bitrateList.ContainsKey(bitrate))
                            bitrateList.Add(bitrate, new SortedList<string, string>());
                        bitrateList[bitrate].Add(ext, url);
                    }
                    Dictionary<string, string> result = new Dictionary<string, string>();
                    foreach (KeyValuePair<int, SortedList<string, string>> option in bitrateList)
                    {
                        SortedList<string, string> extList = option.Value;
                        foreach (var ext in extList)
                        {
                            string key = String.Format("{0}Kb", option.Key / 1000) + ext.Key;
                            if (!result.ContainsKey(key))
                                result.Add(key, ext.Value);
                        }
                    }
                    if (result.Count > 1)
                        video.PlaybackOptions = result;

                    if (result.Count >= 1)
                        return result.Last().Value;

                    return null;
                }
            }
            return null;

        }

        private List<Category> getCats(string url, Category parentCategory)
        {
            string webData = GetWebData(url);
            JObject contentData = JObject.Parse(webData);
            if (contentData != null)
            {
                JArray items = contentData["items"] as JArray;
                if (items != null)
                {
                    List<Category> result = new List<Category>();
                    foreach (JToken item in items)
                    {
                        RssLink subcat = new RssLink();
                        subcat.Name = item.Value<string>("title");
                        subcat.Url = String.Format(categoryUrl, item.Value<string>("_id"));
                        subcat.ParentCategory = parentCategory;
                        subcat.HasSubCategories = subcat.Name != "wideo";

                        JArray types = item["types"] as JArray;
                        if (types != null)
                            foreach (JToken typ in types)
                            {
                                JValue val = typ as JValue;
                                if (val != null && val.Value.Equals("directory_video"))
                                    subcat.HasSubCategories = false;
                            }

                        subcat.Thumb = getImageUrl(item);
                        result.Add(subcat);
                    }
                    return result;
                }
            }
            return null;
        }

        private string getImageUrl(JToken item)
        {
            JToken image = item["image"];
            if (image == null)
                image = item["image_16x9"];
            if (image == null)
                image = item["image_4x3"];
            //http://s.v3.tvp.pl/images/a/1/5/uid_a153fdcd7b79c0a2b4a700eefe3cc4e51371051323929_width_400_play_0_pos_0_gs_0.jpg
            if (image != null)
            {
                string filename = image[0].Value<string>("file_name");
                string width = image[0].Value<string>("width");
                return string.Format(@"http://s.v3.tvp.pl/images/{0}/{1}/{2}/uid_{3}_width_{4}_play_0_pos_0_gs_0.jpg",
                    filename[0], filename[1], filename[2], filename.Substring(0, filename.Length - 4), width);
            }
            return String.Empty;
        }

    }
}

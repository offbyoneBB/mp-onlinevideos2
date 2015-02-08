using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class BigPondVideoUtil : GenericSiteUtil
    {
        public override int DiscoverDynamicCategories()
        {
            string data = GetWebData(baseUrl);
            int p = data.IndexOf('{');
            int q = data.LastIndexOf('}');
            data = data.Substring(p, q - p + 1);

            JToken alldata = JObject.Parse(data) as JToken;
            JArray jCats = alldata["widget"]["carousel"] as JArray;
            foreach (JToken jcat in jCats)
            {
                string tab = @"{""bla"":" + jcat["tab"].Value<string>() + '}';
                JToken jsubcats = JObject.Parse(tab)["bla"] as JArray;

                foreach (JToken jsub in jsubcats)
                {
                    RssLink cat = new RssLink();
                    cat.Name = jsub["tabTitle"].Value<string>();
                    cat.Url = FormatDecodeAbsolutifyUrl(baseUrl, jsub["tabTargetLevel"].Value<string>(), dynamicCategoryUrlFormatString, dynamicCategoryUrlDecoding);
                    Settings.Categories.Add(cat);
                }
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            string data = GetWebData(((RssLink)category).Url);
            int p = data.IndexOf('{');
            int q = data.LastIndexOf('}');
            data = data.Substring(p, q - p + 1);
            JToken alldata = JObject.Parse(data) as JToken;
            JArray jVideos = alldata["list"] as JArray;

            List<VideoInfo> res = new List<VideoInfo>();
            foreach (JToken jVideo in jVideos)
            {
                VideoInfo video = new VideoInfo();
                video.Title = jVideo["title"].Value<string>();
                video.Thumb = jVideo["imageUrl"].Value<string>();
                video.Description = jVideo["description"].Value<string>();
                //video.Airdate = DateTime.Parse(jVideo["publishDate"].Value<string>()).ToString(); not sure what format it is
                video.Length = jVideo["duration"].Value<string>();
                JArray medias = jVideo["media"] as JArray;
                if (medias != null)
                {
                    Dictionary<string, string> PlaybackOptions = new Dictionary<string, string>();
                    foreach (JToken media in medias)
                    {
                        string reso = media["bitrate"].Value<string>();
                        string url = media["url"].Value<string>();
                        video.VideoUrl = url;
                        PlaybackOptions.Add(reso + " lines", url);
                    }
                    if (PlaybackOptions.Count == 1)
                    {
                        res.Add(video);
                    }
                    else
                        if (PlaybackOptions.Count > 1)
                        {
                            res.Add(video);
                            video.Other = "PlaybackOptions://\n" + Helpers.CollectionUtils.DictionaryToString(PlaybackOptions);
                        }
                }
            }

            return res;

        }

    }
}

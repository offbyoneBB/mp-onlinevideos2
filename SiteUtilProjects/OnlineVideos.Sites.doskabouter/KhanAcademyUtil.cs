using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class KhanAcademyUtil : GenericSiteUtil
    {

        public override int DiscoverDynamicCategories()
        {
            string webData = GetWebData(baseUrl);
            JArray jt = JArray.Parse(webData);

            SortedList<string, Category> dynamicCategories = new SortedList<string, Category>(); // put all new discovered Categories in a separate list
            foreach (JToken j in jt)
            {
                RssLink cat = new RssLink();
                cat.Name = j.Value<string>("standalone_title");
                string s = j.Value<string>("title");
                if (cat.Name != s)
                    cat.Name += ":" + s;
                cat.Description = j.Value<string>("description");
                cat.Url = String.Format(@"http://www.khanacademy.org/api/v1/playlists/{0}/videos", s.Replace(" ", "%20"));
                dynamicCategories.Add(cat.Name, cat);
            }
            if (Settings.Categories == null) Settings.Categories = new BindingList<Category>();
            foreach (Category cat in dynamicCategories.Values) Settings.Categories.Add(cat);
            Settings.DynamicCategoriesDiscovered = dynamicCategories.Count > 0;
            return dynamicCategories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> res = new List<VideoInfo>();
            string webData = GetWebData(((RssLink)category).Url);
            JArray jt = JArray.Parse(webData);
            foreach (JToken j in jt)
            {
                VideoInfo video = new VideoInfo();
                video.Title = j.Value<string>("title");
                video.Description = j.Value<string>("description");
                DateTime airDate = DateTime.Parse(j.Value<string>("date_added"));
                video.Airdate = airDate.ToString();

                video.Length = VideoInfo.GetDuration(j.Value<string>("duration"));
                JToken dlurls = j["download_urls"];
                video.ImageUrl = dlurls.Value<string>("png");
                video.VideoUrl = @"http://www.youtube.com/watch?v=" + j.Value<string>("youtube_id");
                res.Add(video);
            }

            return res;
        }
    }
}

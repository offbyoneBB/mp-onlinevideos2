using System;
using System.Globalization;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class KhanAcademyUtil : GenericSiteUtil
    {

        public override int DiscoverDynamicCategories()
        {
            if (Settings.Categories == null) Settings.Categories = new BindingList<Category>();
            foreach (Category cat in getSubcategories(baseUrl, null))
                Settings.Categories.Add(cat);
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategoriesDiscovered = true;
            parentCategory.SubCategories = getSubcategories(((RssLink)parentCategory).Url, parentCategory);
            return parentCategory.SubCategories.Count;
        }

        private List<Category> getSubcategories(string url, Category parentCat)
        {
            string webData = GetWebData(url);

            List<Category> dynamicCategories = new List<Category>(); // put all new discovered Categories in a separate list
            bool videosPresent = false;
            foreach (JToken j in JToken.Parse(webData)["children"])
            {
                switch (j.Value<string>("kind"))
                {
                    case "Topic":
                        RssLink cat = new RssLink()
                            {
                                Name = j.Value<string>("title"),
                                Description = j.Value<string>("description"),
                                Url = String.Format(@"http://www.khanacademy.org/api/v1/topic/{0}", j.Value<string>("id")),
                                HasSubCategories = true,
                                ParentCategory = parentCat
                            };
                        dynamicCategories.Add(cat);
                        break;
                    case "Video":
                        videosPresent = true; break;
                }
            }
            if (videosPresent)
            {
                RssLink vidCat = new RssLink()
                {
                    Name = "Videos",
                    Url = url + "/videos",
                    ParentCategory = parentCat,
                    HasSubCategories = false
                };
                dynamicCategories.Add(vidCat);
            }

            dynamicCategories.Sort(CategoryComparer);
            return dynamicCategories;
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
                DateTime airDate = DateTime.Parse(j.Value<string>("date_added"), CultureInfo.InvariantCulture);
                video.Airdate = airDate.ToString();

                video.Length = VideoInfo.GetDuration(j.Value<string>("duration"));
                JToken dlurls = j["download_urls"];
                video.ImageUrl = dlurls.Value<string>("png");
                video.VideoUrl = @"http://www.youtube.com/watch?v=" + j.Value<string>("youtube_id");
                res.Add(video);
            }

            return res;
        }

        private int CategoryComparer(Category cat1, Category cat2)
        {
            return String.Compare(cat1.Name, cat2.Name);

        }

    }
}

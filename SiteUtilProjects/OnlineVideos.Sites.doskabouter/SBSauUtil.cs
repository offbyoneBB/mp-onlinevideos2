using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class SBSauUtil : GenericSiteUtil
    {

        private DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        public override int DiscoverDynamicCategories()
        {
            string data = GetWebData(baseUrl).Substring(12);
            JObject contentData = JObject.Parse(data);
            foreach (KeyValuePair<string, JToken> item in contentData)
            {
                RssLink categ = new RssLink()
                {
                    Name = item.Key,
                    Thumb = item.Value.Value<string>("thumbnail"),
                    SubCategories = new List<Category>(),
                    HasSubCategories = true,
                    SubCategoriesDiscovered = true
                };
                Settings.Categories.Add(categ);

                Add1Subcat(categ, "Featured", item.Value.Value<string>("furl"));
                Add1Subcat(categ, "Latest", item.Value.Value<string>("url"));
                Add1Subcat(categ, "Most Popular", item.Value.Value<string>("purl"));

                AddSubcats(categ, item.Value.Value<JToken>("children"), true);
            }
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        private void Add1Subcat(Category parentCategory, string name, string url)
        {
            if (!String.IsNullOrEmpty(url))
            {
                if (!Uri.IsWellFormedUriString(url, System.UriKind.Absolute))
                    url = new Uri(new Uri(baseUrl), url).AbsoluteUri;

                RssLink subcat = new RssLink()
                {
                    Name = name,
                    Url = url,
                    ParentCategory = parentCategory
                };
                parentCategory.SubCategories.Add(subcat);
            }
        }

        private void AddSubcats(Category parentCategory, JToken sub, bool getFirst)
        {
            if (sub != null)
                foreach (JToken v in sub)
                {
                    JToken first = v is JProperty ? v.First : v;
                    RssLink subcat = new RssLink()
                    {
                        Name = first.Value<string>("name"),
                        Thumb = first.Value<string>("thumbnail"),
                        Url = first.Value<string>("url"),
                        ParentCategory = parentCategory
                    };
                    if (!String.IsNullOrEmpty(subcat.Url))
                    {
                        if (!Uri.IsWellFormedUriString(subcat.Url, System.UriKind.Absolute))
                            subcat.Url = new Uri(new Uri(baseUrl), subcat.Url).AbsoluteUri;
                        parentCategory.HasSubCategories = true;
                        parentCategory.SubCategoriesDiscovered = true;

                        if (parentCategory.SubCategories == null)
                            parentCategory.SubCategories = new List<Category>();
                        parentCategory.SubCategories.Add(subcat);
                        JToken subs = first.Value<JToken>("children");
                        if (subs != null)
                            AddSubcats(subcat, subs, false);
                    }
                }
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string data = GetWebData(((RssLink)category).Url);
            JObject contentData = JObject.Parse(data);
            List<VideoInfo> result = new List<VideoInfo>();
            foreach (JObject vid in contentData["entries"])
            {
                VideoInfo video = new VideoInfo()
                {
                    Title = vid.Value<string>("title"),
                    Description = vid.Value<string>("description") +
                    " Expires " + epoch.AddSeconds(vid.Value<long>("media$expirationDate") / 1000).ToString(),
                    Airdate = epoch.AddSeconds(vid.Value<long>("pubDate") / 1000).ToString(),
                    VideoUrl = vid.Value<string>("id").Replace(@"http://data.media.theplatform.com/media/data/Media/",
                    @"http://www.sbs.com.au/ondemand/video/")
                };

                JArray thumbs = vid.Value<JArray>("media$thumbnails");
                if (thumbs != null)
                    video.ImageUrl = thumbs[0].Value<string>("plfile$downloadUrl");
                result.Add(video);
            }
            return result;
        }

        public override Dictionary<string, string> GetPlaybackOptions(string playlistUrl)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> kv in base.GetPlaybackOptions(playlistUrl))
            {
                if (kv.Value.Contains("sbsvod-f.akamai") || kv.Value.Contains("sbsauvod-f.akamai"))
                    result.Add(kv.Key, kv.Value + "?v=&fp=&r=&g=");
                else
                    result.Add(kv.Key, kv.Value);
            }
            return result;
        }
    }
}

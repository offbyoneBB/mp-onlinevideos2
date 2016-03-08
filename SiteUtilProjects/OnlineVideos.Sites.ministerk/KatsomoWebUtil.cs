using Newtonsoft.Json.Linq;
using OnlineVideos.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web;

namespace OnlineVideos.Sites
{
    public class KatsomoWebUtil : SiteUtilBase, IBrowserSiteUtil
    {

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Hide premium content"), Description("Hide premium content (premium content not supported)")]
        protected bool hidePremium = true;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Hide geo blocked content"), Description("Hide geo blocked content")]
        protected bool hideGeoBlocked = false;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Show loading spinner"), Description("Show the loading spinner in the Browser Player")]
        protected bool showLoadingSpinner = true;

        private string currentId = "";
        private int currentPage = 0;
        private string currentApiUrl = "http://www.katsomo.fi/api/web/search/categories/{0}/assets.json?size=25&start={1}";

        public override int DiscoverDynamicCategories()
        {
            Category programs = new Category() { Name = "Ohjelmat aakkosittain", HasSubCategories = true };
            Settings.Categories.Add(programs);
            RssLink channels = new RssLink() { Name = "Kanavat", HasSubCategories = false, Url = "33100" };
            Settings.Categories.Add(channels);
            Settings.DynamicCategoriesDiscovered = true;
            return 2;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            JObject json = GetWebData<JObject>("http://www.katsomo.fi/cms_prod/all-programs-subcats.json");
            parentCategory.SubCategories = new List<Category>();
            foreach (JToken cat in json["categories"].Value<JArray>())
            {
                if ((!hidePremium || cat["free"].Value<bool>()) && (!hideGeoBlocked || !cat["geoRegion"].Value<bool>()))
                {
                    RssLink category = new RssLink()
                    {
                        Name = cat["title"].Value<string>(),
                        Url = cat["id"].Value<string>(),
                        EstimatedVideoCount = cat["count"].Value<uint>(),
                        ParentCategory = parentCategory
                    };
                    JArray subs = cat["subs"].Value<JArray>();
                    if (subs.Count > 0)
                    {
                        category.HasSubCategories = true;
                        category.SubCategoriesDiscovered = true;
                        category.SubCategories = new List<Category>();
                        foreach (JToken sub in subs)
                        {
                            RssLink subCategory = new RssLink()
                            {
                                Name = sub["title"].Value<string>(),
                                Url = sub["id"].Value<string>(),
                                EstimatedVideoCount = sub["count"].Value<uint>(),
                                ParentCategory = category,
                                HasSubCategories = false
                            };
                            category.SubCategories.Add(subCategory);
                        }
                    }
                    else
                    {
                        category.HasSubCategories = false;
                    }
                    parentCategory.SubCategories.Add(category);
                }
            }
            parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
            return parentCategory.SubCategories.Count;
        }

        private List<VideoInfo> GetVideos()
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            JObject json = GetWebData<JObject>(string.Format(currentApiUrl, currentId, currentPage));
            int numberOfHits = json["assets"]["numberOfHits"].Value<int>();
            HasNextPage = ((currentPage + 1) * 25) < numberOfHits;
            JToken assets = json["assets"]["asset"];
            if (assets is JArray)
            {
                foreach (JToken asset in assets)
                {
                    videos.Add(GetVideoFromToken(asset));
                }
            }
            else if (assets != null && assets.HasValues && assets is JToken)
            {
                videos.Add(GetVideoFromToken(assets));
            }
            return videos;
        }

        private VideoInfo GetVideoFromToken(JToken token)
        {
            VideoInfo video = new VideoInfo();
            string imageFormatUrl = "http://static.katsomo.fi/multimedia/vman/{0}";
            video.Title = (token["subtitle"] != null) ? token["subtitle"].Value<string>() : token["title"].Value<string>();
            video.Description = (token["description"] != null) ? token["description"].Value<string>() : string.Empty;
            video.VideoUrl = string.Format("http://www.katsomo.fi/#!/jakso/{0}/", token["@id"].Value<string>());
            if (token["imageUrl"] != null)
                video.Thumb = string.Format(imageFormatUrl, token["imageUrl"].Value<string>());
            if (token["accurateDuration"] != null && token["accurateDuration"].Value<int>() > 0)
                video.Length = TimeUtils.TimeFromSeconds(token["accurateDuration"].Value<int>().ToString());
            else if (token["duration"] != null && token["duration"].Value<int>() > 0)
                video.Length = TimeUtils.TimeFromSeconds(token["duration"].Value<int>().ToString());
            return video;

        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            currentPage++;
            return GetVideos();
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            currentId = (category as RssLink).Url;
            currentApiUrl = "http://www.katsomo.fi/api/web/search/categories/{0}/assets.json?size=25&start={1}";
            currentPage = 0;
            return GetVideos();
        }

        public override bool CanSearch
        {
            get
            {
                return true;
            }
        }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            List<SearchResultItem> results = new List<SearchResultItem>();
            currentId = "33";
            currentApiUrl = "http://www.katsomo.fi/api/web/search/categories/{0}/assets.json?text=" + HttpUtility.UrlEncode(query) + "&size=25&start={1}";
            currentPage = 0;
            List<VideoInfo> videos = GetVideos();
            videos.ForEach(v => results.Add(v));
            return results;
        }

        string IBrowserSiteUtil.ConnectorEntityTypeName
        {
            get
            {
                return "OnlineVideos.Sites.BrowserUtilConnectors.KatsomoConnector";
            }
        }

        string IBrowserSiteUtil.UserName
        {
            get { return showLoadingSpinner ? "SHOWLOADING" : string.Empty; }
        }

        string IBrowserSiteUtil.Password
        {
            get { return string.Empty; }
        }
    }
}

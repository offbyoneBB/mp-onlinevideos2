using Newtonsoft.Json.Linq;
using OnlineVideos.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites
{
    public class KatsomoWebUtil : SiteUtilBase, IBrowserSiteUtil
    {

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Hide premium content"), Description("Hide premium content (premium content not supported)")]
        protected bool hidePremium = true;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Hide geo blocked content"), Description("Hide geo blocked content")]
        protected bool hideGeoBlocked = false;

        public override int DiscoverDynamicCategories()
        {
            JObject json = GetWebData<JObject>("http://www.katsomo.fi/cms_prod/all-programs-subcats.json");
            foreach(JToken cat in json["categories"].Value<JArray>())
            {
                if ((!hidePremium || cat["free"].Value<bool>()) && (!hideGeoBlocked || !cat["geoRegion"].Value<bool>()))
                {
                    RssLink category = new RssLink()
                    {
                        Name = cat["title"].Value<string>(),
                        Url = cat["id"].Value<string>(),
                        EstimatedVideoCount = cat["count"].Value<uint>()
                    };
                    JArray subs = cat["subs"].Value<JArray>();
                    if (subs.Count > 0)
                    {
                        category.HasSubCategories = true;
                        category.SubCategoriesDiscovered = true;
                        category.SubCategories = new List<Category>();
                        foreach(JToken sub in subs)
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
                    Settings.Categories.Add(category);
                }
            }
            Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > 0;
            return Settings.Categories.Count;
        }

        private string currentId = "";
        private int currentPage = 0;
        private List<VideoInfo> GetVideos()
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            string apiUrlFormat = "http://www.katsomo.fi/api/web/search/categories/{0}/assets.json?size=25&start={1}";
            string videoUrlFormat = "http://www.katsomo.fi/#!/jakso/{0}/";
            string imageFormatUrl = "http://static.katsomo.fi/multimedia/vman/{0}";
            JObject json = GetWebData<JObject>(string.Format(apiUrlFormat, currentId, currentPage));
            int numberOfHits = json["assets"]["numberOfHits"].Value<int>();
            HasNextPage = ((currentPage + 1) * 25) < numberOfHits;
            JArray assets = json["assets"]["asset"].Value<JArray>();
            foreach (JToken asset in assets)
            {
                if (asset["@id"] != null)
                {
                    VideoInfo video = new VideoInfo();
                    video.Title = (asset["subtitle"] != null) ? asset["subtitle"].Value<string>() : asset["title"].Value<string>();
                    video.Description = (asset["description"] != null) ? asset["description"].Value<string>() : string.Empty;
                    video.VideoUrl = string.Format(videoUrlFormat, asset["@id"].Value<string>());
                    if (asset["imageUrl"] != null)
                        video.Thumb = string.Format(imageFormatUrl, asset["imageUrl"].Value<string>());
                    if (asset["accurateDuration"] != null && asset["accurateDuration"].Value<int>() > 0)
                        video.Length = TimeUtils.TimeFromSeconds(asset["accurateDuration"].Value<int>().ToString());
                    videos.Add(video);
                }
            }
            return videos;
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            currentPage++;
            return GetVideos();
        }
        public override List<VideoInfo> GetVideos(Category category)
        {
            currentId = (category as RssLink).Url;
            currentPage = 0;
            return GetVideos();
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
            get { return string.Empty; }
        }

        string IBrowserSiteUtil.Password
        {
            get { return string.Empty; }
        }
    }
}

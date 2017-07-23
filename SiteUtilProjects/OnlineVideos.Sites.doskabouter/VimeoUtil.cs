using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class VimeoUtil : GenericSiteUtil
    {
        [Category("OnlineVideosConfiguration"), Description("Add some dynamic categories found at startup to the list of configured ones.")]
        bool useDynamicCategories = true;
        [Category("OnlineVideosUserConfiguration"), Description("Defines the default number of videos to display per page. (max 50)")]
        int pageSize = 26;

        private const string StandardAdvancedApiUrl = "https://api.vimeo.com";
        private NameValueCollection customHeader;

        public override void Initialize(SiteSettings siteSettings)
        {
            resolveHoster = HosterResolving.FromUrl;
            var bytes = Encoding.ASCII.GetBytes("client_id" + ":" + "client_secret");
            var postdata = "basic " + Convert.ToBase64String(bytes);
            NameValueCollection tmp = new NameValueCollection();
            tmp.Add("Authorization", postdata);

            customHeader = new NameValueCollection();
            try
            {
                var data = GetWebData<JObject>(StandardAdvancedApiUrl + "/oauth/authorize/client?grant_type=client_credentials", postData: "", headers: tmp);
                string accessToken = data.Value<string>("access_token");
                customHeader.Add("Authorization", "Bearer " + accessToken);
            }
            catch (Exception ex)
            {
                Log.Error("Vimeo: Error getting oauth token : {0}", ex.Message);
            }

            base.Initialize(siteSettings);
        }

        #region overrides

        public override int DiscoverDynamicCategories()
        {
            if (!useDynamicCategories)
            {
                Settings.DynamicCategoriesDiscovered = true;

                foreach (Category cat in Settings.Categories)
                {
                    Match m = Regex.Match(((RssLink)cat).Url, @"https?://vimeo.com/[^/]*/(?<kind>(channels|groups|albums)*)/");
                    if (m.Success)
                        cat.HasSubCategories = "channels".Equals(m.Groups["kind"].Value) ||
                            "groups".Equals(m.Groups["kind"].Value) || "albums".Equals(m.Groups["kind"].Value);
                }
                return 0;
            }

            if (Settings.Categories == null) Settings.Categories = new BindingList<Category>();
            string url = StandardAdvancedApiUrl + "/categories?page=1&per_page=50";
            var data = GetWebData<JObject>(url, headers: customHeader);
            foreach (var cat in data["data"])
                Settings.Categories.Add(CategoryFromJsonObject(cat, null));

            Settings.DynamicCategoriesDiscovered = true;

            return Settings.Categories.Count;
        }


        public override List<VideoInfo> GetVideos(Category category)
        {
            string id = System.IO.Path.GetFileName(category.Other as string);
            return Parse(StandardAdvancedApiUrl + "/categories/" + id + "/videos?per_page=" + pageSize, null);
        }

        public override bool CanSearch { get { return true; } }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            var vids = Parse(StandardAdvancedApiUrl + "/videos?per_page=" + pageSize + "&query=" + query, null);
            return vids.ConvertAll<SearchResultItem>(v => v as SearchResultItem);
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            video.PlaybackOptions = null;
            string res = base.GetVideoUrl(video);
            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
                return video.PlaybackOptions.Last().Value;
            else
                return res;
        }

        protected override List<VideoInfo> Parse(string url, string data)
        {
            var jData = GetWebData<JObject>(url, headers: customHeader);
            List<VideoInfo> res = new List<VideoInfo>();
            foreach (var vid in jData["data"])
            {
                VideoInfo video = new VideoInfo()
                {
                    Title = vid.Value<string>("name"),
                    Description = vid.Value<string>("description"),
                    Length = Helpers.TimeUtils.TimeFromSeconds(vid.Value<string>("duration")),
                    VideoUrl = vid.Value<string>("link"),
                    Airdate = vid.Value<string>("release_time")
                };
                if (vid["pictures"] != null)
                    video.Thumb = vid["pictures"].First().Value<string>("link");
                res.Add(video);
            }

            nextPageUrl = jData["paging"]?["next"].Value<String>();
            nextPageAvailable = !String.IsNullOrEmpty(nextPageUrl);
            if (nextPageAvailable)
                nextPageUrl = FormatDecodeAbsolutifyUrl(StandardAdvancedApiUrl, nextPageUrl, null, UrlDecoding.None);

            return res;
        }
        #endregion

        private Category CategoryFromJsonObject(JToken obj, Category parentCat)
        {
            var res = new Category()
            {
                Name = obj.Value<string>("name"),
                ParentCategory = parentCat,
                Other = obj.Value<string>("uri")
            };
            if (obj["icon"] != null && obj["icon"]["sizes"] != null && obj["icon"]["sizes"].First() != null)
                res.Thumb = obj["icon"]["sizes"].First().Value<string>("link");
            if (obj["stats"] != null && obj["stats"]["videos"] != null)
                res.Description = string.Format("Videos: {0}", obj["stats"].Value<string>("videos"));

            if (obj["subcategories"] != null)
            {
                res.HasSubCategories = true;
                res.SubCategories = new List<Category>();
                foreach (var subcat in obj["subcategories"])
                    res.SubCategories.Add(CategoryFromJsonObject(subcat, res));
                res.SubCategoriesDiscovered = true;
            }

            return res;
        }

    }
}

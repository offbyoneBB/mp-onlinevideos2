using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Xml;
using System.Text.RegularExpressions;
using System.Net;
using Newtonsoft.Json;

namespace OnlineVideos.Sites
{
    public class TV4Play : SiteUtilBase
    {
        // API: http://webapi.tv4play.se/ also http://mobapi.tv4play.se/

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Username"), Description("TV4Play username")]
        protected string username = null;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Password"), Description("TV4Play password")]
        protected string password = null;

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Show movies"), Description("Show \"Filmer\" category or not when logged in (Mostly DRM titles)")]
        protected bool showMovies = true;

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Show live TV"), Description("Show \"TV-kanaler\" category or not when logged in (Some DRM free channels)")]
        protected bool showTv = true;

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Filter DRM videos"), Description("Remove videos marked with DRM from video lists")]
        protected bool tryFilterDrm = true;

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Download Subtitles"), Description("Choose if you want to download available subtitles or not")]
        protected bool retrieveSubtitles = true;

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Videos per page"), Description("Default 50, maximum 500. Lower if you get timouts")]
        protected int numberOfVideosPerPage = 50;
        protected const int maxNumberOfVideosPerPage = 500;


        protected CookieContainer cc = new CookieContainer();

        protected const string loginUrl = @"https://www.tv4play.se/session/new?https=";
        protected const string loginPostUrl = @"https://www.tv4play.se/session";

        protected const string webApiBaseUrl = @"http://webapi.tv4play.se/play";
        protected const string programCategoriesUrl = webApiBaseUrl + @"/categories";
        protected const string programSubCategoryUrl = webApiBaseUrl + @"/programs?per_page=1000&page=1&is_premium={0}&category={1}";
        protected const string programVideosUrl = webApiBaseUrl + @"/video_assets?is_live=false&page={0}&platform=web&node_nids={1}&per_page={2}&type={3}";
        protected const string programImageUrl = @"http://img.tv4cdn.se/?quality=40&resize=340x190&shape=cut&source={0}{1}";
        
        protected const string filmerVideosUrl = webApiBaseUrl + @"/movie_assets?platform=web&rows=1000";
        protected const string filmerImageUrl = @"http://img.tv4cdn.se/?quality=40&resize=340x484&shape=cut&source={0}{1}";

        protected const string tvKanalerVideosUrl = webApiBaseUrl + @"/video_assets?platform=web&is_channel=true";
        
        protected const string videoAssetBaseUrl = @"http://premium.tv4play.se/api/web/asset/{0}/play";

        protected const string playbackSwfUrl = @"http://www.tv4play.se/flash/tv4play_sa.swf";

        protected string currentVideoUrl = "";
        protected int currentPage = 1;
        protected string currentVideoCategoryName = "";

        protected const string program = "Program";
        protected const string helaProgram = "Hela program";
        protected const string klipp = "Klipp";
        protected const string filmer = "Filmer";
        protected const string tvKanaler = "TV-kanaler";

        protected bool isPremium = false;
        protected bool fakePremium = false;

        private bool isLoggedIn()
        {
            if (fakePremium) return fakePremium;
            return GetWebData(loginPostUrl, cc).Trim() == "ok";
        }

        private void login()
        {
            if (fakePremium)
            {
                isPremium = fakePremium;
                return;
            }
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                cc = new CookieContainer();
                isPremium = false;
                return;
            }
            isPremium = isLoggedIn();
            if (isPremium) 
                return;
            var loginpage = GetWebData(loginUrl, cc);
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(loginpage);
            var input = htmlDoc.DocumentNode.SelectSingleNode("//input[@id = 'authenticity_token']");
            var authenticity_token = input.GetAttributeValue("value", "");
            string postData = string.Format("user_name={0}&password={1}&authenticity_token={2}&https=", System.Web.HttpUtility.UrlEncode(username), System.Web.HttpUtility.UrlEncode(password), System.Web.HttpUtility.UrlEncode(authenticity_token));
            string loginresponse = GetWebDataFromPost(loginPostUrl, postData, cc);
            isPremium = isLoggedIn();
        }

        public override int DiscoverDynamicCategories()
        {
            currentPage = 1;
            login();

            Settings.Categories.Clear();
            
            Settings.Categories.Add (new RssLink(){Url = programCategoriesUrl, Name = program, HasSubCategories = true});
            if (isPremium && showMovies)
                Settings.Categories.Add(new RssLink() { Url = filmerVideosUrl, Name = filmer, HasSubCategories = false });
            if (isPremium && showTv)
                Settings.Categories.Add(new RssLink() { Url = tvKanalerVideosUrl, Name = tvKanaler, HasSubCategories = false });

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        // Only program
        public override int DiscoverSubCategories(Category parentCategory)
        {
            currentPage = 1;
            login();
            if (parentCategory.ParentCategory == null)
            {
                string jsonstr = GetWebData((parentCategory as RssLink).Url);
                Newtonsoft.Json.Linq.JArray jsonArray = (Newtonsoft.Json.Linq.JArray)JsonConvert.DeserializeObject(jsonstr);
                List<Category> cats = new List<Category>();
                Category cat = new RssLink()
                {
                    Name = "Alla program",
                    Url = string.Format(programSubCategoryUrl, (isPremium ? "" : "false"), ""),
                    HasSubCategories = true,
                    ParentCategory = parentCategory
                };
                cats.Add(cat);

                foreach (var json in jsonArray)
                {
                    cat = new RssLink()
                    {
                        Name = (string)json["name"],
                        Url = string.Format(programSubCategoryUrl, (isPremium ? "" : "false"), (string)json["nid"]),
                        HasSubCategories = true,
                        ParentCategory = parentCategory
                    };
                    cats.Add(cat);
                }
                parentCategory.SubCategories = cats;
            }
            else if (parentCategory.ParentCategory.ParentCategory == null)
            {
                var json = GetWebData<Newtonsoft.Json.Linq.JObject>((parentCategory as RssLink).Url);
                Newtonsoft.Json.Linq.JArray results = (Newtonsoft.Json.Linq.JArray)json["results"];
                List<Category> cats = new List<Category>();
                Category cat = null;
                foreach (var result in results)
                {
                    cat = new RssLink()
                    {
                        Other = (string)result["nid"],
                        Name = (string)result["name"],
                        Thumb = string.Format(programImageUrl, HttpUtility.UrlEncode(webApiBaseUrl), HttpUtility.UrlEncode((string)result["program_image"])),
                        Description = (string)result["description"],
                        ParentCategory = parentCategory,
                        HasSubCategories = true
                    };
                    cats.Add(cat);
                }
                (parentCategory as RssLink).EstimatedVideoCount = (uint)cats.Count;
                parentCategory.SubCategories = cats;
            }
            else if (parentCategory.ParentCategory.ParentCategory.ParentCategory == null)
            {
                if (numberOfVideosPerPage > maxNumberOfVideosPerPage)
                {
                    numberOfVideosPerPage = maxNumberOfVideosPerPage;
                }
                parentCategory.SubCategories = new List<Category>() { 
                    new RssLink() { 
                        HasSubCategories = false, 
                                Name = helaProgram, 
                                ParentCategory = parentCategory, 
                                Url = string.Format(string.Format(programVideosUrl, currentPage, parentCategory.Other, numberOfVideosPerPage, "episode"))
                            },
                            new RssLink() 
                            { 
                                HasSubCategories = false, 
                                Name = klipp, 
                                ParentCategory = parentCategory, 
                                Url = string.Format(string.Format(programVideosUrl, currentPage, parentCategory.Other, numberOfVideosPerPage, "clip"))
                            }
                };
            }
            else
            {
                parentCategory.SubCategories = new List<Category>();
            }
            //(parentCategory as RssLink).EstimatedVideoCount = (uint)parentCategory.SubCategories.Count;
            parentCategory.SubCategoriesDiscovered = (parentCategory.SubCategories.Count > 0);
            return parentCategory.SubCategories.Count;
        }

        
        public override List<VideoInfo> getVideoList(Category category)
        {
            currentPage = 1;
            currentVideoCategoryName = category.Name;
            return generateVideoList(category);
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return generateVideoList(new RssLink() { Url = currentVideoUrl, Name = currentVideoCategoryName });
        }

        private List<VideoInfo> generateVideoList(Category category)
        {
            login();
            currentVideoUrl = (category as RssLink).Url;
            List<VideoInfo> videos = new List<VideoInfo>();
            Newtonsoft.Json.Linq.JObject json = new Newtonsoft.Json.Linq.JObject();
            try
            {
                json = GetWebData<Newtonsoft.Json.Linq.JObject>((category as RssLink).Url);
            }
            catch(Exception e)
            {
                Log.Error("Error when trying to get TV4Play videos: {0}", (category as RssLink).Url);
                Log.Error(e);
                throw new OnlineVideosException("Error getting videos, try again!",false);
            }
            Newtonsoft.Json.Linq.JArray results = (Newtonsoft.Json.Linq.JArray)json["results"];
            foreach (var result in results)
            {
                if ((!tryFilterDrm || !(bool)result["is_drm_protected"]) && (isPremium || int.Parse(((string)result["availability"]["availability_group_free"]).Replace("+",string.Empty)) > 0))
                {
                    VideoInfo video = new VideoInfo();
                    video.Title = (string)result["title"];

                    if (category.Name == klipp || category.Name == helaProgram)
                    {
                        //broadcast_date_time=2012-12-12T21:30:00+01:00
                        var dateNode = result["broadcast_date_time"];
                        if (dateNode != null) video.Airdate = ((DateTime)dateNode).ToString("g", OnlineVideoSettings.Instance.Locale);
                    }
                    video.VideoUrl = string.Format(videoAssetBaseUrl, (string)result["id"]);
                    if (category.Name == filmer)
                    {
                        if (((string)result["poster_image"]) != null)
                        {
                            video.ImageUrl = ((string)result["poster_image"]).StartsWith("/") ?
                                string.Format(filmerImageUrl, webApiBaseUrl, (string)result["poster_image"]) :
                                (string)result["poster_image"];
                        }
                        video.Description = (string)result["synopsis"];
                    }
                    else
                    {
                        video.ImageUrl = string.Format(programImageUrl, "", HttpUtility.UrlEncode((string)result["image"]));
                        video.Description = (string)result["description"];
                    }
                    videos.Add(video);
                }
            }
            HasNextPage = false;
            if (category.Name == klipp || category.Name == helaProgram)
            {
                var assets_types_hits = json["assets_types_hits"];
                bool gotClip = (assets_types_hits["clip"] != null);
                bool gotProgram = (assets_types_hits["program"] != null);
                int hits = 0;
                if (gotClip && gotProgram)
                {
                    hits = (int)json["assets_types_hits"][(category.Name == helaProgram) ? "program" : "clip"];
                }
                else if (gotProgram && category.Name == helaProgram)
                {
                    hits = (int)json["assets_types_hits"]["program"];
                }
                else if (gotClip && category.Name == klipp)
                {
                    hits = (int)json["assets_types_hits"]["clip"];
                }

                HasNextPage = hits > (currentPage * numberOfVideosPerPage);
                if (HasNextPage)
                {
                    currentVideoUrl = (category as RssLink).Url.Replace("&page=" + currentPage, "&page=" + (currentPage + 1));
                    currentPage++;
                }
            }
            return videos;
        }

        public override string getUrl(VideoInfo video)
        {
            //To be certain if the user is *really* logged in...
            login();
            
            string result = string.Empty;
            video.PlaybackOptions = new Dictionary<string, string>();
            XmlDocument xDoc = GetWebData<XmlDocument>(video.VideoUrl, cc);
            var errorElements = xDoc.SelectNodes("//meta[@name = 'error']");
            if (errorElements != null && errorElements.Count > 0)
            {
                throw new OnlineVideosException(((XmlElement)errorElements[0]).GetAttribute("content"));
            }
            else
            {
                List<KeyValuePair<int, string>> urls = new List<KeyValuePair<int, string>>();
                string mediaformat;
                string urlbase;
                string scheme = "";
                foreach (XmlElement videoElem in xDoc.SelectNodes("//items/item"))
                {
                    mediaformat = videoElem.GetElementsByTagName("mediaFormat")[0].InnerText.ToLower();
                    var schemeNode = videoElem.GetElementsByTagName("scheme");
                    if (schemeNode != null && schemeNode.Count > 0)
                        scheme = schemeNode[0].InnerText.ToLower();

                    if (mediaformat.StartsWith("mp4"))
                    {
                        urlbase = videoElem.GetElementsByTagName("base")[0].InnerText.ToLower().Trim();
                        if (urlbase.StartsWith("rtmp"))
                        {
                            urls.Add(new KeyValuePair<int, string>(
                                int.Parse(videoElem.GetElementsByTagName("bitrate")[0].InnerText),
                                new MPUrlSourceFilter.RtmpUrl(videoElem.GetElementsByTagName("base")[0].InnerText)
                                {
                                    PlayPath = videoElem.GetElementsByTagName("url")[0].InnerText.Replace(".mp4", ""),
                                    SwfUrl = playbackSwfUrl,
                                    SwfVerify = true
                                }.ToString()));
                        }
                        else if (urlbase.EndsWith(".f4m"))
                        {
                            urls.Add(new KeyValuePair<int, string>(
                                int.Parse(videoElem.GetElementsByTagName("bitrate")[0].InnerText),
                                videoElem.GetElementsByTagName("url")[0].InnerText + "?hdcore=2.10.3&g=" + OnlineVideos.Sites.Utils.HelperUtils.GetRandomChars(12)));
                        }
                        else if (scheme.StartsWith("http"))
                        {
                            urls.Add(new KeyValuePair<int, string>(
                                int.Parse(videoElem.GetElementsByTagName("bitrate")[0].InnerText),
                                videoElem.GetElementsByTagName("url")[0].InnerText + "hdcore=2.10.3&g=" + OnlineVideos.Sites.Utils.HelperUtils.GetRandomChars(12)));
                        }

                    } /* Can not play wvm format, drm'ed? (widevine) ? */
                    else if (mediaformat.StartsWith("wvm"))
                    {
                        throw new OnlineVideosException("Sorry, unable to play video (Needs widevine plug in)", false);
                        /*urls.Add(new KeyValuePair<int, string>(
                            int.Parse(videoElem.GetElementsByTagName("bitrate")[0].InnerText),
                            videoElem.GetElementsByTagName("url")[0].InnerText));*/
                    }
                    else if (retrieveSubtitles && mediaformat.StartsWith("smi"))
                    {
                        video.SubtitleText = GetWebData(videoElem.GetElementsByTagName("url")[0].InnerText, cc, null, null, false, false, null, System.Text.Encoding.Default);
                    }
                }
                foreach (var item in urls.OrderBy(u => u.Key))
                {
                    video.PlaybackOptions.Add(string.Format("{0} kbps", item.Key), item.Value);
                    result = item.Value;
                }
                return result;
            }
        }
    }
}
using Newtonsoft.Json.Linq;
using OnlineVideos.Helpers;
using OnlineVideos.Sites.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public class YleAppInfo
    {
        public string AppId { get; set; }
        public string AppKey { get; set; }
    }

    public class YleCategory : RssLink
    {
        public bool IsSeries { get; set; }
    }

    public class YleUtil : SiteUtilBase
    {
        #region OnlineVideosUserConfiguration

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Vain ulkomailla katsottavat"), Description("Rajaa tarkemmin: Vain ulkomailla katsottavat")]
        protected bool onlyNonGeoblockedContent = false;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("På svenska"), Description("Arenan: På svenska")]
        protected bool inSwedish = false;

        #endregion

        #region Variables and constants

        private YleAppInfo appInfo = new YleAppInfo() { AppId = "89868a18", AppKey = "54bb4ea4d92854a2a45e98f961f0d7da" };

        private const string imageFormat = "https://images.cdn.yle.fi/image/upload/c_fill,d_yle-areena.jpg,f_auto,q_auto,w_320/{0}.jpg";
        
        #endregion

        #region Properties

        private string BaseUrl
        {
            get
            {
                return inSwedish ? "http://arenan.yle.fi/" : "http://areena.yle.fi/";
            }
        }

        private string ApiRegion
        {
            get
            {
                return onlyNonGeoblockedContent ? "world" : "fi";
            }
        }

        private string ApiLanguage
        {
            get
            {
                return inSwedish ? "sv" : "fi";
            }
        }

        private string ApiOtherLanguage
        {
            get
            {
                return inSwedish ? "fi" : "sv";
            }
        }

        #endregion

        #region Categories

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            GetGenres().ForEach(c => Settings.Categories.Add(c));
            Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > 0;
            GetStaticGenres().ForEach(c => Settings.Categories.Insert(0, c));
            return Settings.Categories.Count;
        }

        private List<RssLink> GetStaticGenres()
        {
            List<RssLink> genres = new List<RssLink>();
            string urlFormat = "{0}tv/{1}/{2}";
            string Url = string.Format(urlFormat, BaseUrl, inSwedish ? "program" : "ohjelmat", "");
            genres.Add(new RssLink() { Name = "Teema", Url = string.Format(urlFormat, BaseUrl, inSwedish ? "program" : "ohjelmat", "yle-teema"), HasSubCategories = true });
            genres.Add(new RssLink() { Name = "Fem", Url = string.Format(urlFormat, BaseUrl, inSwedish ? "program" : "ohjelmat", "yle-fem"), HasSubCategories = true });
            genres.Add(new RssLink() { Name = "TV2", Url = string.Format(urlFormat, BaseUrl, inSwedish ? "program" : "ohjelmat", "yle-tv2"), HasSubCategories = true });
            genres.Add(new RssLink() { Name = "TV1", Url = string.Format(urlFormat, BaseUrl, inSwedish ? "program" : "ohjelmat", "yle-tv1"), HasSubCategories = true });
            return genres;
        }

        private List<RssLink> GetGenres()
        {
            List<RssLink> genres = new List<RssLink>();
            string data = GetWebData(BaseUrl + "tv");
            Regex r = new Regex(@"/(?<url>tv/[^/]+/[^""]*)"">(?<name>[^<]+)<");
            foreach(Match m in r.Matches(data))
            {
                if (m.Success && !m.Groups["url"].Value.Contains("/"+ (inSwedish ? "alla" : "kaikki")))
                {
                    genres.Add(new RssLink() { Name = m.Groups["name"].Value, Url = BaseUrl + m.Groups["url"].Value, HasSubCategories = true });
                }
            }
            return genres;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            Regex r = new Regex(@"&quot;(?<url>https[^\?]+?/packages/[^\?]+)");
            Match m = r.Match(GetWebData((parentCategory as RssLink).Url));
            parentCategory.SubCategories = new List<Category>();
            if (m.Success)
            {
                if (parentCategory.Name == "X3M" || parentCategory.Name == "KIOSKI")
                {
                    GetGenrePrograms(parentCategory, m.Groups["url"].Value, 0, sortOrder: (inSwedish ? "rekommenderas" : "suositellut"));
                }
                else
                {
                    GetGenrePrograms(parentCategory, m.Groups["url"].Value, 0);
                }
            }
            parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
            return parentCategory.SubCategories.Count;
        }

        private void GetGenrePrograms(Category parentCategory, string apiUrl, int offset, int limit = 100, string sortOrder = "a-o")
        {
            string url = string.Format("{0}?o={1}&app_id={2}&app_key={3}&client=yle-areena-web&language={4}&v=5&offset={5}&limit={6}&filter.region={7}",
                apiUrl,
                sortOrder,
                appInfo.AppId,
                appInfo.AppKey,
                ApiLanguage,
                offset,
                limit,
                ApiRegion);
            List<Category> programs = new List<Category>();
            JObject json = GetWebData<JObject>(url);
            int noOfpackages = 0;
            foreach(JToken programToken in json["data"].Value<JArray>())
            {
                string type = programToken["pointer"]["type"].Value<string>();
                if (type != "package")
                {
                    YleCategory program = new YleCategory();
                    program.IsSeries = type == "series";
                    program.Name = programToken["title"].Value<string>();
                    program.Url = programToken["labels"].First(t => t["type"].Value<string>() == "itemId")["raw"].Value<string>();
                    program.Description = programToken["description"].Value<string>();
                    program.Thumb = string.Format(imageFormat, programToken["image"]["id"].Value<string>());
                    program.ParentCategory = parentCategory;
                    programs.Add(program);
                }
                else
                {
                    noOfpackages++;
                }
            }
            if (programs.Count + noOfpackages >= limit)
            {
                NextPageCategory next = new NextPageCategory();
                next.Url = apiUrl;
                next.Other = (offset + limit).ToString();
                next.ParentCategory = parentCategory;
                programs.Add(next);
            }
            parentCategory.SubCategories.AddRange(programs);
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            category.ParentCategory.SubCategories.Remove(category);
            int offset = int.Parse(category.GetOtherAsString());
            GetGenrePrograms(category.ParentCategory, category.Url, offset);
            return category.ParentCategory.SubCategories.Count - offset;
        }

        #endregion

        #region Videos

        private int currentSeriesVideoOffset = 0;
        private YleCategory currentCategory;
        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            currentCategory = category as YleCategory;
            HasNextPage = false;
            if (currentCategory.IsSeries)
            {
                string urlFormat = "https://programs-cdn.api.yle.fi/v1/episodes/{0}.json?availability=ondemand&limit=100&order=episode.hash%3Aasc%2Cpublication.starttime%3Aasc%2Ctitle.fi%3Aasc&app_id={1}&app_key={2}&offset={3}";
                JObject json = GetWebData<JObject>(string.Format(urlFormat, currentCategory.Url, appInfo.AppId, appInfo.AppKey, currentSeriesVideoOffset));
                if (json["data"] != null && json["data"].Count() > 0)
                {
                    foreach (JToken data in json["data"])
                    {
                        VideoInfo video = GetVideoInfoFromJsonData(data, currentCategory.IsSeries);
                        if (video != null)
                        {
                            videos.Add(video);
                        }
                    }
                }
                int count = json["meta"]["count"].Value<int>();
                HasNextPage = count > currentSeriesVideoOffset + 100;
                if (HasNextPage)
                {
                    currentSeriesVideoOffset += 100;
                }
                else
                {
                    currentSeriesVideoOffset = 0;
                }
            }
            else
            {
                currentSeriesVideoOffset = 0;
                string urlFormat = "http://arenan.yle.fi/api/programs/v1/id/{0}.json?app_id={1}&app_key={2}";
                JObject json = GetWebData<JObject>(string.Format(urlFormat, currentCategory.Url, appInfo.AppId, appInfo.AppKey));
                VideoInfo video = GetVideoInfoFromJsonData(json["data"], currentCategory.IsSeries);
                if (video != null)
                {
                    videos.Add(video);
                }
            }
            return videos;
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            //Only TV-Series have paging
            return GetVideos(currentCategory);
        }

        private VideoInfo GetVideoInfoFromJsonData(JToken data, bool isSeries)
        {
            VideoInfo video = null;
            JToken publicationEvents = data["publicationEvent"];
            JToken publicationEvent = publicationEvents.FirstOrDefault(pe =>
                    pe["type"].Value<string>() == "OnDemandPublication" &&
                    pe["temporalStatus"].Value<string>() == "currently" &&
                    pe["media"] != null &&
                    pe["media"]["available"] != null &&
                    pe["media"]["available"].Value<bool>() &&
                    (!onlyNonGeoblockedContent || pe["region"].Value<string>() == "World")
                    );
            if (publicationEvent != null)
            {
                string id = publicationEvent["media"]["id"].Value<string>();
                DateTime start = publicationEvent["startTime"].Value<DateTime>();
                string title = string.Empty;
                string episodeTitle = string.Empty;
                string type = data["type"].Value<string>();
                if (isSeries && type == "TVProgram")
                {
                    JToken episode = data["episodeNumber"];
                    if (episode != null && episode.Value<uint?>() != null)
                    {
                        episodeTitle = (inSwedish ? "Avsnitt" : "Jaksot") + " " + episode.Value<uint>() + " ";
                    }
                }
                JToken itemTitle = data["itemTitle"];
                if (itemTitle != null && itemTitle.Count() > 0)
                {
                    JToken itemTitleLang = itemTitle[ApiLanguage];
                    JToken itemTitleOtherLang = itemTitle[ApiOtherLanguage];
                    if (itemTitleLang != null)
                    {
                        title = itemTitleLang.Value<string>();
                    }
                    else if (itemTitleOtherLang != null)
                    {
                        title = itemTitleOtherLang.Value<string>();
                    }
                }
                if (string.IsNullOrEmpty(title))
                {
                    JToken promotionTitle = data["promotionTitle"];
                    if (promotionTitle != null && promotionTitle.Count() > 0)
                    {
                        JToken titleLang = promotionTitle[ApiLanguage];
                        JToken titleOtherLang = promotionTitle[ApiOtherLanguage];
                        if (titleLang != null)
                        {
                            title = titleLang.Value<string>();
                        }
                        else if (titleOtherLang != null)
                        {
                            title = titleOtherLang.Value<string>();
                        }
                    }
                }
                if (string.IsNullOrEmpty(title))
                {
                    JToken titleToken = data["title"];
                    if (titleToken != null && titleToken.Count() > 0)
                    {
                        JToken titleLang = titleToken[ApiLanguage];
                        JToken titleOtherLang = titleToken[ApiOtherLanguage];
                        if (titleLang != null)
                        {
                            title = titleLang.Value<string>();
                        }
                        else if (titleOtherLang != null)
                        {
                            title = titleOtherLang.Value<string>();
                        }
                    }
                }
                title = episodeTitle + title;
                string description = string.Empty;
                JToken descriptionJson = data["description"];
                if (descriptionJson != null && descriptionJson.Count() > 0)
                {
                    description = descriptionJson[ApiLanguage] == null ? descriptionJson[ApiOtherLanguage].Value<string>() : descriptionJson[ApiLanguage].Value<string>();
                }
                JToken imageJson = data["image"];
                string image = string.Empty;
                if (imageJson != null && imageJson.Count() > 0 && imageJson["available"] != null && imageJson["available"].Value<bool>())
                {
                    image = (imageJson["id"] != null) ? string.Format(imageFormat, imageJson["id"].Value<string>()) : image;
                }

                video = new VideoInfo()
                {
                    Title = title,
                    Description = description,
                    Thumb = image,
                    Airdate = start.ToString(),
                    VideoUrl = id
                };
            }
            return video;
        }

        private string DecryptData(string data)
        {
            byte[] bytes = Convert.FromBase64String(data);
            RijndaelManaged rijndael = new RijndaelManaged();
            byte[] iv = new byte[16];
            Array.Copy(bytes, iv, 16);
            rijndael.IV = iv;
            rijndael.Key = Encoding.ASCII.GetBytes("yjuap4n5ok9wzg43");
            rijndael.Mode = CipherMode.CFB;
            rijndael.Padding = PaddingMode.Zeros;
            ICryptoTransform decryptor = rijndael.CreateDecryptor(rijndael.Key, rijndael.IV);
            int padLen = 16 - bytes.Length % 16;
            byte[] newbytes = new byte[bytes.Length - 16 + padLen];
            Array.Copy(bytes, 16, newbytes, 0, bytes.Length - 16);
            Array.Clear(newbytes, newbytes.Length - padLen, padLen);
            string result = null;
            using (MemoryStream msDecrypt = new MemoryStream(newbytes))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        result = srDecrypt.ReadToEnd();
                    }
                }
            }
            return result;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string url = string.Format("http://player.yle.fi/api/v1/media.jsonp?protocol=HDS&client=areena-flash-player&id={0}", video.VideoUrl);
            JObject json = GetWebData<JObject>(url, cache: false);
            JToken hdsStream = json["data"]["media"]["HDS"].FirstOrDefault(h => h["subtitles"] != null && h["subtitles"].Count() > 0);
            if (hdsStream == null)
            {
                hdsStream = json["data"]["media"]["HDS"].First;
            }
            else
            {
                JToken subtitle = hdsStream["subtitles"].FirstOrDefault(s => s["lang"].Value<string>() == ApiLanguage);
                if (subtitle == null) subtitle = hdsStream["subtitles"].FirstOrDefault(s => s["lang"].Value<string>() == ApiOtherLanguage);
                if (subtitle != null && subtitle["uri"] != null) video.SubtitleUrl = subtitle["uri"].Value<string>();
            }
            string data = hdsStream["url"].Value<string>();
            string result = DecryptData(data);
            Regex r;
            bool useHls = !result.Contains(".f4m") && !result.Contains("*~hmac");
            if (useHls)
            {
                url = string.Format("http://player.yle.fi/api/v1/media.jsonp?protocol=HLS&client=areena-flash-player&id={0}", video.VideoUrl);
                json = GetWebData<JObject>(url, cache: false);
                data = json["data"]["media"]["HLS"].First["url"].Value<string>();
                result = DecryptData(data);
                r = new Regex(@"(?<url>.*\.m3u8)");
                Match m = r.Match(result);
                if (m.Success)
                {
                    result = m.Groups["url"].Value;
                    video.PlaybackOptions = HlsPlaylistParser.GetPlaybackOptions(GetWebData(result), result);
                    result = video.PlaybackOptions.Last().Value;

                }
            }
            else
            {
                r = new Regex(@"(?<url>.*hmac=[a-z0-9]*)");
                Match m = r.Match(result);
                if (m.Success)
                {
                    result = m.Groups["url"].Value + "&g=" + HelperUtils.GetRandomChars(12) + "&hdcore=3.8.0&plugin=flowplayer-3.8.0.0";
                }
            }
            return result;
        }

        #endregion

        #region Search

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
            JObject json = GetWebData<JObject>(string.Format("https://areena.api.yle.fi/v1/ui/search?app_id=areena_web_personal_prod&app_key=6c64d890124735033c50099ca25dd2fe&client=yle-areena-web&language={0}&v=4&episodes=false&packages=false&query={1}&service=tv&limit=100", ApiLanguage, HttpUtility.UrlEncode(query)));
            foreach (JToken programToken in json["data"].Value<JArray>())
            {
                string type = programToken["pointer"]["type"].Value<string>();
                YleCategory program = new YleCategory();
                program.IsSeries = type == "series";
                program.Name = programToken["title"].Value<string>();
                program.Description = programToken["description"] != null ? programToken["description"].Value<string>() : "";
                program.Url = programToken["pointer"]["uri"].Value<string>().Replace("yleareena://items/", "");
                program.Thumb = programToken["image"] != null ? string.Format(imageFormat, programToken["image"]["id"].Value<string>()) : "";
                results.Add(program);
            }
            return results;
        }

        #endregion
    }
}

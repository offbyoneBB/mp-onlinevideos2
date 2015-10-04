using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineVideos.Sites.WebAutomation.ConnectorImplementations.SkyGo.Extensions
{
    public static class SkyGoJSONExtensions
    {
        /// <summary>
        /// Load the category/video id from the href in the token - it will basically be everything after the last "/"
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static string GetIdFromHrefValue(this JToken token)
        {
            var id = token.GetValue("_href");
            id = id.Replace(id.Substring(0, id.LastIndexOf('/') + 1), "");
            return id;
        }

        /// <summary>
        /// Load the medium sized image for this token
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static string GetImage(this JToken token)
        {
            JToken mediumImage = null;
            if (token["images"] != null)
            {
                mediumImage = token["images"].Where(x => token.GetValue("type") == "medium").FirstOrDefault();
                if (mediumImage == null)
                    mediumImage = token["images"].FirstOrDefault();
            }
            return mediumImage == null ? string.Empty : mediumImage["url"].ToString();
        }

        /// <summary>
        /// Get the stars of the show
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static string GetStarring(this JToken token)
        {
            var starring = "";
            if (token["stars"] != null)
            {
                foreach (var star in token["stars"])
                    starring += (star.ToString().Length > 0 ? "," : "") + star.ToString();
            }
            return starring;
        }

        /// <summary>
        /// Create a video info object from the token
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static VideoInfo VideoInfoFromToken(this JToken token, string videoType = "episodes")
        {
            var video = new VideoInfo();
            video.Airdate = token.GetValue("yearOfRelease");
            video.Description = token.GetValue("synopsis") + "\r\n" + GetStarring(token);
            
            var mins = 0;

            if (int.TryParse(  token.GetValue("durationMinutes").Replace(".0", ""),out mins))
                video.Length = TimeSpan.FromMinutes(mins).ToString();
            video.Thumb = token.GetImage();
            video.Title = token.GetValue("title");
            video.Other = videoType + "/" + token.GetValue("id");
            return video;
        }

        /// <summary>
        /// Load the _Links sub-section from the JSON data returned from the URL - this will be our entry point to the data
        /// </summary>
        /// <param name="url"></param>
        /// <param name="parentCategType"></param>
        /// <returns></returns>
        public static JToken GetLinksTokensFromUrl(this string url, SkyGoCategoryData.CategoryType parentCategType)
        {
            var browser = new SkyGoBrowserSession();
            var browserResponse = browser.LoadAsStr(url);
            browserResponse = browserResponse.Replace("azPage(", "").Replace("catalogueFeed(", "").Replace("series(","");
            browserResponse = browserResponse.Replace(");", "");

            var jsonObj = JObject.Parse(browserResponse);

            if (jsonObj == null || jsonObj["_links"] == null) return null;

            var tmpObj = jsonObj["_links"];

            if (parentCategType == SkyGoCategoryData.CategoryType.CatchUpSubCategory)
            {
                if (jsonObj["_links"][2]["_links"] != null)
                    tmpObj = jsonObj["_links"][2]["_links"][2]["_links"];
            }
            return tmpObj;
        }

        /// <summary>
        /// Load the Live TV channels
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static List<VideoInfo> GetChannelsFromURL(this string url)
        {
            var browser = new SkyGoBrowserSession();
            var browserResponse = browser.LoadAsStr(url);
            browserResponse = browserResponse.Replace("feedChannelList(", "");
            browserResponse = browserResponse.Replace(");", "");

            var jsonObj = JObject.Parse(browserResponse);
            var result = new List<VideoInfo>();

            foreach (var item in jsonObj["linearStreams"].Children())
            {
                var video = new VideoInfo();
                //video.Description = token.GetValue("synopsis") + "\r\n" + GetStarring(token);

                video.Thumb = item.GetImage();
                video.Title = item.GetValue("title");
                video.Other = "LTV~" + item.GetValue("epgChannelId");
                result.Add(video);

            }
            return result.OrderBy(x=>x.Title).ToList();
        }

        /// <summary>
        /// Get a value from the token as a string
        /// </summary>
        /// <param name="token"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetValue(this JToken token, string key)
        {
            var item = token[key];

            if (item == null) return string.Empty;

            return item.ToString();
        }

        /// <summary>
        /// Get the now/next information into the description of the video
        /// </summary>
        /// <param name="videos"></param>
        public static void LoadNowNext(this List<VideoInfo> videos)
        {
            var pool = new List<Task>();
            try
            {
                // Loop through all the videos
                foreach (var video in videos)
                {
                    pool.Add(Task.Factory.StartNew(() => GetNowNext(video)));
                }
                var timeout = OnlineVideoSettings.Instance.UtilTimeout <= 0 ? 30000 : OnlineVideoSettings.Instance.UtilTimeout * 1000;
                Task.WaitAll(pool.ToArray(), timeout);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        /// <summary>
        /// Load the now/next info on a separate thread
        /// </summary>
        /// <param name="video"></param>
        private static void GetNowNext(VideoInfo video)
        {
            var browser = new SkyGoBrowserSession();
            var browserResponse = browser.LoadAsStr(Properties.Resources.SkyGo_LiveTvGetNowNextUrl(video.AssetId()));

            try
            {
                var jsonObj = JObject.Parse(browserResponse);
                var i = 0;
                video.Title = string.Empty;

                foreach (var item in jsonObj["listings"][video.AssetId()].Children())
                {
                    var head = "Now:";
                    var title = "Now: ";
                    if (i > 0)
                    {
                        head = "\r\nNext:";
                        title = ", Next: ";
                    }

                    var time = 0L;

                    long.TryParse(item.GetValue("s"), out time);

                    video.Title += string.Format("{0} {1} ({2})", title, item.GetValue("t"), FromUnixTime(time).ToString("HH:mm"));

                    video.Description += string.Format("{0} {1} ({2}) {3}", head, item.GetValue("t"), FromUnixTime(time).ToString("HH:mm"), item.GetValue("d"));
                    i++;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        /// <summary>
        /// Convert from unix epoch
        /// </summary>
        /// <param name="unixTime"></param>
        /// <returns></returns>
        public static DateTime FromUnixTime(long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }
    }
}

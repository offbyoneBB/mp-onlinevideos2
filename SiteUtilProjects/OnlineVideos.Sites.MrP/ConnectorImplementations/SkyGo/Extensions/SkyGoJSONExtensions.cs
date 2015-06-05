using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    }
}

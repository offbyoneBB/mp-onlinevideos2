using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.ComponentModel;

namespace OnlineVideos.Sites
{
    public class TEDTalksUtil : GenericSiteUtil
    {
        protected Regex regEx_nextPageOverride;
        protected Regex regEx_VideoListSearchResults;

        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a search results html page for videos. Group names: 'VideoUrl', 'ImageUrl', 'Title', 'Description'.")]
        protected string videoListSearchResultsRegEx;


        public override void Initialize(OnlineVideos.SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            if (!string.IsNullOrEmpty(videoListSearchResultsRegEx)) regEx_VideoListSearchResults = new Regex(videoListSearchResultsRegEx, defaultRegexOptions);

        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            return Parse(nextPageUrl, null, regEx_nextPageOverride);
            regEx_nextPageOverride = null;
        }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            // if an override Encoding was specified, we need to UrlEncode the search string with that encoding
            if (encodingOverride != null) query = HttpUtility.UrlEncode(encodingOverride.GetBytes(query));

            List<SearchResultItem> result;
            if (string.IsNullOrEmpty(searchPostString))
            {
                 result = Parse(string.Format(searchUrl, query), null, regEx_VideoListSearchResults)
                    .ConvertAll<SearchResultItem>(v => v as SearchResultItem);
            }
            else
            {
                result = Parse(searchUrl, GetWebData<string>(searchUrl, string.Format(searchPostString, query), cookies: GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride))
                    .ConvertAll<SearchResultItem>(v => v as SearchResultItem);
            }

            if (nextPageAvailable)
                regEx_nextPageOverride = regEx_VideoListSearchResults;

            return result;

        }

        public override int DiscoverDynamicCategories()
        {
            foreach (var cat in Settings.Categories)
                cat.Other = true;
            int res = base.DiscoverDynamicCategories();
            foreach (var cat in Settings.Categories)
                cat.HasSubCategories = (true.Equals(cat.Other));
            return res;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            video.PlaybackOptions = new Dictionary<string, string>();
            string data = GetWebData(video.VideoUrl);
            var talkDetailsMatch = regEx_FileUrl.Match(data);
            if (talkDetailsMatch.Success)
            {
                JObject talkDetails = JsonConvert.DeserializeObject(talkDetailsMatch.Groups["json"].Value) as JObject;
                foreach (JProperty htmlStream in talkDetails["talks"][0]["nativeDownloads"])
                {
                    video.PlaybackOptions.Add(htmlStream.Name, htmlStream.Value.ToString());
                }
            }
            return video.PlaybackOptions.Count > 0 ? video.PlaybackOptions.Last().Value : "";
        }
    }
}

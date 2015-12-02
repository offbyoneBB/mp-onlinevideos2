using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Globalization;

namespace OnlineVideos.Sites
{
    public class RTLGemistUtil : GenericSiteUtil
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public override int DiscoverDynamicCategories()
        {
            /*let know that the rsslinks have sub categories*/
            for (int i = 0; i < Settings.Categories.Count; i++)
                if (!(Settings.Categories[i] is Group))
                    Settings.Categories[i].HasSubCategories = true;
            return 0;
        }

        public override int ParseSubCategories(Category parentCategory, string data)
        {
            List<Category> dynamicSubCategories = new List<Category>(); // put all new discovered Categories in a separate list
            JObject jsonData = GetWebData<JObject>((parentCategory as RssLink).Url);
            Log.Debug("Number of items: " + jsonData["meta"].Value<string>("nr_of_items_total"));
            if (jsonData != null)
            {
                foreach (JToken item in jsonData["abstracts"])
                {
                    RssLink cat = new RssLink();
                    cat.Url = String.Format("http://www.rtl.nl/system/s4m/vfd/version=2/fun=abstract/d=a3t/fmt=progressive/ak={0}/output=json/pg=1/", item.Value<string>("key"));
                    cat.Name = item.Value<string>("name");
                    cat.Thumb = jsonData["meta"].Value<string>("poster_base_url") + item.Value<string>("coverurl");
                    cat.Description = item.Value<string>("synopsis");
                    cat.ParentCategory = parentCategory;
                    dynamicSubCategories.Add(cat);
                }
                // discovery finished, copy them to the actual list -> prevents double entries if error occurs in the middle of adding
                if (parentCategory.SubCategories == null) parentCategory.SubCategories = new List<Category>();
                foreach (Category cat in dynamicSubCategories) parentCategory.SubCategories.Add(cat);
                parentCategory.SubCategoriesDiscovered = dynamicSubCategories.Count > 0; // only set to true if actually discovered (forces re-discovery until found)
            }
            return parentCategory.SubCategories == null ? 0 : parentCategory.SubCategories.Count;

        }

        protected override List<VideoInfo> Parse(string url, string data)
        {
            JObject jsonEpsData = GetWebData<JObject>(url);

            List<VideoInfo> videoList = new List<VideoInfo>();

            if (jsonEpsData != null)
            {
                /*
                JToken episodes = jsonEpsData["episodes"];
                foreach (var ep in episodes)
                {
                    JToken epData = getEpisodeData(ep.Value<string>("key"), jsonEpsData);
                    DateTime airdate = UnixTimeToDateTime(epData["original_date"].Value<string>());

                    VideoInfo videoInfo = CreateVideoInfo();
                    videoInfo.Title = ep.Value<string>("name");
                    videoInfo.VideoUrl = String.Empty;
                    videoInfo.Thumb = String.Format("{0}{1}", jsonEpsData["meta"]["poster_base_url"].Value<string>(), epData["uuid"].Value<string>());
                    //videoInfo.Length = epData["duration"].Value<string>();
                    videoInfo.Airdate = String.Format("{0}, {1} {2}", airdate.ToString("dddd"), airdate.ToShortDateString(), airdate.ToShortTimeString());
                    videoInfo.Description = String.Format("{0}\n{1} {2}", epData["classname"].Value<string>(), ep.Value<string>("synopsis"), epData["synopsis"].Value<string>());
                    videoList.Add(videoInfo);
                }*/
                JToken videos = jsonEpsData["material"];
                foreach (var vid in videos)
                {
                    JToken eInfo = getEpisodeInfo(vid["episode_key"].Value<string>(), jsonEpsData);
                    DateTime airdate = UnixTimeToDateTime(vid["original_date"].Value<string>());

                    VideoInfo videoInfo = CreateVideoInfo();

                    if(eInfo != null)
                    {
                        videoInfo.Title = String.Format("{0} {1}", eInfo.Value<string>("name"), vid.Value<string>("title"));
                        videoInfo.Description = String.Format("{0}\n{1}\n{2}", vid["classname"].Value<string>(), eInfo["synopsis"].Value<string>(), vid["synopsis"].Value<string>());
                    }
                    else
                    {
                        videoInfo.Title = vid.Value<string>("title");
                        videoInfo.Description = String.Format("{0}\n{1}", vid["classname"].Value<string>(), vid["synopsis"].Value<string>());
                    }
                    videoInfo.VideoUrl = String.Format("http://www.rtl.nl/system/s4m/vfd/version=2/d=a3t/fmt=progressive/fun=abstract/uuid={0}/output=json/", vid["uuid"].Value<string>());
                    videoInfo.Thumb = String.Format("{0}{1}", jsonEpsData["meta"]["poster_base_url"].Value<string>(), vid["uuid"].Value<string>());
                    videoInfo.Length = vid["duration"].Value<string>();
                    videoInfo.Airdate = String.Format("{0}, {1} {2}", airdate.ToString("dddd"), airdate.ToShortDateString(), airdate.ToShortTimeString());
                    videoList.Add(videoInfo);
                }
            }
            else
                return null;

            return videoList;
        }


        public override string GetVideoUrl(VideoInfo video)
        {
            //string encodedHTML = getUrl(video);
            string resultUrl = GetFormattedVideoUrl(video);
            string playListUrl = GetPlaylistUrl(resultUrl);
            if (String.IsNullOrEmpty(playListUrl))
                return String.Empty; // if no match, return empty url -> error

            // 3.b find a match in the retrieved data for the final playback url
            if (regEx_FileUrl != null)
            {
                video.PlaybackOptions = GetPlaybackOptions(playListUrl);
                if (video.PlaybackOptions.Count == 0) return ""; // if no match, return empty url -> error
                else
                {
                    // return first found url as default
                    var enumer = video.PlaybackOptions.GetEnumerator();
                    enumer.MoveNext();
                    resultUrl = enumer.Current.Value;
                }
                if (video.PlaybackOptions.Count == 1) video.PlaybackOptions = null; // only one url found, PlaybackOptions not needed
            }

            //parse url
            JObject jsonVidData = GetWebData<JObject>(resultUrl);
            string videohost = jsonVidData["meta"]["videohost"].Value<string>();

            return String.Format("{0}{1}", videohost, jsonVidData["material"].First()["videopath"].Value<string>());

            
        }

        public IEnumerable<JToken> getEpisodeVideos(string key, JObject jsonEpsData)
        {
            var episodes =
                from e in (JArray)jsonEpsData["material"]
                where e["episode_key"].Value<string>() == key
                select e;

            return episodes;
        }

        public JToken getEpisodeInfo(string key, JObject jsonEpsData)
        {
            Log.Debug("Find episode with key {0}", key);
            var episodes =
                from e in (JArray)jsonEpsData["episodes"]
                where e["key"].Value<string>() == key
                select e;

            if (episodes.Values().Count() > 0)
                return episodes.First();
            else
                return null;
        }

        public JToken getEpisodeData(string key, JObject jsonEpsData)
        {
            var episodes =
                from e in (JArray)jsonEpsData["material"]
                where e["episode_key"].Value<string>() == key
                where e["classname"].Value<string>() == "uitzending"
                select e;
            if(episodes.Values().Count() > 0)
                return episodes.First();
            else
                return getEpisodeVideos(key, jsonEpsData).First();
        }

        public static DateTime UnixTimeToDateTime(string text)
        {
            double seconds = double.Parse(text, CultureInfo.InvariantCulture);
            return Epoch.AddSeconds(seconds);
        }
    }
}

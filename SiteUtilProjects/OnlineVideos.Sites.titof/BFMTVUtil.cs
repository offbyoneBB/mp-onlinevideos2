using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace OnlineVideos.Sites
{
    public class BFMTVUtil : GenericSiteUtil
    {
        #region Fields

        internal string _img = "bfmtv";
        internal string _title = "BFM TV";
        internal string _urlMenu = "http://api.nextradiotv.com/bfmtv-android/4/{0}/getMainMenu";
        internal string _urlToken = "http://api.nextradiotv.com/bfmtv-android/4/";
        internal string _urlVideo = "http://api.nextradiotv.com/bfmtv-android/4/{0}/getVideo?idVideo={1}&quality=2";
        internal string _urlVideoList = "http://api.nextradiotv.com/bfmtv-android/4/{0}/getVideosList?count=40&page=1&category={1}";

        #endregion Fields

        #region Methods

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            string sContent = GetWebData(string.Format(_urlMenu, GetToken()));
            JObject jResult = JObject.Parse(sContent);
            JArray tItems = (JArray)jResult["menu"]["right"];

            if (tItems != null)
            {
                foreach (JObject item in tItems)
                {
                    if (((string)item["type"]) == "REPLAY")
                    {
                        RssLink cat = new RssLink();
                        cat.Url = (string)item["category"];
                        cat.Name = (string)item["title"];
                        cat.Thumb = (string)item["image_url"];
                        cat.Other = (string)item["category"];
                        cat.HasSubCategories = false;
                        Settings.Categories.Add(cat);
                    }
                }
            }

            if (tItems == null)
            {
                tItems = (JArray)jResult["menu"]["left"];
                foreach (JObject item in tItems)
                {
                    if (((string)item["type"]) == "ARTICLES")
                    {
                        RssLink cat = new RssLink();
                        cat.Url = ((int)item["category"]).ToString();
                        cat.Name = (string)item["title"];
                        cat.Thumb = (string)item["image_url"];
                        cat.Other = ((int)item["category"]).ToString();
                        cat.HasSubCategories = false;
                        Settings.Categories.Add(cat);
                    }
                }
            }

            if (tItems == null)
                return 0;

            return Settings.Categories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> tVideos = new List<VideoInfo>();
            string sUrl = string.Format(_urlVideoList, GetToken(), (category as RssLink).Url);

            string sContent = GetWebData(sUrl);
            JObject tlist = JObject.Parse(sContent);

            foreach (JObject item in tlist["videos"])
            {
                int duration = 0;
                duration = (int)item["video_duration_ms"];
                if (duration > 0) duration = duration / 1000;

                VideoInfo vid = new VideoInfo()
                {
                    Other = (string)item["video"],
                    Title = (string)item["title"],
                    Description = (string)item["description"],
                    Thumb = (string)item["image_small"],
                    Length = duration.ToString()
                };
                tVideos.Add(vid);
            }

            return tVideos;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string sContent = GetWebData(string.Format(_urlVideo, GetToken(), video.Other));
            JObject jsonParser = JObject.Parse(sContent);

            string video_url = (string)jsonParser["video"]["video_url"];

            List<String> tUrl = new List<string>();
            try
            {
                JArray turlArray = (JArray)jsonParser["video"]["medias"];
                foreach (JObject item in turlArray)
                {
                    tUrl.Add((string)item["video_url"]);
                }
            }
            catch
            { }

            if (tUrl.Count > 2)
            {
                video.PlaybackOptions = new Dictionary<string, string>();
                video.PlaybackOptions.Add("SD", tUrl[tUrl.Count - 2]);
                video.PlaybackOptions.Add("HD", tUrl[tUrl.Count - 1]);
            }
            else
            {
                tUrl.Add(video_url);
            }

            return tUrl[tUrl.Count - 1];
        }

        private string GetToken()
        {
            string sContent = GetWebData(_urlToken);
            Newtonsoft.Json.Linq.JObject jsonParser = Newtonsoft.Json.Linq.JObject.Parse(sContent);
            return (string)jsonParser["session"]["token"];
        }

        #endregion Methods
    }
}
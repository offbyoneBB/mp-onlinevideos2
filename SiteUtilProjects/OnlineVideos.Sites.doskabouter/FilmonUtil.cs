using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;
using OnlineVideos.MPUrlSourceFilter;

namespace OnlineVideos.Sites
{
    public class FilmonUtil : SiteUtilBase
    {
        private CookieContainer cc;
        private const string userAgent = @"Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";
        public override int DiscoverDynamicCategories()
        {
            if (Settings.Categories == null) Settings.Categories = new BindingList<Category>();
            cc = new CookieContainer();
            string data = GetWebData(@"https://www.filmon.com/tv/live", userAgent: userAgent, cookies: cc);
            string jsondata = @"{""result"":" + Helpers.StringUtils.GetSubString(data, "var groups =", @"if(!$.isArray").Trim().TrimEnd(';') + "}";
            JToken jt = JObject.Parse(jsondata) as JToken;
            foreach (JToken jCat in jt["result"] as JArray)
            {
                RssLink cat = new RssLink();
                cat.Name = jCat.Value<string>("title");
                cat.Description = jCat.Value<string>("description");
                cat.Thumb = jCat.Value<string>("logo_uri");
                Settings.Categories.Add(cat);
                JArray channels = jCat["channels"] as JArray;
                List<VideoInfo> videos = new List<VideoInfo>();
                foreach (JToken channel in channels)
                {
                    VideoInfo video = new VideoInfo();
                    video.Thumb = channel.Value<string>("logo");
                    video.Description = channel.Value<string>("description");
                    video.Title = channel.Value<string>("title");
                    video.VideoUrl = @"https://www.filmon.com/ajax/getChannelInfo";
                    video.Other = String.Format(@"channel_id={0}&quality=low", channel.Value<string>("id"));
                    videos.Add(video);
                }
                cat.Other = videos;
            }
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            return (List<VideoInfo>)category.Other;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            CookieContainer newCc = new CookieContainer();
            foreach (Cookie c in cc.GetCookies(new Uri(@"https://www.filmon.com/")))
            {
                newCc.Add(c);
            }

            NameValueCollection headers = new NameValueCollection();
            headers.Add("Accept", "*/*");
            headers.Add("User-Agent", userAgent);
            headers.Add("X-Requested-With", "XMLHttpRequest");
            string webdata = GetWebData(video.VideoUrl, (string)video.Other, newCc, headers: headers);

            JToken jt = JObject.Parse(webdata) as JToken;
            JArray streams = jt.Value<JArray>("streams");
            video.PlaybackOptions = new Dictionary<string, string>();
            foreach (JToken stream in streams)
            {
                string serverUrl = stream.Value<string>("url");

                RtmpUrl res = new RtmpUrl(serverUrl);
                res.Live = true;
                res.PlayPath = stream.Value<string>("name");

                int p = serverUrl.IndexOf("live/?id");
                res.App = serverUrl.Substring(p);
                video.PlaybackOptions.Add(stream.Value<string>("quality"), res.ToString());
            }

            return video.PlaybackOptions.First().Value;
        }

    }
}

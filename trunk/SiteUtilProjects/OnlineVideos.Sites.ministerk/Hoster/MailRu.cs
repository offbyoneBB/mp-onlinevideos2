using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class MailRu : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "mail.ru";
        }

        public override Dictionary<string, string> GetPlaybackOptions(string url)
        {
            Dictionary<string, string> playbackOptions = new Dictionary<string, string>();
            CookieContainer cc = new CookieContainer();
            string data = GetWebData(url, cookies: cc);
            Regex rgx = new Regex(@"""metadataUrl"":""(?<url>[^""]*)");
            Match m = rgx.Match(data);
            if (m.Success)
            {
                JObject json = GetWebData<JObject>(m.Groups["url"].Value, cookies:cc);
                JToken videos = json["videos"];
                if (videos != null)
                {
                    foreach(JToken video in videos)
                    {
                        string key = video["key"].Value<string>();
                        string videoUrl = video["url"].Value<string>();
                        MPUrlSourceFilter.HttpUrl httpUrl = new MPUrlSourceFilter.HttpUrl(videoUrl);
                        CookieCollection cookies = cc.GetCookies(new Uri("http://my.mail.ru"));
                        httpUrl.Cookies.Add(cookies);
                        playbackOptions.Add(key, httpUrl.ToString());
                    }
                }
            }
            playbackOptions = playbackOptions.OrderByDescending((p) =>
            {
                string resKey = p.Key.Replace("p","");
                int parsedRes = 0;
                int.TryParse(resKey, out parsedRes);
                return parsedRes;
            }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return playbackOptions;
        }

        public override string GetVideoUrl(string url)
        {
            Dictionary<string, string> urls = GetPlaybackOptions(url);
            if (urls.Count > 0)
                return urls.First().Value;
            else
                return "";
        }

    }
}

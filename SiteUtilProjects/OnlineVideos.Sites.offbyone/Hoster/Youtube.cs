using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using OnlineVideos.Hoster.Base;
using System.Web;

namespace OnlineVideos.Hoster
{
    public class Youtube : HosterBase
    {
        public override string getHosterUrl()
        {
            return "Youtube.com";
        }

        static readonly byte[] fmtOptionsQualitySorted = new byte[] { 38, 37, 45, 22, 44, 35, 43, 18, 34, 5, 0, 17, 13 };
        static Regex swfJsonArgs = new Regex(@"(?:var\s)?(?:swfArgs|'SWF_ARGS')\s*(?:=|\:)\s(?<json>\{.+\})|(?:\<param\sname=\\""flashvars\\""\svalue=\\""(?<params>[^""]+)\\""\>)|(flashvars=""(?<params>[^""]+)"")", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public override Dictionary<string, string> getPlaybackOptions(string url)
        {
            Dictionary<string, string> PlaybackOptions = null;

            string videoId = url;
            if (videoId.ToLower().Contains("youtube.com"))
            {
                // get an Id from the Url
                int p = videoId.LastIndexOf("watch?v="); // for http://www.youtube.com/watch?v=jAgBeAFZVgI
                if (p >= 0)
                    p += +8;
                else
                    p = videoId.LastIndexOf('/') + 1;
                int q = videoId.IndexOf('?', p);
                if (q < 0) q = videoId.IndexOf('&', p);
                if (q < 0) q = videoId.Length;
                videoId = videoId.Substring(p, q - p);
            }

            NameValueCollection Items = new NameValueCollection();
            string contents = "";
            try
            {
                contents = Sites.SiteUtilBase.GetWebData(string.Format("http://youtube.com/get_video_info?video_id={0}&has_verified=1", videoId));
                Items = System.Web.HttpUtility.ParseQueryString(contents);
                if (Items["status"] == "fail")
                {
                    contents = Sites.SiteUtilBase.GetWebData(string.Format("http://www.youtube.com/watch?v={0}&has_verified=1", videoId));
                    Match m = swfJsonArgs.Match(contents);
                    if (m.Success)
                    {
                        if (m.Groups["params"].Success)
                        {
                            Items = System.Web.HttpUtility.ParseQueryString(System.Web.HttpUtility.HtmlDecode(m.Groups["params"].Value));
                        }
                        else if (m.Groups["json"].Success)
                        {
                            Items.Clear();
                            foreach (var z in Newtonsoft.Json.Linq.JObject.Parse(m.Groups["json"].Value))
                            {
                                Items.Add(z.Key, z.Value.Value<string>(z.Key));
                            }
                        }
                    }
                }
            }
            catch { }

            if (!string.IsNullOrEmpty(Items.Get("url_encoded_fmt_stream_map")))
            {
                string swfUrl = Regex.Match(contents, "\"url\":\\s\"([^\"]+)\"").Groups[1].Value.Replace("\\/", "/");// "url": "http:\/\/s.ytimg.com\/yt\/swfbin\/watch_as3-vflOCLBVA.swf"

                string[] FmtUrlMap = Items["url_encoded_fmt_stream_map"].Split(',');
                string[] FmtList = Items["fmt_list"].Split(',');

                List<KeyValuePair<string[], string>> qualities = new List<KeyValuePair<string[], string>>();
                for (int i = 0; i < FmtList.Length; i++) qualities.Add(new KeyValuePair<string[], string>(FmtList[i].Split('/'), FmtUrlMap[i]));
                qualities.Sort(new Comparison<KeyValuePair<string[], string>>((a,b)=>
                {
                    return Array.IndexOf(fmtOptionsQualitySorted, byte.Parse(b.Key[0])).CompareTo(Array.IndexOf(fmtOptionsQualitySorted, byte.Parse(a.Key[0])));
                }));

                PlaybackOptions = new Dictionary<string, string>();
                foreach (var quality in qualities)
                {
                    var urlOptions = HttpUtility.ParseQueryString(quality.Value);
                    string type = urlOptions.Get("type");
                    if (!string.IsNullOrEmpty(type))
                    {
                        type = Regex.Replace(type, @"; codecs=""[^""]*""", "");
                        type = type.Substring(type.LastIndexOfAny(new char[] { '/', '-' }) + 1);
                    }
                    string finalUrl = urlOptions.Get("url");
                    if (!string.IsNullOrEmpty(finalUrl))
                        PlaybackOptions.Add(string.Format("{0} | {1} ({2})", quality.Key[1], type, quality.Key[0]), finalUrl + "&ext=." + type.Replace("webm", "mkv"));
                    else
                    {
                        string rtmpUrl = urlOptions.Get("conn");
                        string rtmpPlayPath = urlOptions.Get("stream");
                        if (!string.IsNullOrEmpty(rtmpUrl) && !string.IsNullOrEmpty(rtmpPlayPath))
                        {
                            PlaybackOptions.Add(string.Format("{0} | {1} ({2})", quality.Key[1], type, quality.Key[0]), 
								new MPUrlSourceFilter.RtmpUrl(rtmpUrl) { PlayPath = rtmpPlayPath, SwfUrl = swfUrl, SwfVerify = true}.ToString());
                        }
                    }
                }
            }
            else if (Items.Get("status")== "fail")
            {
                string reason = Items.Get("reason");
                if (!string.IsNullOrEmpty(reason) && (PlaybackOptions == null || PlaybackOptions.Count == 0)) throw new OnlineVideosException(reason);
            }

            return PlaybackOptions;
        }

        public override string getVideoUrls(string url)
        {
            // return highest quality by default
            var result = getPlaybackOptions(url);
            if (result != null && result.Count > 0) return result.Last().Value;
            else return String.Empty;
        }
    }
}

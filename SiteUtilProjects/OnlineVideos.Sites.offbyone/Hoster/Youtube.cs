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
                }
            }
            else if (!string.IsNullOrEmpty(Items.Get("fmt_stream_map")))
            {
                string swfUrl = Regex.Match(contents, "\"url\":\\s\"([^\"]+)\"").Groups[1].Value.Replace("\\/", "/");// "url": "http:\/\/s.ytimg.com\/yt\/swfbin\/watch_as3-vflOCLBVA.swf"

                string[] FmtMap = Items["fmt_stream_map"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                Array.Sort(FmtMap, new Comparison<string>(delegate(string a, string b)
                {
                    int a_i = int.Parse(a.Substring(0, a.IndexOf("|")));
                    int b_i = int.Parse(b.Substring(0, b.IndexOf("|")));
                    int index_a = Array.IndexOf(fmtOptionsQualitySorted, a_i);
                    int index_b = Array.IndexOf(fmtOptionsQualitySorted, b_i);
                    return index_b.CompareTo(index_a);
                }));
                PlaybackOptions = new Dictionary<string, string>();
                foreach (string fmtValue in FmtMap)
                {
                    int fmtValueInt = int.Parse(fmtValue.Substring(0, fmtValue.IndexOf("|")));
                    switch (fmtValueInt)
                    {
                        case 0:
                        case 5:
                        case 34:
                            PlaybackOptions.Add("320x240 | (" + fmtValueInt.ToString().PadLeft(2, ' ') + ") | .flv", ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                            string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&swfVfy={1}",
                                System.Web.HttpUtility.UrlEncode(fmtValue.Substring(fmtValue.IndexOf("rtmp"))),
                                System.Web.HttpUtility.UrlEncode(swfUrl)))); break;
                        case 13:
                        case 17:
                            PlaybackOptions.Add("176x144 | (" + fmtValueInt.ToString().PadLeft(2, ' ') + ") | .mp4", ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                            string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&swfVfy={1}",
                                System.Web.HttpUtility.UrlEncode(fmtValue.Substring(fmtValue.IndexOf("rtmp"))),
                                System.Web.HttpUtility.UrlEncode(swfUrl)))); break;
                        case 18:
                            PlaybackOptions.Add("480x360 | (" + fmtValueInt.ToString().PadLeft(2, ' ') + ") | .mp4", ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                            string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&swfVfy={1}",
                                System.Web.HttpUtility.UrlEncode(fmtValue.Substring(fmtValue.IndexOf("rtmp"))),
                                System.Web.HttpUtility.UrlEncode(swfUrl)))); break;
                        case 35:
                            PlaybackOptions.Add("640x480 | (" + fmtValueInt.ToString().PadLeft(2, ' ') + ") | .flv", ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                            string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&swfVfy={1}",
                                System.Web.HttpUtility.UrlEncode(fmtValue.Substring(fmtValue.IndexOf("rtmp"))),
                                System.Web.HttpUtility.UrlEncode(swfUrl)))); break;
                        case 22:
                            PlaybackOptions.Add("1280x720 | (" + fmtValueInt.ToString().PadLeft(2, ' ') + ") | .mp4", ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                            string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&swfVfy={1}",
                                System.Web.HttpUtility.UrlEncode(fmtValue.Substring(fmtValue.IndexOf("rtmp"))),
                                System.Web.HttpUtility.UrlEncode(swfUrl)))); break;
                        case 37:
                            PlaybackOptions.Add("1920x1080 | (" + fmtValueInt.ToString().PadLeft(2, ' ') + ") | .mp4", ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                            string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&swfVfy={1}",
                                System.Web.HttpUtility.UrlEncode(fmtValue.Substring(fmtValue.IndexOf("rtmp"))),
                                System.Web.HttpUtility.UrlEncode(swfUrl)))); break;
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

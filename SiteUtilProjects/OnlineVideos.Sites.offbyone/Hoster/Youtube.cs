using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using OnlineVideos.Helpers;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Hoster
{
    public class Youtube : HosterBase, ISubtitle
    {
        [Category("OnlineVideosUserConfiguration"), Description("Select subtitle language preferences (; separated and ISO 3166-2?), for example: en;de")]
        protected string subtitleLanguages = "";

        private string subtitleText = null;
        private const string YoutubePlayerKey = "<playerkey>";
        private const string YoutubePlayerUrl = "https://www.youtube.com/youtubei/v1/player?key=" + YoutubePlayerKey;

        public override string GetHosterUrl()
        {
            return "Youtube.com";
        }

        [Category("OnlineVideosUserConfiguration"), Description("Don't show the 3D formats that youtube offers on some clips."), LocalizableDisplayName("Hide 3D Formats")]
        protected bool hide3DFormats = true;
        [Category("OnlineVideosUserConfiguration"), Description("Don't show the 3gpp formats that youtube offers on some clips."), LocalizableDisplayName("Hide Mobile Formats")]
        protected bool hideMobileFormats = true;

        static readonly ushort[] fmtOptions3D = new ushort[] { 82, 83, 84, 85, 100, 101, 102 };
        static readonly ushort[] fmtOptionsMobile = new ushort[] { 13, 17 };
        static readonly ushort[] fmtOptionsQualitySorted = new ushort[] { 38, 85, 137, 46, 37, 102, 84, 136, 45, 22, 101, 135, 83, 134, 44, 35, 100, 82, 43, 18, 34, 133, 6, 5, 0, 17, 13 };

        public override Dictionary<string, string> GetPlaybackOptions(string url)
        {
            IWebProxy proxy = null;
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

            List<KeyValuePair<string[], string>> qualities = new List<KeyValuePair<string[], string>>();// KeyValuePair.Value is url 

            try
            {
                CookieContainer cc = new CookieContainer();
                cc.Add(new Cookie("CONSENT", "YES+cb.20210328-17-p0.en+FX+684", "", ".youtube.com"));

                NameValueCollection headers = new NameValueCollection
                {
                    { "X-Youtube-Client-Name", "3" },
                    { "X-Youtube-Client-Version", "16.20" },
                    { "Origin", "https://www.youtube.com" },
                    { "Content-Type", "application/json" },
                    { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/73.0.3683.81 Safari/537.36" },
                    { "Accept-Charset", "ISO-8859-1,utf-8;q=0.7,*;q=0.7" },
                    { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8" },
                    { "Accept-Encoding", "gzip, deflate" },
                    { "Accept-Language", "en-us,en;q=0.5" }
                };

                string postdata = String.Format(@"{{""context"": {{""client"": {{""clientName"": ""ANDROID"", ""clientVersion"": ""16.20"", ""hl"": ""en""}}}}, ""videoId"": ""{0}"", ""playbackContext"": {{""contentPlaybackContext"": {{""html5Preference"": ""HTML5_PREF_WANTS""}}}}, ""contentCheckOk"": true, ""racyCheckOk"": true}}", videoId);
                var apicontents = WebCache.Instance.GetWebData<JObject>(YoutubePlayerUrl, postData: postdata, headers: headers);
                parsePlayerStatus(apicontents["streamingData"], qualities);
                if (qualities.Count == 0)
                {
                    postdata = String.Format(@"{{""context"": {{""client"": {{""clientName"": ""ANDROID"", ""clientVersion"": ""16.20"", ""hl"": ""en"", ""clientScreen"": ""EMBED""}}, ""thirdParty"": {{""embedUrl"": ""https://google.com""}}}}, ""videoId"": ""{0}"", ""playbackContext"": {{""contentPlaybackContext"": {{""html5Preference"": ""HTML5_PREF_WANTS""}}}}, ""contentCheckOk"": true, ""racyCheckOk"": true}}", videoId);
                    apicontents = WebCache.Instance.GetWebData<JObject>(YoutubePlayerUrl, postData: postdata, headers: headers);
                    parsePlayerStatus(apicontents["streamingData"], qualities);
                }

                if (qualities.Count == 0)
                {
                    //this one can be slow
                    contents = WebCache.Instance.GetWebData(string.Format("https://www.youtube.com/watch?v={0}&bpctr=9999999999&has_verified=1", videoId), proxy: proxy, cookies: cc);
                    Match m = Regex.Match(contents, @"""(?i)(?:sts|signatureTimestamp)"":(?<sts>[0-9]{5})", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        postdata = String.Format(@"{{""context"": {{""client"": {{""clientName"": ""WEB"", ""clientVersion"": ""2.20210622.10.00"", ""hl"": ""en"", ""clientScreen"": ""EMBED""}}, ""thirdParty"": {{""embedUrl"": ""https://google.com""}}}}, ""videoId"": ""{0}"", ""playbackContext"": {{""contentPlaybackContext"": {{""html5Preference"": ""HTML5_PREF_WANTS"", ""signatureTimestamp"": {1}}}}}, ""contentCheckOk"": true, ""racyCheckOk"": true}}", videoId, m.Groups["sts"].Value);
                        apicontents = WebCache.Instance.GetWebData<JObject>(YoutubePlayerUrl, postData: postdata, headers: headers);
                        parsePlayerStatus(apicontents["streamingData"], qualities);
                    }
                }
                qualities.Sort(new Comparison<KeyValuePair<string[], string>>((a, b) =>
                               {
                                   return Array.IndexOf(fmtOptionsQualitySorted, ushort.Parse(b.Key[0])).CompareTo(Array.IndexOf(fmtOptionsQualitySorted, ushort.Parse(a.Key[0])));
                               }));

                PlaybackOptions = new Dictionary<string, string>();
                foreach (var quality in qualities)
                {
                    ushort fmt_quality = ushort.Parse(quality.Key[0]);

                    if (!fmtOptionsQualitySorted.Contains(fmt_quality)) continue;

                    if (hideMobileFormats && fmtOptionsMobile.Any(b => b == fmt_quality)) continue;
                    if (hide3DFormats && fmtOptions3D.Any(b => b == fmt_quality)) continue;

                    string finalUrl;
                    var urlOptions = HttpUtility.ParseQueryString(quality.Value);
                    string type = urlOptions.Get("type");
                    string stereo = urlOptions["stereo3d"] == "1" ? " 3D " : " ";
                    if (!string.IsNullOrEmpty(type))
                    {
                        type = Regex.Replace(type, @"; codecs=""[^""]*""", "");
                        type = type.Substring(type.LastIndexOfAny(new char[] { '/', '-' }) + 1);
                    }

                    if (Helpers.UriUtils.IsValidUri(quality.Value))
                    {
                        finalUrl = quality.Value;
                    }
                    else
                    {
                        finalUrl = null;
                    }

                    if (!string.IsNullOrEmpty(finalUrl))
                    {
                        PlaybackOptions.Add(string.Format("{0} | {1}{2}({3})", quality.Key[1], type, stereo, quality.Key[0]), finalUrl);
                    }
                };

                subtitleText = null;
                if (!String.IsNullOrEmpty(subtitleLanguages) && !string.IsNullOrEmpty(Items.Get("player_response")))
                {
                    try
                    {
                        var jdata = JToken.Parse(Items.Get("player_response"));
                        var captions = jdata["captions"]?["playerCaptionsTracklistRenderer"]?["captionTracks"] as JArray;

                        string subUrl = getSubUrl(captions, subtitleLanguages);
                        if (!String.IsNullOrEmpty(subUrl))
                        {
                            string data = WebCache.Instance.GetWebData(subUrl + "&fmt=vtt");
                            subtitleText = Helpers.SubtitleUtils.Webvtt2SRT(data);
                            if (subtitleText.StartsWith("Kind: captions\r\nLanguage: "))
                            {
                                subtitleText = subtitleText.Substring(30);
                            }
                        }

                        //if (jdata !=)
                    }
                    catch { };
                }
            }
            catch (Exception e)
            {
                Log.Error("Error getting url {0}", e.Message);
            }
            return PlaybackOptions;
        }
        public override string GetVideoUrl(string url)
        {
            // return highest quality by default
            var result = GetPlaybackOptions(url);
            if (result != null && result.Count > 0) return result.Last().Value;
            else return String.Empty;
        }

        public string SubtitleText
        {
            get
            {
                return subtitleText;
            }
        }


        private string getSubUrl(JArray captions, string languages)
        {
            if (captions != null)
            {
                string[] langs = languages.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string lang in langs)
                    foreach (JToken caption in captions)
                        if (lang == caption.Value<string>("languageCode"))
                            return caption.Value<string>("baseUrl");
            }
            return null;
        }

        private void parsePlayerStatus(JToken streamingData, List<KeyValuePair<string[], string>> qualities)
        {
            if (streamingData == null)
                return;
            var formats = streamingData["formats"] as JArray;
            if (formats == null)
            {
                string hlsUrl = streamingData.Value<String>("hlsManifestUrl");
                if (!String.IsNullOrEmpty(hlsUrl))
                {
                    var data = GetWebData(hlsUrl);
                    var res = HlsPlaylistParser.GetPlaybackOptions(data, hlsUrl, (x, y) => x.Bandwidth.CompareTo(y.Bandwidth), (x) => x.Width + "x" + x.Height);
                    foreach (var kv in res)
                    {
                        string[] qualityKey = { "0", kv.Key };
                        qualities.Add(new KeyValuePair<string[], string>(qualityKey, kv.Value));
                    }
                }
            }
            else
                foreach (var format in formats)
                {
                    string[] qualityKey = { format.Value<String>("itag"), format.Value<String>("width") + 'x' + format.Value<String>("height") };
                    var qualityValue = format.Value<String>("url");
                    qualities.Add(new KeyValuePair<string[], string>(qualityKey, qualityValue));
                }
        }
    }
}

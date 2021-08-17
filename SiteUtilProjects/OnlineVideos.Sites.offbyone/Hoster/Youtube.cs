﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using OnlineVideos.Helpers;
using OnlineVideos.Hoster;
using OnlineVideos.JavaScript;
using System.Text;
using Jurassic;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Hoster
{
    public class Youtube : HosterBase, ISubtitle
    {
        [Category("OnlineVideosUserConfiguration"), Description("Select subtitle language preferences (; separated and ISO 3166-2?), for example: en;de")]
        protected string subtitleLanguages = "";

        private string subtitleText = null;

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
        static Regex swfJsonArgs = new Regex(@"(?:var\s)?(?:swfArgs|'SWF_ARGS'|swf)\s*(?:=|\:)\s((""\s*(?<html>.*)"";)|
(?<json>\{.+\})|
(?:\<param\sname=\\""flashvars\\""\svalue=\\""(?<params>[^""]+)\\""\>)|
(flashvars=""(?<params>[^""]+)""))|
(ytInitialPlayerResponse\s*=\s*(?<json>{[^<]*});</script>)|
(yt\.?player\.?Config\s*=\s*\{.*?""args""\:\s*(?<json>\{(?>\{(?<c>)|[^{}]+|\}(?<-c>))*(?(c)(?!))\}))", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        //the json part uses balancing groups, as demonstrated here: http://stackoverflow.com/a/35271017
        static Regex unicodeFinder = new Regex(@"\\[uU]([0-9A-F]{4})", RegexOptions.Compiled);

        Dictionary<string, Jurassic.ScriptEngine> cachedJavascript = new Dictionary<string, Jurassic.ScriptEngine>();

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

            List<KeyValuePair<string[], string[]>> qualities = new List<KeyValuePair<string[], string[]>>();// KeyValuePair.Value is either {url} or {url,sig}

            try
            {
                contents = WebCache.Instance.GetWebData(string.Format("https://www.youtube.com/watch?v={0}&has_verified=1", videoId), proxy: proxy);
                Match m = swfJsonArgs.Match(contents);
                if (m.Success)
                {
                    if (m.Groups["params"].Success)
                    {
                        Items = System.Web.HttpUtility.ParseQueryString(System.Web.HttpUtility.HtmlDecode(m.Groups["params"].Value));
                    }
                    else if (m.Groups["json"].Success)
                    {
                        Items = new NameValueCollection();
                        foreach (var z in Newtonsoft.Json.Linq.JObject.Parse(m.Groups["json"].Value))
                        {
                            Items.Add(z.Key, z.Value.ToString());
                        }

                        if (string.IsNullOrEmpty(Items.Get("url_encoded_fmt_stream_map")))
                        {
                            if (!string.IsNullOrEmpty(Items.Get("player_response")))
                            {
                                qualities.Clear();
                                parsePlayerStatus(JToken.Parse(Items.Get("player_response"))["streamingData"], qualities);
                            }
                            else
                            if (!string.IsNullOrEmpty(Items.Get("streamingData")))
                            {
                                qualities.Clear();
                                parsePlayerStatus(JToken.Parse(Items.Get("streamingData")), qualities);
                            }
                        }
                    }
                    else if (m.Groups["html"].Success)
                    {
                        Items = new NameValueCollection();
                        string html = Regex.Match(m.Groups["html"].Value, @"flashvars=\\""(?<value>.+?)\\""").Groups["value"].Value;
                        html = unicodeFinder.Replace(html, match => ((char)Int32.Parse(match.Value.Substring(2), NumberStyles.HexNumber)).ToString());
                        Items = System.Web.HttpUtility.ParseQueryString(System.Web.HttpUtility.HtmlDecode(html));
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error getting url {0}", e.Message);
            }

            if (!string.IsNullOrEmpty(Items.Get("url_encoded_fmt_stream_map")) || qualities.Count > 0)
            {
                string swfUrl = Regex.Unescape(Regex.Match(contents, @"""url""\s*:\s*""(https?:\\/\\/.*?watch[^""]+\.swf)""").Groups[1].Value);

                if (qualities.Count == 0)
                {
                    string[] FmtUrlMap = Items["url_encoded_fmt_stream_map"].Split(',');
                    string[] FmtList = Items["fmt_list"].Split(',');
                    for (int i = 0; i < FmtList.Length; i++)
                        if (i < FmtUrlMap.Length)
                        {
                            string[] tmp = { FmtUrlMap[i] };
                            qualities.Add(new KeyValuePair<string[], string[]>(FmtList[i].Split('/'), tmp));
                        }
                }
                /*
                string[] AdaptiveFmtUrlMap = Items["adaptive_fmts"].Split(',');
                for (int i = 0; i < AdaptiveFmtUrlMap.Length; i++)
                {
                    var adaptive_options = HttpUtility.ParseQueryString(AdaptiveFmtUrlMap[i]);
                    var quality = new string[] { adaptive_options["itag"], adaptive_options["size"] };
                    qualities.Add(new KeyValuePair<string[], string>(quality, AdaptiveFmtUrlMap[i]));
                }
                */
                qualities.Sort(new Comparison<KeyValuePair<string[], string[]>>((a, b) =>
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
                    var urlOptions = HttpUtility.ParseQueryString(quality.Value[0]);
                    string type = urlOptions.Get("type");
                    string stereo = urlOptions["stereo3d"] == "1" ? " 3D " : " ";
                    if (!string.IsNullOrEmpty(type))
                    {
                        type = Regex.Replace(type, @"; codecs=""[^""]*""", "");
                        type = type.Substring(type.LastIndexOfAny(new char[] { '/', '-' }) + 1);
                    }
                    string signature = null;

                    if (Helpers.UriUtils.IsValidUri(quality.Value[0]) && quality.Value.Length == 1)
                    {
                        finalUrl = quality.Value[0];
                    }
                    else
                    {
                        signature = urlOptions.Get("sig");
                        if (string.IsNullOrEmpty(signature))
                        {
                            string playerUrl = "";
                            var jsPlayerMatch = Regex.Match(contents, "\"assets\":.+?\"js\":\\s*(\"[^\"]+\")");
                            if (!jsPlayerMatch.Success)
                                jsPlayerMatch = Regex.Match(contents, "\"jsUrl\":\\s*(\"[^\"]+\")");
                            if (jsPlayerMatch.Success)
                            {
                                playerUrl = Newtonsoft.Json.Linq.JToken.Parse(jsPlayerMatch.Groups[1].Value).ToString();
                                if (!Uri.IsWellFormedUriString(playerUrl, UriKind.Absolute))
                                {
                                    Uri uri = null;
                                    if (Uri.TryCreate(new Uri(@"https://www.youtube.com"), playerUrl, out uri))
                                        playerUrl = uri.ToString();
                                    else
                                        playerUrl = string.Empty;
                                }
                            }
                            signature = DecryptSignature(playerUrl, quality.Value.Length == 2 ? quality.Value[1] : urlOptions.Get("s"));
                        }
                        if (quality.Value.Length == 1)
                            finalUrl = urlOptions.Get("url");
                        else
                            finalUrl = quality.Value[0];
                    }

                    if (!string.IsNullOrEmpty(finalUrl))
                    {
                        if (!finalUrl.Contains("ratebypass"))
                            finalUrl += "&ratebypass=yes";
                        PlaybackOptions.Add(string.Format("{0} | {1}{2}({3})", quality.Key[1], type, stereo, quality.Key[0]), finalUrl + (!string.IsNullOrEmpty(signature) ? ("&sig=" + signature) : ""));
                    }
                    else
                    {
                        string rtmpUrl = urlOptions.Get("conn");
                        string rtmpPlayPath = urlOptions.Get("stream");
                        if (!string.IsNullOrEmpty(rtmpUrl) && !string.IsNullOrEmpty(rtmpPlayPath))
                        {
                            PlaybackOptions.Add(string.Format("{0} | {1} ({2})", quality.Key[1], type, quality.Key[0]),
                                new MPUrlSourceFilter.RtmpUrl(rtmpUrl) { PlayPath = rtmpPlayPath, SwfUrl = swfUrl, SwfVerify = true }.ToString());
                        }
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
            else if (Items.Get("status") == "fail")
            {
                string reason = Items.Get("reason");
                if (!string.IsNullOrEmpty(reason) && (PlaybackOptions == null || PlaybackOptions.Count == 0)) throw new OnlineVideosException(reason);
            }
            else
            {
                Match m = Regex.Match(contents, @"""status"":""LOGIN_REQUIRED"",""reason"":""(?<reason>[^""]*)""");
                if (m.Success)
                    throw new OnlineVideosException(m.Groups["reason"].Value);
            }

            return PlaybackOptions;
        }

        private void parsePlayerStatus(JToken streamingData, List<KeyValuePair<string[], string[]>> qualities)
        {
            var formats = streamingData["formats"] as JArray;
            if (formats == null)
            {
                string hlsUrl = streamingData.Value<String>("hlsManifestUrl");
                if (!String.IsNullOrEmpty(hlsUrl))
                {
                    var data = GetWebData(hlsUrl);
                    var res = HlsPlaylistParser.GetPlaybackOptions(data, hlsUrl, HlsStreamInfoFormatter.VideoDimensionAndBitrate);
                    foreach (var kv in res)
                    {
                        string[] tmp = { kv.Value };
                        string[] qualityKey = { "0", kv.Key };
                        qualities.Add(new KeyValuePair<string[], string[]>(qualityKey, tmp));
                    }
                }
            }
            else
                foreach (var format in formats)
                {
                    string[] qualityKey = { format.Value<String>("itag"), format.Value<String>("width") + 'x' + format.Value<String>("height") };
                    var qualityValue = format.Value<String>("url");
                    if (String.IsNullOrEmpty(qualityValue))
                    {
                        string cipher = format.Value<String>("cipher");
                        if (String.IsNullOrEmpty(cipher))
                            cipher = format.Value<String>("signatureCipher");

                        NameValueCollection cipherItems = HttpUtility.ParseQueryString(HttpUtility.HtmlDecode(cipher));
                        qualityValue = cipherItems.Get("url");
                        string[] tmp = { qualityValue, cipherItems.Get("s") };
                        qualities.Add(new KeyValuePair<string[], string[]>(qualityKey, tmp));
                    }
                    else
                    {
                        string[] tmp = { qualityValue };
                        qualities.Add(new KeyValuePair<string[], string[]>(qualityKey, tmp));
                    }
                }
        }

        public override string GetVideoUrl(string url)
        {
            // return highest quality by default
            var result = GetPlaybackOptions(url);
            if (result != null && result.Count > 0) return result.Last().Value;
            else return String.Empty;
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

        /// <summary>/// Turn the encrypted s parameter into a valid signature</summary>
        /// <param name="s">s Parameter value of the URL parameters</param>
        /// <returns>Decrypted string for the s parameter</returns>
        string DecryptSignature(string javascriptUrl, string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;

            // try decryption by executing the Javascript from Youtube
            if (!string.IsNullOrEmpty(javascriptUrl))
            {
                if (javascriptUrl.StartsWith("//"))
                    javascriptUrl = "https:" + javascriptUrl;

                /*
                var match = Regex.Match(javascriptUrl, @".*?-(?<id>[a-zA-Z0-9_-]+)(?:/watch_as3|/html5player)?\.(?<ext>[a-z]+)$");
                if (!match.Success)
                    throw new Exception(string.Format("Cannot identify player {0}", javascriptUrl));
                var player_type = match.Groups["ext"];
                var player_id = match.Groups["id"];
                */

                string jsContent = WebCache.Instance.GetWebData(javascriptUrl);

                var decrypted = DecryptWithCustomParser(jsContent, s);

                if (!string.IsNullOrEmpty(decrypted))
                    return decrypted;
                else
                    Log.Info("Javascript decryption function returned nothing!");
            }
            else
            {
                Log.Info("No Javascript url for decrpyting signature!");
            }
            return string.Empty;
        }

        private string DecryptWithCustomParser(string jsContent, string s)
        {
            try
            {
                // Try to get the functions out of the java script
                JavaScriptParser javaScriptParser = new JavaScriptParser(jsContent);
                FunctionData functionData = javaScriptParser.Parse();

                StringBuilder stringBuilder = new StringBuilder();

                foreach (var functionBody in functionData.Bodies)
                {
                    stringBuilder.Append(functionBody);
                }

                ScriptEngine engine = new ScriptEngine();

                engine.Global["window"] = engine.Global;
                engine.Global["document"] = engine.Global;
                engine.Global["navigator"] = engine.Global;

                engine.Execute(stringBuilder.ToString());

                return engine.CallGlobalFunction(functionData.StartFunctionName, s).ToString();
            }
            catch (Exception ex)
            {
                Log.Info("Signature decryption with custom parser failed: {0}", ex.Message);
            }
            return string.Empty;
        }

        public string SubtitleText
        {
            get
            {
                return subtitleText;
            }
        }


        /*
        private string DecryptWithJurassicEngine(string jsContent, string s)
        {
            try
            {
                string signatureMethodName = Regex.Match(jsContent, @"\.sig\|\|([a-zA-Z0-9$]+)\(").Groups[1].Value;

                Jurassic.ScriptEngine engine;
                if (!cachedJavascript.TryGetValue(jsContent, out engine))
                {
                    engine = new Jurassic.ScriptEngine();

                    // define globals that are used in the script
                    engine.Global["window"] = engine.Global;
                    engine.Global["document"] = engine.Global;
                    engine.Global["navigator"] = engine.Global;

                    // this regexp is not valid for .net but js : https://developer.mozilla.org/de/docs/Web/JavaScript/Reference/Global_Objects/RegExp
                    var fixedJs = jsContent.Replace("[^]", ".");

                    // cut out the global function(){...} surrounding everything, so our method is defined global in Jurassic
                    fixedJs = fixedJs.Substring(fixedJs.IndexOf('{') + 1);
                    fixedJs = fixedJs.Substring(0, fixedJs.LastIndexOf('}'));

                    // due to js nature - all methods called in our target function must be defined before, so for performance cut off where our function ends
                    var method = Regex.Match(fixedJs, "(function\\s+" + signatureMethodName + @".*?)function [a-zA-Z]+\(\)").Groups[1];
                    fixedJs = fixedJs.Substring(0, method.Index + method.Length);

                    engine.Execute(fixedJs);
                    cachedJavascript.Add(jsContent, engine);
                }
                return engine.CallGlobalFunction(signatureMethodName, s).ToString();
            }
            catch (Exception ex)
            {
                Log.Info("Signature decryption by executing the Javascript failed: {0}", ex.Message);
            }
            return string.Empty;
        }
        */
    }
}

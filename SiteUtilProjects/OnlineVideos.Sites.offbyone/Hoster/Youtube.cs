﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using OnlineVideos.Hoster;
using OnlineVideos._3rdParty.Newtonsoft.Json.Linq;

namespace OnlineVideos.Hoster
{
    public class Youtube : HosterBase
    {
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
(yt\.?player\.?Config\s*=\s*\{.*?""args""\:\s*(?<json>\{[^\}]+\}))", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
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

			NameValueCollection ItemsAPI = new NameValueCollection();
            NameValueCollection Items = new NameValueCollection();
            string contents = "";
            try
            {
				try
				{
                    contents = WebCache.Instance.GetWebData(string.Format("http://youtube.com/get_video_info?video_id={0}&has_verified=1", videoId), proxy: proxy);
				}
				catch
				{
					if (contents == null) contents = "";
				}
                Items = System.Web.HttpUtility.ParseQueryString(contents);
                if (Items.Count == 0 || Items["status"] == "fail")
                {
					ItemsAPI = Items;
                    contents = WebCache.Instance.GetWebData(string.Format("http://www.youtube.com/watch?v={0}&has_verified=1", videoId), proxy: proxy);
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
                            foreach (var z in JObject.Parse(m.Groups["json"].Value))
                            {
                                Items.Add(z.Key, z.Value.ToString());
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
            }
            catch { }

            if (!string.IsNullOrEmpty(Items.Get("url_encoded_fmt_stream_map")))
            {
                string swfUrl = Regex.Unescape(Regex.Match(contents, @"""url""\s*:\s*""(https?:\\/\\/.*?watch[^""]+\.swf)""").Groups[1].Value);

				List<KeyValuePair<string[], string>> qualities = new List<KeyValuePair<string[], string>>();

                string[] FmtUrlMap = Items["url_encoded_fmt_stream_map"].Split(',');
				string[] FmtList = Items["fmt_list"].Split(',');
				for (int i = 0; i < FmtList.Length; i++)
				  if (i < FmtUrlMap.Length)
				  {
					qualities.Add(new KeyValuePair<string[], string>(FmtList[i].Split('/'), FmtUrlMap[i]));
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
                qualities.Sort(new Comparison<KeyValuePair<string[], string>>((a,b)=>
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

                    var urlOptions = HttpUtility.ParseQueryString(quality.Value);
                    string type = urlOptions.Get("type");
					string stereo = urlOptions["stereo3d"] == "1" ? " 3D " : " ";
                    if (!string.IsNullOrEmpty(type))
                    {
                        type = Regex.Replace(type, @"; codecs=""[^""]*""", "");
                        type = type.Substring(type.LastIndexOfAny(new char[] { '/', '-' }) + 1);
                    }
					string signature = urlOptions.Get("sig");
					if (string.IsNullOrEmpty(signature))
					{
						string playerUrl = "";
						var jsPlayerMatch = Regex.Match(contents, "\"assets\":.+?\"js\":\\s*(\"[^\"]+\")");
						if (jsPlayerMatch.Success)
							playerUrl = JToken.Parse(jsPlayerMatch.Groups[1].Value).ToString();
						signature = DecryptSignature(playerUrl, urlOptions.Get("s"));
					}
					string finalUrl = urlOptions.Get("url");
					if (!string.IsNullOrEmpty(finalUrl))
					{
						if (!finalUrl.Contains("ratebypass"))
							finalUrl += "&ratebypass=yes";
						PlaybackOptions.Add(string.Format("{0} | {1}{2}({3})", quality.Key[1], type, stereo, quality.Key[0]), finalUrl + (!string.IsNullOrEmpty(signature) ? ("&signature=" + signature) : "") + "&ext=." + type.Replace("webm", "mkv"));
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
                }
            }
            else if (Items.Get("status")== "fail")
            {
                string reason = Items.Get("reason");
                if (!string.IsNullOrEmpty(reason) && (PlaybackOptions == null || PlaybackOptions.Count == 0)) throw new OnlineVideosException(reason);
            }
			else if (ItemsAPI.Get("status") == "fail")
			{
				string reason = ItemsAPI.Get("reason");
				if (!string.IsNullOrEmpty(reason) && (PlaybackOptions == null || PlaybackOptions.Count == 0)) throw new OnlineVideosException(reason);
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

		/// <summary>
		/// Turn the encrypted s parameter into a valid signature
		/// </summary>
		/// <param name="s">s Parameter value of the URL parameters</param>
		/// <returns></returns>
		string DecryptSignature(string javascriptUrl, string s)
        {
			if (string.IsNullOrEmpty(s)) return string.Empty;

			// try decryption by executing the Javascript from Youtube
			if (!string.IsNullOrEmpty(javascriptUrl))
			{
				try
				{
					if (javascriptUrl.StartsWith("//"))
						javascriptUrl = "http:" + javascriptUrl;
                    /*
                    var match = Regex.Match(javascriptUrl, @".*?-(?<id>[a-zA-Z0-9_-]+)(?:/watch_as3|/html5player)?\.(?<ext>[a-z]+)$");
                    if (!match.Success)
                        throw new Exception(string.Format("Cannot identify player {0}", javascriptUrl));
                    var player_type = match.Groups["ext"];
                    var player_id = match.Groups["id"];
                    */
					string jsContent = WebCache.Instance.GetWebData(javascriptUrl);

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
					string decrypted = engine.CallGlobalFunction(signatureMethodName, s).ToString();
                    if (!string.IsNullOrEmpty(decrypted))
                        return decrypted;
                    else
                        Log.Info("Javascript decryption function returned nothing!");
				}
				catch (Exception ex)
				{
					Log.Info("Signature decryption by executing the Javascript failed: {0}", ex.Message);
				}
			}
            else
            {
                Log.Info("No Javascript url for decrpyting signature!");
            }
            return string.Empty;
		}
    }
}

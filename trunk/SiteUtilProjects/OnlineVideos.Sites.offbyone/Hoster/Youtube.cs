using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using OnlineVideos.Hoster.Base;
using System.Web;
using System.Globalization;
using System.ComponentModel;
using System.Net;

namespace OnlineVideos.Hoster
{
    public class Youtube : HosterBase
    {
        public override string getHosterUrl()
        {
            return "Youtube.com";
        }

		[Category("OnlineVideosUserConfiguration"), Description("Don't show the 3D formats that youtube offers on some clips."), LocalizableDisplayName("Hide 3D Formats")]
		protected bool hide3DFormats = true;
		[Category("OnlineVideosUserConfiguration"), Description("Don't show the 3gpp formats that youtube offers on some clips."), LocalizableDisplayName("Hide Mobile Formats")]
		protected bool hideMobileFormats = true;

		static readonly byte[] fmtOptions3D = new byte[] { 82, 83, 84, 85, 100, 101, 102 };
		static readonly byte[] fmtOptionsMobile = new byte[] { 13, 17 };
		static readonly byte[] fmtOptionsQualitySorted = new byte[] { 38, 85, 46, 37, 102, 84, 45, 22, 101, 83, 44, 35, 100, 82, 43, 18, 34, 6, 5, 0, 17, 13 };
        static Regex swfJsonArgs = new Regex(@"(?:var\s)?(?:swfArgs|'SWF_ARGS'|swf)\s*(?:=|\:)\s((""\s*(?<html>.*)"";)|
(?<json>\{.+\})|
(?:\<param\sname=\\""flashvars\\""\svalue=\\""(?<params>[^""]+)\\""\>)|
(flashvars=""(?<params>[^""]+)""))|
(yt\.?player\.?Config\s*=\s*\{.*?""args""\:\s*(?<json>\{[^\}]+\}))", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        static Regex unicodeFinder = new Regex(@"\\[uU]([0-9A-F]{4})", RegexOptions.Compiled);

		public override Dictionary<string, string> getPlaybackOptions(string url)
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
            try
            {
				try
				{
					contents = Sites.SiteUtilBase.GetWebData(string.Format("http://youtube.com/get_video_info?video_id={0}&has_verified=1", videoId), proxy: proxy);
				}
				catch
				{
					if (contents == null) contents = "";
				}
                Items = System.Web.HttpUtility.ParseQueryString(contents);
                if (Items.Count == 0 || Items["status"] == "fail")
                {
					contents = Sites.SiteUtilBase.GetWebData(string.Format("http://www.youtube.com/watch?v={0}&has_verified=1", videoId), proxy: proxy);
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
                                Items.Add(z.Key, z.Value.ToString());
                            }
                        }
                        else if (m.Groups["html"].Success)
                        {                            
                            Items.Clear();
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
					byte fmt_quality = byte.Parse(quality.Key[0]);

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
						signature = DecryptSignature(urlOptions.Get("s"));
					string finalUrl = urlOptions.Get("url");
                    if (!string.IsNullOrEmpty(finalUrl))
						PlaybackOptions.Add(string.Format("{0} | {1}{2}({3})", quality.Key[1], type, stereo, quality.Key[0]), finalUrl + "&signature=" + signature + "&ext=." + type.Replace("webm", "mkv"));
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

		/// <summary>
		/// Turn the encrypted s parameter into a valid signature
		/// </summary>
		/// <param name="s">s Parameter value of the URL parameters</param>
		/// <returns></returns>
		string DecryptSignature(string s)
        {
			if (string.IsNullOrEmpty(s)) return string.Empty;
			switch (s.Length)
			{
				case 92:
					return s[25] + s.Substring(3, 25 - 3) + s[0] + s.Substring(26, 42 - 26) + s[79] + s.Substring(43, 79 - 43) + s[91] + s.Substring(80, 83 - 80);
				case 90:
					return s[25] + s.Substring(3, 25 - 3) + s[2] + s.Substring(26, 40 - 26) + s[77] + s.Substring(41, 77 - 41) + s[89] + s.Substring(78, 81 - 78);
				case 88:
					return s[48] + new string(s.Substring(67 + 1, 81 - 67).Reverse().ToArray()) + s[82] + new string(s.Substring(62 + 1, 66 - 62).Reverse().ToArray()) + s[85] + new string(s.Substring(48 + 1, 61 - 48).Reverse().ToArray()) + s[67] + new string(s.Substring(12 + 1, 47 - 12).Reverse().ToArray()) + s[3] + new string(s.Substring(3 + 1, 11 - 3).Reverse().ToArray()) + s[2] + s[12];
				case 87:
					return s.Substring(4, 23 - 4) + s[86] + s.Substring(24, 85 - 24);
				case 86:
					return s.Substring(2, 63 - 2) + s[82] + s.Substring(64, 82 - 64) + s[63];
				case 85:
					return s.Substring(2, 8 - 2) + s[0] + s.Substring(9, 21 - 9) + s[65] + s.Substring(22, 65 - 22) + s[84] + s.Substring(66, 82 - 66) + s[21];
				case 84:
					return new string(s.Substring(36 + 1, 83 - 36).Reverse().ToArray()) + s[2] + new string(s.Substring(26 + 1, 35 - 26).Reverse().ToArray()) + s[3] + new string(s.Substring(3 + 1, 25 - 3).Reverse().ToArray()) + s[26];
				case 83:
					return s[6] + s.Substring(3, 6 - 3) + s[33] + s.Substring(7, 24 - 7) + s[0] + s.Substring(25, 33 - 25) + s[53] + s.Substring(34, 53 - 34) + s[24] + s.Substring(54);
				case 82:
					return s[36] + new string(s.Substring(67 + 1, 79 - 67).Reverse().ToArray()) + s[81] + new string(s.Substring(40 + 1, 66 - 40).Reverse().ToArray()) + s[33] + new string(s.Substring(36 + 1, 39 - 36).Reverse().ToArray()) + s[40] + s[35] + s[0] + s[67] + new string(s.Substring(0 + 1, 32).Reverse().ToArray()) + s[34];
				case 81:
					return s[56] + new string(s.Substring(56 + 1, 79 - 56).Reverse().ToArray()) + s[41] + new string(s.Substring(41 + 1, 55 - 41).Reverse().ToArray()) + s[80] + new string(s.Substring(34 + 1, 40 - 34).Reverse().ToArray()) + s[0] + new string(s.Substring(29 + 1, 33 - 29).Reverse().ToArray()) + s[34] + new string(s.Substring(9 + 1, 28 - 9).Reverse().ToArray()) + s[29] + new string(s.Substring(0 + 1, 8 - 0).Reverse().ToArray()) + s[9];
				case 79:
					return s[54] + new string(s.Substring(54 + 1, 77 - 54).Reverse().ToArray()) + s[39] + new string(s.Substring(39 + 1, 53 - 39).Reverse().ToArray()) + s[78] + new string(s.Substring(34 + 1, 38 - 34).Reverse().ToArray()) + s[0] + new string(s.Substring(29 + 1, 33 - 29).Reverse().ToArray()) + s[34] + new string(s.Substring(9 + 1, 28 - 9).Reverse().ToArray()) + s[29] + new string(s.Substring(0 + 1, 8 - 0).Reverse().ToArray()) + s[9];
				default:
					throw new OnlineVideosException(string.Format("Unable to decrypt signature, key length {0} not supported; retrying might work", s.Length));
			}
		}
    }
}

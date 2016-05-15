using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class VkPass : HosterBase, ISubtitle, IReferer
    {
        private string subtitleText = null;
        private string refererUrl = null;

        public override string GetHosterUrl()
        {
            return "vkpass.com";
        }

        public override Dictionary<string, string> GetPlaybackOptions(string url)
        {
            subtitleText = null;
            string refUrl = RefererUrl;
            //Clear referer
            RefererUrl = null;

            Dictionary<string, string> playbackOptions = new Dictionary<string, string>();
            try
            {
                string data = GetWebData(url, referer: refUrl);
                Regex rgx = new Regex(@"<script>(?<js>eval.*?\|vkpass\|.*?)</script>");
                string js = null;
                Match m = rgx.Match(data);
                if (m.Success)
                {
                    js = m.Groups["js"].Value;
                }
                if (!string.IsNullOrEmpty(js))
                {
                    js = "var document={s:'',write:function(s){this.s=s;return;},read:function(){return this.s;}};function html(){return document.read();}; " + js;
                    var engine = new Jurassic.ScriptEngine();
                    engine.Execute(js);
                    data = engine.CallGlobalFunction("html").ToString();
                    rgx = new Regex(@"<source.*?src=""(?<url>[^""]*).*?label=""(?<label>[^""]*)");
                    foreach (Match match in rgx.Matches(data))
                    {
                        MPUrlSourceFilter.HttpUrl hurl = new MPUrlSourceFilter.HttpUrl(match.Groups["url"].Value);
                        hurl.Referer = "http://vkpass.com";
                        playbackOptions.Add(match.Groups["label"].Value, hurl.ToString());
                    }

                }
                string subUrl = "";

                rgx = new Regex(@"<track.*?src=""(?<url>[^""]*).*?label=""(?<label>[^""]*)");
                foreach (Match match in rgx.Matches(data))
                {
                    string label = match.Groups["label"].Value;
                    if (label.ToLower() == "swedish" || label.ToLower() == "svenska")
                    {
                        subUrl = match.Groups["url"].Value;
                        break;
                    }
                    else if (label.ToLower() == "english" || label.ToLower() == "engelska")
                    {
                        subUrl = match.Groups["url"].Value;
                    }
                    else if (string.IsNullOrEmpty(subUrl))
                    {
                        subUrl = match.Groups["url"].Value;
                    }
                }
                if (!string.IsNullOrWhiteSpace(subUrl))
                {
                    try
                    {
                        subtitleText = WebCache.Instance.GetWebData(subUrl, forceUTF8: true);
                        int index = subtitleText.IndexOf("WEBVTT\r\n\r\n");
                        if (index >= 0)
                            subtitleText = subtitleText.Substring(index).Replace("WEBVTT\r\n\r\n", "");
                        if (!subtitleText.StartsWith("1\r\n"))
                        {
                            string oldSub = subtitleText;
                            rgx = new Regex(@"(?<time>\d\d:\d\d:\d\d.\d\d\d -->)");
                            int i = 1;
                            foreach (Match match in rgx.Matches(oldSub))
                            {
                                string time = match.Groups["time"].Value;
                                subtitleText = subtitleText.Replace(time, "\r\n" + i.ToString() + "\r\n" + time);
                                i++;
                            }
                            subtitleText = subtitleText.TrimStart();
                        }
                    }
                    catch { }
                }
            }
            catch { }
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


        public string SubtitleText
        {
            get { return subtitleText; }
        }

        public string RefererUrl
        {
            get
            {
                return refererUrl;
            }
            set
            {
                refererUrl = value;
            }
        }
    }
}
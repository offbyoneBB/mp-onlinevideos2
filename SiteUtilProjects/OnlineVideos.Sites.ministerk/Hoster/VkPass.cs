using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class VkPass : HosterBase, ISubtitle
    {
        private string subtitleText = null;

        public override string GetHosterUrl()
        {
            return "vkpass.com";
        }

        public override Dictionary<string, string> GetPlaybackOptions(string url)
        {
            subtitleText = null;
            Dictionary<string, string> playbackOptions = new Dictionary<string, string>();
            string data = GetWebData(url);
            Regex rgx = new Regex(@"{file:""(?<url>[^""]*).*?label:""(?<label>[^""]*).*?type:\s*?""mp4""}");
            foreach(Match m in rgx.Matches(data))
            {
                playbackOptions.Add(m.Groups["label"].Value, m.Groups["url"].Value);
            }
            string subUrl = "";
            rgx = new Regex(@"file:\s*?'(?<url>[^']*).*?kind:\s*?'captions'.*?label:\s*?'(?<label>[^']*)");
            foreach(Match match in rgx.Matches(data))
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
                    data = WebCache.Instance.GetWebData(subUrl);
                    subtitleText = data.Replace("WEBVTT\r\n\r\n", "");
                }
                catch { }
            }

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
    }
}
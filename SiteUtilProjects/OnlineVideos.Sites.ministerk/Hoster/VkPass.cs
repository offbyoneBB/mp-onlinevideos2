﻿using System;
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
            string data = GetWebData(url, referer: refUrl);
            Regex rgx = new Regex(@"window.atob\(\\'(?<base64>.*)\\'\)");
            Match m = rgx.Match(data);
            if (m.Success)
            {
                string base64 = m.Groups["base64"].Value;
                byte[] bytes = Convert.FromBase64String(base64);
                data = Encoding.UTF8.GetString(bytes);
            }
            rgx = new Regex(@"video_link:\s*'.*?oid=(?<oid>\d+).*?[^o]id=(?<id>\d+).*?hash=(?<hash>[0-9a-f]*)");
            m = rgx.Match(data);
            if (m.Success)
            {
                string format = @"https://api.vk.com/method/video.getEmbed?oid={0}&video_id={1}&embed_hash={2}&callback=callbackFunc";
                string vkUrl = string.Format(format, m.Groups["oid"].Value, m.Groups["id"].Value, m.Groups["hash"].Value);
                return HosterFactory.GetHoster("vk").GetPlaybackOptions(vkUrl);
            }
            else
            {
                rgx = new Regex(@"<iframe.*src=""(?<url>[^""]*).*?</iframe");
                m = rgx.Match(data);
                if (m.Success)
                {
                    data = GetWebData(m.Groups["url"].Value, referer: url);
                }

                rgx = new Regex(@"{file:""(?<url>[^""]*).*?label:""(?<label>[^""]*).*?type:\s*?""mp4""");
                foreach (Match match in rgx.Matches(data))
                {
                    string vUrl = match.Groups["url"].Value;
                    playbackOptions.Add(match.Groups["label"].Value, vUrl);
                }
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
                    subtitleText = WebCache.Instance.GetWebData(subUrl, forceUTF8: true);
                    int index = subtitleText.IndexOf("WEBVTT\r\n\r\n");
                    if (index >= 0)
                        subtitleText = subtitleText.Substring(index).Replace("WEBVTT\r\n\r\n", "");
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
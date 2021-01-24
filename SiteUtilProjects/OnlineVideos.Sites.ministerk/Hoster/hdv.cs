using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class hdv : HosterBase, ISubtitle
    {
        [Category("OnlineVideosUserConfiguration"), Description("Select subtitle language preferences (; separated and full name), for example: english;danish")]
        protected string subtitleLanguages = "";

        private string subtitleText = null;

        public override string GetHosterUrl()
        {
            return "hdv.fun";
        }

        public override Dictionary<string, string> GetPlaybackOptions(string url)
        {
            var data = GetWebData(url);

            var m = Regex.Match(data, @"var\s*hdv_user=""(?<usr>[^""]*)""");
            var usr ="";
            if (m.Success)
                usr = m.Groups["usr"].Value;

            m = Regex.Match(data, @"{""dislike"":\s[^,]*,\s""fid"":\s[^,]*,\s""ggc"":\s{""0"":\s""[^""]*"",\s""1"":\s""[^""]*"",\s""2"":\s""[^""]*"",\s""3"":\s""[^""]*"",\s""4"":\s""[^""]*"",\s""5"":\s""[^""]*"",\s""6"":\s""[^""]*"",\s""7"":\s""[^""]*"",\s""8"":\s""[^""]*"",\s""9"":\s""[^""]*""},\s""like"":\s[^,]*,\s""lscore"":\s[^,]*,\s""name"":\s""(?<name>[^""]*)"",\s""quality"":\s""(?<quality>[^""]*)"",\s""res"":\s(?<res>[^,]*),\s""ws"":\s""[^""]*""}");
            var res = new Dictionary<string, string>();
            while (m.Success)
            {
                var nm = res.Count().ToString() + ' ' + m.Groups["quality"].Value + " (" + m.Groups["res"].Value + ") ";
                var encodedusr = btoa(rev(btoa(rev("sj6wx79142" + usr))));
                var streamUrl = @"https://hls.hdv.fun/m3u8/" + m.Groups["name"].Value + ".m3u8?u=" + encodedusr;
                res.Add(nm, streamUrl);
                m = m.NextMatch();
            }

            m = Regex.Match(data, @"""(?<lang>[^""]*)"":\s\[\[[^,]*,\s(?<id>[^,]*),");
            Dictionary<string, string> subs = new Dictionary<string, string>();
            while (m.Success)
            {
                var lang = m.Groups["lang"].Value;
                if (!subs.ContainsKey(lang))
                    subs.Add(lang, m.Groups["id"].Value);
                m = m.NextMatch();
            }
            if (subs.Count > 0)
            {
                string subUrl = getSubUrl(subs, subtitleLanguages);
                if (!String.IsNullOrEmpty(subUrl))
                {
                    string subData = WebCache.Instance.GetWebData(subUrl);
                    subtitleText = Helpers.SubtitleUtils.Webvtt2SRT(subData);
                }
            }

            return res;
        }

        public override string GetVideoUrl(string url)
        {
            var result = GetPlaybackOptions(url);
            if (result != null && result.Count > 0) return result.Last().Value;
            else return String.Empty;
        }

        private string getSubUrl(Dictionary<string, string> subs, string languages)
        {
            string[] langs = languages.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string lang in langs)
            {
                if (subs.ContainsKey(lang))
                    return "https://sub1.hdv.fun/vtt1/" + subs[lang] + ".vtt";
            }
            return null;
        }


        public string SubtitleText
        {
            get { return subtitleText; }
        }

        private string rev(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        private string btoa(string s)
        {
            byte[] dataBuffer = Encoding.ASCII.GetBytes(s);
            return Convert.ToBase64String(dataBuffer);
        }

    }
}

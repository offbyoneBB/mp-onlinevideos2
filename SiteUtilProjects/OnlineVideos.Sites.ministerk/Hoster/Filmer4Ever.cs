using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class Filmer4Ever : HosterBase, ISubtitle
    {
        string sub = "";

        public override string GetHosterUrl()
        {
            return "filmer4ever.com";
        }

        public override Dictionary<string, string> GetPlaybackOptions(string url)
        {
            string data = GetWebData(url, forceUTF8: true);
            sub = "";
            Dictionary<string, string> pbos = new Dictionary<string, string>();
            Regex r = new Regex(@"{""file"":""(?<u>[^""]*)"", ""label"":""(?<l>[^""]*)"",""type"":""mp4""}");
            string format = "{0} ({1})";
            foreach (Match m in r.Matches(data))
            {
                string l = m.Groups["l"].Value;
                string u = m.Groups["u"].Value;
                int c = 1;
                while (pbos.ContainsKey(string.Format(format, l, c)))
                    c++;
                pbos.Add(string.Format(format, l, c), u);
            }
            pbos = pbos.OrderBy(p =>
            {
                return p.Key;
            }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            try
            {
                r = new Regex(@"{""file"": ""(?<u>[^""]*)"", ""label"": ""[^""]*"", ""kind"": ""captions"",""default"": true}");

                Match subMatch = r.Match(data);
                if (subMatch.Success)
                {
                    string subUrl = subMatch.Groups["u"].Value;
                    if (subUrl.StartsWith("/"))
                        subUrl = "http://www.filmer4ever.com" + subUrl;
                    sub = GetWebData(subUrl, forceUTF8: true);
                    sub = sub.Substring(sub.IndexOf("1"));
                }
            }
            catch { }
            return pbos;
        }

        public override string GetVideoUrl(string url)
        {
            Dictionary<string, string> pbos = GetPlaybackOptions(url);
            if (pbos.Count == 0)
                return "";
            return pbos.First().Value;
        }

        public string SubtitleText
        {
            get 
            {
                return sub; 
            }
        }
    }
}

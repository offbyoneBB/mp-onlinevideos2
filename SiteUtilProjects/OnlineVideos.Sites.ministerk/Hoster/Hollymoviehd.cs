using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class Hollymoviehd : HosterBase, ISubtitle
    {
        private string subtitleText = null;

        public override string GetHosterUrl()
        {
            return "hollymoviehd.com";
        }

        public override Dictionary<string, string> GetPlaybackOptions(string url)
        {
            subtitleText = null;

            Dictionary<string, string> playbackOptions = new Dictionary<string, string>();
            try
            {
                string data = GetWebData(url);
                Regex rgx = new Regex(@"""file"":""(?<url>[^""]*)""[^\}]*""label"":""(?<res>\d+)p");
                foreach (Match match in rgx.Matches(data))
                {
                    if (match.Success && !playbackOptions.ContainsKey(match.Groups["res"].Value))
                    {
                        string redirectUrl = WebCache.Instance.GetRedirectedUrl(match.Groups["url"].Value.Replace(@"\/", @"/"));
                        playbackOptions.Add(match.Groups["res"].Value, redirectUrl);
                    }
                }

                string subUrl = "";
                if (playbackOptions.Count > 0)
                {
                    playbackOptions = playbackOptions.OrderByDescending((p) =>
                    {
                        return int.Parse(p.Key);
                    }).ToDictionary(p => p.Key, p => p.Value);
                    Regex regex = new Regex(@"'(?<url>[^""']*.vtt)'");
                    Match m = regex.Match(data);
                    if (m.Success)
                    {
                        subUrl = m.Groups["url"].Value;
                    }
                    if (!string.IsNullOrWhiteSpace(subUrl))
                    {
                        try
                        {
                            if (subUrl.StartsWith("//"))
                                subUrl = "https:" + subUrl;
                            subtitleText = "";
                            string tmpTxt = WebCache.Instance.GetWebData(subUrl).Replace("\r","");
                            //Only allow ASCII + Extended ASCII (many subtitles contain crap characters -> fail to load)
                            tmpTxt = Regex.Replace(tmpTxt, @"[^\u0000-\u00FF]", "");
                            rgx = new Regex(@"(?<text>\d\d:\d\d:\d\d\.\d\d\d\s*?-->\s*?\d\d:\d\d:\d\d\.\d\d\d\n.*?\n\n)", RegexOptions.Singleline);
                            int i = 0;
                            foreach (Match match in rgx.Matches(tmpTxt))
                            {
                                i++;
                                subtitleText += i + "\n" + match.Groups["text"].Value;
                            }
                        }
                        catch { }
                    }
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
    }
}

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Hoster
{
    public class Svt : HosterBase, ISubtitle
    {
        private string subtitleUrl = null;

        public override string GetHosterUrl()
        {
            return "svt.se";
        }

        public string SubtitleText
        {
            get
            {
                string subtitleText = "";
                if (!string.IsNullOrWhiteSpace(subtitleUrl))
                {
                    subtitleText = GetWebData(subtitleUrl);
                    Regex rgx;
                    //Remove WEBVTT stuff

                    //This is if we don't use all.vtt...
                    //rgx = new Regex(@"WEBVTT.*?00:00:00.000", RegexOptions.Singleline);
                    //subtitleText = rgx.Replace(subtitleText, new MatchEvaluator((Match m) =>
                    //{
                    //   return string.Empty;
                    //}));
                    rgx = new Regex(@"WEBVTT");
                    subtitleText = rgx.Replace(subtitleText, new MatchEvaluator((Match m) =>
                    {
                        return string.Empty;
                    }));
                    // For some reason the time codes in the subtitles from Öppet arkiv starts @ 10 hours. replacing first number in the
                    // hour position with 0. Hope and pray there will not be any shows with 10+ h playtime...
                    // Remove all trailing stuff, ie in 00:45:21.960 --> 00:45:25.400 A:end L:82%
                    rgx = new Regex(@"\d(\d:\d\d:\d\d\.\d\d\d)\s*-->\s*\d(\d:\d\d:\d\d\.\d\d\d).*$", RegexOptions.Multiline);
                    subtitleText = rgx.Replace(subtitleText, new MatchEvaluator((Match m) =>
                    {
                        return "0" + m.Groups[1].Value + " --> 0" + m.Groups[2].Value + "\r";
                    }));

                    rgx = new Regex(@"</{0,1}[^>]+>");
                    subtitleText = rgx.Replace(subtitleText, string.Empty);
                    rgx = new Regex(@"(?<time>\d\d:\d\d:\d\d\.\d\d\d\s*?-->\s*?\d\d:\d\d:\d\d\.\d\d\d)");
                    int i = 0;
                    foreach (Match m in rgx.Matches(subtitleText))
                    {
                        i++;
                        string time = m.Groups["time"].Value;
                        subtitleText = subtitleText.Replace(time, i + "\r\n" + time);
                    }
                    subtitleText = HttpUtility.HtmlDecode(subtitleText);
                }
                return subtitleText;
            }
        }

        public override Dictionary<string, string> GetPlaybackOptions(string url)
        {
            subtitleUrl = null;
            Dictionary<string, string> playbackOptions = new Dictionary<string, string>();
            JObject json = GetWebData<JObject>(url);
            JArray videoReferences = json["videoReferences"].Value<JArray>();
            JToken subtitleReferences = json["subtitleReferences"];
            JToken videoReference = videoReferences.FirstOrDefault(vr => vr["format"].Value<string>() == "hls");
            if (videoReference != null)
            {
                url = videoReference["url"].Value<string>();
                OnlineVideos.Sites.Utils.MyHlsPlaylistParser parser = new OnlineVideos.Sites.Utils.MyHlsPlaylistParser(GetWebData(url), url);
                foreach (OnlineVideos.Sites.Utils.MyHlsStreamInfo streamInfo in parser.StreamInfos)
                    playbackOptions.Add(streamInfo.Width + "x" + streamInfo.Height + " (" + streamInfo.Bandwidth / 1000 + " kbps)", streamInfo.Url);
            }
            else
            {
                videoReference = videoReferences.FirstOrDefault(vr => vr["format"].Value<string>() == "hds");
                if (videoReference != null)
                {
                    url = videoReference["url"].Value<string>();
                    url += url.Contains("?") ? "&" : "?";
                    url += "hdcore=3.7.0&g=" + OnlineVideos.Sites.Utils.HelperUtils.GetRandomChars(12);
                    playbackOptions.Add("HDS", url);
                }
            }
            if (playbackOptions.Count == 0)
                return playbackOptions;

            if (subtitleReferences != null && subtitleReferences.Type == JTokenType.Array)
            {
                JToken subtitleReference = subtitleReferences.FirstOrDefault(sr => sr["format"].Value<string>() == "webvtt");
                if (subtitleReference != null)
                    subtitleUrl = subtitleReference["url"].Value<string>();
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
    }

    public class SVTPlay : Svt
    {
        public override string GetHosterUrl()
        {
            return "svtplay.se";
        }

        public override Dictionary<string, string> GetPlaybackOptions(string url)
        {
            string data = GetWebData(url);
            JArray episodeIds = JArray.Parse(data);
            return base.GetPlaybackOptions(string.Format("http://api.svt.se/videoplayer-api/video/{0}", episodeIds.Count > 0 ? episodeIds.First().ToString() : url.Replace("http://www.svtplay.se/api/episodeIds?ids=", "")));
        }
    }

    public class OppetArkiv : Svt
    {
        public override string GetHosterUrl()
        {
            return "oppetarkiv.se";
        }

        public override Dictionary<string, string> GetPlaybackOptions(string url)
        {
            string data = GetWebData(url);
            Regex r = new Regex(@"data-video-id=""(?<url>[^""]*)");
            Match m = r.Match(data);
            if (m.Success)
                return base.GetPlaybackOptions(string.Format("http://api.svt.se/videoplayer-api/video/{0}", m.Groups["url"].Value));
            else
                return new Dictionary<string, string>();
        }
    }

}

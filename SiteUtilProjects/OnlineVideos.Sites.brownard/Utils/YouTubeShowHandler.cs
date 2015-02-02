using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OnlineVideos.Hoster.Base;

namespace OnlineVideos.Sites.Brownard
{
    class EpisodeInfo
    {
        public string SeriesTitle { get; set; }
        public string SeriesNumber { get; set; }
        public string EpisodeNumber { get; set; }
        public string AirDate { get; set; }
    }

    class YouTubeShowHandler
    {
        public static Dictionary<string, string> GetYouTubePlaybackOptions(EpisodeInfo info)
        {
            if (!HosterFactory.ContainsName("youtube"))
            {
                Log.Warn("youtube hoster was not found");
                return null;
            }

            if (info == null ||
                string.IsNullOrEmpty(info.SeriesTitle) ||
                string.IsNullOrEmpty(info.SeriesNumber) ||
                (string.IsNullOrEmpty(info.EpisodeNumber) && string.IsNullOrEmpty(info.AirDate))
                )
            {
                Log.Warn("Not enough info to locate video");
                return null;
            }

            string youtubeTitle = Regex.Replace(info.SeriesTitle, "[^A-z0-9]", "").ToLower();
            Log.Debug("Searching for youtube video: Show: {0}, Season: {1}, {2}", youtubeTitle, info.SeriesNumber, string.IsNullOrEmpty(info.AirDate) ? "Episode: " + info.EpisodeNumber : "Air Date: " + info.AirDate);

            string html = WebCache.Instance.GetWebData("http://www.youtube.com/show/" + youtubeTitle);

            //look for season
            string seriesReg = string.Format(@"<a class=""yt-uix-tile-link"" href=""([^""]*)"">[\s\n]*Season {0} Episodes", info.SeriesNumber);
            Match m = Regex.Match(html, seriesReg);
            if (m.Success)
            {
                //found specified season
                string playlist = WebCache.Instance.GetWebData("http://www.youtube.com" + m.Groups[1].Value);
                string episodeReg;

                //look for specified episode
                if (string.IsNullOrEmpty(info.EpisodeNumber))
                {
                    episodeReg = string.Format(@"<a href=""(/watch[^""]*)"".*?>[\s\n]*<span.*?>.*?{0}", info.AirDate);
                    m = Regex.Match(playlist, episodeReg);
                    if (m.Success)
                        return HosterFactory.GetHoster("youtube").GetPlaybackOptions("http://youtube.com" + m.Groups[1].Value);
                    return null;
                }

                episodeReg = string.Format(@"<span class=""video-index"">{0}</span>.*?<a href=""(/watch[^""]*)""", info.EpisodeNumber);
                m = Regex.Match(playlist, episodeReg, RegexOptions.Singleline);
                if (m.Success)
                {
                    if (verifyYoutubePage(m.Groups[1].Value, info.SeriesNumber, info.EpisodeNumber))
                        return HosterFactory.GetHoster("youtube").GetPlaybackOptions("http://youtube.com" + m.Groups[1].Value);
                    return null;
                }
                //didn't find specified episode directly, loop through all videos and see if we get a match
                foreach (Match ep in Regex.Matches(playlist, @"<a href=""(/watch[^""]*)"""))
                {
                    if (verifyYoutubePage(ep.Groups[1].Value, info.SeriesNumber, info.EpisodeNumber))
                        return HosterFactory.GetHoster("youtube").GetPlaybackOptions("http://youtube.com" + ep.Groups[1].Value);
                }
            }
            Log.Warn("Unable to locate 4od video on youtube");
            return null;
        }

        static bool verifyYoutubePage(string url, string targetSeries, string targetEpisode)
        {
            Match m = Regex.Match(url, "v=([^&]*)");
            if (m.Success)
            {
                string youtubeId = m.Groups[1].Value;
                string page = WebCache.Instance.GetWebData(string.Format("http://www.youtube.com/watch?v={0}&has_verified=1&has_verified=1", youtubeId));
                if (!string.IsNullOrEmpty(page))
                {
                    m = Regex.Match(page, string.Format(@"Season {0} Ep\. ([0-9]+)", targetSeries));
                    if (m.Success)
                    {
                        return m.Groups[1].Value == targetEpisode;
                    }
                }
            }
            return false;
        }
    }
}

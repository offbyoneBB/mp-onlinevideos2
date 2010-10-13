using System;
using System.Collections.Generic;
using OnlineVideos.Hoster.Base;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{

    public class VideoInfoEx : VideoInfo
    {
        public override string GetPlaybackOptionUrl(string url)
        {
            string newUrl = base.GetPlaybackOptionUrl(url);
            Uri uri = new Uri(newUrl);
            string key = uri.Host.Replace("www.", "");
            key = key.Substring(0, key.IndexOf("."));
            try
            {
                HosterBase hosterUtil = HosterFactory.GetHoster(key);
                string ret = hosterUtil.getVideoUrls(newUrl);
                if (!string.IsNullOrEmpty(ret))
                    return ret;
            }
            catch
            {}
            return null;
        }
    }

    public class VideoHosterUtil : GenericSiteUtil
    {
        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            foreach (VideoInfo vid in base.getVideoList(category))
                videos.Add((VideoInfoEx)vid);

            return videos;
        }

        public override string getUrl(VideoInfo video)
        {
            if (video.PlaybackOptions == null)
                video.PlaybackOptions = getMirrorList(video.VideoUrl);

            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
            {
                var enumer = video.PlaybackOptions.GetEnumerator();
                enumer.MoveNext();
                return enumer.Current.Value;
            }
            return "";
        }

        public virtual Dictionary<string, string> getMirrorList(string url)
        {
            string webData = GetWebData(url);
            Dictionary<string, string> mirrors = parseHosterLinks(webData);
            return mirrors;
        }

        public virtual Dictionary<string, string> parseHosterLinks(string webData)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();

            foreach (HosterBase hosterUtil in HosterFactory.GetAllHosters())
            {
                string regEx = @"(""|')(?<url>[^(""|')]+" + hosterUtil.getHosterUrl().ToLower() + "/" + @"[^(""|')]+)(""|')";

                MatchCollection n = Regex.Matches(webData, regEx);
                List<string> results = new List<string>();
                foreach (Match m in n)
                {
                    if (!results.Contains(m.Groups["url"].Value))
                        results.Add(m.Groups["url"].Value);
                }

                foreach (string url in results)
                {
                    if (url.Length > (hosterUtil.getHosterUrl().ToLower().Length + 4))
                    {
                        string targetUrl = url;
                        if (targetUrl.Contains("\\/")) targetUrl = targetUrl.Replace("\\/", "/");

                        if (results.Count > 1)
                        {
                            int i = 1;

                            string playbackName = hosterUtil.getHosterUrl() + " - " + i + "/" + results.Count;
                            
                            while (ret.ContainsKey(playbackName))
                            {
                                i++;
                                playbackName = hosterUtil.getHosterUrl() + " - " + i + "/" + results.Count;
                            }
                            ret.Add(playbackName, targetUrl);
                        }
                        else
                        {
                            ret.Add(hosterUtil.getHosterUrl(), targetUrl);
                        }
                    }
                }
            }
            return ret;
        }

    }
}

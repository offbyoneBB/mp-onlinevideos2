using System;
using System.Collections.Generic;
using System.Reflection;
using OnlineVideos.Hoster.Base;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{

    public class VideoInfoEx : VideoInfo
    {
        public override string GetPlaybackOptionUrl(string url)
        {
            string newUrl = base.GetPlaybackOptionUrl(url);

            foreach (Type type in VideoHosterUtil.hoster.Values)
            {
                HosterBase hosterUtil = (HosterBase)Activator.CreateInstance(type);
                if (newUrl.ToLower().Contains(hosterUtil.getHosterUrl().ToLower()))
                {
                    string ret = hosterUtil.getVideoUrls(newUrl);
                    if (!string.IsNullOrEmpty(ret))
                        return ret;
                }
            }

            return null;
        }
    }

    public class VideoHosterUtil : GenericSiteUtil
    {
        public static Dictionary<String, Type> hoster = new Dictionary<String, Type>();

        public override void Initialize(SiteSettings siteSettings)
        {

            //Load Hoster Classes
            //A hoster is a Childclass of HosterBase in the Namespace OnlineVideos.Hoster and
            //has to implement at least getVideoUrls() and getHosterUrl() Methods

            Assembly mainLibrary = Assembly.GetExecutingAssembly();
            Type[] typeArray = mainLibrary.GetExportedTypes();

            foreach (Type type in typeArray)
            {
                if (type.BaseType != null && type.IsSubclassOf(typeof(HosterBase)) && type.Namespace.Contains("OnlineVideos.Hoster"))
                {
                    if (!hoster.ContainsKey(type.Name))
                        hoster.Add(type.Name, type);
                }
            }

            base.Initialize(siteSettings);
        }

        public override string getUrl(VideoInfo video)
        {
            if (video.PlaybackOptions == null)
            {
                video.PlaybackOptions = getMirrorList(video.VideoUrl);
            }
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

            foreach (Type type in hoster.Values)
            {
                HosterBase hosterUtil = (HosterBase)Activator.CreateInstance(type);
                string regEx = @"(""|')(?<url>[^(""|')]+" + hosterUtil.getHosterUrl().ToLower() + "/" + @"[^(""|')]+)(""|')";

                MatchCollection n = Regex.Matches(webData, regEx);
                foreach (Match m in n)
                {
                    if (m.Groups["url"].Value.Length > (hosterUtil.getHosterUrl().ToLower().Length + 4))
                    {
                        string targetUrl = m.Groups["url"].Value;
                        if (targetUrl.Contains("\\/")) targetUrl = targetUrl.Replace("\\/", "/");

                        if (n.Count > 1)
                        {
                            int i = 1;

                            string playbackName = hosterUtil.getHosterUrl() + " - " + i + "/" + n.Count;
                            while (ret.ContainsKey(playbackName))
                            {
                                i++;
                                playbackName = hosterUtil.getHosterUrl() + " - " + i + "/" + n.Count;
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

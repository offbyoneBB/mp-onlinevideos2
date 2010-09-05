using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Hoster;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class VideoHosterUtil : GenericSiteUtil
    {
        private Dictionary<String, Type> hoster = new Dictionary<String, Type>();

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
                video.PlaybackOptions = new Dictionary<string, string>();
                List<string> hosterLinks = getMirrorList(video.VideoUrl);

                foreach (string link in hosterLinks)
                {
                    foreach (Type type in hoster.Values)
                    {
                        HosterBase hosterUtil = (HosterBase)Activator.CreateInstance(type);
                        if (link.ToLower().Contains(hosterUtil.getHosterUrl().ToLower()))
                        {
                            string name = hosterUtil.getHosterUrl();
                            string url = hosterUtil.getVideoUrls(link);
                            if (!string.IsNullOrEmpty(url))
                            {
                                int i = 1;
                                string playbackName = name + " - " + i;
                                while (video.PlaybackOptions.ContainsKey(playbackName))
                                {
                                    i++;
                                    playbackName = name + " - " + i;
                                }
                                video.PlaybackOptions.Add(playbackName, url);
                            }
                        }
                    }
                }
            }
            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
            {
                var enumer = video.PlaybackOptions.GetEnumerator();
                enumer.MoveNext();
                return enumer.Current.Value;
            }
            return "";
        }

        public virtual List<string> getMirrorList(string url)
        {
            string webData = GetWebData(url);
            List<string> mirrors = parseHosterLinks(webData);
            return mirrors;
        }

        public virtual List<string> parseHosterLinks(string webData)
        {
            List<string> ret = new List<string>();

            foreach (Type type in hoster.Values)
            {
                HosterBase hosterUtil = (HosterBase)Activator.CreateInstance(type);
                string regEx = @"(""|')(?<url>[^(""|')]+" + hosterUtil.getHosterUrl().ToLower() + @"[^(""|')]+)(""|')";

                Match m = Regex.Match(webData, regEx);
                while (m.Success)
                {
                    if (m.Groups["url"].Value.Length > (hosterUtil.getHosterUrl().ToLower().Length + 4))
                    {
                        string targetUrl = m.Groups["url"].Value;
                        if (targetUrl.Contains("\\/")) targetUrl = targetUrl.Replace("\\/", "/");
                        if (!ret.Contains(targetUrl))
                            ret.Add(targetUrl);
                    }
                    m = m.NextMatch();
                }
            }
            return ret;
        }
    }
}

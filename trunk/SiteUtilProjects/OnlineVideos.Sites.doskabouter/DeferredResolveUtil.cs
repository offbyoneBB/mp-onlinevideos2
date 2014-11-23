using System;
using System.Collections.Generic;
using System.ComponentModel;
using OnlineVideos.Hoster.Base;
using OnlineVideos.Subtitles;

namespace OnlineVideos.Sites
{
    public class DeferredResolveUtil : GenericSiteUtil
    {
        /// <summary>
        /// Defines the number of urls that are shown for each hoster. 
        /// 
        /// For example, if the limit is 5, only the top 5 entries will be shown
        /// for each hoster (e.g. putlocker, megavideo,...).
        /// </summary>
        [Category("OnlineVideosUserConfiguration"), Description("Limit Number of Urls that are shown per hosters (0: show all).")]
        protected int limitUrlsPerHoster = 5;

        /// <summary>
        /// If false, all video urls that have no hoster utility yet will be hidden. If true, all urls will
        /// be shown (unknown hosters will get a "ns" suffix)
        /// </summary>
        [Category("OnlineVideosUserConfiguration"), Description("Show hosters for which no provider exists.")]
        protected bool showUnknownHosters = false;

        [Category("OnlineVideosUserConfiguration"), Description("Select subtitle source, for example: TvSubtitles")]
        protected string subtitleSource = "";
        [Category("OnlineVideosUserConfiguration"), Description("Select subtitle language preferences (; separated and ISO 639-2), for example: eng;ger")]
        protected string subtitleLanguages = "";

        [Category("OnlineVideosConfiguration"), Description("Enables getting the redirected url instead of the given url for hoster.")]
        protected bool getRedirectedFileUrlForHoster = false;

        private SubtitleHandler sh = null;
        protected string lastPlaybackOptionUrl = null;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            sh = new SubtitleHandler(subtitleSource, subtitleLanguages);
        }

        public override string getUrl(VideoInfo video)
        {
            sh.SetSubtitleText(video, GetTrackingInfo, true);
            string tmp = base.getUrl(video);
            return SortPlaybackOptions(video, baseUrl, tmp, limitUrlsPerHoster, showUnknownHosters);
        }

        public override VideoInfo CreateVideoInfo()
        {
            return new DeferredResolveVideoInfo() { parent = this };
        }

        /// <summary>
        /// Formats the name so that a playbackelement could be created from it
        /// </summary>
        /// <param name="name"></param>
        /// <returns>should return [percentage]%[hoster] or [hoster]
        /// f.e. "95%putlocker" or "putlocker"</returns>
        public virtual string FormatHosterName(string name)
        {
            return name;
        }

        /// <summary>
        /// Formats the url to the appropriate value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual string FormatHosterUrl(string name)
        {
            return name;
        }

        /// <summary>
        /// Sorts and filters all video links (hosters) for a given video
        /// </summary>
        /// <param name="video">The video that is handled</param>
        /// <param name="baseUrl">The base url of the video</param>
        /// <param name="tmp"></param>
        /// <param name="limit">How many playback options are at most shown per hoster (0=all)</param>
        /// <param name="showUnknown">Also show playback options where no hoster is available yet</param>
        /// <returns></returns>
        public string SortPlaybackOptions(VideoInfo video, string baseUrl, string tmp, int limit, bool showUnknown)
        {
            List<PlaybackElement> lst = new List<PlaybackElement>();
            if (video.PlaybackOptions == null) // just one
                lst.Add(new PlaybackElement("100%justone", FormatHosterUrl(tmp)));
            else
                foreach (string name in video.PlaybackOptions.Keys)
                {
                    PlaybackElement element = new PlaybackElement(FormatHosterName(name), FormatHosterUrl(video.PlaybackOptions[name]));
                    element.status = "ns";
                    if (element.server.Equals("videoclipuri") ||
                        HosterFactory.ContainsName(element.server.ToLower().Replace("google", "googlevideo")))
                        element.status = String.Empty;
                    lst.Add(element);
                }

            Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (PlaybackElement el in lst)
            {
                if (counts.ContainsKey(el.server))
                    counts[el.server]++;
                else
                    counts.Add(el.server, 1);
            }
            Dictionary<string, int> counts2 = new Dictionary<string, int>();
            foreach (string name in counts.Keys)
                if (counts[name] != 1)
                    counts2.Add(name, counts[name]);

            lst.Sort(PlaybackComparer);

            for (int i = lst.Count - 1; i >= 0; i--)
            {
                if (counts2.ContainsKey(lst[i].server))
                {
                    lst[i].dupcnt = counts2[lst[i].server];
                    counts2[lst[i].server]--;
                }
            }

            video.PlaybackOptions = new Dictionary<string, string>();
            bool lastPlaybackOptionUrlPresent = false;

            foreach (PlaybackElement el in lst)
            {
                if (!Uri.IsWellFormedUriString(el.url, System.UriKind.Absolute))
                    el.url = new Uri(new Uri(baseUrl), el.url).AbsoluteUri;

                if ((limit == 0 || el.dupcnt <= limit) && (showUnknown || el.status == null || !el.status.Equals("ns")))
                {
                    video.PlaybackOptions.Add(el.GetName(), el.url);
                    lastPlaybackOptionUrlPresent |= (el.url == lastPlaybackOptionUrl);
                }
            }

            if (lst.Count == 1)
            {
                video.VideoUrl = video.GetPlaybackOptionUrl(lst[0].GetName());
                video.PlaybackOptions = null;
                return video.VideoUrl;
            }

            if (lastPlaybackOptionUrlPresent)
                return lastPlaybackOptionUrl;

            if (lst.Count > 0)
                tmp = lst[0].url;

            return tmp;
        }

        public virtual string ResolveVideoUrl(string url)
        {
            if (getRedirectedFileUrlForHoster)
                url = GetRedirectedUrl(url);
            return GetVideoUrl(url);
        }

        private static int IntComparer(int i1, int i2)
        {
            if (i1 == i2) return 0;
            if (i1 > i2) return -1;
            return 1;
        }

        private static int PlaybackComparer(PlaybackElement e1, PlaybackElement e2)
        {
            HosterBase h1 = HosterFactory.GetHoster(e1.server);
            HosterBase h2 = HosterFactory.GetHoster(e2.server);

            //first stage is to compare priorities (the higher the user priority, the better)
            int res = (h1 != null && h2 != null) ? IntComparer(h1.UserPriority, h2.UserPriority) : 0;
            if (res != 0)
            {
                return res;
            }
            else
            {
                //no priorities found or equal priorites -> see if status is different
                res = String.Compare(e1.status, e2.status);

                if (res != 0)
                {
                    return res;
                }
                else
                {
                    //equal status -> compare percentage
                    res = IntComparer(e1.percentage, e2.percentage);
                    if (res != 0)
                    {
                        return res;
                    }
                    else
                    {
                        //everything else is same -> compare alphabetically
                        return String.Compare(e1.server, e2.server);
                    }
                }
            }
        }

        public class DeferredResolveVideoInfo : VideoInfo
        {
            public DeferredResolveUtil parent;

            public override string GetPlaybackOptionUrl(string url)
            {
                parent.lastPlaybackOptionUrl = PlaybackOptions[url];
                string result = parent.ResolveVideoUrl(PlaybackOptions[url]);
                parent.sh.WaitForSubtitleCompleted();
                return result;

            }
        }

        class PlaybackElement
        {
            public int percentage;
            public string server;
            public string url;
            public string status;
            public string extra;
            public int dupcnt;

            public PlaybackElement()
            {
            }

            public string GetName()
            {
                string res = server;
                if (dupcnt != 0)
                    res += " (" + dupcnt.ToString() + ')';
                if (!String.IsNullOrEmpty(extra))
                    res += ' ' + extra;
                if (percentage > 0)
                    res += ' ' + percentage.ToString() + '%';
                if (!String.IsNullOrEmpty(status))
                    res += " - " + status;
                return res;
            }

            public PlaybackElement(string aPlaybackName, string aUrl)
            {
                string ser = aPlaybackName;
                if (aPlaybackName.Contains("%"))
                {
                    string[] tmp = aPlaybackName.Split('%');
                    percentage = int.Parse(tmp[0]);
                    ser = tmp[1];
                }
                else
                {
                    percentage = -1;
                }
                server = ser.TrimEnd(new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }).Trim();
                int i = server.IndexOf(" ");
                if (i >= 0)
                {
                    extra = server.Substring(i + 1);
                    server = server.Substring(0, i);
                }
                i = server.LastIndexOf(".");
                if (i >= 0)
                    server = server.Substring(0, i);
                url = aUrl;
            }

        }
    }
}

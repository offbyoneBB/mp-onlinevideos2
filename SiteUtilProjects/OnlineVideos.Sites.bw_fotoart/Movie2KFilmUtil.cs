using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;

using OnlineVideos.Hoster.Base;


namespace OnlineVideos.Sites.bw_fotoart
{
    public class Movie2KFilmUtil : GenericSiteUtil
    {
        public class Movie2KFilmVideoInfo : VideoInfo
        {
            public Movie2KFilmUtil Util { get; set; }

            public override string GetPlaybackOptionUrl(string option)
            {
                return getPlaybackUrl(PlaybackOptions[option], Util);
            }

            public static string getPlaybackUrl(string playerUrl, Movie2KFilmUtil Util)
            {
                string data = GetWebData(playerUrl, cookies: Util.GetCookie(), forceUTF8: Util.forceUTF8Encoding, allowUnsafeHeader: Util.allowUnsafeHeaders, encoding: Util.encodingOverride);
                Match m = Regex.Match(data, Util.hosterUrlRegEx);
                string url = m.Groups["url"].Value;
                Uri uri = new Uri(url);
                foreach (HosterBase hosterUtil in HosterFactory.GetAllHosters())
                    if (uri.Host.ToLower().Contains(hosterUtil.getHosterUrl().ToLower()))
                    {
                        Dictionary<string, string> options = hosterUtil.getPlaybackOptions(url);
                        if (options != null && options.Count > 0)
                        {
                            url = options.Last().Value;
                        }
                        break;
                    }
                return url;
            }
        }

      
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the url for a specific hoster")]
        protected string hosterUrlRegEx;

        [Category("OnlineVideosUserConfiguration"), Description("Define if you only want to see German videos listed.")]
        protected bool OnlyGerman = true;

        public override VideoInfo CreateVideoInfo()
        {
            return new Movie2KFilmVideoInfo() { Util = this };
        }

        //protected override CookieContainer GetCookie()
        //{
        //    if (OnlyGerman) return base.GetCookie();
        //    else return null;
        //}

        public override string GetVideoUrl(VideoInfo video)
        {
            string result = base.GetVideoUrl(video);
            if (video.PlaybackOptions == null && !string.IsNullOrEmpty(result))
                result = Movie2KFilmVideoInfo.getPlaybackUrl(result, this);
            return result;
        }
    }
}
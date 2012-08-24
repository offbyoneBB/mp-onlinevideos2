using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace OnlineVideos.Sites
{
    public class HardwareInfoUtil : GenericSiteUtil
    {
        public override List<String> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            List<string> newUrls = new List<String>();
            string s = base.getUrl(video);
            string newUrl;
            if (s.StartsWith(@"http://www.youtube.com/p/"))
            {
                int p = video.VideoUrl.IndexOf(@"/p/");
                //http://www.youtube.com/p/0541E51E6DE963A0&amp;hl=en_US&amp;fs=1&amp;hd=1
                string playListId = GetSubString(s, @"/p/", "&");
                if (!String.IsNullOrEmpty(playListId))
                {
                    //http://gdata.youtube.com/feeds/api/playlists/0541E51E6DE963A0?&v=2&max-results=50
                    string webData = SiteUtilBase.GetWebData(@"http://gdata.youtube.com/feeds/api/playlists/" + playListId);
                    if (!String.IsNullOrEmpty(webData))
                    {
                        string urlRegex = @"player\surl='(?<url>[^']*)'";
                        Regex regex_Url = new Regex(urlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
                        Match m = regex_Url.Match(webData);
                        while (m.Success)
                        {
                            string thisUrl = m.Groups["url"].Value;
                            if (!String.IsNullOrEmpty(thisUrl))
                            {
                                video.PlaybackOptions = null;
                                video.PlaybackOptions = Hoster.Base.HosterFactory.GetHoster("Youtube").getPlaybackOptions(thisUrl);
                                newUrl = null;
                                if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
                                {
                                    var enumer = video.PlaybackOptions.GetEnumerator();
                                    while (enumer.MoveNext())
                                        newUrl = enumer.Current.Value;
                                }
                                if (!String.IsNullOrEmpty(newUrl))
                                    newUrls.Add(newUrl);
                                video.PlaybackOptions = null;

                            }
                            m = m.NextMatch();
                        }
                    }
                }
            }
            else
            {
                video.PlaybackOptions = Hoster.Base.HosterFactory.GetHoster("Youtube").getPlaybackOptions(s);
                newUrls.Add(s);
            }

            return newUrls;
        }

        protected override List<VideoInfo> Parse(string url, string data)
        {
            nextPageRegExUrlFormatString = url.Split('?')[0] + "{0}";
            return base.Parse(url, data);
        }

        private static string GetSubString(string s, string start, string until)
        {
            int p = s.IndexOf(start);
            if (p == -1) return String.Empty;
            p += start.Length;
            int q = s.IndexOf(until, p);
            if (q == -1) return s.Substring(p);
            return s.Substring(p, q - p);
        }

    }
}

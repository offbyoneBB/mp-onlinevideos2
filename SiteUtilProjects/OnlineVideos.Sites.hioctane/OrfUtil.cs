using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Windows.Forms;
using System.Net;
using System.IO;

namespace OnlineVideos.Sites
{
    public class OrfUtil : GenericSiteUtil
    {
        private const String ORF_BASE = "http://tvthek.orf.at";

        public enum MediaType { flv, wmv };
        public enum MediaQuality { medium, high };

        string videolistRegex = @"</a></div>\s*<h3\sclass=""title"">\s*<span>(?<title>[^<]+)</span>";
        string videolistRegex2 = @"<li.*?><a\shref.*?>(?<day>[^<]+)</a>(?<items>.*?)</ul>";
        string videolistRegex3 = @"<li><a\shref=""(?<url>[^""]+)""\stitle=""(?<alt>[^""]+)"">(?<title>[^<]+)</a>";
        string videolistRegex4 = @"<li><a\shref=""(?<url>[^""]+)"">(?<title>[^<]+)</a>";
        string playlistRegex = @"<div\sid=""btn_playlist""(\sstyle=""[^""]+"")?>\s*<a\shref=""(?<url>[^""]+)""\sid=""open_playlist""";

        Regex regEx_Videolist;
        Regex regEx_Videolist2;
        Regex regEx_Videolist3;
        Regex regEx_Videolist4;
        Regex regEx_Playlist;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            regEx_Videolist = new Regex(videolistRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_Videolist2 = new Regex(videolistRegex2, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
            regEx_Videolist3 = new Regex(videolistRegex3, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_Videolist4 = new Regex(videolistRegex4, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_Playlist = new Regex(playlistRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

        }

        public override List<String> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            string data = GetWebData(video.VideoUrl);
            string videoUrl = "";
            if (!string.IsNullOrEmpty(data))
            {
                Match m = regEx_Playlist.Match(data);
                if (m.Success)
                {
                    videoUrl = m.Groups["url"].Value;
                    videoUrl = ORF_BASE + videoUrl;
                    return ParseASX(videoUrl);
                }
            }

            return null;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();

            string data = GetWebData((category as RssLink).Url);
            if (!string.IsNullOrEmpty(data))
            {
                Match m = regEx_Videolist.Match(data);
                if (m.Success)
                {
                    VideoInfo video = new VideoInfo();
                    video.Title = m.Groups["title"].Value;
                    video.Title = video.Title.Replace("&amp;", "&");
                    video.VideoUrl = (category as RssLink).Url;

                    videos.Add(video);
                }

                string cutOut = "";
                int idx1 = data.IndexOf("<span>Weitere Folgen</span>");
                if (idx1 > -1)
                {
                    int idx2 = data.IndexOf("</div>", idx1);
                    cutOut = data.Substring(idx1, idx2 - idx1);
                    if (!string.IsNullOrEmpty(cutOut))
                    {
                        Match c = regEx_Videolist2.Match(cutOut);
                        while (c.Success)
                        {
                            String day = c.Groups["day"].Value;
                            String items = c.Groups["items"].Value;

                            Match n = regEx_Videolist3.Match(items);
                            while (n.Success)
                            {
                                VideoInfo video = new VideoInfo();
                                video.Title = n.Groups["title"].Value;
                                video.VideoUrl = n.Groups["url"].Value;
                                video.VideoUrl = ORF_BASE + video.VideoUrl;
                                videos.Add(video);

                                n = n.NextMatch();
                            }
                            string prefix = "";
                            Match o = regEx_Videolist4.Match(items);
                            while (o.Success)
                            {
                                if (o.Groups["url"].Value.StartsWith("#"))
                                {
                                    prefix = o.Groups["title"].Value;
                                }
                                else
                                {
                                    VideoInfo video = new VideoInfo();
                                    video.Title = prefix + " " + HttpUtility.HtmlDecode(o.Groups["title"].Value) + ", " + day;
                                    video.VideoUrl = o.Groups["url"].Value;
                                    video.VideoUrl = ORF_BASE + video.VideoUrl;

                                    //only add episode from the same show (e.g. ZiB 9, ZiB 13, ...)
                                    if (CompareEpisodes(videos[0].Title, video.Title))
                                    {
                                        videos.Add(video);
                                    }
                                }

                                o = o.NextMatch();
                            }

                            c = c.NextMatch();
                        }
                    }
                }

            }
            return videos;
        }

        /// <summary>
        /// Compares 2 episodes titles and returns true if it's the same show
        /// </summary>
        /// <param name="_ep1">Episode 1</param>
        /// <param name="_ep2">Episode 2</param>
        /// <returns>true if it's the same show</returns>
        private bool CompareEpisodes(string _ep1, string _ep2)
        {
            try
            {
                if (_ep1 != null && _ep2 != null && !_ep1.Equals("") && !_ep2.Equals(""))
                {
                    String show1 = _ep1.Remove(_ep1.ToLower().IndexOfAny(new char[] { ',', '-', ':' })).Trim();
                    String show2 = _ep2.Remove(_ep2.ToLower().IndexOfAny(new char[] { ',', '-', ':' })).Trim();

                    int croppedPos = GetPositionOfCrop(show1, show2);
                    if (croppedPos != -99)
                    {
                        show1 = show1.Remove(croppedPos);
                        show2 = show2.Remove(croppedPos);
                    }
                    if (show1.Equals(show2)) return true;
                }
            }
            catch (Exception) { }
            return false;
        }

        /// <summary>
        /// Some shows will have their names cropped by ... if they are too long, so if
        /// that happens only compare the string until their cropped position (e.g. "Frisch gekocht")
        /// </summary>
        /// <param name="_string1">String 1</param>
        /// <param name="_string2">String 2</param>
        /// <returns>The position of the string that got cropped most</returns>
        private int GetPositionOfCrop(string _string1, string _string2)
        {
            int croppedPos = -99;
            if (_string1.EndsWith("...")) croppedPos = _string1.IndexOf("...");
            if (_string2.EndsWith("..."))
            {
                int cropped2 = _string2.IndexOf("...");
                if (croppedPos == -99 || cropped2 < croppedPos) croppedPos = cropped2;
            }

            return croppedPos;
        }
    }
}
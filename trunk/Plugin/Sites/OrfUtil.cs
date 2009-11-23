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
    public class OrfUtil : SiteUtilBase
    {
        public enum MediaType { flv, wmv };
        public enum MediaQuality { medium, high };

        string catRegex = @"<option\svalue=""(?<url>[^""]+)"">(?<title>[^<]+)</option>";
        string videolistRegex = @"title=""Diese\sSendung\sempfehlen.""></a></div>\s*<h3\sclass=""title"">\s*<span>(?<title>[^<]+)</span>";
        string videolistRegex2 = @"<li><a\shref=""(?<url>[^""]+)""\stitle=""(?<alt>[^""]+)"">(?<title>[^<]+)</a>";
        string videolistRegex3 = @"<li><a\shref=""(?<url>[^""]+)"">(?<title>[^<]+)</a>";
        string playlistRegex = @"<div\sid=""btn_playlist""\sstyle=""(?<style>[^""]+)"">\s*<a\shref=""(?<url>[^""]+)""\sid=""open_playlist""";

        string baseUrl = "http://tvthek.orf.at/";

        Regex regEx_Category;
        Regex regEx_Videolist;
        Regex regEx_Videolist2;
        Regex regEx_Videolist3;
        Regex regEx_Playlist;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_Category = new Regex(catRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_Videolist = new Regex(videolistRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_Videolist2 = new Regex(videolistRegex2, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_Videolist3 = new Regex(videolistRegex3, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_Playlist = new Regex(playlistRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            string data = GetWebData(baseUrl);

            if (!string.IsNullOrEmpty(data))
            {
                Match m = regEx_Category.Match(data);
                while (m.Success)
                {
                    
                    if (m.Groups["url"].Value.StartsWith("/"))
                    {
                        RssLink cat = new RssLink();
                        cat.Name = m.Groups["title"].Value;
                        cat.Name = cat.Name.Replace("&amp;", "&");
                        cat.Url = m.Groups["url"].Value;
                        cat.Url = "http://tvthek.orf.at" + cat.Url;

                        Settings.Categories.Add(cat);
                    }
                    m = m.NextMatch();
                }
                Settings.DynamicCategoriesDiscovered = true;
                return Settings.Categories.Count;
            }
            return 0;
        }

        public override bool MultipleFilePlay
        {
            get { return true; }
        }

        public override List<string> getMultipleVideoUrls(VideoInfo video)
        {
            string data = GetWebData(video.VideoUrl);
            string videoUrl = "";
            if (!string.IsNullOrEmpty(data))
            {
                Match m = regEx_Playlist.Match(data);
                if (m.Success)
                {
                    videoUrl = m.Groups["url"].Value;
                    videoUrl = "http://tvthek.orf.at" + videoUrl;
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

                string cutOut="";
                int idx1 = data.IndexOf("<span>Weitere Folgen</span>");
                if (idx1 > -1)
                {
                    int idx2 = data.IndexOf("</div>", idx1);
                    cutOut = data.Substring(idx1, idx2 - idx1);
                    if (!string.IsNullOrEmpty(cutOut))
                    {
                        Match n = regEx_Videolist2.Match(cutOut);
                        while (n.Success)
                        {
                            VideoInfo video = new VideoInfo();
                            video.Title = n.Groups["title"].Value;
                            video.VideoUrl = n.Groups["url"].Value;
                            video.VideoUrl = "http://tvthek.orf.at" + video.VideoUrl;
                            videos.Add(video);

                            n = n.NextMatch();
                        }
                        string prefix = "";
                        Match o = regEx_Videolist3.Match(cutOut);
                        while (o.Success)
                        {
                            if (o.Groups["url"].Value.StartsWith("#"))
                            {
                                prefix = o.Groups["title"].Value;
                            }
                            else
                            {
                                VideoInfo video = new VideoInfo();
                                video.Title = prefix + " " + HttpUtility.HtmlDecode(o.Groups["title"].Value);
                                video.VideoUrl = o.Groups["url"].Value;
                                video.VideoUrl = "http://tvthek.orf.at" + video.VideoUrl;
                                videos.Add(video);
                            }

                            o = o.NextMatch();
                        }
                    }
                }

            }
            return videos;
        }
    }
}
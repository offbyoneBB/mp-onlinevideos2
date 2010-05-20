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
    public class ArtePlus7Util : SiteUtilBase
    {
        public enum MediaType { flv, wmv };
        public enum MediaQuality { medium, high };
        MediaType preferredMediaType = MediaType.wmv;
        MediaQuality preferredMediaQuality = MediaQuality.high;

        string catUrlRegex = @"<div\sid=""nuage"">\s*
<noscript><div><a\shref=""(?<url>[^""]+)""[^>]*>(?<title>[^<]+)</a></div>\s*";


        string catRegex = @"<span\sclass=""niveau[^""]""\s><a\shref=""(?<url>[^""]+)""[^>]*>(?<title>[^<]+)</a></span>
";
        string videolistRegex = @"so.addVariable\(""xmlURL"",\s""(?<url>[^""]+)""\)";

        string playlistRegex = @"availableFormats\[[0-9]\]\[""format""\]\s=\s""(?<format>[^""]+)"";\s*availableFormats\[[0-9]\]\[""quality""\]\s=\s""(?<quality>[^""]+)"";\s*availableFormats\[[0-9]\]\[""url""\]\s=\s""(?<url>[^""]+)"";";

        string playlistItemRegex = @"<REF\sHREF=""(?<url>[^""]+)""/>";

        string subCategoryRegex = @"<h1><a\shref=""(?<url>[^""]+)""\sonfocus=""this.blur\(\)"">\s*<img\ssrc=""(?<img>[^""]+)""\salt=""(?<title>[^""]+)""";


        //string baseUrl = "http://plus7.arte.tv/de/1697480,filter=emissions.html";
        string baseUrl = "http://videos.arte.tv/de/videos";

        Regex regEx_CategoryUrl;
        Regex regEx_Category;
        Regex regEx_Videolist;
        Regex regEx_Playlist;
        Regex regEx_PlaylistItem;
        Regex regEx_SubcategoryItem;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_CategoryUrl = new Regex(catUrlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_Category = new Regex(catRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_Videolist = new Regex(videolistRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_Playlist = new Regex(playlistRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_PlaylistItem = new Regex(playlistItemRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_SubcategoryItem = new Regex(subCategoryRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            string data = GetWebData(baseUrl);

            if (!string.IsNullOrEmpty(data))
            {
                string languageField = Regex.Match(data, @"<div\sclass=""languageChoice"">\s*<ul>\s*(?<inner>[^.]+)</ul>\s*</div>").Groups["inner"].Value;

                Match m = Regex.Match(languageField, @"<li><a\shref=""(?<url>[^""]+)""(|[^>]+)>(?<title>[^<]+)</a></li>");
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.HasSubCategories = true;
                    cat.Name = m.Groups["title"].Value;
                    cat.Url = m.Groups["url"].Value;
                    cat.Url = "http://videos.arte.tv" + cat.Url;

                    Settings.Categories.Add(cat);

                    m = m.NextMatch();
                }
                Settings.DynamicCategoriesDiscovered = true;
                return Settings.Categories.Count;
            }
            return 0;
        }


        public override int DiscoverSubCategories(Category parentCategory)
        {
            string data = GetWebData((parentCategory as RssLink).Url);
            data = data.Substring(data.IndexOf(@"<div class=""program"">"));

            parentCategory.SubCategories = new List<Category>();

            if (!string.IsNullOrEmpty(data))
            {

                Match m = Regex.Match(data, @"<li>\s*<input\stype=""checkbox""\svalue=""[^""]+""/>\s*<label>\s*<a\shref=""(?<url>[^""]+)"">(?<title>[^<]+)</a></label>");
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.SubCategoriesDiscovered = true;
                    cat.HasSubCategories = false;

                    cat.Url = m.Groups["url"].Value;
                    cat.Url = "http://videos.arte.tv" + cat.Url;

                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);

                    parentCategory.SubCategories.Add(cat);
                    cat.ParentCategory = parentCategory;
                    m = m.NextMatch();
                }
                parentCategory.SubCategoriesDiscovered = true;
                return Settings.Categories.Count;
            }
            return 0;
        }

        public override String getUrl(VideoInfo video)
        {
            string data = GetWebData(video.VideoUrl);

            data = HttpUtility.UrlDecode(data);

            if (video.PlaybackOptions == null)
            {
                video.PlaybackOptions = new Dictionary<string, string>();

                if (!string.IsNullOrEmpty(data))
                {
                    string xmlUrl = Regex.Match(data, @"videorefFileUrl=(?<url>[^""]+)""/>").Groups["url"].Value;

                    if (!string.IsNullOrEmpty(xmlUrl))
                    {
                        List<string> langValues = new List<string>();
                        List<string> urlValues = new List<string>();

                        data = GetWebData(xmlUrl);

                        Match m = Regex.Match(data, @"<video\slang=""(?<lang>[^""]+)""\sref=""(?<url>[^""]+)""/>");
                        while (m.Success)
                        {
                            langValues.Add(m.Groups["lang"].Value);
                            urlValues.Add(m.Groups["url"].Value);
                            m = m.NextMatch();
                        }
                        for (int i = 0; i < langValues.Count; i++)
                        {
                            if (!string.IsNullOrEmpty(urlValues[i]))
                            {
                                string xmlFile = GetWebData(urlValues[i]);
                                if (!string.IsNullOrEmpty(xmlFile))
                                {
                                    Match n = Regex.Match(xmlFile, @"<url\squality=""(?<quality>[^""]+)"">(?<url>[^<]+)</url>");
                                    while (n.Success)
                                    {
                                        string title = langValues[i] + " - " + n.Groups["quality"].Value;

                                        string url = n.Groups["url"].Value;
                                        string host = url.Substring(7, url.IndexOf("/", 7)-7);
                                        string app = url.Substring(host.Length + url.IndexOf(host) + 1, (url.IndexOf("/", url.IndexOf("/", (host.Length + url.IndexOf(host) + 1)) + 1)) - (host.Length + url.IndexOf(host) + 1));
                                        string tcUrl = "rtmp://" + host + ":1935" + "/" + app;
                                        string playPath = url.Substring(url.IndexOf(app) + app.Length + 1);

                                        string resultUrl = string.Format("http://127.0.0.1:{0}/stream.flv?rtmpurl={1}&hostname={2}&tcUrl={3}&app={4}&swfurl={5}&swfsize={6}&swfhash={7}&pageurl={8}&playpath={9}",
                                        OnlineVideoSettings.RTMP_PROXY_PORT,
                                        url, //rtmpUrl
                                        host, //host
                                        tcUrl, //tcUrl
                                        app, //app
                                        "http://artestras.vo.llnwd.net/o35/geo/arte7/player/ALL/artep7_hd_16_9_v2.swf", //swfurl
                                        "105878",
                                        "061e498c18ca7ce1244caaa0311f35cddc6cf69b4ff810ab88caf7b546a6795e",
                                        video.VideoUrl, //pageUrl
                                        playPath //playpath
                                        );
                                        if(video.PlaybackOptions.ContainsKey(title)) title += " - 2";
                                        video.PlaybackOptions.Add(title, resultUrl);
                                        n = n.NextMatch();
                                    }
                                }
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

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();

            string data = GetWebData((category as RssLink).Url);

            if (!string.IsNullOrEmpty(data))
            {

                Match m = Regex.Match(data, @"<div\sclass=""video"">\s<div\sclass=""thumbnailContainer"">\s*<a\shref=""(?<url>[^""]+)""><img\salt=""[^""]*""\s*class=""thumbnail""\s*width=""[^""]*""\sheight=""[^""]*""\ssrc=""(?<thumb>[^""]+)""\s/>\s*</a>\s*<div\sclass=""videoHover"">\s*<p\sclass=""teaserText"">(?<description>[^<]+)</p>\s*</div>\s*</div>\s*<h2><a\shref=""[^""]+"">(?<title>[^<]+)</a></h2>\s*<p>(?<airdate>[^<]+)</p>");

                while(m.Success)
                {
                    VideoInfo video = new VideoInfo();

                    video.Title = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    video.ImageUrl = "http://videos.arte.tv" + m.Groups["thumb"].Value;
                    video.VideoUrl = "http://videos.arte.tv" + m.Groups["url"].Value;
                    video.Description = HttpUtility.HtmlDecode(m.Groups["description"].Value);
                    video.Length = HttpUtility.HtmlDecode(m.Groups["airdate"].Value);

                    videos.Add(video);

                    m = m.NextMatch();
                }
            }
            return videos;
        }
    }
}
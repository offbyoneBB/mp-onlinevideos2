using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public class ArtePlus7Util : SiteUtilBase
    {
        string baseUrl = "http://videos.arte.tv/de/videos";

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            string data = GetWebData(baseUrl);
            if (!string.IsNullOrEmpty(data))
            {
                string languageField = Regex.Match(data, @"Logout</a></li>\s*(?<inner>[^.]+)</ul>\s*</div>").Groups["inner"].Value;
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
                                        string host = url.Substring(url.IndexOf(":") + 3, url.IndexOf("/", url.IndexOf(":") + 3) - (url.IndexOf(":") + 3));
                                        string app = url.Substring(host.Length + url.IndexOf(host) + 1, (url.IndexOf("/", url.IndexOf("/", (host.Length + url.IndexOf(host) + 1)) + 1)) - (host.Length + url.IndexOf(host) + 1));
                                        string tcUrl = "rtmp://" + host + ":1935" + "/" + app;
                                        string playPath = url.Substring(url.IndexOf(app) + app.Length + 1);

                                        string resultUrl = ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                                            string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&hostname={1}&tcUrl={2}&app={3}&swfurl={4}&swfsize={5}&swfhash={6}&pageurl={7}&playpath={8}",
                                                url, //rtmpUrl
                                                host, //host
                                                tcUrl, //tcUrl
                                                app, //app
                                                "http://artestras.vo.llnwd.net/o35/geo/arte7/player/ALL/artep7_hd_16_9_v2.swf", //swfurl
                                                "105878",
                                                "061e498c18ca7ce1244caaa0311f35cddc6cf69b4ff810ab88caf7b546a6795e",
                                                video.VideoUrl, //pageUrl
                                                playPath //playpath
                                                ));

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
                Match m = Regex.Match(data, @"src=""(?<thumb>[^""]+)""\s*/></a>\s*<div\sclass=""videoHover"">\s*<p\sclass=""teaserText"">(?<description>[^<]+)</p>\s*</div>\s*</div>\s*<h2><a\shref=""(?<url>[^""]+)"">(?<title>[^<]+)</a></h2>\s*<p>(?<airdate>[^<]+)</p>");
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
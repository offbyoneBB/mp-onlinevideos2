using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Web;

namespace OnlineVideos.Sites
{
    public class CinemassacreUtil : GenericSiteUtil
    {
        private string[] videoUrlRegex = new String[3];
        private new Regex[] regEx_VideoUrl = new Regex[3];

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            videoUrlRegex[0] = @"<embed.*?src=""(?<url>[^""]+)""";
            videoUrlRegex[1] = @"<a\shref=""(?<url>[^""]+)""";
            videoUrlRegex[2] = @"param\sname=""src""\s*value=""(?<url>[^""]+)""";

            for (int i = 0; i < videoUrlRegex.Length; i++)
                regEx_VideoUrl[i] = new Regex(videoUrlRegex[i], RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

        }

        private void AddSubcats(RssLink parentCategory, XmlNode node)
        {
            parentCategory.SubCategories = new List<Category>();
            parentCategory.HasSubCategories = true;
            foreach (XmlNode sub in node.ChildNodes)
            {
                RssLink subcat = new RssLink();
                XmlNode a = sub.SelectSingleNode("a");
                subcat.Name = a.InnerText;
                subcat.Url = a.Attributes["href"].Value;
                subcat.ParentCategory = parentCategory;
                parentCategory.SubCategories.Add(subcat);

                XmlNode subsub = sub.SelectSingleNode("ul");
                subcat.HasSubCategories = subsub != null;
                if (subcat.HasSubCategories)
                    AddSubcats(subcat, subsub);

            }
            parentCategory.SubCategoriesDiscovered = true;
        }

        public override int DiscoverDynamicCategories()
        {
            string data = GetWebData(baseUrl);

            string[] subs = data.Split(new[] { @"class=""menu-item menu-item-type-custom menu-item-object-custom menu-item" }, StringSplitOptions.RemoveEmptyEntries);

            data = GetSubString(data, @"<div id=""navArea"" class=""menu-main-menu-container"">", @"</div");
            data = @"<?xml version=""1.0"" encoding=""iso-8859-1""?>" + data;

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(data);
            XmlNodeList cats = doc.SelectNodes(@"//ul[@id=""menu-main-menu""]/li");
            foreach (XmlNode node in cats)
            {
                RssLink cat = new RssLink();
                XmlNode a = node.SelectSingleNode("a");
                cat.Name = a.InnerText;
                XmlNode sub = node.SelectSingleNode("ul");
                cat.HasSubCategories = sub != null;
                if (cat.HasSubCategories)
                    AddSubcats(cat, sub);
                else
                    cat.Url = a.Attributes["href"].Value;
                Settings.Categories.Add(cat);
            }
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override string getUrl(VideoInfo video)
        {
            string data = GetWebData(video.VideoUrl);
            string data2 = GetSubString(data, @"<div id=""video-content""", @"<div\sid=""video-details""");
            if (String.IsNullOrEmpty(data2))
                data2 = data;
            Match matchFileUrl = regEx_FileUrl.Match(data2);
            string thisUrl = null;
            while (matchFileUrl.Success && thisUrl == null)
            {
                thisUrl = matchFileUrl.Groups["m0"].Value;
                if (thisUrl.Contains("facebook"))
                    thisUrl = null;
                matchFileUrl = matchFileUrl.NextMatch();
            }

            if (String.IsNullOrEmpty(thisUrl))
            {
                // for some springboards
                Match m = Regex.Match(data2, @"{""sbFeed"":{""partnerId"":(?<partnerid>[^,]*),""type"":""video"",""contentId"":(?<contentid>[^,]*),""cname"":""(?<cname>[^,]*)""}");
                if (m.Success)
                {
                    thisUrl = String.Format(@"http://{0}.springboardplatform.com/mediaplayer/springboard/video/ciin001/{1}/{2}/",
                        m.Groups["cname"].Value, m.Groups["partnerid"].Value, m.Groups["contentid"].Value);
                }
            }

            if (String.IsNullOrEmpty(thisUrl))
            {
                Match m = Regex.Match(data2, @"<script\stype='text/javascript'\ssrc='(?<url>http://player\.screenwavemedia\.com/play/embed[^']*)'></script>", defaultRegexOptions);
                if (m.Success)
                {
                    data2 = GetWebData(m.Groups["url"].Value);
                    video.PlaybackOptions = new Dictionary<string, string>();
                    m = Regex.Match(data2, @"'streamer':\s'(?<url>[^']*)',", defaultRegexOptions);
                    if (m.Success)
                    {
                        string streamer = m.Groups["url"].Value;
                        m = Regex.Match(data2, @"{\sbitrate:\s(?<bitrate>[^,]*),\sfile:\s""(?<file>[^""]*)"",\swidth:\s(?<width>[^\s]*)\s}", defaultRegexOptions);
                        while (m.Success)
                        {
                            MPUrlSourceFilter.RtmpUrl rtmpUrl = new MPUrlSourceFilter.RtmpUrl(streamer + '/' + m.Groups["file"].Value);
                            video.PlaybackOptions.Add("Bitrate:" + m.Groups["bitrate"].Value, rtmpUrl.ToString());
                            m = m.NextMatch();
                        }
                        return video.PlaybackOptions.Last().Value;
                    }
                }
            }
            if (String.IsNullOrEmpty(thisUrl)) return null;

            if (thisUrl.StartsWith("http://www.youtube.com"))
                return GetVideoUrl(thisUrl);

            if (thisUrl.StartsWith("http://blip.tv/play"))
                return GetVideoUrl(thisUrl);

            if (thisUrl.StartsWith("http://v.giantrealm.com"))
            {
                // next is shamelessly copied from hioctane
                Match m = Regex.Match(data, @"<param\sname=""FlashVars""\svalue=""vi=(?<vi>[^&]*)&amp;pid=(?<pid>[^&]*)&amp");
                thisUrl = String.Format(@"http://v.giantrealm.com/sax/{0}/{1}", m.Groups["pid"].Value, m.Groups["vi"].Value);
                data = GetWebData(thisUrl);
                if (!string.IsNullOrEmpty(data))
                {
                    m = Regex.Match(data, @"<file-hq>\s*(.+?)\s*</file-hq>");
                    if (m.Success) return m.Groups[1].Value;

                    m = Regex.Match(data, @"<file>\s*(.+?)\s*</file>");
                    if (m.Success) return m.Groups[1].Value;
                }
                return null;
            }

            if (thisUrl.StartsWith("http://www.gametrailers.com"))
            {
                string webData = GetWebData(thisUrl);
                Match m = Regex.Match(webData, @"<meta\sproperty=""og:video""\scontent=""http://media.mtvnservices.com/fb/(?<id>[^""]*)""");
                if (m.Success)
                {
                    webData = GetWebData(String.Format(@"http://www.gametrailers.com/feeds/mediagen?uri={0}",
                        HttpUtility.UrlEncode(m.Groups["id"].Value.Replace(".swf", ""))));
                    //http://www.gametrailers.com/feeds/mediagen/?uri=mgid%3Aarc%3Avideo%3Agametrailers.com%3Adb2b0e67-9546-421b-b57d-227c7f23c8ac

                    XmlDocument doc2 = new XmlDocument();
                    doc2.LoadXml(webData);
                    video.PlaybackOptions = new Dictionary<string, string>();
                    foreach (XmlNode rendition in doc2.SelectNodes("//rendition"))
                    {
                        video.PlaybackOptions.Add(rendition.Attributes["bitrate"].Value,
                            rendition.SelectSingleNode("src").InnerText);
                    }
                    string resultUrl;
                    if (video.PlaybackOptions.Count == 0) return String.Empty;
                    else
                    {
                        var enumer = video.PlaybackOptions.GetEnumerator();
                        enumer.MoveNext();
                        resultUrl = enumer.Current.Value;
                    }
                    if (video.PlaybackOptions.Count == 1) video.PlaybackOptions = null;

                    return resultUrl;
                }

                //old gametrailers:
                int i = thisUrl.LastIndexOf('/');
                data = thisUrl.Substring(i + 1);
                string url = String.Format(@"http://www.gametrailers.com/neo/?page=xml.mediaplayer.Mediagen&movieId={0}", data);
                string data3 = GetWebData(url);

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(data3);
                return doc.SelectSingleNode("//rendition/src").InnerText;
            }

            if (thisUrl.StartsWith("http://www.spike.com"))
            {
                int p = thisUrl.LastIndexOf('/');
                string id = thisUrl.Substring(p + 1);
                if (id == "efp")
                {
                    id = GetSubString(data, "flvbaseclip=", @"""");
                }
                string url = String.Format(@"http://www.spike.com/ui/xml/mediaplayer/mediagen.groovy?videoId={0}", id);
                string data3 = GetWebData(url);

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(data3);
                return url = doc.SelectSingleNode("//rendition/src").InnerText;
            }

            if (thisUrl.IndexOf("springboardplatform.com") >= 0)
            {
                string newUrl = GetRedirectedUrl(thisUrl);
                if (newUrl == thisUrl)
                {
                    Match m = Regex.Match(GetWebData(thisUrl), @"property=""og:video""\s*content=""(?<url>[^""]*)""");
                    if (m.Success)
                        return m.Groups["url"].Value;
                }
                string[] parts = newUrl.Split(new[] { "%22" }, StringSplitOptions.RemoveEmptyEntries);
                newUrl = parts[parts.Length - 2];
                XmlDocument doc = new XmlDocument();
                string xmlData = GetWebData(newUrl);
                doc.LoadXml(xmlData);
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("a", "http://search.yahoo.com/mrss/");
                XmlNode node = doc.SelectSingleNode("rss/channel/item/a:content", nsmgr);
                return node.Attributes["url"].Value;
            }
            if (thisUrl.IndexOf("bit.ly/") >= 0)
            {
                string newUrl = GetRedirectedUrl(thisUrl);
                string webData = GetWebData(newUrl);
                Match m = Regex.Match(webData, @"<meta\sproperty=""og:video""\scontent=""(?<url>[^""]*)""");
                if (m.Success)
                    return m.Groups["url"].Value;
            }
            return null;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string data = GetWebData(((RssLink)category).Url);
            int p = data.IndexOf(@"<!-- /content -->");
            if (p >= 0) data = data.Substring(0, p);
            return Parse(((RssLink)category).Url, data);
        }

        public override string GetFileNameForDownload(VideoInfo video, Category category, string url)
        {
            if (string.IsNullOrEmpty(url)) // called for adding to favorites
                return video.Title;
            else // called for downloading
            {
                string saveName = Utils.GetSaveFilename(video.Title);
                if (url.Contains("gametrailers.com"))
                    return saveName + ".mp4";
                return saveName + ".flv";
            }
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

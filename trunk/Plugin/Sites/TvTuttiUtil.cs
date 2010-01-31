using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Xml;
using System.IO;
using System.Net;

namespace OnlineVideos.Sites
{
    public class TvTuttiUtil : SiteUtilBase, IFilter
    {
        private int pageNr = 1;
        private bool hasPages = false;
        private Category bareCategory;
        private Dictionary<String, String> timeFrameList;
        private List<int> steps;
        private Dictionary<String, String> source;
        private Dictionary<String, String> orderBy;

        private string currentTimeFrame = null;
        private string currentSource = null;

        private const string defaultTimeFrame = "Vandaag en gisteren";
        private const string defaultSource = "Alles";

        public TvTuttiUtil()
        {
            timeFrameList = new Dictionary<string, string>();
            timeFrameList.Add(defaultTimeFrame, "24h");
            timeFrameList.Add("Gisteren", "yesterday");
            timeFrameList.Add("Afgelopen 7 dagen", "7d");
            timeFrameList.Add("Afgelopen 30 dagen", "30d");
            timeFrameList.Add("Alle", "all");
            steps = new List<int>();

            orderBy = new Dictionary<string, string>();

            source = new Dictionary<string, string>();
            source.Add(defaultSource, String.Empty);
            source.Add("Veronica", "15");
            source.Add("Net 5", "14");
            source.Add("RTL", "2");
            source.Add("SBS 6", "5");
            source.Add("Uitzending gemist", "1");

        }

        public override String getUrl(VideoInfo video)
        {
            string url = GetRedirectedUrl(video.VideoUrl);
            /*
             if (url.ToLower().StartsWith("http://asx") ||
                url.ToLower().StartsWith("http://cgi"))
            {
                url = ParseASX(url)[0];
            }
             */
            return url;
        }
        /// <summary>
        /// This will be called to find out if there is a next page for the videos that have just been returned 
        /// by a call to <see cref="getVideoList"/>. If returns true, the menu entry for "next page" will be enabled, otherwise disabled.<br/>
        /// Example: <see cref="MtvMusicVideosUtil"/><br/>
        /// default: always false
        /// </summary>
        public override bool HasNextPage
        {
            get { return hasPages; }
        }

        /// <summary>
        /// This function should return the videos of the next page. No state is given, 
        /// so the class implementation has to remember and set the current category and page itself.
        /// It will only be called if <see cref="HasNextPage"/> returned true on the last call 
        /// and after the user selected the menu entry for "next page".<br/>
        /// Example: <see cref="MtvMusicVideosUtil"/><br/>
        /// default: empty list
        /// </summary>
        /// <returns>a list of <see cref="VideoInfo"/> objects for the next page of the last queried category.</returns>
        public override List<VideoInfo> getNextPageVideos()
        {
            pageNr++;
            return getPagedVideoList(bareCategory);
        }

        /// <summary>
        /// This will be called to find out if there is a previous page for the videos that have just been returned 
        /// by a call to <see cref="getVideoList"/>. If returns true, the menu entry for "previous page" will be enabled, otherwise disabled.<br/>
        /// Example: <see cref="MtvMusicVideosUtil"/><br/>
        /// default: always false
        /// </summary>
        public override bool HasPreviousPage
        {
            get { return hasPages && pageNr > 1; }
        }

        /// <summary>
        /// This function should return the videos of the previous page. No state is given, 
        /// so the class implementation has to remember and set the current category and page itself.
        /// It will only be called if <see cref="HasPreviousPage"/> returned true on the last call 
        /// and after the user selected the menu entry for "previous page".<br/>
        /// Example: <see cref="MtvMusicVideosUtil"/><br/>
        /// default: empty list
        /// </summary>
        /// <returns>a list of <see cref="VideoInfo"/> objects for the previous page of the last queried category.</returns>
        public override List<VideoInfo> getPreviousPageVideos()
        {
            pageNr--;
            return getPagedVideoList(bareCategory);
        }

        public override List<VideoInfo> Search(string query, string category)
        {
            currentSource = category;
            return null;
            //return getPagedVideoList(bareCategory);
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            RssLink rssLink = (RssLink)category;
            bareCategory = rssLink;
            pageNr = 1;
            currentSource = source[defaultSource];
            currentTimeFrame = timeFrameList[defaultTimeFrame];
            return getPagedVideoList(category);
        }

        public static string GetPlayerOmroepUrl(string Url)
        {
            int aflID = Convert.ToInt32(Url.Split('=')[1]);
            CookieContainer cc = new CookieContainer();
            string step1 = GetWebData(Url, cc);
            CookieCollection ccol = cc.GetCookies(new Uri("http://tmp.player.omroep.nl/"));
            CookieContainer newcc = new CookieContainer();
            foreach (Cookie c in ccol) newcc.Add(c);

            step1 = GetWebData("http://player.omroep.nl/js/initialization.js.php?aflID=" + aflID.ToString(), newcc);
            if (!String.IsNullOrEmpty(step1))
            {
                int p = step1.IndexOf("securityCode = '");
                if (p != -1)
                {
                    step1 = step1.Remove(0, p + 16);
                    string sec = step1.Split('\'')[0];
                    string step2 = GetWebData("http://player.omroep.nl/xml/metaplayer.xml.php?aflID=" + aflID.ToString() + "&md5=" + sec, newcc);
                    if (!String.IsNullOrEmpty(step2))
                    {
                        XmlDocument tdoc = new XmlDocument();
                        tdoc.LoadXml(step2);
                        XmlNode final = tdoc.SelectSingleNode("/media_export_player/aflevering/streams/stream[@compressie_kwaliteit='bb' and @compressie_formaat='wmv']");
                        if (final != null)
                            return final.InnerText;

                    }
                }

            }
            return null;
        }

        private List<VideoInfo> getPagedVideoList(Category category)
        {
            string url = String.Format(((RssLink)category).Url, pageNr, currentTimeFrame, currentSource);
            string webData = GetWebData(url);
            List<VideoInfo> videos = new List<VideoInfo>();
            if (!string.IsNullOrEmpty(webData))
            {
                XmlDocument doc = new XmlDocument();
                webData = "<root>" + webData + "</root>";
                doc.LoadXml(webData);
                XmlNodeList episodes = doc.SelectNodes("/root/ul/li/div[@class=\"episodebox\"]");

                foreach (XmlNode node in episodes)
                {
                    try
                    {
                        VideoInfo video = new VideoInfo();
                        XmlNode infoNode = node.ParentNode.SelectSingleNode("h3/a[@class=\"programtitle\"]");
                        if (infoNode != null)
                            video.Title = infoNode.Attributes["title"].Value;
                        infoNode = node.ParentNode.SelectSingleNode("div[@class=\"episodeinfo\"]");
                        if (infoNode != null)
                        {

                            video.Description = infoNode.InnerText;
                            XmlNode tmp = infoNode.SelectSingleNode("span/a");
                            if (tmp != null)
                            {
                                if (video.Description != String.Empty)
                                    video.Description += " ";
                                video.Description += tmp.Attributes["title"].Value;
                            }
                        }
                        XmlNode imgNode = node.SelectSingleNode("div[@class=\"episodeimage\"]/a/img");
                        if (imgNode != null)
                            video.ImageUrl = "http://tvtutti.nl" + imgNode.Attributes["src"].Value;

                        imgNode = node.SelectSingleNode("div[@class=\"episodeimage\"]/a");
                        if (imgNode != null)
                        {
                            string rel = imgNode.Attributes["rel"].Value;
                            if (rel.StartsWith("http://player.omroep.nl/?aflID="))
                                video.VideoUrl = GetPlayerOmroepUrl(rel);
                            else
                            {
                                string data = GetWebData(rel);
                                if (!String.IsNullOrEmpty(data))
                                {
                                    video.VideoUrl = GetSubString(data, "http://av.rtl.nl/", "'}");
                                    if (String.IsNullOrEmpty(video.VideoUrl))
                                        video.VideoUrl = GetSubString(data, "http://asx.sbsnet.nl/", "\">");
                                    if (String.IsNullOrEmpty(video.VideoUrl))
                                        video.VideoUrl = rel;
                                }
                            }
                        }
                        //video.Description = video.VideoUrl;
                        if (video.VideoUrl != String.Empty) videos.Add(video);
                    }
                    catch
                    { }
                }

                XmlNode pageRefs = doc.SelectSingleNode("/root/div[@id=\"pagelinks\"]/ul");
                hasPages = pageRefs != null && pageRefs.FirstChild != null;
                if (hasPages)
                    pageNr = Convert.ToInt32(pageRefs.SelectSingleNode("li/span[@class=\"activepage\"]").InnerText);

            }
            return videos;

        }

        public override bool HasFilterCategories
        {
            get { return true; }
        }

        public override Dictionary<string, string> GetSearchableCategories()
        {
            return source;
        }

        public List<VideoInfo> filterVideoList(Category category, int maxResult, string orderBy, string timeFrame)
        {
            pageNr = 1;
            currentTimeFrame = timeFrame;
            return getPagedVideoList(category);
        }

        public List<VideoInfo> filterSearchResultList(string query, int maxResult, string orderBy, string timeFrame)
        {
            pageNr = 1;
            currentTimeFrame = timeFrame;
            return getPagedVideoList(bareCategory);
        }

        public List<VideoInfo> filterSearchResultList(string query, string category, int maxResult, string orderBy, string timeFrame)
        {
            throw new NotImplementedException();
        }

        public List<int> getResultSteps()
        {
            return steps;
        }

        public Dictionary<string, string> getOrderbyList()
        {
            return orderBy;
        }

        public Dictionary<string, string> getTimeFrameList()
        {
            return timeFrameList;
        }

        private string GetSubString(string s, string start, string until)
        {
            int p = s.IndexOf(start);
            if (p == -1) return String.Empty;
            int q = s.IndexOf(until, p);
            if (q == -1) return s.Substring(p);
            return s.Substring(p, q - p);
        }
    }
}

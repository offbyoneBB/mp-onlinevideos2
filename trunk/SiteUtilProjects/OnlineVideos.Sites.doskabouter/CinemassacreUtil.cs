using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.IO;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class CinemassacreUtil : SiteUtilBase
    {
        /*			 
          <ul id="pagine2">
		  <li class="page_item page-item-4180"><a href="http://www.cinemassacre.com/new/?page_id=4180" title="2010 Vids">2010 Vids</a></li>
		  </ul>
        */
        private string categoryRegex = @"<a\shref=""(?<url>[^""]+)""[^>]+>(?<title>[^<]+)<";
        /* 
        <td><a href="http://www.gametrailers.com/video/angry-video-screwattack/60452"><br />
        </a><a href="http://www.gametrailers.com/video/angry-video-screwattack/60452" target="_self"><img 
                 src="http://www.cinemassacre.com/new/wp-content/uploads/2010/01/SF2010sm.jpg" alt="" width="117" height="160" /></a><a href="http://www.cinemassacre.com/new/?p=3824"><br />
        </a><a href="http://www.gametrailers.com/video/angry-video-screwattack/60452" target="_self">Episode 85</a><br />
        January 6, 2010<br />
        Run time: 17:56</td>
         */

        /* list:  
        <p class="date">August 15, 2009</p>
        <p><a href="http://www.cinemassacre.com/new/?p=1785" target="_self">13. Too Much Cream Cheese</a></p>
        <p><a href="http://www.cinemassacre.com/new/?p=1456">12. Movie Titles</a></p>
        
        <p><a href="http://www.cinemassacre.com/new/?p=2993" target="_self"><strong>79 CastleVania Part 1 </strong></a></p>
        <p><a href="http://www.cinemassacre.com/new/?p=2831" target="_self"><strong><strong>78 Wayne&#8217;s World </strong></strong></a></p>

         */
        private string videoListRegex = @"<a\shref=""(?<url>[^""]+)[^>]+>(?:<strong>)*(<img.*?src=""(?<thumb>[^""]+)[^>]*>)?(?<title>[^<]*)<";

        private string[] videoUrlRegex = new String[3];

        private Regex regEx_Category;
        private Regex regEx_VideoList;
        private Regex[] regEx_VideoUrl = new Regex[3];

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            //param name="src" value="http://www.youtube.com/v/V4we8iFk-fY&amp;hl=en_US&amp;fs=1&amp;"
            videoUrlRegex[0] = @"<embed.*?src=""(?<url>[^""]+)""";
            /* 
             <a href="http://www.gametrailers.com/download/60452/t_screwattack_avgn_05k_streetf10_gt.mov">Quicktime (145.5MB)</a>
             <a href="http://www.gametrailers.com/download/60452/t_screwattack_avgn_05k_streetf10_gt.wmv">WMV  (150.5MB)</a>
             <a href="http://www.gametrailers.com/download/60452/t_screwattack_avgn_05k_streetf10_gt.mp4">MP4 for iPod (113.2MB)</a>
             */
            videoUrlRegex[1] = @"<a\shref=""(?<url>[^""]+)""";
            videoUrlRegex[2] = @"param\sname=""src""\s*value=""(?<url>[^""]+)""";

            regEx_Category = new Regex(categoryRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
            regEx_VideoList = new Regex(videoListRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
            for (int i = 0; i < videoUrlRegex.Length; i++)
                regEx_VideoUrl[i] = new Regex(videoUrlRegex[i], RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

        }

        public override int DiscoverDynamicCategories()
        {
            foreach (Category cat in Settings.Categories)
            {
                string url = ((RssLink)cat).Url;
                string data = GetWebData(url);
                if (!string.IsNullOrEmpty(data))
                    cat.HasSubCategories = data.IndexOf(@"id=""pagine2""") >= 0;
            }

            return base.DiscoverDynamicCategories();
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            string url = ((RssLink)parentCategory).Url;
            parentCategory.SubCategories = new List<Category>();

            string data = GetWebData(url);
            if (!string.IsNullOrEmpty(data))
            {
                data = GetSubString(data, @"id=""pagine2""", @"</ul>");

                Match m = regEx_Category.Match(data);
                while (m.Success)
                {
                    RssLink cat = new RssLink();
                    cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    cat.Url = m.Groups["url"].Value;
                    cat.SubCategoriesDiscovered = true;
                    cat.HasSubCategories = false;

                    parentCategory.SubCategories.Add(cat);
                    cat.ParentCategory = parentCategory;
                    m = m.NextMatch();
                }
                parentCategory.SubCategoriesDiscovered = true;
                return parentCategory.SubCategories.Count;
            }
            return 0;
        }

        public override string getUrl(VideoInfo video)
        {
            string data = GetWebData(video.VideoUrl);

            if (video.VideoUrl.StartsWith("http://screwattack.com"))
            {
                int i = data.IndexOf("gr.vau.videoURLHQ = '");
                if (i < 0) i = data.IndexOf("gr.vau.videoURL = '");
                i = data.IndexOf('\'', i);
                i++;
                int j = data.IndexOf('\'', i);
                return data.Substring(i, j - i);
            }

            string lsUrl = null;

            if (!string.IsNullOrEmpty(data))
            {
                data = HttpUtility.HtmlDecode(data);
                for (int i = 0; i < regEx_VideoUrl.Length; i++)
                {
                    string tdata;
                    if (i == 1)
                        tdata = GetSubString(data, @"class=""Downloads""", @"</span>");
                    else
                        tdata = data;
                    Match m = regEx_VideoUrl[i].Match(tdata);
                    while (m.Success)
                    {
                        string url = m.Groups["url"].Value;
                        if (!String.IsNullOrEmpty(url))
                        {
                            if (url.Contains("wmv")) lsUrl = url;
                            if (url.Contains("flv")) lsUrl = url;
                            if (i != 1) lsUrl = url;
                        }
                        m = m.NextMatch();
                    }
                }
            }

            if (video.VideoUrl.IndexOf("spike.com") >= 0)
            {
                int p = video.VideoUrl.LastIndexOf('/');
                string id = video.VideoUrl.Substring(p + 1);
                string url = String.Format(@"http://www.spike.com/ui/xml/mediaplayer/mediagen.groovy?videoId={0}", id);
                string data2 = GetWebData(url);

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(data2);
                return url = doc.SelectSingleNode("//rendition/src").InnerText;
            }

            if (!String.IsNullOrEmpty(lsUrl))
            {
                if (lsUrl.ToLower().Contains("youtube.com"))
                {
                    video.VideoUrl = lsUrl;
                    video.GetYouTubePlaybackOptions();
                    return "";
                }
                else
                    if (lsUrl.StartsWith("http://blip.tv/play"))
                    {
                        string s = HttpUtility.UrlDecode(GetRedirectedUrl(lsUrl));
                        int p = s.IndexOf("file=");
                        int q = s.IndexOf('&', p);
                        if (q < 0) q = s.Length;
                        s = s.Substring(p + 5, q - p - 5);
                        string rss = GetWebData(s);
                        p = rss.IndexOf(@"enclosure url="""); p += 15;
                        q = rss.IndexOf('"', p);
                        return rss.Substring(p, q - p);
                    }
                    else
                        if (lsUrl.StartsWith("http://www.gametrailers.com/download/"))
                        {
                            string name = lsUrl.Split('/')[5];
                            return @"http://trailers-ll.gametrailers.com/gt_vault/3000/" + Path.ChangeExtension(name, ".flv");
                            //"http://www.gametrailers.com/download/61549/t_screwattack_avgn_ninjag_gt_int5h.wmv"
                            // ->
                            //http://trailers-ll.gametrailers.com/gt_vault/3000/t_screwattack_avgn_ninjag_gt_int5h.flv
                        }
                        else
                            if (lsUrl.StartsWith(@"http://www.gametrailers.com/remote_wrap.php?mid="))
                            {
                                string id = lsUrl.Substring(48);
                                string s = @"http://mosii.gametrailers.com/getmediainfo4.php?mid=" + id;
                                string data2 = GetWebDataFromPost(s, "");
                                string[] parameters = data2.Split('&');
                                string umHostOverride = null;
                                string umFileName = null;
                                foreach (string param in parameters)
                                {
                                    string[] nameValue = param.Split('=');
                                    if (nameValue.Length == 2)
                                    {
                                        if (nameValue[0].Equals("umhostoverride")) umHostOverride = nameValue[1];
                                        if (nameValue[0].Equals("umfilename")) umFileName = HttpUtility.UrlDecode(nameValue[1]) + ".flv";
                                    }
                                }
                                if (!String.IsNullOrEmpty(umHostOverride) && !String.IsNullOrEmpty(umFileName))
                                    return "http://" + umHostOverride + "/gt_vault/" + umFileName;
                                else
                                    return lsUrl;
                            }
                            else
                                return lsUrl;
            }
            else
                return video.VideoUrl;
        }

        private List<VideoInfo> getVideoList1(string webData)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            string data = GetSubString(webData, @"<tbody>", @"</tbody>");
            data = data.Replace("<p>", String.Empty);
            data = data.Replace("</p>", String.Empty);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(data);
            XmlNodeList episodes = doc.SelectNodes("//td");


            foreach (XmlNode node in episodes)
            {
                VideoInfo video = new VideoInfo();

                if (node.FirstChild != null)
                {
                    XmlNode subNode;
                    if (node.FirstChild.Name == "span")
                        subNode = node.FirstChild;
                    else
                        subNode = node;

                    XmlNode imgNode = subNode.SelectSingleNode("a/img");
                    if (imgNode != null)
                    {
                        video.ImageUrl = imgNode.Attributes["src"].Value;
                        video.VideoUrl = imgNode.ParentNode.Attributes["href"].Value;
                    }

                    XmlNodeList aNodes = subNode.SelectNodes("a");
                    foreach (XmlNode node2 in aNodes)
                    {
                        if (String.IsNullOrEmpty(video.Title))
                            video.Title = node2.InnerText.Trim();
                        else
                            video.Description += ' ' + node2.InnerText;
                    }

                    foreach (XmlNode child in subNode.ChildNodes)
                        if (child.Name != "a")
                        {
                            string s = child.InnerText;
                            if (s.ToLower().StartsWith("\nrun time: "))
                                video.Length = s.Substring(11);
                            else
                            {
                                s = s.Trim('\n');
                                if (String.IsNullOrEmpty(video.Title))
                                    video.Title = s;
                                else
                                    video.Description += ' ' + s;
                            }
                        }

                    if (video.VideoUrl != String.Empty)
                        videos.Add(video);

                }
            }
            return videos;
        }

        private List<VideoInfo> getVideoList2(string webData)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            int i = webData.IndexOf(@"class=""date""");
            if (i >= 0) webData = webData.Substring(i);
            i = webData.IndexOf("span");
            int j = webData.IndexOf("script");
            if (i < 0) i = j;
            else
                if (j >= 0) i = Math.Min(i, j);

            string data = webData.Substring(0, i);

            Match m = regEx_VideoList.Match(data);
            while (m.Success)
            {
                VideoInfo video = new VideoInfo();
                video.Title = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                video.VideoUrl = m.Groups["url"].Value;
                video.ImageUrl = m.Groups["thumb"].Value;
                if (video.VideoUrl != String.Empty &&
                    (video.Title != String.Empty || video.ImageUrl != String.Empty))
                    videos.Add(video);
                m = m.NextMatch();
            }
            return videos;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            string url = ((RssLink)category).Url;
            string data = GetWebData(url);
            if (!string.IsNullOrEmpty(data))
            {
                List<VideoInfo> videos = null;

                try
                {
                    videos = getVideoList1(data);
                }
                catch
                { }

                if (videos == null || videos.Count == 0)
                    videos = getVideoList2(data);

                /*
                 foreach (VideoInfo video in videos)
                {
                    //video.Description += '\n' + video.VideoUrl;
                    video.Description += '\n' + getUrl(video);
                }
                 */
                return videos;
            }
            else
                return new List<VideoInfo>();

        }

        private string GetSubString(string s, string start, string until)
        {
            int p = s.IndexOf(start);
            if (p == -1) return String.Empty;
            int q = s.IndexOf(until, p);
            if (q == -1) return s.Substring(p);
            return s.Substring(p, q + start.Length + 1 - p);
        }

    }
}

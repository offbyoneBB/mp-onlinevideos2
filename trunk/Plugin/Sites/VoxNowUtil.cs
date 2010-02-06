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
    public class VoxNowUtil : SiteUtilBase
    {

        string CategoryRegex = @">FREE\s\|</a>\s*</div>\s*<div\sstyle=""""\sclass=""seriennavi_link"">\s*<a\shref=""(?<url>[^""]+)""\sstyle="""">(?<title>[^<]+)</a>";

        //<a href="aufunddavon.php?film_id=20616&player=1&season=0&na=1">Daniela Katzenberger: Wie alles begann</a> </div>		<div class="season">0.</div><div class="number"></div><div class="time">27.12.2009 15:15</div><div class="buy">    <a href=aufunddavon.php?container_id=32628&player=1&season=0>kostenlos</a>
        string VideoListRegex = @"<a\shref=""(?<url>[^""]+)"">(?<title>[^<]+)</a>\s*</div>\s*<div\sclass=""season"">(?<dummy>[^<]+)</div><div\sclass=""number"">(?<dummy2>[^>]+)><div\sclass=""time"">(?<dummy3>[^<]+)</div><div\sclass=""buy"">\s*<a\shref=(?<dummy3>[^>]+)>kostenlos</a>";

        string VideoXmlRegex = @"data\:""(?<url>[^""]+)"",";

        string baseUrl = "http://www.voxnow.de";

        Regex regEx_Category;
        Regex regEx_VideoList;
        Regex regEx_VideoXml;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_Category = new Regex(CategoryRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_VideoList = new Regex(VideoListRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_VideoXml = new Regex(VideoXmlRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
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
                    RssLink cat = new RssLink();
                    cat.Url = baseUrl + "/" + m.Groups["url"].Value;
                    cat.Name = m.Groups["title"].Value;
                    Settings.Categories.Add(cat);
                    m = m.NextMatch();
                }
                Settings.DynamicCategoriesDiscovered = true;
                return Settings.Categories.Count;
            }
            return 0;
        }

        public override String getUrl(VideoInfo video)
        {

            string data = GetWebData(video.VideoUrl);

            if (!string.IsNullOrEmpty(data))
            {
                Match m = regEx_VideoXml.Match(data);
                if (m.Success)
                {
                    string url = HttpUtility.UrlDecode(m.Groups["url"].Value);
                    data = GetWebData(url);
                    if (!string.IsNullOrEmpty(data))
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(data);
                        XmlElement root = doc.DocumentElement;
                        string rtmpeUrl = root.SelectSingleNode("./playlist/videoinfo/filename").InnerText;
                        string host = rtmpeUrl.Substring(rtmpeUrl.IndexOf("//") + 2, rtmpeUrl.IndexOf("/", rtmpeUrl.IndexOf("//") + 2) - rtmpeUrl.IndexOf("//") - 2);
                        string tcUrl = rtmpeUrl.Substring(0, rtmpeUrl.IndexOf("voxnow")+6);
                        //"rtmpe://fms-fra33.rtl.de/voxnow/176/V_34016_AOYK_m02251_18515_16x9-lq-512x288-h264-c2_bd091eab4b02b727e21b37220d0a2ed7.f4v"
                        string playpath = rtmpeUrl.Substring(rtmpeUrl.IndexOf("voxnow") + 7);
                        if (playpath.Contains(".f4v"))
                        {
                            playpath = "MP4:" + playpath;
                        }
                        else
                        {
                            playpath = playpath.Substring(0, playpath.Length - 4);
                        }
                        string resultUrl = string.Format("http://127.0.0.1:{0}/stream.flv?rtmpurl={1}&hostname={2}&tcUrl={3}&app={4}&swfurl={5}&swfsize={6}&swfhash={7}&pageurl={8}&playpath={9}",
                                            OnlineVideoSettings.RTMP_PROXY_PORT,
                                            rtmpeUrl, //rtmpUrl
                                            host, //host
                                            tcUrl, //tcUrl
                                            "voxnow", //app
                                            "http://www.voxnow.de/includes/rtlnow_videoplayer09_2.swf", //swfurl
                                            "414239",
                                            "6a31c95d659eb33bfffc315e9da4cf74ed6498e599d2bacb31675968b355fbdf",
                                            video.VideoUrl, //pageUrl
                                            playpath //playpath
                                            );
                        return resultUrl;
                    }
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
                Match m = regEx_VideoList.Match(data);
                while (m.Success)
                {
                    VideoInfo video = new VideoInfo();
                    
                    video.VideoUrl = baseUrl + "/" + m.Groups["url"].Value;
                    video.Title = HttpUtility.HtmlDecode(m.Groups["title"].Value);

                    videos.Add(video);
                    m = m.NextMatch();
                }
            }
            return videos;
        }
    }
}
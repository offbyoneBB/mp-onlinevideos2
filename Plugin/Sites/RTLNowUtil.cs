using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace OnlineVideos.Sites
{    
    public class RTLNowUtil : SiteUtilBase
    {
        public enum VideoQuality { Normal, High };

        [Category("OnlineVideosUserConfiguration"), Description("Normal or high quality for the videos according to bandwidth.")]
        VideoQuality videoQuality = VideoQuality.Normal;        

        //<a href="/30-minuten-deutschland.php"><img src="/img_08/free_short.jpg" border="0" alt="30 Minuten Deutschland"></a>

        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the baseUrl for categories.")]
        string categoriesRegex = @"<a\shref=""(?<url>[^""]+)""><img\ssrc=""(?<image>[^""]+)""\sborder=""0""\salt=""(?<title>[^""]+)""></a>";

        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the videos for a category.")]
        string videosRegex = @"<div\sclass=""title""[^>]*>\s*
<a\shref=""[^""]+"">(?<title>[^<]*)</a>\s*
</div>\s*
<div\sclass=""season"">[^<]*</div>\s*
<div\sclass=""number"">[^<]*</div>\s*
<div\sclass=""time"">(?<date>[^<]*)</div>\s*
<div\sclass=""buy"">\s*
<a\shref=(?<url>[^>]+)>kostenlos</a>\s*
</div>";

        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the playlist url for a video.")]
        string playlistRegex = @"data\:""(?<url>[^""]+)"",";

        string baseUrl = "http://rtl-now.rtl.de";

        Regex regEx_Categories;
        Regex regEx_Videos;
        Regex regEx_Playlist;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            regEx_Categories = new Regex(categoriesRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_Videos = new Regex(videosRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
            regEx_Playlist = new Regex(playlistRegex, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            string data = GetWebData(baseUrl);
            if (!string.IsNullOrEmpty(data))
            {
                Match m = regEx_Categories.Match(data);
                while (m.Success)
                {
                    if (!m.Groups["image"].Value.Contains("blank.gif"))
                    {
                        RssLink cat = new RssLink();
                        cat.Url = m.Groups["url"].Value;
                        if (!cat.Url.StartsWith("http://")) cat.Url = baseUrl + cat.Url;
                        cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                        Settings.Categories.Add(cat);
                    }
                    m = m.NextMatch();
                }
                Settings.DynamicCategoriesDiscovered = true;
            }
            return Settings.Categories.Count;
        }
        
        public override String getUrl(VideoInfo video)
        {
            string data = GetWebData(video.VideoUrl);
            if (!string.IsNullOrEmpty(data))
            {
                Match m = regEx_Playlist.Match(data);
                if (m.Success)
                {
                    string url = HttpUtility.UrlDecode(m.Groups["url"].Value);
                    data = GetWebData(url);
                    XmlDocument xml = new XmlDocument();
                    xml.LoadXml(data);

                    string rtmpeUrl = xml.SelectSingleNode("//playlist/videoinfo/filename").InnerText;
                    string host = rtmpeUrl.Substring(rtmpeUrl.IndexOf("//") + 2, rtmpeUrl.IndexOf("/", rtmpeUrl.IndexOf("//") + 2) - rtmpeUrl.IndexOf("//") - 2);
                    string tcUrl = rtmpeUrl.Substring(0, rtmpeUrl.IndexOf("rtlnow") + 6);
                    string playpath = rtmpeUrl.Substring(rtmpeUrl.IndexOf("rtlnow") + 7);
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
                                        "rtlnow", //app
                                        "http://rtl-now.rtl.de/includes/rtlnow_videoplayer09_2.swf", //swfurl
                                        "414239",
                                        "6a31c95d659eb33bfffc315e9da4cf74ed6498e599d2bacb31675968b355fbdf",
                                        video.VideoUrl, //pageUrl
                                        playpath //playpath
                                        );
                    return resultUrl;
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
                Match m = regEx_Videos.Match(data);
                while (m.Success)
                {
                    VideoInfo video = new VideoInfo();
                    video.VideoUrl = m.Groups["url"].Value;
                    if (!video.VideoUrl.StartsWith("http://")) video.VideoUrl = baseUrl + "/" + video.VideoUrl;
                    video.Title = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                    video.Length = m.Groups["date"].Value;
                    videos.Add(video);
                    m = m.NextMatch();
                }                
            }
            return videos;           
        }       
    }
}
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Web;

namespace OnlineVideos.Sites
{
    public class BildFilmeUtil : GenericSiteUtil
    {
        public override String getUrl(VideoInfo video)
        {
            string data = GetWebData(video.VideoUrl);
            string id = Regex.Match(data, @"'\.xml\?s=(?<url>xml[^']+)'").Groups["url"].Value;
            string xmlUrl = "http://film.bild.de/movie.xml?s=" + id;
            data = GetWebData(xmlUrl);
            string url = Regex.Match(data, @"<enclosure\s*url=""(?<url>[^""]+)").Groups["url"].Value;
            if(!url.Contains("rtmpe:"))url = url.Replace("rtmp:","rtmpe:");
            url = HttpUtility.HtmlDecode(url);

            string host = url.Substring(url.IndexOf(":") + 3, url.IndexOf("/", url.IndexOf(":") + 3) - (url.IndexOf(":") + 3));
            string app = "ondemand?ovpfv=1.1&" +url.Substring(url.IndexOf("?") + 1);
            string tcUrl = "rtmpe://" + host + ":1935" + "/" + app;
            string playpath = "mp4:" + url.Substring(url.LastIndexOf("=") + 1) + ".flv" + url.Substring(url.IndexOf("?"));

			string resultUrl = new MPUrlSourceFilter.RtmpUrl(tcUrl, host, 1935)
			{
				App = app,
				SwfUrl = "http://film.bild.de/swf/SmartclipPlayer_1.0.2_nf_28_04_2010_13_40.swf",
				SwfVerify = true,
				PlayPath = playpath
			}.ToString();

            return resultUrl;
        }
    }
}
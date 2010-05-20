using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class RTLGroupUtil : GenericSiteUtil
    {
        [Category("OnlineVideosConfiguration"), Description("Value used to RTMPe Verification")]
        string app;

        public override String getUrl(VideoInfo video)
        {
            string data = GetWebData(video.VideoUrl);

            if (!string.IsNullOrEmpty(data))
            {
                Match m = Regex.Match(data, @"data\:""(?<url>[^""]+)"",");
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
                        string tcUrl = rtmpeUrl.Substring(0, rtmpeUrl.IndexOf(app) + 6);
                        string playpath = rtmpeUrl.Substring(rtmpeUrl.IndexOf(app) + 7);
                        if (playpath.Contains(".f4v"))
                            playpath = "MP4:" + playpath;
                        else
                            playpath = playpath.Substring(0, playpath.Length - 4);

                        string resultUrl = string.Format("http://127.0.0.1:{0}/stream.flv?rtmpurl={1}&hostname={2}&tcUrl={3}&app={4}&swfurl={5}&swfsize={6}&swfhash={7}&pageurl={8}&playpath={9}",
                                            OnlineVideoSettings.RTMP_PROXY_PORT,
                                            rtmpeUrl, //rtmpUrl
                                            host, //host
                                            tcUrl, //tcUrl
                                            app, //app
                                            baseUrl + "/includes/rtlnow_videoplayer09_2.swf", //swfurl
                                            "444122",
                                            "d7b8968195c92ab83c571c1329bfd473198ab19a1917c9d0a6b8c71222fab454",
                                            video.VideoUrl, //pageUrl
                                            playpath //playpath
                                            );
                        return resultUrl;
                    }
                }
            }
            return null;
        }
    }
}
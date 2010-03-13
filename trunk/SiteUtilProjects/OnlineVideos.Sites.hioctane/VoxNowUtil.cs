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
    public class VoxNowUtil : GenericSiteUtil
    {
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse the baseUrl for dynamic categories. Group names: 'url', 'title'. Will not be used if not set.")]
        string dynamicCategoriesRegEx;
        [Category("OnlineVideosConfiguration"), Description("Url used for prepending relative links.")]
        string baseUrl;
        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for videos. Group names: 'VideoUrl', 'ImageUrl', 'Title', 'Duration', 'Description'.")]
        string videoListRegEx;

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
                        string tcUrl = rtmpeUrl.Substring(0, rtmpeUrl.IndexOf("voxnow")+6);
                        string playpath = rtmpeUrl.Substring(rtmpeUrl.IndexOf("voxnow") + 7);
                        if (playpath.Contains(".f4v"))
                            playpath = "MP4:" + playpath;
                        else
                            playpath = playpath.Substring(0, playpath.Length - 4);

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
    }
}
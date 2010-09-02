using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Windows.Forms;

namespace OnlineVideos.Sites
{
    public class RTLGroupUtil : GenericSiteUtil
    {
        [Category("OnlineVideosConfiguration"), Description("Value used to RTMPe Verification")]
        string app;

        public override String getUrl(VideoInfo video)
        {
            string data = GetWebData(HttpUtility.HtmlDecode(video.VideoUrl));

            if (!string.IsNullOrEmpty(data))
            {
                Match m = Regex.Match(data, @"data\:""(?<url>[^""]+)"",");
                if (m.Success)
                {
                    string url = HttpUtility.UrlDecode(m.Groups["url"].Value);
                    if (!Uri.IsWellFormedUriString(url, System.UriKind.Absolute)) url = new Uri(new Uri(baseUrl), url).AbsoluteUri;
                    data = GetWebData(url);
                    if (!string.IsNullOrEmpty(data))
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(data);
                        XmlElement root = doc.DocumentElement;

                        string para1 = root.SelectSingleNode("./para1").InnerText;
                        string para2 = root.SelectSingleNode("./para2").InnerText;
                        string para4 = root.SelectSingleNode("./para4").InnerText;
                        string timetype = root.SelectSingleNode("./timetype").InnerText;
                        string fkcontent = root.SelectSingleNode("./fkcontent").InnerText;
                        string season = root.SelectSingleNode("./season").InnerText;
                        string fmstoken = root.SelectSingleNode("./fmstoken").InnerText;
                        string fmstoken_time = root.SelectSingleNode("./fmstoken_time").InnerText;
                        string fmstoken_renew = root.SelectSingleNode("./fmstoken_renew").InnerText;
                        string ivw = Regex.Match(url, "ivw=(?<ivw>[^&]+)&").Groups["ivw"].Value;

                        string rtmpeUrl = root.SelectSingleNode("./playlist/videoinfo/filename").InnerText;

                        string tokenUrl = fmstoken_renew + "&token=" + fmstoken + "&ts=" + fmstoken_time;
                        string fmsData = GetWebData(tokenUrl);

                        if (!string.IsNullOrEmpty(fmsData))
                        {
                            XmlDocument fmsDoc = new XmlDocument();
                            fmsDoc.LoadXml(fmsData);
                            XmlElement fmsRoot = fmsDoc.DocumentElement;

                            string secret = fmsRoot.SelectSingleNode("./secret").InnerText;
                            string onetime = fmsRoot.SelectSingleNode("./onetime").InnerText;

                            string host = rtmpeUrl.Substring(rtmpeUrl.IndexOf("//") + 2, rtmpeUrl.IndexOf("/", rtmpeUrl.IndexOf("//") + 2) - rtmpeUrl.IndexOf("//") - 2);
                            string tcUrl = "rtmpe://" + host + ":1935" + "/" + app;
                            string playpath = rtmpeUrl.Substring(rtmpeUrl.IndexOf(app) + app.Length + 1);

                            string combinedPlaypath = "";
                            if (playpath.Contains(".f4v"))
                                combinedPlaypath = "mp4:" + playpath;

                            combinedPlaypath += "?ivw=" + ivw;
                            combinedPlaypath += "&client=videoplayer&type=content&user=2880224004&session=2289727260&angebot=rtlnow&starttime=00:00:00:00&timetype=" + timetype;
                            combinedPlaypath += "&fkcontent=" + fkcontent;
                            combinedPlaypath += "&season=" + season;

                            //-C S:0 
                            //-C S:718e64ad271752d80263459c9a968e83 
                            //-C S:2010-08-12 11:05:40 
                            //-C S: 
                            //-C S: 
                            //-C S:29482 
                            //-C S:2/V_52030_BBF2_E81816_45570_16x9-lq-512x288-h264-c0_804b3855e95c396edad43b16081bbff9

                            /*  - C1Command ist $para2
                                - C2Command ist $secret
                                - C3Command ist $onetime
                                - C6Command ist $para1
                                - C7Command ist $filename ohne das "rtmpe://" und ohne Dateiendung ".f4v"
                                */

                            string resultUrl = ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                                        string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&hostname={1}&tcUrl={2}&app={3}&swfurl={4}&swfsize={5}&swfhash={6}&pageurl={7}&playpath={8}&conn={9}&conn={10}&conn={11}&conn={12}&conn={13}&conn={14}&conn={15}",
                                                tcUrl, //rtmpUrl
                                                host, //host
                                                tcUrl, //tcUrl
                                                app, //app
                                                baseUrl + "/includes/rtlnow_videoplayer09_2.swf", //swfurl
                                                "444122",
                                                "d7b8968195c92ab83c571c1329bfd473198ab19a1917c9d0a6b8c71222fab454",
                                                video.VideoUrl, //pageUrl
                                                combinedPlaypath, //playpath
                                                "S:" + para2,
                                                "S:" + secret,
                                                "S:" + onetime,
                                                "S:",
                                                "S:",
                                                "S:" + para1,
                                                "S:" + playpath.Substring(0,playpath.Length-4)));
                            return resultUrl;
                        }
                    }
                }
            }
            return null;
        }
    }
}
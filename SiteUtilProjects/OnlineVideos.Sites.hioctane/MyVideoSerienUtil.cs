using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class MyVideoSerienUtil : GenericSiteUtil
    {
        string GK = "WXpnME1EZGhNRGhpTTJNM01XVmhOREU0WldNNVpHTTJOakptTW1FMU5tVTBNR05pWkRaa05XRXhNVFJoWVRVd1ptSXhaVEV3TnpsbA0KTVRkbU1tSTRNdz09";

        public override String getUrl(VideoInfo video)
        {
            // get the Id of the video from the VideoUrl
            string videoId = Regex.Match(video.VideoUrl, @"watch/(\d+)/").Groups[1].Value;
            if (string.IsNullOrEmpty(videoId)) throw new OnlineVideosException("Couldn't find Video Id!");
            // build the url where we can get the encoded Xml that holds playback information
            string data = GetWebData(video.VideoUrl);
            string url = "";
            var parameters = HttpUtility.ParseQueryString(string.Empty);
            foreach(Match m in Regex.Matches(data, @"p.addVariable\('([^']+)',\s*'([^']+)'\)"))
            {
                if (m.Groups[1].Value == "_encxml") url = HttpUtility.UrlDecode(m.Groups[2].Value);
                else
                {
                    if (m.Groups[1].Value == "flash_playertype" && m.Groups[2].Value == "MTV")
                        parameters.Add("flash_playertype", "D");
                    else
                        parameters.Add(m.Groups[1].Value, m.Groups[2].Value);
                }
            }
            // check if webpage uses a different type of player
            if (string.IsNullOrEmpty(url))
            {
                string sevenLoadUrl = Regex.Match(data, @"<object\s+type='application/x-shockwave-flash'\s+data='(http://de.sevenload.com[^']+)'").Groups[1].Value;
                if (!string.IsNullOrEmpty(sevenLoadUrl))
                {
                    sevenLoadUrl = GetRedirectedUrl(sevenLoadUrl);
                    if (!string.IsNullOrEmpty(sevenLoadUrl))
                    {
                        sevenLoadUrl = HttpUtility.UrlDecode(HttpUtility.ParseQueryString(new Uri(sevenLoadUrl).Query)["configPath"]);
                        string sevenLoadXml = GetWebData(sevenLoadUrl);
                        XmlDocument xDoc = new XmlDocument();
                        xDoc.LoadXml(sevenLoadXml);
                        video.PlaybackOptions = new Dictionary<string, string>();
                        foreach (XmlElement streamElement in xDoc.SelectNodes("//stream"))
                        {
                            video.PlaybackOptions.Add(
                                string.Format("{0} - {1}x{2} ({3})", 
                                    streamElement.GetAttribute("quality"), 
                                    streamElement.GetAttribute("width"), 
                                    streamElement.GetAttribute("height"), 
                                    streamElement.GetAttribute("codec")), 
                                streamElement.InnerText);
                        }
                        return video.PlaybackOptions.Last().Value;
                    }
                }
            }
            else
            {
                // decode the xml
                string enc_data = GetWebData(url + "?" + parameters.ToString(), referer: video.VideoUrl).Split('=')[1];
                var enc_data_b = ArrayFromHexstring(enc_data);
                var p1 = ASCIIEncoding.ASCII.GetString(Convert.FromBase64String(ASCIIEncoding.ASCII.GetString(Convert.FromBase64String(GK))));
                var p2 = Utils.GetMD5Hash(videoId.ToString());
                var sk = ASCIIEncoding.ASCII.GetBytes(Utils.GetMD5Hash(p1 + p2));
                byte[] dec_data = new byte[enc_data_b.Length];
                var rc4 = new Org.BouncyCastle.Crypto.Engines.RC4Engine();
                rc4.Init(false, new Org.BouncyCastle.Crypto.Parameters.KeyParameter(sk));
                rc4.ProcessBytes(enc_data_b, 0, enc_data_b.Length, dec_data, 0);
                var dec = ASCIIEncoding.ASCII.GetString(dec_data);
                XmlDocument xDoc = new XmlDocument();
                xDoc.LoadXml(dec);
                // get playback url from decoded xml
                XmlElement videoElement = xDoc.SelectSingleNode("//video") as XmlElement;
                string rtmpUrl = HttpUtility.UrlDecode(videoElement.GetAttribute("connectionurl"));
                if (rtmpUrl.StartsWith("rtmp"))
                {
                    if (rtmpUrl.Contains("myvideo2flash")) rtmpUrl = rtmpUrl.Replace("rtmpe://", "rtmpt://");
                    string playPath = HttpUtility.UrlDecode(videoElement.GetAttribute("source"));
                    playPath = string.Format("{0}:{1}", playPath.Substring(playPath.LastIndexOf('.') + 1), playPath.Substring(0, playPath.LastIndexOf('.')));
                    string pageUrl = string.Format("http://www.myvideo.de/watch/{0}/", videoId);
                    string swfUrl = HttpUtility.UrlDecode(Regex.Match(data, @"new SWFObject\(\'(.+?)\'").Groups[1].Value);
                    return new MPUrlSourceFilter.RtmpUrl(rtmpUrl) { TcUrl = rtmpUrl, SwfUrl = swfUrl, SwfVerify = true, PlayPath = playPath, PageUrl = pageUrl }.ToString();
                }
                else
                {
                    return HttpUtility.UrlDecode(videoElement.GetAttribute("path")) + HttpUtility.UrlDecode(videoElement.GetAttribute("source"));
                }
            }
            return "";
        }

        byte[] ArrayFromHexstring(string s)
        {
            List<byte> a = new List<byte>();
            for (int i = 0; i < s.Length; i = i + 2)
            {
                a.Add(byte.Parse(s.Substring(i, 2), System.Globalization.NumberStyles.HexNumber));
            }
            return a.ToArray();
        }
    }
}
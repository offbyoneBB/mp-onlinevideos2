using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows.Forms;
using Ionic.Zip;
using System.Xml;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    /*public class MyVideoFilmeUtil : GenericSiteUtil
    {

        public override String getUrl(VideoInfo video)
        {
            string data = GetWebData(video.VideoUrl);

            string url = Regex.Match(data, @"<ref\shref=""(?<url>[^""]+)""/>").Groups["url"].Value;
            string host = url.Substring(url.IndexOf(":") + 3, url.IndexOf("/", url.IndexOf(":") + 3) - (url.IndexOf(":") + 3));
            string app = url.Substring(host.Length + url.IndexOf(host) + 1, (url.IndexOf("/", url.IndexOf("/", (host.Length + url.IndexOf(host) + 1)) + 1)) - (host.Length + url.IndexOf(host) + 1));
            string tcUrl = "rtmpe://" + host + ":1935" + "/" + app;
            string playpath = url.Substring(url.IndexOf(app) + app.Length + 1);
            if (data.Contains("mp4:"))
                playpath = "mp4:" + playpath;

            string resultUrl = ReverseProxy.Instance.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&hostname={1}&tcUrl={2}&app={3}&swfurl={4}&swfsize={5}&swfhash={6}&playpath={7 }",
                    url, //rtmpUrl
                    host, //host
                    tcUrl, //tcUrl
                    app, //app
                    "http://statix.myvideo.de/dev_syncro/player/my_video_avod_player.swf", //swfurl
                    "1586064", //swfsize
                    "a850e63353c8254e6aff48928b8d85320784092f98256d60b34d6316267a4338", //swfhash
                    playpath //playpath
                ));
            return resultUrl;

        }
    }*/
}
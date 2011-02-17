using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Xml;
using System.Web;

namespace OnlineVideos.Sites
{
    public class TouTvUtil : GenericSiteUtil
    {
        public override string getUrl(VideoInfo video)
        {
            string playListUrl = getPlaylistUrl(video.VideoUrl);
            if (String.IsNullOrEmpty(playListUrl))
                return String.Empty; // if no match, return empty url -> error

            video.PlaybackOptions = new Dictionary<string, string>();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(GetWebData(playListUrl));

            XmlNamespaceManager nsmRequest = new XmlNamespaceManager(doc.NameTable);
            nsmRequest.AddNamespace("a", @"http://www.w3.org/2001/SMIL20/Language");

            string rtmpUrl = doc.SelectSingleNode("//a:head/a:meta", nsmRequest).Attributes["base"].Value;
            Uri uri = new Uri(rtmpUrl);
            NameValueCollection nn = HttpUtility.ParseQueryString(uri.Query);
            string auth = nn["auth"];

            foreach (XmlNode node in doc.SelectNodes("//a:body/a:switch/a:video", nsmRequest))
            {
                int bitrate = Int32.Parse(node.Attributes["system-bitrate"].Value);
                string vidUrl = node.Attributes["src"].Value;

                string url = ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                    string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&playpath={1}&swfVfy={2}&auth={3}",
                    HttpUtility.UrlEncode(rtmpUrl),
                    HttpUtility.UrlEncode(vidUrl),
                    HttpUtility.UrlEncode(@"http://static.tou.tv/lib/ThePlatform/4.2/swf/flvPlayer.swf"),
                    auth));
                video.PlaybackOptions.Add((bitrate / 1000).ToString() + "Kb", url);
            }

            string resultUrl;
            if (video.PlaybackOptions.Count == 0) return String.Empty;// if no match, return empty url -> error
            else
            {
                // return first found url as default
                var enumer = video.PlaybackOptions.GetEnumerator();
                enumer.MoveNext();
                resultUrl = enumer.Current.Value;
            }
            if (video.PlaybackOptions.Count == 1) video.PlaybackOptions = null;// only one url found, PlaybackOptions not needed
            return resultUrl;
        }

    }
}

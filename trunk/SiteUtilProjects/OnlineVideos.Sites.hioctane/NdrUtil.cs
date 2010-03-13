using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
namespace OnlineVideos.Sites
{
    public class NdrUtil : GenericSiteUtil
    {
        public override String getUrl(VideoInfo video)
        {
            if (video.PlaybackOptions == null)
            {
                string data = GetWebData(video.VideoUrl);
                video.PlaybackOptions = new Dictionary<string, string>();
                
                if (!string.IsNullOrEmpty(data))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(data);
                    XmlElement root = doc.DocumentElement;
                    XmlNodeList list;
                    list = root.SelectNodes("./sources/source");
                    foreach (XmlNode node in list)
                    {
                        string title = node.SelectSingleNode("@mimetype").InnerText + " - " + node.SelectSingleNode("@format").InnerText;
                        string url = node.InnerText;
                        url = url.Replace("rtmpt://fms.edge.newmedia.nacamar.net", "rtmpt://ndr.fcod.llnwd.net/a3715/d1/flashmedia/streams");
                        string resultUrl = string.Format("http://127.0.0.1:{0}/stream.flv?rtmpurl={1}", OnlineVideoSettings.RTMP_PROXY_PORT, System.Web.HttpUtility.UrlEncode(url));
                        video.PlaybackOptions.Add(title, resultUrl);
                    }
                }
            }
            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
            {
                var enumer = video.PlaybackOptions.GetEnumerator();
                enumer.MoveNext();
                return enumer.Current.Value;
            }
            return "";
        }
    }
}
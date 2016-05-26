using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class DreiSatUtil : GenericSiteUtil
    {
        public override string GetVideoUrl(VideoInfo video)
        {
            var page = GetWebData(video.VideoUrl);

            var m = Regex.Match(page, "var\\s+url_xmlservice=\"(?<url>[^\"]+)\"");
            if (m.Success)
            {
                var xmlUrl = m.Groups["url"].Value;
                if (xmlUrl.StartsWith("/")) xmlUrl = "http:" + xmlUrl;
                var xml = GetWebData<XmlDocument>(xmlUrl);

                var ns = new XmlNamespaceManager(xml.NameTable);
                ns.AddNamespace("my", xml.DocumentElement.NamespaceURI);
                var basename = xml.SelectSingleNode("//my:video/my:details/my:basename", ns);
                var streamVersion = xml.SelectSingleNode("//my:video/my:details/my:streamVersion", ns);

                var jsonUrl = "http://www.zdf.de/ptmd/vod/3sat/" + basename.InnerText;
                if (streamVersion != null) jsonUrl += "/" + streamVersion.InnerText;

                var json = GetWebData<JObject>(jsonUrl);

                video.PlaybackOptions = new Dictionary<string, string>();

                foreach(var stream in json["formitaeten"])
                {
                    var type = stream.Value<string>("type");
                    if (!type.Contains("3gp") && !type.Contains("rtsp") && !type.Contains("rtmp") && !type.Contains("m3u8") && !type.Contains("vorbis") && !type.Contains("asx"))
                    {
                        var quality = stream.Value<string>("quality");
                        var url = stream["playouts"]["main"]["uris"][0].ToString();
                        video.PlaybackOptions[string.Format("{0} ({1})", quality, type)] = url;
                    }
                }
            }

            return video.PlaybackOptions.FirstOrDefault().Value;
        }
    }
}

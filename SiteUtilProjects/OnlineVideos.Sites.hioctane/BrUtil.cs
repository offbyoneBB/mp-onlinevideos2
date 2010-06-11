using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows.Forms;
using Ionic.Zip;
using System.Xml;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class BrUtil : SiteUtilBase
    {
        string baseUrl = "http://rd.gl-systemhaus.de/br/b7/archive/archive.xml.zip.adler32";
        string outputString = "";

        public override int DiscoverDynamicCategories()
        {
            WebClient Client = new WebClient();
            byte[] downloadBuffer = Client.DownloadData (baseUrl);
            ZipFile zipFile = ZipFile.Read(downloadBuffer);
            foreach (ZipEntry zipEntry in zipFile)
            {
                byte[] output = new byte[0];
                using (MemoryStream ms = new MemoryStream())
                {
                    zipEntry.Extract(ms);
                    output = ms.ToArray();
                }
                outputString = new System.Text.UTF8Encoding().GetString(output);

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(outputString);
                XmlElement root = doc.DocumentElement;
                XmlNodeList list;
                list = root.SelectNodes("../archiv/sendungen/sendung");
                foreach (XmlNode sendung in list)
                {
                    RssLink cat = new RssLink();
                    foreach(XmlAttribute attribute in sendung.Attributes)
                    {
                        switch (attribute.Name)
                        {
                            case "name":
                                cat.Name = attribute.Value;
                                break;
                            case "bild":
                                cat.Thumb = attribute.Value;
                                break;
                            case "id":
                                cat.Url = attribute.Value;
                                break;
                        }
                    }
                    if (!String.IsNullOrEmpty(cat.Name) && !String.IsNullOrEmpty(cat.Url))
                    {
                        XmlNodeList list2;
                        list2 = root.SelectNodes("../archiv/ausstrahlungen/ausstrahlung");
                        int i = 0;
                        foreach (XmlNode ausstrahlung in list2)
                        {
                            if (cat.Url.CompareTo(ausstrahlung.SelectSingleNode("sendung").InnerText) == 0)
                                if (ausstrahlung.SelectSingleNode("videos").ChildNodes.Count > 0)
                                   i++;
                        }
                        if (i > 0)
                        {
                            cat.EstimatedVideoCount = (uint)i;
                            Settings.Categories.Add(cat);
                        }
                    }
                }
                Settings.DynamicCategoriesDiscovered = true;
                return Settings.Categories.Count;
            }
            return 0;
        }

        public override String getUrl(VideoInfo video)
        {
            if (video.PlaybackOptions == null)
            {
                video.PlaybackOptions = new Dictionary<string, string>();
                Match m = Regex.Match(video.VideoUrl, @"<video\sapplication=""(?<app>[^""]+)""\s*host=""(?<host>[^""]+)""\s*groesse=""(?<title>[^""]+)""\s*stream=""(?<stream>[^""]+)""");
                while (m.Success)
                {

                    string rtmpurl = "rtmp://" + m.Groups["host"].Value + ":1935/" + m.Groups["app"].Value + "/";
                    if(m.Groups["stream"].Value.Contains("mp4"))
                        rtmpurl = rtmpurl + "mp4:" + m.Groups["stream"].Value;
                    else
                        rtmpurl = rtmpurl + m.Groups["stream"].Value;
                    video.PlaybackOptions.Add(m.Groups["title"].Value, rtmpurl);
                    m = m.NextMatch();
                }
            }
            if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
            {
                var enumer = video.PlaybackOptions.GetEnumerator();
                enumer.MoveNext();
                return enumer.Current.Value;
            } 
            return null;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(outputString);
            XmlElement root = doc.DocumentElement;
            XmlNodeList list;
            list = root.SelectNodes("../archiv/ausstrahlungen/ausstrahlung");
            foreach (XmlNode ausstrahlung in list)
            {
                if ((category as RssLink).Url.CompareTo(ausstrahlung.SelectSingleNode("sendung").InnerText) == 0)
                {
                    VideoInfo video = new VideoInfo();
                    video.Title = ausstrahlung.SelectSingleNode("titel").InnerText;
                    video.Title = video.Title + " - " + ausstrahlung.SelectSingleNode("nebentitel").InnerText;
                    if(ausstrahlung.SelectSingleNode("bild") != null)video.ImageUrl = ausstrahlung.SelectSingleNode("bild").InnerText;
                    video.Description = ausstrahlung.SelectSingleNode("beschreibung").InnerText;
                    if (string.IsNullOrEmpty(video.Description)) video.Description = ausstrahlung.SelectSingleNode("kurzbeschreibung").InnerText;
                    video.VideoUrl = ausstrahlung.SelectSingleNode("videos").InnerXml;
                    if(!string.IsNullOrEmpty(video.VideoUrl))
                        videos.Add(video);
                }
            }
            return videos;
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using Ionic.Zip;

namespace OnlineVideos.Sites
{
    public class BrUtil : SiteUtilBase
    {
		XmlDocument masterXmlDoc;

		public override int DiscoverDynamicCategories()
		{
			Settings.Categories.Clear();

			string js = GetWebData("http://mediathek-video.br.de/js/config.js");
			string baseUrl = Regex.Match(js, @"http://.*/archive/archive\.xml\.zip\.adler32").Value;
			WebClient Client = new WebClient();
			byte[] downloadBuffer = Client.DownloadData(baseUrl);
			ZipFile zipFile = ZipFile.Read(downloadBuffer);
			ZipEntry zipEntry = zipFile[0];
			masterXmlDoc = new XmlDocument();
			using (MemoryStream ms = new MemoryStream())
			{
				zipEntry.Extract(ms);
				ms.Position = 0;
				masterXmlDoc.Load(ms);
			}
			foreach (XmlElement sendung in masterXmlDoc.DocumentElement.SelectNodes("../archiv/sendungen/sendung"))
			{
				RssLink cat = new RssLink() { Name = sendung.GetAttribute("name"), Thumb = sendung.GetAttribute("bild"), Url = sendung.GetAttribute("id") };
				if (!String.IsNullOrEmpty(cat.Name) && !String.IsNullOrEmpty(cat.Url))
				{
					cat.EstimatedVideoCount = (uint)masterXmlDoc.DocumentElement.SelectNodes("../archiv/ausstrahlungen/ausstrahlung[sendung='" + cat.Url + "' and count(videos/video)>0]").Count;
					if (cat.EstimatedVideoCount > 0)
					{
						Settings.Categories.Add(cat);
					}
				}
			}

			Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > 0;
			return Settings.Categories.Count;
		}

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            foreach (XmlNode ausstrahlung in masterXmlDoc.DocumentElement.SelectNodes("../archiv/ausstrahlungen/ausstrahlung[sendung='" + ((RssLink)category).Url + "' and count(videos/video)>0]"))
            {
                if ((category as RssLink).Url.CompareTo(ausstrahlung.SelectSingleNode("sendung").InnerText) == 0)
                {
                    VideoInfo video = new VideoInfo();
					DateTime airdate = DateTime.MinValue;
					if (ausstrahlung.SelectSingleNode("beginnPlan") != null) if (DateTime.TryParse(ausstrahlung.SelectSingleNode("beginnPlan").InnerText, out airdate)) video.Airdate = airdate.ToString("g", OnlineVideoSettings.Instance.Locale);
                    video.Title = ausstrahlung.SelectSingleNode("titel").InnerText;
					string titleAppend = ausstrahlung.SelectSingleNode("nebentitel").InnerText;
					if (string.IsNullOrEmpty(titleAppend) && airdate != DateTime.MinValue) titleAppend = airdate.ToShortDateString();
					if (!string.IsNullOrEmpty(titleAppend)) video.Title += " - " + titleAppend;
                    if(ausstrahlung.SelectSingleNode("bild") != null)video.ImageUrl = ausstrahlung.SelectSingleNode("bild").InnerText;
                    video.Description = ausstrahlung.SelectSingleNode("beschreibung").InnerText;
                    if (string.IsNullOrEmpty(video.Description)) video.Description = ausstrahlung.SelectSingleNode("kurzbeschreibung").InnerText;
					video.VideoUrl = ausstrahlung.SelectSingleNode("hdslink").InnerText + "?hdcore=2.11.3&g=AAAAAAAAAAAA";
                    if(!string.IsNullOrEmpty(video.VideoUrl))
                        videos.Add(video);
                }
            }
            return videos;
        }

    }
}
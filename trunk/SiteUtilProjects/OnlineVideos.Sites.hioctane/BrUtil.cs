using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using Ionic.Zip;

namespace OnlineVideos.Sites
{
    public class BrUtil : SiteUtilBase
    {
		public enum VideoQuality { small, large, xlarge };

		[Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Video Quality", TranslationFieldName = "VideoQuality"), Description("Defines the preferred quality for the video to be played.")]
		VideoQuality videoQuality = VideoQuality.large;

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
                    video.VideoUrl = ausstrahlung.SelectSingleNode("videos").InnerXml;
                    if(!string.IsNullOrEmpty(video.VideoUrl))
                        videos.Add(video);
                }
            }
            return videos;
        }

		public override String getUrl(VideoInfo video)
		{
			string defaultUrl =  null;
			if (video.PlaybackOptions == null)
			{
				video.PlaybackOptions = new Dictionary<string, string>();
				Match m = Regex.Match(video.VideoUrl, @"<video\sapplication=""(?<app>[^""]+)""\s*host=""(?<host>[^""]+)""\s*groesse=""(?<title>[^""]+)""\s*stream=""(?<stream>[^""]+)""");
				while (m.Success)
				{
					string rtmpurl = new MPUrlSourceFilter.RtmpUrl("rtmp://" + m.Groups["host"].Value + ":1935/" + m.Groups["app"].Value)
					{
						App = m.Groups["app"].Value,
						PlayPath = m.Groups["stream"].Value
					}.ToString();
					video.PlaybackOptions.Add(m.Groups["title"].Value, rtmpurl);
					if (m.Groups["title"].Value == videoQuality.ToString()) defaultUrl = rtmpurl;
					m = m.NextMatch();
				}
			}
			if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
			{
				var keyList = video.PlaybackOptions.Keys.ToList();
				keyList.Sort((s1, s2) =>
				{
					try
					{
						VideoQuality v1 = (VideoQuality)Enum.Parse(typeof(VideoQuality), s1);
						VideoQuality v2 = (VideoQuality)Enum.Parse(typeof(VideoQuality), s2);
						return v1.CompareTo(v2);
					}
					catch (Exception)
					{
						return 0;
					}
				});
				Dictionary<string, string> newPlaybackOptions = new Dictionary<string, string>();
				keyList.ForEach(k => newPlaybackOptions.Add(k, video.PlaybackOptions[k]));
				video.PlaybackOptions = newPlaybackOptions;
				if (defaultUrl != null) return defaultUrl;
				else return video.PlaybackOptions.First().Value;
			}
			return null;
		}

    }
}
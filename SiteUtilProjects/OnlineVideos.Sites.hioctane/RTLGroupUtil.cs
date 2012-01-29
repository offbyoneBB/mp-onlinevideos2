using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace OnlineVideos.Sites
{
	public class RTLGroupUtil : GenericSiteUtil
	{
		[Category("OnlineVideosConfiguration"), Description("Value used to connect to the RTMP server.")]
		protected string app;

		public override String getUrl(VideoInfo video)
		{
			string data = GetWebData(HttpUtility.HtmlDecode(video.VideoUrl));

			if (!string.IsNullOrEmpty(data))
			{
				Match m = Regex.Match(data, @"data\:'(?<url>[^']+)',");
				if (m.Success)
				{
					string swfUrl = new Uri(new Uri(baseUrl), "/includes/rtlnow_videoplayer09_2.swf").AbsoluteUri;
					Match swfMatch = Regex.Match(data, @"http://.*?\.swf[^""]+");
					if (swfMatch.Success) swfUrl = swfMatch.Value;

					string url = HttpUtility.UrlDecode(m.Groups["url"].Value);
					if (!Uri.IsWellFormedUriString(url, System.UriKind.Absolute)) url = new Uri(new Uri(baseUrl), url).AbsoluteUri;
					data = GetWebData(url);
					data = data.Replace("<!-- live -->", "");
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
						// string fmstoken = root.SelectSingleNode("./fmstoken").InnerText;
						// string fmstoken_time = root.SelectSingleNode("./fmstoken_time").InnerText;
						// string fmstoken_renew = root.SelectSingleNode("./fmstoken_renew").InnerText;
						string ivw = Regex.Match(url, "ivw=(?<ivw>[^&]+)&").Groups["ivw"].Value;

						string rtmpeUrl = root.SelectSingleNode("./playlist/videoinfo/filename").InnerText;

						//string tokenUrl = fmstoken_renew + "&token=" + fmstoken + "&ts=" + fmstoken_time;
						//string fmsData = GetWebData(tokenUrl);

						//if (!string.IsNullOrEmpty(fmsData))
						//{
						//XmlDocument fmsDoc = new XmlDocument();
						//fmsDoc.LoadXml(fmsData);
						//XmlElement fmsRoot = fmsDoc.DocumentElement;

						//string secret = fmsRoot.SelectSingleNode("./secret").InnerText;
						//string onetime = fmsRoot.SelectSingleNode("./onetime").InnerText;

						string host = rtmpeUrl.Substring(rtmpeUrl.IndexOf("//") + 2, rtmpeUrl.IndexOf("/", rtmpeUrl.IndexOf("//") + 2) - rtmpeUrl.IndexOf("//") - 2);
						string tcUrl = "rtmpe://" + host + ":1935" + "/" + app;
						string playpath = rtmpeUrl.Substring(rtmpeUrl.IndexOf(app.Replace("_free", "")) + app.Replace("_free", "").Length + 1);

						string combinedPlaypath = "";
						if (playpath.Contains(".f4v"))
							combinedPlaypath = "mp4:" + playpath;
						else combinedPlaypath = playpath.Replace(".flv", "");
						/*
						combinedPlaypath += "?ivw=" + ivw;
						combinedPlaypath += "&client=videoplayer&type=content&user=2880224004&session=2289727260&angebot=rtlnow&starttime=00:00:00:00&timetype=" + timetype;
						combinedPlaypath += "&fkcontent=" + fkcontent;
						combinedPlaypath += "&season=" + season;
						*/

						return new MPUrlSourceFilter.RtmpUrl(tcUrl, host, 1935)
						{
							App = app,
							SwfUrl = swfUrl,
							SwfVerify = true,
							PageUrl = new Uri(new Uri(baseUrl), "/p/").AbsoluteUri,
							PlayPath = combinedPlaypath
						}.ToString();

						/*rtmpLink.Add("conn", "S:" + para2);
						rtmpLink.Add("conn", "Z:");
						rtmpLink.Add("conn", "Z:");
						//rtmpLink.Add("conn", "S:" + secret);
						//rtmpLink.Add("conn", "S:" + onetime);
						rtmpLink.Add("conn", "S:");
						rtmpLink.Add("conn", "S:");
						rtmpLink.Add("conn", "S:" + para1);
						rtmpLink.Add("conn", "S:" + playpath.Substring(0, playpath.Length - 4));*/
						//}
					}
				}
			}
			return null;
		}
	}
}
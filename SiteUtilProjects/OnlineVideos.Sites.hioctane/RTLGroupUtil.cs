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

		[Category("OnlineVideosConfiguration"), Description("Secondary Url used for parsing Categories.")]
		protected string baseUrl2;

		public override int DiscoverDynamicCategories()
		{
			var result = base.DiscoverDynamicCategories();
			if (!string.IsNullOrEmpty(baseUrl2))
			{
				string data = GetWebData(baseUrl2, cookies: GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);
				if (!string.IsNullOrEmpty(data))
				{
					return ParseCategories(data);
				}
			}
			return result;
		}

		public override String GetVideoUrl(VideoInfo video)
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

						string timetype = root.SelectSingleNode("./timetype").InnerText;
						string fkcontent = root.SelectSingleNode("./fkcontent").InnerText;
						string season = root.SelectSingleNode("./season").InnerText;
						string ivw = Regex.Match(url, "ivw=(?<ivw>[^&]+)&").Groups["ivw"].Value;
						string rtmpeUrl = root.SelectSingleNode("./playlist/videoinfo/filename").InnerText;

						string host = rtmpeUrl.Substring(rtmpeUrl.IndexOf("//") + 2, rtmpeUrl.IndexOf("/", rtmpeUrl.IndexOf("//") + 2) - rtmpeUrl.IndexOf("//") - 2);
						string tcUrl = "rtmpe://" + host + ":1935" + "/" + app;
						string playpath = rtmpeUrl.Substring(rtmpeUrl.IndexOf(app.Replace("_free", "")) + app.Replace("_free", "").Length);

						string combinedPlaypath = "";
						if (playpath.Contains(".f4v"))
							combinedPlaypath = "mp4:" + playpath;
						else combinedPlaypath = playpath.Replace(".flv", "");
						
						combinedPlaypath += "?ivw=" + ivw;
						combinedPlaypath += "&client=videoplayer&type=content&user=2880224004&session=2289727260&angebot=rtlnow&starttime=00:00:00:00&timetype=" + timetype;
						combinedPlaypath += "&fkcontent=" + fkcontent;
						combinedPlaypath += "&season=" + season;
						
						return new MPUrlSourceFilter.RtmpUrl(tcUrl, host, 1935)
						{
							App = app,
							SwfUrl = swfUrl,
							SwfVerify = true,
							PageUrl = HttpUtility.HtmlDecode(video.VideoUrl),
							PlayPath = combinedPlaypath
						}.ToString();
					}
				}
			}
			return null;
		}
	}
}
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class ScrewattackUtil : GenericSiteUtil
    {
		public override String GetVideoUrl(VideoInfo video)
		{
			string url = getPlaylistUrl(video.VideoUrl);

			if (string.IsNullOrEmpty(url))
			{
				string dataPage = GetWebData(video.VideoUrl, cookies: GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);
				var ytMatch = Regex.Match(dataPage, "<iframe\\s.*?src=\"http://www.youtube.com/embed/(?<url>[^\"?]+)");
				if (ytMatch.Success)
				{
					video.PlaybackOptions = Hoster.Base.HosterFactory.GetHoster("Youtube").getPlaybackOptions(ytMatch.Groups["url"].Value);
					if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
					{
						return video.PlaybackOptions.First().Value;
					}
				}
				return null;
			}

			string newUrl = GetRedirectedUrl(url);
			var queryItems = HttpUtility.ParseQueryString(new Uri(newUrl).Query);
			string rssUrl = null;
			if (!string.IsNullOrEmpty(queryItems.Get("config")))
			{
				rssUrl = JObject.Parse(queryItems.Get("config"))["playlist"].Value<string>();
			}
			else if (!string.IsNullOrEmpty(queryItems.Get("file")))
			{
				rssUrl = queryItems.Get("file");
			}
			if (!string.IsNullOrEmpty(rssUrl))
			{
				string rss = GetWebData(rssUrl, cookies: GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);
				foreach (RssToolkit.Rss.RssItem rssItem in RssToolkit.Rss.RssDocument.Load(rss).Channel.Items)
				{
					VideoInfo aVideo = VideoInfo.FromRssItem(rssItem, regEx_FileUrl != null, new Predicate<string>(IsPossibleVideo));
					if (!string.IsNullOrEmpty(aVideo.VideoUrl))
					{
						video.PlaybackOptions = aVideo.PlaybackOptions;
						return aVideo.VideoUrl;
					}
				}
			}
			return null;
		}
    }
}
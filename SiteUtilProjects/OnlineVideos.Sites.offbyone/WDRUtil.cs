using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Web;
using System.Xml.Linq;
using HtmlAgilityPack;

namespace OnlineVideos.Sites
{
    public class WDRUtil : SiteUtilBase
    {
		[Category("OnlineVideosConfiguration")]
		protected string sendungenUrl;
		[Category("OnlineVideosConfiguration")]
		protected string searchUrl;
		[Category("OnlineVideosConfiguration")]
		protected string livestreamUrl;

		string nextVideoPageUrl;

        public override int DiscoverDynamicCategories()
        {
			Uri baseUri = new Uri(sendungenUrl);
			var doc = GetWebData<HtmlDocument>(sendungenUrl);
			var azUL = doc.DocumentNode.Descendants("ul").Where(ul => ul.GetAttributeValue("class", "") == "azNavi").FirstOrDefault();
			List<string> urls = new List<string>();
			foreach (var li in azUL.Elements("li"))
			{
				var a = li.Element("a");
				if (a != null)
					urls.Add(new Uri(baseUri, a.GetAttributeValue("href", "")).AbsoluteUri);
			}
			List<RssLink>[] sendungen = new List<RssLink>[urls.Count];
			// concurrent requesting and parsing of Catgeories
			ManualResetEvent[] threadWaitHandles = new ManualResetEvent[urls.Count];
			for (int i = 0; i < urls.Count; i++)
			{
				threadWaitHandles[i] = new System.Threading.ManualResetEvent(false);
				new Thread((o) =>
				{
					int o_i = (int)o;
					var o_doc = GetWebData<HtmlDocument>(urls[o_i]);
					if (o_i > 0) 
						WaitHandle.WaitAny(new ManualResetEvent[] { threadWaitHandles[o_i - 1] });
					sendungen[o_i] = FindCategories(o_doc, baseUri);

					threadWaitHandles[o_i].Set();
				}) { IsBackground = true }.Start(i);
			}
			WaitHandle.WaitAll(threadWaitHandles);

			Settings.Categories.Clear();
			Settings.Categories.Add(new RssLink() { Name = "Livestream", EstimatedVideoCount = 1, Url = livestreamUrl });
			foreach(var s in sendungen.SelectMany(s => s))
				Settings.Categories.Add(s);

            return Settings.Categories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
			if (category.Name == "Livestream")
				return new List<VideoInfo>() { new VideoInfo() { Title = "Livestream", VideoUrl = ((RssLink)category).Url } };
			else
			{
				var baseUri = new Uri(((RssLink)category).Url);
				return getVideos(GetWebData<HtmlDocument>(baseUri.AbsoluteUri), baseUri);
			}
        }

		public override List<VideoInfo> getNextPageVideos()
		{
			if (!string.IsNullOrEmpty(nextVideoPageUrl))
			{
				return getVideos(GetWebData<HtmlDocument>(nextVideoPageUrl), new Uri(nextVideoPageUrl));
			}
			else
				return null;
		}
		
		public override bool CanSearch { get { return true; } }

		public override List<ISearchResultItem> DoSearch(string query)
		{
			var searchResultData = GetWebDataFromPost(searchUrl, string.Format("q={0}", query));
			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(searchResultData);
			return getVideos(htmlDoc, new Uri(searchUrl)).ConvertAll(v => (ISearchResultItem)v);
		}

		public override string getUrl(VideoInfo video)
		{
			string playlistUrl = video.VideoUrl;
			if (video.Title != "Livestream")
			{
				var doc = GetWebData<HtmlDocument>(video.VideoUrl);
				var span = doc.DocumentNode.Descendants("span").Where(s => s.GetAttributeValue("class", "") == "videoLink").FirstOrDefault();
				if (span != null)
				{
					var a = span.Element("a");
					if (a != null)
					{
						var link = a.GetAttributeValue("href", "");
						if (!string.IsNullOrEmpty(link))
						{
							playlistUrl = new Uri(new Uri(video.VideoUrl), link).AbsoluteUri;
						}
					}
				}
			}
			if (!string.IsNullOrEmpty(playlistUrl))
				return GetStreamUrl(playlistUrl);
			return "";
		}

		string GetStreamUrl(string videoPageUrl)
		{
			var doc = GetWebData<HtmlDocument>(videoPageUrl);
			var flashParam = doc.DocumentNode.Descendants("param").Where(p => p.GetAttributeValue("name", "") == "flashvars").FirstOrDefault();
			if (flashParam != null)
			{
				string value = flashParam.GetAttributeValue("value", "");
				var f4mUri = HttpUtility.HtmlDecode(HttpUtility.UrlDecode(value)).Substring("dslSrc=".Length);
				f4mUri = f4mUri.Substring(0, f4mUri.IndexOf("&"));
				if (f4mUri.EndsWith("smil"))
					return GetStreamUrlFromSmil(f4mUri) + "&g=" + Utils.GetRandomLetters(12) + "&hdcore=3.3.0";
				else
					return f4mUri + "?g=" + Utils.GetRandomLetters(12) + "&hdcore=3.3.0";
			}
			return "";
		}

		string GetStreamUrlFromSmil(string smilUrl)
		{
			var doc = GetWebData<XDocument>(smilUrl);
			return doc.Descendants("meta").FirstOrDefault().Attribute("base").Value + doc.Descendants("video").FirstOrDefault().Attribute("src").Value;
		}

		List<VideoInfo> getVideos(HtmlDocument doc, Uri baseUri)
		{
			nextVideoPageUrl = null;
			HasNextPage = false;

			List<VideoInfo> result = new List<VideoInfo>();
			var UL = doc.DocumentNode.Descendants("ul").Where(ul => ul.GetAttributeValue("class", "") == "linkList pictured").FirstOrDefault();
			if (UL != null)
			{
				foreach (var li in UL.Elements("li"))
				{
					VideoInfo video = new VideoInfo();
					video.ImageUrl = new Uri(baseUri, li.Element("img").GetAttributeValue("src", "")).AbsoluteUri;
					var a = li.Element("a");
					video.VideoUrl = new Uri(baseUri, a.GetAttributeValue("href", "")).AbsoluteUri;
					video.Title = HttpUtility.HtmlDecode(a.Element("strong").InnerText.Trim());

					var indexOfAirdate = video.Title.IndexOf("Sendung vom ");
					if (indexOfAirdate >= 0)
						video.Airdate = video.Title.Substring(indexOfAirdate + "Sendung vom ".Length);

					var lengthSpan = a.Descendants("span").Where(s => s.GetAttributeValue("class", "") == "mediaLength").FirstOrDefault();
					if (lengthSpan != null)
						video.Length = lengthSpan.Elements("#text").First(t => !string.IsNullOrEmpty(t.InnerText.Trim())).InnerText.Trim();
					result.Add(video);
				}
			}

			var nextPageLink = doc.DocumentNode.Descendants("a").Where(a => a.InnerText == "nächste Seite").FirstOrDefault();
			if (nextPageLink != null)
			{
				HasNextPage = true;
				nextVideoPageUrl = new Uri(baseUri, nextPageLink.GetAttributeValue("href", "")).AbsoluteUri;
			}

			return result;
		}

		List<RssLink> FindCategories(HtmlDocument doc, Uri baseUri)
		{
			List<RssLink> result = new List<RssLink>();
			var itemsUL = doc.DocumentNode.Descendants("ul").Where(ul => ul.GetAttributeValue("class", "") == "linkList pictured").FirstOrDefault();
			foreach (var li in itemsUL.Elements("li"))
			{
				RssLink sendung = new RssLink();
				var a = li.Element("a");
				if (a.Element("strong").Element("strong") != null)
				{
					sendung.Name = HttpUtility.HtmlDecode(a.Element("strong").Element("strong").InnerText.Trim());
					sendung.Url = new Uri(baseUri, a.GetAttributeValue("href", "")).AbsoluteUri;
					var img = li.Element("img");
					if (img != null)
						sendung.Thumb = new Uri(baseUri, img.GetAttributeValue("src", "")).AbsoluteUri;
					result.Add(sendung);
				}
			}
			return result;
		}
    }
}

using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Xml.Linq;

namespace OnlineVideos.Sites
{
    public class WDRUtil : SiteUtilBase
    {
		protected const string sendungenUrl = "http://www1.wdr.de/mediathek/video/sendungen-a-z/index.html";
        protected const string searchUrl = "http://www1.wdr.de/suche/index.jsp";
        protected const string livestreamUrl = "http://wdr_fs_geo-lh.akamaihd.net/z/wdrfs_geogeblockt@112044/manifest.f4m?b=608-&";

        public override int DiscoverDynamicCategories()
        {
			Uri baseUri = new Uri(sendungenUrl);
			var doc = GetWebData<HtmlDocument>(sendungenUrl);

            var content = doc.DocumentNode.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("id", "") == "content");

			List<string> urls = new List<string>();
			foreach (var div in content.Descendants("div").First(d => d.GetAttributeValue("class","")=="labels").Element("div").Elements("div"))
			{
				var a = div.Element("a");
				if (a != null)
					urls.Add(new Uri(baseUri, a.GetAttributeValue("href", "")).AbsoluteUri);
			}

			var shows = new List<RssLink>();
            
            // add shows for current letter (A)
            FindCategories(doc, baseUri, shows);

			// concurrent requesting and parsing of all other shows (listed by letter on website)
			ManualResetEvent[] threadWaitHandles = new ManualResetEvent[urls.Count];
			for (int i = 0; i < urls.Count; i++)
			{
				threadWaitHandles[i] = new ManualResetEvent(false);
				new Thread((o) =>
				{
					int o_i = (int)o;
					var o_doc = GetWebData<HtmlDocument>(urls[o_i]);
					// wait for the previous one to be loaded so we are in alphabetical order
                    if (o_i > 0) 
						WaitHandle.WaitAny(new ManualResetEvent[] { threadWaitHandles[o_i - 1] });
					FindCategories(o_doc, baseUri, shows);

					threadWaitHandles[o_i].Set();
				}) { IsBackground = true }.Start(i);
			}
			WaitHandle.WaitAll(threadWaitHandles);

			Settings.Categories.Clear();
            Settings.Categories.Add(new Category { Name = "Livestream" });

            shows.ForEach(show => Settings.Categories.Add(show));
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            if (category.Name == "Livestream")
                return new List<VideoInfo>() { new VideoInfo() { Title = "Livestream", VideoUrl = "http://www1.wdr.de/mediathek/video/live/index.html" } };
            else
            {
                var baseUri = new Uri(((RssLink)category).Url);
                return getVideos(GetWebData<HtmlDocument>(baseUri.AbsoluteUri), baseUri);
            }
        }
		
		public override bool CanSearch { get { return true; } }

		public override List<SearchResultItem> Search(string query, string category = null)
		{
            var result = new List<SearchResultItem>();

            var htmlDoc = GetWebData<HtmlDocument>(searchUrl, string.Format("sort=date&q={0}&pt_video=on", HttpUtility.UrlEncode(query)));

            var baseUri = new Uri(searchUrl);

            foreach (var teaser in htmlDoc.DocumentNode.Descendants("div").Where(d => d.GetAttributeValue("class", "") == "teaser"))
            {
                result.Add(new VideoInfo
                {
                    VideoUrl = teaser.Descendants("a").First().GetAttributeValue("href", ""),
                    Title = teaser.Descendants("h3").First().InnerText,
                    Thumb = new Uri(baseUri, teaser.Descendants("img").First().GetAttributeValue("src", "")).AbsoluteUri,
                    Description = teaser.Descendants("p").First(p => p.GetAttributeValue("class", "") == "teasertext").InnerText,
                    Airdate = teaser.Descendants("p").First(p => p.GetAttributeValue("class", "") == "dachzeile").Element("a").LastChild.InnerText.Replace("vom", "").Trim()
                });
            }
            
            return result;
		}

        public override string GetVideoUrl(VideoInfo video)
        {
            var doc = GetWebData<HtmlDocument>(video.VideoUrl);
            var link = doc.DocumentNode.Descendants("a").FirstOrDefault(a => a.GetAttributeValue("data-extension", "").Contains("'url': 'http://"));
            if (link != null)
            {
                var json = JObject.Parse(link.GetAttributeValue("data-extension", ""));
                var playlistUrl = json["mediaObj"].Value<string>("url");
                var f = GetWebData(playlistUrl);
                var videoUrl = Regex.Match(f, "\"videoURL\":\"(?<url>.*?(f4m|smil))\"").Groups["url"].Value;
                return (videoUrl.EndsWith("smil") ? GetStreamUrlFromSmil(videoUrl) + "&g=" : videoUrl + "?g=") + Helpers.StringUtils.GetRandomLetters(12) + "&hdcore=3.9.0";
            }

            return null;
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
                    return GetStreamUrlFromSmil(f4mUri) + "&g=" + Helpers.StringUtils.GetRandomLetters(12) + "&hdcore=3.3.0";
				else
                    return f4mUri + "?g=" + Helpers.StringUtils.GetRandomLetters(12) + "&hdcore=3.3.0";
			}
			return "";
		}

		string GetStreamUrlFromSmil(string smilUrl)
		{
			var doc = GetWebData<XDocument>(smilUrl);
			return doc.Descendants("meta").FirstOrDefault().Attribute("base").Value + doc.Descendants("video").FirstOrDefault().Attribute("src").Value;
		}

		private static List<VideoInfo> getVideos(HtmlDocument doc, Uri baseUri)
		{
			List<VideoInfo> result = new List<VideoInfo>();

            var contentDiv = doc.DocumentNode.Descendants("div").FirstOrDefault(d => d.GetAttributeValue("id", "") == "content");
            var localWebPath = baseUri.AbsolutePath.Substring(0, baseUri.AbsolutePath.LastIndexOf("/") + 1);

            // when no links with data-extension attribute were found, use all that are below current path (daheim und unterwegs)
            var dataLinks = contentDiv.Descendants("a")
                .Where(a => a.GetAttributeValue("data-extension", "") != "" ||
                    (a.GetAttributeValue("href", "").StartsWith(localWebPath) && a.GetAttributeValue("href", "") != baseUri.AbsoluteUri && !a.GetAttributeValue("href", "").EndsWith("index.html") && !a.Descendants("strong").Any(s=>s.InnerText.Trim()=="mehr")))
                .GroupBy(a => a.GetAttributeValue("href", "")).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var dataLink in dataLinks)
            {
                HtmlNode parent = null;

                if (dataLink.Value.Count > 1)
                {
                    parent = CommonParent(dataLink.Value[0], dataLink.Value[1], 3);
                }

                if (parent == null)
                    parent = dataLink.Value.First().ParentNode;

                var link = new Uri(baseUri, dataLink.Key).AbsoluteUri;
                if (!link.StartsWith("http")) continue;

                var imgNode = parent.Descendants("img").FirstOrDefault(img => img.GetAttributeValue("src", "") != "");
                var thumb = (imgNode != null) ? new Uri(baseUri, imgNode.GetAttributeValue("src", "")).AbsoluteUri : null;

                var teaserParagraph = parent.Descendants("p").FirstOrDefault(p => p.GetAttributeValue("class", "") == "teasertext");
                var desc = (teaserParagraph != null) ? HttpUtility.HtmlDecode(teaserParagraph.FirstChild.InnerText).Trim().Trim('|').Trim() : null;

                var durationDiv = parent.Descendants("div").FirstOrDefault(p => p.GetAttributeValue("class", "") == "duration");
                var length = (durationDiv != null) ? durationDiv.InnerText.Trim() : null;

                var pi = parent.Descendants("p").FirstOrDefault(p => p.GetAttributeValue("class", "") == "programInfo");
                var airdate = (pi != null) ? pi.FirstChild.InnerText.Replace('|', ' ').Trim() : null;
                if (airdate == null)
                {
                    var airdateTextNode = parent.Descendants("#text").FirstOrDefault(t => Regex.IsMatch(t.InnerText.Trim(), @"\d{2}\.\d{2}\.\d{4}"));
                    if (airdateTextNode != null)
                        airdate = airdateTextNode.InnerText.Trim();
                }

                var header = parent.Descendants().FirstOrDefault(h => h.Name == "h3" || h.Name == "h4");
                var title = (header != null) ? HttpUtility.HtmlDecode(header.InnerText.Replace("Video:", "").Trim()) : null;
                if (title == null)
                {
                    if (parent.Name == "li")
                    {
                        title = parent.Descendants("span").FirstOrDefault().InnerText;
                    }
                }
                result.Add(new VideoInfo { Title = title, Description = desc, Thumb = thumb, VideoUrl = link, Airdate = airdate, Length = length });
            }

			return result;
		}

        private static HtmlNode CommonParent(HtmlNode n1, HtmlNode n2, int depth)
        {
            HtmlNode node = n1;
            while (depth > 0 && node != null)
            {
                var commonParent = node.Descendants().FirstOrDefault(child => child == n2);
                if (commonParent != null)
                    return node;
                node = node.ParentNode;
                depth--;
            }

            return null;
        }

        private static void FindCategories(HtmlDocument doc, Uri baseUri, List<RssLink> categories)
		{
            var content = doc.DocumentNode.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("id", "") == "content");
            foreach (var boxDiv in content.Descendants("div").Where(d => d.GetAttributeValue("class", "") == "box" && d.Descendants("ul").Any()))
			{
                foreach (var li in boxDiv.Descendants("ul").First().Elements("li"))
                {
                    var url = new Uri(baseUri, li.Element("a").GetAttributeValue("href", "")).AbsoluteUri;
                    var name = HttpUtility.HtmlDecode(li.Descendants("span").FirstOrDefault().InnerText.Trim());

                    var img = li.Descendants("img").FirstOrDefault();
                    var thumb = (img != null) ? new Uri(baseUri, img.GetAttributeValue("src", "")).AbsoluteUri : null;

                    lock (categories)
                    {
                        if (!categories.Any(c => c.Url == url))
                            categories.Add(new RssLink { Name = name, Thumb = thumb, Url = url });
                    }
                }
			}
		}
    }
}

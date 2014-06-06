using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{    
    public class DasErsteMediathekUtil : SiteUtilBase
    {        
        public enum VideoQuality { Low, Med, High, HD };

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Video Quality", TranslationFieldName="VideoQuality"), Description("Choose your preferred quality for the videos according to bandwidth.")]
        VideoQuality videoQuality = VideoQuality.High;

		public override int DiscoverDynamicCategories()
		{
			Settings.Categories.Add(new RssLink() { Name = "Sendungen A-Z", HasSubCategories = true, Url = "http://www.ardmediathek.de/tv/sendungen-a-z" });
			Settings.Categories.Add(new RssLink() { Name = "Sendung verpasst?", HasSubCategories = true, Url = "http://www.ardmediathek.de/tv/sendungVerpasst" });
			Settings.Categories.Add(new RssLink() { Name = "TV-Livestreams", Url = "http://www.ardmediathek.de/tv/live" });

			Uri baseUri = new Uri("http://www.ardmediathek.de/tv");
			var baseDoc = GetWebData<HtmlDocument>(baseUri.AbsoluteUri);
			foreach (var modHeadline in baseDoc.DocumentNode.Descendants("h2").Where(h2 => h2.GetAttributeValue("class", "") == "modHeadline"))
			{
				var moreLink = modHeadline.ParentNode.Descendants("a").FirstOrDefault(a => a.GetAttributeValue("class", "") == "more");
				if (moreLink != null)
				{
					Settings.Categories.Add(new RssLink() { Name = HttpUtility.HtmlDecode(modHeadline.InnerText), Url = new Uri(baseUri, moreLink.GetAttributeValue("href", "")).AbsoluteUri });
				}
				else if (!modHeadline.InnerText.ToLower().Contains("live"))
				{
					var cat = new RssLink() { Name = modHeadline.InnerText, Url = baseUri.AbsoluteUri, HasSubCategories = true, SubCategoriesDiscovered = true, SubCategories = new List<Category>() };
					GetSubcategoriesFromDiv(cat, modHeadline.ParentNode);
					Settings.Categories.Add(cat);
				}
			}
			Settings.DynamicCategoriesDiscovered = true;
			return Settings.Categories.Count;
		}

		void GetSubcategoriesFromDiv(RssLink parentCategory, HtmlNode mainDiv)
		{
			var myBaseUri = new Uri((parentCategory as RssLink).Url);
			foreach (var teaser in mainDiv.Descendants("div").Where(div => div.GetAttributeValue("class", "") == "teaser"))
			{
				RssLink subCategory = new RssLink() { ParentCategory = parentCategory };
				var img = teaser.Descendants("img").FirstOrDefault();
				if (img != null) subCategory.Thumb = new Uri(myBaseUri, JObject.Parse(HttpUtility.HtmlDecode(img.GetAttributeValue("data-ctrl-image", ""))).Value<string>("urlScheme").Replace("##width##", "256")).AbsoluteUri;
				var headline = teaser.Descendants("h4").FirstOrDefault(h4 => h4.GetAttributeValue("class", "") == "headline");
				if (headline != null) subCategory.Name = HttpUtility.HtmlDecode(headline.InnerText.Trim());
				var link = teaser.Descendants("a").FirstOrDefault();
				if (link != null) subCategory.Url = new Uri(myBaseUri, HttpUtility.HtmlDecode(link.GetAttributeValue("href", ""))).AbsoluteUri;

				var textWrapper = teaser.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "") == "textWrapper");
				if (textWrapper != null)
				{
					var subtitle = textWrapper.Descendants("p").FirstOrDefault(div => div.GetAttributeValue("class", "") == "subtitle");
					if (subtitle != null) subCategory.Description = subtitle.InnerText;
				}

				parentCategory.SubCategories.Add(subCategory);
			}
		}

		public override int DiscoverSubCategories(Category parentCategory)
		{
			parentCategory.SubCategories = new List<Category>();
			var myBaseUri = new Uri((parentCategory as RssLink).Url);
			var baseDoc = GetWebData<HtmlDocument>(myBaseUri.AbsoluteUri);

			if (parentCategory.Name == "Sendungen A-Z")
			{	
				foreach (HtmlNode entry in baseDoc.DocumentNode.Descendants("ul").FirstOrDefault(ul => ul.GetAttributeValue("class", "") == "subressorts raster").Elements("li"))
				{
					var a = entry.Descendants("a").FirstOrDefault();
					RssLink letter = new RssLink() { Name = a.InnerText.Trim(), ParentCategory = parentCategory, HasSubCategories = true, SubCategories = new List<Category>() };
					if (!string.IsNullOrEmpty(a.GetAttributeValue("href", "")))
					{
						letter.Url = new Uri(myBaseUri, a.GetAttributeValue("href", "")).AbsoluteUri;
						parentCategory.SubCategories.Add(letter);
					}
				}
				parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
			}
			else if (parentCategory.Name == "Sendung verpasst?")
			{
				var senderDiv = baseDoc.DocumentNode.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "").Contains("modSender"))
					.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "").Contains("controls"));
				foreach (HtmlNode entry in senderDiv.Descendants("div").Where(div => div.GetAttributeValue("class", "") == "entry" || div.GetAttributeValue("class", "") == "entry active"))
				{
					var a = entry.Descendants("a").FirstOrDefault();
					if (a != null && a.GetAttributeValue("href", "") != "#")
					{
						parentCategory.SubCategories.Add(new RssLink()
						{
							Name = a.InnerText.Trim(),
							Url = new Uri(myBaseUri, a.GetAttributeValue("href", "")).AbsoluteUri,
							ParentCategory = parentCategory,
							HasSubCategories = true,
							SubCategories = new List<Category>()
						});
					}
				}
				parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
			}
			else if (parentCategory.ParentCategory.Name == "Sendungen A-Z")
			{
				var mainDiv = baseDoc.DocumentNode.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "") == "elementWrapper")
					.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "") == "boxCon");
				GetSubcategoriesFromDiv(parentCategory as RssLink, mainDiv);
				parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
			}
			else if (parentCategory.ParentCategory.Name == "Sendung verpasst?")
			{
				var programmDiv = baseDoc.DocumentNode.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "").Contains("modProgramm"))
					.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "").Contains("controls"));
				foreach (HtmlNode entry in programmDiv.Descendants("div").Where(div => div.GetAttributeValue("class", "") == "entry" || div.GetAttributeValue("class", "") == "entry active").Skip(1))
				{
					var a = entry.Descendants("a").FirstOrDefault();
					var day = new RssLink() { Name = a.InnerText.Trim(), Url = new Uri(myBaseUri, HttpUtility.HtmlDecode(a.GetAttributeValue("href", ""))).AbsoluteUri };
					var j = HttpUtility.HtmlDecode(entry.GetAttributeValue("data-ctrl-programmloader-source", ""));
					if (!string.IsNullOrEmpty(j))
					{
						var f = JObject.Parse(j);
						day.Name += " " + HttpUtility.UrlDecode(f.Value<string>("pixValue")).Split('/')[1];
					}
					parentCategory.SubCategories.Add(day);
				}
			}
			
			return parentCategory.SubCategories.Count;
		}

        public override List<VideoInfo> getVideoList(Category category)
        {
			var myBaseUri = new Uri((category as RssLink).Url);
			var baseDoc = GetWebData<HtmlDocument>(myBaseUri.AbsoluteUri);

			var result = new List<VideoInfo>();
			if (category.Name == "TV-Livestreams")
			{
				var programmDiv = baseDoc.DocumentNode.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "").Contains("modSender"))
					.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "").Contains("controls"));
				foreach (HtmlNode entry in programmDiv.Descendants("div").Where(div => div.GetAttributeValue("class", "") == "entry" || div.GetAttributeValue("class", "") == "entry active").Skip(1))
				{
					var a = entry.Descendants("a").FirstOrDefault();
					if (a != null && a.GetAttributeValue("href","").Length > 1)
					{
						result.Add(new VideoInfo()
						{
							Title = a.InnerText.Trim(),
							VideoUrl = new Uri(myBaseUri, HttpUtility.HtmlDecode(a.GetAttributeValue("href", ""))).AbsoluteUri
						});
					}
				}

				result.Add(new VideoInfo()
				{
					Title = "Das Erste",
					VideoUrl = "http://daserste_live-lh.akamaihd.net/z/daserste_de@91204/manifest.f4m?hdcore=2.11.4&g=" + Utils.GetRandomLetters(12)
				});
			}
			else if (myBaseUri.AbsoluteUri.Contains("sendungVerpasst"))
			{
				var programmDiv = baseDoc.DocumentNode.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "").Contains("modProgramm"));
				foreach (var boxDiv in programmDiv.Descendants("div").Where(div => div.GetAttributeValue("class", "") == "box"))
				{
					foreach (var entryDiv in boxDiv.Elements("div").FirstOrDefault().Elements("div").Where(div => div.GetAttributeValue("class", "") == "entry"))
					{
						var start = entryDiv.Descendants("span").FirstOrDefault(span => span.GetAttributeValue("class", "") == "date").InnerText;
						var title = entryDiv.Descendants("span").FirstOrDefault(span => span.GetAttributeValue("class", "") == "titel").InnerText;
						foreach (var teaser in entryDiv.Descendants("div").Where(div => div.GetAttributeValue("class", "") == "teaser"))
						{
							var video = new VideoInfo();
							var img = teaser.Descendants("img").FirstOrDefault();
							if (img != null) video.ImageUrl = new Uri(myBaseUri, JObject.Parse(HttpUtility.HtmlDecode(img.GetAttributeValue("data-ctrl-image", ""))).Value<string>("urlScheme").Replace("##width##", "256")).AbsoluteUri;

							var textWrapper = teaser.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "") == "textWrapper");
							if (textWrapper != null)
							{
								video.VideoUrl = new Uri(myBaseUri, HttpUtility.HtmlDecode(textWrapper.Element("a").GetAttributeValue("href", ""))).AbsoluteUri;
								video.Title = textWrapper.Descendants("h4").FirstOrDefault().InnerText.Trim();
								if (video.Title != title) video.Title = title + " - " + video.Title;
								video.Length = textWrapper.Descendants("p").FirstOrDefault(div => div.GetAttributeValue("class", "") == "subtitle").InnerText.Split('|')[0].Trim();
								video.Airdate = start + " Uhr";
								result.Add(video);
							}
						}
					}
				}
			}
			else
			{
				var mainDiv = baseDoc.DocumentNode.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "").Contains("modMini")).ParentNode;
				foreach (var teaser in mainDiv.Descendants("div").Where(div => div.GetAttributeValue("class", "") == "teaser"))
				{
					var link = teaser.Descendants("a").FirstOrDefault();
					if (link != null)
					{
						var video = new VideoInfo();
						video.VideoUrl = new Uri(myBaseUri, HttpUtility.HtmlDecode(link.GetAttributeValue("href", ""))).AbsoluteUri;

						var img = teaser.Descendants("img").FirstOrDefault();
						if (img != null) video.ImageUrl = new Uri(myBaseUri, JObject.Parse(HttpUtility.HtmlDecode(img.GetAttributeValue("data-ctrl-image", ""))).Value<string>("urlScheme").Replace("##width##", "256")).AbsoluteUri;

						var headline = teaser.Descendants("h4").FirstOrDefault(h4 => h4.GetAttributeValue("class", "") == "headline");
						if (headline != null) video.Title = HttpUtility.HtmlDecode(headline.InnerText.Trim());

						var textWrapper = teaser.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("class", "") == "textWrapper");
						if (textWrapper != null)
						{
							var dachzeile = HttpUtility.HtmlDecode(textWrapper.Descendants("p").FirstOrDefault(div => div.GetAttributeValue("class", "") == "dachzeile").InnerText);
							var subtitle = textWrapper.Descendants("p").FirstOrDefault(div => div.GetAttributeValue("class", "") == "subtitle").ChildNodes[0].InnerText;
							if (subtitle.Contains('|'))
							{
								video.Description = dachzeile;
								foreach (var subtitleSplit in subtitle.Split('|'))
								{
									if (subtitleSplit.Contains("min"))
										video.Length = subtitleSplit.Trim();
									else if (subtitleSplit.Count(c => c == '.') == 2)
										video.Airdate = subtitleSplit.Trim();
								}
							}
							else
							{
								video.Length = subtitle;
								video.Airdate = dachzeile;
							}
						}
						result.Add(video);
					}
				}
				// todo : paging
			}
			return result;
        }

		public override bool CanSearch { get { return true; } }
		public override List<ISearchResultItem> DoSearch(string query)
		{
			var doc = GetWebData<HtmlDocument>(string.Format("http://www.ardmediathek.de/tv/suche?searchText={0}", HttpUtility.UrlEncode(query)));
			
			return base.DoSearch(query);
		}

		public override String getUrl(VideoInfo video)
		{
			if (video.VideoUrl.StartsWith("http://daserste_live-lh.akamaihd.net"))
				return video.VideoUrl;
			else
			{
				var baseDoc = GetWebData<HtmlDocument>(video.VideoUrl);
				var mediaDiv = baseDoc.DocumentNode.Descendants("div").FirstOrDefault(div => div.GetAttributeValue("data-ctrl-player", "") != "");
				if (mediaDiv != null)
				{
					var configUrl = new Uri(new Uri(video.VideoUrl), JObject.Parse(HttpUtility.HtmlDecode(mediaDiv.GetAttributeValue("data-ctrl-player", ""))).Value<string>("mcUrl")).AbsoluteUri;
					var mediaJson = GetWebData<JObject>(configUrl);
					video.PlaybackOptions = new Dictionary<string, string>();
					foreach (var media in mediaJson["_mediaArray"].SelectMany(m => m["_mediaStreamArray"]))
					{
						var quali = ((JValue)media["_quality"]).Type == JTokenType.Integer ? ((VideoQuality)media.Value<int>("_quality")).ToString() : "HD";
						if (!media.Value<bool>("flashUrl"))
						{
							var url = media["_stream"] is JArray ? media["_stream"][0].Value<string>() : media.Value<string>("_stream");
							if (!url.EndsWith(".smil"))
							{
								if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
								{
									if (url.EndsWith("f4m"))
										url += "?g=" + Utils.GetRandomLetters(12) + "&hdcore=3.3.0";
									video.PlaybackOptions[quali] = url;
								}
							}
						}
						else
						{
							if (mediaJson.Value<bool>("_isLive"))
							{
								var server = media.Value<string>("_server");
								var stream = media.Value<string>("_stream");
								string url = "";
								if (string.IsNullOrEmpty(stream))
								{
									string guessedStream = server.Substring(server.LastIndexOf('/') + 1);
									url = new MPUrlSourceFilter.RtmpUrl(server) { Live = true, LiveStream = true, Subscribe = guessedStream, PageUrl = video.VideoUrl }.ToString();
								}
								else if (stream.Contains('?'))
								{
									var tcUrl = server.TrimEnd('/') + stream.Substring(stream.IndexOf('?'));
									var app = new Uri(server).AbsolutePath.Trim('/') + stream.Substring(stream.IndexOf('?'));
									var playPath = stream;
									url = new MPUrlSourceFilter.RtmpUrl(tcUrl) { App = app, PlayPath = playPath, Live = true, PageUrl = video.VideoUrl, Subscribe = playPath }.ToString();
								}
								else
								{
									url = new MPUrlSourceFilter.RtmpUrl(server + "/" + stream) { Live = true, LiveStream = true, Subscribe = stream, PageUrl = video.VideoUrl }.ToString();
								}
								if (!video.PlaybackOptions.ContainsKey(quali)) video.PlaybackOptions[quali] = url;
							}
						}
					}
				}
			}
			return video.PlaybackOptions.Select(p => p.Value).LastOrDefault();
		}

    }
}


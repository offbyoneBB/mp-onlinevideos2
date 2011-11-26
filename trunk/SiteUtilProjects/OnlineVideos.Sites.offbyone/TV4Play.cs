using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace OnlineVideos.Sites
{
	public class TV4Play : GenericSiteUtil
	{
		protected string tv4VideolistDividingRegEx = @"<div\sclass=""module-center-wrapper"">.*?<h2>.*?{0}.*?</h2>(?<data>.*?)</section>";
		
		[Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse a html page for dynamic categories. Group names: 'url', 'title', 'thumb', 'description'. Will be used on the web pages resulting from the links from the dynamicCategoriesRegEx. Will not be used if not set.")]
		protected string dynamicSubCategoriesRegEx2;

		Regex regEx_dynamicSubCategories2;

		public override void Initialize(SiteSettings siteSettings)
		{
			base.Initialize(siteSettings);

			if (!string.IsNullOrEmpty(dynamicSubCategoriesRegEx2)) regEx_dynamicSubCategories2 = new Regex(dynamicSubCategoriesRegEx2, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
		}

		public override int DiscoverSubCategories(Category parentCategory)
		{
			string data = GetWebData((parentCategory as RssLink).Url);
			parentCategory.SubCategories = new List<Category>();
			if (!string.IsNullOrEmpty(data))
			{
				if (parentCategory.ParentCategory == null)
				{
					string jsonUrl = "http://www.tv4play.se/program_format_searches.json?ids=";
					bool found = false;
					Match m = regEx_dynamicSubCategories.Match(data);
					if (m.Success)
					{
						found = true;
						jsonUrl += System.Web.HttpUtility.UrlDecode(m.Groups["ids"].Value) + "&rows=100";
					}
					if (found)
					{
						Newtonsoft.Json.Linq.JObject json = GetWebData<Newtonsoft.Json.Linq.JObject>(jsonUrl);
						if (json != null)
						{
							foreach (var result in json["results"])
							{
								RssLink cat = new RssLink();
								cat.Url = result.Value<string>("href");
								if (!Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(baseUrl), cat.Url).AbsoluteUri;
								cat.Name = result.Value<string>("name");
								cat.Thumb = result.Value<string>("smallformatimage");
								cat.Description = result.Value<string>("text");
								cat.HasSubCategories = true;
								parentCategory.SubCategories.Add(cat);
								cat.ParentCategory = parentCategory;
							}
						}
					}
					else
					{
						Match m2 = regEx_dynamicSubCategories2.Match(data);
						while (m2.Success)
						{
							RssLink cat = new RssLink();
							cat.Url = m2.Groups["url"].Value;
							if (!Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(baseUrl), cat.Url).AbsoluteUri;
							cat.Name = HttpUtility.HtmlDecode(m2.Groups["title"].Value.Trim().Replace('\n', ' '));
							cat.Thumb = m2.Groups["thumb"].Value;
							cat.Description = HttpUtility.HtmlDecode(m2.Groups["desc"].Value.Trim());
							cat.HasSubCategories = true;
							parentCategory.SubCategories.Add(cat);
							cat.ParentCategory = parentCategory;
							m2 = m2.NextMatch();
						}
					}
				}
				else
				{
					Match m3 = Regex.Match(data, @"<h2><span>(?<title>[^<]+)</span></h2>");
					while (m3.Success)
					{
						parentCategory.SubCategories.Add(new RssLink() 
						{ 
							Name = m3.Groups["title"].Value, 
							ParentCategory = parentCategory, 
							Url = (parentCategory as RssLink).Url 
						});
						m3 = m3.NextMatch();
					}
				}
			}

			parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
			return parentCategory.SubCategories.Count;
		}

		int currentStart = 0;
		
		public override List<VideoInfo> getNextPageVideos()
		{
			return getVideoList(new RssLink() { Url = nextPageUrl });
		}

		public override List<VideoInfo> getVideoList(Category category)
		{
			List<VideoInfo> videos = new List<VideoInfo>();
			string data = "";
			if (category.ParentCategory != null)
			{
				currentStart = 0;
				data = GetWebData((category.ParentCategory as RssLink).Url);
				data = Regex.Match(data, string.Format(tv4VideolistDividingRegEx, category.Name.Replace(" ", "\\s")), RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture).Groups["data"].Value;
			}
			else
			{
				// called from getNextPageVideos()
				data = GetWebData((category as RssLink).Url);
			}
			Match m = regEx_VideoList.Match(data);
			while (m.Success)
			{
				VideoInfo video = new VideoInfo();
				video.Title = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim().Replace('\n', ' '));
				video.Airdate = HttpUtility.HtmlDecode(m.Groups["date"].Value.Trim());
				video.ImageUrl = m.Groups["thumb"].Value;
				video.VideoUrl = m.Groups["url"].Value;
				videos.Add(video);
				m = m.NextMatch();
			}
			nextPageAvailable = false;
			int currentMaxVideos = -1;
			int.TryParse(Regex.Match(data, @"<p\sclass=""info"">Visar\s\d+\sav\s(?<max>\d+)</p>", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture).Groups["max"].Value, out currentMaxVideos);
			if (currentMaxVideos > 0) (category as RssLink).EstimatedVideoCount = (uint)currentMaxVideos;
			m = regEx_NextPage.Match(data);
			if (m.Success)
			{
				nextPageUrl = HttpUtility.HtmlDecode(m.Groups["url"].Value);
				if (!Uri.IsWellFormedUriString(nextPageUrl, System.UriKind.Absolute)) nextPageUrl = new Uri(new Uri(baseUrl), nextPageUrl).AbsoluteUri;

				int inc = 12;
				string incString = Regex.Match(nextPageUrl, @"increment=(\d+)").Groups[1].Value;
				if (incString != "")
				{
					int.TryParse(incString, out inc);
					nextPageUrl = Regex.Replace(nextPageUrl, @"&increment=(\d+)", "");
				}

				string rowsString = Regex.Match(nextPageUrl, @"rows=(\d+)").Groups[1].Value;
				int rows = 12;
				if (rowsString != "") int.TryParse(rowsString, out rows);

				nextPageUrl = Regex.Replace(nextPageUrl, @"rows=(\d+)", "rows=" + inc.ToString());

				currentStart += inc;

				if (currentStart < currentMaxVideos)
				{
					nextPageUrl += "&start=" + currentStart.ToString();

					nextPageAvailable = true;
				}
			}
			return videos;
		}

		public override string getUrl(VideoInfo video)
		{
			string result = string.Empty;
			video.PlaybackOptions = new Dictionary<string, string>();
			XmlDocument xDoc = GetWebData<XmlDocument>(string.Format("http://anytime.tv4.se/webtv/metafileFlash.smil?p={0}&bw=1000&emulate=true&sl=true", video.VideoUrl));
			var errorElements = xDoc.SelectNodes("//meta[@name = 'error']");
			if (errorElements != null && errorElements.Count > 0)
			{
				throw new OnlineVideosException(((XmlElement)errorElements[0]).GetAttribute("content"));
			}
			else
			{
				string host = xDoc.SelectSingleNode("//meta[@base]/@base").Value;
				foreach (XmlElement videoElem in xDoc.SelectNodes("//video[@src]"))
				{
					result = new MPUrlSourceFilter.RtmpUrl(host) { PlayPath = videoElem.GetAttribute("src") }.ToString();
					video.PlaybackOptions.Add(string.Format("{0} kbps", int.Parse(videoElem.GetAttribute("system-bitrate")) / 1000), result);
				}
				return result;
			}
		}
	}
}

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
	public class HeiseVideoUtil : SiteUtilBase
	{
		const string search_url = "http://www.heise.de/video/suche/?q={0}&rm=search";

		const string archiv_json_url = "http://www.heise.de/video/includes/cttv_archiv_json.pl";
		const string years_list_url_parameter = "?rm=liste_rubrik_zu_jahren";
		const string categories_list_url_parameter = "?rm=liste_jahr_zu_rubriken&jahr={0}";
		const string video_list_url_parameter = "?jahr={0}&rubrik={1}";

		readonly static Dictionary<string, string> category_names = new Dictionary<string, string>() 
		{ 
			{"2523","Alle Rubriken"},	
			{"2524","Vorsicht, Kunde!"},
			{"2525","Drangeblieben"},
			{"2526","Schnurer hilft"},
			{"2527","News"},
			{"2528","Computer ABC"},
			{"2529","Prüfstand"},
			{"2530","Specials"}
		};

		string nextPageUrl = "";
		string currentVideosTitle = "";

		public override int DiscoverDynamicCategories()
		{
			Settings.Categories = new BindingList<Category>();
			Settings.Categories.Add(new RssLink() { Name = "Neuste Videos", Url = "http://www.heise.de/video/?teaser=neuste&hajax=1" });
			var archiv = new Category() { Name = "Archiv", HasSubCategories = true };
			archiv.SubCategories = new List<Category>();
			foreach (var year in JsonConvert.DeserializeObject<JArray>(GetWebData(archiv_json_url + years_list_url_parameter).Trim(new char[] { '(', ')' })))
			{
				archiv.SubCategories.Add(new Category() { Name = year.Value<string>(), HasSubCategories = true, ParentCategory = archiv });
			}
			archiv.SubCategoriesDiscovered = true;
			Settings.Categories.Add(archiv);
			Settings.DynamicCategoriesDiscovered = true;
			return Settings.Categories.Count;
		}

		public override int DiscoverSubCategories(Category parentCategory)
		{
			parentCategory.SubCategories = new List<Category>();
			foreach (var categoryId in JsonConvert.DeserializeObject<JArray>(GetWebData(archiv_json_url + string.Format(categories_list_url_parameter, parentCategory.Name)).Trim(new char[] { '(', ')' })))
			{
				parentCategory.SubCategories.Add(new RssLink() { Name = category_names[categoryId.Value<string>()], Url = string.Format(archiv_json_url + video_list_url_parameter, parentCategory.Name, categoryId.Value<string>()), ParentCategory = parentCategory });
			}
			parentCategory.SubCategoriesDiscovered = true;
			return parentCategory.SubCategories.Count;
		}

		public override List<VideoInfo> GetVideos(Category category)
		{
			HasNextPage = false;
			currentVideosTitle = null;

			List<VideoInfo> result = new List<VideoInfo>();

			if ((category as RssLink).Url.StartsWith(archiv_json_url))
			{
				var json = JsonConvert.DeserializeObject<JArray>(GetWebData((category as RssLink).Url).Trim(new char[] { '(', ')' }));
				foreach (var item in json)
				{
					result.Add(new VideoInfo()
					{
						Title = item.Value<string>("titel"),
						Description = item.Value<string>("anrisstext"),
						Airdate = item.Value<string>("datum"),
						ImageUrl = "http://www.heise.de" + item["anrissbild"].Value<string>("src"),
						VideoUrl = "http://www.heise.de" + item.Value<string>("url")
					});
				}
			}
			else
			{
				var json = JsonConvert.DeserializeObject<JObject>(GetWebData((category as RssLink).Url));
				var htmlFrag = json["actions"][1].Value<string>("html");
				var html = new HtmlDocument();
				html.LoadHtml(htmlFrag);
				GetLatestVideos(ref result, html.DocumentNode);
			}
			return result;
		}

		public override List<VideoInfo> GetNextPageVideos()
		{
			HasNextPage = false;
			if (nextPageUrl.Contains("suche"))
			{
				var result = new List<ISearchResultItem>();
				GetSearchResultVideos(ref result, nextPageUrl);
				return result.ConvertAll<VideoInfo>(v => (VideoInfo)v).ToList();
			}
			else if (nextPageUrl.Contains("thema"))
			{
				var result = new List<VideoInfo>();
				string data = GetWebData(nextPageUrl);
				var html = new HtmlDocument();
				html.LoadHtml(data);
				var section = html.DocumentNode.Descendants("section").FirstOrDefault(s => s.GetAttributeValue("id", "") == "content");
				GetLatestVideos(ref result, section.Descendants("ul").First());
				var nextPageA = html.DocumentNode.Descendants("a").FirstOrDefault(a => a.InnerText == "nächste");
				if (nextPageA != null)
				{
					HasNextPage = true;
					nextPageUrl = "http://www.heise.de" + nextPageA.GetAttributeValue("href", "");
				}
				return result;
			}
			else
			{
				List<VideoInfo> result = new List<VideoInfo>();
				var json = JsonConvert.DeserializeObject<JObject>(GetWebData(nextPageUrl));
				var htmlFrag = json["actions"][1].Value<string>("html");
				var html = new HtmlDocument();
				html.LoadHtml(htmlFrag);
				GetLatestVideos(ref result, html.DocumentNode);
				return result;
			}
		}

		public override string GetVideoUrl(VideoInfo video)
		{
			var match = Regex.Match(GetWebData(video.VideoUrl), @"json_url:\s*""(?<url>http://www.heise.de/videout/[^""]+)""");
			if (match.Success)
			{
				string json_url = match.Groups["url"].Value;
				var json = JsonConvert.DeserializeObject<JObject>(GetWebData(json_url));
				return json["formats"]["mp4"].First.First.Value<string>("url");
			}
			else
				throw new OnlineVideosException("Regex for Json Url outdated!");
		}

		public override bool CanSearch { get { return true; } }

        public override List<ISearchResultItem> Search(string query, string category = null)
		{
			currentVideosTitle = null;
			var result = new List<ISearchResultItem>();
			GetSearchResultVideos(ref result, string.Format(search_url, query));
			return result;
		}

		public override List<ContextMenuEntry> GetContextMenuEntries(Category selectedCategory, VideoInfo selectedItem)
		{
			var result = new List<ContextMenuEntry>();
			if (selectedItem != null && selectedItem.Other is SerializableDictionary<string, string>)
			{
				ContextMenuEntry mehrZumThema = new ContextMenuEntry() { DisplayText = "Mehr zum Thema ...", Action = ContextMenuEntry.UIAction.ShowList };
				foreach (var item in (selectedItem.Other as SerializableDictionary<string, string>))
				{
					mehrZumThema.SubEntries.Add(new ContextMenuEntry() { DisplayText = item.Key, Action = ContextMenuEntry.UIAction.Execute, Other = item.Value });
				}
				result.Add(mehrZumThema);
			}
			return result;
		}

		public override ContextMenuExecutionResult ExecuteContextMenuEntry(Category selectedCategory, VideoInfo selectedItem, ContextMenuEntry choice)
		{
			HasNextPage = false;
			currentVideosTitle = "Mehr zum Thema [" + choice.DisplayText + "]";

			var result = new List<VideoInfo>();
			string data = GetWebData(choice.Other as string);
			var html = new HtmlDocument();
			html.LoadHtml(data);
			var section = html.DocumentNode.Descendants("section").FirstOrDefault(s => s.GetAttributeValue("id", "") == "content");
			GetLatestVideos(ref result, section.Descendants("ul").First());

			var nextPageA = html.DocumentNode.Descendants("a").FirstOrDefault(a => a.InnerText == "nächste");
			if (nextPageA != null)
			{
				HasNextPage = true;
				nextPageUrl = "http://www.heise.de" + nextPageA.GetAttributeValue("href", "");
			}

			return new ContextMenuExecutionResult() { ResultItems = result.ConvertAll<ISearchResultItem>(v => (ISearchResultItem)v) };
		}

		public override string GetCurrentVideosTitle()
		{
			return currentVideosTitle;
		}

		void GetSearchResultVideos(ref List<ISearchResultItem> result, string url)
		{
			HasNextPage = false;

			string data = GetWebData(url);
			var html = new HtmlDocument();
			html.LoadHtml(data);
			var ol = html.DocumentNode.Descendants("ol").FirstOrDefault();
			if (ol != null)
			{
				foreach (var li in ol.Elements("li"))
				{
					result.Add(new VideoInfo()
					{
						Title = li.Element("h5").Element("a").InnerText,
						VideoUrl = li.Element("h5").Element("a").GetAttributeValue("href", ""),
						ImageUrl = "http://www.heise.de" + li.Descendants("img").First().GetAttributeValue("src", ""),
						Description = li.Descendants("p").First().InnerText.Trim(),
						Airdate = li.Descendants("span").First(s => s.GetAttributeValue("class", "") == "date").InnerText.Replace("&ndash;", "").Trim()
					});
				}
			}
			var nextPageA = html.DocumentNode.Descendants("a").FirstOrDefault(a => a.InnerText == "&raquo;");
			if (nextPageA != null)
			{
				HasNextPage = true;
				nextPageUrl = "http://www.heise.de" + nextPageA.GetAttributeValue("href", "");
			}
		}

		void GetLatestVideos(ref List<VideoInfo> result, HtmlNode li_container)
		{
			foreach (var li in li_container.Elements("li"))
			{
				if (li.GetAttributeValue("class", "").Contains("more_reiter"))
				{
					HasNextPage = true;
					nextPageUrl = "http://www.heise.de/video/" + li.Element("a").GetAttributeValue("href", "") + "&hajax=1";
				}
				else
				{
					SerializableDictionary<string, string> tags = null;
					if (li.Element("ul") != null)
					{
						tags = new SerializableDictionary<string, string>();
						foreach (var tag_li in li.Element("ul").Elements("li"))
						{
							tags.Add(tag_li.Element("a").InnerText, "http://www.heise.de" + tag_li.Element("a").GetAttributeValue("href", ""));
						}
					}
					var a = li.Descendants("h3").First().Element("a");
					result.Add(new VideoInfo()
					{
						Title = a.InnerText,
						VideoUrl = "http://www.heise.de" + a.GetAttributeValue("href", ""),
						ImageUrl = "http://www.heise.de" + li.Descendants("img").First().GetAttributeValue("src", ""),
						Description = li.Descendants("p").First().FirstChild.InnerText.Trim(),
						Other = tags
					});
				}
			}
		}
	}
}

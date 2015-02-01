using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;

using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class TVOUtil : CanadaBrightCoveUtilBase
    {
        // following were found by looking at AMF POST requests using Firebug/Flashbug
        protected override string hashValue { get { return @"82c0aa70e540000aa934812f3573fd475d131a63"; } }
        protected override string playerId { get { return @"756015080001"; } }
        protected override string publisherId { get { return @"18140038001"; } }

        protected virtual string baseUrlPrefix { get { return @"http://ww3.tvo.org"; } }
        protected virtual string mainCategoriesUrl { get { return @"http://tvo.org/video"; } }
        protected virtual string quickTab { get { return @"qt_3"; } }
        
        protected static string mainCategoriesUrlFormat = @"{0}/views/ajax?view_name={1}&view_display_id={2}";
        private static string videoListUrl = @"{0}/views/ajax?field_web_master_series_nid_1={1}&view_name=video_landing_page&view_display_id={2}";
        
        private static Regex nidRegex = new Regex(@"(?<nid>[\d]+)\-wrapper$", RegexOptions.Compiled);
        private static Regex rtmpUrlRegex = new Regex(@"(?<rtmp>rtmpe?)://(?<host>[^/]+)/(?<app>[^&]*)&(?<leftover>.*)", RegexOptions.Compiled);
        private static Regex nextPageLinkRegex = new Regex(@"(/video|/views/ajax)\?page=(?<page>\d+)", RegexOptions.Compiled);
        private static Regex videoIdRegex = new Regex(@"<param name=""@videoPlayer"" value=""(?<videoId>[^""]*)""", RegexOptions.Compiled);
        private static Regex jsonRegex = new Regex(@"jQuery.extend\(Drupal\.settings,\s+(?<json>.*?)\);",
                                                   RegexOptions.Compiled);
        
        private Category currentCategory = null;

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();
            
            string webData = GetWebData(mainCategoriesUrl);
            if (!string.IsNullOrEmpty(webData))
            {
                Match jsonMatch = jsonRegex.Match(webData);
                if (jsonMatch.Success)
                {
                    JObject json = JObject.Parse(jsonMatch.Groups["json"].Value);
                    JArray tabs = (JArray) json["quicktabs"][quickTab]["tabs"];
                    foreach (JToken tab in tabs)
                    {
                        Settings.Categories.Add(
                            new RssLink() {
                                Name = (string) tab["title"],
                                HasSubCategories = true,
                                Url = string.Format(mainCategoriesUrlFormat, baseUrlPrefix, (string) tab["vid"], (string) tab["display"])
                            });
                    }
                }
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }
        
        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();

            string url = ((RssLink) parentCategory).Url;
            string viewDisplayId = HttpUtility.ParseQueryString(new Uri(url).Query)["view_display_id"];
            
            // retrieve contents of URL using JSON
            JObject json = GetWebData<JObject>(url);
            if (json != null)
            {
                string display = json.Value<string>("display");
                
                HtmlDocument html = new HtmlDocument();
                html.LoadHtml(display);
                
                foreach (HtmlNode div in html.DocumentNode.SelectNodes("//div[@class='form-item']"))
                {
                    HtmlNode label = div.SelectSingleNode("./label");
                    
                    RssLink cat = new RssLink();
                    cat.ParentCategory = parentCategory;
                    cat.Name = label.InnerText.Replace("&lt;Any&gt;", "All");

                    string id = div.Attributes["id"].Value;
                    Match nidMatch = nidRegex.Match(id);
                    
                    if (nidMatch.Success)
                    {
                        cat.Url = String.Format(videoListUrl, baseUrlPrefix, nidMatch.Groups["nid"], viewDisplayId);
                    }
                    else if (id.EndsWith("All-wrapper"))
                    {
                        cat.Url = String.Format(videoListUrl, baseUrlPrefix, "All", viewDisplayId);
                    }
                    cat.HasSubCategories = false;
                    Log.Debug("text: {0}, id: {1}", cat.Name, cat.Url);

                    parentCategory.SubCategories.Add(cat);
                }
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }
        
        public override List<VideoInfo> GetVideos(Category category)
        {
            return getVideoListForSinglePage(category, ((RssLink) category).Url);
        }

        private List<VideoInfo> getVideoListForSinglePage(Category category, string url)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            nextPageUrl = "";
            currentCategory = category;

            // retrieve contents of URL using JSON
            JObject json = GetWebData<JObject>(url);
            if (json != null)
            {
                string display = json.Value<string>("display");
                
                HtmlDocument html = new HtmlDocument();
                html.LoadHtml(display);

                foreach (HtmlNode td in html.DocumentNode.SelectNodes("//td"))
                {
                    HtmlNode anchor = td.SelectSingleNode("./span[@class='views-field-field-thumbnail-url-value']//a");                    
                    HtmlNode lengthNode = td.SelectSingleNode("./span[@class='views-field-field-length-value']");
                    HtmlNode releaseNode = td.SelectSingleNode("./span[@class='views-field-field-release-date-value']");
                    HtmlNode titleNode = td.SelectSingleNode(".//h5");
                    HtmlNode descriptionNode = td.SelectSingleNode(".//span[@class='views-field-field-description-value']");
                    
                    if (anchor != null)
                    {
                        result.Add(new VideoInfo() {
                                       VideoUrl = anchor.Attributes["href"].Value,
                                       ImageUrl = anchor.SelectSingleNode("./img").Attributes["src"].Value,
                                       Length = lengthNode.SelectSingleNode(".//span[@class='field-length-value']").InnerText,
                                       Airdate = releaseNode.SelectSingleNode(".//span[@class='date-display-single']/span[@class='date-display-single']").InnerText,
                                       Title = titleNode.InnerText,
                                       Description = descriptionNode.SelectSingleNode("./span[@class='field-content']").InnerText
                                   });
                    }
                }
                
                HtmlNode nextPageNode = html.DocumentNode.SelectSingleNode("//li[@class='pager-next']");
                if (nextPageNode != null)
                {
                    string link = nextPageNode.SelectSingleNode("./a").Attributes["href"].Value;
                    Match nextPageLinkMatch = nextPageLinkRegex.Match(link);
                    if (nextPageLinkMatch.Success)
                    {
                        string page = nextPageLinkMatch.Groups["page"].Value;
                        nextPageUrl = String.Format(@"{0}&page={1}", ((RssLink) category).Url, page);
                    }
                }
            }

            return result;
        }
        
        public override bool HasNextPage {
            get { return !string.IsNullOrEmpty(nextPageUrl); }
        }
        
        public override List<VideoInfo> GetNextPageVideos()
        {
            return getVideoListForSinglePage(currentCategory, nextPageUrl);
        }
        
        public override string getBrightCoveVideoIdForViewerExperienceRequest(string videoUrl)
        {
            string result = string.Empty;
            string webData = GetWebData(videoUrl);
            if (!string.IsNullOrEmpty(webData))
            {
                Match videoIdMatch = videoIdRegex.Match(webData);
                if (videoIdMatch.Success)
                {
                    result = videoIdMatch.Groups["videoId"].Value;
                }
            }
            return result;
        }
    }
}

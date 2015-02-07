using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Description of NbaUtil.
    /// </summary>
    public class NbaUtil : SiteUtilBase
    {
        [Category("OnlineVideosUserConfiguration"), Description("Defines number of videos to retrieve as one page.")]
        int itemsPerPage = 15;

        [Category("OnlineVideosConfiguration"), Description("Regular Expression used to parse logos from a html page for NBA teams. Groups need to be named 'logo' and 'teamname'.")]
        string logosRegEx = @"<li>\s*<a[^>]*>\s*<img\s+src=""(?<logo>[^""]+)"">\s*(?<teamname>[^<]*?)\s*</a>.*?</li>";
        [Category("OnlineVideosConfiguration"), Description("Url for a page that has links to all logos of the NBA teams")]
        string logosPage = "http://www.sportslogos.net/league.php?id=6";
        
        Regex regEx_logos;

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);

            if (!string.IsNullOrEmpty(logosRegEx)) regEx_logos = new Regex(logosRegEx, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
        }

        public override int DiscoverDynamicCategories()
        {
            Dictionary<string, string> teamLogos = GetTeamLogoUrls();

            string js = GetWebData("http://i.cdn.turner.com/nba/nba/z/.e/js/pkg/video/901.js");
            string json = Regex.Match(js, @"var\snbaChannelConfig=(?<json>[^\;]+)").Groups["json"].Value;

            List<Category> categories = new List<Category>();
            foreach (var jsonToken in Newtonsoft.Json.Linq.JObject.Parse(json))
            {
                string name = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(jsonToken.Key.Replace("/", ": "));
                Category mainCategory = new Category() { Name = name, HasSubCategories = true, SubCategoriesDiscovered = true, SubCategories = new List<Category>() };
                SetLogoAndName(teamLogos, mainCategory, jsonToken.Value as Newtonsoft.Json.Linq.JArray);          
                foreach (var subJo in jsonToken.Value)
                {
                    RssLink subCategory = new RssLink() { Name = subJo.Value<string>("display"), ParentCategory = mainCategory, Url = subJo.Value<string>("search_string") };
                    mainCategory.SubCategories.Add(subCategory);
                }
                categories.Add(mainCategory);
                categories.Sort();
                Settings.Categories.Clear();
                foreach (Category cat in categories) Settings.Categories.Add(cat);
            }
                        
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            currentPage = 1; pagesInCategory = 1; sectionBaseUrl = ((RssLink)category).Url; // reset next/prev fields
            return getVideoList("http://searchapp.nba.com/nba-search/query.jsp?type=advvideo&start=1&npp=" + itemsPerPage + "&" + ((RssLink)category).Url + "&season=1213&sort=recent");
        }

        List<VideoInfo> getVideoList(string inUrl)
        {
            List<VideoInfo> videos = new List<VideoInfo>();

            string data = GetWebData(inUrl);
            string json = Regex.Match(data, "<textarea id=\"jsCode\">(?<json>.+)</textarea>", RegexOptions.Singleline).Groups["json"].Value;

            Newtonsoft.Json.Linq.JObject jsonData = Newtonsoft.Json.Linq.JObject.Parse(json);
            int NumVideosTotal = jsonData["metaResults"].Value<int>("advvideo");            
            pagesInCategory = NumVideosTotal / itemsPerPage;
            foreach (var jo in jsonData["results"][0])
            {
                VideoInfo vi = new VideoInfo();
                vi.VideoUrl = jo.Value<string>("id").Replace("/video","");
                vi.VideoUrl = "http://nba.cdn.turner.com/nba/big" + vi.VideoUrl.Substring(0, vi.VideoUrl.LastIndexOf("/"));
                vi.VideoUrl += vi.VideoUrl.EndsWith(".nba") ? "_nba_576x324.flv" : ".flv";
                vi.Title = jo.Value<string>("title");
                vi.Description = jo["metadata"]["media"].Value<string>("excerpt");
                vi.Thumb = jo["metadata"]["media"]["thumbnail"].Value<string>("url");
                vi.Length = jo["metadata"]["video"].Value<string>("length");
                vi.Airdate = OnlineVideos.Utils.UNIXTimeToDateTime(jo.Value<long>("mediaDateUts")).ToString("g", OnlineVideoSettings.Instance.Locale);

                videos.Add(vi);
            }            
            return videos;
        }

        Dictionary<string, string> GetTeamLogoUrls()
        {
            Dictionary<string, string> teamLogos = new Dictionary<string, string>();
            try
            {
                if (regEx_logos != null && !string.IsNullOrEmpty(logosPage))
                {
                    string teamLogosPage = GetWebData(logosPage);
                    Match m = regEx_logos.Match(teamLogosPage);
                    while (m.Success)
                    {
                        string logoUrl = m.Groups["logo"].Value;
                        if (!Uri.IsWellFormedUriString(logoUrl, System.UriKind.Absolute)) logoUrl = new Uri(new Uri(logosPage), logoUrl).AbsoluteUri;
                        if (!teamLogos.ContainsKey(m.Groups["teamname"].Value.ToLower())) teamLogos.Add(m.Groups["teamname"].Value.ToLower(), logoUrl);
                        m = m.NextMatch();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Error loading team logos: {0}", ex.Message);
            }
            return teamLogos;
        }

        void SetLogoAndName(Dictionary<string, string> teamLogos, Category category, Newtonsoft.Json.Linq.JArray jsonValue)
        {
            try
            {
                string searchString = jsonValue[0].Value<string>("search_string");
                int index = searchString.IndexOf("&team=");
                if (index > 0)
                {
                    string name = System.Web.HttpUtility.UrlDecode(searchString.Substring(index + 6));
                    string logoUrl = "";
                    if (teamLogos.TryGetValue(name.ToLower(), out logoUrl))
                    {
                        category.Thumb = logoUrl;
                    }
                    else
                    {
                        // no direct match found, do weak matching
                        string relaxedName = name.ToLower().Substring(0, name.Length - 1);
                        foreach (string key in teamLogos.Keys)
                        {
                            if (key.Contains(relaxedName))
                            {
                                category.Thumb = teamLogos[key];
                                break;
                            }
                        }
                    }
                    name = "Team: " + name;
                    category.Name = name;
                }
                else if (category.Name.ToLower().Contains("league"))
                {
                    foreach (string key in teamLogos.Keys)
                    {
                        if (key.Contains("national basketball asso"))
                        {
                            category.Thumb = teamLogos[key];
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex.ToString());
            }
        }

        #region Paging

        int pagesInCategory = 1;
        int currentPage = 1;
        string sectionBaseUrl = "";
        public override bool HasNextPage
        {
            get { return currentPage < pagesInCategory; }
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            currentPage++;
            int start = ((currentPage - 1) * itemsPerPage) + 1;
            return getVideoList("http://searchapp.nba.com/nba-search/query.jsp?type=advvideo&start=" + start.ToString() + "&npp=" + itemsPerPage + "&" + sectionBaseUrl + "&season=1213&sort=recent");
        }

        #endregion

        #region Search

        string searchUrl = "http://searchapp.nba.com/nba-search/query.jsp?type=advvideo&start=1&npp={1}&text={0}&sort=recent";

        public override bool CanSearch { get { return true; } }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            query = System.Web.HttpUtility.UrlEncode(query);
            currentPage = 1; pagesInCategory = 1; sectionBaseUrl = "text="+query; // reset next/prev fields
            string url = string.Format(searchUrl, query, itemsPerPage);
            return getVideoList(url).ConvertAll<SearchResultItem>(v => v as SearchResultItem);
        }

        #endregion

    }
}
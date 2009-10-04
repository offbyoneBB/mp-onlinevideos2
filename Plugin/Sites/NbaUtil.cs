using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Net;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;
using MediaPortal.GUI.Library;
using MediaPortal.Configuration;
using Jayrock.Json;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Description of NbaUtil.
    /// </summary>
    public class NbaUtil : SiteUtilBase
    {
        static Regex subCategoriesAvailableRegEx = new Regex(@"<div\sid=""nbaSubNav""", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        static Regex subCategoriesRegEx = new Regex(@"\s<li><a\sid=""(?<url>.+)\#subNav""\shref=""javascript\:void\(0\)\;"">(?<name>.+)</a></li>", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        static Regex videosRegEx = new Regex(@"loadVideoArray\(\snew\sArray\((\s+'(?<url>[^']+)'[^']+)*\)\)\;", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.ExplicitCapture);

        static Regex amountPagesInSectionRegEx = new Regex(@"<div\sid=""nbaVideoFileCount"">(\d+)</div>", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        static Regex sectionPath1RegEx = new Regex(@"<div\sid=""nbaVideoFilePath"">(.+)</div>", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        static Regex sectionPath2RegEx = new Regex(@"<div\sid=""nbaVideoFileMap"">(.+)</div>", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        
        public override int DiscoverDynamicCategories()
        {
            foreach(RssLink link in Settings.Categories)
            {
                string data = GetWebData(link.Url);
                if (subCategoriesAvailableRegEx.IsMatch(data))
                {
                    link.HasSubCategories = true;
                    link.SubCategories = new List<Category>();
                    Match match = subCategoriesRegEx.Match(data);
                    while (match.Success)
                    {
                        RssLink subCat = new RssLink() { Name = string.Format("{0}: {1}", link.Name, System.Web.HttpUtility.HtmlDecode(match.Groups["name"].Value)), Url = link.Url.Substring(0, link.Url.LastIndexOf('/')+1) + match.Groups["url"].Value + ".html" };
                        subCat.ParentCategory = link;
                        link.SubCategories.Add(subCat);
                        match = match.NextMatch();
                    }
                    link.SubCategoriesDiscovered = true;
                }
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            currentPage = 1; pagesInCategory = 1; sectionBaseUrl = ""; // reset next/prev fields
            return getVideoList(((RssLink)category).Url);
        }

        List<VideoInfo> getVideoList(string inUrl)
        {
            List<VideoInfo> videos = new List<VideoInfo>();

            string data = GetWebData(inUrl);

            // prepare next previous infos
            Match m = amountPagesInSectionRegEx.Match(data);
            if (m.Success)
            {
                pagesInCategory = int.Parse(m.Groups[1].Value);

                m = sectionPath1RegEx.Match(data);
                if (m.Success) sectionBaseUrl = "http://www.nba.com/" + m.Groups[1].Value;

                m = sectionPath2RegEx.Match(data);
                if (m.Success) sectionBaseUrl += m.Groups[1].Value;
            }

            m = videosRegEx.Match(data);
            if (m.Success)
            {
                foreach (Capture c in m.Groups["url"].Captures)
                {
                    VideoInfo vi = new VideoInfo();

                    string jsonUrl = c.Value;
                    if (!jsonUrl.StartsWith("/"))
                        jsonUrl = "/video/" + jsonUrl;
                    if (!jsonUrl.EndsWith(".json")) 
                        jsonUrl += ".json";

                    jsonUrl = "http://www.nba.com" + jsonUrl;
                    JsonObject jsonData = (JsonObject)GetWebDataAsJson(jsonUrl);
                    vi.Title = (string)jsonData["headline"];
                    vi.Description = (string)jsonData["description"];
                    vi.Length = (string)jsonData["dateCreated"];
                    if ((jsonData["images"] as JsonArray).Count > 2)
                    {
                        vi.ImageUrl = (string)((jsonData["images"] as JsonArray)[2] as JsonObject)["resource"];
                    }

                    vi.VideoUrl = string.Format("http://nba.cdn.turner.com/nba/big{0}_nba_{1}.flv", (string)jsonData["location"], (string)((JsonArray)jsonData["sizes"])[0]);

                    videos.Add(vi);
                }                
            }

            return videos;
        }

        int pagesInCategory = 1;
        int currentPage = 1;
        string sectionBaseUrl = "";
        public override bool HasNextPage
        {
            get { return currentPage < pagesInCategory; }
        }
                
        public override bool HasPreviousPage
        {
            get { return currentPage > 1; }
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            currentPage++;
            return getVideoList(sectionBaseUrl + currentPage.ToString() + ".html");
        }

        public override List<VideoInfo> getPreviousPageVideos()
        {
            currentPage--;
            return getVideoList(sectionBaseUrl + currentPage.ToString() + ".html");
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections;
using System.Xml;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;
using RssToolkit.Rss;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// SVTPlayUtil gets streams from www.svtplay.se. It gets its categories 
    /// dynamicly and then uses rssfeeds to get the videos for each category
    /// </summary>
    public class SVTPlayUtil : SiteUtilBase
    {
        private const string listPage = "http://svtplay.se/alfabetisk";
        private const string rssCategoryUrl = "http://feeds.svtplay.se/v1/video/list/{0}?expression=full&mode=plain";
        private const string categoryImagesUrl = "http://material.svtplay.se/content/2/c6/";

        Regex reFindCategories = new Regex(@"(?<HTML><a[^>]*href\s*=\s*[\""\'']?/t/(?<Id>[^/]*)(?<CatPath>[^""''>\s]*)[\""\'']?[^>]*>(?<Title>[^<]+|.*?)?</a\s*>)");
        Regex reFindASX = new Regex(@"(?<HTML><a[^>]*href\s*=\s*[\""\'']?(?<HRef>[^""''>\s]*\.asx)[\""\'']?[^>]*>(?<Title>[^<]+|.*?)?</a\s*>)");
        Regex reFindWMV = new Regex(@"(?<HTML><a[^>]*href\s*=\s*[\""\'']?(?<HRef>[^""''>\s]*\.wmv)[\""\'']?[^>]*>(?<Title>[^<]+|.*?)?</a\s*>)");
        Regex reFindFLV = new Regex(@"(?<HTML><param[^>]*pathflv\s*=\s*[\""\'']?(?<HRef>[^""''>\s]*\.flv)[\""\'']?[^>]*/>)");
        Regex reFindImgUrl = new Regex(@"(?<HTML><link[^>]*href\s*=\s*[\""\'']?(?<IMGUrl>[^""''>\s]*\.jpg)[\""\'']?[^>]*/>)");
        Regex reFindStartTime = new Regex(@"starttime\svalue\s*=\s*[\""\'']?(?<starttime>[^""''>\s]*)");
        Regex reFindDuration = new Regex(@"duration\svalue\s*=\s*[\""\'']?(?<duration>[^""''>\s]*)");

        public override int DiscoverDynamicCategories()
        {
            SVTPlayCategory item;

            // Get Alfabeticlisting of shows (categories) from site
            string listPageContent = GetWebData(listPage, GetCookie());

            // Regex will find all anchor tags with href like 'href="/t/'
            Match match = reFindCategories.Match(listPageContent);

            while (match.Success)
            {
                // Found a href for a show (category)
                item = new SVTPlayCategory();

                item.Name = System.Web.HttpUtility.HtmlDecode(match.Groups["Title"].Value);
                item.Id = match.Groups["Id"].Value;
                item.Url = match.Groups["Url"].Value;                

                item.Thumb = categoryImagesUrl + item.Id.Substring(0, 2) + "/" + item.Id.Substring(2, 2) + "/" + item.Id.Substring(4, 2) + "/" + match.Groups["CatPath"].Value.Substring(1) + "_a.jpg" + "|" +
                             categoryImagesUrl + item.Id.Substring(0, 2) + "/" + item.Id.Substring(2, 2) + "/" + item.Id.Substring(4, 2) + "/" + match.Groups["CatPath"].Value.Substring(1).Replace("_", "") + "_a.jpg" + "|" +
                             categoryImagesUrl + item.Id.Substring(0, 2) + "/" + item.Id.Substring(2, 2) + "/" + item.Id.Substring(4, 2) + "/a_" + match.Groups["CatPath"].Value.Substring(1).Replace("_", "") + "_168.jpg";
                
                // Add category to List
                Settings.Categories.Add(item);

                // Find next match if possible
                match = match.NextMatch();
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        /// <summary>
        /// Gets web WITH Coockie information (both Media Player and Flash Player
        /// is supported by www.svtplay.se, we want the Media Player version).
        /// </summary>
        /// <param name="fsUrl"></param>
        /// <returns></returns>
        protected CookieContainer GetCookie()
        {
            CookieContainer cookieContainer = new CookieContainer();
            Cookie cookie = new Cookie("hasflash", "false");
            cookie.Domain = "svtplay.se";
            cookieContainer.Add(cookie);
            cookie = new Cookie("hasrealplayer", "false");
            cookie.Domain = "svt.se";
            cookieContainer.Add(cookie);
            cookie = new Cookie("haswinmedia", "true");
            cookie.Domain = "svtplay.se";
            cookieContainer.Add(cookie);
            cookie = new Cookie("prefferedformat", "1"); // Windows media
            cookie.Domain = "svtplay.se";
            cookieContainer.Add(cookie);
            return cookieContainer;
        }

        /// <summary>
        /// Get VideoInfos from category (rssfeed)
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public override List<VideoInfo> getVideoList(Category category)
        {
            VideoInfo videoInfo;

            // Get rss items from rss feed for category
            string url = string.Empty;
            if (category is SVTPlayCategory)
            {
                // Dynamic category is SVTPlayCategory
                url = string.Format(rssCategoryUrl, ((SVTPlayCategory)category).Id);
            }
            else
            {
                // Non dynamic is RssLink
                url = ((RssLink)category).Url;
            }

            // Get rssItems from rssfeed for category
            List<RssItem> rssItems = getRssDataItemsWithCookieData(url);

            // Create empty List<VideoInfo>
            List<VideoInfo> result = new List<VideoInfo>();

            // loop thru rssitems and construct VideoInfo of each RssItem, add VideoInfo to result
            foreach (RssItem rssItem in rssItems)
            {
                try
                {
                    // Create new VideoInfo
                    videoInfo = new VideoInfo();

                    // Title
                    if (!String.IsNullOrEmpty(rssItem.Title))
                        videoInfo.Title = rssItem.Title;

                    // VideoUrl AND ImageUrl
                    if (!String.IsNullOrEmpty(rssItem.Link))
                    {
                        Log.Debug("RssItem.Link: {0}", rssItem.Link);

                        // Try to find video from rss link
                        string rssLinkContent = GetWebData(rssItem.Link, GetCookie());
                        // Searches for href with .asx
                        Match asxMatch = reFindASX.Match(rssLinkContent);
                        if (asxMatch.Success)
                        {
                            Log.Debug("Found ASXlink: {0}", asxMatch.Groups["HRef"].Value);
                            string asxContent = GetWebData(asxMatch.Groups["HRef"].Value, GetCookie());
                            Log.Debug("ASXContent: {0}", asxContent);

                            // Find URL for video in ASX
                            Match mmsMatch = Regex.Match(asxContent.ToLower(), @"(?<=href.?=.?"").*?(?="")");
                            if (mmsMatch.Success && mmsMatch.Value != "")
                            {
                                if (reFindStartTime.Match(asxContent.ToLower()).Groups["starttime"].Value != "00:00:00.00")
                                {
                                    videoInfo.StartTime = reFindStartTime.Match(asxContent.ToLower()).Groups["starttime"].Value;
                                }

                                videoInfo.Length = reFindDuration.Match(asxContent.ToLower()).Groups["duration"].Value;

                                videoInfo.VideoUrl = mmsMatch.Value;

                                if (videoInfo.VideoUrl.ToLower().StartsWith("mms"))
                                {
                                    // http works better
                                    videoInfo.VideoUrl = "http" + videoInfo.VideoUrl.Remove(0, "mms".Length);
                                }
                            }
                        }
                        if (videoInfo.VideoUrl == string.Empty)
                        {
                            Log.Debug("Trying to find WMV file instead of ASX");

                            // Try finding wmv file (some feeds have WMV, dont know why and when?)
                            Match wmvMatch = reFindWMV.Match(rssLinkContent);
                            if (wmvMatch.Success)
                            {
                                Log.Debug("Found WMV: {0}", wmvMatch.Groups["HRef"].Value);

                                videoInfo.VideoUrl = wmvMatch.Groups["HRef"].Value;

                                if (videoInfo.VideoUrl.ToLower().StartsWith("mms"))
                                {
                                    // http works better
                                    videoInfo.VideoUrl = "http" + videoInfo.VideoUrl.Remove(0, "mms".Length);
                                }
                            }
                        }
                        if (videoInfo.VideoUrl == string.Empty)
                        {
                            Log.Debug("Trying to find FLV file instead of ASX/WMV");

                            // Try finding flv file (some feeds have no WMV/ASX, dont know why and when?)
                            Match wmvMatch = reFindFLV.Match(rssLinkContent);
                            if (wmvMatch.Success)
                            {
                                Log.Debug("Found FLV: {0}", wmvMatch.Groups["HRef"].Value);

                                videoInfo.VideoUrl = wmvMatch.Groups["HRef"].Value;

                                if (videoInfo.VideoUrl.ToLower().StartsWith("mms"))
                                {
                                    // http works better
                                    videoInfo.VideoUrl = "http" + videoInfo.VideoUrl.Remove(0, "mms".Length);
                                }
                            }
                        }

                        // ImageUrl
                        Match matchIMGUrl = reFindImgUrl.Match(rssLinkContent);
                        if (matchIMGUrl.Success)
                        {
                            videoInfo.ImageUrl = matchIMGUrl.Groups["IMGUrl"].Value;
                        }
                        else
                        {
                            videoInfo.ImageUrl = "http://www.svtplay.se";
                        }


                    }

                    // Description
                    if (!String.IsNullOrEmpty(rssItem.Description))
                        videoInfo.Description = rssItem.Description;

                    // Title2 (pubDate from rssItem)
                    if (!String.IsNullOrEmpty(rssItem.PubDate))
                        videoInfo.Title2 = rssItem.PubDate;

                    Log.Debug("Adding videoInfo: {0}", videoInfo.ToString());

                    // Add VideoInfo to List
                    result.Add(videoInfo);
                }
                catch
                {

                }
            }

            return result;
        }

        /// <summary>
        /// getRssDataItemsWithCookieData is used instead of getRssDataItems
        /// because getRssDataItems has cache in it and we dont wont that here
        /// </summary>
        /// <param name="fsUrl"></param>
        /// <returns></returns>
        private List<RssItem> getRssDataItemsWithCookieData(string fsUrl)
        {
            try
            {
                return RssDocument.Load(GetWebData(fsUrl, GetCookie())).Channel.Items;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return new List<RssToolkit.Rss.RssItem>();
            }
        }

    }

    public class SVTPlayCategory : RssLink
    {        
        public string Id { get; set; }
    }

}

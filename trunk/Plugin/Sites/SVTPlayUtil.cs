using System;
using System.Collections.Generic;
using System.Collections;
using System.Xml;
using System.Text;
using System.Net;
using MediaPortal.GUI.Library;
using MediaPortal.Configuration;
using System.Text.RegularExpressions;

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

        Regex reFindCategories = new Regex(@"(?<HTML><a[^>]*href\s*=\s*[\""\'']?/t/(?<Id>[^/)(?<HRef>[^""''>\s]*)[\""\'']?[^>]*>(?<Title>[^<]+|.*?)?</a\s*>)");
        Regex reFindASX = new Regex(@"(?<HTML><a[^>]*href\s*=\s*[\""\'']?(?<HRef>[^""''>\s]*\.asx)[\""\'']?[^>]*>(?<Title>[^<]+|.*?)?</a\s*>)");
        Regex reFindWMV = new Regex(@"(?<HTML><a[^>]*href\s*=\s*[\""\'']?(?<HRef>[^""''>\s]*\.wmv)[\""\'']?[^>]*>(?<Title>[^<]+|.*?)?</a\s*>)");
        Regex reFindFLV = new Regex(@"(?<HTML><param[^>]*pathflv\s*=\s*[\""\'']?(?<HRef>[^""''>\s]*\.flv)[\""\'']?[^>]*/>)");
        Regex reFindImgUrl = new Regex(@"(?<HTML><link[^>]*href\s*=\s*[\""\'']?(?<IMGUrl>[^""''>\s]*\.jpg)[\""\'']?[^>]*/>)");
        Regex reFindStartTime = new Regex(@"starttime\svalue\s*=\s*[\""\'']?(?<starttime>[^""''>\s]*)");
        Regex reFindDuration = new Regex(@"duration\svalue\s*=\s*[\""\'']?(?<duration>[^""''>\s]*)");

        public override List<Category> getDynamicCategories()
        {
            List<Category> result = new List<Category>();
            SVTPlayCategory item;

            // Get Alfabeticlisting of shows (categories) from site
            string listPageContent = GetWebWithCoockieData(listPage);

            // Regex will find all anchor tags with href like 'href="/t/'
            Match match = reFindCategories.Match(listPageContent);

            while (match.Success)
            {
                // Found a href for a show (category)
                item = new SVTPlayCategory();

                item.Name = System.Web.HttpUtility.HtmlDecode(match.Groups["Title"].Value);
                item.Id = match.Groups["Id"].Value;
                item.Url = match.Groups["Url"].Value;

                // Add category to List
                result.Add(item);

                // Find next match if possible
                match = match.NextMatch();
            }

            return result;
        }

        /// <summary>
        /// Gets web WITH Coockie information (both Media Player and Flash Player
        /// is supported by www.svtplay.se, we want the Media Player version).
        /// </summary>
        /// <param name="fsUrl"></param>
        /// <returns></returns>
        protected static string GetWebWithCoockieData(string fsUrl)
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

            HttpWebRequest request = WebRequest.Create(fsUrl) as HttpWebRequest;
            if (request == null) return "";
            request.CookieContainer = cookieContainer;
            request.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.0; sv-SE; rv:1.9.1b2) Gecko/20081201 Firefox/3.1b2";
            request.Timeout = 20000;
            WebResponse response = request.GetResponse();
            using (System.IO.StreamReader reader = new System.IO.StreamReader(response.GetResponseStream(), System.Text.Encoding.UTF8))
            {
                string str = reader.ReadToEnd();
                return str.Trim();
            }
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
                // Create new VideoInfo
                videoInfo = new VideoInfo();

                // Title
                if (!String.IsNullOrEmpty(rssItem.title))
                    videoInfo.Title = rssItem.title;

                // VideoUrl AND ImageUrl
                if (!String.IsNullOrEmpty(rssItem.link))
                {
                    Log.Debug("RssItem.Link: {0}", rssItem.link);

                    // Try to find video from rss link
                    string rssLinkContent = GetWebWithCoockieData(rssItem.link);
                    // Searches for href with .asx
                    Match asxMatch = reFindASX.Match(rssLinkContent);
                    if (asxMatch.Success)
                    {
                        Log.Debug("Found ASXlink: {0}", asxMatch.Groups["HRef"].Value);
                        string asxContent = GetWebWithCoockieData(asxMatch.Groups["HRef"].Value);
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
                if (!String.IsNullOrEmpty(rssItem.description))
                    videoInfo.Description = rssItem.description;

                // Title2 (pubDate from rssItem)
                if (!String.IsNullOrEmpty(rssItem.pubDate))
                    videoInfo.Title2 = rssItem.pubDate;

                Log.Debug("Adding videoInfo: {0}", videoInfo.ToString());

                // Add VideoInfo to List
                result.Add(videoInfo);
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
                return RssWrapper.GetRssItems(GetWebWithCoockieData(fsUrl));
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return new List<RssItem>();
            }
        }

    }

    public class SVTPlayCategory : RssLink
    {
        private string id;
        public string Id
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
            }
        }
    }

}

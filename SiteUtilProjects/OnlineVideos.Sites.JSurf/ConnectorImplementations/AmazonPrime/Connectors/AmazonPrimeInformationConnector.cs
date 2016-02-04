using OnlineVideos.Sites.JSurf.Interfaces;
using System;
using System.Collections.Generic;
using OnlineVideos.Sites.JSurf.ConnectorImplementations.AmazonPrime.Extensions;
using System.Globalization;
using OnlineVideos.Sites.JSurf.Properties;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites.JSurf.ConnectorImplementations.AmazonPrime.Connectors
{
    public class AmazonPrimeInformationConnector : IInformationConnector
    {
        SiteUtilBase _siteUtil;
        AmazonBrowserSession _browserSession;

        public AmazonPrimeInformationConnector(SiteUtilBase siteUtil)
        {
            _siteUtil = siteUtil;
            _browserSession = new AmazonBrowserSession();
        }

        /// <summary>
        /// Don't want the results sorted
        /// </summary>
        public bool ShouldSortResults
        {
            get { return false; }
        }

        /// <summary>
        /// The player class name
        /// </summary>
        public string ConnectorEntityTypeName
        {
            get
            {
                if (_siteUtil.Settings.Language == "de")
                {
                    return "OnlineVideos.Sites.JSurf.ConnectorImplementations.AmazonPrimeDe.Connectors.AmazonPrimeDeConnector";
                }
                else
                {
                    return "OnlineVideos.Sites.JSurf.ConnectorImplementations.AmazonPrime.Connectors.AmazonPrimeConnector";
                }
            }
        }

        /// <summary>
        /// Load the categories
        /// </summary>
        /// <param name="parentCategory"></param>
        /// <returns></returns>
        public List<Category> LoadCategories(Category parentCategory = null)
        {
            Resources.Culture = new CultureInfo(_siteUtil.Settings == null ? string.Empty : _siteUtil.Settings.Language);

            var result = new List<Category>();

            if (parentCategory == null)
            {
                result.Add(new Category { HasSubCategories = true, Name = "Movies", SubCategoriesDiscovered = false, Other = "M", Thumb = Resources.AmazonMovieIcon });
                result.Add(new Category { HasSubCategories = true, Name = "Tv", SubCategoriesDiscovered = false, Other = "T", Thumb = Resources.AmazonTvIcon });
                result.Add(new Category { HasSubCategories = true, Name = "Watchlist", SubCategoriesDiscovered = false, Other = "W", Thumb = Resources.AmazonTvIcon });
            }
            else
            {
                DoLogin();
                // Grab next page categories here (we'll deal with videos as the category)
                if (parentCategory is NextPageCategory)
                {
                    result = (parentCategory as NextPageCategory).Url.LoadAmazonPrimeVideosAsCategoriesFromUrl(parentCategory.ParentCategory, _browserSession);
                    parentCategory.ParentCategory.SubCategories.AddRange(result);
                }
                else
                {
                    if (parentCategory.Other.ToString() == "M")
                    {
                        result = Resources.AmazonMovieCategoriesUrl.LoadAmazonPrimeCategoriesFromUrl(parentCategory, _browserSession);
                        result.Insert(0, new Category { HasSubCategories = true, Name = "Editor's Picks", SubCategoriesDiscovered = false, Other = "ME", Thumb = Resources.AmazonMovieIcon });
                        result.Insert(0, new Category { HasSubCategories = true, Name = "Recently Added", SubCategoriesDiscovered = false, Other = "MA", Thumb = Resources.AmazonMovieIcon });
                        result.Insert(0, new Category { HasSubCategories = true, Name = "Popular Movies", SubCategoriesDiscovered = false, Other = "MP", Thumb = Resources.AmazonMovieIcon });
                        result.Insert(0, new Category { HasSubCategories = true, Name = "Watchlist", SubCategoriesDiscovered = false, Other = "WM", Thumb = Resources.AmazonMovieIcon });
                    }
                    else if (parentCategory.Other.ToString() == "MP")
                    {
                        result = Resources.AmazonMoviePopularUrl.LoadAmazonPrimeVideosAsCategoriesFromUrl(parentCategory, _browserSession);
                    }
                    else if (parentCategory.Other.ToString() == "MA")
                    {
                        result = Resources.AmazonMovieRecentUrl.LoadAmazonPrimeVideosAsCategoriesFromUrl(parentCategory, _browserSession);
                    }
                    else if (parentCategory.Other.ToString() == "ME")
                    {
                        result = Resources.AmazonMovieEditorsUrl.LoadAmazonPrimeCategoriesFromUrl(parentCategory, _browserSession);
                    }
                    else if (parentCategory.Other.ToString() == "T")
                    {
                        result = Resources.AmazonTVCategoriesUrl.LoadAmazonPrimeCategoriesFromUrl(parentCategory, _browserSession);
                        result.Insert(0, new Category { HasSubCategories = true, Name = "Editor's Picks", SubCategoriesDiscovered = false, Other = "TE", Thumb = Resources.AmazonTvIcon });
                        result.Insert(0, new Category { HasSubCategories = true, Name = "Recently Added", SubCategoriesDiscovered = false, Other = "TA", Thumb = Resources.AmazonTvIcon });
                        result.Insert(0, new Category { HasSubCategories = true, Name = "Popular TV Shows", SubCategoriesDiscovered = false, Other = "TP", Thumb = Resources.AmazonTvIcon });
                        result.Insert(0, new Category { HasSubCategories = true, Name = "Watchlist", SubCategoriesDiscovered = false, Other = "WT", Thumb = Resources.AmazonTvIcon });
                    }
                    else if (parentCategory.Other.ToString() == "TP")
                    {
                        result = Resources.AmazonTVPopularUrl.LoadAmazonPrimeVideosAsCategoriesFromUrl(parentCategory, _browserSession);
                    }
                    else if (parentCategory.Other.ToString() == "TA")
                    {
                        result = Resources.AmazonTVRecentUrl.LoadAmazonPrimeVideosAsCategoriesFromUrl(parentCategory, _browserSession);
                    }
                    else if (parentCategory.Other.ToString() == "TE")
                    {
                        result = Resources.AmazonTVEditorsUrl.LoadAmazonPrimeCategoriesFromUrl(parentCategory, _browserSession);
                    }
                    else if (parentCategory.Other.ToString() == "W")
                    {
                        result.Add(new Category { HasSubCategories = true, Name = "TV Watchlist", SubCategoriesDiscovered = false, Other = "WT", Thumb = Resources.AmazonMovieIcon });
                        result.Add(new Category { HasSubCategories = true, Name = "Movies Watchlist", SubCategoriesDiscovered = false, Other = "WM", Thumb = Resources.AmazonMovieIcon });
                    }
                    else if (parentCategory.Other.ToString() == "WM")
                    {
                        result = Resources.AmazonMovieWatchlistUrl.LoadAmazonPrimeWatchlistAsCategoriesFromUrl(parentCategory, _browserSession);
                    }
                    else if (parentCategory.Other.ToString() == "WT")
                    {
                        result = Resources.AmazonTVWatchlistUrl.LoadAmazonPrimeWatchlistAsCategoriesFromUrl(parentCategory, _browserSession);
                    }
                    else if (parentCategory.Other.ToString().StartsWith("V~"))
                    {
                        result = ((parentCategory.Other.ToString().ToLower().Contains(Resources.AmazonRootUrl.ToLower()) ? string.Empty : Resources.AmazonRootUrl) + (parentCategory.Other.ToString()).Replace("V~", string.Empty)).LoadAmazonPrimeVideosAsCategoriesFromUrl(parentCategory, _browserSession);
                    }
                    else
                    {
                        result = Resources.AmazonTVCategoriesUrl.LoadAmazonPrimeCategoriesFromUrl(parentCategory, _browserSession);
                    }
                    parentCategory.SubCategories.AddRange(result);
                }

            }
            return result;
        }

        /// <summary>
        /// Load the videos using either the parentCategory, or the next page url (if parentCategory is null)
        /// </summary>
        /// <param name="parentCategory"></param>
        /// <returns></returns>
        public List<VideoInfo> LoadVideos(Category parentCategory)
        {
            DoLogin();
            return parentCategory.Other.ToString().LoadVideosFromUrl(_browserSession);
        }

        public List<String> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            // Only browser players are supported anymore (since mid of 2015)
            _siteUtil.Settings.Player = PlayerType.Browser;
            return new List<string> { video.Other.ToString() };

            //_siteUtil.Settings.Player = PlayerType.Internal;

            //video.PlaybackOptions = getPlaybackOptions(video);
            //var videoQuality = ((AmazonPrimeSiteUtil)_siteUtil).StreamVideoQuality;

            //List<String> urls = new List<String>();
            //if (video.PlaybackOptions != null && video.PlaybackOptions.Count > 0)
            //{
            //    if (video.PlaybackOptions.Count == 1)
            //    {
            //        // nothing to chose from, only one options available
            //        return new List<string>() { video.PlaybackOptions.First().Value };
            //    }
            //    else
            //    {
            //        KeyValuePair<string, string> foundQuality = default(KeyValuePair<string, string>);
            //        switch (videoQuality)
            //        {
            //            case OnlineVideos.Sites.JSurf.AmazonPrimeSiteUtil.VideoQuality.Low:		//use first available option
            //               foundQuality = video.PlaybackOptions.First(); break;
            //            case OnlineVideos.Sites.JSurf.AmazonPrimeSiteUtil.VideoQuality.Medium:	// 480p 2000kpbs
            //               foundQuality = video.PlaybackOptions.LastOrDefault(q => q.Key.Contains("2000")); break;
            //            case OnlineVideos.Sites.JSurf.AmazonPrimeSiteUtil.VideoQuality.High:		// 720p 2500kbps
            //               foundQuality = video.PlaybackOptions.LastOrDefault(q => q.Key.Contains("2500")); break;
            //            case OnlineVideos.Sites.JSurf.AmazonPrimeSiteUtil.VideoQuality.HD:		// 720p 4000kbps
            //               foundQuality = video.PlaybackOptions.LastOrDefault(q => q.Key.Contains("4000")); break;
            //            case OnlineVideos.Sites.JSurf.AmazonPrimeSiteUtil.VideoQuality.FullHD:	//use highest available quality
            //               foundQuality = video.PlaybackOptions.Last(); break;
            //        }
            //        // fallback when no match was found -> use highest choice
            //        if (string.IsNullOrEmpty(foundQuality.Key)) foundQuality = video.PlaybackOptions.Last();
            //        if (inPlaylist) video.PlaybackOptions = null;
            //        return new List<string>() { foundQuality.Value };
            //    }
            //}
            //return null;
        }

        public Dictionary<string, string> getPlaybackOptions(VideoInfo video)
        {
            DoLogin();
            var docStr = "";
            MatchCollection matchCID = null;
            for (int i = 0; i <= 10; i++)
            {
                docStr = _browserSession.LoadAsStr(Resources.AmazonMovieUrl(video.Other.ToString()));
                matchCID = Regex.Matches(docStr, "\"customerID\":\"(.+?)\"", RegexOptions.None);
                if (matchCID.Count > 0)
                    break;
            }
            Log.Info("matchCID" + matchCID[0].Groups[1].ToString());
            MatchCollection matchToken = Regex.Matches(docStr, "\"csrfToken\":\"(.+?)\"", RegexOptions.None);
            Log.Info("matchToken" + matchToken[0].Groups[1].ToString());
            MatchCollection matchMID = Regex.Matches(docStr, "\"marketplaceID\":\"(.+?)\"", RegexOptions.None);
            Log.Info("matchMID" + matchMID[0].Groups[1].ToString());
            // need to do extra url call...
            string deviceTypeID = "A35LWR0L7KC0TJ"; //"A324MFXUEZFF7B";
            MatchCollection matchSWFUrl = Regex.Matches(docStr, "\\<script.*?type.*?=.*?\"text\\/javascript\".+?src.*?=.*?\"(.+?webplayer[^\"]+?)\"", RegexOptions.None);
            if (matchSWFUrl.Count > 0)
            {
                var swfStr = _browserSession.LoadAsStr(matchSWFUrl[0].Groups[1].ToString());
                Log.Info("matchSwfJS" + matchSWFUrl[0].Groups[1].ToString());
                MatchCollection matchSwf = Regex.Matches(swfStr, "LEGACY_FLASH_SWF=\"(.+?)\"", RegexOptions.None);
                Log.Info("matchSwf" + matchSwf[0].Groups[1].ToString());
                MatchCollection matchDID = Regex.Matches(swfStr, "FLASH_GOOGLE_TV=\"(.+?)\"", RegexOptions.None);
                Log.Info("matchDID" + matchDID[0].Groups[1].ToString());
                //deviceTypeID = matchDID[0].Groups[1].ToString(); //"A324MFXUEZFF7B";
            }

            long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            var urlMainS = Resources.AmazonRootUrl.Replace("http", "https");

            if (docStr.Contains("parental-controls-on"))
            {
                Log.Info("Parental control on, try to send pin");
                var pinUrl = urlMainS + "/gp/video/streaming/player-pin-validation.json/ref=dp_pct_pin_cont?callback=__jpcb1422536298130&pin=" + ((AmazonPrimeSiteUtil)_siteUtil).AmznAdultPin + "&csrftoken=" + Uri.EscapeDataString(matchToken[0].Groups[1].ToString()) + "&_=" + milliseconds;
                var pinResponse = _browserSession.LoadAsStr(pinUrl);
                Log.Info(pinResponse);
            }

            var jsonUrl = urlMainS + "/gp/video/streaming/player-token.json?callback=jQuery16406641344620746118_" + milliseconds + "&csrftoken=" + Uri.EscapeDataString(matchToken[0].Groups[1].ToString()) + "&_=" + milliseconds;
            Log.Info(jsonUrl);
            var tokenResponse = _browserSession.LoadAsStr(jsonUrl);
            Log.Info(tokenResponse);
            matchToken = Regex.Matches(tokenResponse, "\"token\":\"(.+?)\"", RegexOptions.None);
            Log.Info("matchToken" + matchToken[0].Groups[1].ToString());
            string token = matchToken[0].Groups[1].ToString();

            jsonUrl = "https://atv-ps-eu.amazon.com/cdp/catalog/GetStreamingUrlSets?version=1&format=json&firmware=WIN%2011,7,700,224%20PlugIn&marketplaceID=" + matchMID[0].Groups[1].ToString() + "&token=" + token + "&deviceTypeID=" + deviceTypeID + "&asin=" + video.Other.ToString() + "&customerID=" + matchCID[0].Groups[1].ToString() + "&deviceID=" + matchCID[0].Groups[1].ToString() + milliseconds + video.Other.ToString();

            var streamingUrls = _browserSession.LoadAsJSON(jsonUrl);
            //Log.Info(streamingUrls);
            var urlInfos = streamingUrls["message"]["body"]["urlSets"]["streamingURLInfoSet"][0]["streamingURLInfo"];
            Dictionary<string, string> PlaybackOptions = new Dictionary<string, string>();
            Log.Info(urlInfos.ToString());
            foreach (var urlInfo in urlInfos)
            {
                Log.Info(urlInfo.ToString());
                string theUrl = (string)urlInfo["url"];
                theUrl = theUrl.Replace("rtmpe", "rtmp");
                //var theUrl = matchUrl[0].Groups[1].ToString().Replace("rtmpe", "rtmp");

                string videoID = video.Other.ToString();
                string rtmpMain = "azeufms";

                MatchCollection matchPlaypath = Regex.Matches(theUrl, "(mp4:.+)", RegexOptions.None);
                string playpath = matchPlaypath[0].Groups[1].ToString();
                Log.Info(theUrl);
                string resultUrl = "http://azeufms-vodfs.fplive.net/" + playpath.Substring(4);
                /*Log.Info(theUrl + " swfVfy=1 swfUrl=" + matchSwf[0].Groups[1].ToString() + " pageUrl=" + Properties.Resources.AmazonRootUrl + "/dp/" + videoID + " app=" + rtmpMain + "-vod playpath=" + playpath + " tcUrl=rtmpe://" + rtmpMain + "-vodfs.fplive.net:1935/" + rtmpMain + "-vod/");
                //theUrl = theUrl + " swfVfy=1 swfUrl=" + matchSwf[0].Groups[1].ToString() + " pageUrl=" + Properties.Resources.AmazonRootUrl + "/dp/" + videoID + " app=" + rtmpMain + "-vod playpath=" + playpath + " tcUrl=rtmpe://" + rtmpMain + "-vodfs.fplive.net:1935/" + rtmpMain + "-vod/";
                string resultUrl = new MPUrlSourceFilter.RtmpUrl(theUrl)
                {
                    App = rtmpMain + "-vod",
                    PlayPath = playpath,
                    SwfUrl = matchSwf[0].Groups[1].ToString(),
                    SwfVerify = true,
                    PageUrl = Properties.Resources.AmazonMovieUrl(video.Other.ToString()),
                    TcUrl = "rtmpe://" + rtmpMain + "-vodfs.fplive.net:1935/" + rtmpMain + "-vod/"

                }.ToString();*/

                //if (theUrl.Contains("$"))
                //{
                //}

                Log.Info(resultUrl);
                PlaybackOptions.Add(urlInfo["contentQuality"].ToString() + " (" + urlInfo["bitrate"].ToString() + " kbps)", resultUrl);
            }

            return PlaybackOptions;
        }

        public bool CanSearch { get { return true; } }

        public List<SearchResultItem> DoSearch(string query)
        {
            DoLogin();
            return Resources.AmazonSearchUrl.LoadAmazonPrimeSearchAsCategoriesFromUrl(query, _browserSession);
        }

        protected void DoLogin()
        {
            var username = ((AmazonPrimeSiteUtil)_siteUtil).UserName;
            var password = ((AmazonPrimeSiteUtil)_siteUtil).Password;
            _browserSession.Login(username, password);
        }
    }
}

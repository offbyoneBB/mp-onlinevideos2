using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;
using System.Net;
using System.IO;

namespace OnlineVideos.Sites
{
    public class WatchSeriesUtil : DeferredResolveUtil
    {
        private enum Depth { MainMenu = 0, Alfabet = 1, Series = 2, Seasons = 3, BareList = 4 };
        public CookieContainer cc = null;
        private string nextVideoListPageUrl = null;
        private Category currCategory = null;

        public void GetBaseCookie()
        {
            HttpWebRequest request = WebRequest.Create(baseUrl) as HttpWebRequest;
            if (request == null) return;
            request.UserAgent = OnlineVideoSettings.Instance.UserAgent;
            request.Accept = "*/*";
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
            request.CookieContainer = new CookieContainer();
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            finally
            {
                if (response != null) ((IDisposable)response).Dispose();
            }

            cc = new CookieContainer();
            CookieCollection ccol = request.CookieContainer.GetCookies(new Uri(baseUrl));
            foreach (Cookie c in ccol)
                cc.Add(c);
        }

        public override int DiscoverDynamicCategories()
        {
            GetBaseCookie();

            base.DiscoverDynamicCategories();
            int i = 0;
            do
            {
                RssLink cat = (RssLink)Settings.Categories[i];
                if (cat.Name.ToUpperInvariant() == "HOME" || cat.Name.ToUpperInvariant() == "HOW TO WATCH" ||
                    cat.Name.ToUpperInvariant() == "CONTACT" || cat.Name.ToUpperInvariant() == "ABOUT US" ||
                    cat.Name.ToUpperInvariant() == "SPORT"
                   )
                    Settings.Categories.Remove(cat);
                else
                {
                    if (cat.Url.EndsWith("/A"))
                        cat.Other = Depth.MainMenu;
                    else
                    {
                        cat.Other = Depth.BareList;
                        cat.HasSubCategories = false;
                    }
                    i++;
                }
            }
            while (i < Settings.Categories.Count);
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            return GetSubCategories(parentCategory, ((RssLink)parentCategory).Url);
        }

        private int GetSubCategories(Category parentCategory, string url)
        {
            string webData;
            int p = url.IndexOf('#');
            if (p >= 0)
            {
                string nm = url.Substring(p + 1);
                webData = GetWebData(url.Substring(0, p), cc, forceUTF8: true);
                webData = @"class=""listbig"">" + GetSubString(webData, @"class=""listbig""><a name=""" + nm + @"""", @"class=""listbig""");
            }
            else
                webData = GetWebData(url, cc, forceUTF8: true);

            parentCategory.SubCategories = new List<Category>();
            Match m = null;
            switch ((Depth)parentCategory.Other)
            {
                case Depth.MainMenu:
                    webData = GetSubString(webData, @"class=""pagination""", @"class=""listbig""");
                    m = regEx_dynamicCategories.Match(webData);
                    break;
                case Depth.Alfabet:
                    webData = GetSubString(webData, @"class=""listbig""", @"class=""clear""");
                    m = regEx_dynamicSubCategories.Match(webData);
                    break;
                case Depth.Series:
                    webData = GetSubString(webData, @"class=""lists"">", @"class=""clear""");
                    string[] tmp = { @"class=""lists"">" };
                    string[] seasons = webData.Split(tmp, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string s in seasons)
                    {
                        RssLink cat = new RssLink();
                        cat.Name = HttpUtility.HtmlDecode(GetSubString(s, ">", "<")).Trim();
                        cat.Url = s;
                        cat.SubCategoriesDiscovered = true;
                        cat.HasSubCategories = false;
                        cat.Other = ((Depth)parentCategory.Other) + 1;

                        parentCategory.SubCategories.Add(cat);
                        cat.ParentCategory = parentCategory;
                    }
                    break;
                default:
                    m = null;
                    break;
            }

            while (m != null && m.Success)
            {
                RssLink cat = new RssLink();
                cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value);
                cat.Url = m.Groups["url"].Value;
                if (!String.IsNullOrEmpty(cat.Url) && !Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute))
                    cat.Url = new Uri(new Uri(baseUrl), cat.Url).AbsoluteUri;

                cat.Description = HttpUtility.HtmlDecode(m.Groups["description"].Value);
                cat.HasSubCategories = !parentCategory.Other.Equals(Depth.Series);
                cat.Other = ((Depth)parentCategory.Other) + 1;

                if (cat.Name == "NEW")
                {
                    cat.HasSubCategories = false;
                    cat.Other = Depth.BareList;
                }

                parentCategory.SubCategories.Add(cat);
                cat.ParentCategory = parentCategory;
                m = m.NextMatch();
            }

            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            return getOnePageVideoList(category, ((RssLink)category).Url);
        }

        private List<VideoInfo> getOnePageVideoList(Category category, string url)
        {
            currCategory = category;
            nextVideoListPageUrl = null;
            string webData;
            if (category.Other.Equals(Depth.BareList))
            {
                webData = GetWebData(url, cc, forceUTF8: true);
                webData = GetSubString(webData, @"class=""listbig""", @"class=""clear""");
                string[] parts = url.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    if (parts[parts.Length - 1] == "latest")
                        nextVideoListPageUrl = url + "/1";
                    else
                    {
                        int pageNr;
                        if (parts[parts.Length - 2] == "latest" && int.TryParse(parts[parts.Length - 1], out pageNr))
                            if (pageNr + 1 <= 9)
                                nextVideoListPageUrl = url.Substring(0, url.Length - 1) + (pageNr + 1).ToString();
                    }
                }
            }
            else
                webData = url;

            List<VideoInfo> videos = new List<VideoInfo>();
            if (!string.IsNullOrEmpty(webData))
            {
                Match m = regEx_VideoList.Match(webData);
                while (m.Success)
                {
                    VideoInfo video = CreateVideoInfo();

                    video.Title = HttpUtility.HtmlDecode(m.Groups["Title"].Value);
                    video.VideoUrl = m.Groups["VideoUrl"].Value;
                    if (!String.IsNullOrEmpty(video.VideoUrl) && !Uri.IsWellFormedUriString(video.VideoUrl, System.UriKind.Absolute))
                        video.VideoUrl = new Uri(new Uri(baseUrl), video.VideoUrl).AbsoluteUri;
                    video.Airdate = m.Groups["Airdate"].Value;
                    if (video.Airdate == "-")
                        video.Airdate = String.Empty;

                    try
                    {
                        string name = string.Empty;
                        int season = -1;
                        int episode = -1;
                        int year = -1;

                        // 1st way - Seas. X Ep. Y
                        //Modern Family Seas. 1 Ep. 12
                        Match trackingInfoMatch = Regex.Match(video.Title, @"(?<name>.+)\s+Seas\.\s*?(?<season>\d+)\s+Ep\.\s*?(?<episode>\d+)", RegexOptions.IgnoreCase);
                        FillTrackingInfoData(trackingInfoMatch, ref name, ref season, ref episode, ref year);

                        if (!GotTrackingInfoData(name, season, episode, year) &&
                            category != null && category.ParentCategory != null &&
                            !string.IsNullOrEmpty(category.Name) && !string.IsNullOrEmpty(category.ParentCategory.Name))
                        {
                            // 2nd way - using parent category name, category name and video title 
                            //Aaron Stone Season 1 (19 episodes) 1. Episode 21 1 Hero Rising (1)
                            string parseString = string.Format("{0} {1} {2}", category.ParentCategory.Name, category.Name, video.Title);
                            trackingInfoMatch = Regex.Match(parseString, @"(?<name>.+)\s+Season\s*?(?<season>\d+).*?Episode\s*?(?<episode>\d+)", RegexOptions.IgnoreCase);
                            FillTrackingInfoData(trackingInfoMatch, ref name, ref season, ref episode, ref year);
                        }

                        if (GotTrackingInfoData(name, season, episode, year))
                        {
                            TrackingInfo tInfo = new TrackingInfo();
                            tInfo.Title = name;
                            tInfo.Season = (uint)season;
                            tInfo.Episode = (uint)episode;
                            tInfo.VideoKind = VideoKind.TvSeries;
                            video.Other = tInfo;
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Warn("Error parsing TrackingInfo data: {0}", e.ToString());
                    }

                    videos.Add(video);
                    m = m.NextMatch();
                }

            }
            return videos;
        }

        public static bool GotTrackingInfoData(string name, int season, int episode, int year)
        {
            return (!string.IsNullOrEmpty(name) && ((season > -1 && episode > -1) || (year > 1900)));
        }

        public static void FillTrackingInfoData(Match trackingInfoMatch, ref string name, ref int season, ref int episode, ref int year)
        {
            if (trackingInfoMatch != null && trackingInfoMatch.Success)
            {
                name = trackingInfoMatch.Groups["name"].Value.Trim();
                if (!int.TryParse(trackingInfoMatch.Groups["season"].Value, out season))
                {
                    season = -1;
                }
                if (!int.TryParse(trackingInfoMatch.Groups["episode"].Value, out episode))
                {
                    episode = -1;
                }
                if (!int.TryParse(trackingInfoMatch.Groups["year"].Value, out year))
                {
                    year = -1;
                }
            }
        }

        public override bool HasNextPage
        {
            get
            {
                return nextVideoListPageUrl != null;
            }
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return getOnePageVideoList(currCategory, nextVideoListPageUrl);
        }

        public override bool CanSearch
        {
            get
            {
                return true;
            }
        }

        public override List<ISearchResultItem> DoSearch(string query)
        {
            List<ISearchResultItem> cats = new List<ISearchResultItem>();

            Regex r = new Regex(@"<tr><td\svalign=""top"">\s*<a\stitle=""[^""]*""\shref=""(?<url>[^""]*)"">\s*(?:<img\ssrc=""(?<thumb>[^""]*)"">\s*)?</a>\s*</td>\s*<td\svalign=""top"">\s*<a[^>]*><b>(?<title>[^<]*)</b></a>\s*<br>\s*<b>Description:</b>(?<description>[^<]*)</td>", defaultRegexOptions);

            string webData = GetWebData(baseUrl + "/search/" + query, forceUTF8: true);
            Match m = r.Match(webData);
            while (m.Success)
            {
                RssLink cat = new RssLink();
                cat.Url = m.Groups["url"].Value;
                if (!string.IsNullOrEmpty(dynamicSubCategoryUrlFormatString)) cat.Url = string.Format(dynamicSubCategoryUrlFormatString, cat.Url);
                cat.Url = ApplyUrlDecoding(cat.Url, dynamicSubCategoryUrlDecoding);
                if (!Uri.IsWellFormedUriString(cat.Url, System.UriKind.Absolute)) cat.Url = new Uri(new Uri(baseUrl), cat.Url).AbsoluteUri;
                cat.Name = HttpUtility.HtmlDecode(m.Groups["title"].Value.Trim());
                cat.Thumb = m.Groups["thumb"].Value;
                if (!String.IsNullOrEmpty(cat.Thumb) && !Uri.IsWellFormedUriString(cat.Thumb, System.UriKind.Absolute)) cat.Thumb = new Uri(new Uri(baseUrl), cat.Thumb).AbsoluteUri;
                cat.Description = m.Groups["description"].Value;
                cat.Other = Depth.Series;
                cat.HasSubCategories = true;
                cats.Add(cat);
                m = m.NextMatch();
            }

            return cats;
        }

        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            if (video.Other is ITrackingInfo)
                return video.Other as ITrackingInfo;

            return base.GetTrackingInfo(video);
        }

        public override string GetFileNameForDownload(VideoInfo video, Category category, string url)
        {
            if (string.IsNullOrEmpty(url)) // called for adding to favorites
                return video.Title;
            else // called for downloading
            {
                string name = base.GetFileNameForDownload(video, category, url);
                string extension = Path.GetExtension(name);
                if (String.IsNullOrEmpty(extension) || !OnlineVideoSettings.Instance.VideoExtensions.ContainsKey(extension))
                    name += ".flv";
                if (category.ParentCategory != null && !category.Other.Equals(Depth.BareList))
                {
                    string season = category.Name.Split('(')[0];
                    name = category.ParentCategory.Name + ' ' + season + ' ' + name;
                    int l;
                    do
                    {
                        l = name.Length;
                        name = name.Replace("  ", " ");
                    } while (l != name.Length);

                }
                return Utils.GetSaveFilename(name);
            }
        }

        protected override CookieContainer GetCookie()
        {
            return cc;
        }

        public override string getUrl(VideoInfo video)
        {
            GetBaseCookie();
            string dummy = GetWebData(video.VideoUrl, cc); //needed for getting results in getplaybackoptions
            string oldUrl = video.VideoUrl;
            Match m2 = Regex.Match(video.VideoUrl, @"-(?<id>\d+).html");
            if (m2.Success)
            {
                fileUrlPostString = "q=" + m2.Groups["id"].Value + "&domain=all";
                video.VideoUrl = baseUrl + "/getlinks.php";
            }
            string res = base.getUrl(video);
            video.VideoUrl = oldUrl;
            return res;
        }

        public override string ResolveVideoUrl(string url)
        {

            string webData = GetWebData(url, cc, forceUTF8: true);

            url = Regex.Match(webData, @"<a\sclass=""myButton""\shref=""(?<url>[^""]*)""[^>]*>Click\sHere\sto\sPlay").Groups["url"].Value;
            url = GetRedirectedUrl(url);
            if (url.StartsWith(baseUrl))
                return String.Empty;
            return GetVideoUrl(url);
        }


        private static string GetSubString(string s, string start, string until)
        {
            int p = s.IndexOf(start);
            if (p == -1) return String.Empty;
            p += start.Length;
            if (until == null) return s.Substring(p);
            int q = s.IndexOf(until, p);
            if (q == -1) return s.Substring(p);
            return s.Substring(p, q - p);
        }

    }

}

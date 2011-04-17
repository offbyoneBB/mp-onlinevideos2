using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites.Pondman.IMDb {

    using HtmlAgilityPack;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using OnlineVideos.Sites.Pondman.IMDb.Json;
    using OnlineVideos.Sites.Pondman.IMDb.Model;
    using System.Web;    

    public static class IMDbAPI
    {

        #region Regular Expression Patterns

        static Regex videoTitleExpression = new Regex(@"^(?<filename>(.+?)_V1)(.+?)150_ZA(?<title>[^,]+),4(.+?)1_ZA(?<length>[\d:]+),164(.+?)(?<ext>\.[^\.]+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex videoFormatExpression = new Regex(@"case\s+'(?<format>[^']+)'\s+:\s+url = '(?<video>/video/[^']+)'", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex videoFileExpression = new Regex(@"IMDbPlayer.playerKey = ""(?<video>[^\""]+)""", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex videoRTMPExpression = new Regex(@"so.addVariable\(""file"", ""(?<video>[^\""]+)""", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex videoRTMPIdExpression = new Regex(@"so.addVariable\(""id"", ""(?<video>[^\""]+)""", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex imdbIdExpression = new Regex(@"tt\d{7}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex trailerDataExpression = new Regex(@"<span class=.t-o-d-year.>\((?<year>\d{4})\)</span>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        #endregion

        /// <summary>
        /// Returns a new configurable session you can use to query the api
        /// </summary>
        /// <returns></returns>
        public static Session GetSession()
        {
            Session session = new Session();
            session.Settings = new Settings();
            return session;
        }

        /// <summary>
        /// Searches for titles and names matching the given keywords.
        /// </summary>
        /// <param name="session">a session instance.</param>
        /// <param name="query">the search query keywords.</param>
        /// <returns></returns>
        public static SearchResults Search(Session session, string query) {
           
            string response = GetSearchResponse(session, query);
            JObject parsedResults = JObject.Parse(response);
            IList<JToken> results = parsedResults["data"]["results"].Children().ToList();

            SearchResults searchResults = new SearchResults();
            foreach (JToken result in results) {
                if (result.SelectToken("list[0].tconst") != null)
                {
                    // we are dealing with Title results
                    try
                    {
                        IMDbTitleList list = JsonConvert.DeserializeObject<IMDbTitleList>(result.ToString());
                        List<TitleReference> titles = new List<TitleReference>();
                        foreach (IMDbTitleListItem item in list.Titles)
                        {
                            TitleReference title = new TitleReference();
                            title.session = session;
                            title.FillFrom(item);
                            titles.Add(title);
                        }

                        switch (list.Label)
                        {
                            case "Popular Titles":
                                searchResults.Titles.Add(ResultType.Popular, titles);
                                break;
                            case "Titles (Exact Matches)":
                                searchResults.Titles.Add(ResultType.Exact, titles);
                                break;
                            case "Titles (Partial Matches)":
                                searchResults.Titles.Add(ResultType.Partial, titles);
                                break;
                        }
                    }
                    catch(Exception e)
                    {
                        // todo: add logging!
                    }
                }
                else
                {
                    // we are dealing with Name results
                    IMDbNameList list = JsonConvert.DeserializeObject<IMDbNameList>(result.ToString());
                    List<NameReference> names = new List<NameReference>();
                    foreach (IMDbNameListItem item in list.Names)
                    {
                        NameReference name = new NameReference();
                        name.session = session;
                        name.FillFrom(item);
                        names.Add(name);
                    }

                    switch (list.Label)
                    {
                        case "Popular Names":
                            searchResults.Names.Add(ResultType.Popular, names);
                            break;
                        case "Names (Exact Matches)":
                            searchResults.Names.Add(ResultType.Exact, names);
                            break;
                        case "Names (Partial Matches)":
                            searchResults.Names.Add(ResultType.Partial, names);
                            break;
                    }
                }                
            }

            return searchResults;
        }

        /// <summary>
        /// Gets the IMDb title
        /// </summary>
        /// <param name="session">a session instance.</param>
        /// <param name="imdbID">IMDb ID</param>
        /// <returns></returns>
        public static TitleDetails GetTitle(Session session, string imdbID) {

            Dictionary<string, string> d = new Dictionary<string, string>();
            d.Add("tconst", imdbID);

            string responseString = GetResponseFromEndpoint(session, session.Settings.TitleDetails, d);

            IMDbResponse response = JsonConvert.DeserializeObject<IMDbResponse>(responseString);
            string jsonString = response.Data.ToString();
            IMDbTitleDetails dto = JsonConvert.DeserializeObject<IMDbTitleDetails>(jsonString);

            TitleDetails details = new TitleDetails();
            details.session = session;
            details.FillFrom(dto);    

            return details;
        }

        /// <summary>
        /// Gets the IMDb name
        /// </summary>
        /// <param name="session">Session instance.</param>
        /// <param name="imdbID">IMDb ID</param>
        /// <returns></returns>
        public static NameDetails GetName(Session session, string imdbID)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the videos associated with this title (video gallery).
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="imdbID">The imdb ID.</param>
        /// <remarks>uses web scraping</remarks>
        /// <returns></returns>
        public static List<VideoReference> GetVideos(Session session, string imdbID)
        {
            List<VideoReference> videos = new List<VideoReference>();
            string data = session.MakeRequest(string.Format(session.Settings.VideoGallery, imdbID));
            HtmlNode root = Utility.ToHtmlNode(data);

            if (root != null)
            {
                HtmlNodeCollection nodes = root.SelectNodes("//div[@class='results-item']");

                string movieTitle = root.SelectSingleNode("//h1/a").InnerText;

                if (nodes != null)
                {
                    foreach (HtmlNode node in nodes)
                    {
                        HtmlNode v = node.FirstChild.SelectSingleNode("a/img[@class='video']");

                        string src = v.Attributes["src"].Value;
                        Match m = videoTitleExpression.Match(src);
                        if (!m.Success) 
                        {
                            continue;
                        }

                        VideoReference video = new VideoReference();
                        video.session = session;

                        string desc = v.Attributes["title"].Value;
                        int i = desc.IndexOf(" -- ");
                        if (i >= 0)
                        {
                            desc = desc.Substring(i + 4);
                        }

                        string vconst = v.Attributes["viconst"].Value;
                        string title = node.FirstChild.SelectSingleNode("h2/a").InnerText;

                        if (title.ToLower().Trim() == movieTitle.ToLower().Trim())
                        {
                            // if the title is the same as the movie title we will use the video type as the title
                            title = HttpUtility.UrlDecode(m.Groups["title"].Value);
                        }
                        else
                        {
                            // clean up the video title
                            i = title.IndexOf(" -- ");
                            if (i >= 0)
                            {
                                title = title.Substring(i + 4);
                            }

                            title = title.Replace(movieTitle + ":", string.Empty);
                            title = HttpUtility.HtmlDecode(title.Trim());
                        }

                        video.ID = v.Attributes["viconst"].Value;
                        video.Title = title;
                        video.Description = HttpUtility.UrlDecode(desc);
                        video.Image = m.Groups["filename"].Value + m.Groups["ext"].Value;

                        string length = m.Groups["length"].Value;
                        video.Duration = new TimeSpan(0, int.Parse(length.Split(':')[0]), int.Parse(length.Split(':')[1]));

                        videos.Add(video);
                    }
                }
            }

            return videos;
        }

        /// <summary>
        /// Gets the IMDb video
        /// </summary>
        /// <param name="session">Session instance.</param>
        /// <param name="imdbID">IMDb ID</param>
        /// <returns></returns>
        public static VideoDetails GetVideo(Session session, string imdbID)
        {
            VideoDetails details = new VideoDetails();
            details.session = session;
            details.ID = imdbID;

            string data = session.MakeRequest(string.Format(session.Settings.VideoInfo, imdbID));
            HtmlNode root = Utility.ToHtmlNode(data);

            if (root == null)
            {
                return details;
            }

            HtmlNode titleNode = root.SelectSingleNode("//title");
            string title = titleNode.InnerText;
            title = title.Substring(title.IndexOf(':') + 1).Trim();

            details.Title = HttpUtility.HtmlDecode(title);

            HtmlNode descNode = root.SelectSingleNode("//table[@id='video-details']/tr[1]/td[2]");
            details.Description = HttpUtility.HtmlDecode(descNode.InnerText);

            HtmlNode formats = root.SelectSingleNode("//div[@id='hd-ctrl']");
                
            MatchCollection matches = videoFormatExpression.Matches(data);
            foreach (Match m in matches)
            {
                string format = m.Groups["format"].Value;
                string video = session.Settings.BaseUri + m.Groups["video"].Value;

                if (!video.Contains("?uff"))
                {
                    video = video + "?uff=1";
                }

                VideoFormat f = VideoFormat.SD;

                if (formats != null)
                {
                    switch (format.ToLower())
                    {
                        case "480p":
                            f = VideoFormat.HD480;
                            break;
                        case "720p":
                            f = VideoFormat.HD720;
                            break;
                    }

                    details.Files.Add(f, video);
                }
                else
                {
                    details.Files.Add(f, video);
                    break;
                }
            }

            return details;
        }

        /// <summary>
        /// Gets the IMDB video playback url.
        /// </summary>
        /// <param name="session">Session instance.</param>
        /// <param name="url">The player url.</param>
        /// <returns></returns>
        public static string GetVideoFile(Session session, string url)
        {
            string data = session.MakeRequest(url);
            Match match = videoFileExpression.Match(data);
            if (match.Success)
            {
                return match.Groups["video"].Value;
            }

            match = videoRTMPExpression.Match(data);
            if (match.Success)
            {
                string value = match.Groups["video"].Value;

                match = videoRTMPIdExpression.Match(data);
                string file = match.Groups["video"].Value;
                
                return System.Web.HttpUtility.UrlDecode(value + "/" + file);
            }            

            return null;
        }

        #region Browsing

        public static List<TitleReference> GetComingSoon(Session session)
        {
            string responseString = GetResponseFromEndpoint(session, session.Settings.FeatureComingSoon);
            JObject parsedResults = JObject.Parse(responseString);
            IList<JToken> results = parsedResults["data"]["list"]["list"].Children().ToList();

            List<TitleReference> titles = new List<TitleReference>();

            foreach (JToken result in results)
            {
                IMDbReleaseList list = JsonConvert.DeserializeObject<IMDbReleaseList>(result.ToString());
                foreach (IMDbTitleListItem item in list.Titles) 
                {
                    TitleReference title = new TitleReference();
                    title.session = session;
                    title.FillFrom(item);
                    title.ReleaseDate = list.ReleaseDate;

                    titles.Add(title);
                }
            }

            return titles;
        }

        public static List<TitleReference> GetTop250(Session session)
        {
            return GetChart(session, session.Settings.ChartTop250);
        }

        public static List<TitleReference> GetBottom100(Session session)
        {
            return GetChart(session, session.Settings.ChartBottom100);
        }

        public static List<TitleReference> GetTrailersTopHD(Session session)
        {
            return GetTrailers(session, session.Settings.TrailersTopHD);
        }

        public static List<TitleReference> GetTrailersRecent(Session session)
        {
            return GetTrailers(session, session.Settings.TrailersRecent);
        }

        public static List<TitleReference> GetTrailersPopular(Session session)
        {
            return GetTrailers(session, session.Settings.TrailersPopular);
        }

        /// <summary>
        /// Gets the full length movies in a specific category.
        /// </summary>
        /// <param name="session">session to use</param>
        /// <param name="index">a single character in the range: # and A-Z</param>
        /// <returns></returns>
        public static List<TitleReference> GetFullLengthMovies(Session session, string index)
        {
            List<TitleReference> titles = new List<TitleReference>();
            
            string uri = string.Format(session.Settings.FullLengthMovies, index);
            HtmlNode data = GetResponseFromSite(session, uri);

            HtmlNodeCollection nodes = data.SelectNodes("//a[contains(@href,'title/tt')]");

            foreach (HtmlNode node in nodes)
            {
                string href = node.Attributes["href"].Value;
                string tt = GetTitleConstFromInput(href);

                if (tt != null)
                {
                    TitleReference title = new TitleReference();
                    title.Type = TitleType.Movie;
                    title.session = session;
                    title.ID = tt;
                    title.Title = node.InnerText;
                    titles.Add(title);
                }
            }

            return titles;
        }

        /// <summary>
        /// Gets the IMDb id from the input string.
        /// </summary>
        /// <param name="input">a string</param>
        /// <returns>IMDb ID or null if not matched</returns>
        public static string GetTitleConstFromInput(string input)
        {
            string output = null;

            Match match = imdbIdExpression.Match(input);
            if (!match.Success)
            {
                return output;
            }

            return match.Value;
        }

        #endregion

        #region internal methods

        internal static List<TitleReference> GetTrailers(Session session, string uri)
        {
            List<TitleReference> titles = new List<TitleReference>();
            string response = session.MakeRequest(uri);
            JObject parsedResults = JObject.Parse(response);

            IMDbTrailer[] trailerList = JsonConvert.DeserializeObject<IMDbTrailer[]>(parsedResults["videos"].ToString());

            foreach (IMDbTrailer item in trailerList)
            {
                TitleReference title = new TitleReference();
                title.session = session;
                title.FillFrom(item);

                Match match = trailerDataExpression.Match(item.PopupHTML);
                if (match.Success)
                {
                    int year = int.MinValue;
                    if (int.TryParse(match.Groups["year"].Value, out year))
                    {
                        title.Year = year;
                    }
                }

                titles.Add(title);
            }

            return titles;
        }

        internal static List<TitleReference> GetChart(Session session, string chart)
        {
            List<TitleReference> titles = new List<TitleReference>();

            string response = GetResponseFromEndpoint(session, chart);
            JObject parsedResults = JObject.Parse(response);
            IMDbChartList chartList = JsonConvert.DeserializeObject<IMDbChartList>(parsedResults["data"]["list"].ToString());

            foreach (IMDbTitle item in chartList.Titles)
            {
                TitleReference title = new TitleReference();
                title.session = session;
                title.FillFrom(item);

                titles.Add(title);
            }

            return titles;
        }

        internal static string GetSearchResponse(Session session, string query)
        {
            var d = new Dictionary<string, string>();
            d.Add("q", Utility.UrlEncode(query));
            string response = GetResponseFromEndpoint(session, "find", d);
            return response;
        }

        internal static HtmlNode GetResponseFromSite(Session session, string url)
        {
            string data = session.MakeRequest(url);
            HtmlNode node = Utility.ToHtmlNode(data);

            return node;
        }            

        internal static string GetResponseFromEndpoint(Session session, string target)
        {
            return GetResponseFromEndpoint(session, target, null);
        }

        internal static string GetResponseFromEndpoint(Session session, string target, Dictionary<string, string> args) {

            if (session == null)
            {
                throw new ArgumentNullException("session", "session object needed");
            }

            // build the querystring
            string query = string.Empty;

            if (args != null)
            {
                foreach (KeyValuePair<string, string> kvp in args)
                {
                    query += "&" + kvp.Key + "=" + kvp.Value;
                }
            }

            // format the url
            string url = string.Format(session.Settings.BaseApiUri, target, session.Settings.Locale, query);
 
            // make and return the request
            return session.MakeRequest(url);
        }

        #endregion

    }
}

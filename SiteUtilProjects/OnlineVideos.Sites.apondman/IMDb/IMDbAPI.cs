using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites.Pondman.IMDb {

    using HtmlAgilityPack;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using OnlineVideos.Sites.Pondman.IMDb.Model;
    using System.Web;
    using System.Globalization;
    using OnlineVideos.Sites.apondman.IMDb.DTO;
    using OnlineVideos.Sites.Pondman.IMDb.Json;    

    public static class IMDbAPI
    {

        #region Regular Expression Patterns

        static Regex videoTitleExpression = new Regex(@"^(?<filename>(.+?)_V1)(.+?)255,1_ZA(?<length>[\d:]+)(.+?)(?<ext>\.[^\.]+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex videoFormatExpression = new Regex(@"case\s+'(?<format>[^']+)'\s+:\s+url = '(?<video>/video/[^']+)'", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex videoFileExpression = new Regex(@"IMDbPlayer.playerKey = ""(?<video>[^\""]+)""", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex videoPlayerExpression = new Regex(@"IMDbPlayer.playerType = ['""](?<player>[^'""]+)['""]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		static Regex videoPlayerJsonExpression = new Regex(@"<script class=""imdb-player-data"" type=""text/imdb-video-player-json"">(?<json>.*?)</script>", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

        // todo: create single expression for RTMP variables
        static Regex videoRTMPExpression = new Regex(@"so.addVariable\(""file"", ""(?<video>[^\""]+)""", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex videoRTMPIdExpression = new Regex(@"so.addVariable\(""id"", ""(?<video>[^\""]+)""", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex videoThunderExpression = new Regex(@"so.addVariable\(""releaseURL"", ""(?<video>[^\""]+)""", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
        static Regex imdbIdExpression = new Regex(@"tt\d{7}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex trailerDataExpression = new Regex(@"<span class=.t-o-d-year.>\((?<year>\d{4})\)</span>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        static Regex imdbTitleExpression = new Regex(@"(?<title>[^\(]+?)\((?<year>\d{4})[\/IVX]*(?<type>[^\)]*)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex imdbImageExpression = new Regex(@"^(?<filename>(.+?)_V1)(.+?)(?<ext>\.[^\.]+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        #endregion

        #region Public methods

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
        public static SearchResults Search(Session session, string query)
        {
            var d = new Dictionary<string, string>();
            d.Add("q", query);

            string uri = string.Format(session.Settings.BaseUriMobile, session.Settings.SearchMobile, "?{0}");
            HtmlNode root = GetResponseFromSite(session, uri, d);

            SearchResults results = new SearchResults();
            List<TitleReference> titles = new List<TitleReference>();

            HtmlNodeCollection nodes = root.SelectNodes("//div[@class='poster ']");
            foreach(HtmlNode node in nodes) {
                
                HtmlNode titleNode = node.SelectSingleNode("div[@class='label']/div[@class='title']/a[contains(@href,'title')]");
                if (titleNode == null) 
                {
                    continue;
                }
                
                TitleReference title = new TitleReference();
                title.session = session;
                title.ID = titleNode.Attributes["href"].Value.Replace("/title/", "").Replace("/", "");
               
                ParseDisplayStringToTitleBase(title, titleNode.ParentNode.InnerText);

                HtmlNode detailNode = node.SelectSingleNode("div[@class='label']/div[@class='detail']");
                if (detailNode != null)
                {
                    string[] actors = detailNode.InnerText.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string actor in actors)
                    {
                        // todo: the name reference is missing an id
                        title.Principals.Add(new NameReference { Name = actor });
                    }
                }

                HtmlNode imageNode = node.DescendantNodes().Where(x => x.Name == "img").FirstOrDefault();
                if (imageNode != null) {
                    Match match = imdbImageExpression.Match(imageNode.Attributes["src"].Value);
                    if (match.Success)
                    {
                        title.Image = HttpUtility.UrlDecode(match.Groups["filename"].Value + match.Groups["ext"].Value);
                    }
                }

                titles.Add(title);
            }
            
            // todo: this is abuse, need to split the title results using the h1 headers later
            results.Titles[ResultType.Exact] = titles;

            return results;
        }

        /// <summary>
        /// Gets the IMDb title
        /// </summary>
        /// <param name="session">a session instance.</param>
        /// <param name="imdbID">IMDb ID</param>
        /// <returns></returns>
        public static TitleDetails GetTitle(Session session, string imdbID) {
            
            string uri = string.Format(session.Settings.BaseUriMobile, session.Settings.TitleDetailsMobile, "/" + imdbID + "/");
            
            HtmlNode root = GetResponseFromSite(session, uri);

            TitleDetails title = new TitleDetails();
            title.session = session;
            title.ID = imdbID;

            // Main Details
            HtmlNode node = root.SelectSingleNode("//div[@class='media-body']");
            string titleInfo = node.SelectSingleNode("h1").InnerText;

            ParseDisplayStringToTitleBase(title, HttpUtility.HtmlDecode(titleInfo));
  
            // Tagline
            node = node.SelectSingleNode("p");
            if (node != null)
            {
                title.Tagline = HttpUtility.HtmlDecode(node.InnerText);
            }

            // Release date
            node = root.SelectSingleNode("//div[h1='Release Date']/p");
            if (node != null)
            {
                DateTime value;
                if (DateTime.TryParse(node.InnerText, out value))
                {
                    title.ReleaseDate = value;
                }
            }

            // Summary
            node = root.SelectSingleNode("//div[h1='Plot Summary']/p");
            if (node != null)
            {
                node = node.FirstChild;
                if (node != null)
                {
                    title.Plot = HttpUtility.HtmlDecode(node.InnerText);
                }
            }

            // Genres
            node = root.SelectSingleNode("//div[h1='Genre']/p");
            if (node != null)
            {
                string[] genres = node.InnerText.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                title.Genres = genres.Select(s => HttpUtility.HtmlDecode(s)).ToList();
            }

            // Rating
            node = root.SelectSingleNode("//p[@class='votes']/strong");
            if (node != null)
            {
                title.Rating = Convert.ToDouble(node.InnerText, CultureInfo.InvariantCulture.NumberFormat);

                // Votes
                string votes = node.NextSibling.InnerText;
                if (votes.Contains("votes"))
                {
                    votes = Regex.Replace(votes, @".+?([\d\,]+) votes.+", "$1", RegexOptions.Singleline);
                    title.Votes = Convert.ToInt32(votes.Replace(",",""));
                }
            }

            


            // Certification
            node = root.SelectSingleNode("//div[h1='Rated']/p");
            if (node != null)
            {
                title.Certificate = HttpUtility.HtmlDecode(node.InnerText);
            }

            //Poster
            node = root.SelectSingleNode("//div[@class='poster']/a");
            if (node != null)
            {
                Match match = imdbImageExpression.Match(node.Attributes["href"].Value);
                if (match.Success)
                {
                    title.Image = HttpUtility.UrlDecode(match.Groups["filename"].Value + match.Groups["ext"].Value);
                }
            }

            // Cast
            HtmlNodeCollection nodes = root.SelectNodes("//section[@class='topCast posters']/div");
            if (nodes != null)
            {
                foreach (HtmlNode n in nodes)
                {
                    HtmlNode infoNode = n.SelectSingleNode("div[@class='label']/div[@class='detail']");
                    if (infoNode == null) 
                    {
                        continue;
                    }

                    // Character info
                    Character character = new Character();
                    character.Name = HttpUtility.HtmlDecode(infoNode.InnerText);
                    character.Actor = new NameReference();
                    character.Actor.session = session; 

                    infoNode = n.SelectSingleNode("div[@class='label']/div[@class='title']/a");
                    if (infoNode != null) 
                    {
                        character.Actor.ID = infoNode.Attributes["href"].Value.Replace("/name/","").Replace("/","");
                        character.Actor.Name = HttpUtility.HtmlDecode(infoNode.InnerText);
                    }

                    infoNode = n.SelectSingleNode("img");
                    if (infoNode != null)
                    {
                        Match match = imdbImageExpression.Match(infoNode.Attributes["src"].Value);
                        if (match.Success)
                        {
                            character.Actor.Image = HttpUtility.UrlDecode(match.Groups["filename"].Value + match.Groups["ext"].Value);
                        }
                    }

                    // add character object to the title
                    title.Cast.Add(character);
                }
            }

            nodes = root.SelectNodes("//section[@class='topCrew']/div");
            if (nodes != null)
            {
                foreach (HtmlNode n in nodes)
                {
                    HtmlNode headerNode = n.SelectSingleNode("h1");
                    if (headerNode == null)
                    {
                        continue;
                    }

                    string header = headerNode.InnerText.Trim();

                    HtmlNodeCollection personNodes = n.SelectNodes("p/a");
                    foreach (HtmlNode personNode in personNodes)
                    {
                        NameReference person = new NameReference();
                        person.session = session;
                        person.ID = personNode.Attributes["href"].Value.Replace("/name/","").Replace("/","");
                        person.Name = HttpUtility.HtmlDecode(personNode.InnerText);
                        
                        if (header.StartsWith("Director")) 
                        {
                            title.Directors.Add(person);
                        } 
                        else if (header.StartsWith("Writer")) 
                        {
                            title.Writers.Add(person);
                        }
                    }
                }
            }

            HtmlNode trailerNode = root.SelectSingleNode("//span[@data-trailer-id]");
            if (trailerNode != null) 
            {
                string videoId = trailerNode.GetAttributeValue("data-trailer-id", string.Empty);
                if (videoId != string.Empty) 
                {
                    title.trailer = videoId;
                }
            }

            return title;
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
                        HtmlNode v = node.FirstChild.SelectSingleNode("a/img");

                        if (v == null)
                        {
                            continue;
                        }

						var attr = v.Attributes["loadlate"];
                        Match m = Match.Empty;

                        if (attr != null) 
                        {
                            // the src is url encode twice so we decode it twice before parsing
                            string src = HttpUtility.UrlDecode(HttpUtility.UrlDecode(attr.Value));
                            m = videoTitleExpression.Match(src);
                        }

                        if (!m.Success)
                        {
                            continue;
                        }

                        VideoReference video = new VideoReference();
                        video.session = session;

                        string desc = HttpUtility.UrlDecode(v.Attributes["title"].Value);
                        int i = desc.IndexOf(" -- ");
                        if (i >= 0)
                        {
                            desc = desc.Substring(i + 4);
                        }

                        string vconst = v.Attributes["viconst"].Value;
                        string title = node.FirstChild.SelectSingleNode("h2/a").InnerText;
						
						string href = node.FirstChild.SelectSingleNode("h2/a").GetAttributeValue("href","");
						if (href.Contains("/imdblink/"))
						{
							continue; // link to an external trailer
						}

                        if (title.ToLower().Trim() == movieTitle.ToLower().Trim())
                        {
                            // if the title is the same as the movie title try the image's title
							title = v.GetAttributeValue("title", title);
                        }                        

                        // clean up the video title
                        i = title.IndexOf(" -- ");
                        if (i >= 0)
                        {
                            title = title.Substring(i + 4);
                        }

                        title = title.Replace(movieTitle + ":", string.Empty).Trim();

                        video.ID = vconst;
                        video.Title = HttpUtility.HtmlDecode(title);
                        video.Description = HttpUtility.HtmlDecode(HttpUtility.UrlDecode(desc));
                        video.Image = m.Groups["filename"].Value + m.Groups["ext"].Value;
						
						// when parsing the length make sure it is set as we expect, otherwise one error makes all clips inaccessible
                        string length = m.Groups["length"].Value.Trim();
						if (!string.IsNullOrEmpty(length))
						{
							string[] splits = length.Split(':');
							if (splits.Length > 1)
							{
								video.Duration = new TimeSpan(0, int.Parse(length.Split(':')[0]), int.Parse(length.Split(':')[1]));
							}
							else
							{
								video.Duration = TimeSpan.FromSeconds(int.Parse(splits[0]));
							}
						}

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

            string url = string.Format(session.Settings.VideoInfo, imdbID);
            string data = session.MakeRequest(url);
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

            string[] formats = null;
            
            MatchCollection matches = videoFormatExpression.Matches(data);
            foreach (Match m in matches)
            {
                string format = m.Groups["format"].Value;
                string video = m.Groups["video"].Value;

				if (formats == null)
				{
					string videoPlayerHtml = session.MakeRequest(session.Settings.BaseUri + video);
					var json = GetJsonForVideo(videoPlayerHtml);
					if (json != null)
					{
						formats = json["titleObject"]["encoding"].Select(q => (q as JProperty).Name).Where(q => q != "selected").ToArray();
					}
				}

				if (formats != null)
                {
                    switch (format)
                    {
						case "SD":
							if (formats.Contains("240p"))
								details.Files[VideoFormat.SD] = video;
                            break;
                        case "480p":
							if (formats.Contains("480p"))
								details.Files[VideoFormat.HD480] = video;
                            break;
                        case "720p":
							if (formats.Contains("720p"))
								details.Files[VideoFormat.HD720] = video;
                            break;
                    }

                    
                }
                else
                {
                    details.Files[VideoFormat.SD] = video;
                    break;
                }
            }

            if (details.Files.Count == 0)
            {
                details.Files.Add(VideoFormat.SD, "/video/screenplay/" + imdbID + "/player");
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
            if (!url.StartsWith("/"))
            {
                // we only need to post-process the relative urls
                return url;
            }

            string response = session.MakeRequest(session.Settings.BaseUri + url);

            Match match = videoPlayerExpression.Match(response);
            if (match.Success)
            {
                string player = match.Groups["player"].Value;
                switch(player) 
                {
                    case "thunder":
                        match = videoThunderExpression.Match(response);
                        if (match.Success)
                        {
                            string smilURL = match.Groups["video"].Value;
                            HtmlNode node = GetResponseFromSite(session, smilURL);
                            HtmlNode rtmp = node.SelectSingleNode("//ref/@src[contains(.,'rtmp:')]");
                            if (rtmp != null)
                            {
                                return HttpUtility.UrlDecode(rtmp.Attributes["src"].Value);
                            }
                        }
                        break;

                    // todo add more player types
                }
            }            

            match = videoFileExpression.Match(response);
            if (match.Success)
            {
                return match.Groups["video"].Value;
            }
            

            match = videoRTMPExpression.Match(response);
            if (match.Success)
            {
                string value = match.Groups["video"].Value;

                match = videoRTMPIdExpression.Match(response);
                string file = match.Groups["video"].Value;
                
                return System.Web.HttpUtility.UrlDecode(value + "/" + file);
            }

			var json = GetJsonForVideo(response);
			if (json != null)
			{
				return json["videoPlayerObject"]["video"].Value<string>("url");
			}

            return null;
        }

        #region Browsing

        /// <summary>
        /// Browse the "Coming Soon" listing.
        /// </summary>
        /// <param name="session"></param>
        /// <returns>a collection of titles</returns>
        public static List<TitleReference> GetComingSoon(Session session)
        {
            return GetList(session, session.Settings.ComingSoon);
        }

        /// <summary>
        /// Browse the movie top 250.
        /// </summary>
        /// <param name="session"></param>
        /// <returns>a collection of titles</returns>
        public static List<TitleReference> GetTop250(Session session)
        {
            return GetList(session, session.Settings.ChartTop250Mobile);
        }

        /// <summary>
        /// Browse the movie bottom top 100.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public static List<TitleReference> GetBottom100(Session session)
        {
            return GetList(session, session.Settings.ChartBottom100Mobile);
        }

        /// <summary>
        /// Browse the US weekend box office results.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns></returns>
        public static List<TitleReference> GetBoxOffice(Session session)
        {
            return GetList(session, session.Settings.BoxOfficeMobile);
        }

        /// <summary>
        /// Browse the Best Picture winners
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns></returns>
        public static List<TitleReference> GetBestPictureWinners(Session session)
        {
            return GetList(session, session.Settings.BestPictureWinners);
        }

        /// <summary>
        /// Browse the MOVIEmeter movies.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns></returns>
        public static List<TitleReference> GetMovieMeter(Session session)
        {
            return GetList(session, session.Settings.ChartMovieMeterMobile);
        }

        /// <summary>
        /// Browse the popular tv series
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public static List<TitleReference> GetPopularTV(Session session)
        {
            return GetList(session, session.Settings.PopularTVSeriesMobile);
        }

        /// <summary>
        /// Browser the top HD movie trailers.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public static List<TitleReference> GetTrailersTopHD(Session session)
        {
            return GetTrailers(session, session.Settings.TrailersTopHD, 0);
        }

        /// <summary>
        /// Browse recently added movie trailers and videos
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public static List<TitleReference> GetTrailersRecent(Session session, int page)
        {
            return GetTrailers(session, session.Settings.TrailersRecent, page);
        }

        /// <summary>
        /// Browse popular movie trailers
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public static List<TitleReference> GetTrailersPopular(Session session, int page)
        {
            return GetTrailers(session, session.Settings.TrailersPopular, page);
        }

        /// <summary>
        /// Browse the full length movies in a specific category.
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
                string tt = ParseTitleConst(href);

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

        #endregion

        #region Utility methods

        /// <summary>
        /// Parses the IMDb id from the input string.
        /// </summary>
        /// <param name="input">a string</param>
        /// <returns>IMDb ID or null if not matched</returns>
        public static string ParseTitleConst(string input)
        {
            string output = null;

            Match match = imdbIdExpression.Match(input);
            if (!match.Success)
            {
                return output;
            }

            return match.Value;
        }

		static JObject GetJsonForVideo(string html)
		{
			var match = videoPlayerJsonExpression.Match(html);
			if (match.Success)
			{
				string jsonText = match.Groups["json"].Value;
				return Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(jsonText);
			}
			return null;
		}

        #endregion

        #endregion

        #region Official IMDb interface

        // todo: not used because we don't own legit api key, move into seperate class / interface

        /*
        /// <summary>
        /// Searches for titles and names matching the given keywords.
        /// </summary>
        /// <param name="session">a session instance.</param>
        /// <param name="query">the search query keywords.</param>
        /// <returns></returns>
        private static SearchResults SearchDeprecated(Session session, string query)
        {

            string response = GetSearchResponse(session, query);
            JObject parsedResults = JObject.Parse(response);
            IList<JToken> results = parsedResults["data"]["results"].Children().ToList();

            SearchResults searchResults = new SearchResults();
            foreach (JToken result in results)
            {
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
                    catch (Exception e)
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
        private static TitleDetails GetTitleDeprecated(Session session, string imdbID)
        {

            Dictionary<string, string> d = new Dictionary<string, string>();
            d.Add("tconst", imdbID);

            string data = GetResponseFromEndpoint(session, session.Settings.TitleDetails, d);
            IMDbResponse<IMDbTitleDetails> response = JsonConvert.DeserializeObject<IMDbResponse<IMDbTitleDetails>>(data);

            TitleDetails details = new TitleDetails();
            details.session = session;
            details.FillFrom(response.Data);

            return details;
        }

        /// <summary>
        /// Browse the coming soon category.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private static List<TitleReference> GetComingSoonDeprecated(Session session)
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

        /// <summary>
        /// Browse the popular tv series
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private static List<TitleReference> GetPopularTVDeprecated(Session session)
        {
            List<TitleReference> titles = new List<TitleReference>();

            string data = GetResponseFromEndpoint(session, session.Settings.PopularTVSeries);
            IMDbResponse<IMDbSingleList<IMDbTitleListItem[]>> response = JsonConvert.DeserializeObject<IMDbResponse<IMDbSingleList<IMDbTitleListItem[]>>>(data);

            foreach (IMDbTitleListItem item in response.Data.List)
            {
                TitleReference title = new TitleReference();
                title.session = session;
                title.FillFrom(item);

                titles.Add(title);
            }

            return titles;
        }

        /// <summary>
        /// Browse the US weekend box office results.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns></returns>
        private static List<TitleReference> GetBoxOfficeDeprecated(Session session)
        {
            List<TitleReference> titles = new List<TitleReference>();

            string data = GetResponseFromEndpoint(session, session.Settings.BoxOffice);

            IMDbResponse<IMDbSingleList<IMDbList<IMDbBoxOfficeTitle>>> response = JsonConvert.DeserializeObject<IMDbResponse<IMDbSingleList<IMDbList<IMDbBoxOfficeTitle>>>>(data);

            foreach (IMDbBoxOfficeTitle item in response.Data.List.Items)
            {
                if (item.Title != null)
                {
                    TitleReference title = new TitleReference();
                    title.session = session;
                    title.FillFrom(item.Title);

                    titles.Add(title);
                }
            }


            return titles;
        }

        /// <summary>
        /// Browse the MOVIEmeter movies.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns></returns>
        private static List<TitleReference> GetMovieMeterDeprecated(Session session)
        {
            List<TitleReference> titles = new List<TitleReference>();

            string data = GetResponseFromEndpoint(session, session.Settings.ChartMovieMeter);

            IMDbRankedResponse<IMDbTitleListItem> response = JsonConvert.DeserializeObject<IMDbRankedResponse<IMDbTitleListItem>>(data);

            foreach (IMDbRankedObject<IMDbTitleListItem> item in response.Data.Items)
            {
                TitleReference title = new TitleReference();
                title.session = session;
                title.FillFrom(item.Object);

                titles.Add(title);
            }


            return titles;
        }
        */
        #endregion

        #region internal methods

        /// <summary>
        /// Parses the IMDb title display string into the proper fields of a TitleBase object.
        /// </summary>
        /// <param name="title">a TitleBase object instance</param>
        /// <param name="input">the display string.</param>
        /// <returns></returns>
        internal static bool ParseDisplayStringToTitleBase(TitleBase title, string input) 
        {
            Match match = imdbTitleExpression.Match(input);
            if (match.Success)
            {
                title.Title = HttpUtility.HtmlDecode(match.Groups["title"].Value.Trim());
                title.Year = int.Parse(match.Groups["year"].Value);
                title.Type = TitleType.Unknown;

                switch (match.Groups["type"].Value.Trim())
                {
                    case "":
                        title.Type = TitleType.Movie;
                        break;
                    case "TV series":
                        title.Type = TitleType.TVSeries;
                        break;
                    case "Video game":
                        title.Type = TitleType.Game;
                        break;
                    case "Short":
                        title.Type = TitleType.Short;
                        break;
                    default:
                        title.Type = TitleType.Unknown;
                        break;
                    // todo: add more types
                }
            }

            return match.Success;
        }

        /// <summary>
        /// Parses the image URL to get the maximum resolution filename.
        /// </summary>
        /// <param name="url">the url of the image</param>
        /// <returns></returns>
        internal static string ParseImageUrl(string url) {
            if (url == null)
            {
                return string.Empty;
            }

            Match match = imdbImageExpression.Match(url);
            if (match.Success)
            {
                url = HttpUtility.UrlDecode(match.Groups["filename"].Value + match.Groups["ext"].Value);
            }

            return url;
        }

        /// <summary>
        /// Common method to get a list of titles from an IMDb trailer JSON feed
        /// </summary>
        /// <param name="session"></param>
        /// <param name="path">path to JSON feed</param>
        /// <returns>a collection of titles</returns>
        internal static List<TitleReference> GetTrailers(Session session, string uri, int token)
        {
            string url = (token > 0) ? uri + "&token=" + token : uri;
            
            List<TitleReference> titles = new List<TitleReference>();
            string response = session.MakeRequest(url);
            //JObject parsedResults = JObject.Parse(response);

            
            var imdbResponse = JsonConvert.DeserializeObject<OnlineVideos.Sites.apondman.IMDb.DTO.IMDbResponse>(response);

            HashSet<string> duplicateFilter = new HashSet<string>();
            foreach (var item in imdbResponse.model.items)
            {
                var titleId = item.display.titleId;
                if (duplicateFilter.Contains(titleId))
                {
                    continue;
                }

                duplicateFilter.Add(titleId);
                
                TitleReference title = new TitleReference();
                title.session = session;
                title.FillFrom(item);

                titles.Add(title);
            }

            return titles;
        }

        /// <summary>
        /// Common method to get a list of titles from an IMDb mobile JSON feed
        /// </summary>
        /// <param name="session"></param>
        /// <param name="path">path to JSON feed</param>
        /// <returns>a collection of titles</returns>
        internal static List<TitleReference> GetList(Session session, string path)
        {
            List<TitleReference> titles = new List<TitleReference>();

            string data = GetResponseFromEndpoint(session, path);

            IMDbMobileResponse<List<IMDbTitleMobile>> response = JsonConvert.DeserializeObject<IMDbMobileResponse<List<IMDbTitleMobile>>>(data);

            DateTime releaseDateHeader = DateTime.MinValue;
            foreach (IMDbTitleMobile item in response.Data)
            {
                if (item.URL == null)
                {
                    if (item.ReleaseDate > DateTime.MinValue)
                    {
                        releaseDateHeader = item.ReleaseDate;
                    }
                    continue;
                }
                TitleReference title = new TitleReference();
                title.session = session;
                title.FillFrom(item);
                title.ReleaseDate = releaseDateHeader;

                titles.Add(title);
            }

            return titles;
        }

        /// <summary>
        /// Common method to get a list of titles from the IMDb app interface (JSON)
        /// </summary>
        /// <param name="session"></param>
        /// <param name="chart">name of the chart</param>
        /// <returns>a collection of titles</returns>
        internal static List<TitleReference> GetChart(Session session, string chart)
        {
            List<TitleReference> titles = new List<TitleReference>();

            string data = GetResponseFromEndpoint(session, chart);
            IMDbResponse<IMDbSingleList<IMDbList<IMDbTitle>>> response = JsonConvert.DeserializeObject<IMDbResponse<IMDbSingleList<IMDbList<IMDbTitle>>>>(data);
            foreach (IMDbTitle item in response.Data.List.Items)
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
            d.Add("q", query);
            string response = GetResponseFromEndpoint(session, "find", d);
            return response;
        }

        internal static HtmlNode GetResponseFromSite(Session session, string url)
        {
            return GetResponseFromSite(session, url, null);
        }

        internal static HtmlNode GetResponseFromSite(Session session, string url, Dictionary<string, string> args)
        {
            if (session == null)
            {
                throw new ArgumentNullException("session", "session object needed");
            }

            string uri = url;
            string query = CreateQuerystringFromDictionary(args);
            
            // create the uri
            uri = string.Format(url, query);

            string data = session.MakeRequest(uri);
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
            string query = CreateQuerystringFromDictionary(args);

            // format the url
            //string url = string.Format(session.Settings.BaseApiUri, target, session.Settings.Locale, query);
            string url = string.Format(session.Settings.BaseUriMobile, target, query);
 
            // make and return the request
            return session.MakeRequest(url);
        }

        internal static string CreateQuerystringFromDictionary(Dictionary<string, string> args)
        {
            string queryString = string.Empty;
            if (args != null)
            {
                foreach (KeyValuePair<string, string> kvp in args)
                {
                    queryString += "&" + Utility.UrlEncode(kvp.Key) + "=" + Utility.UrlEncode(kvp.Value);
                }
            }

            return queryString;
        }

        #endregion

    }
}

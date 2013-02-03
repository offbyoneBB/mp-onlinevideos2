using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OnlineVideos.Sites.Pondman.ITunes;
using OnlineVideos.Sites.Pondman.ITunes.DTO;
using OnlineVideos.Sites.Pondman.Interfaces;
using HtmlAgilityPack;
using OnlineVideos.Sites.Pondman.Nodes;

namespace OnlineVideos.Sites.Pondman.ITunes.Nodes {
    public class Movie : ExternalContentNodeBase, IVideoDetails
    {

        public string Title { get; set; }

        public string Plot { get; set; }

        public DateTime ReleaseDate { get; set; }

        public int Year
        {
            get
            {
                if (ReleaseDate != DateTime.MinValue)
                {
                    return ReleaseDate.Year;
                }

                return -1;
            }
        }

        public string Studio { get; set; }

        public string Certificate { get; set; }

        public Poster Poster { get; set; }        

        public List<string> Genres {
            get {
                if (_genres == null) {
                    _genres = new List<string>();
                }
                return _genres;
            }
            set
            {
                this._genres = value;
            }
        } List<string> _genres;

        public List<string> Actors {
            get {
                if (_actors == null) {
                    _actors = new List<string>();
                }
                return _actors;
            }
            set
            {
                this._actors = value;
            }
        } List<string> _actors;

        public List<string> Directors {
            get {
                if (_directors == null) {
                    _directors = new List<string>();
                }
                return _directors;
            }
            set
            {
                this._directors = value;
            }
        } List<string> _directors;

        public List<Video> Videos {
            get {
                if (_videos == null) {
                    _videos = new List<Video>();
                }
                return _videos;
            }
        } List<Video> _videos;

		public System.Uri OriginalLocation;

        protected HashSet<string> GetPossibleIndexLocations() {
            HashSet<string> alternatives = new HashSet<string>();
            alternatives.Add(Uri);

            // Alternative 1: Try to parse the movie foldername from the poster image filename
            if (Poster != null) {

                Match match = Regex.Match(Poster.Uri, @"/([^/]+)_(\d{12}|poster).jpg$");
                if (match.Success) {
                    string title = match.Groups[1].Value;
                    string url = Regex.Replace(Uri, @"/([^/]+)/index.xml$", "/" + title + "/index.xml");
                    alternatives.Add(url);
                }
            }

            // Alternative 2: Try to recreate movie foldername using the actual movie title
            if (Title != null) {
                string titleWithoutSpecialCharacters = Regex.Replace(Title, @"[\W\s]", "").ToLower();
                string titleWithFailedEncoding = Regex.Replace(Title, @"\s", "").ToLower();
                char[] chars = titleWithFailedEncoding.ToCharArray();

                StringBuilder result = new StringBuilder(titleWithFailedEncoding.Length + (int)(titleWithFailedEncoding.Length * 0.1));
                foreach (char c in chars) {
                    int value = Convert.ToInt32(c);
                    if (value < 48 || value > 127)
                        result.AppendFormat("{0:d3}", value);
                    else
                        result.Append(c);
                }

                titleWithFailedEncoding = result.ToString();

                foreach (string alternative in alternatives.ToList()) {
                    // title without special characters
                    string url = Regex.Replace(alternative, @"/([^/]+)/index.xml$", "/" + titleWithoutSpecialCharacters + "/index.xml");
                    alternatives.Add(url);
                    
                    // title with failed encoding (as seen in the source sometimes)
                    url = Regex.Replace(alternative, @"/([^/]+)/index.xml$", "/" + titleWithFailedEncoding + "/index.xml");
                    alternatives.Add(url);
                }
            }


            // Alternative 3: Try to replace the studio
            if (!String.IsNullOrEmpty(Studio)) {
                string studio = Regex.Replace(Studio, @"[\W\s]", "").ToLower();
                foreach (string alternative in alternatives.ToList()) {
                    string url = Regex.Replace(alternative, @"/s/([^/]+)/", "/s/" + studio + "/");
                    alternatives.Add(url);
                }
            }

            // return all url alternatives
            return alternatives;
        }

        public override NodeResult Update()
        {
            HtmlNode verificationNode = null;
            string url = this.uri;
            string data = this.session.MakeRequest(url);
            HtmlNode root = Utility.ToHtmlNode(data);

            if (root != null)
            {
                verificationNode = root.SelectSingleNode("//pathelement[3]");
            }

            // if we don't have a valid result we are going to try some alternatives
            if (verificationNode == null)
            {
                HashSet<string> alternatives = this.GetPossibleIndexLocations();
                foreach (string alternative in alternatives)
                {
                    // we skip the original uri as we already tried that
                    if (alternative == url)
                    {
                        continue;
                    }

                    data = this.session.MakeRequest(alternative);
                    root = Utility.ToHtmlNode(data);

                    // if we found some valid xml break the loop
                    if (root != null)
                    {
                        verificationNode = root.SelectSingleNode("//pathelement[3]");
                        if (verificationNode == null)
                        {
                            url = alternative;
                            break;
                        }
                    }
                }

                // if we exhausted all alternatives and we still don't 
                // have some valid result try scraping from the OriginalLocation
                if (verificationNode == null)
                {
					return UpdateFromOriginalLocation();
                }
            }

            // Double check the 3rd PathElement node in the xml to see whether we got the
            // expected details XML. If not try to reload from the new url.
            string verifiedUrl = this.session.Config.BaseUri + verificationNode.InnerText;
            if (url != verifiedUrl)
            {
                string newData = this.session.MakeRequest(url);
                HtmlNode newRoot = Utility.ToHtmlNode(newData);
                if (newRoot != null)
                {
                    verificationNode = newRoot.SelectSingleNode("//pathelement[3]");
                    if (verificationNode != null)
                    {
                        root = newRoot;
                    }
                }
            }

            // Grab the poster
            HtmlNode Poster = root.SelectSingleNode("//pictureview/@url[contains(.,'poster')]");
            if (Poster != null && Poster.InnerText.Length > 0)
            {

                // sometimes the poster urls start relative, in that case complete the address
                string poster = (Poster.InnerText.StartsWith("/")) ? this.session.Config.BaseUri + Poster.InnerText : Poster.InnerText;

                // if we had no poster create the poster object
                if (this.Poster == null)
                {
                    this.Poster = this.session.Get<Poster>(poster);
                }

                // set this address to be the large poster
                this.Poster.Large = poster;
            }

            // Synopsis
            HtmlNodeCollection infoNodes = root.SelectNodes("//vboxview/textview/setfontstyle");
            if (infoNodes != null && infoNodes.Count > 1)
            {
                string synopsis = infoNodes[2].InnerText.Trim();
                this.Plot = HttpUtility.HtmlDecode(synopsis);
            }

            // Release Date
            HtmlNodeCollection dateNodes = root.SelectNodes("//vboxview/textview/setfontstyle[contains(b, 'Theaters:')]");
            if (dateNodes != null && dateNodes.Count == 1)
            {
                string date = dateNodes[0].InnerText;
                date = date.Replace("In Theaters:", "");
                date = Regex.Replace(date, @"[^\d]*, ", ", ");
                date = date.Trim();

                DateTime dt;
                if (DateTime.TryParse(date, out dt))
                {
                    this.ReleaseDate = dt;
                }
            }

            // Genres
            HtmlNodeCollection genreNodes = root.SelectNodes("//gotourl[contains(@url, '/moviesxml/g/')]");
            if (genreNodes != null)
            {
                this.Genres = genreNodes.ToStringList();
            }

            // Cast
            HtmlNodeCollection castNodes = root.SelectNodes("//vboxview/textview[@styleset='basic10']/setfontstyle");
            if (castNodes != null)
            {
                this.Actors = castNodes.ToStringList();
            }

            // Find all the videos for this movie.
            HtmlNodeCollection videoNodes = root.SelectNodes("//gotourl[@target='main']");
            if (videoNodes != null && videoNodes.Count > 0)
            {

                // clear videos
                this.Videos.Clear();

                HashSet<string> videoUrls = new HashSet<string>();
                Dictionary<string, string> videoTitles = new Dictionary<string, string>();

                // get the posted dates and runtime nodes
                HtmlNodeCollection postedDates = root.SelectNodes("//setfontstyle[contains(., 'Posted:')]");
                HtmlNodeCollection runtimes = root.SelectNodes("//setfontstyle[contains(., 'Runtime:')]");

                int count = 0;
                //foreach (XmlNode videoNode in videoNodes) {
                foreach (HtmlNode videoNode in videoNodes)
                {

                    // if it does not start with a specific string then ignore this url
                    string videoUrl = videoNode.Attributes["url"].Value;
                    if (!videoUrl.StartsWith("/moviesxml/s/"))
                        continue;

                    // complete the relative path
                    videoUrl = this.session.Config.FixUri(videoUrl);

                    // if we already added this url ignore it
                    if (videoUrls.Contains(videoUrl))
                        continue;

                    videoUrls.Add(videoUrl);

                    Video video = this.session.Get<Video>(videoUrl);
                    video.Title = videoNode.Attributes["draggingName"].Value;

                    // Set the duration of the video
                    string runtime = runtimes[count].InnerText.Trim();
                    runtime = runtime.Replace("Runtime: ", "");
                    string[] units = runtime.Split(':');
                    int minutes = 0;
                    int seconds = 0;
                    int.TryParse(units[0], out minutes);
                    int.TryParse(units[1], out seconds);
                    video.Duration = new TimeSpan(0, minutes, seconds);

                    // set the date of the video
                    string date = postedDates[count].InnerText.Trim();
                    date = date.Replace("Posted: ", "");
                    date = Regex.Replace(date, @"[^\d]*, ", ", ");

                    DateTime dt;
                    if (DateTime.TryParse(date, out dt))
                    {
                        video.Published = dt;
                    }

                    // add this video to the movie
                    this.Videos.Add(video);
                    count++;
                }
            }
            this.state = NodeState.Complete;
            return NodeResult.Success;
        }

		NodeResult UpdateFromOriginalLocation()
		{
			// get trailers from web.inc
			var trailersHtml = SiteUtilBase.GetWebData<HtmlDocument>(new Uri(OriginalLocation, Configuration.HtmlMovieTrailersUri).ToString());

			var trailerList = trailersHtml.DocumentNode.SelectSingleNode("//ul[@class = 'trailers-dropdown']");
			if (trailerList != null)
			{
				// clear videos
				Videos.Clear();
				// add new videos
				foreach (var trailerListItem in trailerList.Elements("li"))
				{
					Video video = this.session.Get<Video>("");
					video.Title = trailerListItem.Element("div").Element("div").Element("h3").InnerText;
					foreach (var text in trailerListItem.Element("div").Element("div").Element("p").Elements("#text"))
					{
						if (text.InnerText.Trim().StartsWith("Posted:"))
						{
							DateTime dt;
							if (System.DateTime.TryParse(text.InnerText.Replace("Posted:", "").Trim(), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dt))
							{
								video.Published = dt;
							}
						}
						else if (text.InnerText.Trim().StartsWith("Runtime:"))
						{
							TimeSpan ts;
							if (TimeSpan.TryParse("00:" + text.InnerText.Replace("Runtime:", "").Trim(), out ts))
							{
								video.Duration = ts;
							}
						}
					}
					var secondDiv = trailerListItem.Element("div").Elements("div").Last();
					video.ThumbUrl = secondDiv.Element("a").Element("img").GetAttributeValue("src", "");

					var trailerFilesDiv = secondDiv.Elements("div").FirstOrDefault(d => d.GetAttributeValue("class", "") == "dropdown-list");
					if (trailerFilesDiv == null)
						trailerFilesDiv = trailersHtml.DocumentNode.SelectSingleNode("//div[@class = 'dropdown-list']");
					if (trailerFilesDiv != null)
					{
						var downloadLi = trailerFilesDiv.Element("ul").Elements("li").FirstOrDefault(li => li.InnerText.Trim() == "Download");
						while (downloadLi != null)
						{
							if (downloadLi.Name == "li" && downloadLi.GetAttributeValue("class", "") == "hd")
							{
								string uri = downloadLi.Element("a").GetAttributeValue("href","");
								if (!string.IsNullOrEmpty(uri))
								{
									VideoQuality vq = Video.ParseVideoQuality(downloadLi.Element("a").InnerText);
									if (vq != VideoQuality.Unknown)
									{
										video.Files.Add(vq, uri);
										video.Uri = uri.Replace("http://", "file://"); // we have to set an url so Onlinevideos downloader can distinguish clips, set one we can filter later
									}
								}
							}
							downloadLi = downloadLi.NextSibling;
						}
					}

					if (video.Files.Count > 0)
						Videos.Add(video);
				}
			}
			return Videos.Count > 0 ? NodeResult.Success : NodeResult.Failed;
		}
        /// <summary>
        /// Parses a JSON feed with movies
        /// </summary>
        /// <param name="data">JSON</param>
        /// <returns>List of ITMovie</returns>
        internal static List<Movie> GetMoviesFromJsonData(ISession session, string data)
        {
            List<MovieDTO> moviesDTO = JsonConvert.DeserializeObject<List<MovieDTO>>(data);
            List<Movie> movies = new List<Movie>();
            if (moviesDTO != null)
            {
                foreach (MovieDTO movieDTO in moviesDTO)
                {
                    Movie movie = CreateMovieFromJson(session, movieDTO);
                    if (movie != null)
                    {
                        movies.Add(movie);
                    }                    
                }
            }
            return movies;
        }

        internal static Movie CreateMovieFromJson(ISession session, MovieDTO movieItem)
        {
            // convert location to actual movie uri
            string movieUri = movieItem.location.Replace("/trailers/", session.Config.XmlMovieDetailsUri) + "index.xml";

            // create the movie object
            Movie movie =  session.Get<Movie>(movieUri);
            if (movie == null)
            {
                return null;
            }

			System.Uri.TryCreate(new System.Uri(session.Config.BaseUri), movieItem.location, out movie.OriginalLocation);

            movie.Title = HttpUtility.HtmlDecode(movieItem.title);
            movie.ReleaseDate = movieItem.releasedate;

            if (movieItem.genre != null && movie.Genres.Count == 0)
            {
                foreach (string genre in movieItem.genre)
                {
                    string g = HttpUtility.HtmlDecode(genre);
                    movie.Genres.Add(g);
                }
            }

            if (movieItem.actors != null && movie.Actors.Count == 0)
            {
                foreach (string actor in movieItem.actors)
                {
                    string a = HttpUtility.HtmlDecode(actor);
                    movie.Actors.Add(a);
                }
            }

            if (movieItem.directors != null && movie.Directors.Count == 0)
            {
                string[] directors = movieItem.directors.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string director in directors)
                {
                    string d = HttpUtility.HtmlDecode(director);
                    movie.Directors.Add(d);
                }
            }

            movie.Studio = HttpUtility.HtmlDecode(movieItem.studio);
            movie.Certificate = movieItem.rating;

            if (movieItem.poster != null && movie.Poster == null)
            {
                string poster = session.Config.FixUri(movieItem.poster);
                movie.Poster = session.Get<Poster>(poster);
            }

            return movie;
        }

        internal static List<Movie> GetMoviesFromXml(ISession session, string data)
        {
            List<Movie> movies = new List<Movie>();
            HtmlNode root = Utility.ToHtmlNode(data);

            if (root == null)
            {
                return movies;
            }

            // Movie nodes
            HtmlNodeCollection nodes = root.SelectNodes("//matrixview[contains(gotourl/@url, '/moviesxml/s/')]");
            foreach (HtmlNode node in nodes)
            {

                HtmlNode infoNode = node.SelectSingleNode("gotourl");
                string uri = infoNode.Attributes["url"].Value;
                uri = session.Config.FixUri(uri);

                string poster = node.SelectSingleNode("gotourl/pictureview").Attributes["url"].Value;
                string title = node.SelectSingleNode("vboxview/hboxview/textview/gotourl/setfontstyle/b").InnerText;
                string studio = node.SelectSingleNode("vboxview/textview/gotourl[contains(@url, '/moviesxml/s/')]/setfontstyle").InnerText;

                HtmlNodeCollection others = node.SelectNodes("vboxview/textview/setfontstyle");

                string actorsText = others[0].InnerText;
                string genreText = others[1].InnerText.Replace("Category:", string.Empty);
                string rating = others[2].InnerText.Replace("Rating:", string.Empty).Trim();

                // create a new ITMovie objects from the gathered information.
                Movie movie = session.Get<Movie>(uri);
                if (movie == null)
                {
                    continue;
                }

                movie.Title = HttpUtility.HtmlDecode(title);
                movie.Studio = HttpUtility.HtmlDecode(studio);
                movie.Certificate = HttpUtility.HtmlDecode(rating);

                // Actors
                string[] actors = actorsText.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string actor in actors)
                {
                    string a = HttpUtility.HtmlDecode(actor).Trim();
                    movie.Actors.Add(a);
                }

                // Genres
                string[] genres = genreText.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string genre in genres)
                {
                    string g = HttpUtility.HtmlDecode(genre).Trim();
                    movie.Genres.Add(g);
                }

                // Poster
                if (!String.IsNullOrEmpty(poster))
                {

                    poster = session.Config.FixUri(poster);

                    // if we find this string the poster is small so we change it to the normal version
                    poster = poster.Replace("_m20", "_20");
                    movie.Poster = session.Get<Poster>(poster);
                }

                movies.Add(movie);
            }


            return movies;
        }

        internal static List<Movie> GetMoviesFromXmlInclude(ISession session, string data)
        {
            List<Movie> movies = new List<Movie>();
            HtmlNode root = Utility.ToHtmlNode(data);

            if (root == null)
            {
                return movies;
            }

            // Movie nodes
            HtmlNodeCollection nodes = root.SelectNodes("//gotourl[contains(@url, '/moviesxml/s/')]");
            foreach (HtmlNode node in nodes)
            {

                string uri = node.Attributes["url"].Value;
                uri = session.Config.FixUri(uri);

                string title = node.SelectSingleNode("view/vboxview/textview/setfontstyle/b").InnerText;

                // create a new ITMovie objects from the gathered information.
                Movie movie = session.Get<Movie>(uri);
                if (movie == null)
                {
                    continue;
                }
                movie.Title = HttpUtility.HtmlDecode(title);

                //string posterUri = uri.Replace("/index.xml", "/images/poster.jpg");
                //movie.Poster = new ITPoster(posterUri);

                movies.Add(movie);
            }


            return movies;
        }

        #region IVideoDetails Members

        public Dictionary<string, string> GetExtendedProperties()
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();

            properties.Add("Title", this.Title);
            properties.Add("Synopsis", this.Plot);  // todo: redundant remove later (renamed to Plot)
            properties.Add("Plot", this.Plot);
            properties.Add("Directors", this.Directors.ToCommaSeperatedString());
            properties.Add("Actors", this.Actors.ToCommaSeperatedString());
            properties.Add("Genres", this.Genres.ToCommaSeperatedString());
            properties.Add("Studio", this.Studio);
            properties.Add("Rating", this.Certificate);      // todo: redundant remove later (renamed to Certificate)
            properties.Add("Certificate", this.Certificate); 

            string releaseDate = this.ReleaseDate != DateTime.MinValue ? this.ReleaseDate.ToShortDateString() : "Coming Soon";
            properties.Add("ReleaseDate", releaseDate);
            properties.Add("Year", this.Year.ToString());

            return properties;
        }

        #endregion
    }
}

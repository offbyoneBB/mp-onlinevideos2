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
using System.Globalization;

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

        public override NodeResult Update()
        {
            string url = this.uri;
            string data = this.session.MakeRequest(url);
            HtmlNode root = Utility.ToHtmlNode(data);

            if (root == null)
            {
                return NodeResult.Failed;
            }

            HtmlNode item = root.SelectSingleNode("atv/body/itemdetail");
            if (item == null)
            {
                return NodeResult.Failed;
            }

            // Grab the poster
            HtmlNode Poster = item.SelectSingleNode("image");
            if (Poster != null && Poster.InnerText.Length > 0)
            {
                // sometimes the poster urls start relative, in that case complete the address
                string poster = (Poster.InnerText.StartsWith("/")) ? this.session.Config.BaseUri + Poster.InnerText : Poster.InnerText;

                // if we had no poster create the poster object
                if (this.Poster == null)
                {
                    this.Poster = this.session.Get<Poster>(poster);
                }

                // set this address to be the x-large poster
                this.Poster.XL = poster;
            }
            
            // Synopsis
            HtmlNode summary = item.SelectSingleNode("summary");          
            if (summary != null)
            {
                string synopsis = summary.InnerText.Trim();
                this.Plot = HttpUtility.HtmlDecode(synopsis);
            }

            // Synopsis
            if (string.IsNullOrEmpty(this.Studio))
            {
                HtmlNode studio = item.SelectSingleNode("subtitle");          
                if (studio != null)
                {
                    this.Studio = HttpUtility.HtmlDecode(studio.InnerText.Trim());
                }
            }

            // other details
            HtmlNodeCollection details = item.SelectNodes("table/rows/row/label");
            if (details != null && details.Count == 20)
            {
                // Release Date  (5th label)
                if (this.ReleaseDate > DateTime.MinValue)
                {
                    string rawDate = details[4].InnerText;
                    rawDate = rawDate.Replace("In Theaters ", "").Trim();

                    DateTime dt;
                    if (DateTime.TryParseExact(rawDate, new string[] { @"M/dd" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                    {
                        this.ReleaseDate = dt;
                    }
                }

                if (this.Directors.Count == 0) 
                {
                    string director = HttpUtility.HtmlDecode(details[2].InnerText.Trim());                 
                    this.Directors.Add(director);
                }

                //if (this.Writers.Count == 0)
                //{
                //    string writer = HttpUtility.HtmlDecode(details[3].InnerText.Trim());
                //    this.Writers.Add(writer);
                //}

                if (this.Actors.Count == 0)
                {
                    this.Actors.Add(HttpUtility.HtmlDecode(details[1].InnerText.Trim()));
                    this.Actors.Add(HttpUtility.HtmlDecode(details[5].InnerText.Trim()));
                    this.Actors.Add(HttpUtility.HtmlDecode(details[9].InnerText.Trim()));
                    this.Actors.Add(HttpUtility.HtmlDecode(details[13].InnerText.Trim()));
                }

                if (this.Genres.Count == 0)
                {
                    this.Actors.Add(HttpUtility.HtmlDecode(details[0].InnerText.Trim()));
                }

            }

            // clear videos
            this.Videos.Clear();

            HashSet<string> videoUrls = new HashSet<string>();
            Dictionary<string, string> videoTitles = new Dictionary<string, string>();

            // Find all the videos for this movie.
            HtmlNodeCollection videoNodes = item.SelectNodes("//actionbutton");
            if (videoNodes != null && videoNodes.Count > 0)
            {
                HtmlNode target = videoNodes.Where(x => x.GetAttributeValue("id", "") == "more").FirstOrDefault();
                if (target != null)
                {
                    // use more link for all videos
                    string allVideosUrl = this.Uri.Replace("index-hd.xml", "more.xml");
                    string allVideosData = this.session.MakeRequest(allVideosUrl);
                    HtmlNode allVideosRoot = Utility.ToHtmlNode(allVideosData);

                    HtmlNodeCollection allVideoNodes = allVideosRoot.SelectNodes("//twolineenhancedmenuitem");
                    foreach (var node in allVideoNodes)
                    {
                        var match = Regex.Match(node.OuterHtml, @"'[^\s]+\.xml'", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                        if (!match.Success)
                        {
                            continue;
                        }

                        Video video = this.session.Get<Video>(match.Value.Replace("'", ""));
                        
                        HtmlNode temp = node.SelectSingleNode("label");          
                        if (temp != null)
                        {
                            video.Title = HttpUtility.HtmlDecode(temp.InnerText.Trim());
                        }
                        
                        temp = node.SelectSingleNode("image");
                        if (temp != null)
                        {
                            video.ThumbUrl = HttpUtility.HtmlDecode(temp.InnerText.Trim());
                        }

                        temp = node.SelectSingleNode("rightlabel");
                        if (temp != null)
                        {
                            DateTime dt;
                            if (DateTime.TryParseExact(temp.InnerText.Trim(), new string[] { @"M/dd" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                            {
                                video.Published = dt;
                            }                            
                        }

                        this.Videos.Add(video);
                    }
                }
                else
                {
                    // use single trailer
                    target = videoNodes.FirstOrDefault();
                    var match = Regex.Match(target.OuterHtml, @"'[^\s]+\.xml'", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    if (match.Success)
                    {
                        Video video = this.session.Get<Video>(match.Value.Replace("'",""));
                        video.Title = "Trailer";
                        this.Videos.Add(video);
                    }
                }             
            }

            this.state = NodeState.Complete;

            return NodeResult.Success;
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
            string movieUri = movieItem.location.Replace("/trailers/", session.Config.XmlMovieDetailsUri) + "index-hd.xml";

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

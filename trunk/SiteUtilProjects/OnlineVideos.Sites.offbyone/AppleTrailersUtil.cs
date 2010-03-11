using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Web;
using System.Net;
using System.Text;
using System.Xml;
using Jayrock.Json;

namespace OnlineVideos.Sites
{
    public class AppleTrailersUtil : SiteUtilBase
    {
        public enum VideoQuality
        {
            UNKNOWN,
            SMALL,
            MEDIUM,
            LARGE,
            HD480,
            HD720,
            FULLHD
        }

        public class Video
        {
            private string _label;
            private DateTime _date = DateTime.MinValue;
            private TimeSpan _length = TimeSpan.Zero;
            private string _thumb = string.Empty;

            public Dictionary<string, string> PlaybackOptions = new Dictionary<string, string>();

            public string Label
            {
                get { return _label; }
                set { _label = value; }
            }

            public DateTime Date
            {
                get { return _date; }
                set { _date = value; }
            }

            public TimeSpan Duration
            {
                get { return _length; }
                set { _length = value; }
            }

            public string Thumb
            {
                get { return _thumb; }
                set { _thumb = value; }
            }

            public Video(string Label)
            {
                _label = Label;
            }

            public string Description { get; set; }

        }

        public class Trailer
        {
            public class StringList : List<string>
            {
                public override string ToString()
                {                    
                    return Count > 0 ? ToString(", ") : " ";
                }

                /// <summary>
                /// Joins a string[] together with the the given seperator
                /// </summary>
                /// <param name="seperator"></param>
                /// <returns>string output</returns>
                public string ToString(string seperator)
                {
                    return string.Join(seperator, base.ToArray());
                }
            }

            #region Enums

            public enum InfoState
            {
                SEARCH,
                INDEX,
                DETAIL
            }            

            #endregion

            #region Variables

            private StringList _genres = new StringList();
            private StringList _cast = new StringList();
            private StringList _platforms = new StringList();
            private int _year = 0;
            public List<Video> Media = new List<Video>();

            #endregion

            #region Properties

            public string Title { get; set; }
            public string Description { get; set; }

            public int Year
            {
                get
                {
                    if (_year > 0)
                    {
                        return _year;
                    }
                    else
                    {
                        return ReleaseDate.Year;
                    }
                }
                set { _year = value; }
            }
            public DateTime ReleaseDate { get; set; }
            public int Runtime { get; set; }
            public string Director { get; set; }
            public string Rating { get; set; }
            public string Studio { get; set; }
            public string Poster { get; set; }
            public string Thumb { get; set; }
            public object Tag { get; set; }
            public InfoState State { get; set; }            

            public StringList Genres
            {
                get { return _genres; }
                set { _genres = value; }
            }

            public StringList Platforms
            {
                get { return _platforms; }
                set { _platforms = value; }
            }

            public StringList Cast
            {
                get { return _cast; }
                set { _cast = value; }
            }

            #endregion

        }

        private const string urlBase = "http://trailers.apple.com/trailers/";

        private const string xmlNamespace = "http://www.apple.com/itms/";
        private const string urlXMLIndexAll = urlBase + "home/xml/widgets/indexall.xml";
        private const string urlXMLCurrent = urlBase + "home/xml/current.xml";
        private const string urlXMLCurrent480 = urlBase + "home/xml/current_480p.xml";
        private const string urlXMLCurrent720 = urlBase + "home/xml/current_720p.xml";
        private const string urlXMLDetails = "http://www.apple.com/moviesxml/s/";
        private const string urlXMLPoster = "http://images.apple.com/moviesxml/s/";

        private const string urlJsonJustAdded = urlBase + "home/feeds/just_added.json";
        private const string urlJsonExlusive = urlBase + "home/feeds/exclusive.json";
        private const string urlJsonPop = urlBase + "home/feeds/most_pop.json";
        private const string urlJsonHD = urlBase + "home/feeds/just_hd.json";
        private const string urlJsonGenre = urlBase + "home/feeds/genres.json";
        private const string urlJsonStudio = urlBase + "home/feeds/studios.json";

        protected Dictionary<string, Trailer> Trailers = new Dictionary<string,Trailer>();

        [Category("OnlineVideosConfiguration"), Description("Url used for getting the results of a search. {0} will be replaced with the query.")]
        string searchUrl = "http://www.apple.com/trailers/home/scripts/quickfind.php?q={0}";

        [Category("OnlineVideosUserConfiguration"), Description("Defines the maximum quality for the trailer to be played.")]
        VideoQuality trailerSize = VideoQuality.HD480;

        const int APPLE_PROXY_PORT = 30005;
        AppleProxyServer proxyApple;

        public override bool HasMultipleVideos
        {
            get { return true; }
        }

        public override int DiscoverDynamicCategories()
        {
            if (proxyApple == null) proxyApple = new OnlineVideos.Sites.AppleProxyServer(APPLE_PROXY_PORT);

            Settings.Categories.Clear();

            RssLink link = new RssLink();
            link.Name = "Just Added";
            link.Url = urlJsonJustAdded;
            Settings.Categories.Add(link);

            link = new RssLink();
            link.Name = "Exclusive";
            link.Url = urlJsonExlusive;
            Settings.Categories.Add(link);

            link = new RssLink();
            link.Name = "Just HD";
            link.Url = urlJsonHD;
            Settings.Categories.Add(link);

            link = new RssLink();
            link.Name = "Most Popular";
            link.Url = urlJsonPop;
            Settings.Categories.Add(link);

            link = new RssLink();
            link.Name = "Genres";
            link.Url = urlJsonGenre;
            link.HasSubCategories = true;
            Settings.Categories.Add(link);

            link = new RssLink();
            link.Name = "Studios";
            link.Url = urlJsonStudio;
            link.HasSubCategories = true;
            Settings.Categories.Add(link);

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            Dictionary<string, string> genresAndStudiosHash = new Dictionary<string, string>();

            parentCategory.SubCategories = new List<Category>();

            List<string> trailers = getJsonTrailerIndex((parentCategory as RssLink).Url);

            foreach (string trailer in trailers)
            {
                switch (parentCategory.Name)
                {
                    case "Genres":
                        foreach (string genre in Trailers[trailer].Genres)
                        {
                            if (!genresAndStudiosHash.ContainsKey(genre)) genresAndStudiosHash.Add(genre, "/featured/genre/" + genre);
                        }
                        break;
                    case "Studios":
                        string studio = Trailers[trailer].Studio;
                        string key = "/featured/studio/" + studio;
                        if (!genresAndStudiosHash.ContainsKey(studio)) genresAndStudiosHash.Add(studio, key);
                        break;
                }
            }

            foreach(KeyValuePair<string,string> aCat in genresAndStudiosHash)
            {
                RssLink link = new RssLink();
                link.Name = aCat.Key;
                link.Url = aCat.Value;
                link.ParentCategory = parentCategory;
                parentCategory.SubCategories.Add(link);
            }

            parentCategory.SubCategoriesDiscovered = true;

            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> result = new List<VideoInfo>();

            string url = ((RssLink)category).Url;
            string queryUrl = url;
            if (queryUrl.Contains("/featured/genre")) queryUrl = urlJsonGenre;
            else if (queryUrl.Contains("/featured/studio")) queryUrl = urlJsonStudio;

            List<string> trailers = getJsonTrailerIndex(queryUrl);

            string genre = string.Empty;
            string studio = string.Empty;
            if (url.Contains("/featured/genre/"))
                genre = url.Substring(url.LastIndexOf('/') + 1);

            if (url.Contains("/featured/studio/"))
                studio = url.Substring(url.LastIndexOf('/') + 1);

            foreach (string trailer in trailers)
            {
                Trailer t = Trailers[trailer];

                if (!String.IsNullOrEmpty(genre) && (!t.Genres.Contains(genre)))
                    continue;
                if (!String.IsNullOrEmpty(studio) && (t.Studio != studio))
                    continue;

                VideoInfo v = new VideoInfo();
                v.Title = t.Title;
                v.VideoUrl = trailer;
                v.ImageUrl = t.Thumb;
                v.Length = t.ReleaseDate != DateTime.MinValue ? t.ReleaseDate.ToShortDateString() : "";
                result.Add(v);
            }

            return result;
        }

        public override List<VideoInfo> getOtherVideoList(VideoInfo video)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();

            Trailer trailer = fetchDetails(video.VideoUrl);

            video.Cast = trailer.Cast.ToString();
            video.Genres = trailer.Genres.ToString();
            video.Description = trailer.Description;
            video.Tags = trailer.Poster;

            foreach (Video v in trailer.Media)
            {
                VideoInfo newVideo = new VideoInfo();
                newVideo.Title = trailer.Title + " - " + v.Label;
                newVideo.Title2 = v.Label;
                newVideo.Description = trailer.Description;
                newVideo.Length = v.Duration.ToString();
                newVideo.ImageUrl = trailer.Thumb;
                newVideo.PlaybackOptions = v.PlaybackOptions;
                newVideo.VideoUrl = GetTrailerUrlForConfiguredResolution(newVideo.PlaybackOptions);
                videoList.Add(newVideo);
            }
            
            return videoList;
        }
        
        private Trailer fetchDetails(string key)
        {
            Trailer trailer;
            if (Trailers.ContainsKey(key)) trailer = Trailers[key];
            else { trailer = new Trailer(); Trailers.Add(key, trailer); }

            if (trailer.State == Trailer.InfoState.DETAIL) 
                return trailer;

            string url = key.Replace("/trailers/", urlXMLDetails) + "index.xml";

            XmlNode Root = GetXml(url);
            if (Root == null)
            {
                Log.Error("Apple Trailers: No XML Found.");
                return null;
            }

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(Root.OwnerDocument.NameTable);
            nsmgr.AddNamespace("a", xmlNamespace);

            // Poster Image URL
            XmlNode Poster = Root.SelectSingleNode("//a:PictureView/@url[contains(.,'poster')]", nsmgr);
            if (Poster != null && Poster.Value.Length > 0)
            {
                trailer.Poster = Poster.Value;
                if (!trailer.Poster.StartsWith("http://")) trailer.Poster = "http://trailers.apple.com" + trailer.Poster;
                Log.Debug("Apple Trailers poster url:" + trailer.Poster);
            }

            // Plot / Description (fuzzy)
            XmlNodeList trlPlot = Root.SelectNodes("//a:VBoxView/a:TextView/a:SetFontStyle", nsmgr);
            if (trlPlot.Count > 1)
            {
                trailer.Description = HttpUtility.HtmlDecode(trlPlot[2].InnerText.Trim());
            }

            // Genres
            XmlNodeList trlGenre = Root.SelectNodes("//a:TextView/*[contains(text(),'Genre')]/a:GotoURL", nsmgr);
            foreach (XmlNode genre in trlGenre)
            {
                string newgenre = HttpUtility.HtmlDecode(genre.InnerText.Trim());
                if (!trailer.Genres.Contains(newgenre)) trailer.Genres.Add(newgenre);
            }

            // Cast
            XmlNodeList trlCast = Root.SelectNodes("//a:VBoxView/a:TextView[@styleSet='basic10']/a:SetFontStyle", nsmgr);
            foreach (XmlNode actor in trlCast)
            {
                string newactor = HttpUtility.HtmlDecode(actor.InnerText.Trim());
                if (!trailer.Cast.Contains(newactor)) trailer.Cast.Add(newactor);
            }

            // Find all the trailer pages for this movie.
            XmlNodeList trlUrls = Root.SelectNodes("//a:GotoURL[@target='main']/@url", nsmgr);
            Dictionary<string, int> validUrls = new Dictionary<string, int>();

            // Match every URL that contains the same path
            // These are most likely the trailer index pages
            string match = url.Replace("/index.xml", "");
            foreach (XmlNode trlUrl in trlUrls)
            {
                string chk = "http://www.apple.com" + trlUrl.Value;
                if (chk.Contains(match) && !validUrls.ContainsKey(chk))
                    // add the urls to the valid urls list
                    validUrls.Add(chk, 0);
            }

            // Now for each trailer page we found we are 
            // going to get the trailer information
            foreach (string urlDetails in validUrls.Keys)
            {
                //Log.Info("trailer url: {0}", urlDetails);
                Root = GetXml(urlDetails);
                if (Root == null)
                {
                    Log.Error("Apple Trailers: No XML Found for trailer page: {0}", urlDetails);
                    continue;
                }

                // Filter the playlist nodes to get the ones we need
                XmlNodeList playlist = Root.FirstChild.SelectNodes("//a:array/a:dict", nsmgr);

                if (playlist.Count > 0)
                {
                    foreach (XmlNode clip in playlist)
                    {
                        // Get the node following the previewURL this is the video url of the trailer
                        string videourl = clip.SelectSingleNode("a:key[contains(.,'previewURL')]", nsmgr).NextSibling.InnerText;
                        // Log.Info("Found video: {0}", videourl);

                        // Filter the Ipod format we don't really need it 
                        // for display on our our TV do we?
                        if (!videourl.Contains(".m4v"))
                        {
                            // Get the node following the songName this is the title of the trailer
                            string title = clip.SelectSingleNode("a:key[contains(.,'songName')]", nsmgr).NextSibling.InnerText;

                            // Get the node following the previewLength this is the duration of the video in seconds
                            int duration = int.Parse(clip.SelectSingleNode("a:key[contains(.,'previewLength')]", nsmgr).NextSibling.InnerText);

                            // get the release date of this specific trailer
                            string date = clip.SelectSingleNode("a:key[contains(.,'releaseDate')]", nsmgr).NextSibling.InnerText;

                            string label;
                            VideoQuality quality;

                            // Parse label and quality from the provided title
                            parseAppleVideoTitle(title, out label, out quality);

                            // Get existing/new video object by label
                            Video video = null;
                            foreach (Video v in trailer.Media)
                            {
                                if (v.Label == label)
                                {
                                    video = v;
                                    break;
                                }
                            }
                            if (video == null)
                            {
                                video = new Video(label);
                                trailer.Media.Add(video);
                            }                            
                            // Add the current quality to the video
                            video.PlaybackOptions[quality.ToString()] = string.Format("http://127.0.0.1:{0}/?url={1}", APPLE_PROXY_PORT, System.Web.HttpUtility.UrlEncode(videourl));
                            // Set the duration of the video
                            video.Duration = new TimeSpan(0, 0, duration);

                            if (!String.IsNullOrEmpty(date))
                            {
                                DateTime dt;
                                if (DateTime.TryParse(date, out dt))
                                {
                                    video.Date = dt;
                                }
                            }

                        }
                    }

                }
            }
            Log.Info("Apple Trailers: Added {0} trailers.", trailer.Media.Count.ToString());
            trailer.State = Trailer.InfoState.DETAIL;
            return trailer;
        }

        private List<string> getJsonTrailerIndex(string url)
        {
            List<string> trailers = new List<string>();

            object jsonData = GetWebDataAsJson(url);

            bool isSearchResult = false;
            // when search was used
            if (!(jsonData is JsonArray))
            {
                jsonData = (jsonData as JsonObject)["results"];
                isSearchResult = true;
            }

            Log.Info("Apple Trailers: Found {0} items.", (jsonData as JsonArray).Count.ToString());

            foreach (JsonObject trailer in jsonData as JsonArray)
            {
                string key = (string)trailer["location"];

                // no key no movie.. nothing to see here move along
                if (string.IsNullOrEmpty(key)) continue;

                // Little sanity check: if we do not have a title we probably don't have a movie
                if (string.IsNullOrEmpty((string)trailer["title"])) continue;

                // Get/Create Trailer object                
                Trailer newTrailer;
                if (Trailers.TryGetValue(key, out newTrailer))
                {
                    // If this Trailer already exists (but not from search result) just add the key to the list and don't fill the object.                    
                    if (newTrailer.State != Trailer.InfoState.SEARCH)
                    {
                        trailers.Add(key);
                        continue;
                    }
                }
                else
                {
                    newTrailer = new Trailer();
                    Trailers.Add(key, newTrailer);
                }

                newTrailer.State = isSearchResult ? Trailer.InfoState.SEARCH : Trailer.InfoState.INDEX;
                newTrailer.Title = Utils.ReplaceEscapedUnicodeCharacter(HttpUtility.HtmlDecode((string)trailer["title"]));
                newTrailer.Studio = HttpUtility.HtmlDecode((string)trailer["studio"]);
                newTrailer.Rating = (string)trailer["rating"];

                // thumbnail
                string poster = (string)trailer["poster"];

                if (poster.StartsWith("/trailers/")) poster = poster.Replace("/trailers/", urlBase);
                else if (poster.StartsWith("/moviesxml/s/")) poster = poster.Replace("/moviesxml/s/", urlXMLPoster);

                newTrailer.Poster = getLargePoster(poster);                
                string secondaryThumb = key.Replace("/trailers/", urlBase) + "images/poster.jpg";
                newTrailer.Thumb = poster + (secondaryThumb != poster ? "|" + secondaryThumb : "");
                
                try
                {
                    newTrailer.ReleaseDate = DateTime.Parse((string)trailer["releasedate"]);
                }
                catch { }
                // This is ugly/heavy needs better logic
                // director isn't always available
                try
                {
                    newTrailer.Director = HttpUtility.HtmlDecode((string)trailer["director"]);
                }
                catch { }

                // List genres
                try
                {
                    foreach (string genre in trailer["genre"] as JsonArray)
                    {
                        if (genre.Length > 3) newTrailer.Genres.Add(HttpUtility.HtmlDecode(genre.ToString()));
                    }
                }
                catch { }

                // This is ugly needs better logic
                // actors isn't always available
                try
                {
                    foreach (string actor in trailer["actors"] as JsonArray)
                    {
                        newTrailer.Cast.Add(HttpUtility.HtmlDecode(actor.Trim(new char[] { ',', ' ' })));
                    }
                }
                catch { }

                // If we made all the way add the key to the list.
                trailers.Add(key);
            }

            // return the key list
            return trailers;
        }

        #region Apple Trailer Tricks

        private static string getLargePoster(string poster)
        {
            // simple replacement to get larger poster
            // should probably change this later into a more safe solution.
            //
            // tackle exceptions (sometimes the time differs with one second.. DOH ><)
            // http://images.apple.com/moviesxml/s/wb/posters/rocknrolla_l200808191536.jpg
            // http://images.apple.com/trailers/wb/images/rocknrolla_200808191537.jpg

            string lposter = poster.Replace("http://images.apple.com/trailers/", urlXMLPoster);
            lposter = lposter.Replace("images/", "posters/");
            //lposter = lposter.Replace("_20", "_l20"); // Large
            lposter = lposter.Replace("_20", "_xl20"); // Xtra Large
            return lposter;
        }

        private static void parseAppleVideoTitle(string input, out string title, out VideoQuality quality)
        {
            // This parses a title formatted as: "Trailer Name (Quality)" 
            // from the playlist and splits it into a title and the quality enum

            string[] parts = input.Split('(');
            if (parts.Length == 2)
            {
                // this should be the title.
                title = parts[0].Trim();
                // this should be the quality identifier
                string q = parts[1].Replace(")", "").ToLower();
                switch (q)
                {
                    case "small":
                        quality = VideoQuality.SMALL;
                        break;
                    case "medium":
                        quality = VideoQuality.MEDIUM;
                        break;
                    case "large":
                        quality = VideoQuality.LARGE;
                        break;
                    case "hd 480p":
                        quality = VideoQuality.HD480;
                        break;
                    case "hd 720p":
                        quality = VideoQuality.HD720;
                        break;
                    case "hd 1080p":
                        quality = VideoQuality.FULLHD;
                        break;
                    default:
                        quality = VideoQuality.UNKNOWN;
                        break;
                }
            }
            else
            {
                title = input;
                quality = VideoQuality.UNKNOWN;
            }
        }

        #endregion

        /// <summary>
        /// Get XML data from url
        /// </summary>
        /// <param name="url"></param>
        /// <returns>XmlNode root</returns>
        protected XmlNode GetXml(string url)
        {
            string WebData = GetWebData(url);
            try
            {
                // attempts to convert the returned string into an XmlDocument
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(WebData);
                XmlNode root = doc.FirstChild.NextSibling;
                return root;
            }
            catch (XmlException e)
            {
                Log.Error("Error parsing results from {0} as XML: {1}", url, e.Message);
            }

            return null;
        }        

        string GetTrailerUrlForConfiguredResolution(Dictionary<string, string> files)
        {
            if (files == null || files.Count == 0) return "";

            if (files.ContainsKey(trailerSize.ToString())) return files[trailerSize.ToString()];
            else
            {
                string[] vq = new string[files.Count];
                files.Keys.CopyTo(vq, 0);
                if (vq.Length > 1) return files[vq[vq.Length - 1]].ToString();
                else return files[vq[0]].ToString();
            }
        }
      
        #region Search

        public override bool CanSearch { get { return true; } }

        public override List<VideoInfo> Search(string query)
        {
            return getVideoList(new RssLink() { Url = string.Format(searchUrl, query) });
        }

        #endregion
    }

    /// <summary>
    /// This class handles HTTP Request that will be used to get apple trailers with the correct User Agent and return them via http.
    /// </summary>
    internal class AppleProxyServer
    {
        HybridDSP.Net.HTTP.HTTPServer _server = null;

        public AppleProxyServer(int port)
        {
            _server = new HybridDSP.Net.HTTP.HTTPServer(new RequestHandlerFactory(), port);
            _server.OnServerException += new HybridDSP.Net.HTTP.HTTPServer.ServerCaughtException(delegate(Exception ex) { Log.Error(ex.Message); });
            _server.Start();
        }

        public void StopListening()
        {
            _server.Stop();
        }

        class RequestHandlerFactory : HybridDSP.Net.HTTP.IHTTPRequestHandlerFactory
        {
            public HybridDSP.Net.HTTP.IHTTPRequestHandler CreateRequestHandler(HybridDSP.Net.HTTP.HTTPServerRequest request)
            {
                return new RequestHandler();
            }
        }

        class RequestHandler : HybridDSP.Net.HTTP.IHTTPRequestHandler
        {
            public bool DetectInvalidPackageHeader()
            {
                return false;
            }
            public void HandleRequest(HybridDSP.Net.HTTP.HTTPServerRequest request, HybridDSP.Net.HTTP.HTTPServerResponse response)
            {
                string url = System.Web.HttpUtility.ParseQueryString(new Uri(new Uri("http://127.0.0.1"), request.URI).Query)["url"];

                HttpWebRequest appleRequest = WebRequest.Create(url) as HttpWebRequest;
                if (appleRequest == null)
                {
                    response.Status = HybridDSP.Net.HTTP.HTTPServerResponse.HTTPStatus.HTTP_NOT_FOUND;
                    response.Send().Close();
                }
                else
                {
                    appleRequest.UserAgent = "QuickTime/7.6.2";
                    WebResponse appleResponse = appleRequest.GetResponse();
                    // copy response settings
                    response.ContentType = appleResponse.ContentType;
                    response.ContentLength = appleResponse.ContentLength;                    
                    // restream data
                    Stream responseStream = response.Send();
                    Stream appleResponseStream = appleResponse.GetResponseStream();
                    int read = 0;
                    while(read < appleResponse.ContentLength)
                    {
                        byte[] data = new byte[1024];
                        int fetched = appleResponseStream.Read(data, 0, 1024);
                        read += fetched;
                        responseStream.Write(data, 0, fetched);
                        if (fetched == 0 || read >= appleResponse.ContentLength) break;
                    }
                    responseStream.Flush();
                    responseStream.Close();
                }
            }
        }
    }
}

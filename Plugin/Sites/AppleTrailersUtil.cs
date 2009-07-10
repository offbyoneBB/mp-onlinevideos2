using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Net;
using System.Text;
using System.Xml;
using Jayrock.Json;
using MediaPortal.GUI.Library;

namespace OnlineVideos.Sites
{
    public class AppleTrailersUtil : SiteUtilBase, ISearch
    {
        public struct IndexItem
        {
            public string Key;
            public bool Folder;
            public string Image;
            public string Label;

            public IndexItem(string label)
            {
                Label = label;
                Folder = false;
                Key = string.Empty;
                Image = string.Empty;
            }

            public IndexItem(string label, string key)
            {
                Key = key;
                Label = label;
                Folder = false;
                Image = string.Empty;
            }

            public IndexItem(string label, string key, bool folder)
            {
                Key = key;
                Label = label;
                Folder = folder;
                Image = string.Empty;
            }
        }

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

            public Dictionary<VideoQuality, Uri> Size = new Dictionary<VideoQuality, Uri>();

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
                    return ToString(", ");
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

        private const string urlBase = "http://www.apple.com/trailers/";

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

        public override bool hasMultipleVideos()
        {
            return true;
        }

        public override int DiscoverDynamicCategories(SiteSettings site)
        {
            site.Categories.Clear();

            RssLink link = new RssLink();
            link.Name = "Just Added";
            link.Url = urlJsonJustAdded;
            site.Categories.Add(link.Name, link);

            link = new RssLink();
            link.Name = "Exclusive";
            link.Url = urlJsonExlusive;
            site.Categories.Add(link.Name, link);

            link = new RssLink();
            link.Name = "Just HD";
            link.Url = urlJsonHD;
            site.Categories.Add(link.Name, link);

            link = new RssLink();
            link.Name = "Most Popular";
            link.Url = urlJsonPop;
            site.Categories.Add(link.Name, link);

            Dictionary<string, string> genresAndStudiosHash = new Dictionary<string, string>();

            List<string> trailers = getJsonTrailerIndex(urlJsonGenre);
            foreach (string trailer in trailers)
            {
                Trailer t = Trailers[trailer];
                string genre = HttpUtility.HtmlDecode(t.Genres[0]);
                string key = "/featured/genre/" + genre;
                if (!genresAndStudiosHash.ContainsKey(genre)) genresAndStudiosHash.Add(genre, key);
            }

            trailers = getJsonTrailerIndex(urlJsonStudio);
            foreach (string trailer in trailers)
            {
                Trailer t = Trailers[trailer];
                string studio = HttpUtility.HtmlDecode(t.Studio);
                string key = "/featured/studio/" + studio;
                if (!genresAndStudiosHash.ContainsKey(studio)) genresAndStudiosHash.Add(studio, key);
            }

            foreach(KeyValuePair<string,string> aCat in genresAndStudiosHash)
            {
                link = new RssLink();
                link.Name = aCat.Key;
                link.Url = aCat.Value;
                site.Categories.Add(link.Name, link);                
            }

            site.DynamicCategoriesDiscovered = true;
            return site.Categories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> result = new List<VideoInfo>();
            foreach(IndexItem item in fetchIndex(((RssLink)category).Url))
            {
                VideoInfo v = new VideoInfo();
                v.Title = item.Label;
                v.VideoUrl = item.Key;
                v.ImageUrl = item.Image;
                result.Add(v);
            }
            return result;
        }

        public override List<VideoInfo> getOtherVideoList(VideoInfo video)
        {
            List<VideoInfo> videoList = new List<VideoInfo>();

            Trailer trailer = fetchDetails(video.VideoUrl);

            video.Other = trailer;
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
                newVideo.VideoUrl = GetTrailerUrlForConfiguredResolution(v.Size);
                videoList.Add(newVideo);
            }
            
            return videoList;
        }

        private List<IndexItem> fetchIndex(string url)
        {
            List<IndexItem> items = new List<IndexItem>();

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

                IndexItem item = new IndexItem();
                item.Key = trailer;
                item.Label = HttpUtility.HtmlDecode(t.Title);
                item.Image = t.Thumb;

                items.Add(item);
            }

            return items;
        }

        private Trailer fetchDetails(string key)
        {
            Trailer trailer;
            if (Trailers.ContainsKey(key)) trailer = Trailers[key];
            else { trailer = new Trailer(); Trailers.Add(key, trailer); }

            if (trailer.State == Trailer.InfoState.DETAIL) 
                return trailer;

            string url = key.Replace("/trailers/", urlXMLDetails) + "index.xml";
            //Log.Info("[MyTrailers][Apple Trailers] XML details URL: {0}", url); 

            XmlNode Root = GetXml(url);
            if (Root == null)
            {
                Log.Error("[MyTrailers][Apple Trailers] No XML Found.");
                return null;
            }

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(Root.OwnerDocument.NameTable);
            nsmgr.AddNamespace("a", xmlNamespace);

            // Poster Image URL
            XmlNode Poster = Root.SelectSingleNode("//a:PictureView/@url[contains(.,'/posters/')]", nsmgr);
            if (Poster.Value.Length > 0)
            {
                Log.Info("[MyTrailers][Apple Trailers] Added 1 poster.");
                trailer.Poster = Poster.Value;
            }

            // Plot / Description (fuzzy)
            XmlNodeList trlPlot = Root.SelectNodes("//a:VBoxView/a:TextView/a:SetFontStyle", nsmgr);
            if (trlPlot.Count > 1)
            {
                trailer.Description = HttpUtility.HtmlDecode(trlPlot[2].InnerText.Trim());
            }

            // Cast
            XmlNodeList trlCast = Root.SelectNodes("//a:VBoxView/a:TextView[@styleSet='basic10']/a:SetFontStyle", nsmgr);
            foreach (XmlNode actor in trlCast)
            {
                string newactor = HttpUtility.HtmlDecode(actor.InnerText.Trim());
                if (!trailer.Cast.Contains(newactor))
                    trailer.Cast.Add(newactor);
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
                    Log.Error("[MyTrailers][Apple Trailers] No XML Found for trailer page: {0}", urlDetails);
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
                            video.Size[quality] = new Uri(videourl);
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
            Log.Info("[MyTrailers][Apple Trailers] Added {0} trailers.", trailer.Media.Count.ToString());
            trailer.State = Trailer.InfoState.DETAIL;
            return trailer;
        }

        private List<string> getJsonTrailerIndex(string url)
        {
            List<string> trailers = new List<string>();

            object jsonData = GetWebDataAsJson(url);

            if (!(jsonData is JsonArray)) jsonData = (jsonData as JsonObject)["results"]; // when search was used

            Log.Info("[MyTrailers][Apple Trailers] Found {0} items.", (jsonData as JsonArray).Count.ToString());

            foreach (JsonObject trailer in jsonData as JsonArray)
            {
                string key = (string)trailer["location"];

                // no key no movie.. nothing to see here move along
                if (string.IsNullOrEmpty(key))
                    continue;

                // If this Trailer exists just add the key to the 
                // list and don't fill the object.
                if (Trailers.ContainsKey(key))
                {
                    trailers.Add(key);
                    continue;
                }

                // Little sanitiy check
                // if we do not have a title we probably don't have a movie
                string title = (string)trailer["title"];
                if (string.IsNullOrEmpty(title))
                    continue;

                // Get/Create Trailer object                
                if (!Trailers.ContainsKey(key)) Trailers.Add(key, new Trailer());
                Trailer newTrailer = Trailers[key];
                newTrailer.Title = HttpUtility.HtmlDecode(title);
                newTrailer.Studio = (string)trailer["studio"];
                string poster = (string)trailer["poster"];
                newTrailer.Poster = getLargePoster(poster);
                newTrailer.Thumb = poster;
                newTrailer.Rating = (string)trailer["rating"];
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
                        newTrailer.Genres.Add(genre.ToString());
                    }
                }
                catch { }

                // This is ugly needs better logic
                // actors isn't always available
                try
                {
                    foreach (string actor in trailer["actors"] as JsonArray)
                    {
                        newTrailer.Cast.Add(HttpUtility.HtmlDecode(actor.ToString()));
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

        public static new string GetWebData(string url)
        {
            string _data = string.Empty;

            int tryCount = 0;
            int maxRetries = 3;
            int timeout = 0;
            int timeoutIncrement = 5000;

            while (_data == string.Empty && tryCount < maxRetries)
            {
                try
                {
                    tryCount++;
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Timeout = timeout + (timeoutIncrement * tryCount);
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    Stream resultData = response.GetResponseStream();
                    StreamReader reader = new StreamReader(resultData, Encoding.UTF8, true);
                    _data = reader.ReadToEnd().Replace('\0', ' ');
                    resultData.Close();
                    reader.Close();
                    response.Close();
                }
                catch (WebException ex)
                {
                    Log.Error("{0}", ex.Message);
                    if (tryCount == maxRetries)
                        Log.Error("[MyTrailers] Error connecting to {0} . Reached retry limit of {1}", url, maxRetries);
                }
            }

            return _data;
        }

        string GetTrailerUrlForConfiguredResolution(Dictionary<VideoQuality, Uri> files)
        {
            if (files == null || files.Count == 0) return "";

            VideoQuality q = OnlineVideoSettings.getInstance().AppleTrailerSize;
            if (files.ContainsKey(q)) return files[q].ToString();
            else
            {
                VideoQuality[] vq = new VideoQuality[files.Count];
                files.Keys.CopyTo(vq, 0);
                if (vq.Length > 1) return files[vq[vq.Length - 1]].ToString();
                else return files[vq[0]].ToString();
            }
        }

        #region ISearch Member

        public Dictionary<string, string> GetSearchableCategories(Category[] configuredCategories)
        {
            return new Dictionary<string, string>();
        }

        public List<VideoInfo> Search(string searchUrl, string query)
        {
            return getVideoList(new RssLink() { Url = string.Format(searchUrl, query) });
        }

        public List<VideoInfo> Search(string searchUrl, string query, string category)
        {
            return Search(searchUrl, query);
        }

        #endregion
    }
}

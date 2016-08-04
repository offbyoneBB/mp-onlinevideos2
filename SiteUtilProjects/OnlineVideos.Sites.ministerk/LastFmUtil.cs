using Newtonsoft.Json.Linq;
using OnlineVideos.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public class LastFmVideo : VideoInfo
    {
        public int Duration { get; set; }
        public string Artist { get; set; }
        public string ArtistSlug { get; set; }
        public string Track { get; set; }
        public string TrackSlug { get; set; }
    }

    public class LastFmCategory : RssLink
    {
        public string User { get; set; }
    }

    public class LastFmUtil : LatestVideosSiteUtilBase
    {

        #region Config

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Username"), Description("Last.fm usernam")]
        protected string username = null;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Password"), Description("Last.fm password"), PasswordPropertyText(true)]
        protected string password = null;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Filter out unplayable content"), Description("Filter out unplayable content, much slower.")]
        protected bool filterUnplayable = true;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Scrobble to Last.fm"), Description("Scrobble to Last.fm if 75% of the track has been played")]
        protected bool scrobbleToLastFm = true;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Publish now playing to Last.fm"), Description("Publish now playing to Last.fm")]
        protected bool publishNowPlaying = true;
        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Set YouTube as preferred player"), Description("Automatically set YouTube as Last.fm preferred player")]
        protected bool setPreferredPlayer = true;

        #endregion

        #region vars

        private Func<List<VideoInfo>> currentVideoMethod;
        private CookieContainer cc = null;
        private Dictionary<string, string> datePresets = new Dictionary<string, string>()
        {
            {"LAST_7_DAYS","Last 7 days"},
            {"LAST_30_DAYS","Last 30 days"},
            {"LAST_90_DAYS","Last 90 days"},
            {"LAST_180_DAYS","Last 180 days"},
            {"LAST_365_DAYS","Last 365 days"},
            {"ALL_TIME","All time"},
        };

        #endregion

        #region Event handler

        public override void OnPlaybackEnded(VideoInfo video, string url, double percent, bool stoppedByUser)
        {
            try
            {
                if (scrobbleToLastFm && percent >= 0.75 && video is LastFmVideo && !string.IsNullOrWhiteSpace((video as LastFmVideo).ArtistSlug) && !string.IsNullOrWhiteSpace((video as LastFmVideo).TrackSlug))
                {
                    int dur = (video as LastFmVideo).Duration;
                    if (dur == 0)
                    {
                        string metaData = GetWebData("https://www.youtube.com/get_video_info?video_id=" + video.VideoUrl.Replace("https://www.youtube.com/watch?v=", "").Replace("http://www.youtube.com/watch?v=", ""));
                        Regex regex = new Regex(@"dur%253D(?<dur>[1-9]\d*)");
                        Match m = regex.Match(metaData);
                        if(m.Success)
                        {
                            dur = int.Parse(m.Groups["dur"].Value);
                        }
                    }
                    Int32 ts = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    ts = ts - (int)(((double)dur) * percent);
                    string scrobbleDataFormat = "timestamp={0}&artist={1}&track={2}&duration={3}&ajax=1&csrfmiddlewaretoken={4}";
                    string scrobbleData = string.Format(scrobbleDataFormat, ts, (video as LastFmVideo).ArtistSlug, (video as LastFmVideo).TrackSlug, dur, GetCsrfToken("http://www.last.fm"));
                    GetWebData("http://www.last.fm/player/scrobble", postData: scrobbleData, cookies: Cookies, referer: "http://www.last.fm/home", cache: false);
                }
            }
            catch { }
            base.OnPlaybackEnded(video, url, percent, stoppedByUser);
        }

        #endregion

        #region Categories

        public override int DiscoverDynamicCategories()
        {
            //Need cookies / login
            if (Cookies == null)
                throw new OnlineVideosException("Unknown error (no cookies)");

            UserCategories(username).ForEach(c => Settings.Categories.Add(c));
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            var method = parentCategory.Other as Func<List<Category>>;
            if (method != null)
            {
                parentCategory.SubCategories = method.Invoke();
                parentCategory.SubCategoriesDiscovered = true;
                return parentCategory.SubCategories.Count;
            }
            return 0;
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            category.ParentCategory.SubCategories.Remove(category);
            var method = category.Other as Func<List<Category>>;
            if (method != null)
            {
                List<Category> cats = method.Invoke();
                category.ParentCategory.SubCategories.AddRange(cats);
                return cats.Count;
            }
            return 0;
        }

        private List<LastFmCategory> UserCategories(string user)
        {
            List<LastFmCategory> cats = new List<LastFmCategory>();
            bool isUser = user == username;
            string name = "your";
            if (!isUser)
                name = user.EndsWith("s") ? user + "'" : user + "'s";
            LastFmCategory library = new LastFmCategory()
            {
                Name = "Play " + name + " library",
                Description = isUser ? "Listen to music you’ve scrobbled before" : "Listen to music " + user + " scrobbled before",
                Url = "http://www.last.fm/player/station/user/" + user + "/library?ajax={0}",
            };
            library.Other = (Func<List<VideoInfo>>)(() => GetJsonVideos(library, 1));
            cats.Add(library);

            LastFmCategory mix = new LastFmCategory()
            {
                Name = "Play " + name + " mix",
                Description = isUser ? "Listen to a mix of music you’ve scrobbled before and recommendations from Last.fm" : "Listen to a mix of music " + user + " scrobbled before and recommendations from Last.fm",
                Url = "http://www.last.fm/player/station/user/" + user + "/mix?ajax={0}",
            };
            mix.Other = (Func<List<VideoInfo>>)(() => GetJsonVideos(mix, 1));
            cats.Add(mix);

            LastFmCategory recommended = new LastFmCategory()
            {
                Name = "Play " + name + " recommendations",
                Description = "Listen to " + name + " recommended music from Last.fm",
                Url = "http://www.last.fm/player/station/user/" + user + "/recommended?ajax={0}",
            };
            recommended.Other = (Func<List<VideoInfo>>)(() => GetJsonVideos(recommended, 1));
            cats.Add(recommended);

            LastFmCategory profile = new LastFmCategory()
            {
                Name = string.Format("Profile ({0})", user),
                User = user,
                HasSubCategories = true,
                SubCategories = new List<Category>()
            };
            profile.Other = (Func<List<Category>>)(() => ProfileCategories(profile));
            cats.Add(profile);

            return cats;
        }

        private List<Category> ProfileCategories(LastFmCategory user)
        {
            List<Category> cats = new List<Category>();
            LastFmCategory library = new LastFmCategory()
            {
                Name = "Library",
                User = user.User,
                Url = "library",
                HasSubCategories = true,
                SubCategories = new List<Category>(),
                ParentCategory = user
            };
            library.Other = (Func<List<Category>>)(() => LibraryCategories(library));
            cats.Add(library);

            /*
            if (user.User == username)
            {
                LastFmCategory recommendations = new LastFmCategory()
                {
                    Name = "Recommendations",
                    User = user.User,
                    HasSubCategories = true,
                    SubCategories = new List<Category>()
                };
                recommendations.Other = (Func<List<Category>>)(() => RecomendationsCategories(recommendations));
                cats.Add(recommendations);

            }
            */

            LastFmCategory following = new LastFmCategory()
            {
                Name = "Following",
                Url = "following",
                User = user.User,
                HasSubCategories = true,
                SubCategories = new List<Category>(),
                ParentCategory = user
            };
            following.Other = (Func<List<Category>>)(() => DiscoverOtherUsersCategories(following, 1));
            cats.Add(following);

            LastFmCategory followers = new LastFmCategory()
            {
                Name = "Followers",
                Url = "followers",
                User = user.User,
                HasSubCategories = true,
                SubCategories = new List<Category>(),
                ParentCategory = user
            };
            followers.Other = (Func<List<Category>>)(() => DiscoverOtherUsersCategories(followers, 1));
            cats.Add(followers);

            LastFmCategory loved = new LastFmCategory()
            {
                Name = "Loved Tracks",
                Url = "http://www.last.fm/user/" + user.User + "/loved",
                User = user.User,
                HasSubCategories = false,
                ParentCategory = user
            };
            loved.Other = (Func<List<VideoInfo>>)(() => GetHtmlVideos(loved, 1, null));
            cats.Add(loved);

            LastFmCategory neighbours = new LastFmCategory()
            {
                Name = "Neighbours",
                Url = "neighbours",
                User = user.User,
                HasSubCategories = true,
                SubCategories = new List<Category>(),
                ParentCategory = user
            };
            neighbours.Other = (Func<List<Category>>)(() => DiscoverOtherUsersCategories(neighbours, 1));
            cats.Add(neighbours);

            return cats;
        }

        private List<Category> LibraryCategories(LastFmCategory category)
        {
            List<Category> cats = new List<Category>();

            LastFmCategory scrobbles = new LastFmCategory()
            {
                Name = "Scrobbles",
                Url = "http://www.last.fm/user/" + category.User + "/library",
                User = category.User,
                HasSubCategories = false,
                ParentCategory = category
            };
            scrobbles.Other = (Func<List<VideoInfo>>)(() => GetHtmlVideos(scrobbles, 1, new SerializableDictionary<string, string>() { { "date_preset", "ALL_TIME" } }));
            cats.Add(scrobbles);

            Category artists = new Category()
            {
                Name = "Artists",
                HasSubCategories = true,
                SubCategoriesDiscovered = true,
                SubCategories = new List<Category>(),
                ParentCategory = category
            };
            foreach (KeyValuePair<string, string> datePreset in datePresets)
            {
                LastFmCategory period = new LastFmCategory()
                {
                    Name = datePreset.Value,
                    Url = datePreset.Key,
                    User = category.User,
                    HasSubCategories = true,
                    SubCategoriesDiscovered = false,
                    SubCategories = new List<Category>(),
                    ParentCategory = artists
                };
                period.Other = (Func<List<Category>>)(() => DiscoverArtistCategories(period, 1));
                artists.SubCategories.Add(period);
            }
            cats.Add(artists);

            Category albums = new Category()
            {
                Name = "Albums",
                HasSubCategories = true,
                SubCategoriesDiscovered = true,
                SubCategories = new List<Category>(),
                ParentCategory = category
            };
            foreach (KeyValuePair<string, string> datePreset in datePresets)
            {
                LastFmCategory period = new LastFmCategory()
                {
                    Name = datePreset.Value,
                    Url = "http://www.last.fm/user/" + category.User + "/library/albums?date_preset=" + datePreset.Key,
                    User = category.User,
                    HasSubCategories = true,
                    SubCategoriesDiscovered = false,
                    SubCategories = new List<Category>(),
                    ParentCategory = albums
                };
                period.Other = (Func<List<Category>>)(() => DiscoverAlbumsCategories(period, 1));
                albums.SubCategories.Add(period);
            }
            cats.Add(albums);


            Category tracks = new Category()
            {
                Name = "Tracks",
                HasSubCategories = true,
                SubCategoriesDiscovered = true,
                SubCategories = new List<Category>(),
                ParentCategory = category
            };

            foreach (KeyValuePair<string, string> datePreset in datePresets)
            {
                LastFmCategory period = new LastFmCategory()
                {
                    Name = datePreset.Value,
                    Url = "http://www.last.fm/user/" + category.User + "/library/tracks",
                    User = category.User,
                    HasSubCategories = false,
                    ParentCategory = tracks
                };
                period.Other = (Func<List<VideoInfo>>)(() => GetHtmlVideos(period, 1, new SerializableDictionary<string, string>() { { "date_preset", datePreset.Key } }));
                tracks.SubCategories.Add(period);
            }
            cats.Add(tracks);

            return cats;
        }

/*
        private List<Category> RecomendationsCategories(LastFmCategory category)
        {
            List<Category> cats = new List<Category>();

            Category artists = new Category()
            {
                Name = "Artists",
                HasSubCategories = true,
                SubCategoriesDiscovered = true,
                SubCategories = new List<Category>(),
                ParentCategory = category
            };
            foreach (KeyValuePair<string, string> datePreset in datePresets)
            {
                LastFmCategory period = new LastFmCategory()
                {
                    Name = datePreset.Value,
                    Url = datePreset.Key,
                    User = category.User,
                    HasSubCategories = true,
                    SubCategoriesDiscovered = false,
                    SubCategories = new List<Category>(),
                    ParentCategory = artists
                };
                period.Other = (Func<List<Category>>)(() => DiscoverArtistCategories(period, 1));
                artists.SubCategories.Add(period);
            }
            cats.Add(artists);

            LastFmCategory albums = new LastFmCategory()
            {
                Name = "Albums",
                Url = "http://www.last.fm/home/albums",
                User = category.User,
                HasSubCategories = true,
                SubCategoriesDiscovered = false,
                SubCategories = new List<Category>(),
                ParentCategory = category
            };
            albums.Other = (Func<List<Category>>)(() => DiscoverAlbumsCategories(albums, 1));
            cats.Add(albums);


            Category tracks = new Category()
            {
                Name = "Tracks",
                HasSubCategories = true,
                SubCategoriesDiscovered = true,
                SubCategories = new List<Category>(),
                ParentCategory = category
            };

            foreach (KeyValuePair<string, string> datePreset in datePresets)
            {
                LastFmCategory period = new LastFmCategory()
                {
                    Name = datePreset.Value,
                    Url = "http://www.last.fm/user/" + username + "/library/tracks",
                    User = category.User,
                    HasSubCategories = false,
                    ParentCategory = tracks
                };
                period.Other = (Func<List<VideoInfo>>)(() => GetHtmlVideos(period, 1, new SerializableDictionary<string, string>() { { "date_preset", datePreset.Key } }));
                tracks.SubCategories.Add(period);
            }
            cats.Add(tracks);

            return cats;
        }
        */

        private List<Category> DiscoverOtherUsersCategories(LastFmCategory category, int page)
        {
            List<Category> users = new List<Category>();
            string refererUrl = "http://www.last.fm/user/" + category.User + "/" + category.Url;
            string url = refererUrl + "?_pjax=%23content&page=" + page;
            string data = GetWebData(url, cookies: Cookies, referer: refererUrl);
            Regex regex = new Regex(@"<a href=""/user/(?<user>[^""]*)""[^>]*?title=""[^""]*?user""[^<]*<img.*?src=""(?<img>[^""]*)", RegexOptions.Singleline);
            foreach (Match m in regex.Matches(data))
            {
                LastFmCategory user = new LastFmCategory()
                {
                    Name = HttpUtility.HtmlDecode(m.Groups["user"].Value),
                    User = m.Groups["user"].Value,
                    ParentCategory = category,
                    Thumb = m.Groups["img"].Value,
                    SubCategories = new List<Category>(),
                    HasSubCategories = true,
                    SubCategoriesDiscovered = true
                };
                UserCategories(m.Groups["user"].Value).ForEach((c) => 
                {
                    c.ParentCategory = user;
                    user.SubCategories.Add(c); 
                });
                users.Add(user);
            }
            return users;
        }

        private List<Category> DiscoverArtistCategories(LastFmCategory category, int page)
        {
            string user = category.User;
            string refererUrl = "http://www.last.fm/user/" + user + "/library/artists?date_preset=" + category.Url;
            string url = refererUrl + "&_pjax=%23content&page=" + page;
            List<Category> cats = new List<Category>();
            string data = GetWebData(url, cookies: Cookies, referer: refererUrl);
            Regex regex = new Regex(@"img src=""(?<img>[^""]*)[^>]*?class=""avatar.*?library/music/(?<url>[^\?]*).*?>(?<name>[^<]*)</a>", RegexOptions.Singleline);
            foreach (Match m in regex.Matches(data))
            {
                RssLink artistCat = new RssLink()
                {
                    Name = HttpUtility.HtmlDecode(m.Groups["name"].Value),
                    Thumb = m.Groups["img"].Value,
                    HasSubCategories = true,
                    SubCategories = new List<Category>(),
                    SubCategoriesDiscovered = true,
                    ParentCategory = category
                };

                LastFmCategory artistRadio = new LastFmCategory()
                {
                    Name = "Play artist radio",
                    Url = "http://www.last.fm/player/station/music/" + m.Groups["url"].Value + "?ajax={0}",
                    User = user,
                    Thumb = m.Groups["img"].Value,
                    ParentCategory = artistCat,
                };
                artistRadio.Other = (Func<List<VideoInfo>>)(() => GetJsonVideos(artistRadio, 1));
                artistCat.SubCategories.Add(artistRadio);

                if (user != username)
                {
                    LastFmCategory artistUserScrobbledTracks = new LastFmCategory()
                    {
                        Name = "Scrobbles (" + username + ")",
                        Url = "http://www.last.fm/user/" + username + "/library/music/" + m.Groups["url"].Value + "/+tracks",
                        User = username,
                        Thumb = m.Groups["img"].Value,
                        ParentCategory = artistCat
                    };
                    artistUserScrobbledTracks.Other = (Func<List<VideoInfo>>)(() => GetHtmlVideos(artistUserScrobbledTracks, 1, new SerializableDictionary<string, string>() { { "date_preset", "ALL_TIME" } }));
                    artistCat.SubCategories.Add(artistUserScrobbledTracks);
                }

                LastFmCategory artistScrobbledTracks = new LastFmCategory()
                {
                    Name = "Scrobbles (" + user + ")",
                    Url = "http://www.last.fm/user/" + user + "/library/music/" + m.Groups["url"].Value + "/+tracks",
                    User = user,
                    Thumb = m.Groups["img"].Value,
                    ParentCategory = artistCat
                };
                artistScrobbledTracks.Other = (Func<List<VideoInfo>>)(() => GetHtmlVideos(artistScrobbledTracks, 1, new SerializableDictionary<string, string>() { { "date_preset", "ALL_TIME" } }));
                artistCat.SubCategories.Add(artistScrobbledTracks);

                LastFmCategory artistAlbums = new LastFmCategory()
                {
                    Name = "Albums",
                    Url = "http://www.last.fm/music/" + m.Groups["url"].Value + "/+albums",
                    User = user,
                    Thumb = m.Groups["img"].Value,
                    HasSubCategories = true,
                    ParentCategory = artistCat,
                    SubCategories = new List<Category>()
                };
                artistAlbums.Other = (Func<List<Category>>)(() => DiscoverAlbumsCategories(artistAlbums, 1));
                artistCat.SubCategories.Add(artistAlbums);

                LastFmCategory artistTracks = new LastFmCategory()
                {
                    Name = "Tracks",
                    Url = "http://www.last.fm/music/" + m.Groups["url"].Value + "/+tracks",
                    User = user,
                    Thumb = m.Groups["img"].Value,
                    ParentCategory = artistCat
                };
                artistTracks.Other = (Func<List<VideoInfo>>)(() => GetHtmlVideos(artistTracks, 1, null));
                artistCat.SubCategories.Add(artistTracks);

                cats.Add(artistCat);
            }
            regex = new Regex(@"<li class=""next"">(?<next>.*?)</li", RegexOptions.Singleline);
            Match nextMatch = regex.Match(data);
            if (nextMatch.Success && !string.IsNullOrWhiteSpace(nextMatch.Groups["next"].Value))
            {
                NextPageCategory npc = new NextPageCategory()
                {
                    Name = "Next",
                    ParentCategory = category,
                    Other = (Func<List<Category>>)(() => DiscoverArtistCategories(category, page + 1))
                };
                cats.Add(npc);
            }
            return cats;
        }

        private List<Category> DiscoverAlbumsCategories(LastFmCategory category, int page)
        {
            List<Category> albums = new List<Category>();
            string refererUrl = category.Url;
            string q = refererUrl.Contains("?") ? "&" : "?";
            string url = refererUrl + q + "_pjax=%23content&page=" + page;
            string data = GetWebData(url, cookies: Cookies, referer: refererUrl);
            string rString;
            if (url.Contains("+albums"))
                rString = @"album-grid-album-art"" src=""(?<img>[^""]*).*?""album-grid-item-main-text"">(?<name>[^<]*).*?data-station-url=""(?<url>[^""]*)";
            else
                rString = @"chartlist-play-image""[^<]*<img src=""(?<img>[^""]*).*?alt=""(?<name>[^""]*)[^<]*<button.*?data-station-url=""(?<url>[^""]*)";
            Regex regex = new Regex(rString, RegexOptions.Singleline);
            foreach (Match m in regex.Matches(data))
            {
                LastFmCategory album = new LastFmCategory()
                {
                    Name = HttpUtility.HtmlDecode(m.Groups["name"].Value),
                    Url = "http://www.last.fm" + m.Groups["url"].Value,
                    User = category.User,
                    ParentCategory = category,
                    Thumb = m.Groups["img"].Value,
                };
                album.Other = (Func<List<VideoInfo>>)(() => GetJsonVideos(album, 1));
                albums.Add(album);
            }

            regex = new Regex(@"<li class=""next"">(?<next>.*?)</", RegexOptions.Singleline);
            Match nextMatch = regex.Match(data);
            if (nextMatch.Success && !string.IsNullOrWhiteSpace(nextMatch.Groups["next"].Value))
            {
                NextPageCategory npc = new NextPageCategory()
                {
                    Name = "Next",
                    ParentCategory = category,
                    Other = (Func<List<Category>>)(() => DiscoverAlbumsCategories(category, page + 1))
                };
                albums.Add(npc);
            }
            return albums;
        }
 
        #endregion

        #region Videos

        public override List<VideoInfo> GetVideos(Category category)
        {
            HasNextPage = false;
            currentVideoMethod = category.Other as Func<List<VideoInfo>>;
            return currentVideoMethod.Invoke();
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            HasNextPage = false;
            return currentVideoMethod.Invoke();
        }

        private List<VideoInfo> GetHtmlVideos(LastFmCategory category, int currentPage, SerializableDictionary<string, string> parameters, bool skipNextPage = false)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            string url = category.Url;
            url += "?" + "_pjax=%23content&page=" + currentPage;
            if (parameters != null)
            {
                foreach (KeyValuePair<string, string> param in parameters)
                {
                    url += "&" + param.Key + "=" + param.Value;
                }
            }
            string data = GetWebData(url, cookies: Cookies, referer: category.Url);
            Regex regex;
            if (url.Contains("/library"))
                regex = new Regex(@"""chartlist-play-image"">[^<]*<img src=""(?<img>[^""]*)[^<]*?<a[^>]*?href=""(?<url>https{0,1}://www.youtube.com/watch[^""]*)[^>]*?data-track-name=""(?<track>[^""]*)[^>]*?data-track-url=""(?<slug>[^""]*)[^>]*?data-artist-name=""(?<artist>[^""]*)", RegexOptions.Singleline);
            else
                regex = new Regex(@"""chartlist-play"">[^<]*<a[^>]*?href=""(?<url>https{0,1}://www.youtube.com/watch[^""]*)[^>]*?data-track-name=""(?<track>[^""]*)[^>]*?data-track-url=""(?<slug>[^""]*)[^>]*?data-artist-name=""(?<artist>[^""]*)", RegexOptions.Singleline);
            foreach (Match m in regex.Matches(data))
            {
                try
                {
                    bool addVideo = true;
                    LastFmVideo track = new LastFmVideo();
                    if (filterUnplayable)
                    {
                        Dictionary<string, string> pbos = Hoster.HosterFactory.GetHoster("youtube").GetPlaybackOptions(m.Groups["url"].Value);
                        track.Other = pbos;
                        addVideo = pbos != null && pbos.Count > 0;
                    }
                    if (addVideo)
                    {
                        Regex trackRegex = new Regex(@"^/music/(?<artist>[^/]*)/[^/]*/(?<track>.*?)$");
                        string trackUrl = m.Groups["slug"].Value;
                        Match trackMatch = trackRegex.Match(trackUrl);
                        if (trackMatch.Success)
                        {
                            track.ArtistSlug = trackMatch.Groups["artist"].Value;
                            track.TrackSlug = trackMatch.Groups["track"].Value;
                            track.Track = m.Groups["track"].Value;
                            track.Artist = m.Groups["artist"].Value;
                            track.VideoUrl = m.Groups["url"].Value;
                            track.Thumb = m.Groups["img"].Value;
                            track.Title = track.Artist + " - " + track.Track;
                            videos.Add(track);
                        }
                    }
                }
                catch (OnlineVideosException e)
                {
                    Log.Debug("Not a playable video, Url: {0}, message: {1}", m.Groups["url"].Value, e.Message);
                }
            }
            if (!skipNextPage)
            {
                regex = new Regex(@"<li class=""next"">(?<next>.*?)</", RegexOptions.Singleline);
                Match nextMatch = regex.Match(data);
                HasNextPage = (nextMatch.Success && !string.IsNullOrWhiteSpace(nextMatch.Groups["next"].Value));
                if (HasNextPage)
                    currentVideoMethod = (Func<List<VideoInfo>>)(() => GetHtmlVideos(category, currentPage + 1, parameters));
            }
            return videos;
        }

        public List<VideoInfo> GetJsonVideos(LastFmCategory category, int currentPage)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            string currentUrl = category.Url;
            bool paginate = currentUrl.EndsWith("{0}");
            string url = paginate ? string.Format(currentUrl, currentPage) : currentUrl;
            JObject json = GetWebData<JObject>(url, cookies: Cookies, cache: false);
            foreach (JToken song in json["playlist"])
            {
                JToken playlink = song["playlinks"].First(pl => pl["affiliate"].Value<string>() == "youtube");
                if (playlink != null)
                {
                    try
                    {
                        bool addVideo = true;
                        LastFmVideo video = new LastFmVideo();
                        if (filterUnplayable)
                        {
                            Dictionary<string, string> pbos = Hoster.HosterFactory.GetHoster("youtube").GetPlaybackOptions(playlink["url"].Value<string>());
                            video.Other = pbos;
                            addVideo = pbos != null && pbos.Count > 0;
                        }
                        if (addVideo)
                        {
                            video.Artist = song["artists"].First()["name"].Value<string>();
                            video.Track = song["name"].Value<string>();
                            video.Title = video.Artist + " - " + video.Track;
                            video.VideoUrl = playlink["url"].Value<string>();
                            if (song["duration"] == null || song["duration"].Type == JTokenType.Null)
                            {
                                video.Duration = 0;
                            }
                            else
                            {
                                video.Duration = song["duration"].Value<int>();
                                video.Length = OnlineVideos.Helpers.TimeUtils.TimeFromSeconds(video.Duration.ToString());
                            }
                            string trackUrl = song["url"].Value<string>();
                            Regex regex = new Regex(@"^/music/(?<artist>[^/]*)/[^/]*/(?<track>.*?)$");
                            Match m = regex.Match(trackUrl);
                            if (m.Success)
                            {
                                video.ArtistSlug = m.Groups["artist"].Value;
                                video.TrackSlug = m.Groups["track"].Value;
                            }
                            videos.Add(video);
                        }
                    }
                    catch (OnlineVideosException e)
                    {
                        Log.Debug("Not a playable video, Url: {0}, message: {1}", playlink["url"].Value<string>(), e.Message);
                    }
                }
            }
            HasNextPage = paginate;
            if (HasNextPage)
                currentVideoMethod = (Func<List<VideoInfo>>)(() => GetJsonVideos(category, currentPage + 1));
            return videos;
        }

        public override List<string> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            if (filterUnplayable)
            {
                video.PlaybackOptions = new Dictionary<string, string>();
                foreach (KeyValuePair<string, string> pbo in video.Other as Dictionary<string, string>)
                {
                    video.PlaybackOptions.Add(pbo.Key, pbo.Value);
                }
            }
            else
            {
                video.PlaybackOptions = Hoster.HosterFactory.GetHoster("youtube").GetPlaybackOptions(video.VideoUrl);
            }
            string url = video.PlaybackOptions.Last().Value;
            if (inPlaylist) video.PlaybackOptions.Clear();

            if (publishNowPlaying && video is LastFmVideo && !string.IsNullOrWhiteSpace((video as LastFmVideo).ArtistSlug) && !string.IsNullOrWhiteSpace((video as LastFmVideo).TrackSlug))
            {
                try
                {
                    string publishDataFormat = "artist={0}&track={1}&duration={2}&ajax=1&csrfmiddlewaretoken={3}";
                    string publishData = string.Format(publishDataFormat, (video as LastFmVideo).ArtistSlug, (video as LastFmVideo).TrackSlug, (video as LastFmVideo).Duration, GetCsrfToken("http://www.last.fm"));
                    GetWebData("http://www.last.fm/player/now-playing", postData: publishData, cookies: Cookies, referer: "http://www.last.fm/home", cache: false);
                }
                catch{ }
            }
            return new List<string>() { url };
        }

        #endregion

        #region Context menu

        /*
        public override List<ContextMenuEntry> GetContextMenuEntries(Category selectedCategory, VideoInfo selectedItem)
        {
            if (selectedItem != null && selectedItem is LastFmVideo && !string.IsNullOrWhiteSpace((selectedItem as LastFmVideo).ArtistSlug ))
            {
                List<ContextMenuEntry> entries = new List<ContextMenuEntry>();
                ContextMenuEntry entry = new ContextMenuEntry();
                entry.Action = ContextMenuEntry.UIAction.Execute;
                entry.DisplayText = (selectedItem as LastFmVideo).Artist + " radio";
                entries.Add(entry);
                return entries;
            }
            return base.GetContextMenuEntries(selectedCategory, selectedItem);
        }

        public override ContextMenuExecutionResult ExecuteContextMenuEntry(Category selectedCategory, VideoInfo selectedItem, ContextMenuEntry choice)
        {
            if (selectedItem != null && selectedItem is LastFmVideo && !string.IsNullOrWhiteSpace((selectedItem as LastFmVideo).ArtistSlug) && choice.DisplayText == (selectedItem as LastFmVideo).Artist + " radio")
            {
            //    ContextMenuExecutionResult result = new ContextMenuExecutionResult();
              //  result.ResultItems = new List<SearchResultItem>();
//                currentUrlsss = "http://www.last.fm/player/station/music/" + (selectedItem as LastFmVideo).ArtistSlug + "?ajax={0}";
//                currentPagessss = 1;
//                GetAjaxVideos().ForEach(v => result.ResultItems.Add(v));
//                return result;
            }
            return base.ExecuteContextMenuEntry(selectedCategory, selectedItem, choice);
        }
        */

        #endregion

        #region Cookies

        private CookieContainer Cookies
        {
            get
            {
                if (cc == null)
                {
                    if (username == null || password == null)
                    {
                        throw new OnlineVideosException("Please enter a username and password.");
                    }
                    cc = new CookieContainer();
                    string url = "https://secure.last.fm/login";
                    GetWebData(url, cookies: cc, cache: false);
                    string csrftoken = GetCsrfToken("https://secure.last.fm");
                    string postdata = string.Format("csrfmiddlewaretoken={0}&next=%2F&username={1}&password={2}&submit=", csrftoken, HttpUtility.UrlEncode(username), HttpUtility.UrlEncode(password));
                    string data = GetWebData(url, postData: postdata, cookies: cc, referer: url, cache: false);
                    if (data.Contains("action=\"/login\""))
                    {
                        cc = null;
                        throw new OnlineVideosException("Please enter a correct username and password.");
                    }
                    if (setPreferredPlayer)
                    {
                        // Set youtube as prefered player
                        string postDataFormat = "csrfmiddlewaretoken={0}&preferred_affiliate=youtube&submit=playback";
                        string postData = string.Format(postDataFormat, GetCsrfToken("https://secure.last.fm"));
                        GetWebData("https://secure.last.fm/settings/website", postData: postData, cookies: Cookies, referer: "https://secure.last.fm/settings/website", cache: false);
                    }
                }
                return cc;
            }
        }

        private string GetCsrfToken(string domain)
        {
            if (cc != null)
            {
                foreach (Cookie cookie in cc.GetCookies(new Uri(domain)))
                {
                    if (cookie.Name == "csrftoken")
                        return cookie.Value;
                }
            }
            return string.Empty;
        }

        #endregion

        #region LatestVideos

        public override List<VideoInfo> GetLatestVideos()
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return new List<VideoInfo>();
            LastFmCategory scrobbles = new LastFmCategory()
            {
                Name = "Scrobbles",
                Url = "http://www.last.fm/user/" + username + "/library",
                User = username,
                HasSubCategories = false
            };
            scrobbles.Other = (Func<List<VideoInfo>>)(() => GetHtmlVideos(scrobbles, 1, new SerializableDictionary<string, string>() { { "date_preset", "ALL_TIME" } }, true));
            List<VideoInfo> videos = (scrobbles.Other  as Func<List<VideoInfo>>).Invoke();
            return videos.Count >= LatestVideosCount ? videos.GetRange(0, (int)LatestVideosCount) : new List<VideoInfo>();

        }

        #endregion

    }
}

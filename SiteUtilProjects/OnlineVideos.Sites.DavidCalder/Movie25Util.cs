using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;



namespace OnlineVideos.Sites.DavidCalder
{
    public class Movie25Util : DeferredResolveUtil, IChoice
    {

        public override VideoInfo CreateVideoInfo()
        {
            return new TMDBVideoInfo();
        }

        public class TMDBVideoInfo : DetailVideoInfo
        {
            public TMDBVideoInfo()
                : base()
            {
                Title2 = string.Empty;
            }

            public List<TMDbVideoDetails> VidoesChoices { get; set; }

            public TMDBVideoInfo(VideoInfo video)
                : base(video)
            {
            }

            public Movie25Util parent = null;

            public override string GetPlaybackOptionUrl(string url)
            {
                string data = WebCache.Instance.GetWebData(PlaybackOptions[url]);
                if (parent != null)
                {
                    parent.sh.WaitForSubtitleCompleted();
                    parent.lastPlaybackOptionUrl = PlaybackOptions[url];
                }
                Match n = Regex.Match(data, @"<IFRAME\sid=""showvideo""\ssrc=""(?<url>[^""]*)""");
                if (n.Success)
                {
                    if (n.Groups["url"].Value.Contains("/tz.php?url=external.php?url="))
                    {
                        string newurl = n.Groups["url"].Value.Replace(@"/tz.php?url=external.php?url=", "");
                        byte[] tmp = Convert.FromBase64String(newurl);
                        string i = Encoding.ASCII.GetString(tmp);
                        return GetVideoUrl(i);
                    }
                    return GetVideoUrl(n.Groups["url"].Value);
                }
                return GetVideoUrl(url);
            }
        }

        public override int DiscoverDynamicCategories()
        {
            base.DiscoverDynamicCategories();
            {
                int i = Settings.Categories.Count - 1;
                do
                {
                    RssLink cat = (RssLink)Settings.Categories[i];
                    if (cat.Name == "Movies" || cat.Name == "TV Shows")
                        Settings.Categories.Remove(cat);
                    i--;
                }
                while (i >= 0);
            }
            return Settings.Categories.Count;
        }

        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            try
            {
                TrackingInfo tInfo = new TrackingInfo()
                {
                    Regex = Regex.Match(video.Title, @"(?<Title>[^(]*)\((?<Year>[^)]*)\)"),
                    VideoKind = VideoKind.Movie
                };
                return tInfo;

            }
            catch (Exception e)
            {
                Log.Warn("Error parsing TrackingInfo data: {0}", e.ToString());
            }

            return base.GetTrackingInfo(video);
        }

        public override Dictionary<string, string> GetPlaybackOptions(string playlistUrl)
        {
            if (playlistUrl.StartsWith("https://www.youtube.com/watch?v="))
                return Hoster.HosterFactory.GetHoster("Youtube").GetPlaybackOptions(playlistUrl);
            else return base.GetPlaybackOptions(playlistUrl);
        }

        public override bool CanSearch { get { return true; } }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> videos = base.GetVideos(category);
            TMDB.BackgroundWorker worker = new TMDB.BackgroundWorker();
            worker.start(videos);
            System.Threading.Thread.Sleep(Convert.ToInt32(5) * 1000);
            return videos;
        }

        public List<DetailVideoInfo> GetVideoChoices(VideoInfo video)
        {
            List<DetailVideoInfo> DetailedVideos = new List<DetailVideoInfo>();
            DetailedVideos.Add(new TMDBVideoInfo(video) { Title2 = "Full Movie", parent = this });
            if (video.Other.GetType() == typeof(TMDbVideoDetails))
            {
                TMDbVideoDetails videoDetail = (TMDbVideoDetails)video.Other;

                foreach (TMDB.Trailer trailer in videoDetail.TMDbDetails.videos.TrailerList())
                {
                    var clip = new TMDBVideoInfo();
                    clip.Description = videoDetail.TMDbDetails.overview;
                    clip.Thumb = videoDetail.TMDbDetails.PosterPathFullUrl();
                    clip.Title = string.Format("{0}  –  {1}", video.Title, trailer.name);
                    clip.Airdate = videoDetail.TMDbDetails.ReleaseDateAsString();
                    clip.Title2 = trailer.name;
                    clip.VideoUrl = "https://www.youtube.com/watch?v=" + trailer.key;
                    DetailedVideos.Add(clip);
                }
            }
            return DetailedVideos;
        }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            searchUrl = string.Format("http://www.movie25.so/search.php?key={0}&submit=", query.Replace(" ", "+"));
            List<SearchResultItem> videos = base.Search(searchUrl, category);
            TMDB.BackgroundWorker worker = new TMDB.BackgroundWorker();
            worker.start(videos.ConvertAll<VideoInfo>(v => v as VideoInfo));
            return videos;
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            List<VideoInfo> videos = base.GetNextPageVideos();
            TMDB.BackgroundWorker worker = new TMDB.BackgroundWorker();
            worker.start(videos);
            return videos;
        }
    }
}

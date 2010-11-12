using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using Pondman.Metadata.ITunes.MovieTrailers;
using OnlineVideos.Sites.apondman.ITMovieTrailers;

namespace OnlineVideos.Sites.apondman {

    /// <summary>
    /// iTunes Movie Trailers
    /// </summary>
    public partial class ITMovieTrailersUtil : SiteUtilBase, IChoice, ISimpleRequestHandler {

        #region iTunes Movie Trailers

        ITunesTrailersApi _trailersApi;
        Stack<ITSection> _sectionPages;

        /// <summary>
        /// Initialize
        /// </summary>
        private void Init() {
            
            // create api instance
            if (_trailersApi == null) {
                _trailersApi = new ITunesTrailersApi();
                _trailersApi.DoWebRequest = doWebRequest;
            }

            // add a special reversed proxy handler
            ReverseProxy.AddHandler(this);

        }

        /// <summary>
        /// Make a webrequest
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private string doWebRequest(Uri uri) {
            return GetWebData(uri.AbsoluteUri, null, null, null, false, true);
        }

        /// <summary>
        /// Creates a new VideoInfo object using an instance of ITMovie
        /// </summary>
        /// <param name="movie"></param>
        /// <returns></returns>
        private VideoInfo createVideoInfoFromMovie(ITMovie movie) {
            VideoInfo video = new VideoInfo();
            video.Other = new MovieDetails(movie);
            video.Title = movie.Title;
            video.ImageUrl = movie.Poster != null ? movie.Poster.Uri.AbsoluteUri : string.Empty;
            
            // extra
            string actors = movie.Actors.ToCommaSeperatedString();
            video.Description = String.IsNullOrEmpty(movie.Synopsis) ? actors : movie.Synopsis;
            video.Length = movie.ReleaseDate != DateTime.MinValue ? movie.ReleaseDate.ToShortDateString() : "Coming Soon";

            return video;
        }

        private bool hasSubCategories(ITSection section) {
            string uri = section.Uri.AbsoluteUri;

            // the following sections have sub categories
            if (uri == _trailersApi.Configuration.FeaturedGenresUri || uri == _trailersApi.Configuration.FeaturedStudiosUri ||
                 uri == ITSection.FeaturedUri || uri == ITSection.StudiosUri || uri == ITSection.GenresUri) {
                return true;
            }

            return false;
        }

        private List<VideoInfo> getVideoList(ITSection section) {
            List<VideoInfo> videos = new List<VideoInfo>();

            ITResult result = _trailersApi.Update(section);
            if (section.State != ITState.Complete) {
                return videos;
            }

            _sectionPages.Push(section);

            foreach (ITMovie movie in section.Movies) {
                VideoInfo video = createVideoInfoFromMovie(movie);
                videos.Add(video);
            }

            return videos;
        }

        #endregion

        #region ISimpleRequestHandler Members

        public void UpdateRequest(HttpWebRequest request) {
            request.UserAgent = QuickTimeUserAgent;
        }

        #endregion

        #region SiteUtilBase

        public override bool HasNextPage {
            get {
                return ( _sectionPages.Count > 0 && _sectionPages.Peek().Sections.Count > 0);
            }
        }

        public override bool HasPreviousPage {
            get {
                return (_sectionPages.Count > 1);
            }
        }        

        public override bool CanSearch { 
            get { return true; } 
        }

        public override void Initialize(SiteSettings siteSettings) {
            base.Initialize(siteSettings);
            Init();
        }

        public override int DiscoverDynamicCategories() {
            Settings.Categories.Clear();

            ITSection rootSection = _trailersApi.Browse();

            foreach (ITSection section in rootSection.Sections) {
                Category cat = new Category();
                cat.Name = section.Name;
                cat.Other = section;
                cat.HasSubCategories = hasSubCategories(section);
                Settings.Categories.Add(cat);
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory) {
            ITSection section = (ITSection)parentCategory.Other;
            
            parentCategory.Other = section;
            parentCategory.SubCategories = new List<Category>();

            ITResult result = _trailersApi.Get(section);
            if (result == ITResult.Failed)
                return parentCategory.SubCategories.Count;

            parentCategory.SubCategories = new List<Category>();
            foreach (ITSection subSection in section.Sections) {
                Category cat = new Category();
                cat.ParentCategory = parentCategory;
                cat.Name = subSection.Name;
                cat.Other = subSection;
                cat.HasSubCategories = hasSubCategories(subSection);
                parentCategory.SubCategories.Add(cat);
            }
            
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category) {
            ITSection section = (ITSection)category.Other;
            _sectionPages = new Stack<ITSection>();
            return getVideoList(section);
        }

        public List<VideoInfo> getVideoChoices(VideoInfo video) {
            List<VideoInfo> clips = new List<VideoInfo>();

            // make the movie request
            ITMovie movie;

            if (video.Other != null && video.Other is MovieDetails) {
                movie = ((MovieDetails)video.Other).Movie;
            }
            else {
                movie = new ITMovie(video.VideoUrl);
                video.Other = new MovieDetails(movie);
            }

            ITResult result = _trailersApi.Update(movie);
            if (movie.State != ITState.Complete)
                return clips;

            // complete movie metadata
            video.Description = movie.Synopsis;
            video.Length = movie.ReleaseDate != DateTime.MinValue ? movie.ReleaseDate.ToShortDateString() : "Coming Soon";
            video.ImageUrl = movie.Poster != null ? movie.Poster.Large.AbsoluteUri : string.Empty;
            video.VideoUrl = movie.Uri.AbsoluteUri;

            // get initial video list
            foreach (ITVideo clip in movie.Videos) {
                VideoInfo vid = new VideoInfo();
                //vid.Other = clip;
                vid.Other = VideoDetails.Create(clip);
                vid.Title = movie.Title + " - " + clip.Title;
                vid.Title2 = clip.Title;
                vid.Description = movie.Synopsis;
                //vid.Length = clip.Duration.ToString();
                vid.Length = clip.Published != DateTime.MinValue ? clip.Published.ToShortDateString() : "N/A";
                vid.ImageUrl = movie.Poster != null ? movie.Poster.Uri.AbsoluteUri : string.Empty;
                vid.ThumbnailImage = video.ThumbnailImage;
                vid.VideoUrl = clip.Uri.AbsoluteUri;
                clips.Add(vid);
            }

           return clips;
        }

        public override List<VideoInfo> getNextPageVideos() {
            ITSection nextSection = _sectionPages.Peek().Sections[0];
            return getVideoList(nextSection);
        }

        public override List<VideoInfo> getPreviousPageVideos() {
            ITSection currentSection = _sectionPages.Pop();
            ITSection prevSection = _sectionPages.Pop();

            return getVideoList(prevSection);
        }       

        public override string getUrl(VideoInfo video) {
            string videoUrl = string.Empty;
            
            ITVideo clip;
            if (video.Other != null && video.Other is ITVideo) {
                clip = (ITVideo)video.Other;
            }
            else {
                clip = new ITVideo(video.VideoUrl);
                video.Other = clip;
            }

            ITResult result = _trailersApi.Update(clip);
            if (clip.State != ITState.Complete)
                return videoUrl;

            video.Length = clip.Duration.ToString();

            Dictionary<string, string> files = new Dictionary<string, string>();

            foreach (KeyValuePair<VideoQuality, Uri> file in clip.Files) {
                files[file.Key.ToTitleString()] = ReverseProxy.GetProxyUri(this, file.Value.AbsoluteUri);
            }

            // no files
            if (files.Count == 0)
                return videoUrl;

            if (AlwaysPlaybackHighestQuality) {
                // todo: the last value should be the highest quality tho dictionary doesn't garantee order (should verify this)
                videoUrl = files.Values.ToArray()[files.Count - 1];
            }
            else {
                video.PlaybackOptions = files;
                if (files.Count > 0 && !files.TryGetValue(PreferredVideoQuality.ToTitleString(), out videoUrl)) {
                    videoUrl = files.Values.ToArray()[files.Count - 1];
                }
            }

            return videoUrl;
        }

        public override List<VideoInfo> Search(string query) {
            List<VideoInfo> videos = new List<VideoInfo>();
            List<ITMovie> movies = _trailersApi.Search(query);
            
            foreach (ITMovie movie in movies) {
                VideoInfo video = createVideoInfoFromMovie(movie);
                videos.Add(video);
            }

            return videos;
        }

        #endregion

    }

}

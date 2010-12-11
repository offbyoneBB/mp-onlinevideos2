using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using Pondman.OnlineVideos;
using Pondman.OnlineVideos.ITunes;
using Pondman.OnlineVideos.ITunes.Nodes;
using OnlineVideos.Sites.apondman.ITMovieTrailers;
using Pondman.OnlineVideos.Interfaces;
using HtmlAgilityPack;

namespace OnlineVideos.Sites.apondman {

    /// <summary>
    /// iTunes Movie Trailers
    /// </summary>
    public partial class ITMovieTrailersUtil : SiteUtilBase, IChoice, ISimpleRequestHandler {

        #region iTunes Movie Trailers

        ISession apiSession = null;

        Stack<Section> _sectionPages;

        /// <summary>
        /// Initialize
        /// </summary>
        private void Init() {
            
            // create movie trailer session
            if (apiSession == null) 
            {
                apiSession = API.GetSession();
                apiSession.MakeRequest = doWebRequest;
            }

            // add a special reversed proxy handler
            ReverseProxy.AddHandler(this);

        }

        /// <summary>
        /// Make a webrequest
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private string doWebRequest(string uri) {
            return GetWebData(uri, null, null, null, false, true);
        }

        /// <summary>
        /// Creates a new VideoInfo object using an instance of ITMovie
        /// </summary>
        /// <param name="movie"></param>
        /// <returns></returns>
        private VideoInfo createVideoInfoFromMovie(Movie movie) {
            VideoInfo video = new VideoInfo();
            video.Other = new MovieDetails(movie);
            video.Title = movie.Title;
            video.ImageUrl = movie.Poster != null ? movie.Poster.Large : string.Empty;
            
            // extra
            string actors = movie.Actors.ToCommaSeperatedString();
            video.Description = String.IsNullOrEmpty(movie.Synopsis) ? actors : movie.Synopsis;
            video.Length = movie.ReleaseDate != DateTime.MinValue ? movie.ReleaseDate.ToShortDateString() : "Coming Soon";

            return video;
        }

        private bool hasSubCategories(Section section) {
            string uri = section.Uri;

            // the following sections have sub categories
            if (uri == apiSession.Config.FeaturedGenresUri || uri == apiSession.Config.FeaturedStudiosUri ||
                 uri == Section.FeaturedUri || uri == Section.StudiosUri || uri == Section.GenresUri) {
                return true;
            }

            return false;
        }

        private List<VideoInfo> getVideoList(Section section) {
            List<VideoInfo> videos = new List<VideoInfo>();

            ITResult result = section.Update();
            if (section.State != ITState.Complete) {
                return videos;
            }

            _sectionPages.Push(section);

            foreach (Movie movie in section.Movies) {
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
                return ( _sectionPages != null && _sectionPages.Count > 0 && _sectionPages.Peek().Sections.Count > 0);
            }
        }

        public override bool HasPreviousPage {
            get {
                return (_sectionPages != null && _sectionPages.Count > 1);
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

            Section rootSection = API.Browse(apiSession);

            foreach (Section section in rootSection.Sections) {
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
            Section section = (Section)parentCategory.Other;
            
            parentCategory.Other = section;
            parentCategory.SubCategories = new List<Category>();

            ITResult result = section.Update();
            if (result == ITResult.Failed)
            {
                return parentCategory.SubCategories.Count;
            }

            parentCategory.SubCategories = new List<Category>();
            foreach (Section subSection in section.Sections) {
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
            Section section = (Section)category.Other;
            _sectionPages = new Stack<Section>();
            return getVideoList(section);
        }

        public List<VideoInfo> getVideoChoices(VideoInfo video) {
            List<VideoInfo> clips = new List<VideoInfo>();

            // make the movie request
            Movie movie;

            if (video.Other != null && video.Other is MovieDetails) {
                movie = ((MovieDetails)video.Other).Movie;
            }
            else {
                movie = apiSession.Get<Movie>(video.VideoUrl);
                video.Other = new MovieDetails(movie);
            }

            ITResult result = movie.Update();
            if (movie.State != ITState.Complete)
            {
                return clips;
            }

            // complete movie metadata
            video.Description = movie.Synopsis;
            video.Length = movie.ReleaseDate != DateTime.MinValue ? movie.ReleaseDate.ToShortDateString() : "Coming Soon";
            video.ImageUrl = movie.Poster != null ? movie.Poster.Large : string.Empty;
            video.VideoUrl = movie.Uri;

            // get initial video list
            foreach (Video clip in movie.Videos) {
                VideoInfo vid = new VideoInfo();
                //vid.Other = clip;
                vid.Other = new VideoDetails(clip);
                vid.Title = movie.Title + " - " + clip.Title;
                vid.Title2 = clip.Title;
                vid.Description = movie.Synopsis;
                //vid.Length = clip.Duration.ToString();
                vid.Length = clip.Published != DateTime.MinValue ? clip.Published.ToShortDateString() : "N/A";
                vid.ImageUrl = movie.Poster != null ? movie.Poster.Uri : string.Empty;
                vid.ThumbnailImage = video.ThumbnailImage;
                vid.VideoUrl = clip.Uri;
                clips.Add(vid);
            }

           return clips;
        }

        public override List<VideoInfo> getNextPageVideos() {
            Section nextSection = _sectionPages.Peek().Sections[0];
            return getVideoList(nextSection);
        }

        public override List<VideoInfo> getPreviousPageVideos() {
            Section currentSection = _sectionPages.Pop();
            Section prevSection = _sectionPages.Pop();

            return getVideoList(prevSection);
        }       

        public override string getUrl(VideoInfo video) {
            string videoUrl = string.Empty;
            
            Video clip;
            if (video.Other != null && video.Other is Video) {
                clip = apiSession.Get<Video>(video.VideoUrl);
            }
            else {
                clip = apiSession.Get<Video>(video.VideoUrl);
                video.Other = new VideoDetails(clip);
            }

            ITResult result = clip.Update();
            if (clip.State != ITState.Complete)
            {
                return videoUrl;
            }

            video.Length = clip.Duration.ToString();

            Dictionary<string, string> files = new Dictionary<string, string>();

            foreach (KeyValuePair<VideoQuality, Uri> file in clip.Files) {
                files[file.Key.ToTitleString()] = ReverseProxy.GetProxyUri(this, file.Value.AbsoluteUri);
            }

            // no files
            if (files.Count == 0)
                return videoUrl;

            if (AlwaysPlaybackPreferredQuality) {
                if (files.Count > 0 && !files.TryGetValue(PreferredVideoQuality.ToTitleString(), out videoUrl))
                {
                    video.PlaybackOptions = files;
                    videoUrl = files.Values.ToArray()[files.Count - 1];
                }
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
            List<Movie> movies = API.Search(apiSession, query);
            
            foreach (Movie movie in movies) {
                VideoInfo video = createVideoInfoFromMovie(movie);
                videos.Add(video);
            }

            return videos;
        }

        #endregion

    }

}

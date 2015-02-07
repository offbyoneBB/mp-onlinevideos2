using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using OnlineVideos.Sites.Pondman.Interfaces;
using OnlineVideos.Sites.Pondman.ITunes;
using OnlineVideos.Sites.Pondman.ITunes.Nodes;
using OnlineVideos.Sites.Pondman.Nodes;

namespace OnlineVideos.Sites.Pondman {

	/// <summary>
	/// iTunes Movie Trailers
	/// </summary>
	public partial class ITMovieTrailersUtil : BaseUtil, IChoice
	{

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
		}
   
		/// <summary>
		/// Creates a new VideoInfo object using an instance of ITMovie
		/// </summary>
		/// <param name="movie"></param>
		/// <returns></returns>
		private VideoInfo createVideoInfoFromMovie(Movie movie) {
			VideoInfo video = new VideoInfo();
			video.Other = movie;
			video.Title = movie.Title;
			video.Thumb = movie.Poster != null ? movie.Poster.Large : string.Empty;
			
			// extra
			string actors = movie.Actors.ToCommaSeperatedString();
			video.Description = String.IsNullOrEmpty(movie.Plot) ? actors : movie.Plot;
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

			NodeResult result = section.Update();
			if (section.State != NodeState.Complete) {
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

		#region SiteUtilBase

		public override bool HasNextPage {
			get {
				return ( _sectionPages != null && _sectionPages.Count > 0 && _sectionPages.Peek().Sections.Count > 0);
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

			NodeResult result = section.Update();
			if (result == NodeResult.Failed)
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

		public override List<VideoInfo> GetVideos(Category category)
		{
			Section section = (Section)category.Other;
			_sectionPages = new Stack<Section>();
			return getVideoList(section);
		}

		public List<VideoInfo> GetVideoChoices(VideoInfo video) {
			List<VideoInfo> clips = new List<VideoInfo>();

			// make the movie request
			Movie movie = video.Other as Movie;

			if (movie == null)
			{
				movie = apiSession.Get<Movie>(video.VideoUrl);
				video.Other = movie;
			}

			NodeResult result = movie.Update();
			if (movie.State == NodeState.Complete)
			{
				// complete movie metadata
				video.Description = movie.Plot;
				video.Length = movie.ReleaseDate != DateTime.MinValue ? movie.ReleaseDate.ToShortDateString() : "Coming Soon";
				video.Thumb = movie.Poster != null ? movie.Poster.Large : string.Empty;
				video.VideoUrl = movie.Uri;
			}
			// get initial video list
			foreach (Video clip in movie.Videos) {
				VideoInfo vid = new VideoInfo();
				vid.Other = clip;
				vid.Title = movie.Title + " - " + clip.Title;
				vid.Title2 = clip.Title;
				vid.Description = movie.Plot;
				//vid.Length = clip.Duration.ToString();
				vid.Length = clip.Published != DateTime.MinValue ? clip.Published.ToShortDateString() : "N/A";
				vid.Thumb = !string.IsNullOrEmpty(clip.ThumbUrl) ? clip.ThumbUrl : (movie.Poster != null ? movie.Poster.Uri : string.Empty);
				//vid.ThumbnailImage = video.ThumbnailImage;
				vid.VideoUrl = clip.Uri;
				clips.Add(vid);
			}

		   return clips;
		}

		public override List<VideoInfo> GetNextPageVideos() {
			Section nextSection = _sectionPages.Peek().Sections[0];
			return getVideoList(nextSection);
		}
		
		public override string GetVideoUrl(VideoInfo video) {
			string videoUrl = string.Empty;

			Video clip = video.Other as Video;
			if (clip == null)
			{
				clip = apiSession.Get<Video>(video.VideoUrl);
				video.Other = clip;
			}
			else if (!string.IsNullOrEmpty(video.VideoUrl) && !video.VideoUrl.StartsWith("file://")) // todo : test with favorites!
			{
				clip = apiSession.Get<Video>(video.VideoUrl);
			}

			NodeResult result = clip.Update();
			if (clip.State != NodeState.Complete)
			{
				return videoUrl;
			}

			video.Length = clip.Duration.ToString();

			Dictionary<string, string> files = new Dictionary<string, string>();

			foreach (var file in clip.Files) {
				var uri = new OnlineVideos.MPUrlSourceFilter.HttpUrl(file.Value);
				uri.UserAgent = QuickTimeUserAgent;
				files[file.Key.ToTitleString()] = uri.ToString();
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

		public override List<SearchResultItem> Search(string query, string category = null) {
			var videos = new List<SearchResultItem>();
			List<Movie> movies = API.Search(apiSession, query);
			
			foreach (Movie movie in movies) {
				VideoInfo video = createVideoInfoFromMovie(movie);
				videos.Add(video);
			}

			return videos;
		}

		public override string GetFileNameForDownload(VideoInfo video, Category category, string url) {
			if (url == null)
				return video.Title; // called when adding to favorites
			else
				return video.Title + ".mov"; // called when downloading
		}
		#endregion

	}

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites.Pondman.IMDb;
using OnlineVideos.Sites.Pondman.IMDb.Model;

namespace OnlineVideos.Sites.Pondman
{

    public partial class IMDbUtil : BaseUtil, IChoice
    {
        Session apiSession = null;

        /// <summary>
        /// Initialize
        /// </summary>
        private void Init()
        {
            // create an IMDb session
            if (apiSession == null)
            {
                apiSession = IMDbAPI.GetSession();
                apiSession.MakeRequest = doWebRequest;
            }
        }

        /// <summary>
        /// Creates a new VideoInfo object using an instance of an IMDb TitleReference
        /// </summary>
        /// <param name="movie"></param>
        /// <returns></returns>
        private VideoInfo createVideoInfoFromTitleReference(TitleReference title)
        {
            VideoInfo video = new VideoInfo();
            video.Other = title;
            video.Title = title.Title;
            video.ImageUrl = getResizedImage(title.Image);
            video.Description = title.Principals.Select(p => p.Name).ToList().ToCommaSeperatedString();
            video.VideoUrl = title.ID;

            return video;
        }

        /// <summary>
        /// Gets the resized image.
        /// </summary>
        /// <param name="input">the image url</param>
        /// <returns></returns>
        private string getResizedImage(string input)
        {
            if (ResizeImageMaximumHeight > 0 && input != null)
            {
                return input.Replace("_.jpg", ".jpg").Replace(".jpg", "._SY" + ResizeImageMaximumHeight + "_.jpg");
            }

            return input;
        }

        #region SiteUtilBase

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
            Init();
        }

        public override bool CanSearch
        {
            get { return true; }
        }

        public override List<VideoInfo> Search(string query)
        {
            List<VideoInfo> videos = new List<VideoInfo>();

            // check if we have an IMDb in the search query
            string id = IMDbAPI.ParseTitleConst(query);
            if (id != null)
            {
                // we found an IMDb id so we do a details request
                TitleDetails title = IMDbAPI.GetTitle(apiSession, id);
                VideoInfo video = new VideoInfo();
                video.Other = title;
                video.Title = title.Title;
                video.Description = title.Plot;
                video.ImageUrl = getResizedImage(title.Image);
                video.VideoUrl = title.ID;

                videos.Add(video);
                
                // return the result
                return videos;
            }

            SearchResults results = IMDbAPI.Search(apiSession, query);

            foreach (ResultType key in results.Titles.Keys)
            {
                var titles = results.Titles[key];
                foreach (TitleReference title in titles)
                {
                    VideoInfo video = createVideoInfoFromTitleReference(title);
                    videos.Add(video);
                }
            }

            return videos;
        }

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            // todo: category translation?

            Category cat = new Category();
            cat.Other = "1";
            cat.Name = "Coming Soon";
            Settings.Categories.Add(cat);

            cat = new Category();
            cat.Other = "2";
            cat.Name = "Most Viewed Trailers";
            Settings.Categories.Add(cat);

            cat = new Category();
            cat.Other = "3";
            cat.Name = "Recent Trailers";
            Settings.Categories.Add(cat);

            cat = new Category();
            cat.Other = "4";
            cat.Name = "Popular Movies";
            Settings.Categories.Add(cat);

            cat = new Category();
            cat.Other = "5";
            cat.Name = "Popular TV Series";
            Settings.Categories.Add(cat);            

            cat = new Category();
            cat.Other = "100";
            cat.Name = "Charts";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            cat = new Category();
            cat.Other = "200";
            cat.Name = "Full-length Movies";
            cat.HasSubCategories = true;
            Settings.Categories.Add(cat);

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parent)
        {
            parent.SubCategories = new List<Category>();
            string catid = parent.Other as string;

            switch (catid)
            {
                // Charts
                case "100":
                    // todo: subcategory translation?
                    addSubCategory(parent, "101", "US Box Office Results");
                    addSubCategory(parent, "102", "MOVIEmeter");
                    addSubCategory(parent, "103", "Top 250 Movies");
                    addSubCategory(parent, "104", "Bottom 100 Films");
                    addSubCategory(parent, "105", "Best Picture winners");
                    break;
                // Full Length Movies
                case "200":
                    string index = "#ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                    foreach (char letter in index)
                    {
                        string n = letter.ToString();
                        addSubCategory(parent, catid + n, n);
                    }
                    break;
            }

            return parent.SubCategories.Count;
        }

        private void addSubCategory(Category parent, string id, string name)
        {
            Category cat = new Category();
            cat.Other = id;
            cat.Name = name;
            cat.ParentCategory = parent;
            parent.SubCategories.Add(cat);
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();

            string catid = category.Other as string;
            List<TitleReference> titles = null;

            switch (catid)
            {
                case "1":
                    titles = IMDbAPI.GetComingSoon(apiSession);
                    break;
                case "2":
                    titles = IMDbAPI.GetTrailersTopHD(apiSession);
                    break;
                case "3":
                    titles = IMDbAPI.GetTrailersRecent(apiSession, 0);
                    break;
                case "4":
                    titles = IMDbAPI.GetTrailersPopular(apiSession, 0);
                    break;
                case "5":
                    titles = IMDbAPI.GetPopularTV(apiSession);
                    break;
                case "101":
                    titles = IMDbAPI.GetBoxOffice(apiSession);
                    break;
                case "102":
                    titles = IMDbAPI.GetMovieMeter(apiSession);
                    break;
                case "103":
                    titles = IMDbAPI.GetTop250(apiSession);
                    break;
                case "104":
                    titles = IMDbAPI.GetBottom100(apiSession);
                    break;
                case "105":
                    titles = IMDbAPI.GetBestPictureWinners(apiSession);
                    break;
                default:
                    if (catid.StartsWith("200"))
                    {
                        titles = IMDbAPI.GetFullLengthMovies(apiSession, catid.Substring(3));
                    }
                    break;
            }

            if (titles != null)
            {
                foreach (TitleReference title in titles)
                {
                    VideoInfo video = createVideoInfoFromTitleReference(title);
                    videos.Add(video);
                }
            }

            return videos;
        }

        public override string getUrl(VideoInfo video)
        {
            string videoUrl = string.Empty;
            VideoDetails clip = video.Other as VideoDetails;

            if (clip == null)
            {
                clip = IMDbAPI.GetVideo(apiSession, video.VideoUrl);
                video.Other = clip;
            }

            Dictionary<string, string> files = new Dictionary<string, string>();

            foreach (KeyValuePair<VideoFormat, string> file in clip.Files)
            {
                // todo: how can I resolve the actual video url on playback instead of here?
                files[file.Key.ToString()] = IMDbAPI.GetVideoFile(apiSession, file.Value);
            }

            // no files
            if (files.Count == 0)
            {
                return videoUrl;
            }

            if (AlwaysPlaybackPreferredQuality)
            {
                if (files.Count > 0 && !files.TryGetValue(PreferredVideoQuality.ToString(), out videoUrl))
                {
                    video.PlaybackOptions = files;
                    videoUrl = files.Values.ToArray()[files.Count - 1];
                }
            }
            else
            {
                video.PlaybackOptions = files;
                if (files.Count > 0 && !files.TryGetValue(PreferredVideoQuality.ToString(), out videoUrl))
                {
                    videoUrl = files.Values.ToArray()[files.Count - 1];
                }
            }

            return videoUrl;
        }

        #endregion

        #region IChoice

        public List<VideoInfo> getVideoChoices(VideoInfo video)
        {
            List<VideoInfo> clips = new List<VideoInfo>();
            
            TitleDetails title = IMDbAPI.GetTitle(apiSession, video.VideoUrl as string);

            video.Other = title;
            video.Title = title.Title;
            video.Description = title.Plot;
            if (!string.IsNullOrEmpty(title.Image))
                video.ImageUrl = getResizedImage(title.Image);
            
            List<VideoReference> videos = title.GetVideos();

            if (videos != null)
            {
                foreach (VideoReference clip in videos)
                {
					if (clip.Description == null)
						clip.Description = video.Description;

                    VideoInfo vid = new VideoInfo();
                    vid.Other = clip;
                    vid.Title = title.Title + " - " + clip.Title;
                    vid.Title2 = clip.Title;
					vid.Description = clip.Description;
                    
                    vid.ImageUrl = clip.Image;
                    vid.VideoUrl = clip.ID;
                    vid.Length = clip.Duration.ToString();
                    clips.Add(vid);
                }
            }

            return clips;
        }

        #endregion
    }
}

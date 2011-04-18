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
            video.ImageUrl = title.Image;
            video.Description = title.Principals.Select(p => p.Name).ToList().ToCommaSeperatedString();
            video.VideoUrl = title.ID;

            return video;
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
            string id = IMDbAPI.GetTitleConstFromInput(query);
            if (id != null)
            {
                // we found an IMDb id so we do a details request
                TitleDetails title = IMDbAPI.GetTitle(apiSession, id);
                VideoInfo video = new VideoInfo();
                video.Other = title;
                video.Title = title.Title;
                video.Description = title.Plot;
                video.ImageUrl = title.Image;
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

            Category cat = new Category();
            cat.Other = "1";
            cat.Name = "Most Viewed";
            Settings.Categories.Add(cat);

            cat = new Category();
            cat.Other = "2";
            cat.Name = "Recent";
            Settings.Categories.Add(cat);

            cat = new Category();
            cat.Other = "3";
            cat.Name = "Popular Movies";
            Settings.Categories.Add(cat);

            cat = new Category();
            cat.Other = "4";
            cat.Name = "Coming Soon";
            Settings.Categories.Add(cat);

            cat = new Category();
            cat.Other = "9";
            cat.Name = "USA Weekend Box-Office";
            Settings.Categories.Add(cat);  

            cat = new Category();
            cat.Other = "8";
            cat.Name = "Weekly MOVIEmeter";
            Settings.Categories.Add(cat);            

            cat = new Category();
            cat.Other = "5";
            cat.Name = "Top 250 movies";
            Settings.Categories.Add(cat);

            cat = new Category();
            cat.Other = "6";
            cat.Name = "Bottom 100 movies";
            Settings.Categories.Add(cat);

            cat = new Category();
            cat.Other = "7";
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

            // Full Length Movies
            if (catid == "7")
            {
                string index = "#ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                foreach (char letter in index)
                {
                    Category cat = new Category();
                    cat.ParentCategory = parent;
                    cat.Other = catid + letter.ToString();
                    cat.Name = letter.ToString();
                    parent.SubCategories.Add(cat);
                }
            }

            return parent.SubCategories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();

            string catid = category.Other as string;
            List<TitleReference> titles = null;

            switch (catid)
            {
                case "9":
                    titles = IMDbAPI.GetBoxOffice(apiSession);
                    break;
                case "8":
                    titles = IMDbAPI.GetMovieMeter(apiSession);
                    break;
                case "6":
                    titles = IMDbAPI.GetBottom100(apiSession);
                    break;
                case "5":
                    titles = IMDbAPI.GetTop250(apiSession);
                    break;
                case "4":
                    titles = IMDbAPI.GetComingSoon(apiSession);
                    break;
                case "3":
                    titles = IMDbAPI.GetTrailersPopular(apiSession);
                    break;
                case "2":
                    titles = IMDbAPI.GetTrailersRecent(apiSession);
                    break;
                case "1":
                    titles = IMDbAPI.GetTrailersTopHD(apiSession);
                    break;
                default:
                    if (catid.StartsWith("7"))
                    {
                        titles = IMDbAPI.GetFullLengthMovies(apiSession, catid.Substring(1));
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

            VideoDetails clip = IMDbAPI.GetVideo(apiSession, video.VideoUrl);
            video.Other = clip;

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
            video.ImageUrl = title.Image;
            
            List<VideoReference> videos = title.GetVideos();

            if (videos != null)
            {
                foreach (VideoReference clip in videos)
                {
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

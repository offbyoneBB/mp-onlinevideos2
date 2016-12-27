using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace OnlineVideos.Sites
{
    public class IteleUtil : GenericSiteUtil
    {
        #region Fields

        internal string _urlVideoList = "http://service.itele.fr/iphone/categorie_news?query=";

        #endregion Fields

        #region Methods

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            RssLink cat = new RssLink()
            {
                Url = "http://service.itele.fr/iphone/topnews",
                Name = "A la une",
                HasSubCategories = false
            };
            Settings.Categories.Add(cat);

            cat = new RssLink()
            {
                Url = "http://service.itele.fr/iphone/emissions?query=chroniques",
                Name = "Chroniques",
                HasSubCategories = false
            };
            Settings.Categories.Add(cat);

            cat = new RssLink()
            {
                Url = _urlVideoList + "france",
                Name = "France",
                HasSubCategories = false
            };
            Settings.Categories.Add(cat);

            cat = new RssLink()
            {
                Url = _urlVideoList + "monde",
                Name = "Monde",
                HasSubCategories = false
            };
            Settings.Categories.Add(cat);

            cat = new RssLink()
            {
                Url = _urlVideoList + "politique",
                Name = "Politique",
                HasSubCategories = false
            };
            Settings.Categories.Add(cat);

            cat = new RssLink()
            {
                Url = _urlVideoList + "justice",
                Name = "Justice",
                HasSubCategories = false
            };
            Settings.Categories.Add(cat);

            cat = new RssLink()
            {
                Url = _urlVideoList + "economie",
                Name = "Economie",
                HasSubCategories = false
            };
            Settings.Categories.Add(cat);

            cat = new RssLink()
            {
                Url = _urlVideoList + "sport",
                Name = "Sport",
                HasSubCategories = false
            };
            Settings.Categories.Add(cat);

            cat = new RssLink()
            {
                Url = _urlVideoList + "culture",
                Name = "Culture",
                HasSubCategories = false
            };
            Settings.Categories.Add(cat);

            cat = new RssLink()
            {
                Url = _urlVideoList + "insolite",
                Name = "Insolite",
                HasSubCategories = false
            };
            Settings.Categories.Add(cat);

            return Settings.Categories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> tVideos = new List<VideoInfo>();
            string sUrl = string.Format((category as RssLink).Url);

            string sContent = GetWebData(sUrl);
            JObject tlist = JObject.Parse(sContent);
            string sArry = "news";
            if ((category as RssLink).Url.Contains("topnews")) { sArry = "topnews"; }
            if ((category as RssLink).Url.Contains("emissions")) { sArry = "programs"; }

            foreach (JObject item in tlist[sArry])
            {
                try
                {
                    VideoInfo vid = new VideoInfo()
                    {
                        Airdate = (string)item["date"],
                        Other = (string)item["video"],
                        Title = (string)item["title"],
                        Description = (string)item["description"],
                        Thumb = (string)item["preview"],
                        VideoUrl = (string)item["video_urlhd"]
                    };

                    if (!string.IsNullOrEmpty(vid.VideoUrl))
                        tVideos.Add(vid);
                }
                catch { }
            }

            return tVideos;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            M3U.M3U.M3UPlaylist play = new M3U.M3U.M3UPlaylist();
            play.Read(video.VideoUrl);
            IEnumerable<OnlineVideos.Sites.M3U.M3U.M3UComponent> telem = from item in play.OrderBy("BRANDWITH")
                                                                         select item;

            if (telem.Count() > 2)
            {
                video.PlaybackOptions = new Dictionary<string, string>();
                video.PlaybackOptions.Add("HD", telem.ToList()[telem.Count() - 2].Path);
                video.PlaybackOptions.Add("SD", telem.ToList()[telem.Count() - 1].Path);
            }

            return telem.Last().Path;
        }

        #endregion Methods
    }
}
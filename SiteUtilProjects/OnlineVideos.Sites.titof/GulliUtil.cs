using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class GulliUtil : GenericSiteUtil
    {
        #region Fields

        private string _baseUrl = "http://replay.gulli.fr/";
        private Category _currentCategory = null;
        private int _currentPage = 1;

        #endregion Fields

        #region Methods

        public override int DiscoverDynamicCategories()
        {
            RssLink cat = new RssLink();
            cat.Url = _baseUrl + "dessins-animes";
            cat.Name = "Dessins-animés";
            cat.Thumb = @"http://replay.gulli.fr/bundles/jeunesseintegrationreplay/images/header/logo-gulli.png";
            cat.Other = 1;
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = _baseUrl + "series";
            cat.Name = "Séries & films";
            cat.Thumb = @"http://replay.gulli.fr/bundles/jeunesseintegrationreplay/images/header/logo-gulli.png";
            cat.Other = 1;
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = _baseUrl + "emissions";
            cat.Name = "Emissions";
            cat.Thumb = @"http://replay.gulli.fr/bundles/jeunesseintegrationreplay/images/header/logo-gulli.png";
            cat.Other = 1;
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = _baseUrl + "derniere_chance";
            cat.Name = "Dernière chance";
            cat.Thumb = @"http://replay.gulli.fr/bundles/jeunesseintegrationreplay/images/header/logo-gulli.png";
            cat.Other = 2;
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = _baseUrl + "all/month";
            cat.Name = "Par date - Moins d'un mois";
            cat.Thumb = @"http://replay.gulli.fr/bundles/jeunesseintegrationreplay/images/header/logo-gulli.png";
            cat.Other = 2;
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = _baseUrl + "all/week";
            cat.Name = "Par date - Moins d'une semaine";
            cat.Thumb = @"http://replay.gulli.fr/bundles/jeunesseintegrationreplay/images/header/logo-gulli.png";
            cat.Other = 2;
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = _baseUrl + "all/day";
            cat.Name = "Par date - Moins d'un jour";
            cat.Thumb = @"http://replay.gulli.fr/bundles/jeunesseintegrationreplay/images/header/logo-gulli.png";
            cat.Other = 2;
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            Settings.DynamicCategoriesDiscovered = true;

            return 1;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> listVideos = new List<VideoInfo>();
            string url = (category as RssLink).Url;
            string webData = GetWebData((category as RssLink).Url);
            _currentCategory = null;
            _currentPage = 1;

            if (1 == ((int)category.Other))
            {
                GetVideos_Method1(listVideos, webData);
            }
            else
                GetVideos_Method2(listVideos, webData);

            if (listVideos.Count > 0)
            {
                _currentCategory = category;
            }
            else
            {
                _currentCategory = null;
                _currentPage = 1;
            }

            return listVideos;
        }

        public override bool HasNextPage { get { return true; } }

        public override List<VideoInfo> GetNextPageVideos()
        {
            List<VideoInfo> listVideos = new List<VideoInfo>();

            if (null != _currentCategory)
            {
                _currentPage++;
                string webData = GetWebData((_currentCategory as RssLink).Url + "/" + _currentPage);

                if (1 == ((int)_currentCategory.Other))
                {
                    GetVideos_Method1(listVideos, webData);
                }
                else
                    GetVideos_Method2(listVideos, webData);

                if (listVideos.Count > 0)
                {
                }
                else
                {
                    _currentCategory = null;
                    _currentPage = 1;
                }
            }

            return listVideos;
        }

        private static void GetVideos_Method1(List<VideoInfo> listVideos, string webData)
        {
            string strRegex = @"<li class=""col-md-4"">\s*<a class=""clearfix"" href=""(?<url>[^""""]*)"">\s*<span class=""wrap-img"">\s*<img src=""(?<thumb>[^""""]*)""\sclass=""img-responsive"">\s*</span>\s*<span class=""bloc"">\s*<span class=""title"">(?<title>[^<]*)</span>\s*<span class=""saison"">(?<saison>[^<]*)</span>\s*<span class=""episode_title"">(?<episode_title>[^<]*)</span>";

            Regex r = new Regex(strRegex,
                 RegexOptions.IgnoreCase);
            Match m = r.Match(webData);

            while (m.Success)
            {
                VideoInfo video = new VideoInfo()
                {
                    VideoUrl = m.Groups["url"].Value,
                    Title = m.Groups["title"].Value.Replace(".", " "),
                    Thumb = m.Groups["thumb"].Value,
                    Description = m.Groups["episode_title"].Value + "\r\n" + m.Groups["saison"].Value.Trim()
                };
                if (video.Title.Contains(" - "))
                {
                    video.Title = video.Title.Substring(0, video.Title.IndexOf(" - "));
                }
                if (!string.IsNullOrEmpty(video.Thumb))
                {
                    video.Thumb = "http:" + video.Thumb;
                }
                else
                {
                    video.Thumb = @"http://replay.gulli.fr/bundles/jeunesseintegrationreplay/images/header/logo-gulli-replay.png";
                }
                listVideos.Add(video);
                m = m.NextMatch();
            }
        }

        private static void GetVideos_Method2(List<VideoInfo> listVideos, string webData)
        {
            string strRegex = @"<li class=""col-md-4"">\s*<a class=""clearfix"" href=""(?<url>[^""""]*)"">\s*<span class=""wrap-img"">\s*<img class=""img-responsive"" src=""(?<thumb>[^""""]*)""/>\s*</span>\s*<span class=""bloc"">\s*<span class=""title"">(?<title>[^""""]*)</span>\s*<span class=""saison"">(?<saison>[^""""]*)</span>\s*<span class=""episode_title"">(?<episode_title>[^""""]*)</span>";

            Regex r = new Regex(strRegex,
                 RegexOptions.IgnoreCase);
            Match m = r.Match(webData);

            while (m.Success)
            {
                VideoInfo video = new VideoInfo()
                {
                    VideoUrl = m.Groups["url"].Value,
                    Title = m.Groups["title"].Value.Replace(".", " "),
                    Thumb = m.Groups["thumb"].Value,
                    Description = m.Groups["episode_title"].Value + "\r\n" + m.Groups["saison"].Value.Trim()
                };
                if (video.Title.Contains(" - "))
                {
                    video.Title = video.Title.Substring(0, video.Title.IndexOf(" - "));
                }
                if (!string.IsNullOrEmpty(video.Thumb))
                {
                    video.Thumb = "http:" + video.Thumb;
                }
                else
                {
                    video.Thumb = @"http://replay.gulli.fr/bundles/jeunesseintegrationreplay/images/header/logo-gulli-replay.png";
                }
                listVideos.Add(video);
                m = m.NextMatch();
            }
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            List<string> tReturn = new List<string>();
            string url = video.VideoUrl.Substring(video.VideoUrl.IndexOf("VOD"));
            string id = url.Replace("VOD", string.Empty);
            string videoFile = "http://httpg3.scdn.arkena.com/10624/id/id_Ipad.smil/playlist.m3u8";
            videoFile = videoFile.Replace("id", id);

            string resultUrl = videoFile;

            M3U.M3U.M3UPlaylist play = new M3U.M3U.M3UPlaylist();
            play.Configuration.Depth = 1;
            play.Read(resultUrl);
            IEnumerable<OnlineVideos.Sites.M3U.M3U.M3UComponent> telem = from item in play.OrderBy("BRANDWITH")
                                                                         select item;

            if (telem.Count() >= 4)
            {
                video.PlaybackOptions = new Dictionary<string, string>();
                video.PlaybackOptions.Add("LOW RES", telem.ToList()[0].Path);
                video.PlaybackOptions.Add("SD", telem.ToList()[2].Path);
            }
            return telem.Last().Path;
        }

        #endregion Methods
    }
}
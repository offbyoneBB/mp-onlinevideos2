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

        #endregion Fields

        #region Methods

        public override int DiscoverDynamicCategories()
        {
            RssLink cat = new RssLink();
            cat.Url = _baseUrl +"dessins-animes";
            cat.Name = "Dessins-animés";
            cat.Thumb = @"http://replay.gulli.fr/bundles/jeunesseintegrationreplay/images/header/logo-gulli.png";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = _baseUrl + "series" ;
            cat.Name = "Séries & films";
            cat.Thumb = @"http://replay.gulli.fr/bundles/jeunesseintegrationreplay/images/header/logo-gulli.png";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = _baseUrl + "emissions";
            cat.Name = "Emissions";
            cat.Thumb = @"http://replay.gulli.fr/bundles/jeunesseintegrationreplay/images/header/logo-gulli.png";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = _baseUrl + "derniere_chance";
            cat.Name = "Dernière chance";
            cat.Thumb = @"http://replay.gulli.fr/bundles/jeunesseintegrationreplay/images/header/logo-gulli.png";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = _baseUrl + "all/plus-vus";
            cat.Name = "Le top Gulli Replay";
            cat.Thumb = @"http://replay.gulli.fr/bundles/jeunesseintegrationreplay/images/header/logo-gulli.png";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = _baseUrl + "all/month";
            cat.Name = "Par date - Moins d'un mois";
            cat.Thumb = @"http://replay.gulli.fr/bundles/jeunesseintegrationreplay/images/header/logo-gulli.png";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = _baseUrl + "all/week";
            cat.Name = "Par date - Moins d'une semaine";
            cat.Thumb = @"http://replay.gulli.fr/bundles/jeunesseintegrationreplay/images/header/logo-gulli.png";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = _baseUrl + "all/day";
            cat.Name = "Par date - Moins d'un jour";
            cat.Thumb = @"http://replay.gulli.fr/bundles/jeunesseintegrationreplay/images/header/logo-gulli.png";
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


            string strRegex = @"<li class=""col-md-4"">\s*<a class=""clearfix"" href=""(?<url>[^""""]*)"">\s*<span class=""wrap-img"">\s*<img src=""(?<thumb>[^""""]*)""\sclass=""img-responsive"">\s*</span>\s*<span class=""bloc"">\s*<span class=""title"">(?<title>[^<]*)</span>\s*<span class=""saison"">(?<saison>[^<]*)</span>\s*<span class=""episode_title"">(?<episode_title>[^<]*)</span>";

            Regex r = new Regex(strRegex,
                 RegexOptions.IgnoreCase);
            Match m = r.Match(webData);


            while (m.Success)
            {
                VideoInfo video = new VideoInfo()
                {
                    VideoUrl = m.Groups["url"].Value,
                    Title = m.Groups["title"].Value.Replace("."," ") ,
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

            return listVideos;
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
            return telem.First().Path;

            //string webData = GetWebData(resultUrl);
            //string rgxstring = @"(?<url>[\w.,?=\/-]*).m3u8";
            //Regex rgx = new Regex(rgxstring);
            //var tresult = rgx.Matches(webData);
            //List<string> tUrl = new List<string>();
            //foreach (Match match in tresult)
            //{
            //    tUrl.Add( videoFile.Replace("playlist", match.Groups["url"].ToString()));

            //    //tUrl.Add(webData.Substring(webData.IndexOf(id), webData.IndexOf("m3u8") - webData.IndexOf(id))+"m3u8" match.Groups["url"]);
            //}

            //if (tUrl.Count==5)
            //{
            //    video.PlaybackOptions = new Dictionary<string, string>();
            //    video.PlaybackOptions.Add("LOW RES", tUrl[0]);
            //    video.PlaybackOptions.Add("SD", tUrl[3]);
            //}

            //return tUrl[0];
            //string nexturl = webData.Substring(webData.IndexOf(id), webData.IndexOf("m3u8") - webData.IndexOf(id))+"m3u8";
            //nexturl = videoFile.Replace("playlist.m3u8",nexturl);

            //return nexturl;
        }

        #endregion Methods
    }
}
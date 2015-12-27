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
            cat.Url = _baseUrl + "nouveautes";
            cat.Name = "Nouveautés";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = _baseUrl + "AaZ";
            cat.Name = "De A à Z";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = _baseUrl + "replay/dessins-animes";
            cat.Name = "Dessins animés";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = _baseUrl + "replay/emissions";
            cat.Name = "Emissions";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = _baseUrl + "replay/series";
            cat.Name = "Séries";
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

            Regex r = new Regex(@"href=""(?<url>[^""]*)""></a><span\sclass=""play_video""></span>\s*<img\ssrc=""(?<thumb>[^""]*)""\swidth=""120""\sheight=""90""\salt=""""\s/>\s*</div>\s*<p>\s*<strong>(?<title>[^<]*)</strong>\s*<span>(?<description>[^<]*)<br/>(?<description2>[^<]*)</span>\s*</p>",
                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);

            Match m = r.Match(webData);
            while (m.Success)
            {
                VideoInfo video = new VideoInfo()
                {
                    VideoUrl = m.Groups["url"].Value,
                    Title = m.Groups["title"].Value,
                    Thumb = m.Groups["thumb"].Value,
                    Description = m.Groups["description"].Value.Trim() + "\n" + m.Groups["description2"].Value.Trim()
                };

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
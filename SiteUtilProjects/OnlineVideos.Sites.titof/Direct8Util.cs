using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OnlineVideos._3rdParty.Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class Direct8Util : GenericSiteUtil
    {
        #region Fields

        internal string _siteindex = "1";
        internal string _sitekey = "d8";
        private string _baseurl = "http://lab.canal-plus.pro/web/app_prod.php/api/replay/{0}";

        #endregion Fields

        #region Methods

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            RssLink dir = new RssLink();
            dir.Name = "Le Direct";
            dir.Url = "http://hls-live-m5-l3.canal-plus.com/live/hls/d8-clair-hd-and/and-hd-clair/index.m3u8";

            dir.HasSubCategories = false;
            Settings.Categories.Add(dir);

            

            string sContent = GetWebData(string.Format(_baseurl, _siteindex));
            JArray tList = JArray.Parse(sContent);

            foreach (JObject obj in tList)
            {
                RssLink cat1 = new RssLink();
                cat1.Name = obj.Value<string>("title");
                cat1.Url = string.Format(_baseurl, _siteindex) + "/" + cat1.Name;

                cat1.HasSubCategories = false;
                JArray prog = obj.Value<JArray>("programs");
                if (prog != null)
                {
                    cat1.HasSubCategories = true;
                    cat1.SubCategories = new List<Category>();
                    foreach (JObject sub in prog)
                    {
                        RssLink cat = new RssLink();
                        cat.Name = sub.Value<string>("title");
                        cat.Url = string.Format("http://lab.canal-plus.pro/web/app_prod.php/api/pfv/list/{0}/", _siteindex) + sub.Value<string>("videos_recent");
                        cat.HasSubCategories = true;
                        cat.SubCategories = new List<Category>();

                        RssLink cat2 = new RssLink();
                        cat2.Name = "Videos récentes";
                        cat2.Url = string.Format("http://lab.canal-plus.pro/web/app_prod.php/api/pfv/list/{0}/", _siteindex) + sub.Value<string>("videos_recent");
                        cat2.HasSubCategories = false;
                        cat.SubCategories.Add(cat2);

                        cat2 = new RssLink();
                        cat2.Name = "Videos les plus vues";
                        cat2.Url = string.Format("http://lab.canal-plus.pro/web/app_prod.php/api/pfv/list/{0}/", _siteindex) + sub.Value<string>("videos_view");
                        cat2.HasSubCategories = false;
                        cat.SubCategories.Add(cat2);

                        cat2 = new RssLink();
                        cat2.Name = "Videos les mieux notés";
                        cat2.Url = string.Format("http://lab.canal-plus.pro/web/app_prod.php/api/pfv/list/{0}/", _siteindex) + sub.Value<string>("videos_hot");
                        cat2.HasSubCategories = false;
                        cat.SubCategories.Add(cat2);

                        cat1.SubCategories.Add(cat);
                    }
                }
                Settings.Categories.Add(cat1);
            }

            return Settings.Categories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> tVideos = new List<VideoInfo>();
            string sUrl = (category as RssLink).Url;
            string sContent = GetWebData((category as RssLink).Url);

            JArray tArray = JArray.Parse(sContent);
            foreach (JObject obj in tArray)
            {
                try
                {
                    VideoInfo vid = new VideoInfo()
                    {
                        Thumb = (string)obj["MEDIA"]["IMAGES"]["PETIT"],
                        Title = (string)obj["INFOS"]["TITRAGE"]["TITRE"],
                        Description = (string)obj["INFOS"]["DESCRIPTION"],
                        Length = (string)obj["DURATION"],
                        VideoUrl = (string)obj["ID"],
                        StartTime = (string)obj["INFOS"]["DIFFUSION"]["DATE"]
                    };
                    tVideos.Add(vid);
                }
                catch { }
            }
            return tVideos;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string sUrl = "http://service.canal-plus.com/video/rest/getvideos/{0}/{1}?format=json";
            sUrl = string.Format(sUrl, _sitekey, video.VideoUrl);
            string sContent = GetWebData(sUrl);
            JObject obj = JObject.Parse(sContent);

            string shls = (string)obj["MEDIA"]["VIDEOS"]["HLS"];

            M3U.M3U.M3UPlaylist play = new M3U.M3U.M3UPlaylist();
            play.Configuration.Depth = 1;
            play.Read(shls);
            IEnumerable<OnlineVideos.Sites.M3U.M3U.M3UComponent> telem = from item in play.OrderBy("BRANDWITH")
                                                                         select item;

            if (telem.Count() > 2)
            {
                video.PlaybackOptions = new Dictionary<string, string>();
                video.PlaybackOptions.Add("SD", telem.ToList()[telem.Count() - 2].Path);
                video.PlaybackOptions.Add("HD", telem.ToList()[telem.Count() - 1].Path);
            }
            return telem.Last().Path;

            //string webdata = GetWebData(shls);
            //string rgxstring = @"http:\/\/(?<url>[\w.,?=\/-]*)";
            //Regex rgx = new Regex(rgxstring);
            //var tresult = rgx.Matches(webdata);
            //List<string> tUrl = new List<string>();
            //foreach (Match match in tresult)
            //{
            //    tUrl.Add(@"http://" + match.Groups["url"]);
            //}

            //if (tUrl.Count > 2)
            //{
            //    video.PlaybackOptions = new Dictionary<string, string>();
            //    video.PlaybackOptions.Add("SD", tUrl[tUrl.Count - 2]);
            //    video.PlaybackOptions.Add("HD", tUrl[tUrl.Count - 1]);
            //}

            //return tUrl[tUrl.Count - 1];
        }

        #endregion Methods
    }
}
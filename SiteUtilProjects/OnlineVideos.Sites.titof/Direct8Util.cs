using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace OnlineVideos.Sites
{
    public class Direct8Util : GenericSiteUtil
    {
        #region Fields

        internal string _siteindex = "1";
        internal string _sitekey = "c8";
        private string _baseurl = "http://lab.canal-plus.pro/web/app_prod.php/api/replay/{0}";
        internal string _baselive = "http://hls-live-m5-l3.canal-plus.com/live/hls/d8-clair-hd-and/and-hd-clair/index.m3u8";

        #endregion Fields

        #region Methods

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            RssLink dir = new RssLink();
            dir.Name = "Le Direct";
            dir.Url = _baselive;

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
                        string subvalue = sub.Value<string>("videos_recent");
                        
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

                        subvalue = sub.Value<string>("videos_view");
                        if (subvalue != "0")
                        {
                            cat2 = new RssLink();
                            cat2.Name = "Videos les plus vues";
                            cat2.Url = string.Format("http://lab.canal-plus.pro/web/app_prod.php/api/pfv/list/{0}/", _siteindex) + sub.Value<string>("videos_view");
                            cat2.HasSubCategories = false;
                            cat.SubCategories.Add(cat2);
                        }

                        subvalue = sub.Value<string>("videos_hot");
                        if (subvalue != "0")
                        {
                            cat2 = new RssLink();
                            cat2.Name = "Videos les mieux notés";
                            cat2.Url = string.Format("http://lab.canal-plus.pro/web/app_prod.php/api/pfv/list/{0}/", _siteindex) + sub.Value<string>("videos_hot");
                            cat2.HasSubCategories = false;
                            cat.SubCategories.Add(cat2);
                        }

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

            if (category.Name == "Le Direct")
            {
                VideoInfo vid = new VideoInfo()
                {
                    Thumb = category.Thumb,
                    Title = category.Name,
                    Description = category.Description,
                    VideoUrl = (category as RssLink).Url,
                };
                tVideos.Add(vid);

                return tVideos;
            }

            string sUrl = (category as RssLink).Url;
            string sContent = GetWebData((category as RssLink).Url);

            if (sContent == "[\"\"]")
            {
                return tVideos;
            }

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
            tArray = null;

            return tVideos;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            string shls= string.Empty;
            try
            {
                if (video.VideoUrl.Contains(".m3u8")) shls = video.VideoUrl;
                else
                {
                    string sUrl = "http://lab.canal-plus.pro/web/app_prod.php/api/pfv/video/{0}/{1}";
                    sUrl = string.Format(sUrl, _sitekey, video.VideoUrl);
                    string sContent = GetWebData(sUrl);
                    JObject obj = JObject.Parse(sContent);

                    shls = (string)obj["main"]["MEDIA"]["VIDEOS"]["HLS"];
                    if (string.IsNullOrEmpty(shls))
                        shls = (string)obj["main"]["MEDIA"]["VIDEOS"]["IPAD"];
                }

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
            }
            catch
            {
                return shls;
            }
            
        }

        #endregion Methods
    }
}
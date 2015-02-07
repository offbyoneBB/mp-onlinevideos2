using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Xml;
using System.Web;
using Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites
{
    public class ViasatUtil : LatestVideosSiteUtilBase
    {
        [Category("OnlineVideosConfiguration"), Description("Url of the swf file that used for playing the videos and rtmp verification")]
        protected string swfUrl = "http://flvplayer.viastream.viasat.tv/flvplayer/play/swf/player.swf";

        [Category("OnlineVideosConfiguration"), Description("Api config")]
        protected string apiConfig;

        [Category("OnlineVideosConfiguration"), Description("Translation All")]
        protected string tAll;

        [Category("OnlineVideosConfiguration"), Description("Translation Episodes")]
        protected string tEpisodes;

        [Category("OnlineVideosConfiguration"), Description("Translation Clips")]
        protected string tClips;

        [Category("OnlineVideosConfiguration"), Description("Country code for search")]
        protected string cc;

        protected string redirectedSwfUrl;

        protected string _searchUrl = "";
        protected string SearchUrl 
        { 
            get 
            { 
                if(string.IsNullOrEmpty(_searchUrl))
                {
                    JObject config = GetWebData<JObject>(apiConfig);
                    JToken search = config["views"].First(v => v["name"].Value<string>() == "search");
                    _searchUrl = search["_links"]["url"]["href"].Value<string>() + "&limit=500";
                    _searchUrl = _searchUrl.Replace("{country}", cc);
                }
                return _searchUrl; 
            } 
        }

        public override List<VideoInfo> GetLatestVideos()
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            try
            {
                JObject json = GetWebData<JObject>(apiConfig);
                JToken view = json["views"].First(v => v["name"].Value<string>() == "startpage");
                string url = view["_links"]["url"]["href"].Value<string>();
                JToken scope = view["channel_scopes"].First(v => v["default"] != null && v["default"].Value<bool>());
                string channel = scope["channel"].Value<string>();
                string mixed = scope["mixed"].Value<uint>().ToString();
                url = url.Replace("{channel}", channel).Replace("{mixed}", mixed);
                videos = GetVideos(new RssLink() { Url = url });
            }
            catch { }
            return videos.Count >= LatestVideosCount ? videos.GetRange(0, (int)LatestVideosCount) : new List<VideoInfo>();
        }

        public override int DiscoverDynamicCategories()
        {
            redirectedSwfUrl = WebCache.Instance.GetRedirectedUrl(swfUrl); // rtmplib does not work with redirected urls to swf files - we find the actual url here
            JObject config = GetWebData<JObject>(apiConfig);
            JToken formats = config["views"].First(v => v["name"].Value<string>() == "formats");
            string url = formats["_links"]["url"]["href"].Value<string>();
            JToken categories = formats["filters"]["categories"];
            JToken channels = formats["filters"]["channels"];

            foreach (JToken channel in channels)
            {
                RssLink channelCat = new RssLink()
                {
                    Name = channel["default"] != null && channel["default"].Value<bool>() ? tAll : channel["name"].Value<string>(),
                    HasSubCategories = true,
                    Url = url,
                    Other = "titles"
                };
                channelCat.Url = channelCat.Url.Replace("{channels}", channel["value"].Value<string>());
                channelCat.Url = channelCat.Url.Replace("{categories}", categories.First(c => c["default"].Value<bool>())["value"].Value<string>());
                channelCat.Url += "&order=title";
                Settings.Categories.Add(channelCat);
            }
            Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > 0;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            parentCategory.SubCategories = new List<Category>();
            if (parentCategory.Other.ToString() == "titles")
            {
                JObject json = GetWebData<JObject>((parentCategory as RssLink).Url);
                foreach (JToken title in json["_embedded"]["formats"])
                {
                    Category c = new RssLink()
                    {
                        Name = title["title"].Value<string>(),
                        Thumb = title["image"].Value<string>(),
                        Url = title["_links"]["seasons"]["href"].Value<string>(),
                        ParentCategory = parentCategory,
                        HasSubCategories = true,
                        Other = "seasons"
                    };
                    parentCategory.SubCategories.Add(c);
                }
            }
            else if (parentCategory.Other.ToString() == "title")
            {
                JObject json = GetWebData<JObject>((parentCategory as RssLink).Url);
                if (json["_links"] != null && json["title"] != null)
                {
                    Category c = new RssLink()
                    {
                        Name = json["title"].Value<string>(),
                        Thumb = json["image"].Value<string>(),
                        Url = json["_links"]["seasons"]["href"].Value<string>(),
                        ParentCategory = parentCategory,
                        HasSubCategories = true,
                        Other = "seasons"
                    };
                    parentCategory.SubCategories.Add(c);
                }
            }
            else if (parentCategory.Other.ToString() == "seasons")
            {
                JObject json = GetWebData<JObject>((parentCategory as RssLink).Url);
                foreach (JToken season in json["_embedded"]["seasons"])
                {
                    string url = season["_links"]["videos"]["href"].Value<string>() + "&order=airdate";
                    uint count = GetWebData<JObject>(url + "&limit=1")["count"]["total_items"].Value<uint>();
                    if (count > 0)
                    {
                        Category c = new RssLink()
                        {
                            Name = season["title"].Value<string>(),
                            Thumb = parentCategory.Thumb,
                            Url = url,
                            ParentCategory = parentCategory,
                            HasSubCategories = true,
                            EstimatedVideoCount = count,
                            Other = ""
                        };
                        parentCategory.SubCategories.Add(c);
                    }
                }
            }
            else
            {
                uint count = GetWebData<JObject>((parentCategory as RssLink).Url + "&limit=1&type=program")["count"]["total_items"].Value<uint>();
                if (count > 0)
                {
                    parentCategory.SubCategories.Add(new RssLink()
                    {
                        Name = tEpisodes,
                        Thumb = parentCategory.Thumb,
                        Url = (parentCategory as RssLink).Url + "&limit=500&type=program",
                        EstimatedVideoCount = count,
                        ParentCategory = parentCategory
                    });
                }
                count = GetWebData<JObject>((parentCategory as RssLink).Url + "&limit=1&type=clip")["count"]["total_items"].Value<uint>();
                if (count > 0)
                {
                    parentCategory.SubCategories.Add(new RssLink()
                    {
                        Name = tClips,
                        Thumb = parentCategory.Thumb,
                        Url = (parentCategory as RssLink).Url + "&limit=500&type=clip",
                        EstimatedVideoCount = count,
                        ParentCategory = parentCategory
                    });
                }
            }
            parentCategory.SubCategoriesDiscovered = parentCategory.SubCategories.Count > 0;
            return parentCategory.SubCategories.Count;
        }


        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            JObject json = GetWebData<JObject>((category as RssLink).Url);
            if (json["_embedded"]["sections"] != null)
            {
                json = (JObject)json["_embedded"]["sections"].First(s => s["_meta"]["section_name"].Value<string>() == "videos.latest");
            }
            foreach (JToken v in json["_embedded"]["videos"].Where(v => !v["publishing_status"]["login_required"].Value<bool>()))
            {
                VideoInfo video = new VideoInfo();
                video.Title = v["title"].Value<string>();
                video.SubtitleUrl = "";
                if (v["sami_path"] != null)
                    video.SubtitleUrl = v["sami_path"].Value<string>();
                if (string.IsNullOrEmpty(video.SubtitleUrl) && v["subtitles_for_hearing_impaired"] != null)
                    video.SubtitleUrl = v["subtitles_for_hearing_impaired"].Value<string>();
                video.VideoUrl = v["_links"]["stream"]["href"].Value<string>();
                string desc = "";
                if (v["summary"] != null)
                    desc = v["summary"].Value<string>();
                if (v["description"] != null)
                    desc += "\r\n" + v["description"].Value<string>();
                video.Description = desc;
                video.ImageUrl = v["_links"]["image"]["href"].Value<string>().Replace("{size}", "230x150");
                if (v["duration"] != null && !string.IsNullOrEmpty(v["duration"].ToString()) && v["duration"].Value<int>() > 0)
                {
                    TimeSpan t = TimeSpan.FromSeconds(v["duration"].Value<int>());
                    video.Length = string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);
                }
                else
                {
                    video.Length = "--:--:--";
                }
                if (v["broadcasts"] != null && v["broadcasts"].Count() > 0 && !string.IsNullOrEmpty(v["broadcasts"].First()["air_at"].ToString()))
                {
                    try
                    {
                        DateTime dt = Convert.ToDateTime(v["broadcasts"].First()["air_at"].Value<string>());
                        video.Airdate = dt.ToString();
                    }
                    catch { }
                }
                //Extra carefull...
                JToken fp = v["format_position"];
                if (fp != null 
                    && !string.IsNullOrEmpty(fp["is_episodic"].ToString())
                    && fp["is_episodic"].Value<bool>() 
                    && !string.IsNullOrEmpty(fp["season"].ToString()) 
                    && !string.IsNullOrEmpty(fp["episode"].ToString())
                    )
                {
                    uint s = fp["season"].Value<uint>();
                    uint e = fp["episode"].Value<uint>();
                    if (s > 0 && e > 0)
                    {
                        ITrackingInfo ti = new TrackingInfo() { VideoKind = VideoKind.TvSeries, Episode = e, Season = s, Title = v["format_title"].Value<string>() };
                        video.Other = ti;
                    }
                }
                videos.Add(video);
            }
            return videos;
        }

        public override string GetVideoUrl(VideoInfo video)
        {

            JObject data = GetWebData<JObject>(video.VideoUrl);

            string playstr = data["streams"]["medium"].Value<string>();
            if (playstr.ToLower().StartsWith("rtmp"))
            {
                int mp4IndexFlash = playstr.ToLower().IndexOf("mp4:");
                int mp4Index = mp4IndexFlash >= 0 ? mp4IndexFlash : playstr.ToLower().IndexOf("flv:"); 
                if (mp4Index > 0)
                {
                    playstr = new MPUrlSourceFilter.RtmpUrl(playstr.Substring(0, mp4Index)) { PlayPath = playstr.Substring(mp4Index), SwfUrl = redirectedSwfUrl, SwfVerify = true }.ToString();
                }
                else
                {
                    playstr = new MPUrlSourceFilter.RtmpUrl(playstr) { SwfUrl = redirectedSwfUrl, SwfVerify = true }.ToString();
                }
            }
            else if (playstr.ToLower().EndsWith(".f4m"))
            {
                playstr += "?hdcore=3.3.0" + "&g=" + OnlineVideos.Sites.Utils.HelperUtils.GetRandomChars(12);
            }

            if (!string.IsNullOrEmpty(video.SubtitleUrl))
            {
                video.SubtitleText = GetSubtitle(video.SubtitleUrl);
                video.SubtitleUrl = "";
            }
            return playstr;
        }

        public override ITrackingInfo GetTrackingInfo(VideoInfo video)
        {
            if (video.Other is ITrackingInfo)
            {
                return video.Other as ITrackingInfo;
            }
            return base.GetTrackingInfo(video);
        }

        public override bool CanSearch
        {
            get
            {
                return !string.IsNullOrEmpty(cc);
            }
        }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            List<SearchResultItem> results = new List<SearchResultItem>();
            string searchUrl = SearchUrl.Replace("{term}", HttpUtility.UrlEncode(query));
            JObject json = GetWebData<JObject>(searchUrl);
            if (json["_embedded"] != null && json["_embedded"]["formats"] != null && json["_embedded"]["formats"].Count() > 0)
            {
                foreach (JToken format in json["_embedded"]["formats"])
                {
                    results.Add(new RssLink()
                    {
                        Name = format["title"].Value<string>(),
                        Url = format["_links"]["self"]["href"].Value<string>(),
                        Thumb = format["_links"]["image"]["href"].Value<string>().Replace("{size}", "230x150"),
                        HasSubCategories = true,
                        Other = "title"
                    });
                }
            }
            return results;
        }

        private string GetSubtitle(string url)
        {
            XmlDocument xDoc = GetWebData<XmlDocument>(url, encoding: System.Text.Encoding.UTF8);
            string srt = string.Empty;
            string srtFormat = "{0}\r\n{1}0 --> {2}0\r\n{3}\r\n";
            string begin;
            string end;
            string text;
            string textPart;
            string line;
            foreach (XmlElement p in xDoc.GetElementsByTagName("Subtitle"))
            {
                text = string.Empty;
                begin = p.GetAttribute("TimeIn");
                end = p.GetAttribute("TimeOut");
                line = p.GetAttribute("SpotNumber");
                XmlNodeList textNodes = p.SelectNodes(".//text()");
                foreach (XmlNode textNode in textNodes)
                {
                    textPart = textNode.InnerText;
                    textPart.Trim();
                    text += string.IsNullOrEmpty(textPart) ? "" : textPart + "\r\n";
                }
                srt += string.Format(srtFormat, line, ReplaceLastOccurrence(begin, ":", ",").Substring(0, begin.Length - 1), ReplaceLastOccurrence(end, ":", ",").Substring(0, begin.Length - 1), text);
            }
            return srt;
        }

        private string ReplaceLastOccurrence(string Source, string Find, string Replace)
        {
            int Place = Source.LastIndexOf(Find);
            string result = Source.Remove(Place, Find.Length).Insert(Place, Replace);
            return result;
        }

    }
}

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
    public class ViasatUtil : SiteUtilBase
    {
        string[] channels = { "TV3", "TV6", "TV8", "TV10" };
        [Category("OnlineVideosConfiguration"), Description("Url of the swf file that used for playing the videos and rtmp verification")]
        protected string swfUrl = "http://flvplayer.viastream.viasat.tv/flvplayer/play/swf/player.swf";

        [Category("OnlineVideosConfiguration"), Description("Url for channel logos")]
        protected string logoUrl = "http://www.tv3.se/sites/all/themes/free_tv/css/{0}.se/images/{1}-logo.png";

        [Category("OnlineVideosConfiguration"), Description("Url for program listing")]
        protected string programsListUrl = "http://www.{0}play.se//mobileapi/format";

        [Category("OnlineVideosConfiguration"), Description("Url for program listing")]
        protected string videosListUrl = "http://www.{0}play.se//mobileapi/detailed?formatid={1}";

        [Category("OnlineVideosConfiguration"), Description("Url for images")]
        protected string imageUrl = "http://play.pdl.viaplay.com/imagecache/290x162/{0}";

        [Category("OnlineVideosConfiguration"), Description("Url for streams")]
        protected string streamUrl = "http://viastream.viasat.tv/PlayProduct/";

        [Category("OnlineVideosUserConfiguration"), LocalizableDisplayName("Download Subtitles"), Description("Chose if you want to download available subtitles or not")]
        protected bool retrieveSubtitles = true;

        protected string redirectedSwfUrl;

        public override int DiscoverDynamicCategories()
        {
            redirectedSwfUrl = GetRedirectedUrl(swfUrl); // rtmplib does not work with redirected urls to swf files - we find the actual url here

            Settings.Categories.Clear();
            foreach (string channel in channels)
            {
                string channelLow = channel.ToLower();
                RssLink cat = new RssLink() { Name = channel, HasSubCategories = true, SubCategoriesDiscovered = false };
                cat.Thumb = string.Format(logoUrl, channelLow, channelLow);
                Settings.Categories.Add(cat);
            }
            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

		public override int DiscoverSubCategories(Category parentCategory)
		{
			parentCategory.SubCategories = new List<Category>();
            string url = (parentCategory as RssLink).Url;
            if (string.IsNullOrEmpty(url))
                url = string.Format(programsListUrl, parentCategory.Name.ToLower());
            JObject data = GetWebData<JObject>(url);
            
            if (data["sections"] != null)
            {
                foreach (JToken section in data["sections"])
                {
                    foreach (JToken format in section["formats"])
                    {
                        RssLink subCat = new RssLink()
                        {
                            Name = (string)format["title"],
                            Url = string.Format(videosListUrl, parentCategory.Name.ToLower(), (string)format["id"]),
                            ParentCategory = parentCategory,
                            Thumb = string.Format(imageUrl, (string)format["image"]),
                            HasSubCategories = true
                        };
                        parentCategory.SubCategories.Add(subCat);
                    }
                }
            }
            else if (data["formatcategories"] != null)
            {
                foreach (JToken formatCategory in data["formatcategories"])
                {
                    RssLink subCat = new RssLink()
                    {
                        Name = (string)formatCategory["name"],
                        Url = (string)formatCategory["videos_call"],
                        ParentCategory = parentCategory,
                        Thumb = string.Format(imageUrl, (string)formatCategory["image"]),
                        HasSubCategories = false
                    };
                    parentCategory.SubCategories.Add(subCat);
                }
            }
			parentCategory.SubCategoriesDiscovered = true;
			return parentCategory.SubCategories.Count;
		}


        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            {
                JObject data = GetWebData<JObject>((category as RssLink).Url);
                foreach (JToken episode in data["video_program"])
                {
                    VideoInfo video = new VideoInfo();
                    video.Title = (string)episode["title"];
                    video.VideoUrl = streamUrl + episode["id"].ToString();
                    video.ImageUrl = string.Format(imageUrl, (string)episode["image"]);
                    video.Description = (string)episode["summary"];
                    videos.Add(video);
                }
                foreach (JToken episode in data["video_clip"])
                {
                    VideoInfo video = new VideoInfo();
                    video.Title = (string)episode["title"];
                    video.VideoUrl = streamUrl + episode["id"].ToString();
                    video.ImageUrl = string.Format(imageUrl, (string)episode["image"]);
                    video.Description = (string)episode["summary"];
                    videos.Add(video);
                }
            }
            return videos;
        }

        public override string getUrl(VideoInfo video)
        {
            string doc = GetWebData(video.VideoUrl);
            doc = System.Text.RegularExpressions.Regex.Replace(doc, "&(?!amp;)", "&amp;");
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(doc);

            string sub = null;
            if (retrieveSubtitles)
            {
                var subNode = xDoc.SelectSingleNode("Products/Product/SamiFile");
                if (subNode != null)
                    sub = subNode.InnerText;
            }
            
            string playstr = xDoc.SelectSingleNode("Products/Product/Videos/Video/Url").InnerText;

            XmlNode geo;
            if ((geo = xDoc.SelectSingleNode("Products/Product/Geoblock")) != null)
            {
                if (geo.InnerText == "true")
                {
                    xDoc.LoadXml(GetWebData(playstr));
                    if (xDoc.SelectSingleNode("GeoLock/Success").InnerText != "false")
                    {
                        playstr = xDoc.SelectSingleNode("GeoLock/Url").InnerText;
                    }
                    else
                    {
                        throw new OnlineVideosException(xDoc.SelectSingleNode("GeoLock/Msg").InnerText);
                    }
                }
            }

            if (playstr.ToLower().StartsWith("rtmp"))
            {
                int mp4IndexFlash = playstr.ToLower().IndexOf("mp4:flash");
                int mp4Index = mp4IndexFlash >= 0 ? mp4IndexFlash : playstr.ToLower().IndexOf("mp4:pitcher");
                if (mp4Index > 0)
                {
                    playstr = new MPUrlSourceFilter.RtmpUrl(playstr.Substring(0, mp4Index)) { PlayPath = playstr.Substring(mp4Index), SwfUrl = redirectedSwfUrl, SwfVerify = true }.ToString();
                }
                else
                {
                    playstr = new MPUrlSourceFilter.RtmpUrl(playstr) { SwfUrl = redirectedSwfUrl, SwfVerify = true }.ToString();
                }
            }
            
            if (!string.IsNullOrEmpty(sub) && retrieveSubtitles)
            {
                sub = GetSubtitle(sub);
                video.SubtitleText = sub;
            }
            return playstr;
        }

        private string GetSubtitle(string url)
        {
            XmlDocument xDoc = GetWebData<XmlDocument>(url);
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

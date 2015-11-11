using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Web;
using System.Net;
using System.Xml;
using System.Text.RegularExpressions;
using System.Collections;
using System.ComponentModel;

namespace OnlineVideos.Sites
{
    public class GulliUtil : GenericSiteUtil
    {

        public override int DiscoverDynamicCategories()
        {           
            RssLink cat = new RssLink();
            cat.Url = "http://replay.gulli.fr/nouveautes";
            cat.Name = "Nouveautés";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "http://replay.gulli.fr/AaZ";
            cat.Name = "De A à Z";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "http://replay.gulli.fr/replay/dessins-animes";
            cat.Name = "Dessins animés";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "http://replay.gulli.fr/replay/emissions";
            cat.Name = "Emissions";
            cat.HasSubCategories = false;
            Settings.Categories.Add(cat);

            cat = new RssLink();
            cat.Url = "http://replay.gulli.fr/replay/series";
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

            string resultUrl =  videoFile;

            string webData = GetWebData(resultUrl);
            string rgxstring = @"(?<url>[\w.,?=\/-]*).m3u8";
            Regex rgx = new Regex(rgxstring);
            var tresult = rgx.Matches(webData);
            List<string> tUrl = new List<string>();
            foreach (Match match in tresult)
            {
                tUrl.Add( videoFile.Replace("playlist", match.Groups["url"].ToString()));

                //tUrl.Add(webData.Substring(webData.IndexOf(id), webData.IndexOf("m3u8") - webData.IndexOf(id))+"m3u8" match.Groups["url"]);
            }

            if (tUrl.Count==5)
            {
                video.PlaybackOptions = new Dictionary<string, string>();
                video.PlaybackOptions.Add("LOW RES", tUrl[0]);
                video.PlaybackOptions.Add("SD", tUrl[3]);
            }

            return tUrl[0];
            string nexturl = webData.Substring(webData.IndexOf(id), webData.IndexOf("m3u8") - webData.IndexOf(id))+"m3u8";
            nexturl = videoFile.Replace("playlist.m3u8",nexturl);

            //return nexturl;
        }

        //public override List<string> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        //{
            //List<string> tReturn = new List<string>();
            //inPlaylist = true;
            //string url = video.VideoUrl.Substring(video.VideoUrl.IndexOf("VOD"));
            //string id = url.Replace("VOD",string.Empty );
            //string videoFile = "http://httpg3.scdn.arkena.com/10624/id/id_Ipad.smil/playlist.m3u8";
            //videoFile = videoFile.Replace("id",id); 

            //string resultUrl =  videoFile;

            //string webData = GetWebData(resultUrl);
            //string nexturl = webData.Substring(webData.IndexOf(id), webData.IndexOf("m3u8") - webData.IndexOf(id))+"m3u8";
            //nexturl = videoFile.Replace("playlist.m3u8",nexturl);

            //webData = GetWebData(nexturl);
            //Regex r = new Regex(id + @"(?<url>[\w_=-]*).ts");
            //var tmatch = r.Matches(webData);

            
            //foreach (Match item in tmatch)
            //{
            //    tReturn.Add(videoFile.Replace("playlist.m3u8", item.Value));
            //}

            //return tReturn;
        //}
    }
}

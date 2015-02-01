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
            cat.Url = "http://replay.gulli.fr/AaZ";
            cat.Name = "De A à Z";
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
                VideoInfo video = new VideoInfo();
                video.VideoUrl = m.Groups["url"].Value;
                video.Title = m.Groups["title"].Value;
                video.ImageUrl = m.Groups["thumb"].Value;
                video.Description = m.Groups["description"].Value.Trim() + "\n" + m.Groups["description2"].Value.Trim();
                listVideos.Add(video);
                m = m.NextMatch();
            }
           
            return listVideos;
        }

        public override List<string> GetMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {            
            string url = video.VideoUrl.Substring(video.VideoUrl.IndexOf("VOD"));
            List<string> listUrls = new List<string>();
            string smil = GetWebData(@"http://replay.gulli.fr/var/storage/imports/replay/smil/" + url + @".smil");
            string videoFile = Regex.Match(smil, @"<video region=""video"" src=""(?<m0>[^""]*)""").Groups["m0"].Value;
            string resultUrl = "rtmp://stream2.lgdf.yacast.net/gulli_replay/mp4:" + videoFile;
            listUrls.Add(resultUrl);
            return listUrls;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;

namespace OnlineVideos.Sites
{
    public class DrieVoor12Util : GenericSiteUtil
    {
        public override List<VideoInfo> GetVideos(Category category)
        {
            List<VideoInfo> result = base.GetVideos(category);
            result.ForEach(item => item.Other = ((RssLink)category).Url);
            return result;
        }

        public override string GetVideoUrl(VideoInfo video)
        {
            int p = video.VideoUrl.LastIndexOf('.');
            Match m = Regex.Match(video.VideoUrl, @"program\.(?<id>\d*)\.html");
            if (m.Success)
            {
                string webData = GetWebData(@"http://3voor12.vpro.nl/odi/",
                    "locations=" + HttpUtility.UrlEncode(
                    String.Format(@"[{{""urn"":""urn:vpro:media:program:{0}"",""extension"":""mp4""}}]", m.Groups["id"].Value)
                    ),
                    cookies: GetCookie(), referer: (string)video.Other);
                Match matchFileUrl = regEx_FileUrl.Match(webData);
                if (matchFileUrl.Success)
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<string>(matchFileUrl.Groups["m0"].Value);
            }
            return String.Empty;
        }
    }
}

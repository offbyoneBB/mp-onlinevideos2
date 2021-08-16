using System;
using System.ComponentModel;
using OnlineVideos.MPUrlSourceFilter;

namespace OnlineVideos.Sites
{
    public class HttpUtil : GenericSiteUtil
    {
        [Category("OnlineVideosConfiguration"), Description("LiveStream")]
        protected bool liveStream;

        [Category("OnlineVideosConfiguration"), Description("Referer")]
        protected String referer = HttpUrl.DefaultHttpReferer;

        [Category("OnlineVideosConfiguration"), Description("UserAgent")]
        protected String userAgent = HttpUrl.DefaultHttpUserAgent;

        private String streamFileName = HttpUrl.DefaultStreamFileName;
        public override string GetVideoUrl(VideoInfo video)
        {
            HttpUrl httpUrl = new HttpUrl(video.VideoUrl)
            {
                LiveStream = liveStream,
                Referer = referer,
                UserAgent = userAgent,
                StreamFileName = streamFileName
            };
            return httpUrl.ToString();
        }
    }
}

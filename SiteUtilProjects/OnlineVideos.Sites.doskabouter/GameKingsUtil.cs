
namespace OnlineVideos.Sites
{
    public class GameKingsUtil : GenericSiteUtil
    {
        public override string GetVideoUrl(VideoInfo video)
        {
            string url = base.GetVideoUrl(video);
            int p = url.IndexOf(@"http://player.vimeo.com");
            if (p >= 0)
                return url.Substring(p);
            return url;
        }
    }
}

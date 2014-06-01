
namespace OnlineVideos.Sites
{
    public class GameKingsUtil : GenericSiteUtil
    {
        public override string getUrl(VideoInfo video)
        {
            string url = base.getUrl(video);
            int p = url.IndexOf(@"http://player.vimeo.com");
            if (p >= 0)
                return url.Substring(p);
            return url;
        }
    }
}

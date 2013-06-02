using System.Web;

namespace OnlineVideos.Sites
{
    public class TheEscapistUtil : GenericSiteUtil
    {
        public override string getUrl(VideoInfo video)
        {
            string s = base.getUrl(video);
            return HttpUtility.UrlDecode(s);
        }
    }
}

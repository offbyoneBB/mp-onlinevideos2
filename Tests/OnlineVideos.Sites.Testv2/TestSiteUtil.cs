namespace OnlineVideos.Sites.Test
{
    public class TestSiteUtil : SiteUtilBase
    {
        public override List<VideoInfo> GetVideos(Category category)
        {
            return new List<VideoInfo>
            {
                new VideoInfo { Title = "v2" }
            };
        }
    }
}

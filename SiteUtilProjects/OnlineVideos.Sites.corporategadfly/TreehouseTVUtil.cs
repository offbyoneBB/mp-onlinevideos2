namespace OnlineVideos.Sites
{
    /// <summary>
    /// Site Util for treehousetv.com
    /// </summary>
    public class TreehouseTVUtil : YTVUtil
    {
        protected override string landingPageUrl { get { return @"http://media.treehousetv.com/"; } }
        protected override string iframeXpath { get { return @"//div[@id = 'video-container']/iframe"; } }
        protected override int itemsPerPage { get { return 6; } }
    }
}

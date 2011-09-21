
namespace OnlineVideos.Sites
{
    public class BravoUtil : CTVUtilBase
    {
        public override string BaseUrl { get { return @"http://watch.bravo.ca"; } }
        public override string Swf { get { return @"http://watch.bravo.ca/Flash/player.swf?themeURL=http://watch.bravo.ca/themes/CTV/player/theme.aspx"; } }
    }
}

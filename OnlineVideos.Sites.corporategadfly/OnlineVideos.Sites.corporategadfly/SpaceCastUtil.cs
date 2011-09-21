
namespace OnlineVideos.Sites
{
    public class SpaceCastUtil : CTVUtilBase
    {
        public override string BaseUrl { get { return @"http://watch.spacecast.com"; } }
        public override string Swf { get { return @"http://watch.spacecast.com/Flash/player.swf?themeURL=http://watch.spacecast.com/themes/Space/player/theme.aspx"; } }
    }
}

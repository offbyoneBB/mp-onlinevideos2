
namespace OnlineVideos.Sites
{
    public class BravoFactUtil : CTVUtilBase
    {
        public override string BaseUrl { get { return @"http://watch.bravofact.com"; } }
        public override string Swf { get { return @"http://watch.bravofact.com/Flash/player.swf?themeURL=http://watch.bravofact.com/themes/BravoFact/player/theme.aspx"; } }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites
{
    public class DiscoveryUtil : CTVUtilBase
    {
        public override string BaseUrl { get { return @"http://watch.discoverychannel.ca"; } }
        public override string Swf { get { return @"http://watch.discoverychannel.ca/Flash/player.swf?themeURL=http://watch.discoverychannel.ca/themes/Discoverynew/player/theme.aspx"; } }
        public override bool IsLookaheadNeededAtMainLevel { get { return true; } }
    }
}

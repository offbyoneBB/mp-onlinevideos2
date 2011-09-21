using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites
{
    public class ComedyNetworkUtil : CTVUtilBase
    {
        public override string BaseUrl { get { return @"http://watch.thecomedynetwork.ca"; } }
        public override string Swf { get { return @"http://watch.thecomedynetwork.ca/Flash/player.swf?themeURL=http://watch.thecomedynetwork.ca/themes/Comedy/player/theme.aspx"; } }
    }
}

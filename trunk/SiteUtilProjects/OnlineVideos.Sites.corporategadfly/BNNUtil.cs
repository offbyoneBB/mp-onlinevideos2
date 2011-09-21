using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites
{
    public class BNNUtil : CTVUtilBase
    {
        public override string BaseUrl { get { return @"http://watch.bnn.ca"; } }
        public override string Swf { get { return @"http://watch.bnn.ca/news/Flash/player.swf?themeURL=http://watch.bnn.ca/themes/BusinessNews/player/theme.aspx"; } }
        public override bool IsMainCategoryContainsSubCategories { get { return false; } }
        public override bool IsVideoListStartsFromStartingPanelLevel { get { return false; } }
    }
}

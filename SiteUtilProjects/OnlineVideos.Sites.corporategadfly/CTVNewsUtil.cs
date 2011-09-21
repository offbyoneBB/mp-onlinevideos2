using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class CTVNewsUtil : CTVUtilBase
    {
        public override string BaseUrl { get { return @"http://watch.ctv.ca/news"; } }
        public override bool IsMainCategoryContainsSubCategories { get { return false; } }
        public override bool IsVideoListStartsFromStartingPanelLevel { get { return false; } }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites
{
    public class PowerUnlimitedUtil : GenericSiteUtil
    {
        protected override void ExtraVideoMatch(VideoInfo video, System.Text.RegularExpressions.GroupCollection matchGroups)
        {
            video.Airdate = String.Format("{0}-{1} {2}", matchGroups["AirdateDay"].Value.Trim(),
                matchGroups["AirdateMonth"].Value.Trim(), matchGroups["AirdateTime"].Value.Trim());
        }
    }
}

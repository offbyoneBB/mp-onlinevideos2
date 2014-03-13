using System;
using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class TheEscapistUtil : GenericSiteUtil
    {
        protected override void ExtraVideoMatch(VideoInfo video, GroupCollection matchGroups)
        {
            string title2 = matchGroups["Title2"].Value;
            if (!String.IsNullOrEmpty(title2))
                video.Title += " " + title2;
        }
    }
}

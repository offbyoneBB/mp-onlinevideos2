using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using OnlineVideos.Sites.Pondman.IMDb.Model;

namespace OnlineVideos.Sites.Pondman
{
    public partial class IMDbUtil
    {
        [Category("OnlineVideosUserConfiguration"), Description("Always playback the preferred quality.")]
        bool AlwaysPlaybackPreferredQuality = false;

        [Category("OnlineVideosUserConfiguration"), Description("Defines the preferred quality for video playback.")]
        VideoFormat PreferredVideoQuality = VideoFormat.SD;

        [Category("OnlineVideosUserConfiguration"), Description("Defines the maximum height of images when resizing (for poster format). Use 0 to disable resizing and get the full image resolution.")]
        int ResizeImageMaximumHeight = 600;
    }
}

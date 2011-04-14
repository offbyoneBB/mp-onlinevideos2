using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using OnlineVideos.Sites.Pondman.ITunes;

namespace OnlineVideos.Sites.Pondman
{
    public partial class ITMovieTrailersUtil
    {
        [Category("OnlineVideosConfiguration"), Description("Defines the QuickTime user-agent string")]
        string QuickTimeUserAgent = "QuickTime/7.6.2";

        [Category("OnlineVideosUserConfiguration"), Description("Always playback the preferred quality.")]
        bool AlwaysPlaybackPreferredQuality = false;

        [Category("OnlineVideosUserConfiguration"), Description("Defines the preferred quality for trailer playback.")]
        VideoQuality PreferredVideoQuality = VideoQuality.HD480;
    }
}

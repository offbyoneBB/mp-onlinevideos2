using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using Pondman.Metadata.ITunes.MovieTrailers;

namespace OnlineVideos.Sites.apondman
{
    public partial class ITMovieTrailersUtil
    {
        [Category("OnlineVideosConfiguration"), Description("Defines the QuickTime user-agent string")]
        string QuickTimeUserAgent = "QuickTime/7.6.2";

        [Category("OnlineVideosUserConfiguration"), Description("Always playback the highest available quality.")]
        bool AlwaysPlaybackHighestQuality = false;

        [Category("OnlineVideosUserConfiguration"), Description("Defines the preferred quality for trailer playback.")]
        VideoQuality PreferredVideoQuality = VideoQuality.HD480;
    }
}

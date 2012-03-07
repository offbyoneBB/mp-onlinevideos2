using System;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Site Util for ytv.com.
    /// </summary>
    public class YTVUtil : TreehouseTVUtil
    {
        protected override string hashValue { get { return @"142e6d3f11293bcc3e7c5454dae4e2debc220b05"; } }
        protected override string playerId { get { return @"904863021001"; } }
        protected override string publisherId { get { return @"694915334001"; } }
        protected override string baseUrlPrefix { get { return @"http://www.ytv.com/videos/"; } }
    }
}

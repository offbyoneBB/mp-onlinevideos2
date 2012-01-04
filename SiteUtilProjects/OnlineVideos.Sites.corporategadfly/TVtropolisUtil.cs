using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class TVtropolisUtil : CanWestUtilBase
    {
        public override string FeedPIDUrl { get { return @"http://www.tvtropolis.com/video/"; } }
        public override string PlayerTag { get { return @"z/TVTropolis Player - Video Center"; } }
        public override Regex FeedPIDRegex
        {
            get
            {
                return
                    new Regex(@"PID:\s""(?<feedPID>[^""]*)"",",
                        RegexOptions.Compiled);
            }
        }
    }
}

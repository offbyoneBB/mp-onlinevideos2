using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class HistoryTVUtil : CanWestUtilBase
    {
        public override string FeedPIDUrl { get { return @"http://www.history.ca/video/default.aspx"; } }
        public override string PlayerTag { get { return @"z/History Player - Video Center"; } }
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

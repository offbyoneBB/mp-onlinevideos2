using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class DIYUtil : CanWestUtilBase
    {
        public override string FeedPIDUrl { get { return @"http://www.diy.ca/video/"; } }
        public override string PlayerTag { get { return @"z/DIY Network - Video Centre"; } }
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

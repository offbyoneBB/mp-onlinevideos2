using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class HGTVUtil : CanWestUtilBase
    {
        public override string FeedPIDUrl { get { return @"http://www.hgtv.ca/video/"; } }
        public override string PlayerTag { get { return @"z/HGTV Player - Video Center"; } }
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

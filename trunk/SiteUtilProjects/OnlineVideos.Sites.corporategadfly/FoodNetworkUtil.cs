using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class FoodNetworkUtil : CanWestUtilBase
    {
        public override string FeedPIDUrl { get { return @"http://www.foodnetwork.ca/video/"; } }
        public override string PlayerTag { get { return @"z/FOODNET Player - Video Centre"; } }
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

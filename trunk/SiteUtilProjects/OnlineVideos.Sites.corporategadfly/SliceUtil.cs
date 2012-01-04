using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class SliceUtil : CanWestUtilBase
    {
        public override string FeedPIDUrl { get { return @"http://www.slice.ca/Slice/Watch/Default.aspx?ID=v"; } }
        public override string PlayerTag { get { return @"z/Slice Player - New Video Center"; } }
        public override Regex FeedPIDRegex
        {
            get
            {
                return
                    new Regex(@"var\scwpPid\s+=\s""(?<feedPID>[^""]*)"";",
                        RegexOptions.Compiled);
            }
        }
    }
}

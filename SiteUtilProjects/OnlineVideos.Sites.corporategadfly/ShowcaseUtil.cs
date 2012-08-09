using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class ShowcaseUtil : CanWestUtilBase
    {
        public override string FeedPIDUrl { get { return @"http://www.showcase.ca/CommonUI/Scripts/video/cwpShowcaseVC.js"; } }
        public override string PlayerTag { get { return @"z/Showcase Video Centre"; } }
        public override string SwfUrl { get { return @"http://www.showcase.ca/video/swf/flvPlayer.swf"; } }
        public override Regex FeedPIDRegex
        {
            get
            {
                return
                    new Regex(@"data.PID\s=\sdata\.PID\s\|\|\s""(?<feedPID>[^""]*)"";",
                        RegexOptions.Compiled);
            }
        }
    }
}

using System.Text.RegularExpressions;

namespace OnlineVideos.Sites
{
    public class GlobalUtil : CanWestUtilBase
    {
        public override string FeedPIDUrl { get {  return @"http://www.globaltv.com/widgets/ThePlatformContentBrowser/js/cwpGlobalVC.js"; } }
        public override string PlayerTag { get { return @"z/Global Video Centre"; } }
        public override bool IsSwfUrlNeeded { get { return true; } }
        public override string SwfUrl { get { return @"http://www.globaltv.com/widgets/ThePlatformContentBrowser/swf/flvPlayer.swf"; } }
        public override Regex FeedPIDRegex {
            get {
                return 
                    new Regex(@"data.PID\s=\sdata\.PID\s\|\|\s""(?<feedPID>[^""]*)"";",
                    RegexOptions.Compiled);
            }
        }
    }
}

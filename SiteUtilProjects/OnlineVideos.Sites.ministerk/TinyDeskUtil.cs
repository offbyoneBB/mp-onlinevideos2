using System.Collections.Generic;
using System.Linq;

namespace OnlineVideos.Sites
{
    public class TinyDeskUtil : GenericSiteUtil
    {
        public override Dictionary<string, string> GetPlaybackOptions(string playlistUrl)
        {
            return base.GetPlaybackOptions(playlistUrl).OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.Value.Replace("\\/", "/"));
        }
    }
}

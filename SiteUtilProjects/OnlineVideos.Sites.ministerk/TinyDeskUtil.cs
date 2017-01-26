using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites
{
    public class TinyDeskUtil : GenericSiteUtil
    {
        public override Dictionary<string, string> GetPlaybackOptions(string playlistUrl)
        {
            return base.GetPlaybackOptions(playlistUrl).ToDictionary(p => p.Key, p => p.Value.Replace("\\/", "/"));
        }
    }
}

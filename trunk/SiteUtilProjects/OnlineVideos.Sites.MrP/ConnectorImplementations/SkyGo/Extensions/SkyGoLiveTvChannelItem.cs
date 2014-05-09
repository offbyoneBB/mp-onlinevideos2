using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.WebAutomation.ConnectorImplementations.SkyGo.Extensions
{
    /// <summary>
    /// Store some values for the live tv channel
    /// </summary>
    public class SkyGoLiveTvChannelItem
    {
        public string ChannelId { get; set; }
        public string ChannelName { get; set; }
        public string ChannelImageUrl { get; set; }
        public string ChannelVideoId { get; set; }
    }
}

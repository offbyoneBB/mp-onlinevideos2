using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.WebAutomation.ConnectorImplementations.SkyGo.Extensions
{
    /// <summary>
    /// Extend Video with methods to get the Id
    /// </summary>
    public static class SkyGoVideoData
    {
        /// <summary>
        /// Other will contain {Video Id}_{Asset Id}
        /// </summary>
        /// <param name="Video"></param>
        /// <returns></returns>
        public static string VideoId(this VideoInfo video)
        {
            if (string.IsNullOrEmpty(video.Other.ToString())) return string.Empty;
            return video.Other.ToString().Split('~').First();
        }

        /// <summary>
        /// Other will contain {Video Id}_{Asset Id}
        /// </summary>
        /// <param name="Video"></param>
        /// <returns></returns>
        public static string AssetId(this VideoInfo video)
        {
            if (string.IsNullOrEmpty(video.Other.ToString())) return string.Empty;
            return video.Other.ToString().Split('~')[1];
        }
    }
}

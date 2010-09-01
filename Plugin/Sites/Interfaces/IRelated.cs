using System;
using System.Collections.Generic;

namespace OnlineVideos
{
    /// <summary>
    /// If a <see cref="SiteUtilBase"/> implements this interface, the site supports querying for related videos.<br/>
    /// A context menu entry "show related videos" is added on every video.
    /// </summary>
    public interface IRelated
    {
        /// <summary>
        /// This function should return a list of videos that are related to the given <paramref name="video"/>.
        /// </summary>
        /// <param name="video">The <see cref="VideoInfo"/> object, for which to get a list of related of videos.</param>
        /// <returns>A list of <see cref="VideoInfo"/> objects that are related to the input video</returns>
        List<VideoInfo> getRelatedVideos(VideoInfo video);
    }
}

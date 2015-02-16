using System;
using System.Collections.Generic;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// If a <see cref="SiteUtilBase"/> implements this interface, the site has multiple choices for a video.<br/>
	/// The GUI will show a details view with a selection of videos, taken from <see cref="GetVideoChoices"/>.
    /// </summary>
	/// <remarks>An example would be a list of trailers for a movie.</remarks>
    public interface IChoice
    {
        /// <summary>
        /// This function will be called to retreive a list of videos that will be displayed in a details view, as choices for a given video.
        /// </summary>
        /// <param name="video">The base <see cref="VideoInfo"/> object, for which to get a choice of videos.</param>
        /// <returns>A list of <see cref="DetailVideoInfo"/> objects.</returns>
        List<DetailVideoInfo> GetVideoChoices(VideoInfo video);
    }
}

using System.Collections.Generic;

namespace OnlineVideos.Sites.Interfaces
{
    /// <summary>
    /// This interface is implemented by site utils that allow streaming using the InputStream technology, which also
    /// allows using DRM decoding by internal players.
    /// </summary>
    public interface IInputStreamSite
    {
        /// <summary>
        /// Gets the required properties for InputStream player, depending on the used plugin.
        /// Usually the required information must be retrieved for each item, so filling this information
        /// directly during VideoInfo creation is not possible. Also there could be paid content, so
        /// retrieving all data should be avoided.
        /// </summary>
        /// <param name="videoInfo">Current video</param>
        /// <param name="properties">Returns properties</param>
        /// <returns><c>true</c> if successful</returns>
        bool GetStreamProperties(VideoInfo videoInfo, out Dictionary<string, string> properties);
    }
}

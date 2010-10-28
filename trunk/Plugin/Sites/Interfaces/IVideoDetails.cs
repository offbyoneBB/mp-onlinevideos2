using System;
using System.Collections.Generic;
using System.Text;

namespace OnlineVideos
{
    /// <summary>
    /// If the object found at <see cref="VideoInfo.Other"/> implements this interface, the additional info is set to the GUI.
    /// </summary>
    public interface IVideoDetails
    {
        /// <summary>
        /// Populates a dictionary with custom information that can be published to the skin
        /// </summary>
        /// <returns></returns>
        Dictionary<string, string> GetExtendedProperties();
    }
}

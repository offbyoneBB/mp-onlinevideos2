using System;
using System.Collections.Generic;

namespace OnlineVideos
{
    /// <summary>
    /// If the object found at <see cref="VideoInfo.Other"/> property implements this interface, 
	/// the additional info returned by <see cref="GetExtendedProperties"/> is set to the GUI.
    /// </summary>
    public interface IVideoDetails
    {
        /// <summary>
        /// Gives custom information that will be provided to the GUI.
        /// </summary>
		/// <returns>A <see cref="Dictionary"/> with Name -> Value entries that should be exposed to the GUI.</returns>
        Dictionary<string, string> GetExtendedProperties();
    }
}

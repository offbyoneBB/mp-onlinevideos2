using System;
using System.Collections.Generic;
using System.Text;

namespace OnlineVideos {
    public interface IVideoDetails {

        /// <summary>
        /// Populates a dictionary with custom information that can be published to the skin
        /// </summary>
        /// <returns></returns>
        Dictionary<string, string> GetExtendedProperties();

    }
}

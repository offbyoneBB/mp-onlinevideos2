using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1
{
    /// <summary>
    /// Represents log verbosity level of MediaPortal Url Source Filter.
    /// </summary>
    public enum LogVerbosity
    {
        /// <summary>
        /// MediaPortal Url Source Filter doesn't log any output.
        /// </summary>
        None = 0,

        /// <summary>
        /// MediaPortal Url Source Filter logs only errors.
        /// </summary>
        Error,

        /// <summary>
        /// MediaPortal Url Source Filter logs errors and warnings.
        /// </summary>
        Warning,

        /// <summary>
        /// MediaPortal Url Source Filter logs erros, warnings and information.
        /// </summary>
        Information,

        /// <summary>
        /// MediaPortal Url Source Filter logs more verbose output.
        /// </summary>
        /// <remarks>This value is default.</remarks>
        Verbose,

        /// <summary>
        /// MediaPortal Url Source Filter logs everything including data.
        /// </summary>
        /// <remarks>
        /// This verbosity level is only for logging of data and checking workflow of source filter.
        /// It can cause heavy load, stuttering or other issue and is not recommended to use.
        /// </remarks>
        Data
    }
}

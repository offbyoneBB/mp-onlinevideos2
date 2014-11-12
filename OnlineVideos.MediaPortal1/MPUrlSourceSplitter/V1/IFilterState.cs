using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1
{
    /// <summary>
    /// Specifies interface for splitter state.
    /// </summary>
    [ComImport, Guid("420E98EF-0338-472F-B77B-C5BA8997ED10"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IFilterState
    {
        /// <summary>
        /// This method tests if filter is ready to connect output pins.
        /// </summary>
        /// <returns>non zero if filter is ready, zero otherwise</returns>
        [return : MarshalAs(UnmanagedType.Bool)]
        bool IsFilterReadyToConnectPins();

        /// <summary>
        /// This method returns filter cache file path.
        /// </summary>
        /// <returns>path to cache file or null if error or cache file not set</returns>
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetCacheFileName();
    }
}

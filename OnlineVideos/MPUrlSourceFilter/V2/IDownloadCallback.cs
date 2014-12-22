using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace OnlineVideos.MPUrlSourceFilter.V2
{
    /// <summary>
    /// Specifies interface for download callback.
    /// </summary>
    [ComImport, Guid("51D2A240-A172-4FA8-AFD7-CC576EC5CA66"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IDownloadCallback
    {
        /// <summary>
        /// This method is called when download finished.
        /// </summary>
        /// <param name="downloadResult">The result of download process.</param>
        [PreserveSig]
        void OnDownloadCallback([In] int downloadResult);
    }
}

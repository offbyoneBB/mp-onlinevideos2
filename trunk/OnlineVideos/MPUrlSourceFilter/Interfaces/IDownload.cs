using System;
using System.Runtime.InteropServices;

namespace OnlineVideos.MPUrlSourceFilter
{
    /// <summary>
    /// Specifies interface for reporting of the progress of a operation and enables the application to cancel the operation.
    /// </summary>
    [ComImport, Guid("8E1C39A1-DE53-11cf-AA63-0080C744528D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAMOpenProgress
    {
        /// <summary>
        /// Retrieves the progress of the operation.
        /// </summary>
        /// <param name="pllTotal">Variable that receives the length in bytes.</param>
        /// <param name="pllCurrent">Variable that receives the length of the downloaded portion in bytes.</param>
        /// <returns>Returns S_OK if successful, VFW_S_ESTIMATED if values are estimated or an HRESULT value indicating the cause of the error.</returns>
        [PreserveSig]
        int QueryProgress([Out] out long pllTotal, [Out] out long pllCurrent);

        /// <summary>
        /// Cancels the operation.
        /// </summary>
        /// <returns>Returns S_OK if successful or an HRESULT value indicating the cause of the error.</returns>
        [PreserveSig]
        int AbortOperation();
    }

    /// <summary>
    /// Specifies interface for downloading single stream with MediaPortal Url Source Filter.
    /// </summary>
    [ComImport, Guid("B7FDAB2F-9870-4DFC-8CC7-8BBC68B1A3BF"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDownload : IAMOpenProgress
    {
        #region IAMOpenProgress interface

        /// <summary>
        /// Retrieves the progress of the operation.
        /// </summary>
        /// <param name="pllTotal">Variable that receives the length in bytes.</param>
        /// <param name="pllCurrent">Variable that receives the length of the downloaded portion in bytes.</param>
        /// <returns>Returns S_OK if successful, VFW_S_ESTIMATED if values are estimated or an HRESULT value indicating the cause of the error.</returns>
        [PreserveSig]
        new int QueryProgress([Out] out long pllTotal, [Out] out long pllCurrent);

        /// <summary>
        /// Cancels the operation.
        /// </summary>
        /// <returns>Returns S_OK if successful or an HRESULT value indicating the cause of the error.</returns>
        [PreserveSig]
        new int AbortOperation();

        #endregion

        /// <summary>
        /// Starts downloading single stream asynchronously and saves output to specified file name.
        /// </summary>
        /// <param name="uri">The uniform resource identifier of source stream.</param>
        /// <param name="fileName">The full path containing file name to save received stream.</param>
        /// <param name="downloadCallback">The callback method called after downloading finished.</param>
        /// <returns>Returns S_OK if successful, E_POINTER if uri, fileName or downloadCallback is <see langword="null"/> or an HRESULT value indicating the cause of the error.</returns>
        [PreserveSig]
        int DownloadAsync([In, MarshalAs(UnmanagedType.LPWStr)] String uri, [In, MarshalAs(UnmanagedType.LPWStr)] String fileName, [In] IDownloadCallback downloadCallback);

        /// <summary>
        /// Starts downloading single stream synchronously and saves output to specified file name.
        /// </summary>
        /// <param name="uri">The uniform resource identifier of source stream.</param>
        /// <param name="fileName">The full path containing file name to save received stream.</param>
        /// <returns>Returns S_OK if successful, E_POINTER if uri or fileName is <see langword="null"/> or an HRESULT value indicating the cause of the error.</returns>
        [PreserveSig]
        int Download([In, MarshalAs(UnmanagedType.LPWStr)] String uri, [In, MarshalAs(UnmanagedType.LPWStr)] String fileName);
    }
}

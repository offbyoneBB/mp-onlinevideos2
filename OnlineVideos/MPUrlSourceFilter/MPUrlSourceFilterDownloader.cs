using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace OnlineVideos.MPUrlSourceFilter
{
    /// <summary>
    /// Represents class for downloading single stream with MediaPortal Url Source Filter.
    /// </summary>
    public class MPUrlSourceFilterDownloader : MarshalByRefObject, IDownloader, IDownloadCallback
    {
        #region Private fields

        System.Threading.Thread downloadThread;
        private Boolean downloadFinished;
        private int downloadResult;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="MPUrlSourceFilterDownloader"/> class.
        /// </summary>
        public MPUrlSourceFilterDownloader()
        {
            this.downloadFinished = false;
            this.downloadResult = 0;
        }

        #endregion

        #region Properties

        public bool Cancelled { get; private set; }

        #endregion

        #region Methods

        public void Abort()
        {
            if (downloadThread != null)
            {
                downloadThread.Abort();
            }
        }

        public void CancelAsync()
        {
            this.Cancelled = true;
        }

        public Exception Download(DownloadInfo downloadInfo)
        {
            try
            {
                downloadThread = System.Threading.Thread.CurrentThread;
                this.downloadResult = 0;
                this.downloadFinished = false;

                IDownload downloadFilter = (IDownload)new MPUrlSourceFilter();
                int result = downloadFilter.DownloadAsync(downloadInfo.Url, downloadInfo.LocalFile, this);
                // throw exception if occured while initializing download
                Marshal.ThrowExceptionForHR(result);

                while (!this.downloadFinished)
                {
                    long total = 0;
                    long current = 0;
                    if (downloadFilter.QueryProgress(out total, out current) >= 0)
                    {
                        // succeeded or estimated value
                        downloadInfo.DownloadProgressCallback(total, current);
                    }

                    // sleep some time
                    System.Threading.Thread.Sleep(100);
                }

                // throw exception if occured while downloading
                Marshal.ThrowExceptionForHR(this.downloadResult);

                return null;
            }
            finally
            {
            }
        }

        internal void OnDownloadCallback(int downloadResult)
        {
            this.downloadResult = downloadResult;
            this.downloadFinished = true;
        }

        #endregion

        #region Constants
        #endregion

        #region Internals

        /// <summary>
        /// Defines MediaPortal Url Source Filter.
        /// </summary>
        [ComImport, Guid("87DD67C7-5D13-4CD5-819B-586FFCE8650F")]
        private class MPUrlSourceFilter { } ;

        #endregion

        #region IDownloadCallback interface

        void IDownloadCallback.OnDownloadCallback(int downloadResult)
        {
            this.OnDownloadCallback(downloadResult);
        }

        #endregion

        #region MarshalByRefObject overrides

        public override object InitializeLifetimeService()
        {
            // In order to have the lease across appdomains live forever, we return null.
            return null;
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace OnlineVideos.MediaPortal1.MPUrlSourceSplitter.V1
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
            this.Cancelled = false;
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
			IDownload downloadFilter = null;
            try
            {
                downloadThread = System.Threading.Thread.CurrentThread;
                this.downloadResult = 0;
                this.downloadFinished = false;
                this.Cancelled = false;

                downloadFilter = (IDownload)new MPUrlSourceFilter();
                int result = downloadFilter.DownloadAsync(downloadInfo.Url, downloadInfo.LocalFile, this);
                // throw exception if error occured while initializing download
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

                    if (this.Cancelled)
                    {
                        downloadFilter.AbortOperation();
                        this.downloadFinished = true;
                        this.downloadResult = 0;
                    }
                }

                // throw exception if error occured while downloading
                Marshal.ThrowExceptionForHR(this.downloadResult);

                return null;
            }
			catch (Exception ex)
			{
				return ex;
			}
            finally
            {
                if (downloadFilter != null)
                {
                    Marshal.ReleaseComObject(downloadFilter);
                }
            }
        }

        internal void OnDownloadCallback(int downloadResult)
        {
            this.downloadResult = downloadResult;
            this.downloadFinished = true;
        }

		public static void ClearDownloadCache()
		{
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MPUrlSource");
			if (Directory.Exists(path)) foreach (var file in Directory.GetFiles(path)) try { File.Delete(file); } catch {}
			path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MPUrlSourceSplitter");
			if (Directory.Exists(path))foreach (var file in Directory.GetFiles(path)) try { File.Delete(file); } catch {}
		}

        #endregion

        #region Constants

        public const string FilterName = "MediaPortal Url Source Splitter";
        public const string FilterCLSID = "59ED045A-A938-4A09-A8A6-8231F5834259";

        #endregion

        #region Internals

        /// <summary>
        /// Defines MediaPortal Url Source Filter.
        /// </summary>
        [ComImport, Guid(FilterCLSID)]
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

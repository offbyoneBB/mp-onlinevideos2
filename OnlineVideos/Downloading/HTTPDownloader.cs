using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;

namespace OnlineVideos.Downloading
{
	public class HTTPDownloader : MarshalByRefObject, IDownloader
    {
		System.Threading.Thread downloadThread;

        public bool Cancelled { get; private set; }

        public void CancelAsync()
        {
            Cancelled = true;
        }

        public Exception Download(DownloadInfo downloadInfo)
        {
            HttpWebResponse response = null;
            try
            {
				downloadThread = System.Threading.Thread.CurrentThread;
                using (FileStream fs = new FileStream(downloadInfo.LocalFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    HttpWebRequest request = (HttpWebRequest)System.Net.WebRequest.Create(downloadInfo.Url);
                    request.Timeout = 15000;
                    request.UserAgent = OnlineVideoSettings.Instance.UserAgent;
                    request.Accept = "*/*";
                    request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
                    response = (HttpWebResponse)request.GetResponse();
                                        
                    Stream responseStream;
                    if (response.ContentEncoding.ToLower().Contains("gzip"))
                        responseStream = new System.IO.Compression.GZipStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                    else if (response.ContentEncoding.ToLower().Contains("deflate"))
                        responseStream = new System.IO.Compression.DeflateStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                    else
                        responseStream = response.GetResponseStream();

                    long size = response.ContentLength;
                    int buffSize = 4096;
                    byte[] buffer = new byte[buffSize];
                    long totalRead = 0;
                    long readSize;
                    do
                    {
                        readSize = responseStream.Read(buffer, 0, buffSize);
                        totalRead += readSize;
                        fs.Write(buffer, 0, (int)readSize);
                        downloadInfo.DownloadProgressCallback(size, totalRead);
                    }
                    while (readSize > 0 && !Cancelled);

                    fs.Flush();
                    fs.Close();

                    return null;
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                if (response != null) response.Close();
            }
        }

		public void Abort()
		{
			if (downloadThread != null) downloadThread.Abort();
		}

		#region MarshalByRefObject overrides
		public override object InitializeLifetimeService()
		{
			// In order to have the lease across appdomains live forever, we return null.
			return null;
		}
		#endregion
    }
}

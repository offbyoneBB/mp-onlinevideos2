using System;
using System.Net;

namespace OnlineVideos.WebService
{
    internal class CompressionWebClient : WebClient
    {
        // The default timeout for HttpWebRequest is 100 seconds
        public const int DEFAULT_REQUEST_TIMEOUT = 100000;

        protected bool _enableCompression;

        protected int _requestTimeOut = DEFAULT_REQUEST_TIMEOUT;

        public int RequestTimeout
        {
            get { return _requestTimeOut; }
            set { _requestTimeOut = value; }
        }

        public CompressionWebClient()
          : this(true)
        {
        }

        public CompressionWebClient(bool enableCompression)
        {
            _enableCompression = enableCompression;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = base.GetWebRequest(address) as HttpWebRequest;
            if (request != null)
            {
                request.Timeout = RequestTimeout;
                if (_enableCompression)
                {
                    Headers["Accept-Encoding"] = "gzip, deflate";
                    request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                }
            }
            return request;
        }
    }
}

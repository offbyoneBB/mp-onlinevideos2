using System;
using System.Linq;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using OnlineVideos;

namespace RTMP_LIB
{
    public class RTMPRequestHandler_OLD : OnlineVideos.IRequestHandler
    {
        #region Singleton
		private static RTMPRequestHandler_OLD _Instance = null;
		public static RTMPRequestHandler_OLD Instance
        {
            get
            {
				if (_Instance == null) _Instance = new RTMPRequestHandler_OLD();
                return _Instance;
            }
        }
		private RTMPRequestHandler_OLD() 
		{
			ReverseProxy.AddHandler(this);
		}
        #endregion

        object sync = new object();

        bool invalidHeader = false;

        public bool DetectInvalidPackageHeader()
        {
            return invalidHeader;
        }

        public static void ConnectAndGetStream(RTMP rtmp, HybridDSP.Net.HTTP.HTTPServerRequest request, HybridDSP.Net.HTTP.HTTPServerResponse response,
            ref bool invalidHeader)
        {
            Stream responseStream = null;
            try
            {
                bool connected = rtmp.Connect();

                if (connected)
                {
                    request.KeepAlive = true; // keep connection alive
                    response.ContentType = "video/x-flv";
                    response.KeepAlive = true;
                    //response.ChunkedTransferEncoding = true;

                    FLVStream fs = new FLVStream();

                    fs.WriteFLV(rtmp, delegate()
                    {
                        // we must set a content length for the File Source filter, otherwise it thinks we have no content
                        // but don't set a length if it is our user agent, so a download will always be complete
                        if (request.Get("User-Agent") != OnlineVideos.OnlineVideoSettings.Instance.UserAgent)
                            response.ContentLength = fs.EstimatedLength;

                        responseStream = response.Send();
                        return responseStream;
                    }, response._session._socket);

                    invalidHeader = rtmp.invalidRTMPHeader;

                    if (responseStream != null)
                    {
                        if (request.Get("User-Agent") != OnlineVideos.OnlineVideoSettings.Instance.UserAgent)
                        {
                            // keep appending "0" - bytes until we filled the estimated length when sending data to the File Source filter
                            long zeroBytes = fs.EstimatedLength - fs.Length;
                            while (zeroBytes > 0)
                            {
                                int chunk = (int)Math.Min(4096, zeroBytes);
                                byte[] buffer = new byte[chunk];
                                responseStream.Write(buffer, 0, chunk);
                                zeroBytes -= chunk;
                            }
                        }
                        responseStream.Close();
                    }
                }
                else
                {
                    response.StatusAndReason = HybridDSP.Net.HTTP.HTTPServerResponse.HTTPStatus.HTTP_INTERNAL_SERVER_ERROR;
                    response.Send().Close();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());

                if (responseStream != null)
                {
                    responseStream.Close();
                }
                else
                {
                    // no data to play was ever received and send to the requesting client -> send an error now
                    response.ContentLength = 0;
                    response.StatusAndReason = HybridDSP.Net.HTTP.HTTPServerResponse.HTTPStatus.HTTP_INTERNAL_SERVER_ERROR;
                    response.Send().Close();
                }
            }
        }

        public void HandleRequest(string url, HybridDSP.Net.HTTP.HTTPServerRequest request, HybridDSP.Net.HTTP.HTTPServerResponse response)
        {
            lock (sync)
            {
                if (Thread.CurrentThread.Name != null) Thread.CurrentThread.Name = "RTMPProxy";
                RTMP rtmp = null;
                try
                {
                    NameValueCollection paramsHash = System.Web.HttpUtility.ParseQueryString(new Uri(url).Query);

                    Logger.Log("RTMP Request Parameters:");
                    foreach (var param in paramsHash.AllKeys) Logger.Log(string.Format("{0}={1}", param, paramsHash[param]));

                    Link link = new Link();
                    if (paramsHash["rtmpurl"] != null) link = Link.FromRtmpUrl(new Uri(paramsHash["rtmpurl"]));
                    if (paramsHash["app"] != null) link.app = paramsHash["app"];
                    if (paramsHash["tcUrl"] != null) link.tcUrl = paramsHash["tcUrl"];
                    if (paramsHash["hostname"] != null) link.hostname = paramsHash["hostname"];
                    if (paramsHash["port"] != null) link.port = int.Parse(paramsHash["port"]);
                    if (paramsHash["playpath"] != null) link.playpath = paramsHash["playpath"];
                    if (paramsHash["subscribepath"] != null) link.subscribepath = paramsHash["subscribepath"];
                    if (paramsHash["pageurl"] != null) link.pageUrl = paramsHash["pageurl"];
                    if (paramsHash["swfurl"] != null) link.swfUrl = paramsHash["swfurl"];
                    if (paramsHash["swfsize"] != null) link.SWFSize = int.Parse(paramsHash["swfsize"]);
                    if (paramsHash["swfhash"] != null) link.SWFHash = Link.ArrayFromHexString(paramsHash["swfhash"]);
                    if (paramsHash["swfVfy"] != null) { link.swfUrl = paramsHash["swfVfy"]; link.swfVerify = true; }
                    if (paramsHash["live"] != null) bool.TryParse(paramsHash["live"], out link.bLiveStream);
                    if (paramsHash["auth"] != null) link.auth = paramsHash["auth"];
                    if (paramsHash["token"] != null) link.token = paramsHash["token"];
                    if (paramsHash["conn"] != null) link.extras = Link.ParseAMF(paramsHash["conn"]);
                    if (link.tcUrl != null && link.tcUrl.ToLower().StartsWith("rtmpe")) link.protocol = Protocol.RTMPE;

                    if (link.swfVerify) link.GetSwf();

                    rtmp = new RTMP() { Link = link };

                    ConnectAndGetStream(rtmp, request, response, ref invalidHeader);
                }
                finally
                {
                    if (rtmp != null) rtmp.Close();
                }

                Logger.Log("Request finished.");
            }
        }
    }
}

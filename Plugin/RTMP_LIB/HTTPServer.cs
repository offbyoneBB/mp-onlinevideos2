using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading;

namespace RTMP_LIB
{
    public class RequestHandlerFactory : HybridDSP.Net.HTTP.IHTTPRequestHandlerFactory
    {
        public HybridDSP.Net.HTTP.IHTTPRequestHandler CreateRequestHandler(HybridDSP.Net.HTTP.HTTPServerRequest request)
        {
            request.ExpectsContinue = true; // so the 100 continue header is sent
            request.KeepAlive = true; // keep connection alive
            return new RequestHandler();
        }
    }

    public class RequestHandler : HybridDSP.Net.HTTP.IHTTPRequestHandler
    {
        public void HandleRequest(HybridDSP.Net.HTTP.HTTPServerRequest request, HybridDSP.Net.HTTP.HTTPServerResponse response)
        {          
            Logger.Log("Request");
            RTMP rtmp = null;
            try
            {
                NameValueCollection paramsHash = System.Web.HttpUtility.ParseQueryString(new Uri(new Uri("http://127.0.0.1"), request.URI).Query);

                Link link = new Link();
                if (!string.IsNullOrEmpty(paramsHash["rtmpurl"])) link = Link.FromRtmpUrl(new Uri(paramsHash["rtmpurl"]));
                if (!string.IsNullOrEmpty(paramsHash["app"])) link.app = paramsHash["app"];
                if (!string.IsNullOrEmpty(paramsHash["tcUrl"])) link.tcUrl = paramsHash["tcUrl"];
                if (!string.IsNullOrEmpty(paramsHash["hostname"])) link.hostname = paramsHash["hostname"];
                if (!string.IsNullOrEmpty(paramsHash["port"])) link.port = int.Parse(paramsHash["port"]);
                if (!string.IsNullOrEmpty(paramsHash["playpath"])) link.playpath = paramsHash["playpath"];
                if (!string.IsNullOrEmpty(paramsHash["subscribepath"])) link.subscribepath = paramsHash["subscribepath"];
                if (!string.IsNullOrEmpty(paramsHash["pageurl"])) link.pageUrl = paramsHash["pageurl"];
                if (!string.IsNullOrEmpty(paramsHash["swfurl"])) link.swfUrl = paramsHash["swfurl"];
                if (!string.IsNullOrEmpty(paramsHash["swfsize"])) link.SWFSize = int.Parse(paramsHash["swfsize"]);
                if (!string.IsNullOrEmpty(paramsHash["swfhash"])) link.SWFHash = Link.ArrayFromHexString(paramsHash["swfhash"]);
                if (!string.IsNullOrEmpty(paramsHash["usefp9"])) link.useFP9Handshake = bool.Parse(paramsHash["usefp9"]);
                if (!string.IsNullOrEmpty(paramsHash["authobj"])) link.authObjName = paramsHash["authobj"];
                if (!string.IsNullOrEmpty(paramsHash["auth"])) link.auth = paramsHash["auth"];
                if (link.tcUrl != null && link.tcUrl.ToLower().StartsWith("rtmpe")) link.protocol = RTMP.RTMP_PROTOCOL_RTMPE;

                rtmp = new RTMP();
                bool connected = rtmp.Connect(link);
                if (connected)
                {
                    response.ContentType = "video/x-flv";
                    response.ChunkedTransferEncoding = true;

                    Stream responseStream = null;
                    FLVStream fs = new FLVStream();
                    fs.WriteFLV(rtmp, delegate()
                    {
                        // we must set a content length for the File Source filter, otherwise it thinks we have no content
                        // but don't set a lenght if it is our user agent, so a download will always be complete
                        if (request.Get("User-Agent") != OnlineVideos.OnlineVideoSettings.UserAgent)
                            response.ContentLength = fs.EstimatedLength;
                        responseStream = response.Send();
                        return responseStream;
                    });

                    long zeroBytes = fs.EstimatedLength - fs.Length;
                    while (zeroBytes > 0)
                    {
                        int chunk = (int)Math.Min(4096, zeroBytes);
                        byte[] buffer = new byte[chunk];
                        responseStream.Write(buffer, 0, chunk);
                        zeroBytes -= chunk;
                    }

                    if (responseStream != null) responseStream.Close();
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
            }
            finally
            {
                if (rtmp != null) rtmp.Close();
            }

            Logger.Log("Request finished.");
        }
    }

    /// <summary>
    /// This class handles HTTP Request that will be used to get rtmp streams and return them via http.
    /// </summary>
    public class HTTPServer
    {
        HybridDSP.Net.HTTP.HTTPServer _server = null;        

        public HTTPServer(int port)
        {
            _server = new HybridDSP.Net.HTTP.HTTPServer(new RequestHandlerFactory(), port);
            _server.OnServerException += new HybridDSP.Net.HTTP.HTTPServer.ServerCaughtException(delegate(Exception ex) { Logger.Log(ex.Message); });
            _server.Start();            
        }        

        public void StopListening()
        {
            _server.Stop();
        }        
    }    
}

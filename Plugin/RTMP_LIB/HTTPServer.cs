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

            response.ContentType = "video/x-flv";
            response.ChunkedTransferEncoding = true;            

            Stream ostr = response.Send();
                
            try
            {
                /*
                NameValueCollection paramsHash = System.Web.HttpUtility.ParseQueryString(ctx.Request.Url.Query);

                Link link = new Link();
                if (!string.IsNullOrEmpty(paramsHash["rtmpurl"]))
                {
                    link = Link.FromRtmpUrl(new Uri(paramsHash["rtmpurl"]));
                }
                if (!string.IsNullOrEmpty(paramsHash["app"]))
                {
                    link.app = paramsHash["app"];
                }
                if (!string.IsNullOrEmpty(paramsHash["tcUrl"]))
                {
                    link.tcUrl = paramsHash["tcUrl"];
                }
                if (!string.IsNullOrEmpty(paramsHash["hostname"]))
                {
                    link.hostname = paramsHash["hostname"];
                }
                if (!string.IsNullOrEmpty(paramsHash["port"]))
                {
                    link.port = int.Parse(paramsHash["port"]);
                }
                if (!string.IsNullOrEmpty(paramsHash["playpath"]))
                {
                    link.playpath = paramsHash["playpath"];
                }

                if (string.IsNullOrEmpty(paramsHash["playpath"]))
                {                    
                    link.protocol = RTMP.RTMP_PROTOCOL_RTMPE;
                    link.hostname = "vod.daserste.de";
                    link.port = 1935;
                    link.app = "ardfs/";
                    link.playpath = "mp4:videoportal/Film/c_80000/87827/format86451.f4v?sen=ARD-Mittagsmagazin&amp;for=Web-M&amp;clip=Alle+Beitr%E4ge+-+die+Sendung+vom+26.+Juni+2009";
                    link.tcUrl = "rtmpe://vod.daserste.de:1935/ardfs/";
                }

                if (link.tcUrl != null && link.tcUrl.ToLower().StartsWith("rtmpe")) link.protocol = RTMP.RTMP_PROTOCOL_RTMPE;

                RTMP rtmp = new RTMP();
                bool connected = rtmp.Connect(link);
                if (connected)
                {                
                    //FileStream myFs = new FileStream(Path.GetTempFileName()+".flv", FileMode.Create, FileAccess.Write, FileShare.Read);
                    new FLVStream().WriteFLV(rtmp, ostr);                              
                }
                */
                
                // stream local file for testing
                using (FileStream fs = new FileStream(@"C:\Users\offbyone\Documents\Code\_Tests\rtmp\s.flv", FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    System.Threading.Thread.Sleep(20); // wait half a second to simulate buffering
                    int amount = 1024 * 10; // prebuffer -> the first chunk should be large enough 

                    while (fs.Position < fs.Length)
                    {
                        if (fs.Position + amount > fs.Length) amount = (int)(fs.Length - fs.Position);
                        byte[] data = new byte[amount];
                        fs.Read(data, 0, amount);
                        ostr.Write(data, 0, amount);

                        amount = 1024;
                        System.Threading.Thread.Sleep(4);
                    }
                    fs.Close();
                }
                
                ostr.Close();
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
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

        public HTTPServer() : this(20004) {}

        public HTTPServer(int port)
        {            
            _server = new HybridDSP.Net.HTTP.HTTPServer(new RequestHandlerFactory(), 20004);
            _server.OnServerException += new HybridDSP.Net.HTTP.HTTPServer.ServerCaughtException(delegate(Exception ex) { Logger.Log(ex.Message); });
            _server.Start();            
        }        

        public void StopListening()
        {
            _server.Stop();
        }        
    }
}

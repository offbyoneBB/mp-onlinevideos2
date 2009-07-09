using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Threading;
using System.Text.RegularExpressions;

namespace RTMP_LIB
{
    /// <summary>
    /// This class handles HTTP Request that will be used to get rtmp streams and return them via http.
    /// Can only handle one request at a time!
    /// </summary>
    public class HTTPServer
    {
        HttpListener listener;
        Thread processingThread;
        bool listen = true;

        public HTTPServer() : this("http://localhost:20004/") {}

        public HTTPServer(string url)
        {            
            listener = new HttpListener();
            listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            listener.Prefixes.Add(url);
            listener.Start();
            processingThread = new Thread(delegate() 
                { 
                    while (listen) 
                    { 
                        Listen(); 
                    } 
                    listener.Stop(); 
                });
            processingThread.Name = "RTMP_HTTP_LISTENER";
            processingThread.IsBackground = true;
            processingThread.Start();
        }

        public void StopListening()
        {
            listen = false;
        }

        void Listen()
        {
            HttpListenerContext ctx = listener.GetContext(); // blocking until request is coming in

            try
            {
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

                RTMP rtmp = new RTMP();
                bool connected = rtmp.Connect(link);
                if (connected)
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "application/octet-stream";
                    ctx.Response.AppendHeader("Content-Disposition", "attachment;Filename=stream.flv");
                    ctx.Response.ContentLength64 = int.MaxValue; // since we don't know the length for sure

                    FLVStream.WriteFLV(rtmp, ctx.Response.OutputStream);

                    ctx.Response.OutputStream.Flush();
                    ctx.Response.OutputStream.Close();
                    ctx.Response.Close();
                }
                else
                {
                    ctx.Response.StatusCode = 500;
                }
            }
            catch (Exception ex)
            {
                ctx.Response.StatusCode = 500;
                Logger.Log(ex.Message);
            }            
        }
    }
}

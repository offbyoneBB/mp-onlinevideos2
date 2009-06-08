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
        static Regex requestParamRegEx = new Regex("[?&]([^=]+)=([^=&]+)");

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
                StringDictionary paramsHash = new StringDictionary();
                Match match = requestParamRegEx.Match(ctx.Request.Url.Query);
                while (match.Success)
                {
                    paramsHash.Add(System.Web.HttpUtility.HtmlDecode(match.Groups[1].Value), System.Web.HttpUtility.HtmlDecode(match.Groups[2].Value));
                    match = match.NextMatch();
                }                

                RTMP rtmp = new RTMP();
                bool connected = rtmp.Connect(paramsHash["rtmpurl"]);
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
                Logger.Log(ex.Message);
            }
        }
    }
}

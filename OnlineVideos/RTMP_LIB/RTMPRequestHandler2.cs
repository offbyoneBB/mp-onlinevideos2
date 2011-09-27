using System;
using System.Text;
using System.IO;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Web;
using HybridDSP.Net.HTTP;
using OnlineVideos;

namespace LibRTMP
{
    public class RTMPRequestHandler2 : IRequestHandler
    {
        #region Singleton
        private static RTMPRequestHandler2 _Instance = null;
        public static RTMPRequestHandler2 Instance
        {
            get
            {
                if (_Instance == null) _Instance = new RTMPRequestHandler2();
                return _Instance;
            }
        }
        private RTMPRequestHandler2()
        {
            ReverseProxy.AddHandler(this);
        }
        #endregion

        #region IRequestHandler
        bool invalidHeader = false;

        public bool DetectInvalidPackageHeader()
        {
            return invalidHeader;
        }

        public void HandleRequest(string url, HTTPServerRequest request, HTTPServerResponse response)
        {
            string url2 = fillVars(url);

            IntPtr rtmp = RTMP.RTMP_Alloc();

            IntPtr ptr = Marshal.StringToHGlobalAnsi(url2);
            int ii = RTMP.InitSockets();
            try
            {
                RTMP.LogCallback lc = new RTMP.LogCallback(LC);
                RTMP.SetLogCallback(lc);

                RTMP.RTMP_Init(rtmp);

                int res = RTMP.RTMP_SetupURL(rtmp, ptr);
                RTMP.RTMP_LNK lnk = RTMP.GetLnk(rtmp);
                Log.Debug("SetupUrl returned {0}", res);
                Log.Debug("Protocol : {0}", lnk.protocol);
                Log.Debug("Hostname : {0}", lnk.hostname);
                Log.Debug("Port     : {0}", lnk.port);
                Log.Debug("Playpath : {0}", lnk.playpath);
                if (!String.IsNullOrEmpty(lnk.tcUrl))
                    Log.Debug("tcUrl    : {0}", lnk.tcUrl);
                if (!String.IsNullOrEmpty(lnk.swfUrl))
                    Log.Debug("swfUrl   : {0}", lnk.swfUrl);
                if (!String.IsNullOrEmpty(lnk.pageUrl))
                    Log.Debug("pageUrl  : {0}", lnk.pageUrl);
                if (!String.IsNullOrEmpty(lnk.app))
                    Log.Debug("app      : {0}", lnk.app);
                if (!String.IsNullOrEmpty(lnk.auth))
                    Log.Debug("auth     : {0}", lnk.auth);
                if (!String.IsNullOrEmpty(lnk.subscribepath))
                    Log.Debug("subscribepath : {0}", lnk.subscribepath);
                if (!String.IsNullOrEmpty(lnk.usherToken))
                    Log.Debug("NetStream.Authenticate.UsherToken : {0}", lnk.usherToken);
                if (!String.IsNullOrEmpty(lnk.flashVer))
                    Log.Debug("flashVer : {0}", lnk.flashVer);
                /*if (dStart > 0)
                    Log.Debug("StartTime     : %d msec", dStart);
                if (dStop > 0)
                    RTMP_Log(RTMP_LOGDEBUG, "StopTime      : %d msec", dStop);
                 */

                Log.Debug("live     : {0}", (lnk.lFlags & RTMP.RTMPFlags.LIVE) != 0 ? "yes" : "no");
                Log.Debug("timeout  : {0} sec", lnk.timeout);

                if ((lnk.lFlags & RTMP.RTMPFlags.SWFV) != 0)
                {
                    Log.Debug("SWFSHA256:");
                    string s = BitConverter.ToString(lnk.SWFHash).Replace('-', ' ');
                    Log.Debug(s.Substring(0, 47));
                    Log.Debug(s.Substring(48, 47));
                    Log.Debug("SWFSize  : {0}", lnk.SWFSize);
                }

                request.KeepAlive = true; // keep connection alive
                response.ContentType = "video/x-flv";
                response.KeepAlive = true;

                res = RTMP.RTMP_Connect(rtmp, IntPtr.Zero);
                Log.Debug("Connect returned {0}", res);
                res = RTMP.RTMP_ConnectStream(rtmp, 0);
                Log.Debug("ConnectStream returned {0}", res);
                int buflen = 2048;
                byte[] buffer = new byte[buflen];
                Stream responseStream = response.Send();
                try
                {
                    int totalRead = 0;
                    bool ready = false;
                    do
                    {
                        int nread = RTMP.RTMP_Read(rtmp, buffer, buflen);
                        totalRead += nread;
                        Log.Debug("Total bytes read:{0}", totalRead);
                        if (nread <= 0)
                            ready = true;
                        else
                            responseStream.Write(buffer, 0, nread);

                    } while (!ready && RTMP.RTMP_IsConnected(rtmp) && !RTMP.RTMP_IsTimedout(rtmp));
                }
                finally
                {
                    responseStream.Flush();
                    responseStream.Close();
                }
            }
            finally
            {
                RTMP.CleanupSockets();
                Marshal.FreeHGlobal(ptr);
                RTMP.SetLogCallback(null);
            }
        }
        #endregion

        public static void LC(RTMP.LogLevel level, string message)
        {
            if (level <= RTMP.LogLevel.ERROR)
                Log.Error(message);
            else
                if (level <= RTMP.LogLevel.WARNING)
                    Log.Warn(message);
                else
                    if (level <= RTMP.LogLevel.INFO)
                        Log.Info(message);
                    else
                        if (level <= RTMP.LogLevel.DEBUG)
                            Log.Debug(message);
        }

        private string fillVars(string url)
        {
            StringBuilder sb = new StringBuilder();

            Uri uri = new Uri(url);
            NameValueCollection paramsHash = HttpUtility.ParseQueryString(uri.Query);

            UriBuilder ub = new UriBuilder(uri);
            ub.Query = String.Empty;

            sb.Append(paramsHash["rtmpurl"]);

            AddValue("app", paramsHash["app"], sb);
            AddValue("tcUrl", paramsHash["tcUrl"], sb);
            AddValue("hostname", paramsHash["hostname"], sb);
            AddValue("port", paramsHash["port"], sb);
            AddValue("playpath", paramsHash["playpath"], sb);
            AddValue("subscribepath", paramsHash["subscribepath"], sb);
            AddValue("pageurl", paramsHash["pageurl"], sb);
            AddValue("swfurl", paramsHash["swfurl"], sb);
            AddValue("swfsize", paramsHash["swfsize"], sb);
            AddValue("swfhash", paramsHash["swfhash"], sb);
            string swfVfy = paramsHash["swfVfy"];
            if (swfVfy != null)
            {
                AddValue("swfurl", swfVfy, sb);
                AddValue("swfVfy", "1", sb);
            }
            AddValue("live", paramsHash["live"], sb);
            AddValue("auth", paramsHash["auth"], sb);
            AddValue("token", paramsHash["token"], sb);
            AddValue("conn", paramsHash["conn"], sb);

            return sb.ToString();
        }

        private void AddValue(string name, string urlValue, StringBuilder sb)
        {
            if (urlValue != null) sb.Append(" " + name + "=" + urlValue);
        }

    }
}

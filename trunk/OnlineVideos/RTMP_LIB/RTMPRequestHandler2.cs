using System;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using HybridDSP.Net.HTTP;
using OnlineVideos;

namespace RTMP_LIB
{
	public class RTMPRequestHandler2 : CrossDomanSingletonBase<RTMPRequestHandler2>, IRequestHandler
    {
        private RTMPRequestHandler2()
        {
            ReverseProxy.Instance.AddHandler(this);
        }

        #region IRequestHandler
        bool invalidHeader = false;

        public bool DetectInvalidPackageHeader()
        {
            return invalidHeader;
        }

        public void HandleRequest(string url, HTTPServerRequest request, HTTPServerResponse response)
        {
            string url2 = fillVars(url);

			IntPtr rtmp = LibRTMP.RTMP_Alloc();

            IntPtr ptr = Marshal.StringToHGlobalAnsi(url2);
			int ii = LibRTMP.InitSockets();
            try
            {
				LibRTMP.LogCallback lc = new LibRTMP.LogCallback(LC);
				LibRTMP.SetLogCallback(lc);

				LibRTMP.RTMP_Init(rtmp);

				int res = LibRTMP.RTMP_SetupURL(rtmp, ptr);
				LibRTMP.RTMP_LNK lnk = LibRTMP.GetLnk(rtmp);
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

				Log.Debug("live     : {0}", (lnk.lFlags & LibRTMP.RTMPFlags.LIVE) != 0 ? "yes" : "no");
                Log.Debug("timeout  : {0} sec", lnk.timeout);

				if ((lnk.lFlags & LibRTMP.RTMPFlags.SWFV) != 0)
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

				res = LibRTMP.RTMP_Connect(rtmp, IntPtr.Zero);
                Log.Debug("Connect returned {0}", res);
				res = LibRTMP.RTMP_ConnectStream(rtmp, 0);
                Log.Debug("ConnectStream returned {0}", res);
                int buflen = 2048;
                byte[] buffer = new byte[buflen];
				Stream responseStream = null;
				long EstimatedLength = 0;
				int totalRead = 0;
                try
                {
                    bool ready = false;
                    do
                    {
						int nread = LibRTMP.RTMP_Read(rtmp, buffer, buflen);
						if (responseStream == null)
						{
							if (nread > 13) // first 13 bytes are the flv header, after that first packet should be the metadata packet
							{
								EstimatedLength = TryFindMetaDataEstimatedLength(buffer, 13, nread);
								// we must set a content length for the File Source filter, otherwise it thinks we have no content
								// but don't set a length if it is our user agent, so a download will always be complete
								if (request.Get("User-Agent") != OnlineVideos.OnlineVideoSettings.Instance.UserAgent)
									response.ContentLength = EstimatedLength;
								responseStream = response.Send();
							}
							else
							{
								response.ContentLength = 0;
								responseStream = response.Send();
							}
						}
                        totalRead += nread;
                        if (nread <= 0)
                            ready = true;
                        else
                            responseStream.Write(buffer, 0, nread);

					} while (!ready && LibRTMP.RTMP_IsConnected(rtmp) && !LibRTMP.RTMP_IsTimedout(rtmp));
                }
                finally
                {
					Log.Debug("Total bytes read:{0}", totalRead);
					if (responseStream != null)
					{
						if (request.Get("User-Agent") != OnlineVideos.OnlineVideoSettings.Instance.UserAgent)
						{
							// keep appending "0" - bytes until we filled the estimated length when sending data to the File Source filter
							long zeroBytes = EstimatedLength - totalRead;
							while (zeroBytes > 0)
							{
								int chunk = (int)Math.Min(4096, zeroBytes);
								buffer = new byte[chunk];
								responseStream.Write(buffer, 0, chunk);
								zeroBytes -= chunk;
							}
						}
						responseStream.Flush();
						responseStream.Close();
					}
                }
            }
            finally
            {
				LibRTMP.CleanupSockets();
                Marshal.FreeHGlobal(ptr);
				LibRTMP.SetLogCallback(null);
            }
        }
        #endregion

		static long TryFindMetaDataEstimatedLength(byte[] packetData, int offset, int length)
		{
			int packetBodyStart = offset;

			byte headerType = (byte)((packetData[offset] & 0xc0) >> 6);
			byte channel = (byte)(packetData[offset] & 0x3f);
			if (channel == 0) packetBodyStart++;
			else if (channel == 1) packetBodyStart+=2;

			int bodySize = RTMP_LIB.RTMP.ReadInt24(packetData, packetBodyStart+1);
			packetBodyStart += (int)RTMP_LIB.RTMP.packetSize[headerType] - 1;

			RTMP_LIB.Metadata metadata = new RTMP_LIB.Metadata();
			metadata.DecodeFromPacketBody(packetData, packetBodyStart, bodySize, null);

			return metadata.EstimateBytes(RTMP_LIB.RTMP.RTMP_DEFAULT_CHUNKSIZE);
		}

		public static void LC(LibRTMP.LogLevel level, string message)
        {
			if (level <= LibRTMP.LogLevel.ERROR)
                Log.Error(message);
            else
				if (level <= LibRTMP.LogLevel.WARNING)
                    Log.Warn(message);
                else
					if (level <= LibRTMP.LogLevel.INFO)
                        Log.Info(message);
                    else
						if (level <= LibRTMP.LogLevel.DEBUG)
                            Log.Debug(message);
        }

        private string fillVars(string url)
        {
            StringBuilder sb = new StringBuilder();

            Uri uri = new Uri(url);
            NameValueCollection paramsHash = HttpUtility.ParseQueryString(uri.Query);

			if (paramsHash["rtmpurl"] != null)
				sb.Append(paramsHash["rtmpurl"]);
			else
			{
				sb.Append(
					(paramsHash["tcUrl"] != null ? new Uri(paramsHash["tcUrl"]).Scheme : "rtmp") + 
					"://" + 
					paramsHash["hostname"] + 
					(paramsHash["port"] != null ? ":" + paramsHash["port"] : ""));
			}
			if (paramsHash["app"] != null) AddValue("app", paramsHash["app"], sb);
			if (paramsHash["tcUrl"] != null) AddValue("tcUrl", paramsHash["tcUrl"], sb);
			if (paramsHash["pageurl"] != null) AddValue("pageUrl", paramsHash["pageurl"], sb);
			if (paramsHash["swfurl"] != null) AddValue("swfUrl", paramsHash["swfurl"], sb);
			if (paramsHash["conn"] != null) AddValue("conn", paramsHash["conn"], sb);
			if (paramsHash["playpath"] != null) AddValue("playpath", paramsHash["playpath"], sb);
			if (paramsHash["live"] != null) AddValue("live", paramsHash["live"], sb);
			if (paramsHash["subscribepath"] != null) AddValue("subscribe", paramsHash["subscribepath"], sb);
			if (paramsHash["token"] != null) AddValue("token", paramsHash["token"], sb);
			if (paramsHash["swfVfy"] != null) { AddValue("swfUrl", paramsHash["swfVfy"], sb); AddValue("swfVfy", "1", sb); }
			//if (paramsHash["swfsize"] != null) AddValue("swfsize", paramsHash["swfsize"], sb);
			//if (paramsHash["swfhash"] != null) AddValue("swfhash", paramsHash["swfhash"], sb);
			//if (paramsHash["auth"] != null) AddValue("auth", paramsHash["auth"], sb);

            return sb.ToString();
        }

        private void AddValue(string name, string urlValue, StringBuilder sb)
        {
            if (urlValue != null) sb.Append(" " + name + "=" + urlValue);
        }

    }
}

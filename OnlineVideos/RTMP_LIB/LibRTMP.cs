using System;
using System.Text;
using System.Runtime.InteropServices;

namespace RTMP_LIB
{
	public class LibRTMP
    {
        public enum RTMPProtocol
        {
            RTMP, RTMPT, RTMPE, RTMPTE, RTMPS, RTMPTS,
            undef1, undef2, RTMFP
        }

        public enum LogLevel
        {
            CRIT = 0, ERROR, WARNING, INFO,
            DEBUG, DEBUG2, ALL
        }

        [Flags]
        public enum RTMPFlags
        {
            AUTH = 0x0001,	/* using auth param */
            LIVE = 0x0002,	/* stream is live */
            SWFV = 0x0004,	/* do SWF verification */
            PLST = 0x0008,	/* send playlist before play */
            BUFX = 0x0010,	/* toggle stream on BufferEmpty msg */
            FTCU = 0x0020	/* free tcUrl on close */
        }

        public struct RTMP_LNK
        {
            public string hostname;
            public string sockshost;

            public string playpath0;	/* parsed from URL */
            public string playpath;	/* passed in explicitly */
            public string tcUrl;
            public string swfUrl;
            public string pageUrl;
            public string app;
            public string auth;
            public string flashVer;
            public string subscribepath;
            public string usherToken;
            public string token;
            //AMFObject extras;
            public int edepth;

            public int seekTime;
            public int stopTime;

            public RTMPFlags lFlags;

            public int swfAge;

            public RTMPProtocol protocol;
            public int timeout;		/* connection timeout in seconds */

            public ushort socksport;
            public ushort port;

            IntPtr dh;			/* for encryption */
            IntPtr rc4keyIn;
            IntPtr rc4keyOut;

            public uint SWFSize;
            public byte[] SWFHash;
            string SWFVerificationResponse;
        }

        public delegate void LogCallback(LogLevel level, string message);

        [DllImport("librtmp.dll")]
        public static extern int RTMP_LibVersion();

        [DllImport("librtmp.dll")]
        public static extern IntPtr RTMP_Alloc();

        [DllImport("librtmp.dll")]
        public static extern void RTMP_Free(IntPtr rtmp);

        [DllImport("librtmp.dll")]
        public static extern void RTMP_Init(IntPtr rtmp);

        [DllImport("librtmp.dll")]
        public static extern void RTMP_Close(IntPtr rtmp);

        [DllImport("librtmp.dll")]
        public static extern void RTMP_EnableWrite(IntPtr rtmp);

        [DllImport("librtmp.dll")]
        public static extern int RTMP_SetupURL(IntPtr rtmp, IntPtr url);
        //Intptr, because memory must remain valid so use StringToHGlobalAnsi

        [DllImport("logstub.dll")]
        public static extern void SetLogCallback(LogCallback cb);

        [DllImport("librtmp.dll")]
        public static extern void RTMP_LogSetLevel(int lvl);

        [DllImport("librtmp.dll")]
        public static extern int RTMP_Connect(IntPtr rtmp, IntPtr cp);

        [DllImport("librtmp.dll")]
        public static extern int RTMP_ConnectStream(IntPtr rtmp, int seekTime);

        [DllImport("librtmp.dll")]
        public static extern int RTMP_Read(IntPtr rtmp, byte[] buffer, int size);

        [DllImport("librtmp.dll")]
        public static extern int RTMP_Pause(IntPtr rtmp, int DoPause);

        [DllImport("librtmp.dll")]
        public static extern bool RTMP_IsConnected(IntPtr rtmp);

        [DllImport("librtmp.dll")]
        public static extern bool RTMP_IsTimedout(IntPtr rtmp);

        [DllImport("librtmp.dll")]
        public static extern bool RTMP_HashSWF(string url, out int size, byte[] hash, int age);

        [DllImport("logstub.dll")]
        public static extern int InitSockets();

        [DllImport("logstub.dll")]
        public static extern void CleanupSockets();

        public static RTMP_LNK GetLnk(IntPtr rtmp)
        {
            IntPtr res = privGetLink(rtmp);
            privRTMP_LNK t = (privRTMP_LNK)Marshal.PtrToStructure(res, typeof(privRTMP_LNK));
            RTMP_LNK r = new RTMP_LNK();
            r.hostname = t.hostname.GetStringValue();
            r.sockshost = t.sockshost.GetStringValue();
            r.playpath0 = t.playpath0.GetStringValue();
            r.playpath = t.playpath.GetStringValue();
            r.tcUrl = t.tcUrl.GetStringValue();
            r.swfUrl = t.swfUrl.GetStringValue();
            r.pageUrl = t.pageUrl.GetStringValue();
            r.app = t.app.GetStringValue();
            r.auth = t.auth.GetStringValue();
            r.flashVer = t.flashVer.GetStringValue();
            r.subscribepath = t.subscribepath.GetStringValue();
            r.usherToken = t.usherToken.GetStringValue();
            r.token = t.token.GetStringValue();
            r.edepth = t.edepth;
            r.seekTime = t.seekTime;
            r.stopTime = t.stopTime;
            r.lFlags = (RTMPFlags)t.lFlags;
            r.swfAge = t.swfAge;
            r.protocol = (RTMPProtocol)t.protocol;
            r.timeout = t.timeout;
            r.socksport = t.socksport;
            r.port = t.port;
            r.SWFSize = t.SWFSize;
            r.SWFHash = t.SwfHash();
            return r;
        }

        [DllImport("logstub.dll", EntryPoint = "GetLink")]
        private static extern IntPtr privGetLink(IntPtr rtmp);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct AVal
        {
            IntPtr av_val;
            int av_len;

            public string GetStringValue()
            {
                if (av_len == 0)
                    return String.Empty;
                byte[] bytes = new byte[av_len];
                Marshal.Copy(av_val, bytes, 0, av_len);
                return Encoding.ASCII.GetString(bytes);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct privRTMP_LNK
        {
            public AVal hostname;
            public AVal sockshost;

            public AVal playpath0;	/* parsed from URL */
            public AVal playpath;	/* passed in explicitly */
            public AVal tcUrl;
            public AVal swfUrl;
            public AVal pageUrl;
            public AVal app;
            public AVal auth;
            public AVal flashVer;
            public AVal subscribepath;
            public AVal usherToken;
            public AVal token;
            //internal AMFObject extras;
            public int edepth;

            public int seekTime;
            public int stopTime;

            public int lFlags;

            public int swfAge;

            public int protocol;
            public int timeout;		/* connection timeout in seconds */

            public ushort socksport;
            public ushort port;

            public IntPtr dh;			/* for encryption */
            public IntPtr rc4keyIn;
            public IntPtr rc4keyOut;

            public uint SWFSize;
            public ulong SWFHash0;
            public ulong SWFHash1;
            public ulong SWFHash2;
            public ulong SWFHash3;
            public byte[] SwfHash()
            {
                byte[] res = new byte[32];
                Buffer.BlockCopy(BitConverter.GetBytes(SWFHash0), 0, res, 0, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(SWFHash1), 0, res, 8, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(SWFHash2), 0, res, 16, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(SWFHash3), 0, res, 24, 8);
                return res;
            }
            //public fixed char SWFVerificationResponse[32];
        }

    }
}

using System;

namespace RTMP_LIB
{
    public class Link
    {
        public string fullUrl;

        public string hostname;
        public int port;
        public Protocol protocol;
        
        public string playpath;
        public string subscribepath;

        public string authObjName;

        public string tcUrl;
        public string swfUrl;
        public string pageUrl;
        public string app;
        public string auth;
        public string token;

        /// <summary>
        /// How to build the swf Hash:
        /// if swf is compressed, decompress with http://flasm.sourceforge.net/
        /// $ openssl sha -sha256 -hmac "Genuine Adobe Flash Player001" file.swf
        /// </summary>
        public byte[] SWFHash;
        public int SWFSize;
        public string flashVer;

        public double seekTime;
        public bool bLiveStream;

        public int timeout; // number of seconds before connection times out

        public Org.BouncyCastle.Crypto.IStreamCipher rc4In;
        public Org.BouncyCastle.Crypto.IStreamCipher rc4Out;
        public Org.BouncyCastle.Crypto.Agreement.DHBasicAgreement keyAgreement;

        public byte[] SWFVerificationResponse = new byte[42];

        public static Link FromRtmpUrl(Uri url)
        {
            Link link = new Link();

            link.hostname = url.Host;
            link.port = url.Port > 0 ? url.Port : 1935;

            link.app = url.AbsolutePath.TrimStart(new char[] { '/' });
            int slistPos = link.app.IndexOf("slist=");
            if (slistPos == -1)
            {
                // no slist parameter. send the path as the app
                // if URL path contains a slash, use the part up to that as the app
                // as we'll send the part after the slash as the thing to play
                int pos_colon = link.app.IndexOf(":");
                if (pos_colon != -1) { link.app = link.app.Substring(0, pos_colon); link.app = link.app.Substring(0, link.app.LastIndexOf('/')); }
                else
                {
                    int pos_slash = link.app.IndexOf("/");
                    if (pos_slash != -1) link.app = link.app.Substring(0, pos_slash);
                }
            }

            link.tcUrl = string.Format("{0}://{1}:{2}/{3}", url.Scheme, link.hostname, link.port, link.app);

            // or use slist parameter, if there is one
            int nPos = url.AbsolutePath.IndexOf("slist=");
            if (nPos > 0)
            {
                link.playpath = url.AbsolutePath.Substring(nPos, 6);
            }
            else
            {
                // use part after app
                link.playpath = url.AbsolutePath.TrimStart(new char[] { '/' }).Substring(link.app.Length).TrimStart(new char[] { '/' });
                if (link.playpath.EndsWith(".flv")) link.playpath = link.playpath.Substring(0, link.playpath.Length - 4);
                if (link.playpath.EndsWith(".mp4") & !link.playpath.StartsWith("mp4:")) link.playpath = "mp4:" + link.playpath;
            }

            return link;
        }

        public static byte[] ArrayFromHexString(string data)
        {
            byte[] result = new byte[data.Length / 2];

            for (int i = 0; i < data.Length-1; i += 2)
            {
                result[i/2] = byte.Parse(data[i].ToString()+data[i+1].ToString(), System.Globalization.NumberStyles.AllowHexSpecifier);
            }

            return result;
        }
    }
}

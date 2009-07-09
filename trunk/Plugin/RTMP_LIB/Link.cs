using System;

namespace RTMP_LIB
{
    public class Link
    {
        public string fullUrl;

        public string hostname;
        public int port;
        public int protocol;
        public string playpath;

        public string tcUrl;
        public string swfUrl;
        public string pageUrl;
        public string app;
        public string auth;
        public byte[] SWFHash;
        public int SWFSize;
        public string flashVer;

        public double seekTime;
        public bool bLiveStream;

        public int timeout; // number of seconds before connection times out
        
        public Org.Mentalis.Security.Cryptography.DiffieHellman dh; // for encryption
        public byte[] rc4keyIn;
        public byte[] rc4keyOut;
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
                int pos_slash = link.app.LastIndexOf("/");
                if (pos_slash != -1) link.app = link.app.Substring(0, pos_slash + 1);
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
                // or use last piece of URL, if there's more than one level
                int pos_slash = url.AbsolutePath.LastIndexOf("/");
                if (pos_slash != -1)
                    link.playpath = url.AbsolutePath.Substring(pos_slash + 1);
                if (link.playpath.EndsWith(".flv")) link.playpath = link.playpath.Substring(0, link.playpath.Length - 4);
            }

            return link;
        }
    }
}

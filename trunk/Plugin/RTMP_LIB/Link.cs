using System;
using System.Collections.Generic;
using System.Net;
using System.IO;

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

        public string tcUrl;
        public string swfUrl;
        public string pageUrl;
        public string app;
        public string auth;
        public string token;
        public AMFObject extras;

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

        public bool swfVerify = false;

        public int timeout; // number of seconds before connection times out

        public Org.BouncyCastle.Crypto.IStreamCipher rc4In;
        public Org.BouncyCastle.Crypto.IStreamCipher rc4Out;
        public Org.BouncyCastle.Crypto.Agreement.DHBasicAgreement keyAgreement;

        public byte[] SWFVerificationResponse = new byte[42];

        public static Link FromRtmpUrl(Uri url)
        {
            Link link = new Link();

            try
            {
                link.protocol = (Protocol)Enum.Parse(typeof(Protocol), url.Scheme.ToUpper());
            }
            catch
            {
                Logger.Log(string.Format("Error parsing protocol from url ({0}), setting default: RTMP", url.Scheme));
                link.protocol = Protocol.RTMP;
            }
            

            link.hostname = url.Host;
            link.port = url.Port > 0 ? url.Port : 1935;

            /* parse application
	         *
	         * rtmp://host[:port]/app[/appinstance][/...]
	         * application = app[/appinstance]
	         */
            string parsePlayPathFrom = "";
            if (url.PathAndQuery.Contains("slist="))
            {
                /* whatever it is, the '?' and slist= means we need to use everything as app and parse playpath from slist= */
                link.app = url.PathAndQuery.Substring(1);
                parsePlayPathFrom = url.PathAndQuery.Substring(url.PathAndQuery.IndexOf("slist="));
            }
            else if (url.PathAndQuery.StartsWith("/ondemand/"))
            {
                /* app = ondemand/foobar, only pass app=ondemand */
                link.app = "ondemand";
                parsePlayPathFrom = url.PathAndQuery.Substring(9);
            }
            else
            {
                /* app!=ondemand, so app is app[/appinstance] */
                int slash2Index = url.PathAndQuery.IndexOf('/', 1);
                int slash3Index = slash2Index >= 0 ? url.PathAndQuery.IndexOf('/', slash2Index+1) : -1;

                if (url.PathAndQuery.Contains("mp4:")) slash3Index = url.PathAndQuery.IndexOf("mp4:");
                
                if(slash3Index >= 0) 
                {
                    link.app = url.PathAndQuery.Substring(1, slash3Index - 1).Trim('/');
                    parsePlayPathFrom = url.PathAndQuery.Substring(slash3Index);
                }
                else if (slash2Index >= 0)
                {
                    link.app = url.PathAndQuery.Substring(1, slash2Index - 1).Trim('/');
                    parsePlayPathFrom = url.PathAndQuery.Substring(slash2Index);
                }
                else
                {
                    link.app = url.PathAndQuery.Trim('/');
                }
            }

            if (parsePlayPathFrom.StartsWith("/")) parsePlayPathFrom = parsePlayPathFrom.Substring(1);

            /*
             * Extracts playpath from RTMP URL. playpath is the file part of the
             * URL, i.e. the part that comes after rtmp://host:port/app/
             *
             * Returns the stream name in a format understood by FMS. The name is
             * the playpath part of the URL with formatting depending on the stream
             * type:
             *
             * mp4 streams: prepend "mp4:", remove extension
             * mp3 streams: prepend "mp3:", remove extension
             * flv streams: remove extension (Only remove .flv from rtmp URL, not slist params)
             */

            // use slist parameter, if there is one
            int nPos = parsePlayPathFrom.IndexOf("slist=");
            if (nPos >= 0)
            {
                parsePlayPathFrom = parsePlayPathFrom.Substring(nPos + 6);
            }
            else
            {
                if (parsePlayPathFrom.EndsWith(".flv")) parsePlayPathFrom = parsePlayPathFrom.Substring(0, parsePlayPathFrom.Length - 4);
            }
            if (parsePlayPathFrom.EndsWith(".mp4"))
            {
                parsePlayPathFrom = parsePlayPathFrom.Substring(0, parsePlayPathFrom.Length - 4);
                if (!parsePlayPathFrom.StartsWith("mp4:")) parsePlayPathFrom = "mp4:" + parsePlayPathFrom;
            }

            link.playpath = parsePlayPathFrom;

            link.tcUrl = string.Format("{0}://{1}:{2}/{3}", url.Scheme, link.hostname, link.port, link.app);

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

        public static AMFObject ParseAMF(string amfString)
        {
            int depth = 0;

            AMFObject obj = new AMFObject();

            string[] args = amfString.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);

            foreach (string arg in args)
            {
                AMFObjectProperty prop = new AMFObjectProperty();
                string p;

                if (arg[1] == ':')
                {
                    p = arg.Substring(2);
                    switch (arg[0])
                    {
                        case 'B':
                            prop.m_type = AMFDataType.AMF_BOOLEAN;
                            prop.p_number = int.Parse(p);
                            break;
                        case 'S':
                            prop.m_type = AMFDataType.AMF_STRING;
                            prop.m_strVal = p;
                            break;
                        case 'N':
                            prop.m_type = AMFDataType.AMF_NUMBER;
                            prop.p_number = double.Parse(p);
                            break;
                        case 'Z':
                            prop.m_type = AMFDataType.AMF_NULL;
                            break;
                        case 'O':
                            int i = int.Parse(p);
                            if (i > 0)
                            {
                                prop.m_type = AMFDataType.AMF_OBJECT;
                            }
                            else
                            {
                                depth--;
                                return obj;
                            }
                            break;
                        default:
                            return null;
                    }
                }
                else if (arg[2] == ':' && arg[0] == 'N')
                {
                    int secondColonIndex = arg.IndexOf(':', 3);
                    if (secondColonIndex < 0 ||depth <= 0) return null;
                    prop.m_strName = arg.Substring(3);
                    p = arg.Substring(secondColonIndex + 1);
                    switch (arg[1])
                    {
                        case 'B':
                            prop.m_type = AMFDataType.AMF_BOOLEAN;
                            prop.p_number = int.Parse(p);
                            break;
                        case 'S':
                            prop.m_type = AMFDataType.AMF_STRING;
                            prop.m_strVal = p;
                            break;
                        case 'N':
                            prop.m_type = AMFDataType.AMF_NUMBER;
                            prop.p_number = double.Parse(p);
                            break;
                        case 'O':
                            prop.m_type = AMFDataType.AMF_OBJECT;
                            break;
                        default:
                            return null;
                    }
                }
                else
                    return null;

                if (depth > 0)
                {
                    AMFObject o2;
                    for (int i = 0; i < depth; i++)
                    {
                        o2 = obj.GetProperty(obj.GetPropertyCount() - 1).GetObject();
                        obj = o2;
                    }
                }
                obj.AddProperty(prop);

                if (prop.m_type == AMFDataType.AMF_OBJECT)
                    depth++;               
            }

            return obj;
        }

        struct SwFInfo
        {
            internal byte[] Hash;
            internal int Size;
            internal DateTime Time;
        }
        static Dictionary<string, SwFInfo> swfCache = new Dictionary<string, SwFInfo>();

        public void GetSwf()
        {
            try
            {
                // check if we can retrieve
                if (string.IsNullOrEmpty(swfUrl)) return;
                Uri swfUri = new Uri(swfUrl);

                if (swfCache.ContainsKey(swfUrl))
                {
                    SWFHash = swfCache[swfUrl].Hash;
                    SWFSize = swfCache[swfUrl].Size;
                }
                else
                {
                    // get the swf file from the web
                    HttpWebRequest request = WebRequest.Create(swfUri) as HttpWebRequest;
                    if (request == null) return;
                    request.Accept = "*/*";
                    request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
                    request.Timeout = 5000; // don't wait longer than 5 seconds
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    System.IO.Stream responseStream;
                    if (response.ContentEncoding.ToLower().Contains("gzip"))
                        responseStream = new System.IO.Compression.GZipStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                    else if (response.ContentEncoding.ToLower().Contains("deflate"))
                        responseStream = new System.IO.Compression.DeflateStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                    else
                        responseStream = response.GetResponseStream();

                    byte[] tempBuff = new byte[1024 * 1024 * 10];
                    int bytesRead = 0;
                    int totalBytesRead = 0;
                    while ((bytesRead = responseStream.Read(tempBuff, totalBytesRead, tempBuff.Length - totalBytesRead)) > 0)
                    {
                        totalBytesRead += bytesRead;
                    }
                    byte[] buff = new byte[totalBytesRead];
                    Array.Copy(tempBuff, buff, totalBytesRead);

                    MemoryStream ms = new MemoryStream(buff);
                    BinaryReader br = new BinaryReader(ms);
                    // compressed swf?
                    if (br.PeekChar() == 'C')
                    {
                        // read size
                        br.BaseStream.Position = 4; // skip signature
                        SWFSize = Convert.ToInt32(br.ReadUInt32());
                        // read swf head
                        byte[] uncompressed = new byte[SWFSize];
                        br.BaseStream.Position = 0;
                        br.Read(uncompressed, 0, 8); // header data is not compressed
                        uncompressed[0] = System.Text.Encoding.ASCII.GetBytes(new char[] { 'F' })[0];
                        // un-zip
                        byte[] compressed = br.ReadBytes(SWFSize);
                        Ionic.Zlib.ZlibStream dStream = new Ionic.Zlib.ZlibStream(new MemoryStream(compressed), Ionic.Zlib.CompressionMode.Decompress);
                        int read = dStream.Read(uncompressed, 8, SWFSize - 8);

                        byte[] finalUncompressed = new byte[8 + read];
                        Array.Copy(uncompressed, finalUncompressed, 8 + read);
                        buff = finalUncompressed;
                    }
                    System.Security.Cryptography.HMACSHA256 sha256Hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.ASCII.GetBytes("Genuine Adobe Flash Player 001"));
                    SWFHash = sha256Hmac.ComputeHash(buff);
                    Logger.Log(string.Format("Size of decompressed SWF: {0}, Hash:", SWFSize));
                    Logger.LogHex(SWFHash, 0, SWFHash.Length);
                    swfCache.Add(swfUrl, new SwFInfo() { Hash = SWFHash, Size = SWFSize, Time = DateTime.Now });
                }
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("Error while getting swf ({0}): {1}", swfUrl, ex.Message));
            }
        }
    }
}

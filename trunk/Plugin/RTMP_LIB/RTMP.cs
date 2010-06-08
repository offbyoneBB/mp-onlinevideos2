using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RTMP_LIB
{
    public enum Protocol : short 
    { 
        UNDEFINED = -1, 
        RTMP      =  0, 
        RTMPT     =  1, 
        RTMPS     =  2, 
        RTMPE     =  3, 
        RTMPTE    =  4, 
        RTMFP     =  5 
    };

    public enum PacketType : byte
    {
        Undefined         = 0x00,
        ChunkSize         = 0x01, 
        Abort             = 0x02, 
        BytesRead         = 0x03, 
        Control           = 0x04,
        ServerBW          = 0x05, 
        ClientBW          = 0x06, 
        Audio             = 0x08, 
        Video             = 0x09,
        Metadata_AMF3     = 0x0F, 
        SharedObject_AMF3 = 0x10, 
        Invoke_AMF3       = 0x11,
        Metadata          = 0x12, 
        SharedObject      = 0x13, 
        Invoke            = 0x14, 
        FlvTags           = 0x16
    };

    public enum HeaderType : byte
    {
        Large   = 0,
        Medium  = 1,
        Small   = 2,
        Minimum = 3
    };

    public class RTMP
    {        
        #region  Constants

        static readonly byte[] DH_MODULUS_BYTES = new byte[128] 
        { 
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xC9, 0x0F, 0xDA, 0xA2, 0x21, 0x68, 0xC2, 0x34, 0xC4, 0xC6, 0x62, 0x8B, 0x80, 0xDC, 0x1C, 0xD1, 0x29, 0x02, 0x4E, 0x08, 0x8A, 0x67, 0xCC, 0x74,
            0x02, 0x0B, 0xBE, 0xA6, 0x3B, 0x13, 0x9B, 0x22, 0x51, 0x4A, 0x08, 0x79, 0x8E, 0x34, 0x04, 0xDD, 0xEF, 0x95, 0x19, 0xB3, 0xCD, 0x3A, 0x43, 0x1B, 0x30, 0x2B, 0x0A, 0x6D, 0xF2, 0x5F, 0x14, 0x37, 
            0x4F, 0xE1, 0x35, 0x6D, 0x6D, 0x51, 0xC2, 0x45, 0xE4, 0x85, 0xB5, 0x76, 0x62, 0x5E, 0x7E, 0xC6, 0xF4, 0x4C, 0x42, 0xE9, 0xA6, 0x37, 0xED, 0x6B, 0x0B, 0xFF, 0x5C, 0xB6, 0xF4, 0x06, 0xB7, 0xED, 
            0xEE, 0x38, 0x6B, 0xFB, 0x5A, 0x89, 0x9F, 0xA5, 0xAE, 0x9F, 0x24, 0x11, 0x7C, 0x4B, 0x1F, 0xE6, 0x49, 0x28, 0x66, 0x51, 0xEC, 0xE6, 0x53, 0x81, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
        };
        
        static readonly byte[] GenuineFPKey = new byte[62] 
        {
            0x47,0x65,0x6E,0x75,0x69,0x6E,0x65,0x20,0x41,0x64,0x6F,0x62,0x65,0x20,0x46,0x6C,
            0x61,0x73,0x68,0x20,0x50,0x6C,0x61,0x79,0x65,0x72,0x20,0x30,0x30,0x31, // Genuine Adobe Flash Player 001
            
            0xF0,0xEE,
            0xC2,0x4A,0x80,0x68,0xBE,0xE8,0x2E,0x00,0xD0,0xD1,0x02,0x9E,0x7E,0x57,0x6E,0xEC,
            0x5D,0x2D,0x29,0x80,0x6F,0xAB,0x93,0xB8,0xE6,0x36,0xCF,0xEB,0x31,0xAE
        };

        static readonly byte[] GenuineFMSKey = new byte[68]  
        {
            0x47, 0x65, 0x6e, 0x75, 0x69, 0x6e, 0x65, 0x20, 0x41, 0x64, 0x6f, 0x62, 0x65, 0x20, 0x46, 0x6c,
            0x61, 0x73, 0x68, 0x20, 0x4d, 0x65, 0x64, 0x69, 0x61, 0x20, 0x53, 0x65, 0x72, 0x76, 0x65, 0x72,
            0x20, 0x30, 0x30, 0x31, // Genuine Adobe Flash Media Server 001 

            0xf0, 0xee, 0xc2, 0x4a, 0x80, 0x68, 0xbe, 0xe8, 0x2e, 0x00, 0xd0, 0xd1,
            0x02, 0x9e, 0x7e, 0x57, 0x6e, 0xec, 0x5d, 0x2d, 0x29, 0x80, 0x6f, 0xab, 0x93, 0xb8, 0xe6, 0x36,
            0xcf, 0xeb, 0x31, 0xae 
        };

        static readonly uint[][] rtmpe8_keys = new uint[16][] 
        {
	        new uint[4]{0xbff034b2, 0x11d9081f, 0xccdfb795, 0x748de732},
            new uint[4]	{0x086a5eb6, 0x1743090e, 0x6ef05ab8, 0xfe5a39e2},
	        new uint[4]{0x7b10956f, 0x76ce0521, 0x2388a73a, 0x440149a1},
	        new uint[4]{0xa943f317, 0xebf11bb2, 0xa691a5ee, 0x17f36339},
	        new uint[4]{0x7a30e00a, 0xb529e22c, 0xa087aea5, 0xc0cb79ac},
	        new uint[4]{0xbdce0c23, 0x2febdeff, 0x1cfaae16, 0x1123239d},
	        new uint[4]{0x55dd3f7b, 0x77e7e62e, 0x9bb8c499, 0xc9481ee4},
	        new uint[4]{0x407bb6b4, 0x71e89136, 0xa7aebf55, 0xca33b839},
	        new uint[4]{0xfcf6bdc3, 0xb63c3697, 0x7ce4f825, 0x04d959b2},
	        new uint[4]{0x28e091fd, 0x41954c4c, 0x7fb7db00, 0xe3a066f8},
	        new uint[4]{0x57845b76, 0x4f251b03, 0x46d45bcd, 0xa2c30d29},
	        new uint[4]{0x0acceef8, 0xda55b546, 0x03473452, 0x5863713b},
	        new uint[4]{0xb82075dc, 0xa75f1fee, 0xd84268e8, 0xa72a44cc},
	        new uint[4]{0x07cf6e9e, 0xa16d7b25, 0x9fa7ae6c, 0xd92f5629},
	        new uint[4]{0xfeb1eae4, 0x8c8c3ce1, 0x4e0064a7, 0x6a387c2a},
	        new uint[4]{0x893a9427, 0xcc3013a2, 0xf106385b, 0xa829f927}
        };

        static readonly uint[] packetSize = { 12, 8, 4, 1 };

        const int RTMP_DEFAULT_CHUNKSIZE = 128;
        const int SHA256_DIGEST_LENGTH = 32;
        const int RTMP_LARGE_HEADER_SIZE = 12;
        const int RTMP_SIG_SIZE = 1536;        

        #endregion        

        #region Public Properties

        /// <summary>
        /// Current ChunkSize for incoming packets (default: 128 byte)
        /// </summary>
        public int InChunkSize { get; protected set; } 
        
        /// <summary>
        /// indicates if currently streaming a media file
        /// </summary>
        public bool Playing { get; protected set; }

        /// <summary>
        /// Duration of stream in seconds returned by Metadata
        /// </summary>
        public double Duration { get; protected set; }
        
        /// <summary>
        /// Sum of bytes of all tracks in the stream (if Metadata was received and held that information)
        /// </summary>
        public long CombinedTracksLength { get; protected set; } // number of bytes
        
        /// <summary>
        /// sum of bitrates of all tracks in the stream (if Metadata was received and held that information)
        /// </summary>
        public long CombinedBitrates { get; protected set; }

        #endregion

        TcpClient tcpClient = null;
        NetworkStream networkStream = null;

        Link Link = new Link();

        int outChunkSize = RTMP_DEFAULT_CHUNKSIZE;
        int bytesReadTotal = 0;
        int lastSentBytesRead = 0;
        int m_numInvokes;
        int m_nBufferMS = (10 * 60 * 60 * 1000)	/* 10 hours default */;
        int m_stream_id; // returned in _result from invoking createStream            
        int m_nBWCheckCounter;
        int m_nServerBW;
        int m_nClientBW;
        byte m_nClientBW2;
        RTMPPacket[] m_vecChannelsIn = new RTMPPacket[64];
        RTMPPacket[] m_vecChannelsOut = new RTMPPacket[64];
        uint[] m_channelTimestamp = new uint[64]; // abs timestamp of last packet
        Queue<string> m_methodCalls = new Queue<string>(); //remote method calls queue
        public bool invalidRTMPHeader = false;

        public bool Connect(Link link)
        {
            // close any previous connection
            Close();

            Link = link;
            
            // connect            
            tcpClient = new TcpClient(Link.hostname, Link.port) { NoDelay = true };
            networkStream = tcpClient.GetStream();

            if (!HandShake(true)) return false;
            if (!SendConnect()) return false;

            return true;
        }
        
        public bool IsConnected()
        {
            return tcpClient != null && tcpClient.Connected;
        }

        public bool GetNextMediaPacket(out RTMPPacket packet)
        {
            packet = null;
            bool bHasMediaPacket = false;
            while (!bHasMediaPacket && IsConnected() && ReadPacket(out packet))
            {
                
                if (!packet.IsReady())
                {                    
                    continue;
                }

                switch (packet.PacketType)
                {
                    case PacketType.ChunkSize:                        
                        HandleChangeChunkSize(packet);
                        break;
                    case PacketType.BytesRead:                        
                        //CLog::Log(LOGDEBUG,"%s, received: bytes read report", __FUNCTION__);
                        break;
                    case PacketType.Control:
                        HandlePing(packet);
                        break;
                    case PacketType.ServerBW:
                        HandleServerBW(packet);
                        break;
                    case PacketType.ClientBW:
                        HandleClientBW(packet);
                        break;
                    case PacketType.Audio:
                        //CLog::Log(LOGDEBUG,"%s, received: audio %lu bytes", __FUNCTION__, packet.m_nBodySize);
                        //HandleAudio(packet);
                        bHasMediaPacket = true;
                        break;
                    case PacketType.Video:
                        //CLog::Log(LOGDEBUG,"%s, received: video %lu bytes", __FUNCTION__, packet.m_nBodySize);
                        //HandleVideo(packet);
                        bHasMediaPacket = true;
                        break;
                    case PacketType.Metadata:
                        //CLog::Log(LOGDEBUG,"%s, received: notify %lu bytes", __FUNCTION__, packet.m_nBodySize);
                        HandleMetadata(packet);
                        bHasMediaPacket = true;
                        break;
                    case PacketType.Invoke:
                        //CLog::Log(LOGDEBUG,"%s, received: invoke %lu bytes", __FUNCTION__, packet.m_nBodySize);
                        HandleInvoke(packet);
                        break;
                    case PacketType.FlvTags:
                        //Logger.Log(string.Format("received: FLV tag(s) {0} bytes", packet.m_nBodySize));
                        HandleFlvTags(packet);                        
                        bHasMediaPacket = true;
                        break;
                    default:
                        Logger.Log(string.Format("Unknown packet type received: {0}", packet.PacketType));
                        break;
                }
                //if (!bHasMediaPacket) packet.FreePacket();
                packet.m_nBytesRead = 0;
            }
            if (bHasMediaPacket) Playing = true;
            return bHasMediaPacket;
        }
        
        bool ReadPacket(out RTMPPacket packet)
        {
            // Chunk Basic Header (1, 2 or 3 bytes)
            // the two most significant bits hold the chunk type
            // value in the 6 least significant bits gives the chunk stream id (0,1,2 are reserved): 0 -> 3 byte header | 1 -> 2 byte header | 2 -> low level protocol message | 3-63 -> stream id
            byte[] singleByteToReadBuffer = new byte[1];
            if (ReadN(singleByteToReadBuffer, 0, 1) != 1)
            {
                Logger.Log("failed to read RTMP packet header");
                packet = null;
                return false;
            }

            byte type = singleByteToReadBuffer[0];

            byte headerType = (byte)((type & 0xc0) >> 6);
            byte channel = (byte)(type & 0x3f);
            uint nSize = packetSize[headerType];

            //Logger.Log(string.Format("reading RTMP packet chunk on channel {0}, headersz {1}", channel, nSize));

            if (nSize < RTMP_LARGE_HEADER_SIZE)
                packet = m_vecChannelsIn[channel]; // using values from the last message of this channel
            else
                packet = new RTMPPacket() { HeaderType = (HeaderType)headerType, m_nChannel = channel }; // new packet

            nSize--;

            byte[] header = new byte[RTMP_LARGE_HEADER_SIZE];            
            if (nSize > 0 && ReadN(header, 0, (int)nSize) != nSize)
            {
                Logger.Log(string.Format("failed to read RTMP packet header. type: {0}", type));
                return false;
            }            

            if (nSize >= 3)
                packet.m_nInfoField1 = ReadInt24(header, 0);

            if (nSize >= 6)
            {
                packet.m_nBodySize = (uint)ReadInt24(header, 3);
                //Logger.Log(string.Format("new packet body to read {0}", packet.m_nBodySize));
                packet.m_nBytesRead = 0;
                packet.FreePacketHeader(); // new packet body
            }

            if (nSize > 6)
                if (Enum.IsDefined(typeof(PacketType), header[6])) packet.PacketType = (PacketType)header[6];

            if (nSize == 11)
                packet.m_nInfoField2 = ReadInt32LE(header, 7);

            if (packet.m_nBodySize >= 0 && packet.m_body == null && !packet.AllocPacket((int)packet.m_nBodySize))
            {
                //CLog::Log(LOGDEBUG,"%s, failed to allocate packet", __FUNCTION__);
                return false;
            }

            uint nToRead = packet.m_nBodySize - packet.m_nBytesRead;
            uint nChunk = (uint)InChunkSize;
            if (nToRead < nChunk)
                nChunk = nToRead;

            int read = ReadN(packet.m_body, (int)packet.m_nBytesRead, (int)nChunk);
            if (read != nChunk)
            {
                Logger.Log(string.Format("failed to read RTMP packet body. len: {0}", packet.m_nBodySize));
                packet.m_body = null; // we dont want it deleted since its pointed to from the stored packets (m_vecChannelsIn)
                return false;
            }            

            packet.m_nBytesRead += nChunk;

            // keep the packet as ref for other packets on this channel
            m_vecChannelsIn[packet.m_nChannel] = packet;                        

            if (packet.IsReady())
            {
                //Logger.Log(string.Format("packet with {0} bytes read", packet.m_nBytesRead));
                packet.m_nTimeStamp = (uint)packet.m_nInfoField1;

                // make packet's timestamp absolute 
                if (!packet.m_hasAbsTimestamp) 
                    packet.m_nTimeStamp += m_channelTimestamp[packet.m_nChannel]; // timestamps seem to be always relative!! 
                m_channelTimestamp[packet.m_nChannel] = packet.m_nTimeStamp;

                // reset the data from the stored packet. we keep the header since we may use it later if a new packet for this channel
                // arrives and requests to re-use some info (small packet header)
                //m_vecChannelsIn[packet.m_nChannel].m_body = null;
                //m_vecChannelsIn[packet.m_nChannel].m_nBytesRead = 0;
                m_vecChannelsIn[packet.m_nChannel].m_hasAbsTimestamp = false; // can only be false if we reuse header
            }

            return true;
        }

        public void Close()
        {
            if (networkStream != null) networkStream.Close(1000);
            if (tcpClient != null && tcpClient.Connected) tcpClient.Close();

            networkStream = null;
            tcpClient = null;

            
            InChunkSize = RTMP_DEFAULT_CHUNKSIZE;
            outChunkSize = RTMP_DEFAULT_CHUNKSIZE;
            Playing = false;
            Duration = 0;
            CombinedTracksLength = 0;
            CombinedBitrates = 0;

            m_nBWCheckCounter = 0;
            m_nClientBW = 2500000;
            m_nClientBW2 = 2;
            m_nServerBW = 2500000;
            bytesReadTotal = 0;
            lastSentBytesRead = 0;
            m_numInvokes = 0;

            for (int i = 0; i < 64; i++)
            {
                m_vecChannelsIn[i] = null;
                m_vecChannelsOut[i] = null;
                m_channelTimestamp[i] = 0;
            }

            m_methodCalls.Clear();
        }

        #region Send Client Packets

        bool SendConnect()
        {            
            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x03;   // control channel (invoke)
            packet.HeaderType = HeaderType.Large;
            packet.PacketType = PacketType.Invoke;
            packet.AllocPacket(4096);

            Logger.Log("Sending connect");
            List<byte> enc = new List<byte>();
            EncodeString(enc, "connect");
            EncodeNumber(enc, m_numInvokes++);
            enc.Add(0x03); //Object Datatype                
            EncodeString(enc, "app", Link.app);
            EncodeString(enc, "flashVer", "WIN 10,0,32,18");
            if (!string.IsNullOrEmpty(Link.swfUrl)) EncodeString(enc, "swfUrl", Link.swfUrl);
            EncodeString(enc, "tcUrl", Link.tcUrl);
            EncodeBoolean(enc, "fpad", false);
            EncodeNumber(enc, "capabilities", 15.0);
            EncodeNumber(enc, "audioCodecs", 3191.0);
            EncodeNumber(enc, "videoCodecs", 252.0);
            EncodeNumber(enc, "videoFunction", 1.0);
            if (!string.IsNullOrEmpty(Link.pageUrl)) EncodeString(enc, "pageUrl", Link.pageUrl);
            EncodeNumber(enc, "objectEncoding", 0.0);
            enc.Add(0); enc.Add(0); enc.Add(0x09); // end of object - 0x00 0x00 0x09

            // add auth string
            if (!string.IsNullOrEmpty(Link.auth))
            {
                EncodeBoolean(enc, true);
                EncodeString(enc, Link.auth);
            }

            Array.Copy(enc.ToArray(), packet.m_body, enc.Count);
            packet.m_nBodySize = (uint)enc.Count;
            
            return SendRTMP(packet);
        }

        bool SendPlay()
        {            
            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x08;   // we make 8 our stream channel
            packet.HeaderType = HeaderType.Large;
            packet.PacketType = PacketType.Invoke;
            packet.m_nInfoField2 = m_stream_id;

            packet.AllocPacket(256); // should be enough
            List<byte> enc = new List<byte>();

            EncodeString(enc, "play");
            EncodeNumber(enc, m_numInvokes++);
            enc.Add(0x05); // NULL  

            Logger.Log(string.Format("Sending play: '{0}'", Link.playpath));

            EncodeString(enc, Link.playpath);

            /* Optional parameters start and len.
             *
             * start: -2, -1, 0, positive number
             *  -2: looks for a live stream, then a recorded stream, if not found any open a live stream
             *  -1: plays a live stream
             * >=0: plays a recorded streams from 'start' milliseconds
            */
            EncodeNumber(enc, 0.0);
            /* len: -1, 0, positive number
             *  -1: plays live or recorded stream to the end (default)
             *   0: plays a frame 'start' ms away from the beginning
             *  >0: plays a live or recoded stream for 'len' milliseconds
             */
            //EncodeNumber(enc, -2.0);

            packet.m_body = enc.ToArray();
            packet.m_nBodySize = (uint)enc.Count;

            return SendRTMP(packet);
        }

        /// <summary>
        /// The type of Ping packet is 0x4 and contains two mandatory parameters and two optional parameters. 
        /// The first parameter is the type of Ping (short integer).
        /// The second parameter is the target of the ping. 
        /// As Ping is always sent in Channel 2 (control channel) and the target object in RTMP header is always 0 
        /// which means the Connection object, 
        /// it's necessary to put an extra parameter to indicate the exact target object the Ping is sent to. 
        /// The second parameter takes this responsibility. 
        /// The value has the same meaning as the target object field in RTMP header. 
        /// (The second value could also be used as other purposes, like RTT Ping/Pong. It is used as the timestamp.) 
        /// The third and fourth parameters are optional and could be looked upon as the parameter of the Ping packet. 
        /// Below is an unexhausted list of Ping messages.
        /// type 0: Clear the stream. No third and fourth parameters. The second parameter could be 0. After the connection is established, a Ping 0,0 will be sent from server to client. The message will also be sent to client on the start of Play and in response of a Seek or Pause/Resume request. This Ping tells client to re-calibrate the clock with the timestamp of the next packet server sends.
        /// type 1: Tell the stream to clear the playing buffer.
        /// type 3: Buffer time of the client. The third parameter is the buffer time in millisecond.
        /// type 4: Reset a stream. Used together with type 0 in the case of VOD. Often sent before type 0.
        /// type 6: Ping the client from server. The second parameter is the current time.
        /// type 7: Pong reply from client. The second parameter is the time the server sent with his ping request.
        /// type 26: SWFVerification request
        /// type 27: SWFVerification response
        /// type 31: Buffer empty
        /// type 32: Buffer full
        /// </summary>
        /// <param name="nType"></param>
        /// <param name="nObject"></param>
        /// <param name="nTime"></param>
        /// <returns></returns>
        bool SendPing(short nType, uint nObject, uint nTime)
        {
            Logger.Log(string.Format("Sending ping type: {0}", nType));

            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x02;   // control channel (ping)
            packet.HeaderType = HeaderType.Medium;
            packet.PacketType = PacketType.Control;
            //packet.m_nInfoField1 = System.Environment.TickCount;

            int nSize = (nType == 0x03 ? 10 : 6); // type 3 is the buffer time and requires all 3 parameters. all in all 10 bytes.
            if (nType == 0x1B) nSize = 44;
            packet.AllocPacket(nSize);
            packet.m_nBodySize = (uint)nSize;

            List<byte> buf = new List<byte>();
            EncodeInt16(buf, nType);

            if (nType == 0x1B)
            {
                buf.AddRange(Link.SWFVerificationResponse);
            }
            else
            {
                if (nSize > 2)
                    EncodeInt32(buf, (int)nObject);

                if (nSize > 6)
                    EncodeInt32(buf, (int)nTime);
            }
            packet.m_body = buf.ToArray();
            return SendRTMP(packet);
        }

        bool SendCheckBW()
        {
            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x03;   // control channel (invoke)

            packet.HeaderType = HeaderType.Large;
            packet.PacketType = PacketType.Invoke;
            //packet.m_nInfoField1 = System.Environment.TickCount;

            Logger.Log("Sending _checkbw");
            List<byte> enc = new List<byte>();
            EncodeString(enc, "_checkbw");
            EncodeNumber(enc, m_numInvokes++);
            enc.Add(0x05); // NULL            

            packet.m_nBodySize = (uint)enc.Count;
            packet.m_body = enc.ToArray();

            // triggers _onbwcheck and eventually results in _onbwdone
            return SendRTMP(packet);
        }

        bool SendCheckBWResult(double txn)
        {
            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x03;   // control channel (invoke)
            packet.HeaderType = HeaderType.Medium;
            packet.PacketType = PacketType.Invoke;
            packet.m_nInfoField1 = 0x16 * m_nBWCheckCounter; // temp inc value. till we figure it out.

            packet.AllocPacket(256); // should be enough
            List<byte> enc = new List<byte>();
            EncodeString(enc, "_result");
            EncodeNumber(enc, txn);
            enc.Add(0x05); // NULL            
            EncodeNumber(enc, (double)m_nBWCheckCounter++);

            packet.m_nBodySize = (uint)enc.Count;
            packet.m_body = enc.ToArray();

            return SendRTMP(packet);
        }
        
        bool SendBytesReceived()
        {
            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x02;   // control channel (invoke)
            packet.HeaderType = HeaderType.Medium;
            packet.PacketType = PacketType.BytesRead;

            packet.AllocPacket(4);
            packet.m_nBodySize = 4;

            List<byte> enc = new List<byte>();
            EncodeInt32(enc, bytesReadTotal);
            packet.m_nBodySize = (uint)enc.Count;
            packet.m_body = enc.ToArray();

            lastSentBytesRead = bytesReadTotal;
            Logger.Log(string.Format("Send bytes report. ({0} bytes)", bytesReadTotal));
            return SendRTMP(packet);
        }

        bool SendServerBW()
        {
            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x02;   // control channel (invoke)
            packet.HeaderType = HeaderType.Large;
            packet.PacketType = PacketType.ServerBW;
            
            packet.AllocPacket(4);
            packet.m_nBodySize = 4;

            List<byte> bytesToSend = new List<byte>();
            EncodeInt32(bytesToSend, m_nServerBW); // was hard coded : 0x001312d0
            packet.m_body = bytesToSend.ToArray();
            return SendRTMP(packet);
        }

        bool SendCreateStream()
        {
            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x03;   // control channel (invoke)
            packet.HeaderType = HeaderType.Medium;
            packet.PacketType = PacketType.Invoke;

            Logger.Log("Sending createStream");
            packet.AllocPacket(256); // should be enough
            List<byte> enc = new List<byte>();
            EncodeString(enc, "createStream");
            EncodeNumber(enc, m_numInvokes++);
            enc.Add(0x05); // NULL

            packet.m_nBodySize = (uint)enc.Count;
            packet.m_body = enc.ToArray();

            return SendRTMP(packet);
        }

        bool SendSecureTokenResponse(string resp)
        {
            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x03;	/* control channel (invoke) */
            packet.HeaderType = HeaderType.Medium;
            packet.PacketType = PacketType.Invoke;

            Logger.Log(string.Format("Sending SecureTokenResponse: {0}", resp));
            List<byte> enc = new List<byte>();
            EncodeString(enc, "secureTokenResponse");
            EncodeNumber(enc, 0.0);
            enc.Add(0x05); // NULL
            EncodeString(enc, resp);

            packet.m_nBodySize = (uint)enc.Count;
            packet.m_body = enc.ToArray();

            return SendRTMP(packet, false);
        }

        bool SendFCSubscribe()
        {
            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x03;   // control channel (invoke)
            packet.HeaderType = HeaderType.Medium;
            packet.PacketType = PacketType.Invoke;

            Logger.Log(string.Format("Sending FCSubscribe: {0}", Link.subscribepath));
            List<byte> enc = new List<byte>();
            EncodeString(enc, "FCSubscribe");
            EncodeNumber(enc, m_numInvokes++);
            enc.Add(0x05); // NULL
            EncodeString(enc, Link.subscribepath);

            packet.m_nBodySize = (uint)enc.Count;
            packet.m_body = enc.ToArray();

            return SendRTMP(packet);
        }

        bool SendAuth()
        {
            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x03;   // control channel (invoke)
            packet.HeaderType = HeaderType.Large;
            packet.PacketType = PacketType.Invoke;

            Logger.Log("Sending " + Link.authObjName);
            List<byte> enc = new List<byte>();
            EncodeString(enc, Link.authObjName);
            EncodeNumber(enc, m_numInvokes++);
            enc.Add(0x05); // NULL
            EncodeString(enc, Link.auth);            

            packet.m_nBodySize = (uint)enc.Count;
            packet.m_body = enc.ToArray();

            return SendRTMP(packet, false);
        }

        bool SendRTMP(RTMPPacket packet) { return SendRTMP(packet, true); }
        bool SendRTMP(RTMPPacket packet, bool queue)
        {
            RTMPPacket prevPacket = m_vecChannelsOut[packet.m_nChannel];
            if (packet.HeaderType != HeaderType.Large)
            {
                // compress a bit by using the prev packet's attributes
                if (prevPacket != null && prevPacket.m_nBodySize == packet.m_nBodySize && packet.HeaderType == HeaderType.Medium)
                    packet.HeaderType = HeaderType.Small;

                if (prevPacket != null && prevPacket.m_nInfoField2 == packet.m_nInfoField2 && packet.HeaderType == HeaderType.Small)
                    packet.HeaderType = HeaderType.Minimum;
            }
            
            uint nSize = packetSize[(byte)packet.HeaderType];
            List<byte> header = new List<byte>();//byte[RTMP_LARGE_HEADER_SIZE];
            header.Add((byte)(((byte)packet.HeaderType << 6) | packet.m_nChannel));
            if (nSize > 1)
                EncodeInt24(header, packet.m_nInfoField1);

            if (nSize > 4)
            {
                EncodeInt24(header, (int)packet.m_nBodySize);
                header.Add((byte)packet.PacketType);
            }

            if (nSize > 8)
                EncodeInt32LE(header, packet.m_nInfoField2);            
            
            uint hSize = nSize;
            byte[] headerBuffer = header.ToArray();
            nSize = packet.m_nBodySize;
            byte[] buffer = packet.m_body;
            uint bufferOffset = 0;
            while (nSize+hSize > 0)
            {
                uint nChunkSize = (uint)outChunkSize; // packet.PacketType == PacketType.Invoke ? (uint)InChunkSize : packet.m_nBodySize;                
                if (nSize < InChunkSize) nChunkSize = nSize;

                if (hSize > 0)
                {
                    byte[] combinedBuffer = new byte[headerBuffer.Length + nChunkSize];
                    Array.Copy(headerBuffer, combinedBuffer, headerBuffer.Length);
                    Array.Copy(buffer, (int)bufferOffset, combinedBuffer, headerBuffer.Length, (int)nChunkSize);
                    WriteN(combinedBuffer, 0, combinedBuffer.Length);
                    hSize = 0;
                }
                else
                {
                    WriteN(buffer, (int)bufferOffset, (int)nChunkSize);
                }

                nSize -= nChunkSize;
                bufferOffset += nChunkSize;

                if (nSize > 0)
                {
                    byte sep = (byte)(0xc0 | packet.m_nChannel);
                    hSize = 1;
                    headerBuffer = new byte[1] { sep };
                }
            }

            if (packet.PacketType == PacketType.Invoke && queue) // we invoked a remote method, keep it in call queue till result arrives
                m_methodCalls.Enqueue(ReadString(packet.m_body, 1));

            m_vecChannelsOut[packet.m_nChannel] = packet;
            m_vecChannelsOut[packet.m_nChannel].m_body = null;
            
            return true;
        }

        #endregion

        #region Handle Server Packets

        void HandleChangeChunkSize(RTMPPacket packet)
        {
            if (packet.m_nBodySize >= 4)
            {
                InChunkSize = ReadInt32(packet.m_body, 0);
                Logger.Log(string.Format("received: chunk size change to {0}", InChunkSize));
            }
        }

        void HandlePing(RTMPPacket packet)
        {
            short nType = -1;
            if (packet.m_body != null && packet.m_nBodySize >= 2)
                nType = (short)ReadInt16(packet.m_body, 0);
            
            Logger.Log(string.Format("received: ping, type: {0}", nType));
            
            if (nType == 0x06 && packet.m_nBodySize >= 6) // server ping. reply with pong.
            {
                uint nTime = (uint)ReadInt32(packet.m_body, 2);
                SendPing(0x07, nTime, 0);
            }

            if (nType == 0x1A)
            {
                // respond with HMAC SHA256 of decompressed SWF, key is the 30byte player key, also the last 30 bytes of the server handshake are applied
                if (Link.SWFHash != null)
                {
                    SendPing(0x1B, 0, 0);
                }
                else
                {
                    Logger.Log("Ignoring SWFVerification request, swfhash and swfsize parameters not set!");
                }
            }
        }

        void HandleServerBW(RTMPPacket packet)
        {
            m_nServerBW = ReadInt32(packet.m_body, 0);
            Logger.Log(string.Format("HandleServerBW: server BW = {0}", m_nServerBW));
        }

        void HandleClientBW(RTMPPacket packet)
        {
            m_nClientBW = ReadInt32(packet.m_body, 0);
            if (packet.m_nBodySize > 4)
                m_nClientBW2 = packet.m_body[4];
            else
                m_nClientBW2 = 0;
            Logger.Log(string.Format("HandleClientBW: client BW = {0} {1}", m_nClientBW, m_nClientBW2));
        }

        void HandleInvoke(RTMPPacket packet)
        {
            if (packet.m_body[0] != 0x02) // make sure it is a string method name we start with
            {
                //CLog::Log(LOGWARNING,"%s, Sanity failed. no string method in invoke packet", __FUNCTION__);
                return;
            }

            AMFObject obj = new AMFObject();
            int nRes = obj.Decode(packet.m_body, 0, (int)packet.m_nBodySize, false);
            if (nRes < 0)
            {
                //CLog::Log(LOGERROR,"%s, error decoding invoke packet", __FUNCTION__);
                return;
            }

            obj.Dump();
            string method = obj.GetProperty(0).GetString();
            double txn = obj.GetProperty(1).GetNumber();

            Logger.Log(string.Format("server invoking <{0}>", method));

            if (method == "_result")
            {
                string methodInvoked = m_methodCalls.Dequeue();                

                Logger.Log(string.Format("received result for method call <{0}>", methodInvoked));

                if (methodInvoked == "connect")
                {
                    if (!string.IsNullOrEmpty(Link.token))
                    {
                        List<AMFObjectProperty> props = new List<AMFObjectProperty>();
                        obj.FindMatchingProperty("secureToken", props, int.MaxValue);
                        if (props.Count > 0)
                        {
                            string decodedToken = Tea.Decrypt(props[0].GetString(), Link.token);
                            SendSecureTokenResponse(decodedToken);
                        }
                    }
                    SendServerBW();                    
                    if (!string.IsNullOrEmpty(Link.subscribepath)) SendFCSubscribe();                    
                    SendCreateStream();
                    SendPing(3, 0, 300);
                }
                else if (methodInvoked == "createStream")
                {
                    m_stream_id = (int)obj.GetProperty(3).GetNumber();
                    SendPlay();
                    SendPing(3, (uint)m_stream_id, (uint)m_nBufferMS);
                }
                else if (methodInvoked == "play")
                {
                    Playing = true;
                }
            }
            else if (method == "onBWDone")
            {
                SendCheckBW();
            }
            else if (method == "_onbwcheck")
            {
                SendCheckBWResult(txn);
            }
            else if (method == "_onbwdone")
            {
                if (m_methodCalls.Contains("_checkbw"))
                {
                    string[] queue = m_methodCalls.ToArray();
                    m_methodCalls.Clear();
                    for (int i = 0; i < queue.Length; i++) if (queue[i] != "_checkbw") m_methodCalls.Enqueue(queue[i]);
                }
            }
            else if (method == "_error")
            {
                Logger.Log("rtmp server sent error");
            }
            else if (method == "close")
            {
                Logger.Log("rtmp server requested close");
                Close();
            }
            else if (method == "onStatus")
            {
                string code = obj.GetProperty(3).GetObject().GetProperty("code").GetString();
                string level = obj.GetProperty(3).GetObject().GetProperty("level").GetString();

                Logger.Log(string.Format("onStatus: code :{0}, level: {1}", code, level));

                if (code == "NetStream.Failed"
                || code == "NetStream.Play.Failed"
                || code == "NetStream.Play.Stop"
                || code == "NetStream.Play.StreamNotFound"
                || code == "NetConnection.Connect.InvalidApp")
                    Close();
            }
            else
            {

            }
        }

        void HandleMetadata(RTMPPacket packet)
        {
            HandleMetadata(packet.m_body, 0, (int)packet.m_nBodySize);
        }

        void HandleMetadata(byte[] buffer, int offset, int size)
        {            
            AMFObject obj = new AMFObject();
            int nRes = obj.Decode(buffer, offset, size, false);
            if (nRes < 0)
            {
                //Log(LOGERROR, "%s, error decoding meta data packet", __FUNCTION__);
                return;
            }

            obj.Dump();
            string metastring = obj.GetProperty(0).GetString();

            if (metastring == "onMetaData")
            {
                List<AMFObjectProperty> props = new List<AMFObjectProperty>();
                obj.FindMatchingProperty("duration", props, 1);
                if (props.Count > 0)
                {
                    Duration = props[0].GetNumber();
                    Logger.Log(string.Format("Set duration: {0}", Duration));
                }
                props.Clear();                
                obj.FindMatchingProperty("audiodatarate", props, 1);
                if (props.Count > 0)
                {
                    int audiodatarate = (int)props[0].GetNumber();
                    CombinedBitrates += audiodatarate;
                    Logger.Log(string.Format("audiodatarate: {0}", audiodatarate));
                }
                props.Clear();
                obj.FindMatchingProperty("videodatarate", props, 1);
                if (props.Count > 0)
                {
                    int videodatarate = (int)props[0].GetNumber();
                    CombinedBitrates += videodatarate;
                    Logger.Log(string.Format("videodatarate: {0}", videodatarate));
                }
                if (CombinedTracksLength == 0)
                {
                    props.Clear();
                    obj.FindMatchingProperty("filesize", props, int.MaxValue);
                    if (props.Count > 0)
                    {
                        CombinedTracksLength = (int)props[0].GetNumber();
                        Logger.Log(string.Format("Set CombinedTracksLength from filesize: {0}", CombinedTracksLength));
                    }
                }
                if (CombinedTracksLength == 0)
                {
                    props.Clear();
                    obj.FindMatchingProperty("datasize", props, int.MaxValue);
                    if (props.Count > 0)
                    {
                        CombinedTracksLength = (int)props[0].GetNumber();
                        Logger.Log(string.Format("Set CombinedTracksLength from datasize: {0}", CombinedTracksLength));
                    }
                }   
            }
        }

        void HandleFlvTags(RTMPPacket packet)
        {
            // go through FLV packets and handle metadata packets
            int pos = 0;

            while (pos + 11 < packet.m_nBodySize)
            {
                int dataSize = ReadInt24(packet.m_body, pos + 1); // size without header (11) and prevTagSize (4)

                if (pos + 11 + dataSize + 4 > packet.m_nBodySize)
                {
                    Logger.Log("Stream corrupt?!");
                    break;
                }
                if (packet.m_body[pos] == 0x12)
                {
                    HandleMetadata(packet.m_body, pos + 11, dataSize);
                }
                pos += (11 + dataSize + 4);
            }	
        }

        #endregion        

        #region Encode Functions

        public static void EncodeString(List<byte> output, string strName, string strValue)
        {
            short length = IPAddress.HostToNetworkOrder((short)strName.Length);
            output.AddRange(BitConverter.GetBytes(length));
            output.AddRange(Encoding.ASCII.GetBytes(strName));
            EncodeString(output, strValue);
        }

        public static void EncodeString(List<byte> output, string strValue)
        {
            output.Add(0x02); // type: String
            short length = IPAddress.HostToNetworkOrder((short)strValue.Length);
            output.AddRange(BitConverter.GetBytes(length));
            output.AddRange(Encoding.ASCII.GetBytes(strValue));
        }

        public static void EncodeBoolean(List<byte> output, string strName, bool bVal)
        {
            short length = IPAddress.HostToNetworkOrder((short)strName.Length);
            output.AddRange(BitConverter.GetBytes(length));
            output.AddRange(Encoding.ASCII.GetBytes(strName));
            EncodeBoolean(output, bVal);
        }

        public static void EncodeBoolean(List<byte> output, bool bVal)
        {
            output.Add(0x01); // type: Boolean
            output.Add(bVal ? (byte)0x01 : (byte)0x00);
        }

        public static void EncodeNumber(List<byte> output, string strName, double dVal)
        {
            short length = IPAddress.HostToNetworkOrder((short)strName.Length);
            output.AddRange(BitConverter.GetBytes(length));
            output.AddRange(Encoding.ASCII.GetBytes(strName));
            EncodeNumber(output, dVal);
        }

        public static void EncodeNumber(List<byte> output, double dVal)
        {
            output.Add(0x00); // type: Number
            byte[] bytes = BitConverter.GetBytes(dVal);
            for (int i = bytes.Length - 1; i >= 0; i--) output.Add(bytes[i]); // add in reversed byte order
        }

        public static void EncodeInt16(List<byte> output, short nVal)
        {
            output.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(nVal)));
        }

        public static void EncodeInt24(List<byte> output, int nVal)
        {
            byte[] bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(nVal));
            for (int i = 1; i < 4; i++) output.Add(bytes[i]);
        }

        /// <summary>
        /// big-endian 32bit integer
        /// </summary>
        /// <param name="output"></param>
        /// <param name="nVal"></param>
        public static void EncodeInt32(List<byte> output, int nVal)
        {
            output.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(nVal)));
        }

        /// <summary>
        /// little-endian 32bit integer
        /// TODO: this is wrong on big-endian processors
        /// </summary>
        /// <param name="output"></param>
        /// <param name="nVal"></param>
        public static void EncodeInt32LE(List<byte> output, int nVal)
        {
            output.AddRange(BitConverter.GetBytes(nVal));
        }

        #endregion

        #region Read Functions

        public static string ReadString(byte[] data, int offset)
        {
            string strRes = "";
            ushort length = ReadInt16(data, offset);
            if (length > 0) strRes = Encoding.ASCII.GetString(data, offset + 2, length);
            return strRes;
        }

        public static string ReadLongString(byte[] data, int offset)
        {
            string strRes = "";
            int length = ReadInt32(data, offset);
            if (length > 0) strRes = Encoding.ASCII.GetString(data, offset + 4, length);
            return strRes;
        }

        public static ushort ReadInt16(byte[] data, int offset)
        {
            return (ushort)((data[offset] << 8) | data[offset+1]);
        }

        public static int ReadInt24(byte[] data, int offset)
        {
            return (data[offset] << 16) | (data[offset + 1] << 8) | data[offset+2];
        }

        /// <summary>
        /// big-endian 32bit integer
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static int ReadInt32(byte[] data, int offset)
        {
            return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset+3];
        }

        /// <summary>
        /// little-endian 32bit integer
        /// TODO: this is wrong on big-endian processors
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static int ReadInt32LE(byte[] data, int offset)
        {
            return BitConverter.ToInt32(data, offset);
        }

        public static bool ReadBool(byte[] data, int offset)
        {
            return data[offset] == 0x01;
        }

        public static double ReadNumber(byte[] data, int offset)
        {
            byte[] bytes = new byte[8];
            Array.Copy(data, offset, bytes, 0, 8);
            Array.Reverse(bytes); // reversed byte order
            return BitConverter.ToDouble(bytes, 0);
        }

        #endregion        

        # region Handshake

        bool HandShake(bool FP9HandShake)
        {            
            bool encrypted = Link.protocol == Protocol.RTMPE || Link.protocol == Protocol.RTMPTE;

            if (encrypted && !FP9HandShake)
            {
                Logger.Log("RTMPE requires FP9 handshake!");
                return false;
            }

            byte[] clientsig = new byte[RTMP_SIG_SIZE + 1];
            byte[] serversig = new byte[RTMP_SIG_SIZE];            

            if (encrypted) clientsig[0] = 0x06; // 0x08 is RTMPE as well
            else clientsig[0] = 0x03;

            int uptime = System.Environment.TickCount;
            byte[] uptime_bytes = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(uptime));
            Array.Copy(uptime_bytes, 0, clientsig, 1, uptime_bytes.Length);

            if (FP9HandShake)
            {                
                /* set version to at least 9.0.115.0 */
                if (encrypted)
                {
                    clientsig[5] = 128;
                    clientsig[7] = 3;
                }
                else
                {
                    clientsig[5] = 10;
                    clientsig[7] = 45;
                }
                clientsig[6] = 0;
                clientsig[8] = 2;
                
                Logger.Log(string.Format("Client type: {0}", clientsig[0]));
            }
            else
            {
                clientsig[5] = 0; clientsig[6] = 0; clientsig[7] = 0; clientsig[8] = 0;
            }

            // generate random data
            Random rand = new Random();
            for (int i = 9; i <= RTMP_SIG_SIZE; i++) clientsig[i] = (byte)rand.Next(0, 256);

            int dhposClient = 0;
            byte[] keyIn = null;
            byte[] keyOut = null;

            if (encrypted)
            {
                // generate Diffie-Hellmann parameters                                
                Org.BouncyCastle.Crypto.Parameters.DHParameters dhParams = 
                    new Org.BouncyCastle.Crypto.Parameters.DHParameters(
                        new Org.BouncyCastle.Math.BigInteger(1, DH_MODULUS_BYTES),
                        Org.BouncyCastle.Math.BigInteger.ValueOf(2));
                Org.BouncyCastle.Crypto.Parameters.DHKeyGenerationParameters keySpec = new Org.BouncyCastle.Crypto.Parameters.DHKeyGenerationParameters(new Org.BouncyCastle.Security.SecureRandom(), dhParams);
                Org.BouncyCastle.Crypto.Generators.DHBasicKeyPairGenerator keyGen = new Org.BouncyCastle.Crypto.Generators.DHBasicKeyPairGenerator();                
                keyGen.Init(keySpec);
                Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair pair = keyGen.GenerateKeyPair();
                Org.BouncyCastle.Crypto.Agreement.DHBasicAgreement keyAgreement = new Org.BouncyCastle.Crypto.Agreement.DHBasicAgreement();
                keyAgreement.Init(pair.Private);
                Link.keyAgreement = keyAgreement;

                byte[] publicKey = (pair.Public as Org.BouncyCastle.Crypto.Parameters.DHPublicKeyParameters).Y.ToByteArray();

                byte[] temp = new byte[128];
                if (publicKey.Length < 128)
                {
                    Array.Copy(publicKey, 0, temp, 128 - publicKey.Length, publicKey.Length);
                    publicKey = temp;
                    Logger.Log("padded public key length to 128");
                }
                else if (publicKey.Length > 128)
                {
                    Array.Copy(publicKey, publicKey.Length - 128, temp, 0, 128);
                    publicKey = temp;
                    Logger.Log("truncated public key length to 128");
                }
                
                dhposClient = (int)GetDHOffset1(clientsig, 1, RTMP_SIG_SIZE);
                Logger.Log(string.Format("DH pubkey position: {0}", dhposClient));

                Array.Copy(publicKey, 0, clientsig, 1 + dhposClient, 128);                
            }

            // set handshake digest
            if (FP9HandShake)
            {
                int digestPosClient = (int)GetDigestOffset1(clientsig,1, RTMP_SIG_SIZE); // maybe reuse this value in verification
                Logger.Log(string.Format("Client digest offset: {0}", digestPosClient));

                CalculateDigest(digestPosClient, clientsig, 1, GenuineFPKey, 30, clientsig, 1 + digestPosClient);

                Logger.Log("Initial client digest: ");
                string digestAsHexString = "";
                for(int i = 1 + digestPosClient; i<1 + digestPosClient+SHA256_DIGEST_LENGTH;i++) digestAsHexString += clientsig[i].ToString("X2") + " ";
                Logger.Log(digestAsHexString);
            }

            WriteN(clientsig, 0, RTMP_SIG_SIZE + 1);

            byte[] singleByteToReadBuffer = new byte[1];
            if (ReadN(singleByteToReadBuffer, 0, 1) != 1) return false;
            byte type = singleByteToReadBuffer[0]; // 0x03 or 0x06

            Logger.Log(string.Format("Type Answer   : {0}", type.ToString("X2")));

            if (type != clientsig[0]) Logger.Log(string.Format("Type mismatch: client sent {0}, server answered {0}", clientsig[0], type));

            if (ReadN(serversig, 0, RTMP_SIG_SIZE) != RTMP_SIG_SIZE) return false;

            // decode server response
            uint suptime = (uint)ReadInt32(serversig,0);            

            Logger.Log(string.Format("Server Uptime : {0}",suptime));
            Logger.Log(string.Format("FMS Version   : {0}.{1}.{2}.{3}", serversig[4], serversig[5], serversig[6], serversig[7]));

            if (FP9HandShake && type == 3 && serversig[4] == 0) FP9HandShake = false;

            byte[] clientResp;

            if (FP9HandShake)
            {
                // we have to use this signature now to find the correct algorithms for getting the digest and DH positions
                int digestPosServer = (int)GetDigestOffset2(serversig, 0, RTMP_SIG_SIZE);
                int dhposServer = (int)GetDHOffset2(serversig, 0, RTMP_SIG_SIZE);

                if (!VerifyDigest(digestPosServer, serversig, GenuineFMSKey, 36))
                {
                    Logger.Log("Trying different position for server digest!");
                    digestPosServer = (int)GetDigestOffset1(serversig, 0, RTMP_SIG_SIZE);
                    dhposServer = (int)GetDHOffset1(serversig, 0, RTMP_SIG_SIZE);

                    if (!VerifyDigest(digestPosServer, serversig, GenuineFMSKey, 36))
                    {
                        Logger.Log("Couldn't verify the server digest");//,  continuing anyway, will probably fail!\n");
                        return false;
                    }
                }

                Logger.Log(string.Format("Server DH public key offset: {0}", dhposServer));

                // generate SWFVerification token (SHA256 HMAC hash of decompressed SWF, key are the last 32 bytes of the server handshake)            
                if (Link.SWFHash != null)
                {
                    byte[] swfVerify = new byte[2] { 0x01, 0x01 };
                    Array.Copy(swfVerify, Link.SWFVerificationResponse, 2);
                    List<byte> data = new List<byte>();
                    EncodeInt32(data, Link.SWFSize);
                    EncodeInt32(data, Link.SWFSize);
                    Array.Copy(data.ToArray(), 0, Link.SWFVerificationResponse, 2, data.Count);
                    byte[] key = new byte[SHA256_DIGEST_LENGTH];
                    Array.Copy(serversig, RTMP_SIG_SIZE - SHA256_DIGEST_LENGTH, key, 0, SHA256_DIGEST_LENGTH);
                    HMACsha256(Link.SWFHash, 0, SHA256_DIGEST_LENGTH, key, SHA256_DIGEST_LENGTH, Link.SWFVerificationResponse, 10);
                }

                // do Diffie-Hellmann Key exchange for encrypted RTMP
                if (encrypted)
                {
                    // compute secret key	
                    byte[] secretKey = new byte[128];

                    byte[] serverKey = new byte[128];
                    Array.Copy(serversig, dhposServer, serverKey, 0, 128);

                    Org.BouncyCastle.Crypto.Parameters.DHParameters dhParams =
                        new Org.BouncyCastle.Crypto.Parameters.DHParameters(
                            new Org.BouncyCastle.Math.BigInteger(1, DH_MODULUS_BYTES),
                            Org.BouncyCastle.Math.BigInteger.ValueOf(2));

                    Org.BouncyCastle.Crypto.Parameters.DHPublicKeyParameters dhPubKey =
                        new Org.BouncyCastle.Crypto.Parameters.DHPublicKeyParameters(
                            new Org.BouncyCastle.Math.BigInteger(1, serverKey),
                            dhParams);

                    secretKey = Link.keyAgreement.CalculateAgreement(dhPubKey).ToByteArray();

                    Logger.Log("DH SecretKey:");
                    Logger.LogHex(secretKey, 0, 128);

                    InitRC4Encryption(
                        secretKey,
                        serversig, dhposServer,
                        clientsig, 1 + dhposClient,
                        out keyIn, out keyOut);
                }

                clientResp = new byte[RTMP_SIG_SIZE];
                for (int i = 0; i < RTMP_SIG_SIZE; i++) clientResp[i] = (byte)(rand.Next(0, 256));

                // calculate response now
                byte[] signatureResp = new byte[SHA256_DIGEST_LENGTH];
                byte[] digestResp = new byte[SHA256_DIGEST_LENGTH];

                HMACsha256(serversig, digestPosServer, SHA256_DIGEST_LENGTH, GenuineFPKey, GenuineFPKey.Length, digestResp, 0);
                HMACsha256(clientResp, 0, RTMP_SIG_SIZE - SHA256_DIGEST_LENGTH, digestResp, SHA256_DIGEST_LENGTH, signatureResp, 0);

                // some info output
                Logger.Log("Calculated digest key from secure key and server digest: ");
                Logger.LogHex(digestResp, 0, SHA256_DIGEST_LENGTH);

                // FP10 stuff
                if (type == 8)
                {
                    /* encrypt signatureResp */
                    for (int i = 0; i < SHA256_DIGEST_LENGTH; i += 8)
                        rtmpe8_sig(signatureResp, i, digestResp[i] % 15);
                }

                Logger.Log("Client signature calculated:");
                Logger.LogHex(signatureResp, 0, SHA256_DIGEST_LENGTH);

                Array.Copy(signatureResp, 0, clientResp, RTMP_SIG_SIZE - SHA256_DIGEST_LENGTH, SHA256_DIGEST_LENGTH);
            }
            else
            {
                clientResp = serversig;
            }

            WriteN(clientResp, 0, RTMP_SIG_SIZE);

            // 2nd part of handshake
            byte[] resp = new byte[RTMP_SIG_SIZE];
            if (ReadN(resp, 0, RTMP_SIG_SIZE) != RTMP_SIG_SIZE) return false;

            if (FP9HandShake)
            {
                if (resp[4] == 0 && resp[5] == 0 && resp[6] == 0 && resp[7] == 0)
                {
                    Logger.Log("Wait, did the server just refuse signed authentication?");
                }

                // verify server response
                int digestPosClient = (int)GetDigestOffset1(clientsig, 1, RTMP_SIG_SIZE);

                byte[] signature = new byte[SHA256_DIGEST_LENGTH];
                byte[] digest = new byte[SHA256_DIGEST_LENGTH];

                Logger.Log(string.Format("Client signature digest position: {0}", digestPosClient));                
                HMACsha256(clientsig, 1 + digestPosClient, SHA256_DIGEST_LENGTH, GenuineFMSKey, GenuineFMSKey.Length, digest, 0);
                HMACsha256(resp,0, RTMP_SIG_SIZE - SHA256_DIGEST_LENGTH, digest, SHA256_DIGEST_LENGTH, signature, 0);

                // show some information
                Logger.Log("Digest key: ");
                Logger.LogHex(digest, 0, SHA256_DIGEST_LENGTH);

                // FP10 stuff
                if (type == 8)
                {
                    /* encrypt signatureResp */
                    for (int i = 0; i < SHA256_DIGEST_LENGTH; i += 8)
                        rtmpe8_sig(signature, i, digest[i] % 15);
                }

                Logger.Log("Signature calculated:");
                Logger.LogHex(signature, 0, SHA256_DIGEST_LENGTH);

                Logger.Log("Server sent signature:");
                Logger.LogHex(resp, RTMP_SIG_SIZE - SHA256_DIGEST_LENGTH, SHA256_DIGEST_LENGTH);

                for (int i = 0; i < SHA256_DIGEST_LENGTH; i++)
                    if (signature[i] != resp[RTMP_SIG_SIZE - SHA256_DIGEST_LENGTH + i])
                    {
                        Logger.Log("Server not genuine Adobe!");
                        return false;
                    }
                Logger.Log("Genuine Adobe Flash Media Server");

                if (encrypted)
                {
                    // set keys for encryption from now on
                    Link.rc4In = new Org.BouncyCastle.Crypto.Engines.RC4Engine();
                    Link.rc4In.Init(false, new Org.BouncyCastle.Crypto.Parameters.KeyParameter(keyIn));

                    Link.rc4Out = new Org.BouncyCastle.Crypto.Engines.RC4Engine();
                    Link.rc4Out.Init(true, new Org.BouncyCastle.Crypto.Parameters.KeyParameter(keyOut));

                    // update 'encoder / decoder state' for the RC4 keys
                    // both parties *pretend* as if handshake part 2 (1536 bytes) was encrypted
                    // effectively this hides / discards the first few bytes of encrypted session
                    // which is known to increase the secure-ness of RC4
                    // RC4 state is just a function of number of bytes processed so far
                    // that's why we just run 1536 arbitrary bytes through the keys below
                    byte[] dummyBytes = new byte[RTMP_SIG_SIZE];
                    Link.rc4In.ProcessBytes(dummyBytes, 0, RTMP_SIG_SIZE, new byte[RTMP_SIG_SIZE], 0);
                    Link.rc4Out.ProcessBytes(dummyBytes, 0, RTMP_SIG_SIZE, new byte[RTMP_SIG_SIZE], 0);
                }                
            }
            else
            {
                for (int i = 0; i < RTMP_SIG_SIZE; i++)
                    if (resp[i] != clientsig[i + 1])
                    {
                        Logger.Log("client signature does not match!");
                        return false;
                    }
            }            

            Logger.Log("Handshaking finished....");
            return true;
        }

        uint GetDHOffset1(byte[] handshake, int bufferoffset, uint len)
        {
            int offset = 0;
            bufferoffset += 1532;

            offset += handshake[bufferoffset]; bufferoffset++;
            offset += handshake[bufferoffset]; bufferoffset++;
            offset += handshake[bufferoffset]; bufferoffset++;
            offset += handshake[bufferoffset];// (*ptr);

            int res = (offset % 632) + 772;

            if (res + 128 > 1531)
            {
                Logger.Log(string.Format("Couldn't calculate DH offset (got {0}), exiting!", res));
                throw new Exception();
            }

            return (uint)res;
        }

        uint GetDigestOffset1(byte[] handshake, int bufferoffset, uint len)
        {
            int offset = 0;
            bufferoffset += 8;

            offset += handshake[bufferoffset]; bufferoffset++;
            offset += handshake[bufferoffset]; bufferoffset++;
            offset += handshake[bufferoffset]; bufferoffset++;
            offset += handshake[bufferoffset];

            int res = (offset % 728) + 12;

            if (res + 32 > 771)
            {
                Logger.Log(string.Format("Couldn't calculate digest offset (got {0}), exiting!", res));
                throw new Exception();
            }

            return (uint)res;
        }

        uint GetDHOffset2(byte[] handshake, int bufferoffset, uint len)
        {
            uint offset = 0;
            bufferoffset += 768;
            //assert(RTMP_SIG_SIZE <= len);

            offset += handshake[bufferoffset]; bufferoffset++;
            offset += handshake[bufferoffset]; bufferoffset++;
            offset += handshake[bufferoffset]; bufferoffset++;
            offset += handshake[bufferoffset];

            uint res = (offset % 632) + 8;

            if (res + 128 > 767)
            {
                Logger.Log(string.Format("Couldn't calculate correct DH offset (got {0}), exiting!", res));
                throw new Exception();
            }
            return res;
        }

        uint GetDigestOffset2(byte[] handshake, int bufferoffset, uint len)
        {
            uint offset = 0;
            bufferoffset += 772;
            //assert(12 <= len);

            offset += handshake[bufferoffset]; bufferoffset++;
            offset += handshake[bufferoffset]; bufferoffset++;
            offset += handshake[bufferoffset]; bufferoffset++;
            offset += handshake[bufferoffset];// (*ptr);

            uint res = (offset % 728) + 776;

            if (res + 32 > 1535)
            {
                Logger.Log(string.Format("Couldn't calculate correct digest offset (got {0}), exiting", res));
                throw new Exception();
            }
            return res;
        }

        void CalculateDigest(int digestPos, byte[] handshakeMessage, int handshakeOffset, byte[] key, int keyLen, byte[] digest, int digestOffset)
        {
	        const int messageLen = RTMP_SIG_SIZE - SHA256_DIGEST_LENGTH;
            byte[] message = new byte[messageLen];

            Array.Copy(handshakeMessage, handshakeOffset, message, 0, digestPos);
            Array.Copy(handshakeMessage, handshakeOffset + digestPos + SHA256_DIGEST_LENGTH, message, digestPos, messageLen - digestPos);

            HMACsha256(message, 0, messageLen, key, keyLen, digest, digestOffset);
        }

        bool VerifyDigest(int digestPos, byte[] handshakeMessage, byte[] key, int keyLen)
        {
            byte[] calcDigest = new byte[SHA256_DIGEST_LENGTH];

	        CalculateDigest(digestPos, handshakeMessage, 0, key, keyLen, calcDigest, 0);

            for (int i = 0; i < SHA256_DIGEST_LENGTH; i++)
            {
                if (handshakeMessage[digestPos + i] != calcDigest[i]) return false;
            }
            return true;            	        
        }

        void HMACsha256(byte[] message, int messageOffset, int messageLen, byte[] key, int keylen, byte[] digest, int digestOffset)
        {
            System.Security.Cryptography.HMAC hmac = System.Security.Cryptography.HMACSHA256.Create("HMACSHA256");
            byte[] actualKey = new byte[keylen]; Array.Copy(key, actualKey, keylen);                       
            hmac.Key = actualKey;

            byte[] actualMessage = new byte[messageLen];
            Array.Copy(message, messageOffset, actualMessage, 0, messageLen);
            
            byte[] calcDigest = hmac.ComputeHash(actualMessage);
            Array.Copy(calcDigest, 0, digest, digestOffset, calcDigest.Length);
        }

        void InitRC4Encryption(byte[] secretKey, byte[] pubKeyIn, int inOffset, byte[] pubKeyOut, int outOffset, out byte[] rc4keyIn, out byte[] rc4keyOut)
        {
            byte[] digest = new byte[SHA256_DIGEST_LENGTH];            

            System.Security.Cryptography.HMAC hmac = System.Security.Cryptography.HMACSHA256.Create("HMACSHA256");            
            hmac.Key = secretKey;

            byte[] actualpubKeyIn = new byte[128];
            Array.Copy(pubKeyIn, inOffset, actualpubKeyIn, 0, 128);
            digest = hmac.ComputeHash(actualpubKeyIn);
            
            rc4keyOut = new byte[16];
            Array.Copy(digest, rc4keyOut, 16);
            Logger.Log("RC4 Out Key: ");
            Logger.LogHex(rc4keyOut, 0, 16);

            hmac = System.Security.Cryptography.HMACSHA256.Create("HMACSHA256");
            hmac.Key = secretKey;
            
            byte[] actualpubKeyOut = new byte[128];
            Array.Copy(pubKeyOut, outOffset, actualpubKeyOut, 0, 128);
            digest = hmac.ComputeHash(actualpubKeyOut);

            rc4keyIn = new byte[16];
            Array.Copy(digest, rc4keyIn, 16);
            Logger.Log("RC4 In Key: ");
            Logger.LogHex(rc4keyIn, 0, 16);
        }

        /// <summary>
        /// RTMPE type 8 uses XTEA on the regular signature (http://en.wikipedia.org/wiki/XTEA)
        /// todo : little or big endian?, might have to reverse bytes on BitConverter calls
        /// </summary>
        /// <param name="?"></param>
        static void rtmpe8_sig(byte[] array, int offset, int keyid)
        {
            uint i, num_rounds = 32;
            uint v0, v1, sum = 0, delta = 0x9E3779B9;
            uint[] k;

            v0 = BitConverter.ToUInt32(array, offset);
            v1 = BitConverter.ToUInt32(array, offset + 4);

            k = rtmpe8_keys[keyid];

            for (i = 0; i < num_rounds; i++)
            {
                v0 += (((v1 << 4) ^ (v1 >> 5)) + v1) ^ (sum + k[sum & 3]);
                sum += delta;
                v1 += (((v0 << 4) ^ (v0 >> 5)) + v0) ^ (sum + k[(sum >> 11) & 3]);
            }

            Array.Copy(BitConverter.GetBytes(v0), 0, array, offset, 4);
            Array.Copy(BitConverter.GetBytes(v1), 0, array, offset + 4, 4);
        }

        #endregion        

        int ReadN(byte[] buffer, int offset, int size)
        {            
            // wait (max) 5 seconds until data is available
            int i = 50;
            while (tcpClient.Available < size && i > 0)
            {
                i--;
                System.Threading.Thread.Sleep(100);
            }
            if (tcpClient.Available < size) 
                return 0;// throw new Exception(string.Format("No Data Available (trying to read {0} byte)", size));

            byte[] data = new byte[size];
            int read = networkStream.Read(data, 0, size);

            // decrypt if needed
            if (read > 0 && Link.rc4In != null)
            {
                Link.rc4In.ProcessBytes(data, 0, size, buffer, offset);
            }
            else
            {
                Array.Copy(data, 0, buffer, offset, size);
            }

            bytesReadTotal += read;
            
            if (bytesReadTotal > lastSentBytesRead + (m_nClientBW / 2)) SendBytesReceived(); // report bytes read

            return read;
        }        

        void WriteN(byte[] buffer, int offset, int size)
        {            
            // encrypt if needed
            if (Link.rc4Out != null)
            {
                byte[] result = new byte[size];
                Link.rc4Out.ProcessBytes(buffer, offset, size, result, 0);
                networkStream.Write(result, 0, size);                
            }
            else
            {
                networkStream.Write(buffer, offset, size);
            }
        }        
    }
}

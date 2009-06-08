using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RTMP_LIB
{
    public class RTMP
    {
        const int RTMP_PACKET_SIZE_LARGE   = 0;
        const int RTMP_PACKET_SIZE_MEDIUM  = 1;
        const int RTMP_PACKET_SIZE_SMALL   = 2;
        const int RTMP_PACKET_SIZE_MINIMUM = 3;
        const int RTMP_LARGE_HEADER_SIZE = 12;
        const int RTMP_SIG_SIZE = 1536;
        static readonly uint[] packetSize = { 12, 8, 4, 1 };

        string m_strLink = "";
        string m_strPlayer = "";
        string m_strPageUrl = "";
        string m_strPlayPath = "";

        int bytesReadTotal = 0;
        int lastSentBytesRead = 0;

        int m_nBufferMS = 300;
        int m_chunkSize = 128;
        int m_stream_id; // returned in _result from invoking createStream    
        double m_fDuration; // duration of stream in seconds returned by Metadata
        int m_nBWCheckCounter;
        public bool m_bPlaying = false;
        RTMPPacket[] m_vecChannelsIn = new RTMPPacket[64];
        RTMPPacket[] m_vecChannelsOut = new RTMPPacket[64];
        Stack<string> m_methodCalls = new Stack<string>(); //remote method calls queue

        TcpClient tcpClient = null;
        NetworkStream stream = null;

        public bool Connect(string rtmpUrl, string tcUrl, string swfUrl, string pageUrl, string app, string playPath)
        {
            // close any previous connection
            Close();

            // set local variables
            m_strLink = rtmpUrl;
            m_strPlayer = swfUrl;
            m_strPageUrl = pageUrl;
            m_strPlayPath = playPath;

            // connect            
            tcpClient = new TcpClient(new Uri(m_strLink).Host, 1935);
            stream = tcpClient.GetStream();

            if (!HandShake()) return false;
            if (!SendConnectPacket(app, tcUrl)) return false;

            return true;
        }

        public bool Connect(string rtmpUrl)
        {
            return Connect(rtmpUrl, "", "", "", "", "");
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
                    packet.FreePacket();
                    continue;
                }

                switch (packet.m_packetType)
                {
                    case 0x01:
                        // chunk size
                        HandleChangeChunkSize(packet);
                        break;
                    case 0x03:
                        // bytes read report
                        //CLog::Log(LOGDEBUG,"%s, received: bytes read report", __FUNCTION__);
                        break;
                    case 0x04:
                        // ping
                        HandlePing(packet);
                        break;
                    case 0x05:
                        // server bw
                        //CLog::Log(LOGDEBUG,"%s, received: server BW", __FUNCTION__);
                        break;
                    case 0x06:
                        // client bw
                        //CLog::Log(LOGDEBUG,"%s, received: client BW", __FUNCTION__);
                        break;
                    case 0x08:
                        // audio data
                        //CLog::Log(LOGDEBUG,"%s, received: audio %lu bytes", __FUNCTION__, packet.m_nBodySize);
                        //HandleAudio(packet);
                        bHasMediaPacket = true;
                        break;
                    case 0x09:
                        // video data
                        //CLog::Log(LOGDEBUG,"%s, received: video %lu bytes", __FUNCTION__, packet.m_nBodySize);
                        //HandleVideo(packet);
                        bHasMediaPacket = true;
                        break;
                    case 0x12:
                        // metadata (notify)
                        //CLog::Log(LOGDEBUG,"%s, received: notify %lu bytes", __FUNCTION__, packet.m_nBodySize);
                        HandleMetadata(packet);
                        bHasMediaPacket = true;
                        break;
                    case 0x14:
                        // invoke
                        //CLog::Log(LOGDEBUG,"%s, received: invoke %lu bytes", __FUNCTION__, packet.m_nBodySize);
                        HandleInvoke(packet);
                        break;
                    case 0x16:
                        // FLV tag(s)
                        //CLog::Log(LOGDEBUG,"%s, received: FLV tag(s) %lu bytes", __FUNCTION__, packet.m_nBodySize);
                        bHasMediaPacket = true;
                        break;
                    default:
                        //CLog::Log(LOGERROR,"%s, unknown packet type received: 0x%02x", __FUNCTION__, packet.m_packetType);
                        break;
                }
                if (!bHasMediaPacket) packet.FreePacket();
            }
            if (bHasMediaPacket) m_bPlaying = true;
            return bHasMediaPacket;
        }

        bool HandShake()
        {
            byte[] clientsig = new byte[RTMP_SIG_SIZE + 1];
            byte[] serversig = new byte[RTMP_SIG_SIZE];
            clientsig[0] = 0x3;
            int uptime = System.Environment.TickCount;
            byte[] uptime_bytes = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(uptime));
            Array.Copy(uptime_bytes, 0, clientsig, 1, uptime_bytes.Length);
            for (int i = 5; i <= 8; i++) clientsig[i] = 0;
            int magic = uptime/* % 256*/;
            int bytes = 9;
            while (bytes < RTMP_SIG_SIZE)
            {
                magic = (1211121 * magic + 1) % 256;
                clientsig[bytes] = (byte)magic;
                clientsig[bytes + 1] = 0;
                bytes += 2;
            }
            stream.Write(clientsig, 0, RTMP_SIG_SIZE + 1);
            int dummy = stream.ReadByte();// 0x03
            if (dummy != 0x03)
                return false;
            while (tcpClient.Available < RTMP_SIG_SIZE) System.Threading.Thread.Sleep(10);            

            if (stream.Read(serversig, 0, RTMP_SIG_SIZE) != RTMP_SIG_SIZE)
                return false;
            byte[] resp = new byte[RTMP_SIG_SIZE];

            while (tcpClient.Available < RTMP_SIG_SIZE) System.Threading.Thread.Sleep(10);            

            if (stream.Read(resp, 0, RTMP_SIG_SIZE) != RTMP_SIG_SIZE)
                return false;
            for (int i = 0; i < RTMP_SIG_SIZE; i++) 
                if (resp[i] != clientsig[i + 1])
                    return false; //client signature does not match!
            stream.Write(serversig, 0, RTMP_SIG_SIZE); // send server signature back to finish handshake
            return true;
        }

        bool ReadPacket(out RTMPPacket packet)
        {
            packet = new RTMPPacket();

            do
            {
                while (tcpClient.Available < 1) System.Threading.Thread.Sleep(10);

                byte type = (byte)stream.ReadByte(); bytesReadTotal++;
                /*{
                  CLog::Log(LOGERROR, "%s, failed to read RTMP packet header", __FUNCTION__);
                  return false;
                }*/

                packet.m_headerType = (byte)((type & 0xc0) >> 6);
                packet.m_nChannel = (byte)(type & 0x3f);

                uint nSize = packetSize[packet.m_headerType];

                //  CLog::Log(LOGDEBUG, "%s, reading RTMP packet chunk on channel %x, headersz %i", __FUNCTION__, packet.m_nChannel, nSize);

                if (nSize < RTMP_LARGE_HEADER_SIZE) // using values from the last message of this channel
                    packet = m_vecChannelsIn[packet.m_nChannel];

                nSize--;

                byte[] header = new byte[RTMP_LARGE_HEADER_SIZE];
                if (nSize > 0 && stream.Read(header, 0, (int)nSize) != nSize)
                {
                    //CLog::Log(LOGERROR, "%s, failed to read RTMP packet header. type: %x", __FUNCTION__, (unsigned int)type);
                    return false;
                }
                bytesReadTotal += (int)nSize;

                if (nSize >= 3)
                    packet.m_nInfoField1 = ReadInt24(header, 0);

                if (nSize >= 6)
                {
                    packet.m_nBodySize = (uint)ReadInt24(header, 3);
                    packet.m_nBytesRead = 0;
                    packet.FreePacketHeader(); // new packet body
                }

                if (nSize > 6)
                    packet.m_packetType = header[6];

                if (nSize == 11)
                    packet.m_nInfoField2 = ReadInt32LE(header, 7);

                if (packet.m_nBodySize >= 0 && packet.m_body == null && !packet.AllocPacket((int)packet.m_nBodySize))
                {
                    //CLog::Log(LOGDEBUG,"%s, failed to allocate packet", __FUNCTION__);
                    return false;
                }

                uint nToRead = packet.m_nBodySize - packet.m_nBytesRead;
                uint nChunk = (uint)m_chunkSize;
                if (nToRead < nChunk)
                    nChunk = nToRead;

                while (tcpClient.Available < nChunk) System.Threading.Thread.Sleep(10);

                int read = stream.Read(packet.m_body, (int)packet.m_nBytesRead, (int)nChunk);
                if (read != nChunk)
                {
                    //CLog::Log(LOGERROR, "%s, failed to read RTMP packet body. len: %lu", __FUNCTION__, packet.m_nBodySize);
                    packet.m_body = null; // we dont want it deleted since its pointed to from the stored packets (m_vecChannelsIn)
                    return false;
                }
                bytesReadTotal += (int)read;

                packet.m_nBytesRead += nChunk;

                // keep the packet as ref for other packets on this channel
                m_vecChannelsIn[packet.m_nChannel] = packet;

                /*
                if (packet.IsReady())
                {
                    // reset the data from the stored packet. we keep the header since we may use it later if a new packet for this channel
                    // arrives and requests to re-use some info (small packet header)
                    //m_vecChannelsIn[packet.m_nChannel].m_body = null;
                    //m_vecChannelsIn[packet.m_nChannel].m_nBytesRead = 0;
                }
                else
                    packet.m_body = null; // so it wont be erased on "free"
                */

                if (bytesReadTotal > lastSentBytesRead + (600 * 1024)) SendBytesReceived(); // report every 600K

            } while (!packet.IsReady());
            return true;
        }

        void Close()
        {
            if (stream != null) stream.Close(1000);
            if (tcpClient != null && tcpClient.Connected) tcpClient.Close();

            stream = null;
            tcpClient = null;

            m_chunkSize = 128;
            m_nBWCheckCounter = 0;
            bytesReadTotal = 0;
            lastSentBytesRead = 0;

            for (int i = 0; i < 64; i++)
            {
                m_vecChannelsIn[i] = null;
                m_vecChannelsOut[i] = null;
            }

            m_bPlaying = false;
        }

        #region Send Client Packets

        bool SendConnectPacket(string app, string tcUrl)
        {
            Uri url = new Uri(m_strLink);
            if (string.IsNullOrEmpty(app)) app = url.AbsolutePath.TrimStart(new char[] { '/' });//.GetFileName();

            int slistPos = app.IndexOf("slist=");
            if (slistPos == -1)
            {
                // no slist parameter. send the path as the app
                // if URL path contains a slash, use the part up to that as the app
                // as we'll send the part after the slash as the thing to play
                int pos_slash = app.LastIndexOf("/");
                if (pos_slash != -1) app = app.Substring(0, pos_slash + 1);
            }

            if (string.IsNullOrEmpty(tcUrl)) tcUrl = url.GetLeftPart(UriPartial.Authority) + app;// .GetURLWithoutFilename(tcURL);

            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x03;   // control channel (invoke)
            packet.m_headerType = RTMP_PACKET_SIZE_LARGE;
            packet.m_packetType = 0x14; // INVOKE
            packet.AllocPacket(4096);

            List<byte> enc = new List<byte>();
            EncodeString(enc, "connect");
            EncodeNumber(enc, 1.0);
            enc.Add(0x03); //Object Datatype            
            EncodeString(enc, "app", app);
            EncodeString(enc, "flashVer", "WIN 10,0,22,87");
            EncodeString(enc, "swfUrl", m_strPlayer);
            EncodeString(enc, "tcUrl", tcUrl);
            EncodeBoolean(enc, "fpad", false);
            EncodeNumber(enc, "capabilities", 15.0);
            EncodeNumber(enc, "audioCodecs", 3191.0);
            EncodeNumber(enc, "videoCodecs", 252.0);
            EncodeNumber(enc, "videoFunction", 1.0);
            EncodeString(enc, "pageUrl", m_strPageUrl);
            enc.Add(0); enc.Add(0); enc.Add(0x09); // end of object - 0x00 0x00 0x09

            Array.Copy(enc.ToArray(), packet.m_body, enc.Count);
            packet.m_nBodySize = (uint)enc.Count; // todo : hier muss 0 1 2 oder 3 stehen

            return SendRTMP(packet);
        }

        bool SendPlay()
        {            
            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x08;   // we make 8 our stream channel
            packet.m_headerType = RTMP_PACKET_SIZE_LARGE;
            packet.m_packetType = 0x14; // INVOKE
            packet.m_nInfoField2 = m_stream_id;

            packet.AllocPacket(256); // should be enough
            List<byte> enc = new List<byte>();

            EncodeString(enc, "play");
            EncodeNumber(enc, 0.0);
            enc.Add(0x05); // NULL  

            // use m_strPlayPath
            string strPlay = m_strPlayPath;
            if (strPlay == string.Empty)
            {
                Uri url = new Uri(m_strLink);
                // or use slist parameter, if there is one
                int nPos = url.AbsolutePath.IndexOf("slist=");
                if (nPos > 0)
                    strPlay = url.AbsolutePath.Substring(nPos, 6);

                if (strPlay == string.Empty)
                {
                    // or use last piece of URL, if there's more than one level
                    int pos_slash = url.AbsolutePath.LastIndexOf("/");
                    if (pos_slash != -1)
                        strPlay = url.AbsolutePath.Substring(pos_slash + 1);
                    if (strPlay.EndsWith(".flv")) strPlay = strPlay.Substring(0, strPlay.Length - 4);
                }

                if (strPlay == string.Empty)
                {
                    //CLog::Log(LOGERROR,"%s, no name to play!", __FUNCTION__);
                    return false;
                }
            }

            //CLog::Log(LOGDEBUG,"%s, invoking play '%s'", __FUNCTION__, strPlay.c_str() );

            EncodeString(enc, strPlay);
            //EncodeNumber(enc, 0.0); - not needed from looking at the streams

            packet.m_body = enc.ToArray();
            packet.m_nBodySize = (uint)enc.Count;


            return SendRTMP(packet);
        }

        bool SendPing(short nType, uint nObject, uint nTime)
        {
            Logger.Log(string.Format("sending ping. type: {0}", nType));

            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x02;   // control channel (ping)
            packet.m_headerType = RTMP_PACKET_SIZE_MEDIUM;
            packet.m_packetType = 0x04; // ping
            packet.m_nInfoField1 = System.Environment.TickCount;

            int nSize = (nType == 0x03 ? 10 : 6); // type 3 is the buffer time and requires all 3 parameters. all in all 10 bytes.
            packet.AllocPacket(nSize);
            packet.m_nBodySize = (uint)nSize;

            List<byte> buf = new List<byte>();
            EncodeInt16(buf, nType);

            if (nSize > 2)
                EncodeInt32(buf, (int)nObject);

            if (nSize > 6)
                EncodeInt32(buf, (int)nTime);

            packet.m_body = buf.ToArray();
            return SendRTMP(packet);
        }

        bool SendCheckBWResult()
        {
            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x03;   // control channel (invoke)
            packet.m_headerType = RTMP_PACKET_SIZE_MEDIUM;
            packet.m_packetType = 0x14; // INVOKE
            packet.m_nInfoField1 = 0x16 * m_nBWCheckCounter; // temp inc value. till we figure it out.

            packet.AllocPacket(256); // should be enough
            List<byte> enc = new List<byte>();
            EncodeString(enc, "_result");
            EncodeNumber(enc, (double)DateTime.Now.Ticks); // temp
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
            packet.m_headerType = RTMP_PACKET_SIZE_MEDIUM;
            packet.m_packetType = 0x03; // bytes in

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
            packet.m_headerType = RTMP_PACKET_SIZE_LARGE;
            packet.m_packetType = 0x05; // Server BW

            packet.AllocPacket(4);
            packet.m_nBodySize = 4;

            List<byte> bytesToSend = new List<byte>();
            EncodeInt32(bytesToSend, 0x001312d0); // hard coded for now
            packet.m_body = bytesToSend.ToArray();
            return SendRTMP(packet);
        }

        bool SendCreateStream(double dStreamId)
        {
            RTMPPacket packet = new RTMPPacket();
            packet.m_nChannel = 0x03;   // control channel (invoke)
            packet.m_headerType = RTMP_PACKET_SIZE_MEDIUM;
            packet.m_packetType = 0x14; // INVOKE

            packet.AllocPacket(256); // should be enough
            List<byte> enc = new List<byte>();
            EncodeString(enc, "createStream");
            EncodeNumber(enc, dStreamId);
            enc.Add(0x05); // NULL

            packet.m_nBodySize = (uint)enc.Count;
            packet.m_body = enc.ToArray();

            return SendRTMP(packet);
        }

        bool SendRTMP(RTMPPacket packet)
        {
            RTMPPacket prevPacket = m_vecChannelsOut[packet.m_nChannel];
            if (packet.m_headerType != RTMP_PACKET_SIZE_LARGE)
            {
                // compress a bit by using the prev packet's attributes
                if (prevPacket.m_nBodySize == packet.m_nBodySize && packet.m_headerType == RTMP_PACKET_SIZE_MEDIUM)
                    packet.m_headerType = RTMP_PACKET_SIZE_SMALL;

                if (prevPacket.m_nInfoField2 == packet.m_nInfoField2 && packet.m_headerType == RTMP_PACKET_SIZE_SMALL)
                    packet.m_headerType = RTMP_PACKET_SIZE_MINIMUM;
            }

            if (packet.m_headerType > 3) // sanity
            {
                Logger.Log(string.Format("sanity failed!! tring to send header of type: {0}.", packet.m_headerType));
                return false;
            }

            uint nSize = packetSize[packet.m_headerType];
            List<byte> header = new List<byte>();//byte[RTMP_LARGE_HEADER_SIZE];
            header.Add((byte)((packet.m_headerType << 6) | packet.m_nChannel));
            if (nSize > 1)
                EncodeInt24(header, packet.m_nInfoField1);

            if (nSize > 4)
            {
                EncodeInt24(header, (int)packet.m_nBodySize);
                header.Add(packet.m_packetType);
            }

            if (nSize > 8)
                EncodeInt32LE(header, packet.m_nInfoField2);

            stream.Write(header.ToArray(), 0, (int)nSize);

            nSize = packet.m_nBodySize;
            byte[] buffer = packet.m_body;
            uint bufferOffset = 0;
            while (nSize > 0)
            {
                uint nChunkSize = packet.m_packetType == 0x14 ? (uint)m_chunkSize : packet.m_nBodySize;
                if (nSize < m_chunkSize)
                    nChunkSize = nSize;

                stream.Write(buffer, (int)bufferOffset, (int)nChunkSize);

                nSize -= nChunkSize;
                bufferOffset += nChunkSize;

                if (nSize > 0)
                {
                    byte sep = (byte)(0xc0 | packet.m_nChannel);
                    stream.WriteByte(sep);
                }
            }

            if (packet.m_packetType == 0x14) // we invoked a remote method, keep it in call queue till result arrives
                m_methodCalls.Push(ReadString(packet.m_body, 1));

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
                m_chunkSize = ReadInt32(packet.m_body, 0);
                Logger.Log(string.Format("received: chunk size change to {0}", m_chunkSize));
            }
        }

        void HandlePing(RTMPPacket packet)
        {
            short nType = -1;
            if (packet.m_body != null && packet.m_nBodySize >= 2)
                nType = ReadInt16(packet.m_body, 0);
            
            Logger.Log(string.Format("received: ping, type: {0}", nType));

            if (nType == 0x06 && packet.m_nBodySize >= 6) // server ping. reply with pong.
            {
                uint nTime = (uint)ReadInt32(packet.m_body, 2);
                SendPing(0x07, nTime, 0);
            }
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
            Logger.Log(string.Format("server invoking <{0}>", method));

            if (method == "_result")
            {
                string methodInvoked = m_methodCalls.Pop();                

                Logger.Log(string.Format("received result for method call <{0}>", methodInvoked));

                if (methodInvoked == "connect")
                {
                    SendServerBW();
                    SendPing(3, 0, 300);
                    SendCreateStream(2.0d);
                }
                else if (methodInvoked == "createStream")
                {
                    m_stream_id = (int)obj.GetProperty(3).GetNumber();
                    SendPlay();
                    SendPing(3, 1, (uint)m_nBufferMS);
                }
                else if (methodInvoked == "play")
                {
                }
            }
            else if (method == "onBWDone")
            {
                //SendCheckBW();
            }
            else if (method == "_onbwcheck")
            {
                SendCheckBWResult();
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
            AMFObject obj = new AMFObject();
            int nRes = obj.Decode(packet.m_body, 0, (int)packet.m_nBodySize, false);
            if (nRes < 0)
            {
                //Log(LOGERROR, "%s, error decoding meta data packet", __FUNCTION__);
                return;
            }

            obj.Dump();
            string metastring = obj.GetProperty(0).GetString();

            if (metastring == "onMetaData")
            {
                AMFObjectProperty prop = null;
                if (obj.FindFirstMatchingProperty("duration", ref prop))
                {
                    m_fDuration = prop.GetNumber();
                    Logger.Log(string.Format("Set duration: {0}", m_fDuration));
                }
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
            short length = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, offset));
            if (length > 0) strRes = Encoding.ASCII.GetString(data, offset + 2, length);
            return strRes;
        }

        public static short ReadInt16(byte[] data, int offset)
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, offset));
        }

        public static int ReadInt24(byte[] data, int offset)
        {
            byte[] number = new byte[4];
            Array.Copy(data, offset, number, 1, 3);
            int result = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(number, 0));
            return result;
        }

        /// <summary>
        /// big-endian 32bit integer
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static int ReadInt32(byte[] data, int offset)
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data, offset));
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
    }
}

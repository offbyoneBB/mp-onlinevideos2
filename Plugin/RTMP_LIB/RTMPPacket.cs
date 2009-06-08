using System;

namespace RTMP_LIB
{
    public class RTMPPacket
    {
        const int RTMP_PACKET_TYPE_AUDIO = 0x08;
        const int RTMP_PACKET_TYPE_VIDEO = 0x09;
        const int RTMP_PACKET_TYPE_INFO = 0x12;

        const int RTMP_MAX_HEADER_SIZE = 12;

        public byte m_headerType;
        public byte m_packetType;
        public byte m_nChannel;
        public int m_nInfoField1; // first 3 bytes
        public int m_nInfoField2; // last  4 bytes in a long header        
        public uint m_nBytesRead;
        public uint m_nInternalTimestamp;
        public uint m_nBodySize;
        public byte[] m_body;

        public bool IsReady() { return m_nBytesRead == m_nBodySize; }

        public RTMPPacket()
        {
            Reset();
        }

        ~RTMPPacket()
        {
            FreePacket();
        }

        public void Reset()
        {
            m_headerType = 0;
            m_packetType = 0;
            m_nChannel = 0;
            m_nInfoField1 = 0;
            m_nInfoField2 = 0;
            m_nBodySize = 0;
            m_nBytesRead = 0;
            m_nInternalTimestamp = 0;
            m_body = null;
        }

        public bool AllocPacket(int nSize)
        {
            m_body = new byte[nSize];
            m_nBytesRead = 0;
            return true;
        }

        public void FreePacket()
        {
            FreePacketHeader();
            Reset();
        }

        public void FreePacketHeader()
        {
            m_body = null;
        }

        public void Dump()
        {
            Logger.Log(string.Format("RTMP PACKET: packet type: 0x%02x. channel: 0x%02x. info 1: %d info 2: %d. Body size: %lu. body: 0x%02x", m_packetType, m_nChannel, m_nInfoField1, m_nInfoField2, m_nBodySize, m_body != null ? m_body[0].ToString() : "0"));
        }

    }
}


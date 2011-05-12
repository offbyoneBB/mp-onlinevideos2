using System;

namespace RTMP_LIB
{
    public class RTMPPacket
    {
        public HeaderType HeaderType;
        public PacketType PacketType;
        public bool m_hasAbsTimestamp; // timestamp absolute or relative?
        public int m_nChannel;
        public uint m_nTimeStamp; // timestamp 
        public int m_nInfoField2; // last  4 bytes in a long header        
        public uint m_nBodySize;
        public uint m_nBytesRead;
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
            HeaderType = 0;
            PacketType = 0;
            m_nChannel = 0;
            m_nTimeStamp = 0;
            m_nInfoField2 = 0;
            m_nBodySize = 0;
            m_nBytesRead = 0;
            m_hasAbsTimestamp = false;            
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
            Free();
            Reset();
        }

        public void Free()
        {
            m_body = null;
        }

        public void Dump()
        {
            Logger.Log(string.Format("RTMP PACKET: packet type: 0x%02x. channel: 0x%02x. info 1: %d info 2: %d. Body size: %lu. body: 0x%02x", PacketType, m_nChannel, m_nTimeStamp, m_nInfoField2, m_nBodySize, m_body != null ? m_body[0].ToString() : "0"));
        }

        public RTMPPacket ShallowCopy()
        {
            return (RTMPPacket)this.MemberwiseClone();
        }

    }
}


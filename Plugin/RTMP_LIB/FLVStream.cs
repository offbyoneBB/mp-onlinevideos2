using System;
using System.Collections.Generic;
using System.IO;

namespace RTMP_LIB
{
    public class FLVStream
    {
        bool headerWritten = false;

        static readonly byte[] flvHeader = new byte[] 
            { 
              Convert.ToByte('F'), Convert.ToByte('L'), Convert.ToByte('V'), 0x01,
              0x05, // video + audio, we finalize later if the value is different
              0x00, 0x00, 0x00, 0x09,
              0x00, 0x00, 0x00, 0x00 // first prevTagSize=0
            };

        public void WriteFLV(RTMP rtmp, Stream stream)
        {
            // rtmp must be connected
            if (!rtmp.IsConnected()) return;

            // we will be writing in chunks, so use our own stream
            MemoryStream ms = new MemoryStream();

            // header
            ms.Write(FLVStream.flvHeader, 0, FLVStream.flvHeader.Length);

            // write stream
            uint timeStammp = 0;
            int nRead = 0;
            int packets = 0;
            int httpChunkSize = 1024 * 10; // first chunk should be big enough so the direct show filter can get all the info and do some buffering
            do
            {
                nRead = WriteStream(rtmp, ms, out timeStammp);
                packets++;
                if (packets > 10 && ms.Length > httpChunkSize)
                {
                    httpChunkSize = 1024; // reduce chunksize
                    Logger.Log("Writing Data to Socket: " + ms.Length.ToString());
                    byte[] buffer = ms.ToArray();
                    stream.Write(buffer, 0, buffer.Length);
                    ms = new MemoryStream();
                }
            }
            while (nRead > -1 && rtmp.m_bPlaying);
        }

        int WriteStream(RTMP rtmp, Stream stream, out uint nTimeStamp)
        {
            nTimeStamp = 0;
            uint prevTagSize = 0;
            RTMPPacket packet;

            if (rtmp.GetNextMediaPacket(out packet))
            {
                // skip video info/command packets
                if (packet.m_packetType == 0x09 && packet.m_nBodySize == 2 && ((packet.m_body[0] & 0xf0) == 0x50))
                {
                    return 0;
                }

                if (packet.m_packetType == 0x09 && packet.m_nBodySize <= 5)
                {
                    //Log(LOGWARNING, "ignoring too small video packet: size: %d", nPacketLen);
                    return 0;
                }
                if (packet.m_packetType == 0x08 && packet.m_nBodySize <= 1)
                {
                    //Log(LOGWARNING, "ignoring too small audio packet: size: %d", nPacketLen);
                    return 0;
                }

                // calculate packet size and reallocate buffer if necessary
                uint size = (uint)(packet.m_nBodySize
                    + ((packet.m_packetType == 0x08 || packet.m_packetType == 0x09 || packet.m_packetType == 0x12) ? 11 : 0)
                    + (packet.m_packetType != 0x16 ? 4 : 0));

                // audio (0x08), video (0x09) or metadata (0x12) packets :
                // construct 11 byte header then add rtmp packet's data
                if (packet.m_packetType == 0x08 || packet.m_packetType == 0x09 || packet.m_packetType == 0x12)
                {
                    // set data type
                    //*dataType |= (((packet.m_packetType == 0x08)<<2)|(packet.m_packetType == 0x09));

                    nTimeStamp = (uint)packet.m_nTimeStamp;
                    prevTagSize = 11 + packet.m_nBodySize;

                    stream.WriteByte(packet.m_packetType);

                    List<byte> somebytes = new List<byte>();
                    RTMP.EncodeInt24(somebytes, (int)packet.m_nBodySize);
                    RTMP.EncodeInt24(somebytes, (int)nTimeStamp);
                    stream.Write(somebytes.ToArray(), 0, somebytes.Count);
                    somebytes.Clear();

                    stream.WriteByte((byte)(((nTimeStamp) & 0xFF000000) >> 24));

                    // stream id
                    RTMP.EncodeInt24(somebytes, 0);
                    stream.Write(somebytes.ToArray(), 0, somebytes.Count);
                    somebytes.Clear();
                }

                // achtung das darf hier noch nicht passieren! erst nach dem if block drunter
                // weil der block drunter die daten ändert die in den stream geschreiben werden!
                //stream.Write(packet.m_body, 0, (int)packet.m_nBodySize); 
                uint len = packet.m_nBodySize;

                // correct tagSize and obtain timestamp if we have an FLV stream
                if (packet.m_packetType == 0x16)
                {
                    uint pos = 0;

                    while (pos + 11 < packet.m_nBodySize)
                    {
                        uint dataSize = (uint)RTMP.ReadInt24(packet.m_body, (int)pos + 1); // size without header (11) and without prevTagSize (4)
                        nTimeStamp = (uint)RTMP.ReadInt24(packet.m_body, (int)pos + 4);
                        nTimeStamp |= (uint)(packet.m_body[pos + 7] << 24);

                        // set data type
                        //*dataType |= (((*(packetBody+pos) == 0x08)<<2)|(*(packetBody+pos) == 0x09));

                        if (pos + 11 + dataSize + 4 > packet.m_nBodySize)
                        {
                            if (pos + 11 + dataSize > packet.m_nBodySize)
                            {
                                //Log(LOGERROR, "Wrong data size (%lu), stream corrupted, aborting!", dataSize);
                                return -2;
                            }
                            //Log(LOGWARNING, "No tagSize found, appending!");

                            // we have to append a last tagSize!
                            prevTagSize = dataSize + 11;

                            List<byte> somemorebytes = new List<byte>();
                            RTMP.EncodeInt32(somemorebytes, (int)prevTagSize);
                            //stream.Write(somemorebytes.ToArray(), 0, somemorebytes.Count);
                            // todo : Append heist wahrscheinlich, dass das Array zu klein ist!
                            Array.Copy(somemorebytes.ToArray(), 0, packet.m_body, pos + 11 + dataSize, somemorebytes.Count); 
                            //RTMP.EncodeInt32(ptr + pos + 11 + dataSize, prevTagSize);                            
                            somemorebytes.Clear();

                            size += 4; len += 4;
                        }
                        else
                        {
                            prevTagSize = (uint)RTMP.ReadInt32(packet.m_body, (int)(pos + 11 + dataSize));

                            if (prevTagSize != (dataSize + 11))
                            {
                                prevTagSize = dataSize + 11;

                                List<byte> somemorebytes = new List<byte>();
                                RTMP.EncodeInt32(somemorebytes, (int)prevTagSize);
                                Array.Copy(somemorebytes.ToArray(), 0, packet.m_body, pos + 11 + dataSize, somemorebytes.Count);
                                //RTMP.EncodeInt32(ptr + pos + 11 + dataSize, prevTagSize);
                                somemorebytes.Clear();
                            }
                        }

                        pos += prevTagSize + 4;//(11+dataSize+4);
                    }
                }

                stream.Write(packet.m_body, 0, (int)packet.m_nBodySize);
                //ptr += len;

                if (packet.m_packetType != 0x16)
                {
                    // FLV tag packets contain their own prevTagSize
                    List<byte> somemorebytes = new List<byte>();
                    RTMP.EncodeInt32(somemorebytes, (int)prevTagSize);
                    stream.Write(somemorebytes.ToArray(), 0, somemorebytes.Count);
                    //RTMP.EncodeInt32(ptr, prevTagSize);
                    //ptr += 4;
                    somemorebytes.Clear();
                }

                return (int)size;
            }

            return -1; // no more media packets
        }
    }
}

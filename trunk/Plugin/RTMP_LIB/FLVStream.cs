using System;
using System.Collections.Generic;
using System.IO;

namespace RTMP_LIB
{
    public class FLVStream
    {
        static readonly byte[] flvHeader = new byte[] 
            { 
              Convert.ToByte('F'), Convert.ToByte('L'), Convert.ToByte('V'), 0x01,
              0x05, // video + audio, we finalize later if the value is different
              0x00, 0x00, 0x00, 0x09,
              0x00, 0x00, 0x00, 0x00 // first prevTagSize=0
            };

        public delegate Stream DataReadyHandler(); // will be called when the first data is going to be written

        public long Length { get; protected set; } // actual amount of bytes written to the stream

        public long EstimatedLength { get; protected set; } // predicted amount of bytes in the file

        public void WriteFLV(RTMP rtmp, DataReadyHandler DataReady, System.Net.Sockets.Socket socket)
        {
            // rtmp must be connected and ready for playback data
            if (!rtmp.IsConnected() || !rtmp.Playing) return;

            Stream outputStream = null;

            // we will be writing in chunks, so use our own stream
            MemoryStream ms = new MemoryStream();

            // header
            ms.Write(FLVStream.flvHeader, 0, FLVStream.flvHeader.Length);

            // write stream
            uint timeStamp = 0;
            int result = 0;
            int packets = 0;
            int httpChunkSize = 1024 * 10; // first chunk should be big enough so the direct show filter can get all the info and do some buffering
            int reconnects = 0;
            do
            {
                try
                {
                    int retries = 0;
                    do
                    {
                        RTMPPacket packet = null;
                        result = rtmp.GetNextMediaPacket(out packet);
                        if (result == 1)
                        {
                            if (!WriteStream(packet, ms, out timeStamp)) break;

                            packets++;
                            if (packets > 10 && ms.Length > httpChunkSize)
                            {
                                if (outputStream == null) // first time writing data
                                {
                                    if (rtmp.CombinedTracksLength > 0)
                                    {
                                        EstimatedLength = rtmp.CombinedTracksLength + (rtmp.CombinedTracksLength / rtmp.InChunkSize) * 11;
                                    }
                                    else if (rtmp.CombinedBitrates > 0)
                                    {
                                        EstimatedLength = (long)(rtmp.CombinedBitrates * 1000 / 8 * rtmp.Duration);
                                    }
                                    else
                                    {
                                        EstimatedLength = (long)(2000 * 1000 / 8 * rtmp.Duration); // nothing was in the metadata -> just use duration and a birate of 2000
                                    }

                                    EstimatedLength = (long)((double)EstimatedLength * 1.5d);

                                    outputStream = DataReady(); // get the stream
                                    httpChunkSize = 1024; // reduce chunksize
                                }

                                byte[] buffer = ms.ToArray();
                                outputStream.Write(buffer, 0, buffer.Length);
                                Length += (uint)buffer.Length;
                                ms = new MemoryStream();
                            }
                        }
                        else if (result == 0)
                        {
                            if (retries > 0)
                            {
                                rtmp.invalidRTMPHeader = true;
                                break;
                            }
                            /* Did we already try pausing, and it still didn't work? */
                            if (rtmp.Pausing == 3)
                            {
                                // Only one try at reconnecting...
                                retries = 1;
                                if (!rtmp.ReconnectStream())
                                {
                                    rtmp.invalidRTMPHeader = true;
                                    Logger.Log("Failed to reconnect the stream.");
                                    break;
                                }
                            }
                            else if (!rtmp.ToggleStream())
                            {
                                rtmp.invalidRTMPHeader = true;
                                Logger.Log("Failed to resume the stream.");
                                break;
                            }
                        }
                    }
                    while (result != 2 && rtmp.Playing && socket.Connected);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.ToString());
                }
                finally
                {
                    // if data already received and connection now closed - try reconnecting rtmp
                    if (result != 2 && timeStamp > 0 && reconnects < 1)
                    {
                        Logger.Log("Connection failed before playback ended - trying reconnect");
                        rtmp.Link.seekTime = timeStamp;
                        reconnects++;
                        if (rtmp.Connect()) reconnects = 0;
                    }
                }
            }
            while (reconnects <= 1 && result != 2 && rtmp.Playing && socket.Connected);
        }

        bool WriteStream(RTMPPacket packet, Stream stream, out uint nTimeStamp)
        {
            nTimeStamp = 0;
            uint prevTagSize = 0;

            // skip video info/command packets
            if (packet.PacketType == PacketType.Video && packet.m_nBodySize == 2 && ((packet.m_body[0] & 0xf0) == 0x50))
            {
                return true;
            }

            if (packet.PacketType == PacketType.Video && packet.m_nBodySize <= 5)
            {
                Logger.Log(string.Format("ignoring too small video packet: size: {0}", packet.m_nBodySize));
                return true;
            }

            if (packet.PacketType == PacketType.Audio && packet.m_nBodySize <= 1)
            {
                Logger.Log(string.Format("ignoring too small audio packet: size: {0}", packet.m_nBodySize));
                return true;
            }

            // audio (0x08), video (0x09) or metadata (0x12) packets :
            // construct 11 byte header then add rtmp packet's data
            if (packet.PacketType == PacketType.Audio || packet.PacketType == PacketType.Video || packet.PacketType == PacketType.Metadata)
            {
                // set data type
                //*dataType |= (((packet.m_packetType == 0x08)<<2)|(packet.m_packetType == 0x09));

                nTimeStamp = (uint)packet.m_nTimeStamp;
                prevTagSize = 11 + packet.m_nBodySize;

                stream.WriteByte((byte)packet.PacketType);

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

            // correct tagSize and obtain timestamp if we have an FLV stream
            if (packet.PacketType == PacketType.FlvTags)
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
                            Logger.Log(string.Format("Wrong data size ({0}), stream corrupted, aborting!", dataSize));
                            return false;
                        }
                        //Log(LOGWARNING, "No tagSize found, appending!");

                        // we have to append a last tagSize!
                        prevTagSize = dataSize + 11;

                        List<byte> somemorebytes = new List<byte>();
                        RTMP.EncodeInt32(somemorebytes, (int)prevTagSize);
                        //stream.Write(somemorebytes.ToArray(), 0, somemorebytes.Count);
                        // todo : does Append mean that the Array is too small?
                        Array.Copy(somemorebytes.ToArray(), 0, packet.m_body, pos + 11 + dataSize, somemorebytes.Count);
                        //RTMP.EncodeInt32(ptr + pos + 11 + dataSize, prevTagSize);                            
                        somemorebytes.Clear();
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

            if (packet.PacketType != PacketType.FlvTags)
            {
                // FLV tag packets contain their own prevTagSize
                List<byte> somemorebytes = new List<byte>();
                RTMP.EncodeInt32(somemorebytes, (int)prevTagSize);
                stream.Write(somemorebytes.ToArray(), 0, somemorebytes.Count);
                //RTMP.EncodeInt32(ptr, prevTagSize);
                //ptr += 4;
                somemorebytes.Clear();
            }

            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
                                        EstimatedLength = (long)(rtmp.CombinedBitrates * 1000 / 8 * (rtmp.Duration <= 0 ? 10800 : rtmp.Duration)); // set 3h if no duration in metadata
                                    }
                                    else
                                    {
                                        // nothing was in the metadata -> just use duration and a bitrate of 2000
                                        EstimatedLength = (long)(2000 * 1000 / 8 * (rtmp.Duration <= 0 ? 10800 : rtmp.Duration)); // set 3h if no duration in metadata
                                    }

                                    EstimatedLength = (long)((double)EstimatedLength * 1.5d);

                                    if (EstimatedLength > 0x7fffffff) EstimatedLength = 0x7fffffff; // honor 2GB size limit

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

                // body
                stream.Write(packet.m_body, 0, (int)packet.m_nBodySize);

                // prevTagSize
                RTMP.EncodeInt32(somebytes, (int)prevTagSize);
                stream.Write(somebytes.ToArray(), 0, somebytes.Count);
                somebytes.Clear();
            }
            // correct tagSize and obtain timestamp if we have an FLV stream
            else if (packet.PacketType == PacketType.FlvTags)
            {
                List<byte> data = packet.m_body.ToList();

                uint pos = 0;

                /* grab first timestamp and see if it needs fixing */
                nTimeStamp = (uint)RTMP.ReadInt24(packet.m_body, 4);
                nTimeStamp |= (uint)(packet.m_body[7] << 24);
                var delta = packet.m_nTimeStamp - nTimeStamp;

                while (pos + 11 < packet.m_nBodySize)
                {
                    uint dataSize = (uint)RTMP.ReadInt24(packet.m_body, (int)pos + 1); // size without header (11) and without prevTagSize (4)
                    nTimeStamp = (uint)RTMP.ReadInt24(packet.m_body, (int)pos + 4);
                    nTimeStamp |= (uint)(packet.m_body[pos + 7] << 24);

                    if (delta != 0)
                    {
                        nTimeStamp += delta;
                        List<byte> newTimeStampData = new List<byte>();
                        RTMP.EncodeInt24(newTimeStampData, (int)nTimeStamp);
                        data[(int)pos + 4] = newTimeStampData[0];
                        data[(int)pos + 5] = newTimeStampData[1];
                        data[(int)pos + 6] = newTimeStampData[2];
                        data[(int)pos + 7] = (byte)(nTimeStamp >> 24);
                    }

                    if (pos + 11 + dataSize + 4 > packet.m_nBodySize)
                    {
                        if (pos + 11 + dataSize > packet.m_nBodySize)
                        {
                            Logger.Log(string.Format("Wrong data size ({0}), stream corrupted, aborting!", dataSize));
                            return false;
                        }
                        // we have to append a last tagSize!
                        prevTagSize = dataSize + 11;
                        RTMP.EncodeInt32(data, (int)prevTagSize);
                    }
                    else
                    {
                        prevTagSize = (uint)RTMP.ReadInt32(packet.m_body, (int)(pos + 11 + dataSize));

                        if (prevTagSize != (dataSize + 11))
                        {
                            //Tag and data size are inconsistent, writing tag size according to dataSize+11
                            prevTagSize = dataSize + 11;
                            RTMP.EncodeInt32(data, (int)prevTagSize, pos + 11 + dataSize);
                        }
                    }

                    pos += prevTagSize + 4;//(11+dataSize+4);
                }

                stream.Write(data.ToArray(), 0, data.Count);
            }
            return true;
        }
    }
}

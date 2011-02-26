using System;
using Vlc.DotNet.Core.Interop;

namespace Vlc.DotNet.Core
{
    public sealed class MediaStatistics
    {
        private readonly MediaBase myMedia;

        #region Audio Output

        public int LostAudioBuffer
        {
            get
            {
                if (myMedia.VlcMedia != IntPtr.Zero)
                {
                    var stats = new libvlc_media_stats_t();
                    if (LibVlcMethods.libvlc_media_get_stats(myMedia.VlcMedia, ref stats))
                        return stats.i_lost_abuffers;
                }
                return 0;
            }
        }

        public int PlayedAudioBuffers
        {
            get
            {
                if (myMedia.VlcMedia != IntPtr.Zero)
                {
                    var stats = new libvlc_media_stats_t();
                    if (LibVlcMethods.libvlc_media_get_stats(myMedia.VlcMedia, ref stats))
                        return stats.i_played_abuffers;
                }
                return 0;
            }
        }

        #endregion

        #region Decoders

        public int DecodedAudio
        {
            get
            {
                if (myMedia.VlcMedia != IntPtr.Zero)
                {
                    var stats = new libvlc_media_stats_t();
                    if (LibVlcMethods.libvlc_media_get_stats(myMedia.VlcMedia, ref stats))
                        return stats.i_decoded_audio;
                }
                return 0;
            }
        }

        public int DecodedVideo
        {
            get
            {
                if (myMedia.VlcMedia != IntPtr.Zero)
                {
                    var stats = new libvlc_media_stats_t();
                    if (LibVlcMethods.libvlc_media_get_stats(myMedia.VlcMedia, ref stats))
                        return stats.i_decoded_video;
                }
                return 0;
            }
        }

        #endregion

        #region Demux

        public float DemuxBitrate
        {
            get
            {
                if (myMedia.VlcMedia != IntPtr.Zero)
                {
                    var stats = new libvlc_media_stats_t();
                    if (LibVlcMethods.libvlc_media_get_stats(myMedia.VlcMedia, ref stats))
                        return stats.f_demux_bitrate;
                }
                return 0;
            }
        }

        public int DemuxCorrupted
        {
            get
            {
                if (myMedia.VlcMedia != IntPtr.Zero)
                {
                    var stats = new libvlc_media_stats_t();
                    if (LibVlcMethods.libvlc_media_get_stats(myMedia.VlcMedia, ref stats))
                        return stats.i_demux_corrupted;
                }
                return 0;
            }
        }

        public int DemuxDiscontinuity
        {
            get
            {
                if (myMedia.VlcMedia != IntPtr.Zero)
                {
                    var stats = new libvlc_media_stats_t();
                    if (LibVlcMethods.libvlc_media_get_stats(myMedia.VlcMedia, ref stats))
                        return stats.i_demux_discontinuity;
                }
                return 0;
            }
        }

        public int DemuxReadBytes
        {
            get
            {
                if (myMedia.VlcMedia != IntPtr.Zero)
                {
                    var stats = new libvlc_media_stats_t();
                    if (LibVlcMethods.libvlc_media_get_stats(myMedia.VlcMedia, ref stats))
                        return stats.i_demux_read_bytes;
                }
                return 0;
            }
        }

        #endregion

        #region Input

        public float InputBitrate
        {
            get
            {
                if (myMedia.VlcMedia != IntPtr.Zero)
                {
                    var stats = new libvlc_media_stats_t();
                    if (LibVlcMethods.libvlc_media_get_stats(myMedia.VlcMedia, ref stats))
                        return stats.f_input_bitrate;
                }
                return 0;
            }
        }

        public int ReadBytes
        {
            get
            {
                if (myMedia.VlcMedia != IntPtr.Zero)
                {
                    var stats = new libvlc_media_stats_t();
                    if (LibVlcMethods.libvlc_media_get_stats(myMedia.VlcMedia, ref stats))
                        return stats.i_read_bytes;
                }
                return 0;
            }
        }

        #endregion

        #region Stream Output

        public int SentPackets
        {
            get
            {
                if (myMedia.VlcMedia != IntPtr.Zero)
                {
                    var stats = new libvlc_media_stats_t();
                    if (LibVlcMethods.libvlc_media_get_stats(myMedia.VlcMedia, ref stats))
                        return stats.i_sent_packets;
                }
                return 0;
            }
        }

        public int SentBytes
        {
            get
            {
                if (myMedia.VlcMedia != IntPtr.Zero)
                {
                    var stats = new libvlc_media_stats_t();
                    if (LibVlcMethods.libvlc_media_get_stats(myMedia.VlcMedia, ref stats))
                        return stats.i_sent_bytes;
                }
                return 0;
            }
        }

        public float SendBitrate
        {
            get
            {
                if (myMedia.VlcMedia != IntPtr.Zero)
                {
                    var stats = new libvlc_media_stats_t();
                    if (LibVlcMethods.libvlc_media_get_stats(myMedia.VlcMedia, ref stats))
                        return stats.f_send_bitrate;
                }
                return 0;
            }
        }

        #endregion

        #region Video Output

        public int DisplayedPictures
        {
            get
            {
                if (myMedia.VlcMedia != IntPtr.Zero)
                {
                    var stats = new libvlc_media_stats_t();
                    if (LibVlcMethods.libvlc_media_get_stats(myMedia.VlcMedia, ref stats))
                        return stats.i_displayed_pictures;
                }
                return 0;
            }
        }

        public int LostPictures
        {
            get
            {
                if (myMedia.VlcMedia != IntPtr.Zero)
                {
                    var stats = new libvlc_media_stats_t();
                    if (LibVlcMethods.libvlc_media_get_stats(myMedia.VlcMedia, ref stats))
                        return stats.i_lost_pictures;
                }
                return 0;
            }
        }

        #endregion

        internal MediaStatistics(MediaBase media)
        {
            myMedia = media;
        }
    }
}
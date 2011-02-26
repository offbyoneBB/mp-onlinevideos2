using System;
using System.Runtime.InteropServices;

namespace Vlc.DotNet.Core.Interop
{

    #region LibVLC meta data

    internal enum libvlc_meta_t
    {
        libvlc_meta_Title,
        libvlc_meta_Artist,
        libvlc_meta_Genre,
        libvlc_meta_Copyright,
        libvlc_meta_Album,
        libvlc_meta_TrackNumber,
        libvlc_meta_Description,
        libvlc_meta_Rating,
        libvlc_meta_Date,
        libvlc_meta_Setting,
        libvlc_meta_URL,
        libvlc_meta_Language,
        libvlc_meta_NowPlaying,
        libvlc_meta_Publisher,
        libvlc_meta_EncodedBy,
        libvlc_meta_ArtworkURL,
        libvlc_meta_TrackID
    }

    #endregion

    #region LibVlc_Media Struct & enum

    internal enum libvlc_state_t
    {
        libvlc_NothingSpecial = 0,
        libvlc_Opening,
        libvlc_Buffering,
        libvlc_Playing,
        libvlc_Paused,
        libvlc_Stopped,
        libvlc_Ended,
        libvlc_Error
    }

    internal enum libvlc_track_type_t
    {
        libvlc_track_unknown = -1,
        libvlc_track_audio = 0,
        libvlc_track_video = 1,
        libvlc_track_text = 2
    }

    internal struct libvlc_media_stats_t
    {
        /* Input */
        public float f_demux_bitrate;
        public float f_input_bitrate;
        public float f_send_bitrate;
        public int i_decoded_audio;
        public int i_decoded_video;
        public int i_demux_corrupted;
        public int i_demux_discontinuity;
        public int i_demux_read_bytes;

        /* Decoders */

        /* Video Output */
        public int i_displayed_pictures;
        public int i_lost_abuffers;
        public int i_lost_pictures;

        /* Audio output */
        public int i_played_abuffers;
        public int i_read_bytes;
        public int i_sent_bytes;
        public int i_sent_packets;
    }

    internal struct libvlc_media_track_info_t
    {
        /* Codec fourcc */
        public audio_struct audio;
        public uint i_codec;
        public int i_id;
        public int i_level;
        public int i_profile;
        public libvlc_track_type_t i_type;

        public video_struct video;
    }

    internal struct audio_struct
    {
        /* Audio specific */
        public uint i_channels;
        public uint i_rate;
    }

    internal struct video_struct
    {
        /* Video specific */
        public uint i_height;
        public uint i_width;
    }

    #endregion

    internal static partial class LibVlcMethods
    {
        /// <summary>
        /// Create a media with a certain given media resource location, for instance a valid URL.
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <param name="mrl">Media location</param>
        /// <returns>Newly created media or NULL on error</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_media_new_location(IntPtr instance, string mrl);

        /// <summary>
        /// Create a media for a certain file path.
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <param name="path">Path local filesystem path</param>
        /// <returns>Newly created media or NULL on error</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_media_new_path(IntPtr instance, string path);

        /// <summary>
        /// Create a media for an already open file descriptor.
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <param name="fd">Open file descriptor</param>
        /// <returns>Newly created media or NULL on error</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_media_new_fd(IntPtr instance, int fd);

        /// <summary>
        /// Create a media as an empty node with a given name.
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <param name="name">The name of the node</param>
        /// <returns>New empty media or NULL on error</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_media_new_as_node(IntPtr instance, string name);

        /// <summary>
        /// Add an option to the media.
        /// </summary>
        /// <param name="mediaInstance">media descriptor</param>
        /// <param name="options">options (as a string)</param>
        [DllImport("libvlc")]
        public static extern void libvlc_media_add_option(IntPtr mediaInstance, string options);

        /// <summary>
        /// Add an option to the media with configurable flags.
        /// </summary>
        /// <param name="mediaInstance">media descriptor</param>
        /// <param name="options">options (as a string)</param>
        /// <param name="flags">flags for this option</param>
        [DllImport("libvlc")]
        public static extern void libvlc_media_add_option_flag(IntPtr mediaInstance, string options, uint flags);

        /// <summary>
        /// Retain a reference to a media descriptor object (libvlc_media_t). Use libvlc_media_release() to decrement the reference count of a media descriptor object.
        /// </summary>
        /// <param name="mediaInstance">media descriptor</param>
        [DllImport("libvlc")]
        public static extern void libvlc_media_retain(IntPtr mediaInstance);

        /// <summary>
        /// Decrement the reference count of a media descriptor object. If the reference count is 0, then libvlc_media_release() will release the media descriptor object. It will send out an libvlc_MediaFreed event to all listeners. If the media descriptor object has been released it should not be used again.
        /// </summary>
        /// <param name="mediaInstance">media descriptor</param>
        /// <returns></returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_media_release(IntPtr mediaInstance);

        /// <summary>
        /// Get the media resource locator (mrl) from a media descriptor object
        /// </summary>
        /// <param name="mediaInstance">media descriptor</param>
        /// <returns></returns>
        [DllImport("libvlc")]
        public static extern IntPtr libvlc_media_get_mrl(IntPtr mediaInstance);

        /// <summary>
        /// Duplicate a media descriptor object.
        /// </summary>
        /// <param name="mediaInstance">media descriptor object.</param>
        /// <returns></returns>
        [DllImport("libvlc")]
        public static extern IntPtr libvlc_media_duplicate(IntPtr mediaInstance);

        /// <summary>
        /// Read the meta of the media.
        /// </summary>
        /// <param name="mediaInstance">Media descriptor object</param>
        /// <param name="meta">Meta to read</param>
        /// <returns></returns>
        [DllImport("libvlc")]
        public static extern IntPtr libvlc_media_get_meta(IntPtr mediaInstance, libvlc_meta_t meta);

        /// <summary>
        /// Set the meta of the media (this function will not save the meta, call libvlc_media_save_meta in order to save the meta)
        /// </summary>
        /// <param name="mediaInstance">Media descriptor object</param>
        /// <param name="meta">Meta to write</param>
        /// <param name="value">Media's meta</param>
        [DllImport("libvlc")]
        public static extern void libvlc_media_set_meta(IntPtr mediaInstance, libvlc_meta_t meta, string value);

        /// <summary>
        /// Save the meta previously set.
        /// </summary>
        /// <param name="mediaInstance">Media descriptor object</param>
        /// <returns>True if the write operation was successfull</returns>
        [DllImport("libvlc")]
        public static extern bool libvlc_media_save_meta(IntPtr mediaInstance);

        /// <summary>
        /// Get current state of media descriptor object.
        /// </summary>
        /// <param name="mediaInstance">Media descriptor object</param>
        /// <returns>State of media descriptor object</returns>
        [DllImport("libvlc")]
        public static extern libvlc_state_t libvlc_media_get_state(IntPtr mediaInstance);

        /// <summary>
        /// Get the current statistics about the media.
        /// </summary>
        /// <param name="mediaInstance">Media descriptor object</param>
        /// <param name="stats">Structure that contain the statistics about the media</param>
        /// <returns></returns>
        [DllImport("libvlc")]
        public static extern bool libvlc_media_get_stats(IntPtr mediaInstance, ref libvlc_media_stats_t stats);

        /// <summary>
        /// Get subitems of media descriptor object.
        /// </summary>
        /// <param name="mediaInstance">Media descriptor object</param>
        /// <returns>List of media descriptor subitems or NULL</returns>
        [DllImport("libvlc")]
        public static extern IntPtr libvlc_media_subitems(IntPtr mediaInstance);

        /// <summary>
        /// Get event manager from media descriptor object.
        /// </summary>
        /// <param name="mediaInstance">Media descriptor object</param>
        /// <returns>Event manager object</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_media_event_manager(IntPtr mediaInstance);

        /// <summary>
        /// Get duration (in ms) of media descriptor object item.
        /// </summary>
        /// <param name="mediaInstance">Media descriptor object</param>
        /// <returns></returns>
        [DllImport("libvlc")]
        public static extern long libvlc_media_get_duration(IntPtr mediaInstance);

        /// <summary>
        /// Parse a media.
        /// </summary>
        /// <param name="mediaInstance">Media descriptor object</param>
        [DllImport("libvlc")]
        public static extern void libvlc_media_parse(IntPtr mediaInstance);

        /// <summary>
        /// Parse a media.
        /// </summary>
        /// <param name="mediaInstance">Media descriptor object</param>
        [DllImport("libvlc")]
        public static extern void libvlc_media_parse_async(IntPtr mediaInstance);

        /// <summary>
        /// Get Parsed status for media descriptor object.
        /// </summary>
        /// <param name="mediaInstance">Media descriptor object</param>
        /// <returns>True if media object has been parsed otherwise it returns false</returns>
        [DllImport("libvlc")]
        public static extern bool libvlc_media_is_parsed(IntPtr mediaInstance);

        /// <summary>
        /// Sets media descriptor's user_data. user_data is specialized data accessed by the host application, VLC.framework uses it as a pointer to an native object that references a libvlc_media_t pointer
        /// </summary>
        /// <param name="mediaInstance">Media descriptor object</param>
        /// <param name="newUserData">Pointer to user data</param>
        [DllImport("libvlc")]
        public static extern void libvlc_media_set_user_data(IntPtr mediaInstance, IntPtr newUserData);

        /// <summary>
        /// Get media descriptor's user_data. user_data is specialized data accessed by the host application, VLC.framework uses it as a pointer to an native object that references a libvlc_media_t pointer
        /// </summary>
        /// <param name="mediaInstance">Media descriptor object</param>
        /// <returns>Pointer to user data</returns>
        [DllImport("libvlc")]
        public static extern IntPtr libvlc_media_get_user_data(IntPtr mediaInstance);

        /// <summary>
        /// Get media descriptor's elementary streams description.
        /// </summary>
        /// <param name="mediaInstance">Media descriptor object</param>
        /// <param name="tracks">Array of Elementary Streams descriptions (must be freed by the caller)</param>
        /// <returns>Number of Elementary Streams</returns>
        [DllImport("libvlc")]
        public static extern int libvlc_media_get_tracks_info(IntPtr mediaInstance, libvlc_media_track_info_t[] tracks);
    }
}
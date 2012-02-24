using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Vlc.DotNet.Core.Interops.Signatures.LibVlc.Media;

namespace Vlc.DotNet.Core.Interops.Signatures
{
    namespace LibVlc
    {
        namespace MediaListPlayer
        {
            public enum PlaybackModes
            {
                Default,
                Loop,
                Repeat
            }

            /// <summary>
            /// Create new media list player.
            /// </summary>
            /// <param name="vlcInstance">Libvlc instance.</param>
            /// <returns>Media list player instance or NULL on error.</returns>
            [LibVlcFunction("libvlc_media_list_player_new")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate IntPtr NewInstance(IntPtr vlcInstance);

            /// <summary>
            /// Release media list player created with NewInstance().
            /// </summary>
            /// <param name="mediaListPlayerInstance">The media list player instance.</param>
            [LibVlcFunction("libvlc_media_list_player_release")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void ReleaseInstance(IntPtr mediaListPlayerInstance);

            /// <summary>
            /// Retain reference to a media list player.
            /// </summary>
            /// <param name="mediaListPlayerInstance">The media list player instance.</param>
            [LibVlcFunction("libvlc_media_list_player_retain", "1.2")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void RetainInstance(IntPtr mediaListPlayerInstance);

            /// <summary>
            /// Get libvlc event manager from this media list player instance.
            /// </summary>
            /// <param name="mediaListPlayerInstance">The media list player instance.</param>
            /// <returns>The event manager associated with mediaListInstance.</returns>
            [LibVlcFunction("libvlc_media_list_player_event_manager")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate IntPtr EventManager(IntPtr mediaListPlayerInstance);

            /// <summary>
            /// Replace media player in media_list_player with this instance.
            /// </summary>
            /// <param name="mediaListPlayerInstance">The media list player instance.</param>
            /// <param name="mediaPlayerInstance">The media player instance.</param>
            [LibVlcFunction("libvlc_media_list_player_set_media_player")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void SetMediaPlayer(IntPtr mediaListPlayerInstance, IntPtr mediaPlayerInstance);

            /// <summary>
            /// Set the media list associated with the player.
            /// </summary>
            /// <param name="mediaListPlayerInstance">The media list player instance.</param>
            /// <param name="mediaListInstance">The media list instance.</param>
            [LibVlcFunction("libvlc_media_list_player_set_media_list")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void SetMediaList(IntPtr mediaListPlayerInstance, IntPtr mediaListInstance);

            /// <summary>
            /// Play media list.
            /// </summary>
            /// <param name="mediaListPlayerInstance">The media list player instance.</param>
            [LibVlcFunction("libvlc_media_list_player_play")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void Play(IntPtr mediaListPlayerInstance);

            /// <summary>
            /// Pause media list.
            /// </summary>
            /// <param name="mediaListPlayerInstance">The media list player instance.</param>
            [LibVlcFunction("libvlc_media_list_player_pause")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void Pause(IntPtr mediaListPlayerInstance);

            /// <summary>
            /// Is media list playing?
            /// </summary>
            /// 
            /// <param name="mediaListPlayerInstance">The media list player instance.</param>
            [LibVlcFunction("libvlc_media_list_player_is_playing")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int IsPlaying(IntPtr mediaListPlayerInstance);

            /// <summary>
            /// Get current state of media list player.
            /// </summary>
            /// <param name="mediaListPlayerInstance">The media list player instance.</param>
            /// <returns>The current state of the media player (playing, paused, ...)</returns>
            [LibVlcFunction("libvlc_media_list_player_get_state")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate States GetState(IntPtr mediaListPlayerInstance);

            /// <summary>
            /// Play media list item at position index.
            /// </summary>
            /// <param name="mediaListPlayerInstance">The media list player instance.</param>
            /// <param name="index">Index in media list to play.</param>
            /// <returns>0 upon success -1 if the item wasn't found.</returns>
            [LibVlcFunction("libvlc_media_list_player_play_item_at_index")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int PlayItemAtIndex(IntPtr mediaListPlayerInstance, int index);

            /// <summary>
            /// Play the given media item.
            /// </summary>
            /// <param name="mediaListPlayerInstance">The media list player instance.</param>
            /// <param name="mediaInstance">Media instance to play.</param>
            /// <returns>0 upon success, -1 if the media is not part of the media list.</returns>
            [LibVlcFunction("libvlc_media_list_player_play_item")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int PlayItem(IntPtr mediaListPlayerInstance, IntPtr mediaInstance);

            /// <summary>
            /// Stop media list.
            /// </summary>
            /// <param name="mediaListPlayerInstance">The media list player instance.</param>
            [LibVlcFunction("libvlc_media_list_player_stop")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void Stop(IntPtr mediaListPlayerInstance);

            /// <summary>
            /// Play next item from media list.
            /// </summary>
            /// <param name="mediaListPlayerInstance">The media list player instance.</param>
            /// <returns>0 upon success -1 if there is no next item.</returns>
            [LibVlcFunction("libvlc_media_list_player_next")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int Next(IntPtr mediaListPlayerInstance);

            /// <summary>
            /// Play previous item from media list.
            /// </summary>
            /// <param name="mediaListPlayerInstance">The media list player instance.</param>
            /// <returns>0 upon success -1 if there is no previous item.</returns>
            [LibVlcFunction("libvlc_media_list_player_previous")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int Previous(IntPtr mediaListPlayerInstance);

            /// <summary>
            /// Sets the playback mode for the playlist.
            /// </summary>
            /// <param name="mediaListPlayerInstance">The media list player instance.</param>
            /// <param name="mode">The playback mode specification.</param>
            /// <returns>0 upon success -1 if there is no previous item.</returns>
            [LibVlcFunction("libvlc_media_list_player_set_playback_mode")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int SetPlaybackMode(IntPtr mediaListPlayerInstance, PlaybackModes mode);
        }
    }
}

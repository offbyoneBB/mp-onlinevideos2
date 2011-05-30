using System;
using System.Runtime.InteropServices;
using Vlc.DotNet.Core.Interops.Signatures.LibVlc.Media;

namespace Vlc.DotNet.Core.Interops.Signatures
{
    namespace LibVlc
    {
        namespace MediaPlayer
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct TrackDescription
            {
                public int id;
                public IntPtr name;
                public IntPtr next;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct AudioOutput
            {
                public IntPtr name;
                public IntPtr description;
                public IntPtr next;
            }

            public struct Rectangle
            {
                public int top, left, bottom, right;
            }

            public enum VideoMarqueeOption
            {
                Enable = 0,
                Text,
                Color,
                Opacity,
                Position,
                Refresh,
                Size,
                Timeout,
                X,
                Y
            }

            public enum NavigationMode
            {
                Activate = 0,
                Up,
                Down,
                Left,
                Right
            }

            /// <summary>
            /// Create an empty Media Player object
            /// </summary>
            /// <param name="vlcInstance">The libvlc instance in which the Media Player</param>
            /// <returns>A new media player object, or NULL on error.</returns>
            [LibVlcFunction("libvlc_media_player_new")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate IntPtr NewInstance(IntPtr vlcInstance);

            /// <summary>
            /// Create a Media Player object from a Media
            /// </summary>
            /// <param name="mediaInstance">The media. Afterwards the p_md can be safely destroyed.</param>
            /// <returns>A new media player object, or NULL on error.</returns>
            [LibVlcFunction("libvlc_media_player_new_from_media")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate IntPtr NewInstanceFromMedia(IntPtr mediaInstance);

            /// <summary>
            /// Release a media_player after use decrement the reference count of a media player object. If the reference count is 0, then libvlc_media_player_release() will release the media player object. If the media player object has been released, then it should not be used again.
            /// </summary>
            /// <param name="playerInstance">The Media Player to free</param>
            [LibVlcFunction("libvlc_media_player_release")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void ReleaseInstance(IntPtr playerInstance);

            /// <summary>
            /// Retain a reference to a media player object. Use libvlc_media_player_release() to decrement reference count.
            /// </summary>
            /// <param name="playerInstance">Media player object</param>
            [LibVlcFunction("libvlc_media_player_retain")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void RetainInstance(IntPtr playerInstance);

            /// <summary>
            /// Set the media that will be used by the media_player. If any, previous md will be released.
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <param name="media">The Media. Afterwards the media can be safely destroyed.</param>
            [LibVlcFunction("libvlc_media_player_set_media")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void SetMedia(IntPtr playerInstance, IntPtr media);

            /// <summary>
            /// Get the media used by the media_player.
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <returns>The media associated with playerInstance, or NULL if no media is associated</returns>
            [LibVlcFunction("libvlc_media_player_get_media")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate IntPtr GetMedia(IntPtr playerInstance);

            /// <summary>
            /// Get the Event Manager from which the media player send event.
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <returns>The event manager associated with playerInstance</returns>
            [LibVlcFunction("libvlc_media_player_event_manager")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate IntPtr EventManager(IntPtr playerInstance);

            /// <summary>
            /// Is playing
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <returns>1 if the media player is playing, 0 otherwise</returns>
            [LibVlcFunction("libvlc_media_player_is_playing")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int IsPlaying(IntPtr playerInstance);

            /// <summary>
            /// Play
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <returns>0 if playback started (and was already started), or -1 on error.</returns>
            [LibVlcFunction("libvlc_media_player_play")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int Play(IntPtr playerInstance);

            /// <summary>
            /// Pause or resume (no effect if there is no media)
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <param name="pause">play/resume if zero, pause if non-zero</param>
            [LibVlcFunction("libvlc_media_player_set_pause", "1.1.1")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void SetPause(IntPtr playerInstance, int pause);

            /// <summary>
            /// Toggle pause (no effect if there is no media)
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            [LibVlcFunction("libvlc_media_player_pause")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void Pause(IntPtr playerInstance);

            /// <summary>
            /// Stop (no effect if there is no media)
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            [LibVlcFunction("libvlc_media_player_stop")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void Stop(IntPtr playerInstance);

            namespace Video
            {
                /// <summary>
                /// Callback prototype to allocate and lock a picture buffer. Whenever a new video frame needs to be decoded, the lock callback is invoked. Depending on the video chroma, one or three pixel planes of adequate dimensions must be returned via the second parameter. Those planes must be aligned on 32-bytes boundaries.
                /// </summary>
                /// <param name="opaque">Private pointer as passed to SetCallbacks()</param>
                /// <param name="planes">Planes start address of the pixel planes (LibVLC allocates the array of void pointers, this callback must initialize the array)</param>
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate void LockCallbackDelegate(IntPtr opaque, ref IntPtr planes);

                /// <summary>
                /// Callback prototype to unlock a picture buffer. When the video frame decoding is complete, the unlock callback is invoked. This callback might not be needed at all. It is only an indication that the application can now read the pixel values if it needs to.
                /// </summary>
                /// <param name="opaque">Private pointer as passed to SetCallbacks()</param>
                /// <param name="picture">Private pointer returned from the LockCallback callback</param>
                /// <param name="planes">Pixel planes as defined by the @ref libvlc_video_lock_cb callback (this parameter is only for convenience)</param>
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate void UnlockCallbackDelegate(IntPtr opaque, IntPtr picture, ref IntPtr planes);

                /// <summary>
                /// Callback prototype to display a picture. When the video frame needs to be shown, as determined by the media playback clock, the display callback is invoked.
                /// </summary>
                /// <param name="opaque">Private pointer as passed to SetCallbacks()</param>
                /// <param name="picture">Private pointer returned from the LockCallback callback</param>
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate void DisplayCallbackDelegate(IntPtr opaque, IntPtr picture);

                [LibVlcFunction("libvlc_video_set_callbacks", "1.1.1")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate void SetCallbacks(IntPtr playerInstance, LockCallbackDelegate @lock, UnlockCallbackDelegate unlock, DisplayCallbackDelegate display, IntPtr opaque);

                [LibVlcFunction("libvlc_video_set_format", "1.1.1")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate void SetFormat(IntPtr playerInstance, string chroma, uint width, uint height, uint pitch);

                /// <summary>
                /// Callback prototype to configure picture buffers format. This callback gets the format of the video as output by the video decoder and the chain of video filters (if any). It can opt to change any parameter as it needs. In that case, LibVLC will attempt to convert the video format (rescaling and chroma conversion) but these operations can be CPU intensive.
                /// </summary>
                /// <param name="opaque">Pointer to the private pointer passed to SetCallbacks()</param>
                /// <param name="chroma">Pointer to the 4 bytes video format identifier</param>
                /// <param name="width">Pointer to the pixel width</param>
                /// <param name="height">Pointer to the pixel height</param>
                /// <param name="pitches">Table of scanline pitches in bytes for each pixel plane (the table is allocated by LibVLC)</param>
                /// <param name="lines">Table of scanlines count for each plane</param>
                /// <returns>Number of picture buffers allocated, 0 indicates failure</returns>
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate uint FormatCallbackDelegate(ref IntPtr opaque, ref uint chroma, ref uint width, ref uint height, ref uint pitches, ref uint lines);

                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate void CleanupCallbackDelegate(IntPtr opaque);

                [LibVlcFunction("libvlc_video_set_format_callbacks", "1.2.0")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate void SetFormatCallbacks(IntPtr playerInstance, FormatCallbackDelegate setup, CleanupCallbackDelegate cleanup);
            }

            /// <summary>
            /// Set a Win32/Win64 API window handle (HWND) where the media player should render its video output. If LibVLC was built without Win32/Win64 API output support, then this has no effects.
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <param name="drawable">Windows handle of the drawable</param>
            [LibVlcFunction("libvlc_media_player_set_hwnd")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void SetHwnd(IntPtr playerInstance, IntPtr drawable);

            /// <summary>
            /// Get the Windows API window handle (HWND) previously set with SetHwnd(). The handle will be returned even if LibVLC is not currently outputting any video to it.
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <returns>Window handle or NULL if there are none.</returns>
            [LibVlcFunction("libvlc_media_player_get_hwnd")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate IntPtr GetHwnd(IntPtr playerInstance);

            /// <summary>
            /// Get the current movie length (in ms).
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <returns>Movie length (in ms), or -1 if there is no media.</returns>
            [LibVlcFunction("libvlc_media_player_get_length")]
            public delegate long GetLength(IntPtr playerInstance);

            /// <summary>
            /// Get the current movie time (in ms).
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <returns>Movie time (in ms), or -1 if there is no media.</returns>
            [LibVlcFunction("libvlc_media_player_get_time")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate long GetTime(IntPtr playerInstance);

            /// <summary>
            /// Set the movie time (in ms). This has no effect if no media is being played. Not all formats and protocols support this.
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <param name="time">The movie time (in ms).</param>
            [LibVlcFunction("libvlc_media_player_set_time")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void SetTime(IntPtr playerInstance, long time);

            /// <summary>
            /// Get movie position.
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <returns>Movie position, or -1. in case of error</returns>
            [LibVlcFunction("libvlc_media_player_get_position")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate float GetPosition(IntPtr playerInstance);

            /// <summary>
            /// Set movie position.
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <param name="position">Movie position</param>
            [LibVlcFunction("libvlc_media_player_set_position")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void SetPosition(IntPtr playerInstance, float position);

            /// <summary>
            /// Set movie chapter (if applicable).
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <param name="chapter">Chapter number to play</param>
            [LibVlcFunction("libvlc_media_player_set_chapter")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void SetChapter(IntPtr playerInstance, int chapter);

            /// <summary>
            /// Get movie chapter.
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <returns>Chapter number currently playing, or -1 if there is no media.</returns>
            [LibVlcFunction("libvlc_media_player_get_chapter")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int GetChapter(IntPtr playerInstance);

            /// <summary>
            /// Get movie chapter count.
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <returns>Number of chapters in movie, or -1.</returns>
            [LibVlcFunction("libvlc_media_player_get_chapter_count")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int GetChapterCount(IntPtr playerInstance);

            /// <summary>
            /// Is the player able to play.
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <returns></returns>
            [LibVlcFunction("libvlc_media_player_will_play")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int WillPlay(IntPtr playerInstance);

            /// <summary>
            /// Get title chapter count.
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <param name="title">Title</param>
            /// <returns>Number of chapters in title, or -1.</returns>
            [LibVlcFunction("libvlc_media_player_get_chapter_count_for_title")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int GetChapterCountForTitle(IntPtr playerInstance, int title);

            /// <summary>
            /// Set movie title.
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <param name="title">Title number to play</param>
            [LibVlcFunction("libvlc_media_player_set_title")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void SetTitle(IntPtr playerInstance, int title);

            /// <summary>
            /// Get movie title.
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <returns>Title number currently playing, or -1.</returns>
            [LibVlcFunction("libvlc_media_player_get_title")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int GetTitle(IntPtr playerInstance);

            /// <summary>
            /// Get movie title count.
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <returns>Title number count, or -1.</returns>
            [LibVlcFunction("libvlc_media_player_get_title_count")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int GetTitleCount(IntPtr playerInstance);

            /// <summary>
            /// Set previous chapter (if applicable)
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            [LibVlcFunction("libvlc_media_player_previous_chapter")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void SetPreviousChapter(IntPtr playerInstance);

            /// <summary>
            /// Set next chapter (if applicable)
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            [LibVlcFunction("libvlc_media_player_next_chapter")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void SetNextChapter(IntPtr playerInstance);

            /// <summary>
            /// Get the requested movie play rate.
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <returns>Movie play rate.</returns>
            [LibVlcFunction("libvlc_media_player_get_rate")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate float GetRate(IntPtr playerInstance);

            /// <summary>
            /// Set the requested movie play rate.
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <param name="rate">Rate movie play rate to set.</param>
            /// <returns>-1 if an error was detected, 0 otherwise (but even then, it might not actually work depending on the underlying media protocol)</returns>
            [LibVlcFunction("libvlc_media_player_set_rate")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int SetRate(IntPtr playerInstance, float rate);

            /// <summary>
            /// Get current movie state.
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <returns>The current state of the media player (playing, paused, ...)</returns>
            [LibVlcFunction("libvlc_media_player_get_state")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate States GetState(IntPtr playerInstance);

            /// <summary>
            /// Get movie fps rate
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <returns>Frames per second (fps) for this playing movie, or 0 if unspecified</returns>
            [LibVlcFunction("libvlc_media_player_get_fps")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate float GetFPS(IntPtr playerInstance);

            /// <summary>
            /// How many video outputs does this media player have?
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <returns>Number of video outputs.</returns>
            [LibVlcFunction("libvlc_media_player_has_vout")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int HasVideoOut(IntPtr playerInstance);

            /// <summary>
            /// Is this media player seekable?
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <returns>True if the media player can seek</returns>
            [LibVlcFunction("libvlc_media_player_is_seekable")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int IsSeekable(IntPtr playerInstance);

            /// <summary>
            /// Can this media player be paused?
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <returns>True if the media player can pause</returns>
            [LibVlcFunction("libvlc_media_player_can_pause")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int IsPausable(IntPtr playerInstance);

            /// <summary>
            /// Display the next frame (if supported)
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            [LibVlcFunction("libvlc_media_player_next_frame")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void NextFrame(IntPtr playerInstance);

            /// <summary>
            /// Navigate through DVD Menu
            /// </summary>
            /// <param name="playerInstance">The Media Player</param>
            /// <param name="navigate">The Navigation mode</param>
            [LibVlcFunction("libvlc_media_player_navigate", "1.2.0")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void Navigate(IntPtr playerInstance, uint navigate);

            /// <summary>
            /// Release (free) trackDescription
            /// </summary>
            /// <param name="trackDescription">TrackDescription to release</param>
            [LibVlcFunction("libvlc_track_description_release")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void ReleaseTrackDescription(TrackDescription trackDescription);

            namespace Video
            {
                /// <summary>
                /// Toggle fullscreen status on non-embedded video outputs.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                [LibVlcFunction("libvlc_toggle_fullscreen")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate void ToggleFullscreen(IntPtr playerInstance);

                /// <summary>
                /// Enable or disable fullscreen.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <param name="fullscreen">Boolean for fullscreen status</param>
                [LibVlcFunction("libvlc_set_fullscreen")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate void SetFullscreen(IntPtr playerInstance, int fullscreen);

                /// <summary>
                /// Get current fullscreen status.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <returns>Fullscreen status (boolean)</returns>
                [LibVlcFunction("libvlc_get_fullscreen")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate int GetFullscreen(IntPtr playerInstance);

                [LibVlcFunction("libvlc_video_set_key_input")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate void SetKeyInput(IntPtr playerInstance, int on);

                [LibVlcFunction("libvlc_video_get_size")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate int GetSize(IntPtr playerInstance, uint num, out uint x, out uint y);

                [LibVlcFunction("libvlc_video_get_cursor")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate int GetCursor(IntPtr playerInstance, uint num, out uint x, out uint y);

                [LibVlcFunction("libvlc_video_get_scale")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate float GetScale(IntPtr playerInstance);

                [LibVlcFunction("libvlc_video_set_scale")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate int SetScale(IntPtr playerInstance, float scale);

                [LibVlcFunction("libvlc_video_get_aspect_ratio")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate string GetAspectRatio(IntPtr playerInstance);

                [LibVlcFunction("libvlc_video_set_aspect_ratio")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate void SetAspectRatio(IntPtr playerInstance, string aspect);

                /// <summary>
                /// Get current video subtitle.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <returns>Video subtitle selected, or -1 if none.</returns>
                [LibVlcFunction("libvlc_video_get_spu")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate int GetSpu(IntPtr playerInstance);

                /// <summary>
                /// Get the number of available video subtitles.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <returns>Number of available video subtitles.</returns>
                [LibVlcFunction("libvlc_video_get_spu_count")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate int GetSpuCount(IntPtr playerInstance);

                [LibVlcFunction("libvlc_video_get_spu_description")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate IntPtr GetSpuDescription(IntPtr playerInstance);

                /// <summary>
                /// Set new video subtitle.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <param name="spu">New video subtitle to select.</param>
                /// <returns>0 on success, -1 if out of range.</returns>
                [LibVlcFunction("libvlc_video_set_spu")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate int SetSpu(IntPtr playerInstance, int spu);

                /// <summary>
                /// Set new video subtitle file.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <param name="subtitleFile">New video subtitle file</param>
                /// <returns>success status (boolean)</returns>
                [LibVlcFunction("libvlc_video_set_subtitle_file")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate int SetSubtitleFile(IntPtr playerInstance, string subtitleFile);

                [LibVlcFunction("libvlc_video_get_title_description")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate IntPtr GetTitleDescription(IntPtr playerInstance);

                [LibVlcFunction("libvlc_video_get_chapter_description")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate IntPtr GetChapterDescription(IntPtr playerInstance, int title);

                //TODO
                //[LibVlcFunction("libvlc_video_get_crop_geometry")]
                //[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                //public delegate string GetCropGeometry(IntPtr playerInstance);

                //TODO
                //[LibVlcFunction("libvlc_video_set_crop_geometry")]
                //[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                //public delegate void SetCropGeometry(IntPtr playerInstance, string geometry);

                /// <summary>
                /// Get current teletext page requested.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <returns>Current teletext page requested.</returns>
                [LibVlcFunction("libvlc_video_get_teletext")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate int GetTeletext(IntPtr playerInstance);

                /// <summary>
                /// Set new teletext page to retrieve.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <param name="teletextPage">Teletex page number requested</param>
                [LibVlcFunction("libvlc_video_set_teletext")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate void SetTeletext(IntPtr playerInstance, int teletextPage);

                /// <summary>
                /// Toggle teletext transparent status on video output.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                [LibVlcFunction("libvlc_toggle_teletext")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate void ToggleTeletext(IntPtr playerInstance);

                /// <summary>
                /// Get number of available video tracks.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <returns>Number of available video tracks</returns>
                [LibVlcFunction("libvlc_video_get_track_count")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate int GetTrackCount(IntPtr playerInstance);

                [LibVlcFunction("libvlc_video_get_track_description")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate IntPtr GetTrackDescription(IntPtr playerInstance);

                /// <summary>
                /// Get current video track.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <returns>Video track (int) or -1 if none</returns>
                [LibVlcFunction("libvlc_video_get_track")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate int GetTrack(IntPtr playerInstance);

                /// <summary>
                /// Set video track.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <param name="track">The track (int)</param>
                /// <returns>0 on success, -1 if out of range</returns>
                [LibVlcFunction("libvlc_video_set_track")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate int SetTrack(IntPtr playerInstance, int track);

                /// <summary>
                /// Take a snapshot of the current video window.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <param name="numVideo">Number of video output (typically 0 for the first/only one)</param>
                /// <param name="filePath">The path where to save the screenshot to</param>
                /// <param name="width">Snapshot's width</param>
                /// <param name="height">Snapshot's height</param>
                /// <returns>0 on success, -1 if the video was not found</returns>
                [LibVlcFunction("libvlc_video_take_snapshot")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate int TakeSnapshot(IntPtr playerInstance, uint numVideo, byte[] filePath, uint width, uint height);

                /// <summary>
                /// Enable or disable deinterlace filter
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <param name="mode">Type of deinterlace filter, NULL to disable</param>
                [LibVlcFunction("libvlc_video_set_deinterlace")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate void SetDeinterlace(IntPtr playerInstance, string mode);

                /// <summary>
                /// Get an integer marquee option value
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <param name="option">Option</param>
                /// <returns>The value</returns>
                [LibVlcFunction("libvlc_video_get_marquee_int")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate int GetIntegerMarquee(IntPtr playerInstance, VideoMarqueeOption option);

                /// <summary>
                /// Get an string marquee option value
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <param name="option">Option</param>
                /// <returns>The value</returns>
                [LibVlcFunction("libvlc_video_get_marquee_string")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate int GetStringMarquee(IntPtr playerInstance, VideoMarqueeOption option);

                /// <summary>
                /// Enable, disable or set an integer marquee option
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <param name="option">Option</param>
                /// <param name="value">The integer value</param>
                [LibVlcFunction("libvlc_video_set_marquee_int")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate void SetIntegerMarquee(IntPtr playerInstance, VideoMarqueeOption option, int value);

                /// <summary>
                /// Set a marquee string option
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <param name="option">Option</param>
                /// <param name="value">The string value</param>
                [LibVlcFunction("libvlc_video_set_marquee_string")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate void SetStringMarquee(IntPtr playerInstance, VideoMarqueeOption option, string value);

                public enum LogoOption
                {
                    Enable,
                    File,
                    X,
                    Y,
                    Delay,
                    Repeat,
                    Opacity,
                    Position
                }

                /// <summary>
                /// Get integer logo option.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <param name="option">Option</param>
                /// <returns>The interger value</returns>
                [LibVlcFunction("libvlc_video_get_logo_int")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate int GetIntegerLogoOption(IntPtr playerInstance, LogoOption option);

                /// <summary>
                /// Set logo option as integer. Options that take a different type value are ignored.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <param name="option">Option</param>
                /// <param name="value">The interger value</param>
                [LibVlcFunction("libvlc_video_set_logo_int")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate void SetIntegerLogoOption(IntPtr playerInstance, LogoOption option, int value);

                /// <summary>
                /// Set logo option as string. Options that take a different type value are ignored.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <param name="option">Option</param>
                /// <param name="value">The string value</param>
                [LibVlcFunction("libvlc_video_set_logo_string")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate void SetStringLogoOption(IntPtr playerInstance, LogoOption option, string value);

                public enum AdjustOption
                {
                    Enable = 0,
                    Contrast,
                    Brightness,
                    Hue,
                    Saturation,
                    Gamma
                }

                /// <summary>
                /// Get integer adjust option.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <param name="option">Option</param>
                /// <returns>The interger value</returns>
                [LibVlcFunction("libvlc_video_get_adjust_int", "1.1.1")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate int GetIntegerAdjust(IntPtr playerInstance, AdjustOption option);

                /// <summary>
                /// Set adjust option as integer. Options that take a different type value are ignored
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <param name="option">Option</param>
                /// <param name="value">The interger value</param>
                [LibVlcFunction("libvlc_video_set_adjust_int", "1.1.1")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate void SetIntegerAdjust(IntPtr playerInstance, AdjustOption option, int value);

                /// <summary>
                /// Get float adjust option.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <param name="option">Option</param>
                /// <returns>The float value</returns>
                [LibVlcFunction("libvlc_video_get_adjust_float", "1.1.1")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate float GetFloatAdjust(IntPtr playerInstance, AdjustOption option);

                /// <summary>
                /// Set adjust option as float. Options that take a different type value are ignored
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <param name="option">Option</param>
                /// <param name="value">The float value</param>
                [LibVlcFunction("libvlc_video_set_adjust_float", "1.1.1")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate void SetFloatAdjust(IntPtr playerInstance, AdjustOption option, float value);

            }

            namespace Audio
            {
                /// <summary>
                /// Audio device types
                /// </summary>
                public enum OutputDeviceTypes
                {
                    Error = -1,
                    Mono = 1,
                    Stereo = 2,
                    _2F2R = 4,
                    _3F2R = 5,
                    _5_1 = 6,
                    _6_1 = 7,
                    _7_1 = 8,
                    SPDIF = 10,
                }

                /// <summary>
                /// Audio channels
                /// </summary>
                public enum OutputChannel
                {
                    Error = -1,
                    Stereo = 1,
                    RStereo = 2,
                    Left = 3,
                    Right = 4,
                    Dolbys = 5,
                }

                /// <summary>
                /// Get the list of available audio outputs
                /// </summary>
                /// <param name="instance">The LibVlc Player</param>
                /// <returns>List of available audio outputs.</returns>
                [LibVlcFunction("libvlc_audio_output_list_get")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate IntPtr NewOutputListInstance(IntPtr instance);

                /// <summary>
                /// Free the list of available audio outputs
                /// </summary>
                /// <param name="outputList">The output list</param>
                [LibVlcFunction("libvlc_audio_output_list_release")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate void ReleaseOutputList(IntPtr outputList);

                /// <summary>
                /// Get count of devices for audio output, these devices are hardware oriented like analor or digital output of sound card
                /// </summary>
                /// <param name="instance">The LibVlc Player</param>
                /// <param name="outputName">Name of audio output</param>
                /// <returns>Number of devices</returns>
                [LibVlcFunction("libvlc_audio_output_device_count")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate int OutputDeviceCount(IntPtr instance, string outputName);

                /// <summary>
                /// Get long name of device, if not available short name given
                /// </summary>
                /// <param name="instance">The LibVlc Player</param>
                /// <param name="outputName">Name of audio output</param>
                /// <param name="deviceIndex">Device index</param>
                /// <returns>Long name of the devide</returns>
                [LibVlcFunction("libvlc_audio_output_device_longname")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate string GetOutputDeviceLongName(IntPtr instance, string outputName, int deviceIndex);

                /// <summary>
                /// Get id name of device
                /// </summary>
                /// <param name="instance">The LibVlc Player</param>
                /// <param name="outputName">Name of audio output</param>
                /// <param name="deviceIndex">Device index</param>
                /// <returns>Id name of device, use for setting device, need to be free after use</returns>
                [LibVlcFunction("libvlc_audio_output_device_id")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate string GetOutputDeviceIdName(IntPtr instance, string outputName, int deviceIndex);

                /// <summary>
                /// Set audio output device. Changes are only effective after stop and play.
                /// </summary>
                /// <param name="instance">The LibVlc Player</param>
                /// <param name="outputName">Name of audio output</param>
                /// <param name="deviceIdName">Id name of device</param>
                [LibVlcFunction("libvlc_audio_output_device_set")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate void SetOutputDevice(IntPtr instance, string outputName, string deviceIdName);

                /// <summary>
                /// Get current audio device type. Device type describes something like character of output sound - stereo sound, 2.1, 5.1 etc
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <returns>The audio device type</returns>
                [LibVlcFunction("libvlc_audio_output_get_device_type")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate OutputDeviceTypes GetOutputDeviceType(IntPtr playerInstance);

                /// <summary>
                /// Set current audio device type.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <param name="deviceType">The audio device type</param>
                [LibVlcFunction("libvlc_audio_output_set_device_type")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate void SetOutputDeviceType(IntPtr playerInstance, OutputDeviceTypes deviceType);

                /// <summary>
                /// Toggle mute status.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                [LibVlcFunction("libvlc_audio_toggle_mute")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate void ToggleMute(IntPtr playerInstance);

                /// <summary>
                /// Get current mute status.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <returns>The mute status (boolean)</returns>
                [LibVlcFunction("libvlc_audio_get_mute")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate int GetMute(IntPtr playerInstance);

                /// <summary>
                /// Set current mute status.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <param name="status">If status is true then mute, otherwise unmute</param>
                [LibVlcFunction("libvlc_audio_set_mute")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate void SetMute(IntPtr playerInstance, int status);

                /// <summary>
                /// Get current audio level.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <returns>The audio level</returns>
                [LibVlcFunction("libvlc_audio_get_volume")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate int GetVolume(IntPtr playerInstance);

                /// <summary>
                /// Set current audio level.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <param name="level">The audio level</param>
                [LibVlcFunction("libvlc_audio_set_volume")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate void SetVolume(IntPtr playerInstance, int level);

                /// <summary>
                /// Get number of available audio tracks.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <returns>Number of available audio tracks, or -1 if unavailable</returns>
                [LibVlcFunction("libvlc_audio_get_track_count")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate int GetTrackCount(IntPtr playerInstance);

                [LibVlcFunction("libvlc_audio_get_track_description")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate TrackDescription GetTrackDescription(IntPtr playerInstance);

                /// <summary>
                /// Get current audio track.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <returns>Audio track (int), or -1 if none.</returns>
                [LibVlcFunction("libvlc_audio_get_track")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate int GetTrack(IntPtr playerInstance);

                /// <summary>
                /// Set current audio track.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <param name="audioTrack">Audio track</param>
                /// <returns>0 on success, -1 on error</returns>
                [LibVlcFunction("libvlc_audio_set_track")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate int SetTrack(IntPtr playerInstance, int audioTrack);

                /// <summary>
                /// Get current audio channel.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <returns>The audio channel</returns>
                [LibVlcFunction("libvlc_audio_get_channel")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate OutputChannel GetChannel(IntPtr playerInstance);

                /// <summary>
                ///  current audio channel.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <param name="channel">The audio channel</param>
                /// <returns>0 on success, -1 on error</returns>
                [LibVlcFunction("libvlc_audio_set_channel")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate int SetChannel(IntPtr playerInstance, OutputChannel channel);

                /// <summary>
                /// Get current audio delay.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <returns>The audio delay (microseconds)</returns>
                [LibVlcFunction("libvlc_audio_get_delay", "1.1.1")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate long GetDelay(IntPtr playerInstance);

                /// <summary>
                /// Set current audio delay. The audio delay will be reset to zero each time the media changes.
                /// </summary>
                /// <param name="playerInstance">The Media Player</param>
                /// <param name="delay">The audio delay (microseconds)</param>
                /// <returns>0 on success, -1 on error</returns>
                [LibVlcFunction("libvlc_audio_set_delay", "1.1.1")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate int SetDelay(IntPtr playerInstance, long delay);
            }
        }
    }
}
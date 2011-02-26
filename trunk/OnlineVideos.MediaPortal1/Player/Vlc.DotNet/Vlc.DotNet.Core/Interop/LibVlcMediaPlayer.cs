using System;
using System.Runtime.InteropServices;

namespace Vlc.DotNet.Core.Interop
{
    internal static partial class LibVlcMethods
    {
        #region LibVLC media player

        #region libvlc_navigate_mode_t enum

        public enum libvlc_navigate_mode_t
        {
            libvlc_navigate_activate = 0,
            libvlc_navigate_up,
            libvlc_navigate_down,
            libvlc_navigate_left,
            libvlc_navigate_right
        }

        #endregion

        #region libvlc_video_marquee_option_t enum

        public enum libvlc_video_marquee_option_t
        {
            libvlc_marquee_Enable = 0,
            libvlc_marquee_Text,
            libvlc_marquee_Color,
            libvlc_marquee_Opacity,
            libvlc_marquee_Position,
            libvlc_marquee_Refresh,
            libvlc_marquee_Size,
            libvlc_marquee_Timeout,
            libvlc_marquee_X,
            libvlc_marquee_Y
        }

        #endregion

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_media_player_new(IntPtr instance);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_media_player_new_from_media(IntPtr media);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_media_player_release(IntPtr playerInstance);

        // libvlc_media_player_retain

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_media_player_set_media(IntPtr playerInstance, IntPtr media);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_media_player_get_media(IntPtr playerInstance);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern IntPtr libvlc_media_player_event_manager(IntPtr playerInstance);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_media_player_is_playing(IntPtr playerInstance);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_media_player_play(IntPtr playerInstance);

        // libvlc_media_player_set_pause 

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_media_player_pause(IntPtr playerInstance);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_media_player_stop(IntPtr playerInstance);

        // libvlc_video_set_callbacks
        // libvlc_video_set_format
        // libvlc_media_player_set_nsobject 
        // libvlc_media_player_get_nsobject 
        // libvlc_media_player_set_agl 
        // libvlc_media_player_get_agl 
        // libvlc_media_player_set_xwindow
        // libvlc_media_player_get_xwindow 

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_media_player_set_hwnd(IntPtr playerInstance, IntPtr drawable);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_media_player_get_hwnd(IntPtr playerInstance);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern Int64 libvlc_media_player_get_length(IntPtr playerInstance);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern Int64 libvlc_media_player_get_time(IntPtr playerInstance);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_media_player_set_time(IntPtr playerInstance, Int64 time);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern float libvlc_media_player_get_position(IntPtr playerInstance, Int64 time);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_media_player_set_position(IntPtr playerInstance, float pos);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_media_player_set_chapter(IntPtr playerInstance, int chapter);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_media_player_get_chapter(IntPtr playerInstance);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_media_player_get_chapter_count(IntPtr playerInstance);

        // libvlc_media_player_will_play

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_media_player_get_chapter_count_for_title(IntPtr playerInstance, int title);

        /// <summary>
        /// Set movie title
        /// </summary>
        /// <param name="playerInstance">The Media Player</param>
        /// <param name="title">Title number to play</param>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_media_player_set_title(IntPtr playerInstance, int title);

        /// <summary>
        /// Get movie title
        /// </summary>
        /// <param name="playerInstance">The Media Player</param>
        /// <returns>Title number currently playing, or -1</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_media_player_get_title(IntPtr playerInstance);

        // libvlc_media_player_get_title_count

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_media_player_previous_chapter(IntPtr playerInstance);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_media_player_next_chapter(IntPtr playerInstance);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern float libvlc_media_player_get_rate(IntPtr playerInstance);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_media_player_set_rate(IntPtr playerInstance, float rate);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern libvlc_state_t libvlc_media_player_get_state(IntPtr playerInstance);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern float libvlc_media_player_get_fps(IntPtr playerInstance);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint libvlc_media_player_has_vout(IntPtr playerInstance);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_media_player_is_seekable(IntPtr playerInstance);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_media_player_can_pause(IntPtr playerInstance);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_media_player_next_frame(IntPtr playerInstance);

        // libvlc_media_player_navigate

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_track_description_release(IntPtr ptrack);

        #region Nested type: libvlc_audio_output_t

        [StructLayout(LayoutKind.Sequential)]
        public struct libvlc_audio_output_t
        {
            [MarshalAs(UnmanagedType.LPStr)] public string psz_name;
            [MarshalAs(UnmanagedType.LPStr)] public string psz_description;
            public IntPtr p_next;
        }

        #endregion

        #region Nested type: libvlc_rectangle_t

        [StructLayout(LayoutKind.Sequential)]
        public struct libvlc_rectangle_t
        {
            public int top, left;
            public int bottom, right;
        }

        #endregion

        #region Nested type: libvlc_track_description_t

        [StructLayout(LayoutKind.Sequential)]
        public struct libvlc_track_description_t
        {
            public uint i_id;
            [MarshalAs(UnmanagedType.LPStr)] public string psz_name;
            public IntPtr p_next;
        }

        #endregion

        #endregion

        #region LibVLC video controls

        #region libvlc_video_adjust_option_t enum

        public enum libvlc_video_adjust_option_t
        {
            libvlc_adjust_Enable = 0,
            libvlc_adjust_Contrast,
            libvlc_adjust_Brightness,
            libvlc_adjust_Hue,
            libvlc_adjust_Saturation,
            libvlc_adjust_Gamma
        }

        #endregion

        #region libvlc_video_logo_option_t enum

        public enum libvlc_video_logo_option_t
        {
            libvlc_logo_enable,
            libvlc_logo_file,
            libvlc_logo_x,
            libvlc_logo_y,
            libvlc_logo_delay,
            libvlc_logo_repeat,
            libvlc_logo_opacity,
            libvlc_logo_position
        }

        #endregion

        /// <summary>
        /// Toggle fullscreen status on non-embedded video outputs.
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_toggle_fullscreen(IntPtr playerInstance);

        /// <summary>
        /// Enable or disable fullscreen.
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <param name="fullscreen">Fullscreen status</param>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_set_fullscreen(IntPtr playerInstance, int fullscreen);

        /// <summary>
        /// Get current fullscreen status.
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <returns>Fullscreen status</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_get_fullscreen(IntPtr playerInstance);

        // libvlc_video_set_key_input
        // libvlc_video_set_mouse_input

        /// <summary>
        /// Get the pixel dimensions of a video.
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <param name="num">Number of the video (starting from, and most commonly 0)</param>
        /// <param name="x">Pixel width</param>
        /// <param name="y">Pixel height</param>
        /// <returns>0 on success, -1 if the specified video does not exist</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_video_get_size(IntPtr playerInstance, uint num, ref uint x, ref uint y);

        /// <summary>
        /// Get the mouse pointer coordinates over a video. Coordinates are expressed in terms of the decoded video resolution, not in terms of pixels on the screen/viewport (to get the latter, you can query your windowing system directly).
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <param name="num">Number of the video (starting from, and most commonly 0)</param>
        /// <param name="x">Pixel width</param>
        /// <param name="y">Pixel height</param>
        /// <returns>0 on success, -1 if the specified video does not exist</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_video_get_cursor(IntPtr playerInstance, uint num, ref uint x, ref uint y);

        /// <summary>
        /// Get the current video scaling factor.
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <returns>currently configured zoom factor, or 0. if the video is set to fit to the output window/drawable automatically</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern float libvlc_video_get_scale(IntPtr playerInstance);

        /// <summary>
        /// Set the video scaling factor. That is the ratio of the number of pixels on screen to the number of pixels in the original decoded video in each dimension. Zero is a special value; it will adjust the video to the output window/drawable (in windowed mode) or the entire screen.
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <param name="factor">Scaling factor, or zero</param>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_video_set_scale(IntPtr playerInstance, float factor);

        /// <summary>
        /// Get current video aspect ratio.
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <returns>Video aspect ratio or NULL if unspecified (the result must be released with free() or libvlc_free())</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern string libvlc_video_get_aspect_ratio(IntPtr playerInstance);

        /// <summary>
        /// Set new video aspect ratio.
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <param name="aspect">new video aspect-ratio or NULL to reset to default</param>
        /// <remarks>Invalid aspect ratios are ignored.</remarks>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_video_set_aspect_ratio(IntPtr playerInstance, string aspect);

        /// <summary>
        /// Get current video subtitle.
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <returns>Video subtitle selected, or -1 if none</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_video_get_spu(IntPtr playerInstance);

        /// <summary>
        /// Get the number of available video subtitles.
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <returns>Number of available video subtitles</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_video_get_spu_count(IntPtr playerInstance);

        /// <summary>
        /// Get the description of available video subtitles.
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <returns>List containing description of available video subtitles</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        public static extern IntPtr libvlc_video_get_spu_description(IntPtr playerInstance);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <param name="spu">new video subtitle to select</param>
        /// <returns>Success status</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_video_set_spu(IntPtr playerInstance, uint spu);

        /// <summary>
        /// Set new video subtitle.
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <param name="subtitleFile">New video subtitle file</param>
        /// <returns>0 on success, -1 if out of range</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_video_set_subtitle_file(IntPtr playerInstance, string subtitleFile);

        /// <summary>
        /// Get the description of available titles.
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <returns>Success status</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_video_get_title_description(IntPtr playerInstance);

        /// <summary>
        /// Get the description of available chapters for specific title.
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <param name="title">Selected title</param>
        /// <returns>List containing description of available chapter for title title</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_video_get_chapter_description(IntPtr playerInstance, int title);

        // libvlc_video_get_crop_geometry
        // libvlc_video_set_crop_geometry
        // libvlc_video_get_teletext
        // libvlc_video_set_teletext
        // libvlc_toggle_teletext

        /// <summary>
        /// Get number of available video tracks.
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <returns>Number of available video tracks</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_video_get_track_count(IntPtr playerInstance);

        /// <summary>
        /// Get the description of available video tracks.
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <returns>List with description of available video tracks, or NULL on error</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_video_get_track_description(IntPtr playerInstance);

        /// <summary>
        /// Get current video track.
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <returns>Video track or -1 if none</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_video_get_track(IntPtr playerInstance);

        /// <summary>
        /// Set video track.
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <param name="track">Track</param>
        /// <returns>0 on success, -1 if out of range</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_video_set_track(IntPtr playerInstance, int track);

        /// <summary>
        /// Take a snapshot of the current video window.
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <param name="num">Number of video output (typically 0 for the first/only one)</param>
        /// <param name="path">Path where to save the screenshot to</param>
        /// <param name="width">Snapshot's width</param>
        /// <param name="height">Snapshot's height</param>
        /// <returns>0 on success, -1 if the video was not found</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_video_take_snapshot(IntPtr playerInstance, uint num, string path, uint width, uint height);

        /// <summary>
        /// Enable or disable deinterlace filter
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <param name="mode">Type of deinterlace filter, NULL to disable</param>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_video_set_deinterlace(IntPtr playerInstance, string mode);

        /// <summary>
        /// Get an integer marquee option value
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <param name="option">Marq option to get</param>
        /// <returns></returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_video_get_marquee_int(IntPtr playerInstance, libvlc_video_marquee_option_t option);

        /// <summary>
        /// Get a string marquee option value
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <param name="option">Marq option to get</param>
        /// <returns></returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_video_marquee_string(IntPtr playerInstance, uint option);

        /// <summary>
        /// Enable, disable or set an integer marquee option
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <param name="option">Marq option to set</param>
        /// <param name="value">Marq option value</param>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_video_set_marquee_int(IntPtr playerInstance, uint option, int value);

        /// <summary>
        /// Set a marquee string option
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <param name="option">Marq option to set</param>
        /// <param name="value">Marq option value</param>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_video_set_marquee_string(IntPtr playerInstance, uint option, string value);

        /// <summary>
        /// Get integer logo option
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <param name="option">Logo option to get, values of libvlc_video_logo_option_t</param>
        /// <returns></returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_video_get_logo_int(IntPtr playerInstance, libvlc_video_logo_option_t option);

        /// <summary>
        /// Set logo option as integer. Options that take a different type value are ignored.
        /// </summary>
        /// <param name="playerInstance">Media player instance</param>
        /// <param name="option">Logo option to set, values of libvlc_video_logo_option_t</param>
        /// <param name="value">Logo option value</param>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_video_set_logo_int(IntPtr playerInstance, libvlc_video_logo_option_t option, int value);

        // libvlc_video_get_adjust_int
        // libvlc_video_set_adjust_int
        // libvlc_video_get_adjust_float
        // libvlc_video_set_adjust_float

        #endregion

        #region LibVLC audio controls

        #region libvlc_audio_output_channel_t enum

        /// <summary>
        /// Audio channels
        /// </summary>
        public enum libvlc_audio_output_channel_t
        {
            libvlc_AudioChannel_Error = -1,
            libvlc_AudioChannel_Stereo = 1,
            libvlc_AudioChannel_RStereo = 2,
            libvlc_AudioChannel_Left = 3,
            libvlc_AudioChannel_Right = 4,
            libvlc_AudioChannel_Dolbys = 5
        }

        #endregion

        #region libvlc_audio_output_device_types_t enum

        /// <summary>
        /// Audio device types
        /// </summary>
        public enum libvlc_audio_output_device_types_t
        {
            libvlc_AudioOutputDevice_Error = -1,
            libvlc_AudioOutputDevice_Mono = 1,
            libvlc_AudioOutputDevice_Stereo = 2,
            libvlc_AudioOutputDevice_2F2R = 4,
            libvlc_AudioOutputDevice_3F2R = 5,
            libvlc_AudioOutputDevice_5_1 = 6,
            libvlc_AudioOutputDevice_6_1 = 7,
            libvlc_AudioOutputDevice_7_1 = 8,
            libvlc_AudioOutputDevice_SPDIF = 10
        }

        #endregion

        /// <summary>
        /// Get the list of available audio outputs.
        /// </summary>
        /// <param name="instance">Libvlc instance</param>
        /// <returns>List of available audio outputs. It must be freed it with. In case of error, NULL is returned.</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern libvlc_audio_output_t libvlc_audio_output_list_get(IntPtr instance);

        /// <summary>
        /// Free the list of available audio outputs.
        /// </summary>
        /// <param name="list">List with audio outputs for release</param>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_audio_output_list_release(libvlc_audio_output_t list);

        /// <summary>
        /// Set the audio output.
        /// Change will be applied after stop and play.
        /// </summary>
        /// <param name="player">Media player instance</param>
        /// <param name="name">Name of audio output (use psz_name of <see cref="libvlc_audio_output_t"/>)</param>
        /// <returns>True if function succeded</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_audio_output_set(IntPtr playerInstance, string name);

        /// <summary>
        /// Get count of devices for audio output, these devices are hardware oriented like analor or digital output of sound card.
        /// </summary>
        /// <param name="instance">Libvlc instance</param>
        /// <param name="audioOutput">Name of audio output, <see cref="libvlc_audio_output_t"/></param>
        /// <returns>Number of devices</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_audio_output_device_count(IntPtr instance, string audioOutput);

        /// <summary>
        /// Get long name of device, if not available short name given.
        /// </summary>
        /// <param name="instance">Libvlc instance</param>
        /// <param name="audioOutput">Name of audio output, <see cref="libvlc_audio_output_t"/></param>
        /// <param name="deviceId">Device index</param>
        /// <returns>Long name of device</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_audio_output_device_longname(IntPtr instance, string audioOutput, int deviceId);

        /// <summary>
        /// Get id name of device
        /// </summary>
        /// <param name="instance">Libvlc instance</param>
        /// <param name="audioOutput">Name of audio output, <see cref="libvlc_audio_output_t"/></param>
        /// <param name="deviceId">Device index</param>
        /// <returns>Id name of device, use for setting device, need to be free after use</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_audio_output_device_id(IntPtr instance, string audioOutput, int deviceId);

        /// <summary>
        /// Set audio output device. Changes are only effective after stop and play.
        /// </summary>
        /// <param name="playerInstance">Media Player instance</param>
        /// <param name="audioOutput">Name of audio output, <see cref="libvlc_audio_output_t"/></param>
        /// <param name="deviceId">Device index</param>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_audio_output_device_set(IntPtr playerInstance, string audioOutput, string deviceId);

        /// <summary>
        /// Get current audio device type. Device type describes something like character of output sound - stereo sound, 2.1, 5.1 etc
        /// </summary>
        /// <param name="playerInstance">Media Player instance</param>
        /// <returns>Audio devices type <see cref="libvlc_audio_output_device_types_t"/></returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_audio_output_get_device_type(IntPtr playerInstance);

        /// <summary>
        /// Set current audio device type.
        /// </summary>
        /// <param name="playerInstance">Media Player instance</param>
        /// <param name="deviceType">Audio device type</param>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_audio_output_set_device_type(IntPtr playerInstance, libvlc_audio_output_device_types_t deviceType);

        //public static extern void libvlc_audio_output_set_device_type(IntPtr playerInstance, int deviceType);

        /// <summary>
        /// Toggle mute status.
        /// </summary>
        /// <param name="playerInstance">Media Player instance</param>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_audio_toggle_mute(IntPtr playerInstance);

        /// <summary>
        /// Get current mute status.
        /// </summary>
        /// <param name="playerInstance">Media Player instance</param>
        /// <returns>Mute status</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_audio_get_mute(IntPtr playerInstance);

        /// <summary>
        /// Set mute status.
        /// </summary>
        /// <param name="playerInstance">Media Player instance</param>
        /// <param name="status">If status is true then mute, otherwise unmute</param>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_audio_set_mute(IntPtr playerInstance, int status);

        /// <summary>
        /// Get current audio level.
        /// </summary>
        /// <param name="playerInstance">Media Player instance</param>
        /// <returns>Audio level</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_audio_get_volume(IntPtr playerInstance);

        /// <summary>
        /// Set current audio level.
        /// </summary>
        /// <param name="playerInstance">Media Player instance</param>
        /// <param name="newVolume">New volume level</param>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_audio_set_volume(IntPtr playerInstance, int newVolume);

        /// <summary>
        /// Get number of available audio tracks.
        /// </summary>
        /// <param name="playerInstance">Media Player instance</param>
        /// <returns>Number of available audio tracks, or -1 if unavailable</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_audio_get_track_count(IntPtr playerInstance);

        /// <summary>
        /// Get the description of available audio tracks.
        /// </summary>
        /// <param name="playerInstance">Media Player instance</param>
        /// <returns>Description of available audio tracks, or NULL</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern libvlc_track_description_t libvlc_audio_get_track_description(IntPtr playerInstance);

        /// <summary>
        /// Get current audio track.
        /// </summary>
        /// <param name="playerInstance">Media Player instance</param>
        /// <returns>Audio track, or -1 if none</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_audio_get_track(IntPtr playerInstance);

        /// <summary>
        /// Set current audio track.
        /// </summary>
        /// <param name="playerInstance">Media Player instance</param>
        /// <param name="track">Track</param>
        /// <returns>0 on success, -1 on error</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_audio_set_track(IntPtr playerInstance, int track);

        /// <summary>
        /// Get current audio channel.
        /// </summary>
        /// <param name="playerInstance">Media Player instance</param>
        /// <returns>audio channel <see cref="libvlc_audio_output_channel_t"/></returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_audio_get_channel(IntPtr playerInstance);

        /// <summary>
        /// Set current audio channel.
        /// </summary>
        /// <param name="playerInstance">Media Player instance</param>
        /// <param name="channel">audio channel, <see cref="libvlc_audio_output_channel_t"/></param>
        /// <returns>0 on success, -1 on error</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_audio_set_channel(IntPtr playerInstance, int channel);

        /// <summary>
        /// Get current audio delay.
        /// </summary>
        /// <param name="playerInstance">Media Player instance</param>
        /// <returns>Audio delay (microseconds)</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern Int64 libvlc_audio_get_delay(IntPtr playerInstance);

        /// <summary>
        /// Set current audio delay. The audio delay will be reset to zero each time the media changes.
        /// </summary>
        /// <param name="playerInstance">Media Player instance</param>
        /// <param name="delay">Audio delay (microseconds)</param>
        /// <returns>0 on success, -1 on error</returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_audio_set_delay(IntPtr playerInstance, Int64 delay);

        #endregion
    }
}
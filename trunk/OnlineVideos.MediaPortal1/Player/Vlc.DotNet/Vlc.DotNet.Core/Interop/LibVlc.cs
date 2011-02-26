using System;
using System.Runtime.InteropServices;

namespace Vlc.DotNet.Core.Interop
{
    internal static partial class LibVlcMethods
    {
        #region LibVLC error handling

        /// <summary>
        /// A human-readable error message for the last LibVLC error in the calling thread. The resulting string is valid until another error occurs (at least until the next LibVLC call).
        /// </summary>
        /// <returns>NULL if there was no error</returns>
        [DllImport("libvlc")]
        public static extern string libvlc_errmsg();

        /// <summary>
        /// Clears the LibVLC error status for the current thread. This is optional. By default, the error status is automatically overridden when a new error occurs, and destroyed when the thread exits.
        /// </summary>
        [DllImport("libvlc")]
        public static extern void libvlc_clearerr();

        // libvlc_vprinterr

        #endregion

        /// <summary>
        /// Create and initialize a libvlc instance. This functions accept a list of "command line" arguments similar to the main(). These arguments affect the LibVLC instance default configuration.
        /// </summary>
        /// <param name="argc"></param>
        /// <param name="argv"></param>
        /// <returns></returns>
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_new(int argc, string[] argv);

        // libvlc_new_with_builtins

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_release(IntPtr instance);

        // libvlc_retain
        // libvlc_add_intf
        // libvlc_set_exit_handler
        // libvlc_wait
        // libvlc_set_user_agent

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern string libvlc_get_version();

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern string libvlc_get_compiler();

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern string libvlc_get_changeset();

        #region LibVLC asynchronous events

        #region Delegates

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void EventCallbackDelegate(ref libvlc_event_t eventType, IntPtr userData);

        #endregion

        [DllImport("libvlc")]
        public static extern int libvlc_event_attach(IntPtr eventManagerInstance, libvlc_event_e eventType, IntPtr callback, IntPtr userData);

        [DllImport("libvlc")]
        public static extern void libvlc_event_detach(IntPtr eventManagerInstance, libvlc_event_e eventType, IntPtr callback, IntPtr userData);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern IntPtr libvlc_event_type_name(libvlc_event_t eventType);

        #endregion

        #region LibVLC logging

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint libvlc_get_log_verbosity(IntPtr instance);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_set_log_verbosity(IntPtr logger, uint level);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_log_open(IntPtr instance);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_log_close(IntPtr logger);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint libvlc_log_count(IntPtr logger);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_log_clear(IntPtr logger);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libvlc_log_get_iterator(IntPtr logger);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libvlc_log_iterator_free(IntPtr logger);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libvlc_log_iterator_has_next(IntPtr iterator);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
        public static extern libvlc_log_message_t libvlc_log_iterator_next(IntPtr iterator, ref libvlc_log_message_t buffer);

        #endregion

        // libvlc_free
    }
}
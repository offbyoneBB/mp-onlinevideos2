using System;
using System.Runtime.InteropServices;

namespace Vlc.DotNet.Core.Interops.Signatures
{
    namespace LibVlc
    {
        namespace ErrorHandling
        {
            /// <summary>
            /// A human-readable error message for the last LibVLC error in the calling thread. The resulting string is valid until another error occurs (at least until the next LibVLC call).
            /// </summary>
            /// <returns>This will be NULL if there was no error.</returns>
            [LibVlcFunction("libvlc_errmsg")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate string GetErrorMessage();

            /// <summary>
            /// Clears the LibVLC error status for the current thread. This is optional. By default, the error status is automatically overridden when a new error occurs, and destroyed when the thread exits.
            /// </summary>
            [LibVlcFunction("libvlc_clearerr")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void ClearError();
        }

        /// <summary>
        /// Create and initialize a libvlc instance. This functions accept a list of "command line" arguments similar to the main(). These arguments affect the LibVLC instance default configuration.
        /// </summary>
        /// <param name="argsCount">The number of arguments</param>
        /// <param name="args">List of arguments</param>
        /// <returns>Libvlc instance or NULL in case of error</returns>
        [LibVlcFunction("libvlc_new")]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr NewInstance(int argsCount, string[] args);

        /// <summary>
        /// Decrement the reference count of a libvlc instance, and destroy it if it reaches zero.
        /// </summary>
        /// <param name="instance">The instance to destroy</param>
        [LibVlcFunction("libvlc_release")]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ReleaseInstance(IntPtr instance);

        /// <summary>
        /// Increments the reference count of a libvlc instance. The initial reference count is 1 after NewInstance() returns.
        /// </summary>
        /// <param name="instance">The instance to reference</param>
        [LibVlcFunction("libvlc_retain")]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void RetainInstance(IntPtr instance);

        /// <summary>
        /// Try to start a user interface for the libvlc instance.
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <param name="name">Interface name, or NULL for default</param>
        /// <returns>0 on success, -1 on error.</returns>
        [LibVlcFunction("libvlc_add_intf")]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int AddInterface(IntPtr instance, string name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ExitCallbackDelegate();

        /// <summary>
        /// Registers a callback for the LibVLC exit event. This is mostly useful if you have started at least one interface with AddInterface(). Typically, this function will wake up your application main loop (from another thread).
        /// </summary>
        /// <param name="instance">The LibVLC instance</param>
        /// <param name="callback">Callback to invoke when LibVLC wants to exit</param>
        /// <param name="opaque">Data pointer for the callback</param>
        [LibVlcFunction("libvlc_set_exit_handler", "1.2")]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void SetExitCallback(IntPtr instance, ExitCallbackDelegate callback, IntPtr opaque);

        /// <summary>
        /// Waits until an interface causes the instance to exit. You should start at least one interface first, using AddInterface().
        /// </summary>
        /// <param name="instance">The LibVLC instance</param>
        [LibVlcFunction("libvlc_wait")]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Wait(IntPtr instance);

        /// <summary>
        /// Sets the application name. LibVLC passes this as the user agent string when a protocol requires it.
        /// </summary>
        /// <param name="instance">The LibVLC instance</param>
        /// <param name="userAgentName">Human-readable application name, e.g. "FooBar player 1.2.3"</param>
        /// <param name="userAgentHttp">HTTP User Agent, e.g. "FooBar/1.2.3 Python/2.6.0"</param>
        [LibVlcFunction("libvlc_set_user_agent", "1.1.1")]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void SetUserAgent(IntPtr instance, string userAgentName, string userAgentHttp);

        /// <summary>
        /// Retrieve libvlc version.
        /// </summary>
        /// <returns>String containing the libvlc version.</returns>
        [LibVlcFunction("libvlc_get_version")]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate string GetVersion();

        /// <summary>
        /// Retrieve libvlc compiler version.
        /// </summary>
        /// <returns>String containing the libvlc compiler version.</returns>
        [LibVlcFunction("libvlc_get_compiler")]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate string GetCompiler();

        /// <summary>
        /// Retrieve libvlc changeset.
        /// </summary>
        /// <returns>String containing the libvlc changeset.</returns>
        
        [LibVlcFunction("libvlc_get_changeset")]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate string GetChangeSet();

        /// <summary>
        /// Frees an heap allocation returned by a LibVLC function.
        /// </summary>
        /// <param name="pointer">Pointer to memory.</param>
        [LibVlcFunction("libvlc_free", "1.2")]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void FreeMemory(IntPtr pointer);

        namespace AsynchronousEvents
        {
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void EventCallbackDelegate(ref LibVlcEventArgs eventType, IntPtr userData);

            /// <summary>
            /// Register for an event notification. 
            /// </summary>
            /// <param name="eventManagerInstance">The event manager to which you want to attach to.</param>
            /// <param name="eventType">The desired event to which we want to listen.</param>
            /// <param name="callback">The function to call when EventType occurs.</param>
            /// <param name="userData">User provided data to carry with the event.</param>
            /// <returns>0 on success, ENOMEM on error.</returns>
            [LibVlcFunction("libvlc_event_attach")]
            public delegate int Attach(IntPtr eventManagerInstance, EventTypes eventType, EventCallbackDelegate callback, IntPtr userData);

            /// <summary>
            /// Unregister an event notification.
            /// </summary>
            /// <param name="eventManagerInstance">The event manager to which you want to attach to.</param>
            /// <param name="eventType">The desired event to which we want to listen.</param>
            /// <param name="callback">The function to call when EventType occurs.</param>
            /// <param name="userData">User provided data to carry with the event.</param>
            [LibVlcFunction("libvlc_event_detach")]
            public delegate void Detach(IntPtr eventManagerInstance, EventTypes eventType, EventCallbackDelegate callback, IntPtr userData);

            /// <summary>
            /// Get an event's type name.
            /// </summary>
            /// <param name="eventType">The desired event.</param>
            /// <returns></returns>
            [LibVlcFunction("libvlc_event_type_name")]
            public delegate string GetTypeName(EventTypes eventType);
        }

        namespace Logging
        {
            /// <summary>
            /// Return the VLC messaging verbosity level
            /// </summary>
            /// <param name="instance">The LibVLC instance</param>
            /// <returns>Verbosity level for messages</returns>
            [LibVlcFunction("libvlc_get_log_verbosity")]
            public delegate uint GetVerbosity(IntPtr instance);

            /// <summary>
            /// Set the VLC messaging verbosity level
            /// </summary>
            /// <param name="instance">The LibVLC instance</param>
            /// <param name="verbosity">Verbosity level for messages</param>
            [LibVlcFunction("libvlc_set_log_verbosity")]
            public delegate void SetVerbosity(IntPtr instance, uint verbosity);

            /// <summary>
            /// Open a VLC message log instance.
            /// </summary>
            /// <param name="instance">The LibVLC instance</param>
            /// <returns>Log instance or NULL on error.</returns>
            [LibVlcFunction("libvlc_log_open")]
            public delegate IntPtr Open(IntPtr instance);

            /// <summary>
            /// Close a VLC message log instance.
            /// </summary>
            /// <param name="logInstance">Log instance or NULL</param>
            [LibVlcFunction("libvlc_log_close")]
            public delegate void Close(IntPtr logInstance);

            /// <summary>
            /// Returns the number of messages in a log instance.
            /// </summary>
            /// <param name="logInstance">Log instance or NULL.</param>
            /// <returns>Number of log messages, 0 if logInstance is NULL.</returns>
            [LibVlcFunction("libvlc_log_count")]
            public delegate uint Count(IntPtr logInstance);

            /// <summary>
            /// Clear a log instance.
            /// </summary>
            /// <param name="logInstance">Log instance or NULL.</param>
            [LibVlcFunction("libvlc_log_clear")]
            public delegate void Clear(IntPtr logInstance);

            /// <summary>
            /// Allocate and returns a new iterator to messages in log.
            /// </summary>
            /// <param name="logInstance">Log instance or NULL.</param>
            /// <returns>Log iterator object or NULL on error</returns>
            [LibVlcFunction("libvlc_log_get_iterator")]
            public delegate IntPtr GetIterator(IntPtr logInstance);

            /// <summary>
            /// Return whether log iterator has more messages.
            /// </summary>
            /// <param name="logIteratorInstance">Log iterator instance or NULL</param>
            /// <returns>True if iterator has more message objects, else false</returns>
            [LibVlcFunction("libvlc_log_iterator_has_next")]
            public delegate bool HasNext(IntPtr logIteratorInstance);

            /// <summary>
            /// Release a previoulsy allocated iterator.
            /// </summary>
            /// <param name="logIteratorInstance">Log iterator instance.</param>
            [LibVlcFunction("libvlc_log_iterator_free")]
            public delegate void FreeInstance(IntPtr logIteratorInstance);

            /// <summary>
            /// The message contents must not be freed.
            /// </summary>
            /// <param name="logIteratorInstance">Log iterator instance or NULL.</param>
            /// <param name="buffer">Log buffer.</param>
            /// <returns>Log message object or NULL if none left.</returns>
            [LibVlcFunction("libvlc_log_iterator_next")]
            public delegate void Next(IntPtr logIteratorInstance, ref LogMessage buffer);
        }
        //TODO
        /*public struct ModuleDescription
        {
            public string Name;
            public string Shortname;
            public string Longname;
            public string Help;
            public IntPtr NextHandle;

            //public static readonly ModuleDescription Empty;

            //public ModuleDescription GetNext()
            //{
            //    if (NextHandle != IntPtr.Zero)
            //        return (ModuleDescription)Marshal.PtrToStructure(NextHandle, typeof(ModuleDescription));
            //    return Empty;
            //}
        }

        
        [VlcFunction("libvlc_module_description_list_release")]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr GetModuleDescriptionList(IntPtr instance);

        /// <summary>
        /// Release a list of module descriptions.
        /// </summary>
        /// <param name="moduleDescription">The list to be released</param>
        [VlcFunction("libvlc_module_description_list_release")]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ReleaseModuleDescription(ModuleDescription moduleDescription);

        namespace Audio
        {
            namespace Filter
            {
                /// <summary>
                /// Returns a list of audio filters that are available.
                /// </summary>
                /// <param name="instance">The LibVLC instance</param>
                /// <returns>List of module descriptions. It should be freed with LibVlcInterop.Module.Release(). In case of an error, NULL is returned.</returns>
                [VlcFunction("libvlc_audio_filter_list_get")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate ModuleDescription GetList(IntPtr instance);
            }
        }

        namespace Video
        {
            namespace Filter
            {
                /// <summary>
                /// Returns a list of video filters that are available.
                /// </summary>
                /// <param name="instance">The LibVLC instance</param>
                /// <returns>List of module descriptions. It should be freed with LibVlcInterop.Module.Release(). In case of an error, NULL is returned.</returns>
                [VlcFunction("libvlc_video_filter_list_get")]
                [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                public delegate ModuleDescription GetList(IntPtr instance);
            }
        }*/
    }
}
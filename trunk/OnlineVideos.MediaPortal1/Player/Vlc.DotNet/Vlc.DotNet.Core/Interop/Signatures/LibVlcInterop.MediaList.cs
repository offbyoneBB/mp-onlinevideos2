using System;
using System.Runtime.InteropServices;

namespace Vlc.DotNet.Core.Interops.Signatures
{
    namespace LibVlc
    {
        namespace MediaList
        {
            /// <summary>
            /// Create an empty media list.
            /// </summary>
            /// <param name="vlcInstance">The libvlc instance.</param>
            /// <returns>Empty media list instance, or NULL on error.</returns>
            [LibVlcFunction("libvlc_media_list_new")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate IntPtr NewInstance(IntPtr vlcInstance);

            /// <summary>
            /// Release media list created with NewInstance().
            /// </summary>
            /// <param name="mediaListInstance">The media list instance.</param>
            [LibVlcFunction("libvlc_media_list_release")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void ReleaseInstance(IntPtr mediaListInstance);

            /// <summary>
            /// Retain reference to a media list.
            /// </summary>
            /// <param name="mediaListInstance">The media list instance.</param>
            [LibVlcFunction("libvlc_media_list_retain")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void RetainInstance(IntPtr mediaListInstance);

            /// <summary>
            /// Associate media instance with this media list instance.
            /// </summary>
            /// <param name="mediaListInstance">The media list instance.</param>
            /// <param name="mediaInstance">The media instance.</param>
            [LibVlcFunction("libvlc_media_list_set_media")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void SetMedia(IntPtr mediaListInstance, IntPtr mediaInstance);

            /// <summary>
            /// Get media instance from this media list instance.
            /// </summary>
            /// <param name="mediaListInstance">The media list instance.</param>
            /// <returns>The media instance.</returns>
            [LibVlcFunction("libvlc_media_list_media")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate IntPtr GetMedia(IntPtr mediaListInstance);

            /// <summary>
            /// Add media instance to media list.
            /// </summary>
            /// <param name="mediaListInstance">The media list instance.</param>
            /// <param name="mediaInstance">The media instance.</param>
            /// <returns>0 on success, -1 if the media list is read-only.</returns>
            [LibVlcFunction("libvlc_media_list_add_media")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int AddMedia(IntPtr mediaListInstance, IntPtr mediaInstance);

            /// <summary>
            ///  Insert media instance in media list on a position.
            /// </summary>
            /// <param name="mediaListInstance">The media list instance.</param>
            /// <param name="mediaInstance">The media instance.</param>
            /// <param name="index">Position in array where to insert.</param>
            /// <returns>0 on success, -1 if the media list si read-only.</returns>
            [LibVlcFunction("libvlc_media_list_insert_media")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int InsertMedia(IntPtr mediaListInstance, IntPtr mediaInstance, int index);

            /// <summary>
            /// Remove media instance from media list on a position.
            /// </summary>
            /// <param name="mediaListInstance">The media list instance.</param>
            /// <param name="index">Position in array where to remove.</param>
            /// <returns>0 on success, -1 if the list is read-only or the item was not found.</returns>
            [LibVlcFunction("libvlc_media_list_remove_index")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int RemoveAt(IntPtr mediaListInstance, int index);

            /// <summary>
            /// Get count on media list items.
            /// </summary>
            /// <param name="mediaListInstance">The media list instance.</param>
            /// <returns>Number of items in media list.</returns>
            [LibVlcFunction("libvlc_media_list_count")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int Count(IntPtr mediaListInstance);

            /// <summary>
            /// List media instance in media list at a position.
            /// </summary>
            /// <param name="mediaListInstance">The media list instance.</param>
            /// <param name="index">Position in array where to get.</param>
            /// <returns>Media instance at position.</returns>
            [LibVlcFunction("libvlc_media_list_item_at_index")]
            public delegate IntPtr GetItemAt(IntPtr mediaListInstance, int index);

            /// <summary>
            /// Find index position of List media instance in media list.
            /// </summary>
            /// <param name="mediaListInstance">The media list instance.</param>
            /// <param name="mediaInstance">The media instance.</param>
            /// <returns>Position of media instance.</returns>
            [LibVlcFunction("libvlc_media_list_index_of_item")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int IndexOf(IntPtr mediaListInstance, IntPtr mediaInstance);

            /// <summary>
            /// This indicates if this media list is read-only from a user point of view.
            /// </summary>
            /// <param name="mediaListInstance">The media list instance.</param>
            /// <returns>0 on readonly, 1 on readwrite.</returns>
            [LibVlcFunction("libvlc_media_list_is_readonly")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate int IsReadOnly(IntPtr mediaListInstance);

            /// <summary>
            /// Get lock on media list items.
            /// </summary>
            /// <param name="mediaListInstance">The media list instance.</param>
            [LibVlcFunction("libvlc_media_list_lock")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void Lock(IntPtr mediaListInstance);

            /// <summary>
            /// Release lock on media list items.
            /// </summary>
            /// <param name="mediaListInstance">The media list instance.</param>
            [LibVlcFunction("libvlc_media_list_unlock")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void Unlock(IntPtr mediaListInstance);

            /// <summary>
            /// Get libvlc_event_manager from this media list instance.
            /// </summary>
            /// <param name="mediaListInstance">The media list instance.</param>
            /// <returns>The event manager associated with mediaListInstance.</returns>
            [LibVlcFunction("libvlc_media_list_event_manager")]
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate IntPtr EventManager(IntPtr mediaListInstance);
        }
    }
}

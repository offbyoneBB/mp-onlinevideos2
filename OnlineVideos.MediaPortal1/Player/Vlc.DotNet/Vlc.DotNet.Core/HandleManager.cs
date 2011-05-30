using System;
using System.Collections.Generic;
using Vlc.DotNet.Core.Medias;

namespace Vlc.DotNet.Core
{
    internal sealed class VlcHandleManager
    {
        public VlcHandleManager()
        {
            MediaPlayerHandles = new Dictionary<IVlcControl, IntPtr>();
            MediasHandles = new Dictionary<MediaBase, IntPtr>();
            EventManagerHandles = new Dictionary<IVlcControl, IntPtr>();
        }

        public IntPtr LibVlcHandle { get; set; }

        public Dictionary<IVlcControl, IntPtr> MediaPlayerHandles { get; private set; }
        public Dictionary<MediaBase, IntPtr> MediasHandles { get; private set; }
        public Dictionary<IVlcControl, IntPtr> EventManagerHandles { get; private set; }
    }
}
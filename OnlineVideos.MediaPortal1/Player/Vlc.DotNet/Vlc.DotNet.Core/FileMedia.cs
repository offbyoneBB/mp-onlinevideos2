using System;
using Vlc.DotNet.Core.Interop;

namespace Vlc.DotNet.Core
{
    public class FileMedia : MediaBase
    {
        private bool myIsInitialized;

        public string Path { get; set; }

        protected internal override IntPtr Initialize(IntPtr vlcClient)
        {
            if (myIsInitialized || string.IsNullOrEmpty(Path))
                return IntPtr.Zero;
            VlcMedia = LibVlcMethods.libvlc_media_new_path(vlcClient, Path);
            if (VlcMedia == IntPtr.Zero)
            {
                throw new NotImplementedException();
            }

            base.Initialize(vlcClient);

            myIsInitialized = true;
            return VlcMedia;
        }
    }
}
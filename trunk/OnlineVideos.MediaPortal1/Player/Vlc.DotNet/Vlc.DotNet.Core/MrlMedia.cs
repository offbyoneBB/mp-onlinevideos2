using System;
using Vlc.DotNet.Core.Interop;

namespace Vlc.DotNet.Core
{
    public class MrlMedia : MediaBase
    {
        private bool myIsInitialized;
        private string myMrl;

        public new string Mrl
        {
            get { return base.Mrl; }
            set { myMrl = value; }
        }

        protected internal override IntPtr Initialize(IntPtr vlcClient)
        {
            if (myIsInitialized || string.IsNullOrEmpty(myMrl))
                return IntPtr.Zero;
            VlcMedia = LibVlcMethods.libvlc_media_new_location(vlcClient, myMrl);
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
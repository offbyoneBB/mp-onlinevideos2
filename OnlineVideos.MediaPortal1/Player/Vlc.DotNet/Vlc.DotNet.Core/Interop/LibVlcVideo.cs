using System;

namespace Vlc.DotNet.Core.Interops
{
    public class LibVlcVideo : IDisposable
    {
        //public LibVlcFunction<Signatures.LibVlc.Video.Filter.GetList> GetAvailableVideoFilters { get; private set; }

        internal LibVlcVideo(IntPtr myLibVlcDllHandle, Version vlcVersion)
        {
            //GetAvailableVideoFilters = new LibVlcFunction<Signatures.LibVlc.Video.Filter.GetList>(myLibVlcDllHandle, "libvlc_video_filter_list_get");
        }

        #region IDisposable Members

        public void Dispose()
        {
            //GetAvailableVideoFilters = null;
        }

        #endregion
    }
}
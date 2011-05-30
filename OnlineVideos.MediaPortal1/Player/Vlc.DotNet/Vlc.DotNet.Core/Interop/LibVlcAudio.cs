using System;

namespace Vlc.DotNet.Core.Interops
{
    public sealed class LibVlcAudio : IDisposable
    {
        //public LibVlcFunction<Signatures.LibVlc.Audio.Filter.GetList> GetAvailableAudioFilters { get; private set; }

        internal LibVlcAudio(IntPtr myLibVlcDllHandle, Version vlcVersion)
        {
            //GetAvailableAudioFilters = new LibVlcFunction<Signatures.LibVlc.Audio.Filter.GetList>(myLibVlcDllHandle, "libvlc_audio_filter_list_get");
        }

        #region IDisposable Members

        public void Dispose()
        {
            //GetAvailableAudioFilters = null;
        }

        #endregion

    }
}

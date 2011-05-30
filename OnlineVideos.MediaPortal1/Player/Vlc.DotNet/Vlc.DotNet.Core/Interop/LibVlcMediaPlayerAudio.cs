using System;

namespace Vlc.DotNet.Core.Interops
{
    /// <summary>
    /// LibVlcMediaPlayerAudio class
    /// </summary>
    public sealed class LibVlcMediaPlayerAudio : IDisposable
    {
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.NewOutputListInstance> NewOutputListInstance { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.ReleaseOutputList> ReleaseOutputList { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.OutputDeviceCount> OutputDeviceCount { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.GetOutputDeviceLongName> GetOutputDeviceLongName { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.GetOutputDeviceIdName> GetOutputDeviceIdName { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.SetOutputDevice> SetOutputDevice { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.GetOutputDeviceType> GetOutputDeviceType { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.SetOutputDeviceType> SetOutputDeviceType { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.ToggleMute> ToggleMute { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.GetMute> GetMute { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.SetMute> SetMute { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.GetVolume> GetVolume { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.SetVolume> SetVolume { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.GetTrackCount> GetTrackCount { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.GetTrack> GetTrack { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.SetTrack> SetTrack { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.GetChannel> GetChannel { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.SetChannel> SetChannel { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.GetDelay> GetDelay { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.SetDelay> SetDelay { get; private set; }

        internal LibVlcMediaPlayerAudio(IntPtr libVlcDllHandle, Version vlcVersion)
        {
            NewOutputListInstance = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.NewOutputListInstance>(libVlcDllHandle, vlcVersion);
            ReleaseOutputList = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.ReleaseOutputList>(libVlcDllHandle, vlcVersion);
            OutputDeviceCount = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.OutputDeviceCount>(libVlcDllHandle, vlcVersion);
            GetOutputDeviceLongName = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.GetOutputDeviceLongName>(libVlcDllHandle, vlcVersion);
            GetOutputDeviceIdName = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.GetOutputDeviceIdName>(libVlcDllHandle, vlcVersion);
            SetOutputDevice = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.SetOutputDevice>(libVlcDllHandle, vlcVersion);
            GetOutputDeviceType = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.GetOutputDeviceType>(libVlcDllHandle, vlcVersion);
            SetOutputDeviceType = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.SetOutputDeviceType>(libVlcDllHandle, vlcVersion);
            ToggleMute = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.ToggleMute>(libVlcDllHandle, vlcVersion);
            GetMute = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.GetMute>(libVlcDllHandle, vlcVersion);
            SetMute = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.SetMute>(libVlcDllHandle, vlcVersion);
            GetVolume = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.GetVolume>(libVlcDllHandle, vlcVersion);
            SetVolume = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.SetVolume>(libVlcDllHandle, vlcVersion);
            GetTrackCount = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.GetTrackCount>(libVlcDllHandle, vlcVersion);
            GetTrack = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.GetTrack>(libVlcDllHandle, vlcVersion);
            SetTrack = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.SetTrack>(libVlcDllHandle, vlcVersion);
            GetChannel = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.GetChannel>(libVlcDllHandle, vlcVersion);
            SetChannel = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.SetChannel>(libVlcDllHandle, vlcVersion);
            GetDelay = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.GetDelay>(libVlcDllHandle, vlcVersion);
            SetDelay = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Audio.SetDelay>(libVlcDllHandle, vlcVersion);
        }

        public void Dispose()
        {
            NewOutputListInstance = null;
            ReleaseOutputList = null;
            OutputDeviceCount = null;
            GetOutputDeviceLongName = null;
            GetOutputDeviceIdName = null;
            SetOutputDevice = null;
            GetOutputDeviceType = null;
            SetOutputDeviceType = null;
            ToggleMute = null;
            GetMute = null;
            SetMute = null;
            GetVolume = null;
            SetVolume = null;
            GetTrackCount = null;
            GetTrack = null;
            SetTrack = null;
            GetChannel = null;
            SetChannel = null;
            GetDelay = null;
            SetDelay = null;
        }
    }
}
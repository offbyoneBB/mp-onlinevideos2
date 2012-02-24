using System;
using Vlc.DotNet.Core.Interops.Signatures.LibVlc.MediaListPlayer;

namespace Vlc.DotNet.Core.Interops
{
    public sealed class LibVlcMediaListPlayer : IDisposable
    {
        public LibVlcFunction<NewInstance> NewInstance { get; private set; }
        public LibVlcFunction<ReleaseInstance> ReleaseInstance { get; private set; }
        public LibVlcFunction<RetainInstance> RetainInstance { get; private set; }
        public LibVlcFunction<EventManager> EventManager { get; private set; }
        public LibVlcFunction<SetMediaPlayer> SetMediaPlayer { get; private set; }
        public LibVlcFunction<SetMediaList> SetMediaList { get; private set; }
        public LibVlcFunction<Play> Play { get; private set; }
        public LibVlcFunction<Pause> Pause { get; private set; }
        public LibVlcFunction<IsPlaying> IsPlaying { get; private set; }
        public LibVlcFunction<GetState> GetState { get; private set; }
        public LibVlcFunction<PlayItemAtIndex> PlayItemAtIndex { get; private set; }
        public LibVlcFunction<PlayItem> PlayItem { get; private set; }
        public LibVlcFunction<Stop> Stop { get; private set; }
        public LibVlcFunction<Next> Next { get; private set; }
        public LibVlcFunction<Previous> Previous { get; private set; }
        public LibVlcFunction<SetPlaybackMode> SetPlaybackMode { get; private set; }

        internal LibVlcMediaListPlayer(IntPtr libVlcDllHandle, Version vlcVersion)
        {
            NewInstance = new LibVlcFunction<NewInstance>(libVlcDllHandle, vlcVersion);
            ReleaseInstance = new LibVlcFunction<ReleaseInstance>(libVlcDllHandle, vlcVersion);
            RetainInstance = new LibVlcFunction<RetainInstance>(libVlcDllHandle, vlcVersion);
            EventManager = new LibVlcFunction<EventManager>(libVlcDllHandle, vlcVersion);
            SetMediaPlayer = new LibVlcFunction<SetMediaPlayer>(libVlcDllHandle, vlcVersion);
            SetMediaList = new LibVlcFunction<SetMediaList>(libVlcDllHandle, vlcVersion);
            Play = new LibVlcFunction<Play>(libVlcDllHandle, vlcVersion);
            Pause = new LibVlcFunction<Pause>(libVlcDllHandle, vlcVersion);
            IsPlaying = new LibVlcFunction<IsPlaying>(libVlcDllHandle, vlcVersion);
            GetState = new LibVlcFunction<GetState>(libVlcDllHandle, vlcVersion);
            PlayItemAtIndex = new LibVlcFunction<PlayItemAtIndex>(libVlcDllHandle, vlcVersion);
            PlayItem = new LibVlcFunction<PlayItem>(libVlcDllHandle, vlcVersion);
            Stop = new LibVlcFunction<Stop>(libVlcDllHandle, vlcVersion);
            Next = new LibVlcFunction<Next>(libVlcDllHandle, vlcVersion);
            Previous = new LibVlcFunction<Previous>(libVlcDllHandle, vlcVersion);
            SetPlaybackMode = new LibVlcFunction<SetPlaybackMode>(libVlcDllHandle, vlcVersion);
        }
        #region IDisposable Members

        public void Dispose()
        {
            NewInstance = null;
            ReleaseInstance = null;
            RetainInstance = null;
            EventManager = null;
            SetMediaPlayer = null;
            SetMediaList = null;
            Play = null;
            Pause = null;
            IsPlaying = null;
            GetState = null;
            PlayItemAtIndex = null;
            PlayItem = null;
            Stop = null;
            Next = null;
            Previous = null;
            SetPlaybackMode = null;
        }

        #endregion

    }
}

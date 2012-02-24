using System;

namespace Vlc.DotNet.Core.Interops
{
    public sealed class LibVlcMediaPlayer : IDisposable
    {
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.NewInstance> NewInstance { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.NewInstanceFromMedia> NewInstanceFromMedia { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.ReleaseInstance> ReleaseInstance { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.RetainInstance> RetainInstance { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.SetMedia> SetMedia { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.GetMedia> GetMedia { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.EventManager> EventManagerNewIntance { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.IsPlaying> IsPlaying { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Play> Play { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.SetPause> SetPause { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Pause> Pause { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Stop> Stop { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.SetHwnd> SetHwnd { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.GetHwnd> GetHwnd { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.GetLength> GetLength { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.GetTime> GetTime { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.SetTime> SetTime { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.GetPosition> GetPosition { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.SetPosition> SetPosition { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.SetChapter> SetChapter { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.GetChapter> GetChapter { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.GetChapterCount> GetChapterCount { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.WillPlay> WillPlay { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.GetChapterCountForTitle> GetChapterCountForTitle { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.SetTitle> SetTitle { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.GetTitle> GetTitle { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.GetTitleCount> GetTitleCount { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.SetPreviousChapter> SetPreviousChapter { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.SetNextChapter> SetNextChapter { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.GetRate> GetRate { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.SetRate> SetRate { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.GetState> GetState { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.GetFPS> GetFPS { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.HasVideoOut> HasVideoOut { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.IsSeekable> IsSeekable { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.IsPausable> IsPausable { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.NextFrame> NextFrame { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Navigate> Navigate { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.ReleaseTrackDescriptionList> ReleaseTrackDescriptionList { get; private set; }

        public LibVlcMediaPlayerAudio AudioInterops { get; private set; }
        public LibVlcMediaPlayerVideo VideoInterops { get; private set; }

        internal LibVlcMediaPlayer(IntPtr libVlcDllHandle, Version vlcVersion)
        {
            NewInstance = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.NewInstance>(libVlcDllHandle, vlcVersion);
            NewInstanceFromMedia = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.NewInstanceFromMedia>(libVlcDllHandle, vlcVersion);
            ReleaseInstance = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.ReleaseInstance>(libVlcDllHandle, vlcVersion);
            RetainInstance = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.RetainInstance>(libVlcDllHandle, vlcVersion);
            SetMedia = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.SetMedia>(libVlcDllHandle, vlcVersion);
            GetMedia = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.GetMedia>(libVlcDllHandle, vlcVersion);
            EventManagerNewIntance = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.EventManager>(libVlcDllHandle, vlcVersion);
            IsPlaying = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.IsPlaying>(libVlcDllHandle, vlcVersion);
            Play = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Play>(libVlcDllHandle, vlcVersion);
            SetPause = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.SetPause>(libVlcDllHandle, vlcVersion);
            Pause = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Pause>(libVlcDllHandle, vlcVersion);
            Stop = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Stop>(libVlcDllHandle, vlcVersion);
            SetHwnd = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.SetHwnd>(libVlcDllHandle, vlcVersion);
            GetHwnd = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.GetHwnd>(libVlcDllHandle, vlcVersion);
            GetLength = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.GetLength>(libVlcDllHandle, vlcVersion);
            GetTime = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.GetTime>(libVlcDllHandle, vlcVersion);
            SetTime = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.SetTime>(libVlcDllHandle, vlcVersion);
            GetPosition = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.GetPosition>(libVlcDllHandle, vlcVersion);
            SetPosition = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.SetPosition>(libVlcDllHandle, vlcVersion);
            SetChapter = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.SetChapter>(libVlcDllHandle, vlcVersion);
            GetChapter = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.GetChapter>(libVlcDllHandle, vlcVersion);
            GetChapterCount = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.GetChapterCount>(libVlcDllHandle, vlcVersion);
            WillPlay = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.WillPlay>(libVlcDllHandle, vlcVersion);
            GetChapterCountForTitle = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.GetChapterCountForTitle>(libVlcDllHandle, vlcVersion);
            SetTitle = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.SetTitle>(libVlcDllHandle, vlcVersion);
            GetTitle = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.GetTitle>(libVlcDllHandle, vlcVersion);
            GetTitleCount = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.GetTitleCount>(libVlcDllHandle, vlcVersion);
            SetPreviousChapter = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.SetPreviousChapter>(libVlcDllHandle, vlcVersion);
            SetNextChapter = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.SetNextChapter>(libVlcDllHandle, vlcVersion);
            GetRate = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.GetRate>(libVlcDllHandle, vlcVersion);
            SetRate = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.SetRate>(libVlcDllHandle, vlcVersion);
            GetState = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.GetState>(libVlcDllHandle, vlcVersion);
            GetFPS = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.GetFPS>(libVlcDllHandle, vlcVersion);
            HasVideoOut = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.HasVideoOut>(libVlcDllHandle, vlcVersion);
            IsSeekable = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.IsSeekable>(libVlcDllHandle, vlcVersion);
            IsPausable = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.IsPausable>(libVlcDllHandle, vlcVersion);
            NextFrame = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.NextFrame>(libVlcDllHandle, vlcVersion);
            Navigate = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Navigate>(libVlcDllHandle, vlcVersion);
            ReleaseTrackDescriptionList = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.ReleaseTrackDescriptionList>(libVlcDllHandle, vlcVersion);

            VideoInterops = new LibVlcMediaPlayerVideo(libVlcDllHandle, vlcVersion);
            AudioInterops = new LibVlcMediaPlayerAudio(libVlcDllHandle, vlcVersion);
        }

        public void Dispose()
        {
            NewInstance = null;
            NewInstanceFromMedia = null;
            ReleaseInstance = null;
            RetainInstance = null;
            SetMedia = null;
            GetMedia = null;
            EventManagerNewIntance = null;
            IsPlaying = null;
            Play = null;
            SetPause = null;
            Pause = null;
            Stop = null;
            SetHwnd = null;
            GetHwnd = null;
            GetLength = null;
            GetTime = null;
            SetTime = null;
            GetPosition = null;
            SetPosition = null;
            SetChapter = null;
            GetChapter = null;
            GetChapterCount = null;
            WillPlay = null;
            GetChapterCountForTitle = null;
            SetTitle = null;
            GetTitle = null;
            GetTitleCount = null;
            SetPreviousChapter = null;
            SetNextChapter = null;
            GetRate = null;
            SetRate = null;
            GetState = null;
            GetFPS = null;
            HasVideoOut = null;
            IsSeekable = null;
            IsPausable = null;
            NextFrame = null;
            Navigate = null;
            ReleaseTrackDescriptionList = null;

            if (VideoInterops != null)
                VideoInterops.Dispose();
            VideoInterops = null;
            if (AudioInterops != null)
                AudioInterops.Dispose();
            AudioInterops = null;
        }
    }
}

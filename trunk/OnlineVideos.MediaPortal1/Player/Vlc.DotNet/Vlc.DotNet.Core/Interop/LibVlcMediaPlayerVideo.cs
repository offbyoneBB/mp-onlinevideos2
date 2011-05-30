using System;

namespace Vlc.DotNet.Core.Interops
{
    /// <summary>
    /// LibVlcMediaPlayerVideo class
    /// </summary>
    public sealed class LibVlcMediaPlayerVideo : IDisposable
    {
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetCallbacks> SetCallbacks { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetFormatCallbacks> SetFormatCallbacks { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetFormat> SetFormat { get; private set; }

        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.ToggleFullscreen> ToggleFullscreen { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetFullscreen> SetFullscreen { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetFullscreen> GetFullscreen { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetKeyInput> SetKeyInput { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetSize> GetSize { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetCursor> GetCursor { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetScale> GetScale { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetScale> SetScale { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetAspectRatio> GetAspectRatio { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetAspectRatio> SetAspectRatio { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetSpu> GetSpu { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetSpuCount> GetSpuCount { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetSpuDescription> GetSpuDescription { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetSpu> SetSpu { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetSubtitleFile> SetSubtitleFile { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetTeletext> GetTeletext { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetTeletext> SetTeletext { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.ToggleTeletext> ToggleTeletext { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetTrackCount> GetTrackCount { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetTrack> GetTrack { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetTrack> SetTrack { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.TakeSnapshot> TakeSnapshot { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetDeinterlace> SetDeinterlace { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetIntegerMarquee> GetIntegerMarquee { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetStringMarquee> GetStringMarquee { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetIntegerMarquee> SetIntegerMarquee { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetStringMarquee> SetStringMarquee { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetIntegerLogoOption> GetIntegerLogoOption { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetIntegerLogoOption> SetIntegerLogoOption { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetStringLogoOption> SetStringLogoOption { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetIntegerAdjust> GetIntegerAdjust { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetIntegerAdjust> SetIntegerAdjust { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetFloatAdjust> GetFloatAdjust { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetFloatAdjust> SetFloatAdjust { get; private set; }

        internal LibVlcMediaPlayerVideo(IntPtr libVlcDllHandle, Version vlcVersion)
        {
            SetCallbacks = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetCallbacks>(libVlcDllHandle, vlcVersion);
            SetFormatCallbacks = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetFormatCallbacks>(libVlcDllHandle, vlcVersion);
            SetFormat = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetFormat>(libVlcDllHandle, vlcVersion);

            ToggleFullscreen = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.ToggleFullscreen>(libVlcDllHandle, vlcVersion);
            SetFullscreen = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetFullscreen>(libVlcDllHandle, vlcVersion);
            GetFullscreen = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetFullscreen>(libVlcDllHandle, vlcVersion);
            SetKeyInput = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetKeyInput>(libVlcDllHandle, vlcVersion);
            GetSize = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetSize>(libVlcDllHandle, vlcVersion);
            GetCursor = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetCursor>(libVlcDllHandle, vlcVersion);
            GetScale = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetScale>(libVlcDllHandle, vlcVersion);
            SetScale = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetScale>(libVlcDllHandle, vlcVersion);
            GetAspectRatio = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetAspectRatio>(libVlcDllHandle, vlcVersion);
            SetAspectRatio = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetAspectRatio>(libVlcDllHandle, vlcVersion);
            GetSpu = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetSpu>(libVlcDllHandle, vlcVersion);
            GetSpuCount = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetSpuCount>(libVlcDllHandle, vlcVersion);
            GetSpuDescription = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetSpuDescription>(libVlcDllHandle, vlcVersion);
            SetSpu = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetSpu>(libVlcDllHandle, vlcVersion);
            SetSubtitleFile = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetSubtitleFile>(libVlcDllHandle, vlcVersion);
            GetTeletext = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetTeletext>(libVlcDllHandle, vlcVersion);
            SetTeletext = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetTeletext>(libVlcDllHandle, vlcVersion);
            ToggleTeletext = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.ToggleTeletext>(libVlcDllHandle, vlcVersion);
            GetTrackCount = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetTrackCount>(libVlcDllHandle, vlcVersion);
            GetTrack = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetTrack>(libVlcDllHandle, vlcVersion);
            SetTrack = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetTrack>(libVlcDllHandle, vlcVersion);
            TakeSnapshot = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.TakeSnapshot>(libVlcDllHandle, vlcVersion);
            SetDeinterlace = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetDeinterlace>(libVlcDllHandle, vlcVersion);
            GetIntegerMarquee = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetIntegerMarquee>(libVlcDllHandle, vlcVersion);
            GetStringMarquee = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetStringMarquee>(libVlcDllHandle, vlcVersion);
            SetIntegerMarquee = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetIntegerMarquee>(libVlcDllHandle, vlcVersion);
            SetStringMarquee = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetStringMarquee>(libVlcDllHandle, vlcVersion);
            GetIntegerLogoOption = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetIntegerLogoOption>(libVlcDllHandle, vlcVersion);
            SetIntegerLogoOption = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetIntegerLogoOption>(libVlcDllHandle, vlcVersion);
            SetStringLogoOption = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetStringLogoOption>(libVlcDllHandle, vlcVersion);
            GetIntegerAdjust = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetIntegerAdjust>(libVlcDllHandle, vlcVersion);
            SetIntegerAdjust = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetIntegerAdjust>(libVlcDllHandle, vlcVersion);
            GetFloatAdjust = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.GetFloatAdjust>(libVlcDllHandle, vlcVersion);
            SetFloatAdjust = new LibVlcFunction<Signatures.LibVlc.MediaPlayer.Video.SetFloatAdjust>(libVlcDllHandle, vlcVersion);
        }

        public void Dispose()
        {
            SetCallbacks = null;
            SetFormatCallbacks = null;
            SetFormat = null;

            ToggleFullscreen = null;
            SetFullscreen = null;
            GetFullscreen = null;
            SetKeyInput = null;
            GetSize = null;
            GetCursor = null;
            GetScale = null;
            SetScale = null;
            GetAspectRatio = null;
            SetAspectRatio = null;
            GetSpu = null;
            GetSpuCount = null;
            GetSpuDescription = null;
            SetSpu = null;
            SetSubtitleFile = null;
            GetTeletext = null;
            SetTeletext = null;
            ToggleTeletext = null;
            GetTrackCount = null;
            GetTrack = null;
            SetTrack = null;
            TakeSnapshot = null;
            SetDeinterlace = null;
            GetIntegerMarquee = null;
            GetStringMarquee = null;
            SetIntegerMarquee = null;
            SetStringMarquee = null;
            GetIntegerLogoOption = null;
            SetIntegerLogoOption = null;
            SetStringLogoOption = null;
            GetIntegerAdjust = null;
            SetIntegerAdjust = null;
            GetFloatAdjust = null;
            SetFloatAdjust = null;
        }
    }
}
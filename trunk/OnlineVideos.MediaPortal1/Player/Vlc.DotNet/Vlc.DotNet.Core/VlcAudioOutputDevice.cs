using System;
using System.Runtime.InteropServices;
using Vlc.DotNet.Core.Interops.Signatures.LibVlc.MediaPlayer;

namespace Vlc.DotNet.Core
{
    /// <summary>
    /// VlcAudioOutput class
    /// </summary>
    public sealed class VlcAudioOutputDevice
    {
        /// <summary>
        /// Retreive the audio output name
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Retreive the audio output description
        /// </summary>
        public string Description { get; private set; }
        internal VlcAudioOutputDevice Next { get; private set; }

        /// <summary>
        /// VlcAudioOutput constrctor
        /// </summary>
        /// <param name="audioOutput">The libvlc audio output structure</param>
        internal VlcAudioOutputDevice(AudioOutput audioOutput)
        {
            Name = IntPtrExtensions.ToStringAnsi(audioOutput.name);
            Description = IntPtrExtensions.ToStringAnsi(audioOutput.description);
            if (audioOutput.next != IntPtr.Zero)
            {
                var next = (AudioOutput)Marshal.PtrToStructure(audioOutput.next, typeof(AudioOutput));
                Next = new VlcAudioOutputDevice(next);
            }
        }
    }
}

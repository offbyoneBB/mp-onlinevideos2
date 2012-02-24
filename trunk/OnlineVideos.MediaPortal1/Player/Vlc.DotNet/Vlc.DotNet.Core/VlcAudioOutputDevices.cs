using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Vlc.DotNet.Core.Interops.Signatures.LibVlc.MediaPlayer;

namespace Vlc.DotNet.Core
{
    /// <summary>
    /// VlcOutputDevices class
    /// </summary>
    public sealed class VlcAudioOutputDevices : IEnumerable<VlcAudioOutputDevice>, IDisposable
    {
        /// <summary>
        /// VlcOutputDevices constructor
        /// </summary>
        internal VlcAudioOutputDevices()
        {
        }

        public void Dispose()
        {
        }

        public IEnumerator<VlcAudioOutputDevice> GetEnumerator()
        {
            if (VlcContext.InteropManager != null &&
                VlcContext.InteropManager.MediaPlayerInterops != null &&
                VlcContext.InteropManager.MediaPlayerInterops.AudioInterops != null &&
                VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.NewOutputListInstance.IsAvailable &&
                VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.ReleaseOutputList.IsAvailable &&
                VlcContext.HandleManager.LibVlcHandle != IntPtr.Zero)
            {
                var outputDeviceListInstance = VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.NewOutputListInstance.Invoke(VlcContext.HandleManager.LibVlcHandle);

                if (outputDeviceListInstance != IntPtr.Zero)
                {
#if SILVERLIGHT
                    var audioOutput = new AudioOutput();
                    Marshal.PtrToStructure(outputDeviceListInstance, audioOutput);
#else
                    var audioOutput = (AudioOutput)Marshal.PtrToStructure(outputDeviceListInstance, typeof(AudioOutput));
#endif
                    var lst = new VlcAudioOutputDevice(audioOutput);

                    yield return lst;

                    while (lst.Next != null)
                    {
                        lst = lst.Next;
                        yield return lst;
                    }
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.ReleaseOutputList.Invoke(outputDeviceListInstance);
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            if (VlcContext.InteropManager != null &&
                VlcContext.InteropManager.MediaPlayerInterops != null &&
                VlcContext.InteropManager.MediaPlayerInterops.AudioInterops != null &&
                VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.NewOutputListInstance.IsAvailable &&
                VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.ReleaseOutputList.IsAvailable &&
                VlcContext.HandleManager.LibVlcHandle != IntPtr.Zero)
            {
                var outputDeviceListInstance = VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.NewOutputListInstance.Invoke(VlcContext.HandleManager.LibVlcHandle);

                if (outputDeviceListInstance != IntPtr.Zero)
                {
#if SILVERLIGHT
                    var audioOutput = new AudioOutput();
                    Marshal.PtrToStructure(outputDeviceListInstance, audioOutput);
#else
                    var audioOutput = (AudioOutput)Marshal.PtrToStructure(outputDeviceListInstance, typeof(AudioOutput));
#endif
                    var lst = new VlcAudioOutputDevice(audioOutput);

                    yield return lst;

                    while (lst.Next != null)
                    {
                        lst = lst.Next;
                        yield return lst;
                    }
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.ReleaseOutputList.Invoke(outputDeviceListInstance);
                }
            }
        }
    }
}
using System;
using System.ComponentModel;
using Vlc.DotNet.Core;
using Vlc.DotNet.Core.Interops.Signatures.LibVlc.MediaPlayer.Audio;

#if WPF
namespace Vlc.DotNet.Wpf
#elif SILVERLIGHT
namespace Vlc.DotNet.Silverlight
#else
namespace Vlc.DotNet.Forms
#endif
{
    /// <summary>
    /// VlcAudioProperties class
    /// </summary>
    public sealed class VlcAudioProperties : IDisposable
    {
        private readonly IVlcControl myHostVlcControl;

        /// <summary>
        /// Gets or sets a value indicating whether audio is muted
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public bool IsMute
        {
            get
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.GetMute.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(myHostVlcControl))
                {
                    return VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.GetMute.Invoke(VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl]) == 1;
                }

                return false;
            }

            set
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.SetMute.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(myHostVlcControl))
                {
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.SetMute.Invoke(VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl], value ? 1 : 0);
                }
            }
        }

        /// <summary>
        /// Gets or sets volume level
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public int Volume
        {
            get
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.GetVolume.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(myHostVlcControl))
                {
                    return VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.GetVolume.Invoke(VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl]);
                }

                return -1;
            }

            set
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.SetVolume.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(myHostVlcControl))
                {
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.SetVolume.Invoke(VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl], value);
                }
            }
        }

        /// <summary>
        /// Gets or sets audio delay in microseconds
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public long Delay
        {
            get
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.GetDelay.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(myHostVlcControl))
                {
                    return VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.GetDelay.Invoke(VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl]);
                }

                return 0;
            }

            set
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.SetDelay.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(myHostVlcControl))
                {
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.SetDelay.Invoke(VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl], value);
                }
            }
        }

        /// <summary>
        /// Gets or sets output device type
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public OutputDeviceTypes OutputDeviceType
        {
            get
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.GetOutputDeviceType.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(myHostVlcControl))
                {
                    return VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.GetOutputDeviceType.Invoke(VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl]);
                }

                return OutputDeviceTypes.Error;
            }

            set
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.SetOutputDeviceType.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(myHostVlcControl))
                {
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.SetOutputDeviceType.Invoke(VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl], value);
                }
            }
        }

        /// <summary>
        /// Gets or sets channel
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public OutputChannel Channel
        {
            get
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.GetChannel.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(myHostVlcControl))
                {
                    return VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.GetChannel.Invoke(VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl]);
                }

                return OutputChannel.Error;
            }

            set
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.SetChannel.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(myHostVlcControl))
                {
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.SetChannel.Invoke(VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl], value);
                }
            }
        }

        /// <summary>
        /// Gets or sets track
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public int Track
        {
            get
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.GetTrack.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(myHostVlcControl))
                {
                    return VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.GetTrack.Invoke(VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl]);
                }

                return -1;
            }

            set
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.SetTrack.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(myHostVlcControl))
                {
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.SetTrack.Invoke(VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl], value);
                }
            }
        }

        /// <summary>
        /// Gets the number of tracks
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public int TrackCount
        {
            get
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.GetTrackCount.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(myHostVlcControl))
                {
                    return VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.GetTrackCount.Invoke(VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl]);
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets output device name
        /// </summary>
        /// <param name="outputName"></param>
        /// <param name="deviceIndex"></param>
        /// <returns></returns>
        public string GetOutputDeviceIdName(string outputName, int deviceIndex)
        {
            if (VlcContext.InteropManager != null &&
                VlcContext.InteropManager.MediaPlayerInterops != null &&
                VlcContext.InteropManager.MediaPlayerInterops.AudioInterops != null &&
                VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.GetOutputDeviceIdName.IsAvailable &&
                VlcContext.HandleManager != null &&
                VlcContext.HandleManager.MediaPlayerHandles != null &&
                VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(myHostVlcControl))
            {
                return IntPtrExtensions.ToStringAnsi(VlcContext.InteropManager.MediaPlayerInterops.AudioInterops.GetOutputDeviceIdName.Invoke(VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl], outputName, deviceIndex));
            }

            return null;
        }

        internal VlcAudioProperties(IVlcControl vlcControl)
        {
            myHostVlcControl = vlcControl;
        }

        public void Dispose()
        {
        }
    }
}
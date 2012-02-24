using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Vlc.DotNet.Core;
using Vlc.DotNet.Core.Interops.Signatures.LibVlc.MediaPlayer;

#if WPF
using System.Windows;
namespace Vlc.DotNet.Wpf
#elif SILVERLIGHT
using System.Windows;
namespace Vlc.DotNet.Silverlight
#else
using System.Drawing;
namespace Vlc.DotNet.Forms
#endif
{
    /// <summary>
    /// VlcVideoProperties class
    /// </summary>
    public sealed class VlcVideoProperties : IDisposable
    {
        private readonly IVlcControl myHostVlcControl;

        /// <summary>
        /// Gets video size
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public Size Size
        {
            get
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.GetSize.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(myHostVlcControl))
                {
                    uint width, height;
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.GetSize.Invoke(
                        VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl],
                        0,
                        out width,
                        out height);

                    if (width <= 0 && height <= 0)
                    {
                        return Size.Empty;
                    }
#if WPF
                    return new Size(width, height);
#else
                    return new Size((int)width, (int)height);
#endif
                }

                return Size.Empty;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether video in fullscreen mode
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public bool IsFullscreen
        {
            get
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.GetFullscreen.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(myHostVlcControl))
                {
                    return VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.GetFullscreen.Invoke(
                        VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl]) != 0;
                }

                return false;
            }

            set
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.SetFullscreen.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(myHostVlcControl))
                {
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.SetFullscreen.Invoke(
                        VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl],
                        value ? 1 : 0);
                }
            }
        }

        /// <summary>
        /// Gets or sets video scale
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public float Scale
        {
            get
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.GetScale.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(myHostVlcControl))
                {
                    return VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.GetScale.Invoke(
                        VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl]);
                }

                return 0;
            }

            set
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.SetScale.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(myHostVlcControl))
                {
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.SetScale.Invoke(
                        VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl],
                        value);
                }
            }
        }

        /// <summary>
        /// Gets or sets video subtitle
        /// </summary>
        public int CurrentSpuIndex
        {
            get
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.GetSpu.IsAvailable &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(myHostVlcControl))
                {
                    return VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.GetSpu.Invoke(
                        VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl]);
                }

                return -1;
            }

            set
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.SetSpu.IsAvailable &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(myHostVlcControl))
                {
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.SetSpu.Invoke(
                        VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl],
                        value);
                }
            }
        }

        /// <summary>
        /// Gets the number of available video subtitles.
        /// </summary>
        public int SpuCount
        {
            get
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.GetSpuCount.IsAvailable &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(myHostVlcControl))
                {
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.GetSpuCount.Invoke(
                        VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl]);
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets the description of available video subtitles.
        /// </summary>
        public VlcTrackDescription SpuDescription
        {
            get
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.GetSpuDescription.IsAvailable &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(myHostVlcControl))
                {
                    var ptr = VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.GetSpuDescription.Invoke(VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl]);
                    if (ptr != IntPtr.Zero)
                    {
#if SILVERLIGHT
                        var td = new TrackDescription();
                        Marshal.PtrToStructure(ptr, td);
#else
                        var td = (TrackDescription)Marshal.PtrToStructure(ptr, typeof(TrackDescription));
#endif
                        return new VlcTrackDescription(td);
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Gets or sets video aspect ration
        /// </summary>
        public string AspectRatio
        {
            get
            {
                string aspect = null;
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.GetAspectRatio.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.EventManagerHandles.ContainsKey(myHostVlcControl))
                {
                    aspect = IntPtrExtensions.ToStringAnsi(VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.GetAspectRatio.Invoke(
                        VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl]));
                }

                return string.IsNullOrEmpty(aspect) ? string.Empty : aspect;
            }

            set
            {
                if (!string.IsNullOrEmpty(value) &&
                    VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.SetAspectRatio.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.EventManagerHandles.ContainsKey(myHostVlcControl))
                {
                    VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.SetAspectRatio.Invoke(
                        VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl],
                        value);
                }
            }
        }

        /// <summary>
        /// Set subtitle file
        /// </summary>
        /// <param name="subtitleFile">The subtitle file</param>
        public void SetSubtitleFile(string subtitleFile)
        {
            if (!string.IsNullOrEmpty(subtitleFile) &&
                File.Exists(subtitleFile) &&
                VlcContext.InteropManager != null &&
                VlcContext.InteropManager.MediaPlayerInterops != null &&
                VlcContext.InteropManager.MediaPlayerInterops.VideoInterops != null &&
                VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.SetSubtitleFile.IsAvailable &&
                VlcContext.HandleManager.MediaPlayerHandles != null &&
                VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(myHostVlcControl))
            {
                VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.SetSubtitleFile.Invoke(
                    VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl],
                    subtitleFile);
            }
        }

        /// <summary>
        /// Set deinterlace mode
        /// </summary>
        /// <param name="mode">Mode of deinterlace</param>
        public void SetDeinterlaceMode(VlcDeinterlaceModes mode)
        {
            if (VlcContext.HandleManager.LibVlcHandle != IntPtr.Zero &&
                VlcContext.InteropManager != null &&
                VlcContext.InteropManager.MediaPlayerInterops != null &&
                VlcContext.InteropManager.MediaPlayerInterops.VideoInterops != null &&
                VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.SetDeinterlace.IsAvailable)
            {
                VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.SetDeinterlace.Invoke(
                    VlcContext.HandleManager.MediaPlayerHandles[myHostVlcControl],
                    mode != VlcDeinterlaceModes.None ? mode.ToString().ToLower() : null);
            }
        }

        internal VlcVideoProperties(IVlcControl vlcControl)
        {
            myHostVlcControl = vlcControl;
        }

        public void Dispose()
        {
        }
    }
}
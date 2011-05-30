using System;
using System.Text;

namespace Vlc.DotNet.Core.Medias
{
    /// <summary>
    /// Path media
    /// </summary>
    public sealed class PathMedia : MediaBase
    {
        /// <summary>
        /// Constructor of path media
        /// </summary>
        /// <param name="path">The path of the media</param>
        public PathMedia(string path)
        {
            Path = path;
            Initialize();
        }

        /// <summary>
        /// Retreive the specified path of the media
        /// </summary>
        public string Path { get; private set; }

        protected override IntPtr GetNewMediaInstance()
        {
            if (!string.IsNullOrEmpty(Path) &&
                VlcContext.InteropManager != null &&
                VlcContext.InteropManager.MediaInterops != null &&
                VlcContext.InteropManager.MediaInterops.NewInstanceFromPath.IsAvailable &&
                VlcContext.HandleManager != null &&
                VlcContext.HandleManager.LibVlcHandle != IntPtr.Zero)
            {
                return VlcContext.InteropManager.MediaInterops.NewInstanceFromPath.Invoke(VlcContext.HandleManager.LibVlcHandle, Encoding.UTF8.GetBytes(Path));
            }
            return IntPtr.Zero;
        }
    }
}
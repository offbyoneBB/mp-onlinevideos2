using System;

namespace Vlc.DotNet.Core.Medias
{
    /// <summary>
    /// Empty media class
    /// </summary>
    public sealed class EmptyMedia : MediaBase
    {
        /// <summary>
        /// Constructor of EmptyMedia
        /// </summary>
        /// <param name="name"></param>
        public EmptyMedia(string name)
        {
            Name = name;
            Initialize();
        }

        /// <summary>
        /// Retreive the specified name of the media
        /// </summary>
        public string Name { get; private set; }

        protected override IntPtr GetNewMediaInstance()
        {
            if (!string.IsNullOrEmpty(Name) &&
                VlcContext.InteropManager != null &&
                VlcContext.InteropManager.MediaInterops != null &&
                VlcContext.InteropManager.MediaInterops.NewInstanceFromPath.IsAvailable &&
                VlcContext.HandleManager != null &&
                VlcContext.HandleManager.LibVlcHandle != IntPtr.Zero)
            {
                return VlcContext.InteropManager.MediaInterops.NewInstanceEmpty.Invoke(VlcContext.HandleManager.LibVlcHandle, Name);
            }
            return IntPtr.Zero;
        }
    }
}
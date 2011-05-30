using System;

namespace Vlc.DotNet.Core.Medias
{
    /// <summary>
    /// LocationMedia class
    /// </summary>
    public sealed class LocationMedia : MediaBase
    {
        /// <summary>
        /// LocationMedia constructor
        /// </summary>
        /// <param name="location"></param>
        public LocationMedia(string location)
        {
            Location = location;
            Initialize();
        }

        /// <summary>
        /// Retreive the specified location of the media
        /// </summary>
        public string Location { get; private set; }

        protected override IntPtr GetNewMediaInstance()
        {
            if (!string.IsNullOrEmpty(Location) &&
                VlcContext.InteropManager != null &&
                VlcContext.InteropManager.MediaInterops != null &&
                VlcContext.InteropManager.MediaInterops.NewInstanceFromPath.IsAvailable &&
                VlcContext.HandleManager != null &&
                VlcContext.HandleManager.LibVlcHandle != IntPtr.Zero)
            {
                return VlcContext.InteropManager.MediaInterops.NewInstanceFromLocation.Invoke(VlcContext.HandleManager.LibVlcHandle, Location);
            }
            return IntPtr.Zero;
        }
    }
}
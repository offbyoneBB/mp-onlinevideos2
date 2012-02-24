using System;
using System.Collections.Generic;

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
        internal LocationMedia(IntPtr handle)
            : base(handle)
        {
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

        /// <summary>
        /// Retrive list of sub media items.
        /// </summary>
        public IList<LocationMedia> SubItems
        {
            get
            {
                var result = new List<LocationMedia>();
                if (VlcContext.HandleManager.MediasHandles.ContainsKey(this) &&
                    VlcContext.InteropManager.MediaInterops.GetSubItems.IsAvailable)
                {
                    var data = VlcContext.InteropManager.MediaInterops.GetSubItems.Invoke(VlcContext.HandleManager.MediasHandles[this]);
                    if (data == IntPtr.Zero)
                        return result;
                    var count = VlcContext.InteropManager.MediaListInterops.Count.Invoke(data);
                    for (var cpt = 0; cpt < count; cpt++)
                    {
                        result.Add(new LocationMedia(VlcContext.InteropManager.MediaListInterops.GetItemAt.Invoke(data, cpt)));
                    }
                }
                return result;
            }
        }

    }
}
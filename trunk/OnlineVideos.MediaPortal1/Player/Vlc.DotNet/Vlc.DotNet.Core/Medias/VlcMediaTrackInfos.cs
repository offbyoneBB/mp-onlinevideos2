using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Vlc.DotNet.Core.Interops.Signatures.LibVlc.Media;

namespace Vlc.DotNet.Core.Medias
{
    public sealed class VlcMediaTrackInfos : IEnumerable<MediaTrackInfo>, IDisposable
    {
        private MediaBase myMediaBase;

        /// <summary>
        /// VlcOutputDevices constructor
        /// </summary>
        internal VlcMediaTrackInfos(MediaBase mediaBase)
        {
            myMediaBase = mediaBase;
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// Returns the collection of media track informations
        /// </summary>
        /// <returns>Media track informations</returns>
        public IEnumerator<MediaTrackInfo> GetEnumerator()
        {
            if (VlcContext.InteropManager.MediaInterops.IsParsed.Invoke(VlcContext.HandleManager.MediasHandles[myMediaBase]) == 0)
            {
                VlcContext.InteropManager.MediaInterops.Parse.Invoke(VlcContext.HandleManager.MediasHandles[myMediaBase]);
            }

            if (VlcContext.HandleManager.MediasHandles.ContainsKey(myMediaBase))
            {
                IntPtr mediaInfoPtr;

                var count =
                    VlcContext.InteropManager.MediaInterops.GetTrackInfo.Invoke(
                        VlcContext.HandleManager.MediasHandles[myMediaBase], out mediaInfoPtr);
                try
                {
                    if (count > 0)
                    {
                        var currentMediaTrackInfosPtr = mediaInfoPtr;
                        var size = Marshal.SizeOf(typeof(MediaTrackInfo));

                        if (currentMediaTrackInfosPtr != IntPtr.Zero)
                        {
                            for (int i = 0; i < count; i++)
                            {
                                yield return (MediaTrackInfo)Marshal.PtrToStructure(
                                    currentMediaTrackInfosPtr,
                                    typeof(MediaTrackInfo));

                                currentMediaTrackInfosPtr = new IntPtr(currentMediaTrackInfosPtr.ToInt64() + size);
                            }
                        }
                    }
                }
                finally
                {
                    if (VlcContext.InteropManager.FreeMemory.IsAvailable)
                    {
                        VlcContext.InteropManager.FreeMemory.Invoke(mediaInfoPtr);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the collection of media track informations
        /// </summary>
        /// <returns>Media track informations</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            if (VlcContext.InteropManager.MediaInterops.IsParsed.Invoke(VlcContext.HandleManager.MediasHandles[myMediaBase]) == 0)
            {
                VlcContext.InteropManager.MediaInterops.Parse.Invoke(VlcContext.HandleManager.MediasHandles[myMediaBase]);
            }

            if (VlcContext.HandleManager.MediasHandles.ContainsKey(myMediaBase))
            {
                IntPtr mediaInfoPtr;

                var count =
                    VlcContext.InteropManager.MediaInterops.GetTrackInfo.Invoke(
                        VlcContext.HandleManager.MediasHandles[myMediaBase], out mediaInfoPtr);
                try
                {
                    if (count > 0)
                    {
                        var currentMediaTrackInfosPtr = mediaInfoPtr;
                        var size = Marshal.SizeOf(typeof(MediaTrackInfo));

                        if (currentMediaTrackInfosPtr != IntPtr.Zero)
                        {
                            for (int i = 0; i < count; i++)
                            {
                                yield return (MediaTrackInfo)Marshal.PtrToStructure(
                                    currentMediaTrackInfosPtr,
                                    typeof(MediaTrackInfo));

                                currentMediaTrackInfosPtr = new IntPtr(currentMediaTrackInfosPtr.ToInt64() + size);
                            }
                        }
                    }
                }
                finally
                {
                    if (VlcContext.InteropManager.FreeMemory.IsAvailable)
                    {
                        VlcContext.InteropManager.FreeMemory.Invoke(mediaInfoPtr);
                    }
                }
            }
        }
    }
}

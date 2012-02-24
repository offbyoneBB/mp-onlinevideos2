using System;
using System.Collections.Generic;

namespace Vlc.DotNet.Core
{
    internal class VlcMediaList : IList<Medias.MediaBase>, IDisposable
    {
        internal VlcMediaList()
        {
            VlcContext.HandleManager.MediasListHandles.Add(this, VlcContext.InteropManager.MediaListInterops.NewInstance.Invoke(VlcContext.HandleManager.LibVlcHandle));
        }
        public int IndexOf(Medias.MediaBase item)
        {
            if (VlcContext.HandleManager.MediasListHandles.ContainsKey(this))
            {
                return VlcContext.InteropManager.MediaListInterops.IndexOf.Invoke(VlcContext.HandleManager.MediasListHandles[this], VlcContext.HandleManager.MediasHandles[item]);
            }
            return -1;
        }
        public void Insert(int index, Medias.MediaBase item)
        {
            if (VlcContext.HandleManager.MediasListHandles.ContainsKey(this) && VlcContext.HandleManager.MediasHandles.ContainsKey(item))
            {
                VlcContext.InteropManager.MediaListInterops.InsertMedia.Invoke(
                    VlcContext.HandleManager.MediasListHandles[this],
                    VlcContext.HandleManager.MediasHandles[item], index);
            }
        }

        public void RemoveAt(int index)
        {
            if (VlcContext.HandleManager.MediasListHandles.ContainsKey(this))
            {
                VlcContext.InteropManager.MediaListInterops.RemoveAt.Invoke(VlcContext.HandleManager.MediasListHandles[this], index);
            }
        }

        public Medias.MediaBase this[int index]
        {
            get
            {
                if (VlcContext.HandleManager.MediasListHandles.ContainsKey(this))
                {
                    var mediaHandle = VlcContext.InteropManager.MediaListInterops.GetItemAt.Invoke(VlcContext.HandleManager.MediasListHandles[this], index);
                    if (VlcContext.HandleManager.MediasHandles.ContainsValue(mediaHandle))
                    {
                        foreach (var medias in VlcContext.HandleManager.MediasHandles)
                        {
                            if (medias.Value == mediaHandle)
                                return medias.Key;
                        }
                    }
                }
                return null;
            }
            set
            {
                if (VlcContext.HandleManager.MediasListHandles.ContainsKey(this))
                {
                    VlcContext.InteropManager.MediaListInterops.SetMedia.Invoke(VlcContext.HandleManager.MediasListHandles[this], VlcContext.HandleManager.MediasHandles[value]);
                }
            }
        }

        public void Add(Medias.MediaBase item)
        {
            if (VlcContext.HandleManager.MediasListHandles.ContainsKey(this) &&
                !IsReadOnly)
            {
                VlcContext.InteropManager.MediaListInterops.AddMedia.Invoke(VlcContext.HandleManager.MediasListHandles[this], VlcContext.HandleManager.MediasHandles[item]);
            }
        }

        public void Clear()
        {
            if (VlcContext.HandleManager.MediasListHandles.ContainsKey(this))
            {
                for (int index = Count - 1; index > 0; index--)
                {
                    VlcContext.InteropManager.MediaListInterops.RemoveAt.Invoke(VlcContext.HandleManager.MediasListHandles[this], index);
                }
            }
        }

        public bool Contains(Medias.MediaBase item)
        {
            return IndexOf(item) > -1;
        }

        public void CopyTo(Medias.MediaBase[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get
            {
                if (VlcContext.HandleManager.MediasListHandles.ContainsKey(this))
                {
                    return VlcContext.InteropManager.MediaListInterops.Count.Invoke(VlcContext.HandleManager.MediasListHandles[this]);
                }
                return -1;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                if (VlcContext.HandleManager.MediasListHandles.ContainsKey(this))
                {
                    return VlcContext.InteropManager.MediaListInterops.IsReadOnly.Invoke(VlcContext.HandleManager.MediasListHandles[this]) == 1;
                }
                return true;
            }
        }

        public bool Remove(Medias.MediaBase item)
        {
            if (VlcContext.HandleManager.MediasListHandles.ContainsKey(this))
            {
                var index = IndexOf(item);
                if (index >= 0)
                {
                    RemoveAt(index);
                    return true;
                }
            }
            return false;
        }

        public IEnumerator<Medias.MediaBase> GetEnumerator()
        {
            if (VlcContext.HandleManager.MediasListHandles.ContainsKey(this))
            {
                for (var index = 0; index < Count; index++)
                {
                    var mediaHandle = VlcContext.InteropManager.MediaListInterops.GetItemAt.Invoke(VlcContext.HandleManager.MediasListHandles[this], index);
                    if (mediaHandle != IntPtr.Zero)
                    {
                        if (VlcContext.HandleManager.MediasHandles.ContainsValue(mediaHandle))
                        {
                            foreach (var media in VlcContext.HandleManager.MediasHandles)
                            {
                                if (media.Value == mediaHandle)
                                    yield return media.Key;
                            }
                        }
                    }
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            VlcContext.InteropManager.MediaListInterops.ReleaseInstance.Invoke(VlcContext.HandleManager.MediasListHandles[this]);
            VlcContext.HandleManager.MediasListHandles.Remove(this);
            GC.SuppressFinalize(this);
        }
    }
}

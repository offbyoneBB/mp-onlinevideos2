using System;
using System.Collections.Generic;
using Vlc.DotNet.Core.Interops.Signatures.LibVlc.MediaListPlayer;

namespace Vlc.DotNet.Core
{
    public class VlcMediaListPlayer : IList<Medias.MediaBase>, IDisposable
    {
        private IVlcControl myVlcControlHost;
        private VlcMediaList myVlcMediaList;
        internal VlcMediaListPlayer(IVlcControl vlcControl)
        {
            var handle = VlcContext.InteropManager.MediaListPlayerInterops.NewInstance.Invoke(VlcContext.HandleManager.LibVlcHandle);
            VlcContext.HandleManager.MediasListPlayerHandles.Add(this, handle);
            myVlcControlHost = vlcControl;
            myVlcMediaList = new VlcMediaList();

            VlcContext.InteropManager.MediaListPlayerInterops.SetMediaList.Invoke(
                VlcContext.HandleManager.MediasListPlayerHandles[this],
                VlcContext.HandleManager.MediasListHandles[myVlcMediaList]);

            VlcContext.InteropManager.MediaListPlayerInterops.SetMediaPlayer.Invoke(
                VlcContext.HandleManager.MediasListPlayerHandles[this],
                VlcContext.HandleManager.MediaPlayerHandles[myVlcControlHost]);
        }

        public void Dispose()
        {
            if (VlcContext.HandleManager.MediasListPlayerHandles[this] != IntPtr.Zero)
            {
                VlcContext.InteropManager.MediaListPlayerInterops.ReleaseInstance.Invoke(VlcContext.HandleManager.MediasListPlayerHandles[this]);
                VlcContext.HandleManager.MediasListPlayerHandles.Remove(this);
            }
        }

        public int Count
        {
            get { return myVlcMediaList.Count; }
        }
        public Medias.MediaBase this[int index]
        {
            get { return myVlcMediaList[index]; }
            set { myVlcMediaList[index] = value; }
        }

        public void Add(Medias.MediaBase media)
        {
            myVlcMediaList.Add(media);
        }
        public void Clear()
        {
            myVlcMediaList.Clear();
        }
        public bool Contains(Medias.MediaBase media)
        {
            return myVlcMediaList.Contains(media);
        }
        public void CopyTo(Medias.MediaBase[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }
        public IEnumerator<Medias.MediaBase> GetEnumerator()
        {
            return myVlcMediaList.GetEnumerator();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return myVlcMediaList.GetEnumerator();
        }
        public int IndexOf(Medias.MediaBase item)
        {
            return myVlcMediaList.IndexOf(item);
        }
        public void Insert(int index, Medias.MediaBase item)
        {
            myVlcMediaList.Insert(index, item);
        }
        public bool IsReadOnly
        {
            get
            {
                return myVlcMediaList.IsReadOnly;
            }
        }
        public bool Remove(Medias.MediaBase item)
        {
            return myVlcMediaList.Remove(item);
        }
        public void RemoveAt(int index)
        {
            myVlcMediaList.RemoveAt(index);
        }

        internal void Play()
        {
            VlcContext.InteropManager.MediaListPlayerInterops.Play.Invoke(VlcContext.HandleManager.MediasListPlayerHandles[this]);
        }
        internal void Play(Medias.MediaBase media)
        {
            var index = IndexOf(media);
            if (index > -1)
            {
                VlcContext.InteropManager.MediaListPlayerInterops.PlayItemAtIndex.Invoke(VlcContext.HandleManager.MediasListPlayerHandles[this], index);
            }
            else
            {
                Clear();
                Add(media);
                VlcContext.InteropManager.MediaListPlayerInterops.PlayItemAtIndex.Invoke(VlcContext.HandleManager.MediasListPlayerHandles[this], 0);
            }
            
        }
        internal void Stop()
        {
            VlcContext.InteropManager.MediaListPlayerInterops.Stop.Invoke(VlcContext.HandleManager.MediasListPlayerHandles[this]);
        }
        internal void Next()
        {
            VlcContext.InteropManager.MediaListPlayerInterops.Next.Invoke(VlcContext.HandleManager.MediasListPlayerHandles[this]);
        }
        internal void Previous()
        {
            VlcContext.InteropManager.MediaListPlayerInterops.Previous.Invoke(VlcContext.HandleManager.MediasListPlayerHandles[this]);
        }
        internal void Pause()
        {
            VlcContext.InteropManager.MediaListPlayerInterops.Pause.Invoke(VlcContext.HandleManager.MediasListPlayerHandles[this]);
        }
        internal void SetPlaybackMode(PlaybackModes mode)
        {
            VlcContext.InteropManager.MediaListPlayerInterops.SetPlaybackMode.Invoke(VlcContext.HandleManager.MediasListPlayerHandles[this], mode);
        }
    }
}

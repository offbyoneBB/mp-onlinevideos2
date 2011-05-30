using System;

namespace Vlc.DotNet.Core.Interops
{
    /// <summary>
    /// LibVlcMediaList class
    /// </summary>
    public sealed class LibVlcMediaList : IDisposable
    {
        public LibVlcFunction<Signatures.LibVlc.MediaList.NewInstance> NewInstance { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaList.ReleaseInstance> ReleaseInstance { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaList.RetainInstance> RetainInstance { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaList.SetMedia> SetMedia { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaList.GetMedia> GetMedia { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaList.AddMedia> AddMedia { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaList.InsertMedia> InsertMedia { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaList.RemoveAt> RemoveAt { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaList.Count> Count { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaList.GetItemAt> GetItemAt { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaList.IndexOf> IndexOf { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaList.IsReadOnly> IsReadOnly { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaList.Lock> Lock { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaList.Unlock> Unlock { get; private set; }
        public LibVlcFunction<Signatures.LibVlc.MediaList.EventManager> EventManager { get; private set; }

        internal LibVlcMediaList(IntPtr libVlcDllHandle, Version vlcVersion)
        {
            NewInstance = new LibVlcFunction<Signatures.LibVlc.MediaList.NewInstance>(libVlcDllHandle, vlcVersion);
            ReleaseInstance = new LibVlcFunction<Signatures.LibVlc.MediaList.ReleaseInstance>(libVlcDllHandle, vlcVersion);
            RetainInstance = new LibVlcFunction<Signatures.LibVlc.MediaList.RetainInstance>(libVlcDllHandle, vlcVersion);
            SetMedia = new LibVlcFunction<Signatures.LibVlc.MediaList.SetMedia>(libVlcDllHandle, vlcVersion);
            GetMedia = new LibVlcFunction<Signatures.LibVlc.MediaList.GetMedia>(libVlcDllHandle, vlcVersion);
            AddMedia = new LibVlcFunction<Signatures.LibVlc.MediaList.AddMedia>(libVlcDllHandle, vlcVersion);
            InsertMedia = new LibVlcFunction<Signatures.LibVlc.MediaList.InsertMedia>(libVlcDllHandle, vlcVersion);
            RemoveAt = new LibVlcFunction<Signatures.LibVlc.MediaList.RemoveAt>(libVlcDllHandle, vlcVersion);
            Count = new LibVlcFunction<Signatures.LibVlc.MediaList.Count>(libVlcDllHandle, vlcVersion);
            GetItemAt = new LibVlcFunction<Signatures.LibVlc.MediaList.GetItemAt>(libVlcDllHandle, vlcVersion);
            IndexOf = new LibVlcFunction<Signatures.LibVlc.MediaList.IndexOf>(libVlcDllHandle, vlcVersion);
            IsReadOnly = new LibVlcFunction<Signatures.LibVlc.MediaList.IsReadOnly>(libVlcDllHandle, vlcVersion);
            Lock = new LibVlcFunction<Signatures.LibVlc.MediaList.Lock>(libVlcDllHandle, vlcVersion);
            Unlock = new LibVlcFunction<Signatures.LibVlc.MediaList.Unlock>(libVlcDllHandle, vlcVersion);
            EventManager = new LibVlcFunction<Signatures.LibVlc.MediaList.EventManager>(libVlcDllHandle, vlcVersion);
        }

        #region IDisposable Members

        public void Dispose()
        {
            NewInstance = null;
            ReleaseInstance = null;
            RetainInstance = null;
            SetMedia = null;
            GetMedia = null;
            AddMedia = null;
            InsertMedia = null;
            RemoveAt = null;
            Count = null;
            GetItemAt = null;
            IndexOf = null;
            IsReadOnly = null;
            Lock = null;
            Unlock = null;
            EventManager = null;
        }

        #endregion
    }
}

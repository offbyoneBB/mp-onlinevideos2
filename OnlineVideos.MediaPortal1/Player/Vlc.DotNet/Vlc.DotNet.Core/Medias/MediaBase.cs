using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Vlc.DotNet.Core.Interops.Signatures.LibVlc.AsynchronousEvents;
using Vlc.DotNet.Core.Interops.Signatures.LibVlc.Media;

namespace Vlc.DotNet.Core.Medias
{
    /// <summary>
    /// Media abstract base class
    /// </summary>
    public abstract class MediaBase : IDisposable
    {
        private EventCallbackDelegate myEventCallback;
        private GCHandle myEventCallbackHandle;
        private IntPtr myEventManagerHandle;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaBase"/> class. 
        /// </summary>
        protected MediaBase()
        {
            Metadatas = new VlcMediaMetadatas(this);
            TrackInfos = new VlcMediaTrackInfos(this);
        }

        /// <summary>
        /// Retreive the duration of the media
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public TimeSpan Duration
        {
            get
            {
                if (!VlcContext.HandleManager.MediasHandles.ContainsKey(this))
                    return TimeSpan.Zero;
                long duration = VlcContext.InteropManager.MediaInterops.GetDuration.Invoke(VlcContext.HandleManager.MediasHandles[this]);
                if (duration == -1)
                    return TimeSpan.Zero;
                return new TimeSpan(0, 0, 0, 0, (int)duration);
            }
        }

        /// <summary>
        /// Gets Media Resource Locator
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public string MRL
        {
            get
            {
                if (!VlcContext.HandleManager.MediasHandles.ContainsKey(this))
                    return null;
                return VlcContext.InteropManager.MediaInterops.GetMrl.Invoke(VlcContext.HandleManager.MediasHandles[this]);
            }
        }

        /// <summary>
        /// Gets the current state of the media
        /// </summary>
        public States State
        {
            get
            {
                if (!VlcContext.HandleManager.MediasHandles.ContainsKey(this))
                    return States.NothingSpecial;
                return VlcContext.InteropManager.MediaInterops.GetState.Invoke(VlcContext.HandleManager.MediasHandles[this]);
            }
        }

        /// <summary>
        /// Gets the current statistics about the media
        /// </summary>
        public Stats Statistics
        {
            get
            {
                var stats = new Stats();
                if (VlcContext.HandleManager.MediasHandles.ContainsKey(this) &&
                    VlcContext.InteropManager.MediaInterops.GetStats.IsAvailable)
                {
                    VlcContext.InteropManager.MediaInterops.GetStats.Invoke(VlcContext.HandleManager.MediasHandles[this], out stats);
                }

                return stats;
            }
        }

        /// <summary>
        /// Gets media descriptor's elementary streams description
        /// </summary>
        public VlcMediaTrackInfos TrackInfos { get; internal set; }

        /// <summary>
        /// Gets the meta of the media
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public VlcMediaMetadatas Metadatas { get; private set; }

        protected abstract IntPtr GetNewMediaInstance();

        #region IDisposable Members

        public void Dispose()
        {
            if (VlcContext.HandleManager.MediasHandles.ContainsKey(this))
            {
                FreeEvents();
                VlcContext.InteropManager.MediaInterops.ReleaseInstance.Invoke(VlcContext.HandleManager.MediasHandles[this]);
            }
            VlcContext.HandleManager.MediasHandles.Remove(this);
        }

        #endregion

        /// <summary>
        /// Initialize media
        /// </summary>
        protected void Initialize()
        {
            IntPtr handle = GetNewMediaInstance();
            if (handle == IntPtr.Zero)
                return;
            VlcContext.HandleManager.MediasHandles[this] = handle;
            InitEvents();
        }

        /// <summary>
        /// Add option for media
        /// </summary>
        /// <param name="option">The options (as a string)</param>
        public void AddOption(string option)
        {
            if (!VlcContext.HandleManager.MediasHandles.ContainsKey(this))
                throw new Exception("Cannot set option while media is not initialized yet.");

            VlcContext.InteropManager.MediaInterops.AddOption.Invoke(VlcContext.HandleManager.MediasHandles[this], option);
        }

        /// <summary>
        /// Add option for media
        /// </summary>
        /// <param name="option">The options (as a string)</param>
        /// <param name="flag">The flags for this option</param>
        public void AddOption(string option, Option flag)
        {
            if (!VlcContext.HandleManager.MediasHandles.ContainsKey(this))
                throw new Exception("Cannot set option while media is not initialized yet.");

            VlcContext.InteropManager.MediaInterops.AddOptionFlag.Invoke(VlcContext.HandleManager.MediasHandles[this], option, flag);
        }

        #region Events

        private void InitEvents()
        {
            if (!VlcContext.HandleManager.MediasHandles.ContainsKey(this))
                return;
            myEventManagerHandle = VlcContext.InteropManager.MediaInterops.EventManager.Invoke(VlcContext.HandleManager.MediasHandles[this]);

            myEventCallback = OnVlcEvent;
            myEventCallbackHandle = GCHandle.Alloc(myEventCallback);

            VlcContext.InteropManager.EventInterops.Attach.Invoke(myEventManagerHandle, EventTypes.MediaDurationChanged, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Attach.Invoke(myEventManagerHandle, EventTypes.MediaFreed, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Attach.Invoke(myEventManagerHandle, EventTypes.MediaMetaChanged, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Attach.Invoke(myEventManagerHandle, EventTypes.MediaParsedChanged, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Attach.Invoke(myEventManagerHandle, EventTypes.MediaStateChanged, myEventCallback, IntPtr.Zero);
            //VlcContext.InteropManager.EventInterops.Attach.Invoke(myEventManagerHandle, EventTypes.MediaSubItemAdded, myEventCallback, IntPtr.Zero);
        }

        private void FreeEvents()
        {
            VlcContext.InteropManager.EventInterops.Detach.Invoke(myEventManagerHandle, EventTypes.MediaDurationChanged, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Detach.Invoke(myEventManagerHandle, EventTypes.MediaFreed, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Detach.Invoke(myEventManagerHandle, EventTypes.MediaMetaChanged, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Detach.Invoke(myEventManagerHandle, EventTypes.MediaParsedChanged, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Detach.Invoke(myEventManagerHandle, EventTypes.MediaStateChanged, myEventCallback, IntPtr.Zero);
            //VlcContext.InteropManager.EventInterops.Detach.Invoke(myEventManagerHandle, EventTypes.MediaSubItemAdded, myEventCallback, IntPtr.Zero);

            myEventCallbackHandle.Free();
        }

        private void OnVlcEvent(ref LibVlcEventArgs eventData, IntPtr userData)
        {
            switch (eventData.Type)
            {
                case EventTypes.MediaDurationChanged:
                    EventsHelper.RaiseEvent(DurationChanged, this, new VlcEventArgs<long>(eventData.MediaDurationChanged.NewDuration));
                    break;
                case EventTypes.MediaFreed:
                    EventsHelper.RaiseEvent(Freed, this, new VlcEventArgs<EventArgs>(EventArgs.Empty));
                    break;
                case EventTypes.MediaMetaChanged:
                    EventsHelper.RaiseEvent(MetaChanged, this, new VlcEventArgs<Metadatas>(eventData.MediaMetaChanged.MetaType));
                    break;
                case EventTypes.MediaParsedChanged:
                    EventsHelper.RaiseEvent(ParsedChanged, this, new VlcEventArgs<int>(eventData.MediaParsedChanged.NewStatus));
                    break;
                case EventTypes.MediaStateChanged:
                    EventsHelper.RaiseEvent(StateChanged, this, new VlcEventArgs<States>(eventData.MediaStateChanged.NewState));
                    break;
                //TODO
                //case EventTypes.MediaSubItemAdded:
                //    //eventData.MediaSubitemAdded.NewChild
                //    EventsHelper.RaiseEvent(MediaSubItemAdded, this, new VlcEventArgs<EventArgs>(EventArgs.Empty));
                //    break;
            }
        }

        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public event VlcEventHandler<MediaBase, long> DurationChanged;

        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public event VlcEventHandler<MediaBase, EventArgs> Freed;

        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public event VlcEventHandler<MediaBase, Metadatas> MetaChanged;

        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public event VlcEventHandler<MediaBase, int> ParsedChanged;

        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public event VlcEventHandler<MediaBase, States> StateChanged;

        //[Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        //public event VlcEventHandler<MediaBase, EventArgs> MediaSubItemAdded;

        #endregion
    }
}
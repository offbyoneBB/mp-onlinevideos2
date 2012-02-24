using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Vlc.DotNet.Core;
using Vlc.DotNet.Core.Interops.Signatures.LibVlc.AsynchronousEvents;
using Vlc.DotNet.Core.Interops.Signatures.LibVlc.Media;
using Vlc.DotNet.Core.Medias;
using Vlc.DotNet.Core.Interops.Signatures.LibVlc.MediaListPlayer;

#if WPF
namespace Vlc.DotNet.Wpf
#elif SILVERLIGHT
namespace Vlc.DotNet.Silverlight
#else
namespace Vlc.DotNet.Forms
#endif
{
    /// <summary>
    /// Vlc control to play media
    /// </summary>
    public sealed partial class VlcControl : IVlcControl
    {
        private EventCallbackDelegate myEventCallback;
        private GCHandle myEventCallbackHandle;

        /// <summary>
        /// Gets the FPS of the video
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public float FPS
        {
            get
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.GetFPS.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(this) &&
                    IsPlaying)
                {
                    return VlcContext.InteropManager.MediaPlayerInterops.GetFPS.Invoke(VlcContext.HandleManager.MediaPlayerHandles[this]);
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether player able to play
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public bool WillPlay
        {
            get
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.WillPlay.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(this))
                {
                    return VlcContext.InteropManager.MediaPlayerInterops.WillPlay.Invoke(VlcContext.HandleManager.MediaPlayerHandles[this]) != 0;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this media player seekable
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public bool IsSeekable
        {
            get
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.IsSeekable.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(this))
                {
                    return VlcContext.InteropManager.MediaPlayerInterops.IsSeekable.Invoke(VlcContext.HandleManager.MediaPlayerHandles[this]) != 0;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this media player can be paused
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public bool IsPausable
        {
            get
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.IsPausable.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(this))
                {
                    return VlcContext.InteropManager.MediaPlayerInterops.IsPausable.Invoke(VlcContext.HandleManager.MediaPlayerHandles[this]) != 0;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether media is playing
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public bool IsPlaying
        {
            get
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.IsPlaying.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(this))
                {
                    return VlcContext.InteropManager.MediaPlayerInterops.IsPlaying.Invoke(VlcContext.HandleManager.MediaPlayerHandles[this]) == 1;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether media is paused
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public bool IsPaused
        {
            get
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.GetState.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(this))
                {
                    return VlcContext.InteropManager.MediaPlayerInterops.GetState.Invoke(VlcContext.HandleManager.MediaPlayerHandles[this]) == States.Paused;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets or sets the current position of the playing media
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
#if !SILVERLIGHT
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#endif
        public float Position
        {
            get
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.GetPosition.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(this))
                {
                    return VlcContext.InteropManager.MediaPlayerInterops.GetPosition.Invoke(VlcContext.HandleManager.MediaPlayerHandles[this]);
                }

                return 0;
            }

            set
            {
                if (value < 0 || value > 1)
                {
                    throw new ArgumentOutOfRangeException("value", "must be between 0 and 1");
                }
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.SetPosition.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(this))
                {
                    VlcContext.InteropManager.MediaPlayerInterops.SetPosition.Invoke(VlcContext.HandleManager.MediaPlayerHandles[this], value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the current time of the playing media
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
#if !SILVERLIGHT
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#endif
        public TimeSpan Time
        {
            get
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.GetTime.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(this) &&
                    IsPlaying)
                {
                    long time = VlcContext.InteropManager.MediaPlayerInterops.GetTime.Invoke(VlcContext.HandleManager.MediaPlayerHandles[this]);
                    if (time == -1)
                        return TimeSpan.Zero;
                    return TimeSpan.FromMilliseconds(time);
                }
                return TimeSpan.Zero;
            }

            set
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.SetTime.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(this))
                {
                    VlcContext.InteropManager.MediaPlayerInterops.SetTime.Invoke(VlcContext.HandleManager.MediaPlayerHandles[this], Convert.ToUInt32(value.TotalMilliseconds));
                }
            }
        }

        /// <summary>
        /// Gets or sets rate of playing
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public float Rate
        {
            get
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.GetRate.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null)
                {
                    return VlcContext.InteropManager.MediaPlayerInterops.GetRate.Invoke(VlcContext.HandleManager.MediaPlayerHandles[this]);
                }

                return 0;
            }

            set
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.SetRate.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null)
                {
                    VlcContext.InteropManager.MediaPlayerInterops.SetRate.Invoke(VlcContext.HandleManager.MediaPlayerHandles[this], value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the current media
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        [Browsable(false)]
#if !SILVERLIGHT
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#endif
        public MediaBase Media
        {
            get
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.GetMedia.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(this) &&
                    VlcContext.HandleManager.MediasHandles != null)
                {
                    var media = VlcContext.InteropManager.MediaPlayerInterops.GetMedia.Invoke(VlcContext.HandleManager.MediaPlayerHandles[this]);
                    if (VlcContext.HandleManager.MediasHandles.ContainsValue(media))
                    {
                        foreach (var mediasHandle in VlcContext.HandleManager.MediasHandles)
                        {
                            if (mediasHandle.Value == media)
                            {
                                return mediasHandle.Key;
                            }
                        }
                    }
                }

                return null;
            }

            set
            {
                //if (value != null && 
                //    VlcContext.InteropManager != null &&
                //    VlcContext.InteropManager.MediaPlayerInterops != null &&
                //    VlcContext.InteropManager.MediaPlayerInterops.SetMedia.IsAvailable &&
                //    VlcContext.HandleManager != null &&
                //    VlcContext.HandleManager.MediaPlayerHandles != null &&
                //    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(this) &&
                //    VlcContext.HandleManager.MediasHandles != null &&
                //    VlcContext.HandleManager.MediasHandles.ContainsKey(value))
                //{
                //    VlcContext.InteropManager.MediaPlayerInterops.SetMedia.Invoke(VlcContext.HandleManager.MediaPlayerHandles[this], VlcContext.HandleManager.MediasHandles[value]);
                //}
                if(value == null)
                    return;
                Stop();
                Medias.Clear();
                Medias.Add(value);
                Play(value);
            }
        }

        /// <summary>
        /// Gets media player state
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public States State
        {
            get
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.GetState.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(this))
                {
                    return VlcContext.InteropManager.MediaPlayerInterops.GetState.Invoke(VlcContext.HandleManager.MediaPlayerHandles[this]);
                }
                return States.NothingSpecial;
            }
        }

        /// <summary>
        /// Gets current media duration
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public TimeSpan Duration
        {
            get
            {
                if (VlcContext.InteropManager != null &&
                    VlcContext.InteropManager.MediaPlayerInterops != null &&
                    VlcContext.InteropManager.MediaPlayerInterops.GetLength.IsAvailable &&
                    VlcContext.HandleManager != null &&
                    VlcContext.HandleManager.MediaPlayerHandles != null &&
                    VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(this))
                {
                    long duration = VlcContext.InteropManager.MediaPlayerInterops.GetLength.Invoke(VlcContext.HandleManager.MediaPlayerHandles[this]);
                    if (duration == -1)
                        return TimeSpan.Zero;
                    return new TimeSpan(0, 0, 0, 0, (int)duration);
                }

                return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Display the next frame if supported
        /// </summary>
        public void NextFrame()
        {
            if (VlcContext.InteropManager != null &&
                VlcContext.InteropManager.MediaPlayerInterops != null &&
                VlcContext.InteropManager.MediaPlayerInterops.NextFrame.IsAvailable &&
                VlcContext.HandleManager != null &&
                VlcContext.HandleManager.MediaPlayerHandles != null &&
                VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(this))
            {
                VlcContext.InteropManager.MediaPlayerInterops.NextFrame.Invoke(VlcContext.HandleManager.MediaPlayerHandles[this]);
            }
        }

        ///// <summary>
        ///// Play the current media
        ///// </summary>
        //public void Play()
        //{
        //    if (VlcContext.InteropManager != null &&
        //        VlcContext.InteropManager.MediaPlayerInterops != null &&
        //        VlcContext.InteropManager.MediaPlayerInterops.Play.IsAvailable &&
        //        VlcContext.HandleManager != null &&
        //        VlcContext.HandleManager.MediaPlayerHandles != null &&
        //        VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(this))
        //    {
        //        VlcContext.InteropManager.MediaPlayerInterops.Play.Invoke(VlcContext.HandleManager.MediaPlayerHandles[this]);
        //    }
        //}

        ///// <summary>
        ///// Play the media
        ///// </summary>
        ///// <param name="media">Media to play</param>
        //public void Play(MediaBase media)
        //{
        //    Media = media;
        //    Play();
        //}

        ///// <summary>
        ///// Pause the current media
        ///// </summary>
        //public void Pause()
        //{
        //    if (VlcContext.InteropManager != null &&
        //        VlcContext.InteropManager.MediaPlayerInterops != null &&
        //        VlcContext.InteropManager.MediaPlayerInterops.Pause.IsAvailable &&
        //        VlcContext.HandleManager != null &&
        //        VlcContext.HandleManager.MediaPlayerHandles != null &&
        //        VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(this))
        //    {
        //        VlcContext.InteropManager.MediaPlayerInterops.Pause.Invoke(VlcContext.HandleManager.MediaPlayerHandles[this]);
        //    }
        //}

        ///// <summary>
        ///// Stop the current media
        ///// </summary>
        //public void Stop()
        //{
        //    if ((IsPlaying || IsPaused) &&
        //        VlcContext.InteropManager != null &&
        //        VlcContext.InteropManager.MediaPlayerInterops != null &&
        //        VlcContext.InteropManager.MediaPlayerInterops.Stop.IsAvailable &&
        //        VlcContext.HandleManager != null &&
        //        VlcContext.HandleManager.MediaPlayerHandles != null &&
        //        VlcContext.HandleManager.MediaPlayerHandles.ContainsKey(this))
        //    {
        //        VlcContext.InteropManager.MediaPlayerInterops.Stop.Invoke(VlcContext.HandleManager.MediaPlayerHandles[this]);
        //    }
        //}

        /// <summary>
        /// Play the current media
        /// </summary>
        public void Play()
        {
            Medias.Play();
        }
        /// <summary>
        /// Play a media
        /// </summary>
        public void Play(MediaBase media)
        {
            Medias.Play(media);
        }
        /// <summary>
        /// Stop the current media
        /// </summary>
        public void Stop()
        {
            Medias.Stop();
        }
        /// <summary>
        /// Play next media in Medias
        /// </summary>
        public void Next()
        {
            Medias.Next();
        }
        /// <summary>
        /// Play previous media
        /// </summary>
        public void Previous()
        {
            Medias.Previous();
        }
        /// <summary>
        /// Pause the current media
        /// </summary>
        public void Pause()
        {
            Medias.Pause();
        }

        /// <summary>
        /// Take snapshot
        /// </summary>
        /// <param name="filePath">The file path</param>
        /// <param name="width">The width of the snapshot</param>
        /// <param name="height">The height of the snapshot</param>
        public void TakeSnapshot(string filePath, uint width, uint height)
        {
            if (VlcContext.InteropManager != null &&
                VlcContext.InteropManager.MediaPlayerInterops != null &&
                VlcContext.InteropManager.MediaPlayerInterops.VideoInterops != null &&
                VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.TakeSnapshot.IsAvailable)
            {
                new Thread(() => VlcContext.InteropManager.MediaPlayerInterops.VideoInterops.TakeSnapshot.Invoke(VlcContext.HandleManager.MediaPlayerHandles[this], 0, Encoding.UTF8.GetBytes(filePath), width, height)).Start();
            }
        }

        #region Events
        private void InitEvents()
        {
            VlcContext.HandleManager.EventManagerHandles[this] = VlcContext.InteropManager.MediaPlayerInterops.EventManagerNewIntance.Invoke(VlcContext.HandleManager.MediaPlayerHandles[this]);

            myEventCallback = OnVlcEvent;
            myEventCallbackHandle = GCHandle.Alloc(myEventCallback);

            VlcContext.InteropManager.EventInterops.Attach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerBackward, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Attach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerBuffering, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Attach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerEncounteredError, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Attach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerEndReached, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Attach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerForward, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Attach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerLengthChanged, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Attach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerMediaChanged, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Attach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerOpening, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Attach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerPausableChanged, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Attach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerPaused, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Attach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerPlaying, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Attach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerPositionChanged, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Attach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerSeekableChanged, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Attach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerSnapshotTaken, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Attach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerStopped, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Attach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerTimeChanged, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Attach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerTitleChanged, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Attach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerVideoOutChanged, myEventCallback, IntPtr.Zero);
        }
        private void FreeEvents()
        {
            if (myEventCallback == null)
                return;
            VlcContext.InteropManager.EventInterops.Detach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerBackward, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Detach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerBuffering, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Detach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerEncounteredError, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Detach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerEndReached, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Detach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerForward, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Detach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerLengthChanged, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Detach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerMediaChanged, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Detach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerOpening, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Detach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerPausableChanged, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Detach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerPaused, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Detach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerPlaying, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Detach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerPositionChanged, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Detach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerSeekableChanged, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Detach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerSnapshotTaken, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Detach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerStopped, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Detach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerTimeChanged, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Detach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerTitleChanged, myEventCallback, IntPtr.Zero);
            VlcContext.InteropManager.EventInterops.Detach.Invoke(VlcContext.HandleManager.EventManagerHandles[this], EventTypes.MediaPlayerVideoOutChanged, myEventCallback, IntPtr.Zero);

            myEventCallbackHandle.Free();
        }

        [AllowReversePInvokeCalls]
        private void OnVlcEvent(ref LibVlcEventArgs eventData, IntPtr userData)
        {
            switch (eventData.Type)
            {
                case EventTypes.MediaPlayerBackward:
                    EventsHelper.RaiseEvent(Backward, this, new VlcEventArgs<EventArgs>(EventArgs.Empty));
                    break;
                case EventTypes.MediaPlayerBuffering:
                    EventsHelper.RaiseEvent(Buffering, this, new VlcEventArgs<float>(eventData.MediaPlayerBuffering.NewCache));
                    break;
                case EventTypes.MediaPlayerEncounteredError:
                    EventsHelper.RaiseEvent(EncounteredError, this, new VlcEventArgs<EventArgs>(EventArgs.Empty));
                    break;
                case EventTypes.MediaPlayerEndReached:
                    EventsHelper.RaiseEvent(EndReached, this, new VlcEventArgs<EventArgs>(EventArgs.Empty));
                    break;
                case EventTypes.MediaPlayerForward:
                    EventsHelper.RaiseEvent(Forward, this, new VlcEventArgs<EventArgs>(EventArgs.Empty));
                    break;
                case EventTypes.MediaPlayerLengthChanged:
                    EventsHelper.RaiseEvent(LengthChanged, this, new VlcEventArgs<long>(eventData.MediaPlayerLengthChanged.NewLength));
                    break;
                case EventTypes.MediaPlayerMediaChanged:
                    //TODO
                    //EventsHelper.RaiseEvent(MediaChanged, this, new VlcEventArgs<MediaBase>(eventData.MediaPlayerMediaChanged.NewMediaHandle));
                    break;
                case EventTypes.MediaPlayerNothingSpecial:
                    break;
                case EventTypes.MediaPlayerOpening:
                    break;
                case EventTypes.MediaPlayerPausableChanged:
                    EventsHelper.RaiseEvent(PausableChanged, this, new VlcEventArgs<int>(eventData.MediaPlayerPausableChanged.NewPausable));
                    break;
                case EventTypes.MediaPlayerPaused:
                    EventsHelper.RaiseEvent(Paused, this, new VlcEventArgs<EventArgs>(EventArgs.Empty));
                    break;
                case EventTypes.MediaPlayerPlaying:
                    EventsHelper.RaiseEvent(Playing, this, new VlcEventArgs<EventArgs>(EventArgs.Empty));
                    break;
                case EventTypes.MediaPlayerPositionChanged:
                    EventsHelper.RaiseEvent(PositionChanged, this, new VlcEventArgs<float>(eventData.MediaPlayerPositionChanged.NewPosition));
                    break;
                case EventTypes.MediaPlayerSeekableChanged:
                    EventsHelper.RaiseEvent(SeekableChanged, this, new VlcEventArgs<int>(eventData.MediaPlayerSeekableChanged.NewSeekable));
                    break;
                case EventTypes.MediaPlayerSnapshotTaken:
                    EventsHelper.RaiseEvent(SnapshotTaken, this, new VlcEventArgs<string>(IntPtrExtensions.ToStringAnsi(eventData.MediaPlayerSnapshotTaken.pszFilename)));
                    break;
                case EventTypes.MediaPlayerStopped:
                    EventsHelper.RaiseEvent(Stopped, this, new VlcEventArgs<EventArgs>(EventArgs.Empty));
                    break;
                case EventTypes.MediaPlayerTimeChanged:
                    EventsHelper.RaiseEvent(TimeChanged, this, new VlcEventArgs<TimeSpan>(TimeSpan.FromMilliseconds(eventData.MediaPlayerTimeChanged.NewTime)));
                    break;
                case EventTypes.MediaPlayerTitleChanged:
                    EventsHelper.RaiseEvent(TitleChanged, this, new VlcEventArgs<long>(eventData.MediaPlayerTitleChanged.NewTitle));
                    break;
                case EventTypes.MediaPlayerVideoOutChanged:
                    EventsHelper.RaiseEvent(VideoOutChanged, this, new VlcEventArgs<int>(eventData.MediaPlayerVideoOutChanged.NewCount));
                    break;
            }
        }

        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public event VlcEventHandler<VlcControl, EventArgs> Backward;

        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public event VlcEventHandler<VlcControl, float> Buffering;

        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public event VlcEventHandler<VlcControl, EventArgs> EncounteredError;

        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public event VlcEventHandler<VlcControl, EventArgs> EndReached;

        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public event VlcEventHandler<VlcControl, EventArgs> Forward;

        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public event VlcEventHandler<VlcControl, long> LengthChanged;

        //TODO
        //[Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        //public event VlcEventHandler<MediaBase> MediaChanged;

        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public event VlcEventHandler<VlcControl, int> PausableChanged;

        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public event VlcEventHandler<VlcControl, EventArgs> Paused;

        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public event VlcEventHandler<VlcControl, EventArgs> Playing;

        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public event VlcEventHandler<VlcControl, float> PositionChanged;

        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public event VlcEventHandler<VlcControl, int> SeekableChanged;

        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public event VlcEventHandler<VlcControl, string> SnapshotTaken;

        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public event VlcEventHandler<VlcControl, EventArgs> Stopped;

        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public event VlcEventHandler<VlcControl, TimeSpan> TimeChanged;

        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public event VlcEventHandler<VlcControl, long> TitleChanged;

        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public event VlcEventHandler<VlcControl, int> VideoOutChanged;

        #endregion

        /// <summary>
        /// Get audio properties
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public VlcAudioProperties AudioProperties { get; private set; }

        /// <summary>
        /// Get video properties
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public VlcVideoProperties VideoProperties { get; private set; }

        /// <summary>
        /// Get log properties
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public VlcLogProperties LogProperties { get; private set; }

        /// <summary>
        /// Get available output devices
        /// </summary>
        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
        public VlcAudioOutputDevices AudioOutputDevices { get; private set; }

        [Category(CommonStrings.VLC_DOTNET_PROPERTIES_CATEGORY)]
#if !SILVERLIGHT
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
#endif
        public VlcMediaListPlayer Medias { get; private set; }

        public PlaybackModes PlaybackMode
        {
            set
            {
                if(Medias != null)
                    Medias.SetPlaybackMode(value);
            }
        }        
    }
}
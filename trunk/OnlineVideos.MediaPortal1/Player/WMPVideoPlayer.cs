using System;
using System.Drawing;
using AxWMPLib;
using WMPLib;
using ExternalOSDLibrary;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using MediaPortal.Dialogs;

namespace OnlineVideos.MediaPortal1.Player
{
    public class WMPVideoPlayer : IPlayer, OVSPLayer
    {
        AxWindowsMediaPlayer wmpCtrl;
        PlayState playState;
        OSDController osd;
        string currentFile;
        bool bufferCompleted;

        public override bool Play(string strFile)
        {
            Log.Instance.Info("WMPVideoPlayer.Play '{0}'", strFile);

            currentFile = strFile;

            wmpCtrl = new AxWindowsMediaPlayer();
            GUIGraphicsContext.form.Controls.Add(wmpCtrl);
            wmpCtrl.Enabled = false;
            wmpCtrl.uiMode = "none";
            wmpCtrl.windowlessVideo = true;
            wmpCtrl.enableContextMenu = false;
            wmpCtrl.Ctlenabled = false;
            wmpCtrl.Visible = false;
            wmpCtrl.stretchToFit = true;

            wmpCtrl.PlayStateChange += WMP_OnPlayStateChange;
            wmpCtrl.Buffering += WMP_OnBuffering;
            wmpCtrl.ErrorEvent += WMP_OnError;
            
            wmpCtrl.URL = strFile;
            wmpCtrl.network.bufferingTime = PluginConfiguration.Instance.wmpbuffer;
            wmpCtrl.Ctlcontrols.play();

            GUIWindowManager.OnNewAction += GUIWindowManager_OnNewAction;
            
            GUIWaitCursor.Init(); GUIWaitCursor.Show(); // init and show the wait cursor while buffering
            return true;
        }

        void GUIWindowManager_OnNewAction(MediaPortal.GUI.Library.Action action)
        {
            if (Playing)
            {
                // IPlayer.Volume Property is never set by MediaPortal -> need to trap volume changes this way
                switch (action.wID)
                {
                    case MediaPortal.GUI.Library.Action.ActionType.ACTION_VOLUME_MUTE:
                        wmpCtrl.settings.mute = VolumeHandler.Instance.IsMuted;
                        break;
                    case MediaPortal.GUI.Library.Action.ActionType.ACTION_VOLUME_DOWN:
                        wmpCtrl.settings.volume = (int)((double)MediaPortal.Player.VolumeHandler.Instance.Previous / MediaPortal.Player.VolumeHandler.Instance.Maximum * 100);
                        break;
                    case MediaPortal.GUI.Library.Action.ActionType.ACTION_VOLUME_UP:
                        wmpCtrl.settings.volume = (int)((double)VolumeHandler.Instance.Next / MediaPortal.Player.VolumeHandler.Instance.Maximum * 100);
                        break;
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public override void Process()
        {
            if (wmpCtrl != null)
            {
                if (Initializing)
                {
                    if (!bufferCompleted && wmpCtrl.playState.Equals(WMPPlayState.wmppsPlaying)) bufferCompleted = true;

                    if (wmpCtrl.playState.Equals(WMPPlayState.wmppsReady))
                    {
                        GUIWaitCursor.Hide(); // hide the wait cursor
                        Log.Instance.Info("WMPVideoPlayer: error encountered while trying to play {0}", CurrentFile);
                        bufferCompleted = true;
                        PlaybackEnded();
                    }
                    else if (bufferCompleted)
                    {
                        GUIWaitCursor.Hide(); // hide the wait cursor
                        wmpCtrl.Visible = true;
                        playState = PlayState.Playing;
                        if (GoFullscreen) GUIWindowManager.ActivateWindow(GUIOnlineVideoFullscreen.WINDOW_FULLSCREEN_ONLINEVIDEO);
                        GUIMessage msgPb = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAYBACK_STARTED, 0, 0, 0, 0, 0, null);
                        msgPb.Label = CurrentFile;
                        GUIWindowManager.SendThreadMessage(msgPb);
                        SetVideoWindow();
                        osd = OSDController.Instance;
                    }
                }
                else if (Playing && osd != null) osd.UpdateGUI();
            }
        }

        public override void SetVideoWindow()
        {
            if (wmpCtrl != null)
            {
                System.Action si = () =>
                {
                    wmpCtrl.Location = new Point(FullScreen ? 0 : GUIGraphicsContext.VideoWindow.X, FullScreen ? 0 : GUIGraphicsContext.VideoWindow.Y);
                    wmpCtrl.ClientSize = new Size(FullScreen ? GUIGraphicsContext.Width : GUIGraphicsContext.VideoWindow.Width, FullScreen ? GUIGraphicsContext.Height : GUIGraphicsContext.VideoWindow.Height);
                };

                if (wmpCtrl.InvokeRequired)
                {
                    IAsyncResult iar = wmpCtrl.BeginInvoke(si);
                    iar.AsyncWaitHandle.WaitOne();
                }
                else
                {
                    si();
                }

                _videoRectangle = new Rectangle(wmpCtrl.Location.X, wmpCtrl.Location.Y, wmpCtrl.ClientSize.Width, wmpCtrl.ClientSize.Height);
                _sourceRectangle = _videoRectangle;
            }
        }

        private void PlaybackEnded()
        {
            Log.Instance.Info("WMPVideoPlayer:ended {0}", currentFile);
            playState = PlayState.Ended;
            if (wmpCtrl != null)
            {
                wmpCtrl.Location = new Point(0,0);
                wmpCtrl.ClientSize = new Size(0, 0);
                wmpCtrl.Visible = false;
                wmpCtrl.Buffering -= WMP_OnBuffering;
                wmpCtrl.PlayStateChange -= WMP_OnPlayStateChange;
                wmpCtrl.ErrorEvent -= WMP_OnError;
            }
        }

        public override void Pause()
        {
            if (wmpCtrl != null)
            {
                if (playState == PlayState.Paused)
                {
                    playState = PlayState.Playing;
                    wmpCtrl.Ctlcontrols.play();
                }
                else if (playState == PlayState.Playing)
                {
                    wmpCtrl.Ctlcontrols.pause();
                    if (wmpCtrl.playState == WMPPlayState.wmppsPaused)
                    {
                        playState = PlayState.Paused;
                    }
                }
            }
        }

        public override void Stop()
        {
            if (wmpCtrl != null)
            {
                wmpCtrl.Ctlcontrols.stop();
                PlaybackEnded();
            }
        }

        public override void SeekRelative(double dTime)
        {
            if (wmpCtrl != null && playState != PlayState.Init)
            {
                dTime = CurrentPosition + dTime;
                if (dTime < 0.0d) dTime = 0.0d;
                if (dTime < Duration)
                {
                    wmpCtrl.Ctlcontrols.currentPosition = dTime;
                }
            }
        }

        public override void SeekAbsolute(double dTime)
        {
            if (wmpCtrl != null && playState != PlayState.Init)
            {
                if (dTime < 0.0d) dTime = 0.0d;
                if (dTime < Duration)
                {
                    wmpCtrl.Ctlcontrols.currentPosition = dTime;
                }
            }
        }

        public override void SeekRelativePercentage(int iPercentage)
        {
            if (wmpCtrl != null && playState != PlayState.Init)
            {
                double fCurPercent = (CurrentPosition / Duration) * 100.0d;
                double fOnePercent = Duration / 100.0d;
                fCurPercent = fCurPercent + (double)iPercentage;
                fCurPercent *= fOnePercent;
                if (fCurPercent < 0.0d) fCurPercent = 0.0d;
                if (fCurPercent < Duration)
                {
                    wmpCtrl.Ctlcontrols.currentPosition = fCurPercent;
                }
            }
        }

        public override void SeekAsolutePercentage(int iPercentage)
        {
            if (wmpCtrl != null && playState != PlayState.Init)
            {
                if (iPercentage < 0) iPercentage = 0;
                else if (iPercentage >= 100) iPercentage = 100;
                double fPercent = Duration / 100.0f;
                fPercent *= (double)iPercentage;
                wmpCtrl.Ctlcontrols.currentPosition = fPercent;
            }
        }

        #region overridden Properties

        public override double Duration
        {
            get
            {
                if (wmpCtrl != null && wmpCtrl.currentMedia != null) try { return wmpCtrl.currentMedia.duration; } catch {}
                return 0.0d;
            }
        }

        public override double CurrentPosition
        {
            get
            {
                if (wmpCtrl != null && !Initializing) try { return wmpCtrl.Ctlcontrols.currentPosition; } catch {}
                return 0.0d;
            }
        }

        public override int Speed
        {
            get
            {
                if (playState == PlayState.Init || playState == PlayState.Ended || wmpCtrl == null || !wmpCtrl.settings.get_isAvailable("Rate"))
                    return 1;
                else
                    return (int)wmpCtrl.settings.rate;
            }
            set
            {
                if (playState != PlayState.Init && playState != PlayState.Ended && wmpCtrl != null && wmpCtrl.settings.get_isAvailable("Rate"))
                {
                    wmpCtrl.settings.rate = (double)value;
                }
            }
        }

        public override bool Initializing 
        {
            get { return playState == PlayState.Init; } 
        }

        public override bool Paused
        {
            get { return playState == PlayState.Paused; }
        }

        public override bool Playing
        {
            get { return !Ended; }
        }

        public override bool Stopped
        {
            get { return Initializing || Ended; }
        }

        public override bool Ended
        {
            get { return playState == PlayState.Ended; }
        }

        public override string CurrentFile
        {
            get { return currentFile; }
        }

        public override bool HasVideo
        {
            get { return true; }
        }

        public override bool HasViz
        {
            get { return true; }
        }

        public override bool IsCDA
        {
            get { return false; }
        }

        #endregion

        #region IDisposable Members

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        public override void Dispose()
        {
            GUIWindowManager.OnNewAction -= GUIWindowManager_OnNewAction;

            if (!bufferCompleted) GUIWaitCursor.Hide();

            try { if (osd != null) { osd.Dispose(); osd = null; } }
            catch (Exception ex) { Log.Instance.Warn(ex.ToString()); }

            try { if (wmpCtrl != null) { wmpCtrl.Dispose(); wmpCtrl = null; } }
            catch (Exception ex) { Log.Instance.Warn(ex.ToString()); }

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        #endregion

        #region WMP event handling

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        private void WMP_OnPlayStateChange(object sender, _WMPOCXEvents_PlayStateChangeEvent e)
        {
            if (wmpCtrl != null)
            {
                switch ((WMPPlayState)e.newState)
                {
                    case WMPPlayState.wmppsStopped:
                        PlaybackEnded();
                        break;
                    case WMPPlayState.wmppsMediaEnded:
                        if (wmpCtrl.currentMedia.isMemberOf(wmpCtrl.currentPlaylist))
                        {
                            if (wmpCtrl.currentMedia.get_isIdentical(wmpCtrl.currentPlaylist.get_Item(wmpCtrl.currentPlaylist.count - 1)))
                            {
                                PlaybackEnded();
                            }
                        }
                        else
                            PlaybackEnded();
                        break;
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        private void WMP_OnBuffering(object sender, _WMPOCXEvents_BufferingEvent e)
        {
            Log.Instance.Debug("WMPVideoPlayer: bandWidth: {0}", wmpCtrl.network.bandWidth);
            Log.Instance.Debug("WMPVideoPlayer: bitRate: {0}", wmpCtrl.network.bitRate);
            Log.Instance.Debug("WMPVideoPlayer: receivedPackets: {0}", wmpCtrl.network.receivedPackets);
            Log.Instance.Debug("WMPVideoPlayer: receptionQuality: {0}", wmpCtrl.network.receptionQuality);
            Log.Instance.Debug("WMPVideoPlayer: onbuffer start:{0}", e.start.ToString());
            bufferCompleted = !e.start;
            if (bufferCompleted) wmpCtrl.Buffering -= WMP_OnBuffering;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        void WMP_OnError(object sender, EventArgs e)
        {
            IWMPErrorItem error = wmpCtrl.Error.get_Item(0);
            // error codes see http://msdn.microsoft.com/en-us/library/cc704587(PROT.10).aspx
            Log.Instance.Warn("WMPVideoPlayer Error '{0}': {1}",error.errorCode, error.errorDescription);
            MediaPortal.Dialogs.GUIDialogOK dlg_error = (MediaPortal.Dialogs.GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
            if (dlg_error != null)
            {
                dlg_error.Reset();
                dlg_error.SetHeading(PluginConfiguration.Instance.BasicHomeScreenName);
                dlg_error.SetLine(1, Translation.Error);
                dlg_error.SetLine(2, error.errorDescription);
                dlg_error.DoModal(GUIWindowManager.ActiveWindow);
            }
            PlaybackEnded();
        }

        #endregion

        #region OVSPLayer Member

        public bool GoFullscreen { get; set; }

        #endregion
    }
}
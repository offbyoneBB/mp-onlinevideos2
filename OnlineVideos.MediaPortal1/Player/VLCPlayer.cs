using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Player;
using MediaPortal.GUI.Library;
using Vlc.DotNet.Forms;
using Vlc.DotNet.Core;
using System.Drawing;
using ExternalOSDLibrary;

namespace OnlineVideos.MediaPortal1.Player
{
    public class VLCPlayer : IPlayer, OVSPLayer
    {
        VlcControl vlcCtrl;
        FileMedia media;
        PlayState playState;
        OSDController osd;
        string url;
        bool bufferingDone;
        float playPosition;        

        public override bool Play(string strFile)
        {
            url = strFile;

            vlcCtrl = new VlcControl();
            GUIGraphicsContext.form.Controls.Add(vlcCtrl);
            vlcCtrl.Enabled = false;
            vlcCtrl.Manager = new VlcManager();

            vlcCtrl.PositionChanged += vlcCtrl_PositionChanged;
            vlcCtrl.EncounteredError += vlcCtrl_EncounteredError;

            media = new FileMedia() { Path = strFile };

            vlcCtrl.Play(media);

            GUIWaitCursor.Init(); GUIWaitCursor.Show(); // init and show the wait cursor while buffering

            return true;
        }

        void vlcCtrl_EncounteredError(VlcControl sender, VlcEventArgs<EventArgs> e)
        {
            GUIWindowManager.SendThreadCallbackAndWait((p1, p2, o) =>
            {
                Log.Instance.Warn("VLCPlayer Error: '{0}'", Vlc.DotNet.Core.Interop.LibVlcMethods.libvlc_errmsg());
                if (!bufferingDone && Initializing) GUIWaitCursor.Hide(); // hide the wait cursor if still showing
                MediaPortal.Dialogs.GUIDialogOK dlg_error = (MediaPortal.Dialogs.GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
                if (dlg_error != null)
                {
                    dlg_error.Reset();
                    dlg_error.SetHeading(PluginConfiguration.Instance.BasicHomeScreenName);
                    dlg_error.SetLine(1, Translation.Error);
                    dlg_error.SetLine(2, Vlc.DotNet.Core.Interop.LibVlcMethods.libvlc_errmsg());
                    dlg_error.DoModal(GUIWindowManager.ActiveWindow);
                }
                PlaybackEnded();
                return 0;
            }, 0, 0, null);
        }

        void vlcCtrl_PositionChanged(VlcControl sender, VlcEventArgs<float> e)
        {
            playPosition = e.Data;
        }

        public override void Process()
        {
            if (media == null) return;

            if (media.State == MediaStates.Ended || media.State == MediaStates.Stopped) 
                playState = PlayState.Ended;
            if (Initializing && media.State == MediaStates.Playing) playState = PlayState.Playing;

            if (!bufferingDone && !Initializing)
            {
                GUIWaitCursor.Hide(); // hide the wait cursor
                bufferingDone = true;

                if (Ended)
                {
                    PlaybackEnded();
                }
                else
                {
                    if (GoFullscreen) GUIWindowManager.ActivateWindow(GUIOnlineVideoFullscreen.WINDOW_FULLSCREEN_ONLINEVIDEO);
                    GUIMessage msgPb = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAYBACK_STARTED, 0, 0, 0, 0, 0, null);
                    msgPb.Label = CurrentFile;
                    GUIWindowManager.SendThreadMessage(msgPb);
                    SetVideoWindow();
                    osd = OSDController.Instance;
                }
            }

            if (osd != null) osd.UpdateGUI();
        }

        public override void SetVideoWindow()
        {
            vlcCtrl.Location = new Point(FullScreen ? 0 : GUIGraphicsContext.VideoWindow.X, FullScreen ? 0 : GUIGraphicsContext.VideoWindow.Y);
            vlcCtrl.ClientSize = new Size(FullScreen ? GUIGraphicsContext.Width : GUIGraphicsContext.VideoWindow.Width, FullScreen ? GUIGraphicsContext.Height : GUIGraphicsContext.VideoWindow.Height);
            _videoRectangle = new Rectangle(vlcCtrl.Location.X, vlcCtrl.Location.Y, vlcCtrl.ClientSize.Width, vlcCtrl.ClientSize.Height);
            _sourceRectangle = _videoRectangle;
        }

        private void PlaybackEnded()
        {
            Log.Instance.Info("VLCPlayer:ended {0}", url);
            playState = PlayState.Ended;            
            if (vlcCtrl != null)
            {
                vlcCtrl.Location = new Point(0, 0);
                vlcCtrl.ClientSize = new Size(0, 0);
                vlcCtrl.Visible = false;
            }
        }

        public override void Pause()
        {
            if (media != null && playState == PlayState.Playing || playState == PlayState.Paused)
            {
                vlcCtrl.Pause();
            if (media.State == MediaStates.Paused)
                playState = PlayState.Paused;
            else 
                playState = PlayState.Playing;
            }
        }

        public override void Stop()
        {
            if (vlcCtrl != null)
            {
                if (media.State != MediaStates.Stopped && media.State != MediaStates.Ended)
                    vlcCtrl.Stop();
                PlaybackEnded();
            }
        }

        public override double Duration
        {
            get { return media != null && media.Duration.HasValue ? media.Duration.Value / 1000 : 0.0; }
        }

        public override double CurrentPosition
        {
            get { return playPosition * Duration; }
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

        public override int Speed
        {
            get { return (Convert.ToInt32(vlcCtrl.VideoRate)); }
            set { vlcCtrl.VideoRate = value; }
        }

        public override void SeekRelative(double dTime)
        {
            double dCurTime = CurrentPosition;
            dTime = dCurTime + dTime;
            if (dTime < 0.0d) dTime = 0.0d;
            if (dTime < Duration)
            {
                SeekAbsolute(dTime);
            }
        }

        public override void SeekAbsolute(double dTime)
        {
            if (dTime < 0.0d) dTime = 0.0d;
            if (dTime < Duration)
            {
                if (vlcCtrl == null) return;
                try
                {
                    vlcCtrl.VideoTime = (long)(dTime * 1000);
                }
                catch (Exception ex) { Log.Instance.Error(ex); }
            }
        }

        public override void SeekRelativePercentage(int iPercentage)
        {
            double dCurrentPos = CurrentPosition;
            double dCurPercent = (dCurrentPos / Duration) * 100.0d;
            double dOnePercent = Duration / 100.0d;
            dCurPercent = dCurPercent + (double)iPercentage;
            dCurPercent *= dOnePercent;
            if (dCurPercent < 0.0d) dCurPercent = 0.0d;
            if (dCurPercent < Duration)
            {
                SeekAbsolute(dCurPercent);
            }
        }

        public override void SeekAsolutePercentage(int iPercentage)
        {
            if (iPercentage < 0) iPercentage = 0;
            if (iPercentage >= 100) iPercentage = 100;
            double dPercent = Duration / 100.0f;
            dPercent *= (double)iPercentage;
            SeekAbsolute(dPercent);
        }

        public override string CurrentFile
        {
            get { return url; }
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

        public override void Dispose()
        {
            if (!bufferingDone) GUIWaitCursor.Hide(); // hide the wait cursor
            if (osd != null) osd.Dispose();
            if (media != null) media.Dispose();
            if (vlcCtrl != null) vlcCtrl.Dispose();
        }

        #region OVSPLayer Member

        public bool GoFullscreen { get; set; }

        #endregion
    }
}

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
    public class VLCPlayer : IPlayer
    {
        VlcControl vlcCtrl;
        FileMedia media;
        float playPosition;
        string url;
        OSDController osd;

        public override bool Play(string strFile)
        {
            url = strFile;

            vlcCtrl = new VlcControl();
            GUIGraphicsContext.form.Controls.Add(vlcCtrl);
            vlcCtrl.Enabled = false;
            vlcCtrl.Manager = new VlcManager();

            vlcCtrl.PositionChanged += delegate(VlcControl sender, VlcEventArgs<float> e) { playPosition = e.Data; };

            media = new FileMedia() { Path = strFile };
            vlcCtrl.Play(media);

            SetVideoWindow();

            osd = OSDController.Instance;

            GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAYBACK_STARTED, 0, 0, 0, 0, 0, null);
            msg.Label = strFile;
            GUIWindowManager.SendThreadMessage(msg);

            return true;
        }

        public override void Process()
        {
            if (osd != null) osd.UpdateGUI();
        }

        public override void SetVideoWindow()
        {
            if (FullScreen)
            {
                _videoRectangle = new Rectangle(0, 0, vlcCtrl.ClientSize.Width, vlcCtrl.ClientSize.Height);
                _sourceRectangle = _videoRectangle;
                vlcCtrl.Location = new Point(0, 0);
                vlcCtrl.ClientSize = new Size(GUIGraphicsContext.Width, GUIGraphicsContext.Height);
            }
            else
            {
                _videoRectangle = new Rectangle(GUIGraphicsContext.VideoWindow.X, GUIGraphicsContext.VideoWindow.Y, GUIGraphicsContext.VideoWindow.Width, GUIGraphicsContext.VideoWindow.Height);
                _sourceRectangle = _videoRectangle;
                vlcCtrl.ClientSize = new Size(GUIGraphicsContext.VideoWindow.Width, GUIGraphicsContext.VideoWindow.Height);
                vlcCtrl.Location = new Point(GUIGraphicsContext.VideoWindow.X, GUIGraphicsContext.VideoWindow.Y);
            }
        }

        public override void Pause()
        {
            vlcCtrl.Pause();
        }

        public override void Stop()
        {
            vlcCtrl.Stop();
            vlcCtrl.Visible = false;
        }

        public override double Duration
        {
            get { return media != null && media.Duration.HasValue ? media.Duration.Value / 1000 : 0.0; }
        }

        public override double CurrentPosition
        {
            get { return playPosition * Duration; }
        }

        public override bool Paused
        {
            get { return media != null && media.State == MediaStates.Paused ? true : false; }
        }

        public override bool Stopped
        {
            get { return media != null && (media.State == MediaStates.Ended || media.State == MediaStates.Stopped || media.State == MediaStates.NothingSpecial) ? true : false; }
        }

        public override bool Playing
        {
            get { return media != null && (media.State == MediaStates.Opening || media.State == MediaStates.Buffering || media.State == MediaStates.Playing || media.State == MediaStates.Paused) ? true : false; }
        }

        public override bool Ended
        {
            get { return media != null && (media.State == MediaStates.Ended || media.State == MediaStates.Error); }
        }

        public override int Volume
        {
            get
            {
                return vlcCtrl != null ? vlcCtrl.VolumeLevel : MediaPortal.Player.VolumeHandler.Instance.Volume;
            }
            set
            {
                if (vlcCtrl != null) vlcCtrl.VolumeLevel = value; else MediaPortal.Player.VolumeHandler.Instance.Volume = value;
            }
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
            if (osd != null) osd.Dispose();
            if (media != null) media.Dispose();
            if (vlcCtrl != null) vlcCtrl.Dispose();
        }
    }
}

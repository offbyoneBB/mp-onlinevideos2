using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Globalization;
using AxShockwaveFlashObjects;
using MediaPortal.Player;
using MediaPortal.GUI.Library;
using System.Xml;

namespace OnlineVideos.Player
{
    public class YahooMusicVideosPlayer : IPlayer
    {
        public enum PlayState { Init, Playing, Paused, Ended }

        FlashControl FlvControl = null;
        string _currentFile = String.Empty;
        bool _notifyPlaying = false;
        double _duration;
        double _currentPosition;
        DateTime _updateTimer;
        bool _needUpdate = true;        
        bool _isFullScreen = false;
        int _positionX = 10, _positionY = 10, _videoWidth = 100, _videoHeight = 100;        
        PlayState _playState = PlayState.Init;
        NumberFormatInfo provider = new NumberFormatInfo();

        public YahooMusicVideosPlayer()
        {            
            provider.NumberDecimalSeparator = ".";
            provider.NumberGroupSeparator = ",";
            provider.NumberGroupSizes = new int[] { 3 };
        }
        
        public override bool Play(string strFile)
        {
            Log.Info("Playing flv with FlvPlayer :{0}", strFile);
            try
            {
                Uri site = new Uri(strFile);                
                FlvControl = new FlashControl();
                FlvControl.Player.AllowScriptAccess = "always";
                FlvControl.Player.FlashCall += new _IShockwaveFlashEvents_FlashCallEventHandler(OnFlashCall);
                FlvControl.Player.FSCommand += new _IShockwaveFlashEvents_FSCommandEventHandler(Player_FSCommand);                
                FlvControl.Player.FlashVars = site.Query.Replace("?", "&") + "&eh=myCallbackEventHandler";
                FlvControl.Player.OnProgress += new _IShockwaveFlashEvents_OnProgressEventHandler(Player_OnProgress);                
                FlvControl.Player.LoadMovie(0, string.Format("{0}://{1}{2}", site.Scheme, site.Host, site.AbsolutePath));

                GUIGraphicsContext.form.Controls.Add(FlvControl);
                GUIWindowManager.OnNewAction += new OnActionHandler(OnAction2);
                GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAYBACK_STARTED, 0, 0, 0, 0, 0, null);
                msg.Label = strFile;
                GUIWindowManager.SendThreadMessage(msg);
                _notifyPlaying = true;
                FlvControl.ClientSize = new Size(0, 0);
                FlvControl.Visible = true;
                FlvControl.Enabled = false;
                FlvControl.SendToBack();
                _needUpdate = true;
                _isFullScreen = GUIGraphicsContext.IsFullScreenVideo;
                _positionX = GUIGraphicsContext.VideoWindow.Left;
                _positionY = GUIGraphicsContext.VideoWindow.Top;
                _videoWidth = GUIGraphicsContext.VideoWindow.Width;
                _videoHeight = GUIGraphicsContext.VideoWindow.Height;
                _currentFile = strFile;
                _duration = -1;
                _currentPosition = -1;
                _playState = PlayState.Playing;
                _updateTimer = DateTime.Now;
                SetVideoWindow();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Flv on Play Error {0} {1} {2}", ex.Message, ex.Source, ex.StackTrace);
            }
            return false;
        }

        void Player_OnProgress(object sender, _IShockwaveFlashEvents_OnProgressEvent e)
        {
            Log.Debug("On progres {0}", e.percentDone.ToString());
        }

        void Player_FSCommand(object sender, _IShockwaveFlashEvents_FSCommandEvent e)
        {
            Log.Info("OnFsCommand reached with value request:{0},Object:{1}", e.args, e.command);
        }

        public void OnFlashCall(object sender, _IShockwaveFlashEvents_FlashCallEvent foEvent)
        {
            //Log.Info("OnFlashCall reached with value request:{0},Object:{1}", foEvent.request, sender);
            XmlDocument document = new XmlDocument();
            document.LoadXml(foEvent.request);
            Log.Debug("FLV event {0}", foEvent.request);
            // Get all the arguments
            XmlNodeList list = document.GetElementsByTagName("invoke");
            String lsName = list[0].Attributes["name"].Value;
            list = document.GetElementsByTagName("arguments");
            String lsState = list[0].FirstChild.InnerText;
            GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAY_ITEM, 0, 0, 0, 0, 0, null);
            if (lsName.Equals("myCallbackEventHandler"))
            {
                switch (lsState)
                {
                    case "itemBegin":
                        _playState = PlayState.Playing;
                        // GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAY_ITEM, 0, 0, 0, 0, 0, null);
                        msg.Object = foEvent.request;
                        GUIWindowManager.SendThreadMessage(msg);
                        break;
                    case "itemEnd":
                        // GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAY_ITEM, 0, 0, 0, 0, 0, null);
                        msg.Object = foEvent.request;
                        GUIWindowManager.SendThreadMessage(msg);
                        //_playState = PlayState.Ended;
                        break;
                    case "done":
                        _playState = PlayState.Ended;
                        break;
                    case "init":
                        _playState = PlayState.Init;
                        break;
                    case "streamPlay":
                        _playState = PlayState.Playing;
                        break;
                    case "streamPause":
                        _playState = PlayState.Paused;
                        break;
                    case "streamStop":
                        _playState = PlayState.Ended;
                        break;
                    case "streamError":
                        _playState = PlayState.Ended;
                        break;                    
                    case "time":
                        _currentPosition = Convert.ToInt32(list[0].ChildNodes[1].InnerText);
                        _duration = _currentPosition + Convert.ToInt32(list[0].ChildNodes[2].InnerText);
                        break;
                }
            }
        }        

        public void OnAction2(Action foAction)
        {
            if (foAction.wID == Action.ActionType.ACTION_SHOW_GUI || foAction.wID == Action.ActionType.ACTION_SHOW_FULLSCREEN)
            {
                SetVideoWindow();
            }

            if (foAction.wID == Action.ActionType.ACTION_NEXT_ITEM)
            {
                try
                {
                    if (_playState == PlayState.Playing || _playState == PlayState.Paused)
                    {
                        Log.Debug("Flv Stop {0}", FlvControl.Player.CallFunction("<invoke name=\"playNext\"></invoke>"));
                    }
                }
                catch
                {
                }
            }

            if (foAction.wID == Action.ActionType.ACTION_PREV_ITEM)
            {
                try
                {
                    if (_playState == PlayState.Playing || _playState == PlayState.Paused)
                    {
                        Log.Debug("Flv Stop {0}", FlvControl.Player.CallFunction("<invoke name=\"playPrevious\"></invoke>"));
                    }
                }
                catch
                {
                }
            }
        }        

        public override double CurrentPosition
        {
            get
            {
                if (FlvControl == null) return 0.0d;
                if (_playState == PlayState.Init) return 0.0d;
                try
                {                    
                    string restp = FlvControl.Player.CallFunction("<invoke name=\"getVidTime\" returntype=\"xml\"></invoke>");                    
                    XmlDocument document = new XmlDocument();
                    document.LoadXml(restp);
                    XmlNode list = document.SelectSingleNode("number");
                    String lsName = list.InnerText;
                    return Convert.ToDouble(lsName, provider);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    return 0.0d;
                }                
            }
        }

        public override bool CanSeek()
        {
            return true;
        }

        public override string CurrentFile
        {
            get
            {
                return _currentFile;
            }
        }

        public override double Duration
        {
            get
            {
                return _duration;
            }
        }

        public override bool Ended
        {
            get
            {
                return (_playState == PlayState.Ended);
            }
        }

        public override bool Playing
        {
            get
            {
                try
                {
                    if (FlvControl == null) return false;                    
                    return (_playState == PlayState.Playing || _playState == PlayState.Paused || _playState == PlayState.Init);
                }
                catch (Exception)
                {                    
                    return false;
                }
            }
        }

        public override void Pause()
        {
            if (FlvControl == null) return;
            try
            {
                if (_playState == PlayState.Paused)
                {
                    FlvControl.Player.CallFunction("<invoke name=\"vidPlay\"></invoke>");
                }
                else
                {
                    FlvControl.Player.CallFunction("<invoke name=\"vidPause\"></invoke>");
                }
            }
            catch (Exception ex)
            {
                FlvControl = null;
                Log.Error("Flv on pause error {0} {1} {2}", ex.Message, ex.Source, ex.StackTrace);
                return;
            }
        }

        public override bool Paused
        {
            get
            {
                try
                {
                    return (_playState == PlayState.Paused);
                }
                catch (Exception)
                {
                    FlvControl = null;
                    return false;
                }
            }
        }        

        public override void Process()
        {
            if (_needUpdate)
            {
                SetVideoWindow();
            }
            if (CurrentPosition >= 10.0)
            {
                if (_notifyPlaying)
                {
                    _notifyPlaying = false;
                    //GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_PLAYING_10SEC, 0, 0, 0, 0, 0, null);
                    //msg.Label = CurrentFile;
                    //GUIWindowManager.SendThreadMessage(msg);
                    //Log.Info("Message Playing 10 sec sent");
                }
            }
        }

        public override void SetVideoWindow()
        {
            if (FlvControl == null) return;
            if (GUIGraphicsContext.IsFullScreenVideo != _isFullScreen)
            {
                _isFullScreen = GUIGraphicsContext.IsFullScreenVideo;
                _needUpdate = true;
            }
            if (!_needUpdate) return;
            _needUpdate = false;

            if (_isFullScreen)
            {
                Log.Info("Flv:Fullscreen");

                _positionX = GUIGraphicsContext.OverScanLeft;
                _positionY = GUIGraphicsContext.OverScanTop;
                _videoWidth = GUIGraphicsContext.OverScanWidth;
                _videoHeight = GUIGraphicsContext.OverScanHeight;

                FlvControl.Location = new Point(0, 0);
                FlvControl.ClientSize = new System.Drawing.Size(GUIGraphicsContext.Width, GUIGraphicsContext.Height);
                FlvControl.Size = new System.Drawing.Size(GUIGraphicsContext.Width, GUIGraphicsContext.Height);

                _videoRectangle = new Rectangle(0, 0, FlvControl.ClientSize.Width, FlvControl.ClientSize.Height);
                _sourceRectangle = _videoRectangle;
                return;
            }
            else
            {
                FlvControl.ClientSize = new System.Drawing.Size(_videoWidth, _videoHeight);
                FlvControl.Location = new Point(_positionX, _positionY);
                _videoRectangle = new Rectangle(_positionX, _positionY, FlvControl.ClientSize.Width, FlvControl.ClientSize.Height);
                _sourceRectangle = _videoRectangle;                
            }
        }

        public override void Stop()
        {
            Log.Info("Attempting to stop...{0}", FlvControl);
            if (FlvControl == null) return;
            try
            {
                FlvControl.Player.StopPlay();
                FlvControl.Player.DisableLocalSecurity();
                try
                {
                    if (_playState == PlayState.Playing || _playState == PlayState.Paused)
                    {
                        Log.Debug("Flv Stop {0}", FlvControl.Player.CallFunction("<invoke name=\"vidStop\"></invoke>"));
                    }
                }
                catch(Exception ex)
                {
                    Log.Error(ex);
                }
                FlvControl.Player.Visible = false;
                FlvControl.Visible = false;
                FlvControl.ClientSize = new Size(0, 0);
                GUIGraphicsContext.form.Controls[0].Enabled = false;
                GUIGraphicsContext.form.Controls[0].Visible = false;
                FlvControl.Dispose();
                //_playerIsPaused = false;
                //_started = false;
                //Playing = false;
                //GUIGraphicsContext.OnNewAction -= new OnActionHandler(OnAction2);
                //Log.Info("after {0}", Playing);
            }
            catch (Exception ex)
            {
                Log.Error("Flv on stop error {0} \n {1} \n {2}", ex.Message, ex.Source, ex.StackTrace);                
                FlvControl = null;
            }
        }

        public override bool HasVideo
        {
            get
            {
                return true;
            }
        }

        public override bool FullScreen
        {
            get
            {
                return _isFullScreen;
            }
            set
            {
                if (value != _isFullScreen)
                {                    
                    _isFullScreen = value;
                    _needUpdate = true;
                }
            }
        }

        public override int PositionX
        {
            get { return _positionX; }
            set
            {
                if (value != _positionX)
                {                    
                    _positionX = value;
                    _needUpdate = true;
                }
            }
        }

        public override int PositionY
        {
            get { return _positionY; }
            set
            {
                if (value != _positionY)
                {
                    _positionY = value;
                    _needUpdate = true;
                }
            }
        }

        public override int RenderWidth
        {
            get { return _videoWidth; }
            set
            {
                if (value != _videoWidth)
                {                    
                    _videoWidth = value;
                    _needUpdate = true;
                }
            }
        }

        public override int RenderHeight
        {
            get { return _videoHeight; }
            set
            {
                if (value != _videoHeight)
                {                    
                    _videoHeight = value;
                    _needUpdate = true;
                }
            }
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
            FlvControl.Player.Forward();
            if (dTime < 0.0d) dTime = 0.0d;
            if (dTime < Duration)
            {
                if (FlvControl == null) return;
                try
                {
                    Log.Info("Attempting to seek...");
                    //FlvControl.Player.CallFunction("<invoke name=\"vidSeek\" returntype=\"xml\"><arguments><number>" + dTime + "</number></arguments></invoke>");
                    //FlvControl.Player.CallFunction("<invoke name=\"scrub\" returntype=\"xml\"><arguments><number>" + dTime + "</number></arguments></invoke>");
                    //FlvControl.Player.CallFunction("<invoke name=\"sendEvent\" returntype=\"xml\"><arguments><string>scrub</string><number>" + dTime + "</number></arguments></invoke>");
                    //FlvControl.Player.CurrentFrame() = (int)dTime;
                    Log.Info("seeking complete");
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        public override void SeekRelativePercentage(int iPercentage)
        {
            double dCurrentPos = CurrentPosition;
            double dDuration = Duration;

            double fCurPercent = (dCurrentPos / Duration) * 100.0d;
            double fOnePercent = Duration / 100.0d;
            fCurPercent = fCurPercent + (double)iPercentage;
            fCurPercent *= fOnePercent;
            if (fCurPercent < 0.0d) fCurPercent = 0.0d;
            if (fCurPercent < Duration)
            {
                SeekAbsolute(fCurPercent);
            }
        }

        public override void SeekAsolutePercentage(int iPercentage)
        {
            if (iPercentage < 0) iPercentage = 0;
            if (iPercentage >= 100) iPercentage = 100;
            double fPercent = Duration / 100.0f;
            fPercent *= (double)iPercentage;
            SeekAbsolute(fPercent);
        }


    }
}

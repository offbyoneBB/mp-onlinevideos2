using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites.Base;
using OnlineVideos.Sites.Entities;
using System.Windows.Forms;
using OnlineVideos.Helpers;
using OnlineVideos.Sites.Properties;
using System.Drawing;
using System.Threading;

namespace OnlineVideos.Sites.BrowserUtilConnectors
{
    public class NetflixConnector : BrowserUtilConnector
    {
        protected PictureBox _loadingPicture = new PictureBox();

        private enum State
        {
            None,
            Login,
            Profile,
            ReadyToPlay,
            Playing
        }

        private string _username;
        private string _password;
        private string _profile;

        private State _currentState = State.None;
        private bool _isPlayingOrPausing = false;

        /// <summary>
        /// Show a loading image
        /// </summary>
        public void ShowLoading()
        {
            _loadingPicture.Image = Resources.loading;
            _loadingPicture.Dock = DockStyle.Fill;
            _loadingPicture.SizeMode = PictureBoxSizeMode.CenterImage;
            _loadingPicture.BackColor = Color.Black;
            if (!Browser.FindForm().Controls.Contains(_loadingPicture))
                Browser.FindForm().Controls.Add(_loadingPicture);
            _loadingPicture.BringToFront();
        }

        public override void OnClosing()
        {
            //Workaround - browserplayer does not always exit properly otherwise
            ProcessComplete.Finished = true;
            ProcessComplete.Success = false;
            Url = "about:blank";
            base.OnClosing();
        }

        public override void OnAction(string actionEnumName)
        {
            if (_currentState == State.Playing && !_isPlayingOrPausing)
            {
                if (actionEnumName == "ACTION_MOVE_LEFT")
                {
                    Cursor.Position = new System.Drawing.Point(300, 300);
                    Application.DoEvents();
                    CursorHelper.DoLeftMouseClick();
                    Application.DoEvents();
                    System.Windows.Forms.SendKeys.Send("{LEFT}");
                }
                if (actionEnumName == "ACTION_MOVE_RIGHT")
                {
                    Cursor.Position = new System.Drawing.Point(300, 300);
                    Application.DoEvents();
                    CursorHelper.DoLeftMouseClick();
                    Application.DoEvents();
                    System.Windows.Forms.SendKeys.Send("{RIGHT}");
                }
                base.OnAction(actionEnumName);
            }
        }

        public override Entities.EventResult PerformLogin(string username, string password)
        {

            ShowLoading();
            string[] userProfile = username.Split('¥');
            _username = userProfile[0];
            _profile = userProfile[1];
            _password = password;
            _currentState = State.Login;
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            Url = @"https://www.netflix.com/Login";
            return EventResult.Complete();
        }


        public override Entities.EventResult PlayVideo(string videoToPlay)
        {
            ShowLoading();
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            Url = videoToPlay;
            _currentState = State.Playing;
            return EventResult.Complete();
        }

        public override Entities.EventResult Play()
        {
            return PlayPause();
        }

        public override Entities.EventResult Pause()
        {
            return PlayPause();
        }

        private EventResult PlayPause()
        {
            /*
            if (_currentState != State.Playing || _isPlayingOrPausing || Browser.Document == null || Browser.Document.Body == null) return EventResult.Complete();
            _isPlayingOrPausing = true;
            //1080 230 960
            //720 160 630
            //Play pause only supported for 720 and 1080 res. (right now...) + 768 for dev machine (160 680)
            var h = Browser.FindForm().Bottom;
            var x = h > 1000 ? 230 : 160;
            var y = h > 1000 ? 960 : (h > 740 ? 680 : 630);
            
            Cursor.Position = new System.Drawing.Point(x - 10, y);
            // We have to move the cursor to show the play button
            while (Cursor.Position.X < x)
            {
                Cursor.Position = new System.Drawing.Point(Cursor.Position.X + 1, y);
                Application.DoEvents();
            }
            Cursor.Position = new System.Drawing.Point(x, y);
            Application.DoEvents();
            CursorHelper.DoLeftMouseClick();
            Application.DoEvents();
            Cursor.Position = new System.Drawing.Point(Browser.FindForm().Left + 10, Browser.FindForm().Bottom - 10);
            Application.DoEvents();
            _isPlayingOrPausing = false;
             */
            if (_currentState != State.Playing || _isPlayingOrPausing || Browser.Document == null || Browser.Document.Body == null) return EventResult.Complete();
            _isPlayingOrPausing = true;
            Cursor.Position = new System.Drawing.Point(300, 300);
            Application.DoEvents();
            CursorHelper.DoLeftMouseClick();
            Application.DoEvents();
            _isPlayingOrPausing = false;
            System.Windows.Forms.SendKeys.Send(" ");
            return EventResult.Complete();
        }


        public override Entities.EventResult BrowserDocumentComplete()
        {
            string jsCode;
            switch (_currentState)
            {
                case State.Login:

                    if (Url.Contains("/Login"))
                    {
                        jsCode = "document.getElementById('email').value = '" + _username + "'; ";
                        jsCode += "document.getElementById('password').value = '" + _password + "'; ";
                        jsCode += "document.getElementById('login-form-contBtn').click();";
                        InvokeScript(jsCode);
                        _currentState = State.Profile;
                    }
                    else
                    {
                        Url = "https://www.netflix.com/SwitchProfile?tkn=" + _profile;
                        _currentState = State.ReadyToPlay;
                    }
                    break;
                case State.Profile:
                    ShowLoading();
                    if (Url.Contains("/Login"))
                        return EventResult.Error("Unable to login");
                    Url = "https://www.netflix.com/SwitchProfile?tkn=" + _profile;
                    _currentState = State.ReadyToPlay;
                    break;
                case State.ReadyToPlay:
                    ShowLoading();
                    _currentState = State.None;
                    ProcessComplete.Finished = true;
                    ProcessComplete.Success = true;
                    break;
                case State.Playing:
                    _loadingPicture.Visible = false;
                    ProcessComplete.Finished = true;
                    ProcessComplete.Success = true;
                    break;
            }
            return EventResult.Complete();
        }
    }
}

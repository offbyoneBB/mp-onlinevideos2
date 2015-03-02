using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites;
using OnlineVideos.Sites.Entities;
using System.Windows.Forms;
using OnlineVideos.Helpers;
using OnlineVideos.Sites.Properties;
using System.Drawing;
using System.Threading;
using System.Diagnostics;

namespace OnlineVideos.Sites.BrowserUtilConnectors
{
    public class NetflixConnector : BrowserUtilConnector
    {

        private enum State
        {
            None,
            Login,
            Profile,
            ReadyToPlay,
            StartPlay,
            Playing
        }

        private string _username;
        private string _password;
        private string _profile;
        private bool _showLoading = true;
        private bool _rememberLogin = false;

        private State _currentState = State.None;
        private bool _isPlayingOrPausing = false;

        public override void OnClosing()
        {
            //Process.GetProcessesByName("OnlineVideos.WebAutomation.BrowserHost").First().Kill();
            Process.GetCurrentProcess().Kill();
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
            }
        }

        public override Entities.EventResult PerformLogin(string username, string password)
        {

            _rememberLogin = username.Contains("REMEMBERLOGIN");
            username = username.Replace("REMEMBERLOGIN", string.Empty);
            _showLoading = username.Contains("SHOWLOADING");
            username = username.Replace("SHOWLOADING", string.Empty);
            if (_showLoading)
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
            if (_showLoading)
                ShowLoading();
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            Url = videoToPlay;
            _currentState = State.StartPlay;
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

                    if (Url.Contains("/Login?"))
                    {
                        jsCode = "document.getElementById('email').value = '" + _username + "'; ";
                        jsCode += "document.getElementById('password').value = '" + _password + "'; ";
                        if (_rememberLogin)
                        {
                            jsCode += "document.getElementById('RememberMe').checked = true; ";
                        }
                        else
                        {
                            jsCode += "document.getElementById('RememberMe').checked = false; ";
                        }
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
                    if (Url.Contains("/WiHome") || Url.Contains("/Kids") || Url.Contains("/ProfilesGate"))
                    {
                        Url = "https://www.netflix.com/SwitchProfile?tkn=" + _profile;
                        _currentState = State.ReadyToPlay;
                    }
                    break;
                case State.ReadyToPlay:
                    if (Url.Contains("/WiHome") || Url.Contains("/Kids"))
                    {
                        ProcessComplete.Finished = true;
                        ProcessComplete.Success = true;
                    }
                    break;
                case State.StartPlay:
                case State.Playing:
                    if (Url.Contains("movieid"))
                    {
                        if (_showLoading)
                            HideLoading();
                        _currentState = State.Playing;
                        ProcessComplete.Finished = true;
                        ProcessComplete.Success = true;
                    }
                    break;
            }
            return EventResult.Complete();
        }
    }
}

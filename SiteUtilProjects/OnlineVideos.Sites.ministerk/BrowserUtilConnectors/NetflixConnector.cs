using OnlineVideos.Helpers;
using OnlineVideos.Sites.Entities;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace OnlineVideos.Sites.BrowserUtilConnectors
{
    public class NetflixConnector : BrowserUtilConnector
    {

        private enum State
        {
            None,
            Login,
            ProfilesGate,
            SelectProfile,
            ReadyToPlay,
            Playing
        }

        private string _username;
        private string _password;
        private string _profile;
        private bool _showLoading = true;
        private bool _enableNetflixOsd = false;
        private bool _useAlternativeProfilePicker = false;
        private bool _disableLogging = false;

        private State _currentState = State.None;
        private bool _isPlayingOrPausing = false;

        private void SendKeyToBrowser(string key)
        {
            Cursor.Position = new System.Drawing.Point(Browser.FindForm().Location.X + 300, Browser.Location.Y + 300);
            Application.DoEvents();
            CursorHelper.DoLeftMouseClick();
            Application.DoEvents();
            System.Windows.Forms.SendKeys.Send(key);
        }

        public override void OnClosing()
        {
            Process.GetCurrentProcess().Kill();
        }
        public override void OnAction(string actionEnumName)
        {
            if (_currentState == State.Playing && !_isPlayingOrPausing)
            {
                if (actionEnumName == "REMOTE_0" && _enableNetflixOsd)
                {
                    SendKeyToBrowser("^(%(+(d)))");
                }
                if (actionEnumName == "ACTION_MOVE_LEFT")
                {
                    SendKeyToBrowser("{LEFT}");
                }
                if (actionEnumName == "ACTION_MOVE_RIGHT")
                {
                    SendKeyToBrowser("{RIGHT}");
                }
            }
        }


        public override Entities.EventResult PerformLogin(string username, string password)
        {

            _disableLogging = username.Contains("DISABLELOGGING");
            username = username.Replace("DISABLELOGGING", string.Empty);
            _showLoading = username.Contains("SHOWLOADING");
            username = username.Replace("SHOWLOADING", string.Empty);
            _enableNetflixOsd = username.Contains("ENABLENETFLIXOSD");
            username = username.Replace("ENABLENETFLIXOSD", string.Empty);
            _useAlternativeProfilePicker = username.Contains("PROFILEPICKER");
            username = username.Replace("PROFILEPICKER", string.Empty);

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
            if (_currentState != State.Playing || _isPlayingOrPausing || Browser.Document == null || Browser.Document.Body == null) return EventResult.Complete();
            _isPlayingOrPausing = true;
            SendKeyToBrowser(" ");
            _isPlayingOrPausing = false;
            return EventResult.Complete();
        }


        private bool activateLoginTimer = true;
        private bool activatePasswordTimer = true;
        private bool usernamePosted = false;
        public override Entities.EventResult BrowserDocumentComplete()
        {
            if (!_disableLogging) MessageHandler.Info("Netflix. Url: {0}, State: {1}", Url, _currentState.ToString());
            switch (_currentState)
            {
                case State.Login:
                    if (Url.Contains("/Login") && Url != @"https://www.netflix.com/Login")
                    {
                        if (activateLoginTimer)
                        {
                            activateLoginTimer = false;
                            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                            timer.Tick += (object sender, EventArgs e) =>
                            {
                                string jsCode = "document.getElementsByName('email')[0].value = '" + _username + "'; ";
                                jsCode += "document.getElementsByName('password')[0].value = '" + _password + "'; ";
                                jsCode += "if (document.getElementById('login-form-contBtn')) { document.getElementById('login-form-contBtn').click(); } else { document.getElementsByTagName('form')[0].submit();}";
                                InvokeScript(jsCode);
                                usernamePosted = true;
                                timer.Stop();
                                timer.Dispose();
                            };

                            timer.Interval = 1000;
                            timer.Start();
                        }
                    }
                    else if (Url.Contains("/Login") && usernamePosted && activatePasswordTimer)
                    {
                        activatePasswordTimer = false;
                        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                        timer.Tick += (object sender, EventArgs e) =>
                        {
                            string jsCode = "document.getElementsByName('password')[0].value = '" + _password + "'; ";
                            jsCode += "document.getElementsByTagName('form')[0].submit(); ";
                            InvokeScript(jsCode);
                            timer.Stop();
                            timer.Dispose();
                        };
                        timer.Interval = 1000;
                        timer.Start();
                    }
                    else if (!Url.Contains("/Login"))
                    {
                        Url = "https://www.netflix.com";
                        _currentState = State.SelectProfile;
                    }
                    break;
                case State.ProfilesGate:
                    Url = "https://www.netflix.com/ProfilesGate";
                    _currentState = State.SelectProfile;
                    break;
                case State.SelectProfile:
                    if (Url.Contains("/ProfilesGate"))
                    {
                        if (!_useAlternativeProfilePicker)
                            Url = "https://www.netflix.com/SwitchProfile?tkn=" + _profile;
                        else
                            InvokeScript("setTimeout(\"document.querySelector('a[data-reactid*=" + _profile + "]').click()\", 500);");
                        _currentState = State.ReadyToPlay;
                    }
                    else
                    {
                        Url = "https://www.netflix.com/ProfilesGate";
                    }
                    break;
                case State.ReadyToPlay:
                    //Sometimes the profiles gate loads again
                    if (Url.Contains("/ProfilesGate"))
                    {
                        if (!_useAlternativeProfilePicker)
                            Url = "https://www.netflix.com/SwitchProfile?tkn=" + _profile;
                        else
                            InvokeScript("setTimeout(\"document.querySelector('a[data-reactid*=" + _profile + "]').click()\", 500);");
                        _currentState = State.ReadyToPlay;
                    }
                    if (Url.Contains("/browse") || Url.ToLower().Contains("/kid"))
                    {
                        ProcessComplete.Finished = true;
                        ProcessComplete.Success = true;
                    }
                    else
                        Url = "http://www.netflix.com/";
                    break;
                case State.Playing:
                    if (_showLoading)
                        HideLoading();
                    _currentState = State.Playing;
                    ProcessComplete.Finished = true;
                    ProcessComplete.Success = true;
                    break;
            }
            return EventResult.Complete();
        }
    }
}

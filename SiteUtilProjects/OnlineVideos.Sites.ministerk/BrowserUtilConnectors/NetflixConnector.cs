using Newtonsoft.Json.Linq;
using OnlineVideos.Helpers;
using OnlineVideos.Sites.Entities;
using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace OnlineVideos.Sites.BrowserUtilConnectors
{
    public class NetflixConnector : BrowserUtilConnector
    {

        private const int INTERNET_OPTION_SUPPRESS_BEHAVIOR = 81;
        private const int INTERNET_SUPPRESS_COOKIE_PERSIST = 3;

        [DllImport("wininet.dll", SetLastError = true)]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int lpdwBufferLength);

        private enum State
        {
            None,
            Login,
            SelectProfile,
            ReadyToPlay,
            GotoToPlay,
            Playing
        }

        private string _username;
        private string _password;
        private string _profileToken;
        private bool _showLoading = true;
        private bool _enableNetflixOsd = false;
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
            if (!_disableLogging) MessageHandler.Info("Netflix. Input: {0}", actionEnumName);
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
            Cursor.Hide();
            Application.DoEvents();
            JObject json = JObject.Parse(System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(password)));
            _showLoading = bool.Parse(json["showLoadingSpinner"].Value<string>());

            if (_showLoading)
                ShowLoading();
            _disableLogging = bool.Parse(json["disableLogging"].Value<string>());
            _enableNetflixOsd = bool.Parse(json["enableNetflixOsd"].Value<string>());
            _profileToken = json["profileToken"].Value<string>();
            _password = json["password"].Value<string>();
            _username = username;
            _currentState = State.Login;
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            unsafe
            {
                Int32 option = INTERNET_SUPPRESS_COOKIE_PERSIST;
                InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SUPPRESS_BEHAVIOR, new IntPtr(&option), sizeof(Int32));
            }

            Url = @"https://www.netflix.com/Login";
            return EventResult.Complete();
        }

        public override Entities.EventResult PlayVideo(string videoToPlay)
        {
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            Url = videoToPlay;
            _currentState = State.Playing;
            return EventResult.Complete();
        }

        public override Entities.EventResult Play()
        {
            if (!_disableLogging) MessageHandler.Info("Netflix. Input: {0}", "Play");
            return PlayPause();
        }

        public override Entities.EventResult Pause()
        {
            if (!_disableLogging) MessageHandler.Info("Netflix. Input: {0}", "Pause");
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
        private bool activateProfileTimer = true;
        private bool activateReadyTimer = true;
        public override Entities.EventResult BrowserDocumentComplete()
        {
            if (!_disableLogging) MessageHandler.Info("Netflix. Url: {0}, State: {1}", Url, _currentState.ToString());
            switch (_currentState)
            {
                case State.Login:
                    if (Url.ToLower().Contains("/login") && activateLoginTimer)
                    {
                        activateLoginTimer = false;
                        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                        timer.Tick += (object sender, EventArgs e) =>
                        {
                            timer.Stop();
                            timer.Dispose();
                            string data = Browser.DocumentText;
                            _currentState = State.SelectProfile;
                            Regex rgx = new Regex(@"""authURL"":""(?<authURL>[^""]*)");
                            Match m = rgx.Match(data);
                            if (m.Success)
                            {
                                string authUrl = m.Groups["authURL"].Value;
                                authUrl = HttpUtility.UrlDecode(authUrl.Replace("\\x", "%"));
                                string loginPostDataFormat = "email={0}&password={1}&rememberMe=true&flow=websiteSignUp&mode=login&action=loginAction&withFields=email%2Cpassword%2CrememberMe%2CnextPage%2CshowPassword&authURL={2}&nextPage=&showPassword=";
                                string loginPostData = string.Format(loginPostDataFormat, HttpUtility.UrlEncode(_username), HttpUtility.UrlEncode(_password), HttpUtility.UrlEncode(authUrl));
                                Browser.Navigate(Url, "", Encoding.UTF8.GetBytes(loginPostData), "Referer: " + Url + "\r\nContent-Type: application/x-www-form-urlencoded\r\n");
                            }
                        };
                        timer.Interval = 1000;
                        timer.Start();
                    }
                    else if (!Url.ToLower().Contains("/login"))
                    {
                        Url = "https://www.netflix.com";
                        _currentState = State.SelectProfile;
                    }
                    break;
                case State.SelectProfile:
                    if (activateProfileTimer)
                    {
                        activateProfileTimer = false;
                        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                        timer.Tick += (object sender, EventArgs e) =>
                        {
                            timer.Stop();
                            timer.Dispose();
                            InvokeScript(Properties.Resources.NetflixJs);
                            InvokeScript("switchProfile('" + _profileToken + "')");
                            _currentState = State.ReadyToPlay;
                        };
                        timer.Interval = 2000;
                        timer.Start();
                    }
                    break;
                case State.ReadyToPlay:
                    if (activateReadyTimer)
                    {
                        activateReadyTimer = false;
                        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                        timer.Tick += (object sender, EventArgs e) =>
                        {
                            timer.Stop();
                            timer.Dispose();
                            _currentState = State.GotoToPlay;
                            InvokeScript("window.location.href = 'https://www.netflix.com/';");
                        };
                        timer.Interval = 1500;
                        timer.Start();
                    }
                    break;
                case State.GotoToPlay:
                    ProcessComplete.Finished = true;
                    ProcessComplete.Success = true;
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

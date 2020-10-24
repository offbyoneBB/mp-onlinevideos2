using Newtonsoft.Json.Linq;
using OnlineVideos.Helpers;
using OnlineVideos.Sites.Entities;
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
            ReadyToPlay,
            Playing
        }

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
            JObject json = JObject.Parse(System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(password)));
            _showLoading = bool.Parse(json["showLoadingSpinner"].Value<string>());

            _disableLogging = bool.Parse(json["disableLogging"].Value<string>());
            _enableNetflixOsd = bool.Parse(json["enableNetflixOsd"].Value<string>());

            if (username == "GET")
            {
                Application.DoEvents();
                _currentState = State.Login;
                ProcessComplete.Finished = false;
                ProcessComplete.Success = false;
                Url = @"https://www.netflix.com/Login";
            }
            else
            {
                if (_showLoading)
                  ShowLoading();
                ProcessComplete.Finished = true;
                ProcessComplete.Success = true;
                _currentState = State.ReadyToPlay;
            }
            return EventResult.Complete();
        }

        public override Entities.EventResult PlayVideo(string videoToPlay)
        {
            Cursor.Hide();
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

        public override Entities.EventResult BrowserDocumentComplete()
        {
            if (!_disableLogging) MessageHandler.Info("Netflix. Url: {0}, State: {1}", Url, _currentState.ToString());
            switch (_currentState)
            {
                case State.Login:
                    {
                        if (Url.ToLowerInvariant().Contains("/browse"))
                        {
                            _currentState = State.None;
                            return EventResult.Error("ignore this");
                        }
                        break;
                    }
                case State.Playing:
                    {
                        ProcessComplete.Finished = true;
                        ProcessComplete.Success = true;
                        break;
                    }
            }
            return EventResult.Complete();
        }
    }
}

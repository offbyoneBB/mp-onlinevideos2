using Newtonsoft.Json;
using OnlineVideos.Helpers;
using OnlineVideos.Sites.Entities;
using System.Diagnostics;
using System.Windows.Forms;
using System.Reflection;
using System.ComponentModel;

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

        private ConnectorSettings _connectorSettings;

        private State _currentState = State.None;
        private bool _isPlayingOrPausing = false;
        private PreviewKeyDownEventHandler _oldKeyDown = null;

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
            if (!_connectorSettings.disableLogging) MessageHandler.Info("Netflix. Input: {0}", actionEnumName);
            if (_currentState == State.Playing && !_isPlayingOrPausing)
            {
                if (actionEnumName == "REMOTE_0" && _connectorSettings.enableNetflixOsd)
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
            _connectorSettings = JsonConvert.DeserializeObject<ConnectorSettings>(System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(password)));

            if (username == "GET")
            {
                Application.DoEvents();
                _currentState = State.Login;
                ProcessComplete.Finished = false;
                ProcessComplete.Success = false;
                Url = @"https://www.netflix.com/Login";
                RemoveEvent();
                Browser.PreviewKeyDown += Browser_PreviewKeyDown;
            }
            else
            {
                if (_connectorSettings.showLoadingSpinner)
                    ShowLoading();
                ProcessComplete.Finished = true;
                ProcessComplete.Success = true;
                _currentState = State.ReadyToPlay;
            }
            return EventResult.Complete();
        }

        private void RemoveEvent()
        {

            FieldInfo f1 = typeof(Control).GetField("EventPreviewKeyDown",
                BindingFlags.Static | BindingFlags.NonPublic);

            object obj = f1.GetValue(Browser);
            PropertyInfo pi = Browser.GetType().GetProperty("Events",
                BindingFlags.NonPublic | BindingFlags.Instance);

            EventHandlerList list = (EventHandlerList)pi.GetValue(Browser, null);
            _oldKeyDown = (PreviewKeyDownEventHandler)list[obj];
            list.RemoveHandler(obj, list[obj]);
        }

        private void Browser_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (_connectorSettings.enableIEDebug)
            {
                if (e.KeyValue == 'G' && e.Alt)
                {
                    Url = @"https://www.google.com";
                }
                else
                if (e.KeyValue == (int)Keys.F4 && e.Alt)
                {
                    var newe = new PreviewKeyDownEventArgs(Keys.Escape);
                    _oldKeyDown(sender, newe);
                }
            }
            else
                if (e.KeyValue == (int)Keys.Escape)
                _oldKeyDown(sender, e);
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
            if (!_connectorSettings.disableLogging) MessageHandler.Info("Netflix. Input: {0}", "Play");
            return PlayPause();
        }

        public override Entities.EventResult Pause()
        {
            if (!_connectorSettings.disableLogging) MessageHandler.Info("Netflix. Input: {0}", "Pause");
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
            if (!_connectorSettings.disableLogging) MessageHandler.Info("Netflix. Url: {0}, State: {1}", Url, _currentState.ToString());
            switch (_currentState)
            {
                case State.Login:
                    {
                        if (Url.ToLowerInvariant().Contains("/browse") && !_connectorSettings.enableIEDebug)
                        {
                            _currentState = State.None;
                            return EventResult.Error("ignore this");
                        }
                        break;
                    }
                case State.Playing:
                    {
                        if (_connectorSettings.showLoadingSpinner)
                            HideLoading();
                        ProcessComplete.Finished = true;
                        ProcessComplete.Success = true;
                        break;
                    }
            }
            return EventResult.Complete();
        }
    }
}

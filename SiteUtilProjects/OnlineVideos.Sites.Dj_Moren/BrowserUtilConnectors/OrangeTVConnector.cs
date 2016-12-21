using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites;
using OnlineVideos.Sites.Entities;
using System.Windows.Forms;
using OnlineVideos.Helpers;

using System.Drawing;
using System.Threading;
using System.Diagnostics;

namespace OnlineVideos.Sites.BrowserUtilConnectors
{
    public class OrangeTVConnector : BrowserUtilConnector
    {

        private enum State
        {
            None,
            Login,
            ReadyToPlay,
            Playing
        }

        private string _alturaVideo;
        private string _scriptDelayTime;
        private State _currentState = State.None;
        private bool _isPlayingOrPausing = false;

        private void SendKeyToBrowser(string key)
        {
            MessageHandler.Debug("SendKeyToBrowser");
            Cursor.Position = new System.Drawing.Point(Browser.FindForm().Location.X + 300, Browser.Location.Y + 300);
            Application.DoEvents();
            CursorHelper.DoLeftMouseClick();
            Application.DoEvents();
            System.Windows.Forms.SendKeys.Send(key);
        }

        public override void OnClosing()
        {
            MessageHandler.Debug("OnClosing");
            Process.GetCurrentProcess().Kill();
        }
        public override void OnAction(string actionEnumName)
        {
            MessageHandler.Debug("OnAction");
            if (_currentState == State.Playing && !_isPlayingOrPausing)
            {
                if (actionEnumName == "REMOTE_0")
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
            MessageHandler.Debug("PerformLogin");
            _currentState = State.Login;
            _alturaVideo = username;
            _scriptDelayTime = password;
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            Url = @"https://orangetv.orange.es";///atv/api/authenticate?appId=es.orange.pc&appVersion=1.0&deviceIdentifier=036f4b3c-599d-4773-bbe3-537ee0c4202e&username=930042856&password=temppass";
            return EventResult.Complete();
        }


        public override Entities.EventResult PlayVideo(string videoToPlay)
        {
            MessageHandler.Debug("PlayVideo");
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            Url = videoToPlay;
            _currentState = State.Playing;
            return EventResult.Complete();
        }

        public override Entities.EventResult Play()
        {
            MessageHandler.Debug("Play");
            return PlayPause();
        }

        public override Entities.EventResult Pause()
        {
            MessageHandler.Debug("Pause");
            return PlayPause();
        }

        private EventResult PlayPause()
        {
            MessageHandler.Debug("PlayPause");
            if (_currentState != State.Playing || _isPlayingOrPausing || Browser.Document == null || Browser.Document.Body == null) return EventResult.Complete();
            _isPlayingOrPausing = true;
            SendKeyToBrowser(" ");
            _isPlayingOrPausing = false;
            return EventResult.Complete();
        }


        public override Entities.EventResult BrowserDocumentComplete()
        {
            MessageHandler.Debug("BrowserDocumentComplete");
            switch (_currentState)
            {
                case State.Login:
                    _currentState = State.ReadyToPlay;
                    break;
                case State.ReadyToPlay:
                    ProcessComplete.Finished = true;
                    ProcessComplete.Success = true;
                    break;
                case State.Playing:
                    _currentState = State.Playing;
                    ProcessComplete.Finished = true;
                    ProcessComplete.Success = true;
                    string jsCode = "var preparePlayer = function(){$('#zone-top, .player-overlay, .live-info-panel, .channel-list, .orange-deco').remove();" +
                             "$('#zone-content').css('margin-top', 0).css('max-width', '100%');" +
                             "$('.playerplugin object').css('height', "+ _alturaVideo + ");}; setTimeout(preparePlayer, "+ _scriptDelayTime + ");";
                    InvokeScript(jsCode);
                    MessageHandler.Debug("Ejecuto script");
                    break;
            }
            return EventResult.Complete();
        }
    }
}

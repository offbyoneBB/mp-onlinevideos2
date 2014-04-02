using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites.Base;
using OnlineVideos.Sites.Entities;
using System.Windows.Forms;
using System.Web;
using System.Threading;
using OnlineVideos.Helpers;



namespace OnlineVideos.Sites.BrowserUtilConnectors
{
    public abstract class ViaplayConnectorBase : BrowserUtilConnector
    {
        protected enum State
        {
            None,
            OpenPage,
            LoginAndPlay,
            Playing
        }

        protected bool _isPlayingOrPausing = false;
        protected State _currentState = State.None;
        protected string _username;
        protected string _password;

        public abstract string BaseUrl { get; }

        public override EventResult Pause()
        {
            return PlayPause();
        }

        public override EventResult Play()
        {
            return PlayPause();
        }

        private EventResult PlayPause()
        {
            if (_currentState != State.Playing || _isPlayingOrPausing || Browser.Document == null || Browser.Document.Body == null) return EventResult.Complete();
            _isPlayingOrPausing = true;
            var x = Browser.FindForm().Left + 35;
            var y = Browser.FindForm().Bottom - 35;
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
            _isPlayingOrPausing = false;
            return EventResult.Complete();
        }

        public override EventResult PerformLogin(string username, string password)
        {
            _password = password;
            _username = username;
            _currentState = State.None;
            Url = "about:blank";
            ProcessComplete.Finished = true;
            ProcessComplete.Success = true;
            return EventResult.Complete();
        }

        public override EventResult PlayVideo(string videoToPlay)
        {
            ProcessComplete.Finished = true;
            ProcessComplete.Success = true;
            Uri uri = new Uri(BaseUrl + videoToPlay);
            Url = uri.GetLeftPart(UriPartial.Path);
            _currentState = State.OpenPage;
            return EventResult.Complete();
        }

        public override EventResult BrowserDocumentComplete()
        {
            switch (_currentState)
            {
                case State.OpenPage:
                    _currentState = State.LoginAndPlay;
                    break;
                case State.LoginAndPlay:
                    //Pause 4 sec. before trying
                    var js = "setTimeout(\"myPlay()\",4000);function myLogin(){if ($('section.login-required').length > 0) {$('section.login-required').find('input[type=email].username').filter(':visible:first').val(\"" + _username + "\");$('section.login-required').find('input[type=password].password').filter(':visible:first').val(\"" + _password + "\");$('section.login-required').find('input[type=submit]').filter(':visible:first').click();} else {setTimeout(\"myLogin()\",250);}};function myPlay() {if ($('figure.mediaplayer>a.play:first').length > 0) {$('figure.mediaplayer>a.play:first').click();setTimeout(\"myLogin()\",250);} else {setTimeout(\"myPlay()\",250);}};";
                    InvokeScript(js);
                    _currentState = State.Playing;
                    break;
                case State.Playing:
                    // Remove banner
                    InvokeScript("setTimeout(function(){if ($('#hellobar-close').length != 0) { $('#hellobar-close').click(); }}, 10000);");
                    _currentState = State.Playing;
                    break;
                default:
                    break;

            }
            ProcessComplete.Finished = true;
            ProcessComplete.Success = true;
            return EventResult.Complete();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites;
using OnlineVideos.Sites.Entities;
using System.Windows.Forms;
using System.Threading;
using OnlineVideos.Helpers;
using System.Diagnostics;
using OnlineVideos.Sites.Utils;

namespace OnlineVideos.Sites.BrowserUtilConnectors
{
    public class HboNordicConnector : BrowserUtilConnector
    {
        protected enum State
        {
            None,
            LoadLogIn,
            LogIn,
            DoLogIn,
            Play,
            Playing
        }
        bool debug = false;
        protected bool _isPlayingOrPausing = false;
        protected bool _hdEnabled;
        protected bool _doEnableHd;
        protected State _currentState = State.None;

        public const string baseUrl = "http://hbonordic.com";
        private string _password;
        private string _username;


        public string LoginUrl
        {
            get { return baseUrl + "/home"; }
        }

        public override EventResult PerformLogin(string username, string password)
        {
            _password = password;
            _username = username;
            _currentState = State.None;
            Url = LoginUrl;
            ProcessComplete.Finished = true;
            ProcessComplete.Success = true;
            return EventResult.Complete();
        }

        public override EventResult PlayVideo(string videoToPlay)
        {
            _doEnableHd = videoToPlay.Contains("DOENABLEHD");
            _hdEnabled = false;
            videoToPlay = videoToPlay.Replace("DOENABLEHD", "");
            ProcessComplete.Finished = true;
            ProcessComplete.Success = true;
            Url = baseUrl + videoToPlay;
            _currentState = State.LoadLogIn;
            return EventResult.Complete();
        }

        public override EventResult Play()
        {

            return PlayPause();
        }

        public override EventResult Pause()
        {
            return PlayPause();
        }

        private EventResult PlayPause()
        {
            if (_currentState != State.Playing || _isPlayingOrPausing || Browser.Document == null || Browser.Document.Body == null) return EventResult.Complete();
            _isPlayingOrPausing = true;
            int x = Browser.FindForm().Left + 30;
            if (_doEnableHd && !_hdEnabled)
            {
                x = Browser.FindForm().Right - 235;
                _hdEnabled = true;
            }
            var y = Browser.FindForm().Bottom - 10;
            // Need to move the cursor a lot, slow animations.
            Cursor.Position = new System.Drawing.Point(x, Browser.FindForm().Bottom - 200);
            Application.DoEvents();
            // We have to move the cursor to show the play/pause button
            while (Cursor.Position.Y < y)
            {
                Cursor.Position = new System.Drawing.Point(x, Cursor.Position.Y + 2);
                Application.DoEvents();
            }
            Cursor.Position = new System.Drawing.Point(x, y);
            Application.DoEvents();
            CursorHelper.DoLeftMouseClick();
            Application.DoEvents();
            _isPlayingOrPausing = false;
            return EventResult.Complete();
        }

        private void runJs()
        {
            var js = "var tryToPlayTimer;";
            js += "var playArray = ['Play','Toista'];";
            js += "function fn(){";
            js += @"if (playArray.indexOf(document.elementFromPoint(window.innerWidth/2,342).innerText.replace(/^\s+|\s+$/g, '')) === -1 && playArray.indexOf(document.elementFromPoint(window.innerWidth/2,322).innerText.replace(/^\s+|\s+$/g, '')) === -1) return;";
            js += "clearInterval(tryToPlayTimer);";
            js += "setTimeout(\"fn2()\",750);";
            js += "};";
            js += "function fn2(){";
            js += "var i342 = document.elementFromPoint(window.innerWidth/2,342);";
            js += @"if (playArray.indexOf(i342.innerText.replace(/^\s+|\s+$/g, '')) > -1){";
            js += "i342.click();";
            js += "} else {";
            js += "document.elementFromPoint(window.innerWidth/2,322).click();";
            js += "};";
            js += "setTimeout(\"fn3()\",500);";
            js += "};";
            js += "function fn3(){";
            js += "var fset = document.getElementsByTagName('fieldset');";
            js += "if (fset.length > 0){";
            js += "fset[0].querySelectorAll('input[type=email]')[0].value = '" + _username + "';";
            js += "fset[0].querySelectorAll('input[type=password]')[0].value = '" + _password + "';";
            js += "fset[0].querySelectorAll('input[type=submit]')[0].click();";
            js += "} else {";
            js += "document.getElementsByTagName('html')[0].style.overflow = 'hidden';";
            js += "var container = document.getElementsByClassName('hbo_video_container')[0];";
            js += "container.style.height = window.innerHeight + 'px';";
            js += "container.className = \"\";";
            js += "container.className = \"js-hbo_video_container hbo_video_container fixed topleft full_width clip_container row z_9\";";
            js += "var obj = document.getElementsByTagName('object')[0];";
            js += "obj.style.height = window.innerHeight + 'px';";
            js += "obj.style.width = window.innerWidth + 'px';";
            js += "obj.height = window.innerHeight + 'px';";
            js += "obj.width = window.innerWidth + 'px';";
            js += "}};";
            js += "$(document).ready(function() {tryToPlayTimer = setInterval(\"fn()\",250);});";
            InvokeScript(js);
        }

        public override EventResult BrowserDocumentComplete()
        {
            if (debug)
            {
                InvokeScript("alert('Debug Time!')");
                debug = false;
            }
            switch (_currentState)
            {
                case State.LoadLogIn:
                    ProcessComplete.Finished = true;
                    ProcessComplete.Success = true;
                    _currentState = State.LogIn;
                    break;
                case State.LogIn:
                    runJs();
                    ProcessComplete.Finished = true;
                    ProcessComplete.Success = true;
                    _currentState = State.DoLogIn;
                    break;
                case State.DoLogIn:
                    ProcessComplete.Finished = true;
                    ProcessComplete.Success = true;
                    _currentState = State.Play;
                    break;
                case State.Play:
                    runJs();
                    _currentState = State.Playing;
                    ProcessComplete.Finished = true;
                    ProcessComplete.Success = true;
                    break;
                case State.Playing:
                    ProcessComplete.Finished = true;
                    ProcessComplete.Success = true;
                    break;
            }
            return EventResult.Complete();
        }
    }
}
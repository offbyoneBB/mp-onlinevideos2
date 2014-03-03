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
            LogInFaze1,
            LogInFaze2,
            Play,
            Playing
        }

        protected bool _isPlayingOrPausing = false;
        protected State _currentState = State.None;
        protected string _username;
        protected string _password;

        public abstract string BaseUrl { get; }

        public string LoginUrl
        {
            get { return BaseUrl; }
        }

        public override EventResult PlayVideo(string videoToPlay)
        {
            ProcessComplete.Finished = true;
            ProcessComplete.Success = true;
            Uri uri = new Uri(BaseUrl + videoToPlay);
            Url = uri.GetLeftPart(UriPartial.Path);
            _currentState = State.Play;
            return EventResult.Complete();
        }

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
            _currentState = State.LogInFaze1;
            Url = LoginUrl;
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            return EventResult.Complete() ;
        }

        public override EventResult BrowserDocumentComplete()
        {
            bool loggedIn;
            HtmlElementCollection forms;
            switch (_currentState)
            {
                case State.LogInFaze1:
                    forms = Browser.Document.GetElementsByTagName("form");
                    loggedIn = true;
                    if (forms != null && forms.Count > 0)
                    {
                        foreach (HtmlElement form in forms)
                        {
                            var className = form.GetAttribute("className");
                            loggedIn &= (string.IsNullOrEmpty(className) || className != "menu-login-form");
                        }
                    }
                    if (!loggedIn)
                    {
                        var js = string.Format(@"$('input.username').val('{0}');$('input.password').val('{1}');$('form.menu-login-form').submit();", _username, _password);
                        InvokeScript(js);
                        //wait for login... Can take some time...    
                        Thread.Sleep(4000);
                        Url = LoginUrl;
                        _currentState = State.LogInFaze2;
                        Browser.Refresh(WebBrowserRefreshOption.Completely);// Need to load page again
                    }
                    else
                    {
                        _currentState = State.None;
                        ProcessComplete.Finished = true;
                        ProcessComplete.Success = true;
                    }
                    break;
                   
                case State.LogInFaze2:
                    _currentState = State.None;

                    forms = Browser.Document.GetElementsByTagName("form");
                    loggedIn = true;
                    if (forms != null && forms.Count > 0)
                    {
                        foreach (HtmlElement form in forms)
                        {
                            var className = form.GetAttribute("className");
                            loggedIn &= (string.IsNullOrEmpty(className) || className != "menu-login-form");
                        }
                    }
                    if (loggedIn)
                    {
                        ProcessComplete.Finished = true;
                        ProcessComplete.Success = true;
                    }
                    else
                    {
                        ProcessComplete.Finished = true;
                        ProcessComplete.Success = true;
                        return EventResult.Error("Could not log in!");

                    }
                    break;
                case State.Play:
                    //Click play button, but first wait for page
                    InvokeScript("setTimeout(function(){$('a.play:first').click()}, 2000);");
                    _currentState = State.Playing;
                    ProcessComplete.Finished = true;
                    ProcessComplete.Success = true;
                    break;
                case State.Playing:
                    // Remove banner
                    InvokeScript("setTimeout(function(){if ($('#hellobar-close').length != 0) { $('#hellobar-close').click(); }}, 5000);");
                    _currentState = State.Playing;
                    ProcessComplete.Finished = true;
                    ProcessComplete.Success = true;
                    break;
            }
            return EventResult.Complete();
        }
    }
}

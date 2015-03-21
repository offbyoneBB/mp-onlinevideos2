using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites;
using OnlineVideos.Sites.Entities;
using System.Windows.Forms;
using System.Web;
using System.Threading;
using OnlineVideos.Helpers;
using OnlineVideos.Sites.Properties;
using System.Drawing;



namespace OnlineVideos.Sites.BrowserUtilConnectors
{
    public abstract class ViaplayConnectorBase : BrowserUtilConnector
    {
        protected enum State
        {
            None,
            OpenPage,
            LoginAndPlay,
            StartPlaying,
            Playing
        }

        protected State _currentState = State.None;
        protected string _username;
        protected string _password;


        public abstract string BaseUrl { get; }

        private bool doInit = true;
        private void initJs()
        {
            if (doInit)
            {
                if (Url.Contains("/player") && Browser.Document.GetElementById("videoPlayer") != null)
                {
                    doInit = false;
                    InvokeScript(Properties.Resources.ViaplayVideoControlJs);
                }
            }
        }

        public override void OnAction(string actionEnumName)
        {
            if (_currentState != State.Playing) return;

            if (actionEnumName == "ACTION_MOVE_LEFT")
            {
                initJs();
                // We have to move the cursor to show the OSD
                Cursor.Position = new System.Drawing.Point(Browser.FindForm().Location.X + 200, Browser.FindForm().Location.Y + 200);
                Application.DoEvents();
                Cursor.Position = new System.Drawing.Point(Browser.FindForm().Location.X + 300, Browser.FindForm().Location.Y + 300);
                Application.DoEvents();
                InvokeScript("try { back(); } catch(e) {}");
            }
            if (actionEnumName == "ACTION_MOVE_RIGHT")
            {
                initJs();
                // We have to move the cursor to show the OSD
                Cursor.Position = new System.Drawing.Point(Browser.FindForm().Location.X + 200, Browser.FindForm().Location.Y + 200);
                Application.DoEvents();
                Cursor.Position = new System.Drawing.Point(Browser.FindForm().Location.X + 300, Browser.FindForm().Location.Y + 300);
                Application.DoEvents();
                InvokeScript("try { forward(); } catch(e) {}");
            }
        }


        public override EventResult Pause()
        {

            return PlayPause();
        }

        public override EventResult Play()
        {
            return PlayPause();
        }

        protected bool _paused = false;
        private EventResult PlayPause()
        {
            if (_currentState != State.Playing) return EventResult.Complete();
            initJs();

            // We have to move the cursor to show the OSD
            Cursor.Position = new System.Drawing.Point(Browser.FindForm().Location.X + 200, Browser.FindForm().Location.Y + 200);
            Application.DoEvents();
            Cursor.Position = new System.Drawing.Point(Browser.FindForm().Location.X + 300, Browser.FindForm().Location.Y + 300);
            Application.DoEvents();

            if (_paused)
                InvokeScript("try { play(); } catch(e) {}");
            else
                InvokeScript("try { pause(); } catch(e) {}");

            _paused = !_paused;
            return EventResult.Complete();
        }


        public override EventResult PerformLogin(string username, string password)
        {
            ShowLoading();
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
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
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
                    //Pause two sec. before trying (wait for animation after page loaded)
                    var js = "setTimeout(\"myPlay()\",2000);function myLogin(){if ($('section.login-required').length > 0) {$('section.login-required').find('input[type=email].username').filter(':visible:first').val(\"" + _username + "\");$('section.login-required').find('input[type=password].password').filter(':visible:first').val(\"" + _password + "\");$('section.login-required').find('input[type=submit]').filter(':visible:first').click();} else {setTimeout(\"myLogin()\",250);}};function myPlay() {if ($('figure.mediaplayer>a.play:first').length > 0 && $('figure.mediaplayer>a.play:first').is(':visible')) {$('figure.mediaplayer>a.play:first').click();setTimeout(\"myLogin()\",250);} else {setTimeout(\"myPlay()\",250);}};";
                    InvokeScript(js);
                    _currentState = State.StartPlaying;
                    break;
                case State.StartPlaying:
                    // Remove banner
                    //Wait some time.. sometimes slow ajax/javascriptloading.
                    InvokeScript("setTimeout(function(){if ($('#hellobar-close').length != 0) { $('#hellobar-close').click(); }}, 8000);");
                    //Remove cookie banner
                    InvokeScript("setTimeout(function(){if ($('.button.agree-button').length != 0) { $('.button.agree-button:first').click(); }}, 9000);");
                    ProcessComplete.Finished = true;
                    ProcessComplete.Success = true;
                    _currentState = State.Playing;
                    break;
                case State.Playing:
                    ProcessComplete.Finished = true;
                    ProcessComplete.Success = true;
                    HideLoading();
                    break;
                default:
                    break;

            }
            return EventResult.Complete();
        }
    }
}

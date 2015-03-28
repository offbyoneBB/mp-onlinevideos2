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
            Login,
            PostLogin,
            StartPlaying,
            WaitForPlay,
            Playing
        }

        protected State _currentState = State.None;
        protected string _username;
        protected string _password;
        protected string _videoToPlay;

        public abstract string BaseUrl { get; }
        public abstract string LoginUrl { get; }

        public override EventResult PerformLogin(string username, string password)
        {
            ShowLoading();
            _password = password;
            _username = username;
            _currentState = State.Login;
            Url = LoginUrl;
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            return EventResult.Complete();
        }

        public override EventResult PlayVideo(string videoToPlay)
        {
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            _videoToPlay = videoToPlay;
            Uri uri = new Uri(BaseUrl + videoToPlay);
            Url = uri.GetLeftPart(UriPartial.Path);
            _currentState = State.StartPlaying;
            return EventResult.Complete();
        }

        public override EventResult BrowserDocumentComplete()
        {
            switch (_currentState)
            {
                case State.Login:
                    if (Url == LoginUrl)
                    {
                        InvokeScript(Properties.Resources.ViaplayPlayMovieJs + "setTimeout(\"myLogin('" + _username + "','" + _password + "')\", 500);");
                    }
                    else
                    {
                        _currentState = State.None;
                        ProcessComplete.Finished = true;
                        ProcessComplete.Success = true;
                    }
                    break;
                case State.PostLogin:
                        _currentState = State.None;
                        ProcessComplete.Finished = true;
                        ProcessComplete.Success = true;
                        break;
                case State.StartPlaying:
                    InvokeScript(Properties.Resources.ViaplayPlayMovieJs + "__url = '"+ _videoToPlay + "'; setTimeout(\"myPlay()\", 1000);");
                    _currentState = State.WaitForPlay;
                    break;
                case State.WaitForPlay:
                    if (Url.Contains("/player"))
                    {
                        ProcessComplete.Finished = true;
                        ProcessComplete.Success = true;
                        _currentState = State.Playing;
                        HideLoading();
                    }
                    break;
                case State.Playing:
                    ProcessComplete.Finished = true;
                    ProcessComplete.Success = true;
                    break;
                default:
                    break;
            }
            return EventResult.Complete();
        }

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

    }
}

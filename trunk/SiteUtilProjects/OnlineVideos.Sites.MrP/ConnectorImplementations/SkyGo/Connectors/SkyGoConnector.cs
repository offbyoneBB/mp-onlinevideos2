using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites.Entities;
using OnlineVideos.Sites;
using System.Windows.Forms;
using OnlineVideos.Helpers;
using System.Threading;
using System.Runtime.InteropServices;

namespace OnlineVideos.Sites.WebAutomation.ConnectorImplementations.SkyGo.Connectors
{
    public class SkyGoConnector : BrowserUtilConnectorBase
    {

        /// <summary> 
        /// The states this connector can be in - useful when waiting for browser responses
        /// </summary>
        private enum State
        { 
            None,
            LoggingIn,
            LoginResult,
            PlayPage,
            PlayPageLiveTv
        }

        private State _currentState = State.None;
        private string _username;
        private string _password;
        private DateTime _lastPlayClick = DateTime.Now;
        private bool _isSilverlightAppStorageEnabled = true;
        private Thread  _disableAppStorageThread;
        private Thread _playPressThread;
        /// <summary>
        /// Perform a log in to the sky go site
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        protected override EventResult PerformActualLogin(string username, string password)
        {
            Browser.NewWindow += Browser_NewWindow;
            // Enable silverlight application storage initially
            _isSilverlightAppStorageEnabled = WebBrowserHelper.IsSilverlightAppStorageEnabled();
            WebBrowserHelper.ToogleSilverlightAppStorage(true);
            _username = username;
            _password = password;
            _currentState = State.LoggingIn;
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            Url = Properties.Resources.SkyGo_LoginUrl;
            return EventResult.Complete();
        }

        /// <summary>
        /// Don't launch a new window if one is created
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Browser_NewWindow(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }

        /// <summary>
        /// Ensure app storage is set to its previous state
        /// </summary>
        public override void OnClosing()
        {
            WebBrowserHelper.ToogleSilverlightAppStorage(_isSilverlightAppStorageEnabled);
            base.OnClosing();
        }

        /// <summary>
        /// Process a message from the web browser
        /// </summary>
        /// <returns></returns>
        public override EventResult BrowserDocumentComplete()
        {
            switch (_currentState)
            {
                case State.LoggingIn:
                    if (Url.EndsWith("/signin/skygo"))
                    {
                        var jsCode = "document.getElementById('username').value = '" + _username + "';";
                        jsCode += "document.getElementById('password').value = '" + _password + "';";
                        jsCode += "document.getElementById('signinform').submit();";
                        InvokeScript(jsCode);
                        _currentState = State.LoginResult;
                    }
                    break;
                case State.LoginResult:
                    if (Url == "http://go.sky.com/")
                    {
                        _currentState = State.None;
                        ProcessComplete.Finished = true;
                        ProcessComplete.Success = true;
                    }
                    else
                        return EventResult.Error("SkyGoGeneralConnector/ProcessMessage/Expected home page after log in, was actually " + Url);
                    break;
                case State.PlayPage:
                    if (Url.Contains("/content/videos"))
                    {
                        //Browser.Refresh(WebBrowserRefreshOption.Completely);// Need to do this for some reason
                        _currentState = State.None;

                         // The js code to wait for the video to appear
                        var jsCode = "setTimeout('doMaximise()', 1000);";
                        jsCode += "function doMaximise() {";
                        jsCode += "if(document.getElementsByClassName('silverlightVodPlayerWrapper') != null) {";
                        jsCode += "    document.getElementsByClassName('silverlightVodPlayerWrapper')[0].setAttribute('style', 'position: fixed; width: 100%; height: 100%; left: 0; top: 0; background: rgba(51,51,51,0.7); z-index: 10;');";
                        jsCode += "}";
                        jsCode += "else setTimeout('doMaximise()', 1000);";
                        jsCode += "}";

                        InvokeScript(jsCode);

                        var startTime = DateTime.Now;
                        
                        _playPressThread = new Thread(new ParameterizedThreadStart(ClickPlayAfterFullScreen));
                        _playPressThread.Start();
                      
                        ProcessComplete.Finished = true;
                        ProcessComplete.Success = true;

                        // Wait 15 seconds for the video to start before disabling app storage - we'll do this in a separate thread
                        if (_disableAppStorageThread == null)
                        {
                            _disableAppStorageThread = new Thread(new ParameterizedThreadStart(DisableAppStorage));
                            _disableAppStorageThread.Start();
                        }
                        
                        Browser.FindForm().Activate();
                        Browser.FindForm().Focus();
                    }
                    else
                    {
                        if (!Url.EndsWith("/content/videos"))
                            return EventResult.Error("SkyGoOnDemandConnector/ProcessMessage/Expected video play page, was actually " + Url);
                    }
                    break;
                case State.PlayPageLiveTv:
                    if (Url.Contains("/detachedLiveTv.do"))
                    {
                        // After 4 seconds we'll assume the page has loaded and will click in the top corner
                        var endDate = DateTime.Now.AddSeconds(4);
                        while (DateTime.Now < endDate)
                        {
                            Application.DoEvents();
                            System.Threading.Thread.Sleep(200);
                        }

                        CursorHelper.MoveMouseTo(50, 50);
                        Application.DoEvents();
                        CursorHelper.DoLeftMouseClick();
                        Application.DoEvents();
                        HideLoading();
                        Browser.FindForm().Activate();
                        Browser.FindForm().Focus();
                        
                        ProcessComplete.Finished = true;
                        ProcessComplete.Success = true;
                    }

                    break;
            }

            return EventResult.Complete();
        }

        /// <summary>
        /// Play the video from the start
        /// </summary>
        /// <param name="videoToPlay"></param>
        /// <returns></returns>
        public override EventResult PlayVideo(string videoToPlay)
        {
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            _currentState = State.PlayPage;
            Url = Properties.Resources.SkyGo_VideoPlayUrl(videoToPlay);
            return EventResult.Complete();
        }

        /// <summary>
        /// Resume a paused video
        /// </summary>
        /// <returns></returns>
        public override EventResult Play()
        {
            MessageHandler.Info("Play", "");
            return DoPlayOrPause();
        }

        /// <summary>
        /// Pause the video
        /// </summary>
        /// <returns></returns>
        public override EventResult Pause()
        {
            MessageHandler.Info("Pause", "");
            return DoPlayOrPause();
        }

        /// <summary>
        /// Find the play/pause button and click it
        /// </summary>
        /// <returns></returns>
        private EventResult DoPlayOrPause()
        {
            if (DateTime.Now < _lastPlayClick.AddMilliseconds(500)) return EventResult.Complete();

            CursorHelper.MoveMouseTo(Browser.FindForm().Left + (Browser.FindForm().Width / 2) + 50, Browser.FindForm().Top + (Browser.FindForm().Height / 2));
            CursorHelper.DoLeftMouseClick();
            CursorHelper.MoveMouseTo(0, 0);
            _lastPlayClick = DateTime.Now;
            return EventResult.Complete();
        }

        /// <summary>
        /// We'll disable the app storage a few seconds after starting the video (we'll do this on a separate thread so the browser continues)
        /// </summary>
        /// <param name="data"></param>
        private void DisableAppStorage(object data)
        {
            var endDate = DateTime.Now.AddSeconds(15);

            while (DateTime.Now < endDate)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(200);
            }
            
            WebBrowserHelper.ToogleSilverlightAppStorage(false);
        }

        /// <summary>
        /// After we've gone full screen we'll click the play button 
        /// </summary>
        /// <param name="data"></param>
        private void ClickPlayAfterFullScreen(object data)
        {
            var endDate = DateTime.Now.AddSeconds(5);

            while (DateTime.Now < endDate)
            {
                Application.DoEvents();
                Thread.Sleep(100);
                Browser.FindForm().BeginInvoke((MethodInvoker)delegate()
                {
                    var firstMatchingDiv = GetFirstMatchingDivClass(Browser.Document, "silverlightVodPlayerWrapper");

                    if (firstMatchingDiv != null && firstMatchingDiv.GetAttribute("style") != null)
                    {
                        if (firstMatchingDiv.OuterHtml.Contains("position: fixed"))
                        {
                            HideLoading();
                            DoPlayOrPause();
                            return;
                        }
                    }
                });
            }
          
        }

        /// <summary>
        /// Find the first div which matches the required class name
        /// </summary>
        /// <param name="document"></param>
        /// <param name="className"></param>
        /// <returns></returns>
        private HtmlElement GetFirstMatchingDivClass( HtmlDocument document,  string className)
        {
            foreach (HtmlElement element in Browser.Document.GetElementsByTagName("div"))
            {
                if (element.OuterHtml.Contains("class=") && element.OuterHtml.Contains(className))
                    return element;
            }
            return null;
        }
    }
}

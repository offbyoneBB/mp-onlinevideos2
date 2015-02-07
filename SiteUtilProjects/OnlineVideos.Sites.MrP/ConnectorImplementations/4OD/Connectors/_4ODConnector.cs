using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using OnlineVideos.Sites.Entities;
using OnlineVideos.Sites;
using System.Xml;
using System.Threading;
using OnlineVideos.Helpers;
using OnlineVideos.Sites.WebAutomation.Properties;
using System.Drawing;

namespace OnlineVideos.Sites.WebAutomation.ConnectorImplementations._4OD.Connectors
{
    public class _4ODConnector : BrowserUtilConnectorBase
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
            Playing
        }

        private State _currentState = State.None;
        private string _username;
        private string _password;
        private string _nextVideoToPlayId;
        private string _nextVideoToPlayName;
        private bool _lastButtonPause = false;
        private Panel _blankPanel = new Panel();

        /// <summary>
        /// Perform a log in to the 4OD site
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        protected override EventResult PerformActualLogin(string username, string password)
        {
         
            _username = username;
            _password = password;
            _currentState = State.LoggingIn;
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            Url = Properties.Resources._4OD_LoginUrl;
            return EventResult.Complete();
        }

        /// <summary>
        /// Navigate to the video playing screen
        /// The video to play id requires the program name and the id of the episode as tilde delimited, e.g.:
        /// 
        /// time-team~3513463
        /// </summary>
        /// <param name="videoToPlay"></param>
        /// <returns></returns>
        public override EventResult PlayVideo(string videoToPlay)
        {
            // Need to clear the swf file from the internet cache first 
            // It took a long time to figure out that this was the problem with the web browser control not loading the video after the first go
            RemoveFileFromTempInternetFiles("4odplayer", ".swf");
            _currentState = State.PlayPage;
            ProcessComplete.Finished = true;
            ProcessComplete.Success = true;
            _nextVideoToPlayId = videoToPlay.Split('~')[1];
            _nextVideoToPlayName = videoToPlay.Split('~')[0];
            Url = Properties.Resources._4OD_VideoPlayUrl(_nextVideoToPlayName, _nextVideoToPlayId);
            
            return EventResult.Complete();
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
                    if (Url.Contains("4id.channel4.com/login"))
                    {
                        // The js code to actually do the login
                        var loginJsCode = "document.getElementById('capture_first_signIn_emailAddress').value = '" + _username + "';";
                        loginJsCode += "document.getElementById('capture_first_signIn_password').value = '" + _password + "';";
                        loginJsCode += "document.getElementById('capture_first_signIn_signInButton').click();";
                        //loginJsCode += "document.getElementById('userInformationForm').submit();";

                        // The js code to wait for the login box to appear
                        var jsCode = "setTimeout('doLogin()', 1000);";
                        jsCode += "function doLogin() {";
                        jsCode += "if(document.getElementById('capture_first_signIn_signInButton') != null) {";
                        jsCode += loginJsCode;
                        jsCode += "}";
                        jsCode += "else setTimeout('doLogin()', 1000);";
                        jsCode += "}";

                        InvokeScript(jsCode);
                        _currentState = State.LoginResult;
                    }
                    else
                    {
                        // Already logged in
                        if (Url.EndsWith("www.channel4.com/"))
                        {
                            _currentState = State.None;
                            ProcessComplete.Finished = true;
                            ProcessComplete.Success = true;
                        }
                    }
                    break;
                case State.LoginResult:
                    if (Url.EndsWith("www.channel4.com/"))
                    {
                        _currentState = State.None;
                        ProcessComplete.Finished = true;
                        ProcessComplete.Success = true;
                    }
                    //else
                    //return EventResult.Error("C4ODGeneralConnector/ProcessMessage/Expected home page after log in, was actually " + _parent.Url);
                    break;
                case State.PlayPage:
                    if (Url.Contains(_nextVideoToPlayName))
                    {
                       /* var swfElement = Browser.Document.GetElementById("catchUpPlayer");

                        if (swfElement != null)
                        {
                            DoResize();
                            // Not 100% sure why, but for some reason the page does reload after the initial element is loaded
                            // We'll basically do the accept and resize on the second refresh
                            _currentState = State.None;
                            ProcessComplete.Finished = true;
                            ProcessComplete.Success = true;
                        }
                        */

                        HideLoading();
                        // Hide the controls of the flash player
                        _blankPanel.Width = Browser.Width;
                        _blankPanel.Height = 50;
                        _blankPanel.BackColor = Color.Black;
                        _blankPanel.Top = Browser.FindForm().Bottom - 50;

                        Browser.FindForm().Controls.Add(_blankPanel);
                        _blankPanel.BringToFront();

                        // Rather than click the age restriction button we'll set the cookie for this program 
                        if (HasAgeRestriction())
                        {
                            InvokeScript("function cookieObject() {this.allowedToWatch = 18};");
                            InvokeScript("C4.Util.setCookieValue('C4AccessControlCookie_" + _nextVideoToPlayName + "', new cookieObject())");
                        }
                      //  DoResize();
                        _currentState = State.Playing;
                        ProcessComplete.Finished = true;
                        ProcessComplete.Success = true;
                    }
                    break;
                case State.Playing:
                    // Handle the accepting of age restrictions, do this before making it fullscreen so we can get the absolute position of the box
                    /*var currPosLeft = Browser.Document.Window.Position.X + 125;
                    var currPosTop = Browser.Document.Window.Position.Y + 260;
                    var coloursToLookFor = new[] { "0F7FA8", "FFFFFF", "009ACA" };

                    if (HasAgeRestriction())
                    {
                        Cursor.Position = new System.Drawing.Point(currPosLeft, currPosTop);
                        Application.DoEvents();

                        // Wait for the warning to come up
                        while (!coloursToLookFor.Contains(CursorHelper.GetColourUnderCursor().Name.Substring(2).ToUpper()))
                        {
                            Application.DoEvents();
                            Thread.Sleep(10);
                        }

                        CursorHelper.DoLeftMouseClick();
                        Application.DoEvents();
                    }*/
                                             
                    DoResize();
                    // We'll keep coming into this case in case the page refreshes for some reason - re-attaching the screen re-size javascript won't do any damage
                    //_currentState = State.None;
                    ProcessComplete.Finished = true;
                    ProcessComplete.Success = true;
                    break;
            }

            return EventResult.Complete();
        }

        /// <summary>
        /// Attach the javascript to the page which will resize the player
        /// </summary>
        private void DoResize()
        {
            // The js code to Increase the size
            //var jsCodeToRun = "document.getElementById('resizeLarge').click();";
            var width = Browser.Width;
            var height = Browser.Height;
            
            var jsCodeToRun = "C4.PopoutPlayer.View.getView().resizePlayer(" + width.ToString() + "," + height.ToString() + ");";
            jsCodeToRun += "document.getElementById('c4nc').style.overflow='hidden';";
            InvokeScript(jsCodeToRun);
            // The js code to wait for the resize to become enabled 
            var jsCode = "setTimeout('doResize()', 1000);";
            jsCode += "function doResize() { ";
            jsCode += "if(document.getElementById('resizeLarge').getAttribute('src').indexOf('pop_large.gif') > -1) {";
            jsCode += jsCodeToRun;
            jsCode += "}";
            jsCode += "else setTimeout('doResize()', 1000);";
            jsCode += "}";
            InvokeScript(jsCode);
        }

        /// <summary>
        /// Play button has been pressed
        /// </summary>
        /// <returns></returns>
        public override EventResult Play()
        {
            // Make sure we've paused before playing
            if (!_lastButtonPause) return EventResult.Complete(); 
            _lastButtonPause = false;
            InvokeScript("C4.Video.Controller.unstall()");
            return EventResult.Complete();
        }

        /// <summary>
        /// Pause button has been pressed 
        /// </summary>
        /// <returns></returns>
        public override EventResult Pause()
        {
            // Try and prevent multiple stall requests as it'll kill the player
            if (_lastButtonPause) return EventResult.Complete();
            _lastButtonPause = true;

            InvokeScript("C4.Video.Controller.stall()");
            return EventResult.Complete();
        }

        /// <summary>
        /// See if this programme has an age restriction applied
        /// </summary>
        /// <returns></returns>
        private bool HasAgeRestriction()
        {
            var document = new XmlDocument();
            try
            {
                document.Load(Properties.Resources._4OD_VideoDetailsUrl(_nextVideoToPlayId));
                var node = document.GetElementsByTagName("rating");
                if (node != null && node.Count > 0)
                {
                    var result = 0;
                    if (int.TryParse(node[0].InnerText, out result))
                        return result >= 16;
                }

                return false;
            }
            catch 
            {
                // Ignore errors here - we'll assume it's not a restricted video
            }
            return false;
        }
    }
}

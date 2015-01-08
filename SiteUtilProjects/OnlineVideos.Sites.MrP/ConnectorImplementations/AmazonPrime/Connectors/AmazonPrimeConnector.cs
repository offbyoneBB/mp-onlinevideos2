using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites.Entities;
using System.Windows.Forms;
using OnlineVideos.Helpers;
using System.Drawing;
using OnlineVideos.Sites.WebAutomation.Extensions;

namespace OnlineVideos.Sites.WebAutomation.ConnectorImplementations.AmazonPrime.Connectors
{
    /// <summary>
    /// Connector for playing amazon prime
    /// </summary>
    public class AmazonPrimeConnector : BrowserUtilConnectorBase
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
            PlayPage1
        }

        private State _currentState = State.None;
        private string _username;
        private string _password;
        private bool _isPlayOrPausing;
        private int _playPausePos = -1;
        private int _playPauseHeight = -1;
        private Panel _blankPanel = new Panel();

        /// <summary>
        /// Do the login
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
            Url = Properties.Resources.AmazonLoginUrl;
            return EventResult.Complete();
        }

        /// <summary>
        /// Play the specified video - try and keep the loading screen showing for as long as possible
        /// </summary>
        /// <param name="videoToPlay"></param>
        /// <returns></returns>
        public override Sites.Entities.EventResult PlayVideo(string videoToPlay)
        {
            ShowLoading();
            Browser.ScrollBarsEnabled = false;
            _currentState = State.PlayPage;
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            Url = Properties.Resources.AmazonMovieUrl(videoToPlay);
            return EventResult.Complete();
        }

        /// <summary>
        /// Play button pressed
        /// </summary>
        /// <returns></returns>
        public override Sites.Entities.EventResult Play()
        {
            DoPlayOrPause();
            return EventResult.Complete();
        }

        /// <summary>
        /// Pause button pressed
        /// </summary>
        /// <returns></returns>
        public override Sites.Entities.EventResult Pause()
        {
            DoPlayOrPause();
            return EventResult.Complete();
        }

        /// <summary>
        /// Document loaded - see what state we're in and react accordingly
        /// </summary>
        /// <returns></returns>
        public override Sites.Entities.EventResult BrowserDocumentComplete()
        {
            switch (_currentState)
            {
                case State.LoggingIn:
                    if (Url.EndsWith("nav_signin_btn"))
                    {
                        var jsCode = "document.getElementById('ap_email').value = '" + _username + "';";
                        jsCode += "document.getElementById('ap_signin_existing_radio').checked='checked';";
                        jsCode += "setElementAvailability('ap_password', true);";
                        jsCode += "document.getElementById('ap_password').value = '" + _password + "';";
                        jsCode += "document.getElementById('ap_signin_form').submit();";
                        InvokeScript(jsCode);
                        _currentState = State.LoginResult;
                    }
                    break;
                case State.LoginResult:
                    if (Url.Contains("yourstore/home"))
                    {
                        _currentState = State.None;
                        ProcessComplete.Finished = true;
                        ProcessComplete.Success = true;
                    }
                    else
                        return EventResult.Error("AmazonPrimeConnector/BrowserDocumentComplete/Expected home page after log in, was actually " + Url);
                    break;
                case State.PlayPage:
                   
                    _currentState = State.PlayPage1;
                    break;
                case State.PlayPage1:
                    
                    InvokeScript("AMZNDetails.dvPlayer.play();");
                    InvokeScript("amzn.webGlobalVideoPlayer._mainPlayer._enableFullWindowPlaybackMode();");

                    // Hide the scroll bar - can't get the webpage to do this nicely :-(
                    _blankPanel.Height = Browser.Height;
                    _blankPanel.Width = 35;
                    _blankPanel.BackColor = Color.Black;
                    _blankPanel.Left = Browser.FindForm().Right - 35;

                    Browser.FindForm().Controls.Add(_blankPanel);
                    _blankPanel.BringToFront();

                    HideLoading();

                    ProcessComplete.Finished = true;
                    ProcessComplete.Success = true;
                    
                    break;
            }

            return EventResult.Complete();
        }


        /// <summary>
        /// Find the play/pause button and click it
        /// </summary>
        /// <returns></returns>
        private EventResult DoPlayOrPause()
        {
            if (_isPlayOrPausing || Browser.Document == null || Browser.Document.Body == null) return EventResult.Complete();

            _isPlayOrPausing = true;
           
            /*
             * if (_playPauseHeight == -1) _playPauseHeight = Browser.FindForm().Bottom - 20;
            var startX = Browser.FindForm().Left;

            // We've previously found the play/pause button, so re-use its position
            if (_playPausePos > -1)
            {
                Cursor.Position = new System.Drawing.Point(startX + 10, _playPauseHeight);

                // We have to move the cursor off the play button for this to work
                while (Cursor.Position.X < _playPausePos)
                {
                    Cursor.Position = new System.Drawing.Point(Cursor.Position.X + 2, _playPauseHeight);
                    Application.DoEvents();
                }

                Cursor.Position = new System.Drawing.Point(_playPausePos, _playPauseHeight);
                Application.DoEvents();
                CursorHelper.DoLeftMouseClick();
                Application.DoEvents();
                // Move the cursor off the controls so they disappear
                Cursor.Position = new System.Drawing.Point(10, 10);
            }
            else
            {
                _playPausePos = FindPlayPauseButton(_playPauseHeight);
                var attempts = 0;
                // Move up the screen in 10 pixel increments trying to find play - only go up 5 times
                while (attempts <= 4)
                {
                    if (_playPausePos == -1 && _isPlayOrPausing)
                    {
                        _playPauseHeight -= 10;
                        _playPausePos = FindPlayPauseButton(_playPauseHeight);
                    }
                    else
                        break;
                    attempts++;
                }
                _isPlayOrPausing = false;
                if (_playPausePos > -1) DoPlayOrPause();
            }
            */
            Cursor.Position = new System.Drawing.Point(300, 300);
            Application.DoEvents();
            CursorHelper.DoLeftMouseClick();
            Application.DoEvents();
            System.Windows.Forms.SendKeys.Send(" ");
            _isPlayOrPausing = false;
            return EventResult.Complete();
        }

        /// <summary>
        /// Move the cursor to try and find to position of the play/pause button
        /// </summary>
        /// <param name="height"></param>
        /// <returns></returns>
        private int FindPlayPauseButton(int height)
        {
            var startX = Browser.FindForm().Left;
            var coloursToLookFor = new[] { "FAFAFA", "F8F8F8", "747576", "6D6E6F", "787879", "5E5E5F", "68696A","505051","59595A", "575859" };

            // Very primitive, but set the cursor at the correct height and move across till we hit the right colour!
            // We have to move the cursor otherwise the play controls disappear
            var currentPos = startX + 10;
            while (currentPos < (startX + (Browser.Document.Body.ClientRectangle.Width / 10)))
            {
                Cursor.Position = new System.Drawing.Point(currentPos + 2, height);
                currentPos = Cursor.Position.X;
                Application.DoEvents();
                var x = CursorHelper.GetColourUnderCursor().Name.Substring(2).ToUpper();
                if (coloursToLookFor.Contains(x))
                    return Cursor.Position.X;
                Application.DoEvents();
                if (!_isPlayOrPausing) break;
            }
            return -1;
        }

        public override void OnAction(string actionEnumName)
        {
            if (_currentState == State.PlayPage1 && !_isPlayOrPausing)
            {
                if (actionEnumName == "ACTION_MOVE_LEFT")
                {
                    Cursor.Position = new System.Drawing.Point(300, 300);
                    Application.DoEvents();
                    CursorHelper.DoLeftMouseClick();
                    Application.DoEvents();
                    System.Windows.Forms.SendKeys.Send("{LEFT}");
                }
                if (actionEnumName == "ACTION_MOVE_RIGHT")
                {
                    Cursor.Position = new System.Drawing.Point(300, 300);
                    Application.DoEvents();
                    CursorHelper.DoLeftMouseClick();
                    Application.DoEvents();
                    System.Windows.Forms.SendKeys.Send("{RIGHT}");
                }
            }
        }
    }
}

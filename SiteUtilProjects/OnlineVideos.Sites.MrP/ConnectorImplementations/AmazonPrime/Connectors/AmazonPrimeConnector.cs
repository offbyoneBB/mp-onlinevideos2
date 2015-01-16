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

                    // Browser.FindForm().Controls.Add(_blankPanel);
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

            SendKeyToControl(" ");

            _isPlayOrPausing = false;
            return EventResult.Complete();
        }

        public override void OnAction(string actionEnumName)
        {
            if (_currentState == State.PlayPage1 && !_isPlayOrPausing)
            {
                if (actionEnumName == "ACTION_MOVE_LEFT")
                    SendKeyToControl("{LEFT}");
                if (actionEnumName == "ACTION_MOVE_RIGHT")
                    SendKeyToControl("{RIGHT}");
            }
        }

        /// <summary>
        /// With the Amazon player it seems that setting it to full screen (_enableFullWindowPlaybackMode) it makes the Silverlight control always take focus
        /// This means that when space bar is pressed to pause it fires in the Silverlight control before the browser host causing a double press
        /// To get around this I've added a dummy control to the page which will take focus after every action to prevent Silverlight getting the event
        /// </summary>
        /// <param name="keyStrokeToSend"></param>
        private void SendKeyToControl(string keyStrokeToSend)
        {
            if (Browser.Document.GetElementById("dummyFocusControl") == null)
            {
                //InvokeScript("$('#player_object').attr('height','99%');");
                var newCtl = "$('<input  type=\"text\" id=\"dummyFocusControl\" style=\"width: 1px; height: 1%;opacity:0;color: transparent;\"/>')";
                InvokeScript("$('#player_container').append(" + newCtl + ");");
            }

            Cursor.Position = new System.Drawing.Point(Browser.FindForm().Left + 50, Browser.FindForm().Top + 50);
            Application.DoEvents();
            CursorHelper.DoLeftMouseClick();
            Application.DoEvents();
            System.Windows.Forms.SendKeys.Send(keyStrokeToSend);
            Application.DoEvents();

            InvokeScript("$('#dummyFocusControl').focus()");
        }
    }
}

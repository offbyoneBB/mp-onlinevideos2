using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using OnlineVideos.Sites.Entities;
using System.Windows.Forms;
using OnlineVideos.Helpers;

namespace OnlineVideos.Sites.JSurf.ConnectorImplementations.AmazonPrime.Connectors
{
    /// <summary>
    /// Connector for playing amazon prime
    /// </summary>
    public class AmazonPrimeConnector : BrowserUtilConnectorBase
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetCursorPos(int x, int y);

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

        // Keys (or Actions) which are directly forwarded to browser
        private string[] _passThroughKeys = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        /// <summary>
        /// Do the login
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        protected override EventResult PerformActualLogin(string username, string password)
        {
            SetTopMostActivate();
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
        public override EventResult PlayVideo(string videoToPlay)
        {
            ShowLoading();
            // Move the curor to a corner, otherwise it is centered at screen and appears from time to time :(
            SetCursorPos(5000, 5000);
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
        public override EventResult Play()
        {
            DoPlayOrPause();
            return EventResult.Complete();
        }

        /// <summary>
        /// Pause button pressed
        /// </summary>
        /// <returns></returns>
        public override EventResult Pause()
        {
            DoPlayOrPause();
            return EventResult.Complete();
        }

        /// <summary>
        /// Document loaded - see what state we're in and react accordingly
        /// </summary>
        /// <returns></returns>
        public override EventResult BrowserDocumentComplete()
        {
            switch (_currentState)
            {
                case State.LoggingIn:
                    if (Url.EndsWith("nav_signin_btn"))
                    {
                        var jsCode = @"var u=document.getElementById('ap_email')||document.getElementById('ap-claim-autofill-hint');
                                    var r=document.getElementById('ap_signin_existing_radio')||document.getElementById('rememberMe');
                                    var p=document.getElementById('ap_password');
                                    var fm=document.getElementById('ap_signin_form')||document.forms['signIn'];
                                    if (u)u.value='" + _username + @"';
                                    if (p)p.value='" + _password + @"';
                                    if (r)r.checked='checked';
                                    if (fm)fm.submit();";
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
                case State.PlayPage1:

                    // Retry play every 1 second(s)
                    /*                    var jsPlay = "var mpOVPlay = function() { ";
                                        jsPlay += "   try {";
                                        jsPlay += "         AMZNDetails.dvPlayer.play();";
                                        jsPlay += "         amzn.webGlobalVideoPlayer._mainPlayer._enableFullWindowPlaybackMode();";
                                        jsPlay += "   } catch(err) {";
                                        jsPlay += "      setTimeout(mpOVPlay,1000);";
                                        jsPlay += "   }";
                                        jsPlay += "};";
                                        jsPlay += "mpOVPlay();";

                                        InvokeScript(jsPlay);*/

                    // Hide the scroll bar - can't get the webpage to do this nicely :-(
                    /*                    _blankPanel.Height = Browser.Height;
                                        _blankPanel.Width = 35;
                                        _blankPanel.BackColor = Color.Black;
                                        _blankPanel.Left = Browser.FindForm().Right - 35;

                                        // Browser.FindForm().Controls.Add(_blankPanel);
                                        _blankPanel.BringToFront();*/

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
                if (_passThroughKeys.Contains(actionEnumName))
                    SendKeyToControl(actionEnumName);
                if (actionEnumName == Constants.ACTION_MOVE_LEFT)
                    SendKeyToControl("{LEFT}");
                if (actionEnumName == Constants.ACTION_MOVE_RIGHT)
                    SendKeyToControl("{RIGHT}");
                // Jump to beginning of clip
                if (actionEnumName == Constants.ACTION_PREV_ITEM)
                    InvokeScript("amzn.webGlobalVideoPlayer._mainPlayer.seek(0)");
                // Jump to next episode, more complicated than it could be, because jquery "click()" does not seem work
                if (actionEnumName == Constants.ACTION_NEXT_ITEM)
                    InvokeScript("$('.episode-list .selected-episode').next().find('a.episode-list-link').each(function() { location.href = $(this).attr('href'); });");
            }
        }

        protected bool IsHtml5Player
        {
            get
            {
                return Process.GetCurrentProcess().ProcessName.Contains("iexplore");
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
            var isHtml5 = IsHtml5Player;
            if (!isHtml5)
            {
                if (Browser.Document.GetElementById("dummyFocusControl") == null)
                {
                    //InvokeScript("$('#player_object').attr('height','99%');");
                    var newCtl = "$('<input  type=\"text\" id=\"dummyFocusControl\" style=\"width: 1px; height: 1%;opacity:0;color: transparent;\"/>')";
                    InvokeScript("$('#player_container').append(" + newCtl + ");");
                }
                var form = Browser.FindForm();
                Cursor.Position = new System.Drawing.Point(form.Left + 50, form.Top + 50);
                Application.DoEvents();
                CursorHelper.DoLeftMouseClick();
                Application.DoEvents();
                SendKeys.Send(keyStrokeToSend);
                Application.DoEvents();
            }
            else
            {
                SetTopMostActivate();
                SendKeys.SendWait(keyStrokeToSend);
            }

            if (!isHtml5)
                InvokeScript("$('#dummyFocusControl').focus()");
        }

        private void SetTopMostActivate()
        {
            var form = Browser.FindForm();
            if (form == null)
                return;
            form.TopMost = true;
            form.BringToFront();
            form.Activate();
        }
    }
}

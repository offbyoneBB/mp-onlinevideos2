using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites.Entities;
using OnlineVideos.Sites.Base;
using System.Windows.Forms;
using OnlineVideos.Helpers;
using System.Threading;

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
            VideoInfo,
            PlayPage,
            PlayPageLiveTv
        }

        private State _currentState = State.None;
        private string _username;
        private string _password;
        private string _nextVideoToPlayId;
        private bool _isPlayOrPausing;
        private int _playPausePos = -1;
        private int _playPauseHeight = -1;
        private bool _isLiveTv = false;
        private bool _isSilverlightAppStorageEnabled = true;
        private Thread  _disableAppStorageThread;

        /// <summary>
        /// Perform a log in to the sky go site
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        protected override EventResult PerformActualLogin(string username, string password)
        {
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
                    else
                    {
                        // Already logged in
                        if (Url.Contains("/home.do"))
                        {
                            _currentState = State.None;
                            ProcessComplete.Finished = true;
                            ProcessComplete.Success = true; 
                        }
                    }
                    break;
                case State.LoginResult:
                    if (Url.Contains("/home.do"))
                    {
                        _currentState = State.None;
                        ProcessComplete.Finished = true;
                        ProcessComplete.Success = true;
                    }
                    else
                        return EventResult.Error("SkyGoGeneralConnector/ProcessMessage/Expected home page after log in, was actually " + Url);
                    break;
                case State.VideoInfo:
                    if (Url.Contains("videoActions.do") && Url.EndsWith("aaxmlrequest=true&aazones=vdactions"))
                    {
                        // Need to lookup the asset id before we can continue
                        var assetId = GetAssetId(Browser.Document);
                        if (assetId != string.Empty)
                        {
                            Browser.Stop();
                            _currentState = State.PlayPage;
                            Url = Properties.Resources.SkyGo_VideoPlayUrl.Replace("{ASSET_ID}", assetId).Replace("{VIDEO_ID}", _nextVideoToPlayId);
                        }
                    }
                    break;
                case State.PlayPage:
                    if (Url.Contains("/progressivePlay.do"))
                    {
                        Browser.Refresh(WebBrowserRefreshOption.Completely);// Need to do this for some reason
                        _currentState = State.None;
                        HideLoading();
                        ProcessComplete.Finished = true;
                        ProcessComplete.Success = true;
                        // Wait 5 seconds for the video to start before disabling app storage - we'll do this in a separate thread
                        if (_disableAppStorageThread == null)
                        {
                            _disableAppStorageThread = new Thread(new ParameterizedThreadStart(DisableAppStorage));
                            _disableAppStorageThread.Start();
                        }
                    }
                    else
                    {
                        if (!Url.EndsWith("videoDetailsPage.do"))
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
                        
                        Cursor.Position = new System.Drawing.Point(50, 50);
                        Application.DoEvents();
                        CursorHelper.DoLeftMouseClick();
                        Application.DoEvents();
                        HideLoading();
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
            _currentState = State.VideoInfo;
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            if (videoToPlay.StartsWith("LTV~")) _isLiveTv = true;
            _nextVideoToPlayId = videoToPlay.Replace("LTV~", string.Empty);

            if (!_isLiveTv)
                Url = Properties.Resources.SkyGo_VideoActionsUrl.Replace("{VIDEO_ID}", _nextVideoToPlayId);
            else
            {
                _currentState = State.PlayPageLiveTv;
                Url = Properties.Resources.SkyGo_LiveTvPlayUrl.Replace("{VIDEO_ID}", _nextVideoToPlayId);
            }

            return EventResult.Complete();
        }

        /// <summary>
        /// Resume a paused video
        /// </summary>
        /// <returns></returns>
        public override EventResult Play()
        {
            return DoPlayOrPause();
        }

        /// <summary>
        /// Pause the video
        /// </summary>
        /// <returns></returns>
        public override EventResult Pause()
        {
            return DoPlayOrPause();
        }

        /// <summary>
        /// Load the asset from the document
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public static string GetAssetId(HtmlDocument document)
        {
            if (document.ActiveElement == null) return string.Empty;
            var stringToParse = document.ActiveElement.OuterHtml;
            // Need to get the asset id from the video details links
            var startPos = stringToParse.IndexOf("assetId: ");
            // Load the asset id
            if (startPos > -1)
            {
                var endPos = stringToParse.IndexOf(",", startPos);
                if (endPos > -1)
                {
                    return stringToParse.Substring(startPos + 8, endPos - startPos - 8).Replace("'", "").Trim() + "____";
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Find the play/pause button and click it
        /// </summary>
        /// <returns></returns>
        private EventResult DoPlayOrPause()
        {
            if (_isPlayOrPausing || Browser.Document == null || Browser.Document.Body == null) return EventResult.Complete();

            _isPlayOrPausing = true;
            if (_playPauseHeight <= 0) _playPauseHeight = Browser.FindForm().Bottom - 80;
         
            // We've previously found the play/pause button, so re-use its position
            if (_playPausePos > -1)
            {
                Cursor.Position = new System.Drawing.Point(Browser.FindForm().Left + 10, _playPauseHeight);

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
            }
            else
            {
                _playPausePos = FindPlayPauseButton(_playPauseHeight);
                var attempts = 0;
                // Move up the screen in 10 pixel increments trying to find play - only go up 20 times
                while (attempts <= 20)
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
            }

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
            var coloursToLookFor = new[] { "0090BF", "D8DDE1", "0099CB", "009BCE", "007297", "00789F", "EFEDEA", "0086B1", "00789E", "0083AD" };

            // Very primitive, but set the cursor at the correct height and move across till we hit the right colour!
            // We have to move the cursor otherwise the play controls disappear
            var currentPos = startX + 40;
            while (currentPos < (startX + (Browser.Document.Body.ClientRectangle.Width / 8)))
            {
                Cursor.Position = new System.Drawing.Point(currentPos + 5, height);
                currentPos = Cursor.Position.X;
                Application.DoEvents();
                if (coloursToLookFor.Contains(CursorHelper.GetColourUnderCursor().Name.Substring(2).ToUpper()))
                    return Cursor.Position.X;
                Application.DoEvents();
                if (!_isPlayOrPausing) break;
            }
            return -1;
        }

        /// <summary>
        /// We'll disable the app storage a few seconds after starting the video (we'll do this on a separate thread so the browser continues)
        /// </summary>
        /// <param name="data"></param>
        private void DisableAppStorage(object data)
        {
            Thread.Sleep(20000);
            WebBrowserHelper.ToogleSilverlightAppStorage(false);
        }
    }
}

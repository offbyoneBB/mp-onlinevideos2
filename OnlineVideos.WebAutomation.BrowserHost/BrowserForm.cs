
using System;
using System.Windows.Forms;
using OnlineVideos.Helpers;
using OnlineVideos.Sites.WebAutomation.BrowserHost.Factories;
using System.IO;
using System.Diagnostics;
using System.Configuration;
using System.Drawing;
using System.Threading;
using OnlineVideos.Sites.WebBrowserPlayerService.ServiceImplementation;
using OnlineVideos.Sites.WebAutomation.BrowserHost.Helpers;
using OnlineVideos.Sites.WebAutomation.BrowserHost.RemoteHandling;

namespace OnlineVideos.Sites.WebAutomation.BrowserHost
{
    public partial class BrowserForm : Form
    {
        /// <summary>
        /// Which was the last of the 2 events fired between play/pause.  We'll use this to handle the media key as that's a single play/pause button.
        /// </summary>
        private enum PlayPauseToggle
        {
            Play,
            Pause
        }

        /// <summary>
        /// Defines different window modes the browser form can enter.
        /// </summary>
        public enum ScreenMode
        {
            /// <summary>
            /// Fullscreeen mode, no borders, TopMost.
            /// </summary>
            Fullscreen,
            /// <summary>
            /// Windowed mode, no borders, TopMost.
            /// </summary>
            Windowed,
            /// <summary>
            /// Debug mode only, will use resizable window, without TopMost.
            /// </summary>
            Debug
        }

        public bool ForceClose { get; private set; }

        private readonly string _connectorType;
        private readonly string _videoInfo;
        private readonly string _userName;
        private readonly string _password;
        private BrowserUtilConnector _connector;
        private readonly bool _debugMode = false; // Allow for the form to be resized/lose focus in debug mode

        private int _lastKeyPressed;
        private DateTime _lastKeyPressedTime;

        private PlayPauseToggle _lastPlayPauseState = PlayPauseToggle.Play;
        private DateTime _lastActionTime;
        private static readonly ILog _logger = new DebugLogger();
        private readonly RemoteProcessing _remoteProcessing = new RemoteProcessing(_logger);
        private readonly int _connectorTimeout = 20;

        /// <summary>
        /// Store/retrieve the current screen the web player is showing on - this is stored in the user config
        /// </summary>
        private int CurrentScreen
        {
            get
            {
                return Properties.Settings.Default.CurrentScreenId;
            }
            set
            {

                Properties.Settings.Default.CurrentScreenId = value;
                Properties.Settings.Default.Save();
            }
        }

        /// <summary>
        /// This form is used to play a video - we use separate exe as there is no reliable way to dispose of the web browser between sessions
        /// and this seemed to cause issues when playing multiple videos
        /// </summary>
        /// <param name="connectorType"></param>
        /// <param name="videoInfo"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        public BrowserForm(string connectorType, string videoInfo, string userName, string password)
        {
            try
            {
                _logger.Debug("Loading browser form");
                InitializeComponent();
                _connectorType = connectorType;
                _videoInfo = videoInfo;
                _userName = userName;
                _password = password;
                _debugMode = false;

                bool.TryParse(ConfigurationManager.AppSettings["DebugMode"], out _debugMode);

                int tmpVal;
                if (Int32.TryParse(ConfigurationManager.AppSettings["BrowserHostWaitTimeout"], out tmpVal))
                    _connectorTimeout = tmpVal;

            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        /// <summary>
        /// The form is loaded - let's navigate to the video
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BrowserForm_Load(object sender, EventArgs e)
        {
            try
            {

                _logger.Debug("Setting current screen");
                SetScreenState(_debugMode ? ScreenMode.Debug : ScreenMode.Fullscreen);
                SetCurrentScreen();

                ForceClose = false;
                Activate();
                Focus();
                _logger.Debug("AppDomain Root {0}", AppDomain.CurrentDomain.BaseDirectory);
                _logger.Debug("Current Directory {0}", Directory.GetCurrentDirectory());
                _logger.Debug(string.Format("Browser Host started with connector type: {0}, video info: {1}", _connectorType, _videoInfo));
                WebBrowserPlayerCallbackService.LogInfo(string.Format("Browser Host started with connector type: {0}, video info: {1}", _connectorType, _videoInfo));

                // Set up remote handling
                _remoteProcessing.ActionReceived += RemoteProcessing_OnNewAction;
                _remoteProcessing.InitHandlers();

                _logger.Debug("Loading Connector");
                _connector = BrowserInstanceConnectorFactory.GetConnector(_connectorType, _logger, webBrowser);

                if (_connector == null)
                {
                    _logger.Error(string.Format("Unable to load connector type {0}, not found in any site utils", _connectorType));
                    throw new ApplicationException(string.Format("Unable to load connector type {0}, not found in any site utils", _connectorType));
                }

                _connector.DebugMode = _debugMode;
                _logger.Debug("Performing Log in");
                _connector.PerformLogin(_userName, _password);

                var result = _connector.WaitForComplete(ForceQuitting, _connectorTimeout);

                if (result)
                {
                    _logger.Debug("Playing Video");
                    _connector.PlayVideo(_videoInfo);
                    result = _connector.WaitForComplete(ForceQuitting, _connectorTimeout);
                    _logger.Debug("Playing WaitforComplete " + result);
                    if (!result)
                        ForceQuit();
                }
                else
                {
                    _logger.Error("Log in failed");
                    ForceQuit();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                Console.Error.WriteLine("{0}\r\n{1}", ex.Message, ex.StackTrace);
                Console.Error.Flush();
                WebBrowserPlayerCallbackService.LogError(ex);
                ForceQuit();
            }
        }

        /// <summary>
        /// Method to pass to the connector to see if the form is force closing
        /// </summary>
        /// <returns></returns>
        public bool ForceQuitting()
        {
            return ForceClose;
        }

        /// <summary>
        /// Handle the escape key click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BrowserForm_KeyDown(object sender, KeyEventArgs e)
        {
            HandleKeyPress(e.KeyValue);
            e.Handled = true;
        }

        /// <summary>
        /// Handle the escape key click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void webBrowser_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            HandleKeyPress(e.KeyValue);
        }

        /// <summary>
        /// Used to pass messages to remotes. Pre-filter to only messages we're likely to be interested in
        /// </summary>
        /// <param name="msg"></param>
        protected override void WndProc(ref Message msg)
        {
            try
            {
                if (_remoteProcessing.ProcessWndProc(msg)) return;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                Console.Error.WriteLine("{0}\r\n{1}", ex.Message, ex.StackTrace);
                Console.Error.Flush();
                WebBrowserPlayerCallbackService.LogError(ex);
            }
            base.WndProc(ref msg);
        }

        /// <summary>
        /// Event sink for key press
        /// </summary>
        /// <param name="keyPressed"></param>
        private void HandleKeyPress(int keyPressed)
        {
            // Ignore duplicate presses within 300ms (happens because sometimes both the browser and form fire the event)
            if (_lastKeyPressed == keyPressed && _lastKeyPressedTime.AddMilliseconds(300) > DateTime.Now) return;
            _lastKeyPressed = keyPressed;
            _lastKeyPressedTime = DateTime.Now;

            _logger.Debug(string.Format("HandleKeyPress to be processed {0} {1}", keyPressed, (Keys)keyPressed));

            // Always force close when escape is pressed
            if (keyPressed == (int)Keys.Escape)
            {
                ForceQuit();
                return;
            }

            _remoteProcessing.ProcessKeyPress(keyPressed);
        }

        /// <summary>
        /// Remote processing event handler
        /// </summary>
        /// <param name="action"></param>
        void RemoteProcessing_OnNewAction(string action)
        {
            if (InvokeRequired)
                BeginInvoke((MethodInvoker)delegate { OnNewAction(action); });
            else
                OnNewAction(action);
        }

        /// <summary>
        /// Handle actions
        /// We'll make play/pause a toggle, just so we can ensure we support media buttons properly
        /// </summary>
        /// <param name="action"></param>
        /// <param name="overrideCheck"></param>
        void OnNewAction(string action, bool overrideCheck = false)
        {
            // Ignore duplicate actions within 300ms (apparently the Netflix connector has duplicate actions, I suspect it's because the Netflix connector is sending space key and this is doing the play/pause and firing an action :-( )
            if (!overrideCheck && _lastActionTime.AddMilliseconds(300) > DateTime.Now)
                return;

            _lastActionTime = DateTime.Now;
            _logger.Debug(string.Format("OnNewAction received {0}", action));

            // Special case, command contains size arguments
            if (action.StartsWith(Constants.ACTION_WINDOWED))
            {
                SetScreenState(ScreenMode.Windowed);
                var sizeArgs = action.Replace(Constants.ACTION_WINDOWED, "").Split(',');
                if (sizeArgs.Length == 4)
                {
                    int left;
                    int top;
                    int width;
                    int height;
                    if (int.TryParse(sizeArgs[0], out left) &&
                        int.TryParse(sizeArgs[1], out top) &&
                        int.TryParse(sizeArgs[2], out width) &&
                        int.TryParse(sizeArgs[3], out height))
                    {
                        Size = new Size(width, height);
                        Location = new Point(left, top);
                        _logger.Debug("ACTION_WINDOWED: Position: {0}, Size: {1}", Location, Size);
                    }
                }
                return;
            }
            switch (action)
            {
                case Constants.ACTION_PLAY:
                case Constants.ACTION_MUSIC_PLAY:
                    if (_lastPlayPauseState == PlayPauseToggle.Play)
                        OnNewAction(Constants.ACTION_PAUSE, true);
                    else
                    {
                        _connector.Play();
                        _lastPlayPauseState = PlayPauseToggle.Play;
                    }
                    break;
                case Constants.ACTION_PAUSE:
                    if (_lastPlayPauseState == PlayPauseToggle.Pause)
                        OnNewAction(Constants.ACTION_PLAY, true);
                    else
                    {
                        _connector.Pause();
                        _lastPlayPauseState = PlayPauseToggle.Pause;
                    }
                    break;
                case Constants.ACTION_STOP:
                case Constants.ACTION_PREVIOUS_MENU:
                    ForceQuit();
                    break;
                case Constants.ACTION_CONTEXT_MENU: // Change the screen we're on using the context menu button
                    CurrentScreen++;
                    SetCurrentScreen();
                    break;
                case Constants.ACTION_FULLSCREEN:
                    SetScreenState(ScreenMode.Fullscreen);
                    break;
                default:
                    // fire the action on the connector also
                    _connector.OnAction(action);
                    break;
            }
        }

        /// <summary>
        /// Close the application
        /// </summary>
        private void ForceQuit()
        {
            try
            {
                WebBrowserPlayerCallbackService.OnBrowserClosing(); // Let MePo know we're closing
                if (_connector != null) _connector.OnClosing();
                ForceClose = true;
            }
            catch (Exception ex)
            {
                _logger.Error(string.Format("{0}\r\n{1}", ex.Message, ex.StackTrace));
            }
            finally
            {
                Thread.Sleep(1000); // Wait 1 second for MePo to show
                Process.GetCurrentProcess().Kill(); // In case we've got some weird browser issues or something hogging the process
            }
        }

        /// <summary>
        /// Attempt to keep the current application on top
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tmrKeepOnTop_Tick(object sender, EventArgs e)
        {
            ProcessHelper.SetForeground(Process.GetCurrentProcess().MainWindowHandle);
            Activate();
            Focus();
        }

        /// <summary>
        /// Set the screen state depending on which mode it's in
        /// </summary>
        private void SetScreenState(ScreenMode screenMode)
        {
            switch (screenMode)
            {
                case ScreenMode.Debug:
                    WindowState = FormWindowState.Normal;
                    FormBorderStyle = FormBorderStyle.Sizable;
                    tmrKeepOnTop.Enabled = false;
                    break;
                case ScreenMode.Fullscreen:
                    WindowState = FormWindowState.Maximized;
                    FormBorderStyle = FormBorderStyle.None;
                    tmrKeepOnTop.Enabled = true;
                    break;
                case ScreenMode.Windowed:
                    WindowState = FormWindowState.Normal;
                    FormBorderStyle = FormBorderStyle.None;
                    tmrKeepOnTop.Enabled = true;
                    break;
            }
            ControlBox = screenMode == ScreenMode.Debug;
        }

        /// <summary>
        /// Set the screen which the form is on based on the CurrentScreen property
        /// </summary>
        private void SetCurrentScreen()
        {
            if (CurrentScreen >= Screen.AllScreens.Length)
                CurrentScreen = 0;

            if (Screen.AllScreens.Length > 1)
            {
                if (!_debugMode) WindowState = FormWindowState.Normal;
                Location = Screen.AllScreens[CurrentScreen].Bounds.Location;
                if (!_debugMode) WindowState = FormWindowState.Maximized;
            }
        }

    }
}

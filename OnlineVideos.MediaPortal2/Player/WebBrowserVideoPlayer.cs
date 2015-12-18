using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Threading;
using System.Windows.Forms;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.RawUrlResourceProvider;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.SkinEngine.InputManagement;
using OnlineVideos.Helpers;
using OnlineVideos.Sites;
using OnlineVideos.Sites.Proxy.WebBrowserPlayerService;
using OnlineVideos.Sites.WebBrowserPlayerService.ServiceImplementation;

namespace OnlineVideos.MediaPortal2
{
    /// <summary>
    /// Player which automates a web browser - will minimise MediaPortal and shell to the WebBrowserHost when play is requested
    /// </summary>
    public class WebBrowserVideoPlayer : IPlayer, IPlayerEvents
    {
        public const string ONLINEVIDEOSBROWSER_MIMETYPE = "video/onlinebrowser";

        private IntPtr _mpWindowHandle = IntPtr.Zero;
        private bool _mpWindowHidden;
        private Process _browserProcess;
        private string _automationType;
        private string _username;
        private string _password;
        private string _lastError;
        private WebBrowserPlayerCallbackServiceProxy _callbackServiceProxy;
        private readonly WebBrowserPlayerCallback _callback = new WebBrowserPlayerCallback();
        private WebBrowserPlayerServiceProxy _serviceProxy;
        private string _fileOrUrl;

        // Player event delegates
        protected PlayerEventDlgt _started = null;
        protected PlayerEventDlgt _stateReady = null;
        protected PlayerEventDlgt _stopped = null;
        protected PlayerEventDlgt _ended = null;
        protected PlayerEventDlgt _playbackStateChanged = null;
        protected PlayerEventDlgt _playbackError = null;

        public bool GoFullscreen { get; set; }
        public string SubtitleFile { get; set; }
        public string PlaybackUrl { get; set; }

        /// <summary>
        /// We require the command line parameters for the web browser host
        /// Util should be an implementation of IBrowserSiteUtil
        /// </summary>
        /// <param name="util"></param>
        public void Initialise(SiteUtilBase util)
        {
            var browserConfig = util as IBrowserSiteUtil;
            if (browserConfig != null)
            {
                _automationType = browserConfig.ConnectorEntityTypeName;
                _username = browserConfig.UserName;
                _password = browserConfig.Password;
            }
            _lastError = string.Empty;

            _callback.OnBrowserClosing += _callback_OnBrowserHostClosing;
            _callback.OnBrowserKeyPress += _callback_OnBrowserKeyPress;
            _callback.OnBrowserWndProc += _callback_OnBrowserWndProc;

            // Wire up to an existing browser process if one exists
            var processes = Process.GetProcessesByName("OnlineVideos.WebAutomation.BrowserHost");
            if (processes.Any())
                _browserProcess = processes[0];
        }

        /// <summary>
        /// Initialise the connection to the service
        /// </summary>
        private void ReinitialiseService()
        {
            if (_callbackServiceProxy != null) _callbackServiceProxy.Dispose();
            _callbackServiceProxy = new WebBrowserPlayerCallbackServiceProxy(_callback);
            if (_serviceProxy != null) _callbackServiceProxy.Dispose();
            _serviceProxy = new WebBrowserPlayerServiceProxy();
        }

        public bool Init(MediaItem mediaItem)
        {
            IResourceAccessor ra = null;
            try
            {
                ra = mediaItem.GetResourceLocator().CreateAccessor();
                RawUrlResourceAccessor rra = ra as RawUrlResourceAccessor;
                if (rra == null)
                    return false;
                _fileOrUrl = rra.URL;
                Play();
                return true;
            }
            catch (Exception)
            {
                // Log
            }
            finally
            {
                if (ra != null)
                    ra.Dispose();
            }
            return false;
        }

        /// <summary>
        /// Play the specified file - file will actually be the video id from the website 
        /// This method will hide MediaPortal and run the BrowserHost - BorwserHost needs to support the WebAutomationType and have the code to actually navigate to the site
        /// </summary>
        /// <returns></returns>
        public bool Play()
        {
            _lastError = string.Empty;

            if (_browserProcess != null)
            {
                //ReinitialiseService();
                ProcessHelper.SetForeground(_browserProcess.MainWindowHandle);
                return true;
            }


            // Set up the process
            // Process requires path to MediaPortal, Web Automation Type, Video Id, Username, Password
            _browserProcess = new Process();
            _browserProcess.StartInfo.UseShellExecute = false;
            _browserProcess.StartInfo.RedirectStandardError = true;
            _browserProcess.EnableRaisingEvents = true;
            //_browserProcess.StartInfo.FileName = "plugins\\Windows\\OnlineVideos\\OnlineVideos.Sites.WebAutomation.BrowserHost.exe";
            var dir = OnlineVideoSettings.Instance.DllsDir;

            _browserProcess.StartInfo.FileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "OnlineVideos.WebAutomation.BrowserHost.exe");
            _browserProcess.StartInfo.Arguments = string.Format("\"{0} \" \"{1}\" \"{2}\" \"{3}\" \"{4}\"",
                                            dir,
                                            _fileOrUrl,
                                            _automationType,
                                            string.IsNullOrEmpty(_username) ? "_" : _username,
                                            string.IsNullOrEmpty(_password) ? "_" : _password);
            _browserProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal; // ProcessWindowStyle.Maximized;

            // Restart MP or Restore MP Window if needed
            _browserProcess.Exited += BrowserProcess_Exited;

            // Hide MediaPortal
            if (_browserProcess.Start())
            {
                Thread.Sleep(2000); // Sleep for 2 seconds to allow the browser host to load - should prevent the desktop flashing up
                if (_browserProcess != null)
                {
                    ReinitialiseService();
                    SuspendMP(true);
                    ProcessHelper.SetForeground(_browserProcess.MainWindowHandle);
                    Redirect(_browserProcess.StandardError);
                }
                else
                {
                    Log.Error("Browser process closed on startup");
                    SuspendMP(false);
                }
            }

            return true;
        }

        private readonly Dictionary<Key, string> KEY_MAPPINGS = new Dictionary<Key, string>
        {
            { Key.Play, "ACTION_PLAY" },
            { Key.PlayPause, "ACTION_PLAY" },
            { Key.Pause, "ACTION_PAUSE" },
            { Key.Stop, "ACTION_STOP" },
            { Key.Info, "ACTION_CONTEXT_MENU" }
        };

        //TODO: no result yet
        /// <summary>
        /// When a new action is received we'll forward them to the browser host using a WCF service
        /// </summary>
        /// <param name="key"></param>
        private void InstanceOnKeyPressed(ref Key key)
        {
            // Forward the key on to the browser process 
            if (_browserProcess != null)
            {
                string action;
                if (!KEY_MAPPINGS.TryGetValue(key, out action))
                    return;

                try
                {
                    if (_serviceProxy == null) ReinitialiseService();
                    _serviceProxy.OnNewAction(action);
                    key = Key.None; // Handled
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    ReinitialiseService(); // Attempt to reinitialise the connection to the service
                    _serviceProxy.OnNewAction(action);
                }
            }
        }

        /// <summary>
        /// Read the standard streams using a separate thread
        /// </summary>
        /// <param name="input"></param>
        private void Redirect(StreamReader input)
        {
            new Thread(a =>
            {
                var buffer = new char[1];
                while (input.Read(buffer, 0, 1) > 0)
                {
                    _lastError += new string(buffer);
                }
            }).Start();
        }

        /// <summary>
        /// Restore the MP window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BrowserProcess_Exited(object sender, EventArgs e)
        {
            SuspendMP(false);
            _browserProcess = null;
            if (!string.IsNullOrEmpty(_lastError))
                Log.Error(_lastError);
            Ended();
        }

        /// <summary>
        /// Suspend mediaportal and hide the window
        /// </summary>
        /// <param name="suspend"></param>
        void SuspendMP(bool suspend)
        {
            if (suspend && _mpWindowHidden) return; // Should already be suspended
            if (!suspend && !_mpWindowHidden) return; // Should already be visible

            if (suspend) //suspend and hide MediaPortal
            {
                InputManager.Instance.KeyPressed += InstanceOnKeyPressed;

                // Minimise MePo to tray - this is preferrable 
                ToggleMinimise(true);

                _mpWindowHidden = true;
            }
            else //resume Mediaportal
            {
                InputManager.Instance.KeyPressed -= InstanceOnKeyPressed;

                // Resume Mediaportal rendering
                ToggleMinimise(false);

                ProcessHelper.SetForeground("mp2-client");

                _mpWindowHidden = false;
            }
        }

        /// <summary>
        /// We'll use reflection to call in to some core MediaPortal methods to minimise to the system tray
        /// This seems to work better that other methods I've found because they cause MediaPortal to hog a core when set to suspending  
        /// </summary>
        /// <param name="shouldMinimise"></param>
        private void ToggleMinimise(bool shouldMinimise)
        {
            if (shouldMinimise)
                ServiceRegistration.Get<IScreenControl>().Minimize();
            else
                ServiceRegistration.Get<IScreenControl>().Restore();
        }

        /// <summary>
        /// Clean up
        /// </summary>
        public void Dispose()
        {
            // Clean up the service proxy
            if (_serviceProxy != null)
            {
                if (_serviceProxy.State == CommunicationState.Opened) _serviceProxy.Close();
                _serviceProxy.Dispose();
            }
            // Clean up the callback service proxy
            if (_callbackServiceProxy != null)
            {
                if (_callbackServiceProxy.State == CommunicationState.Opened) _callbackServiceProxy.Close();
                _callbackServiceProxy.Dispose();
            }
            if (_mpWindowHidden)
                SuspendMP(false);
            KillBrowserProcess();
        }

        /// <summary>
        /// Kill the browser process if it's set to an instance
        /// </summary>
        private void KillBrowserProcess()
        {
            if (_browserProcess != null)
            {
                if (!_browserProcess.HasExited)
                    _browserProcess.Kill();
            }
            _browserProcess = null;
        }

        #region Callback methods from service
        /// <summary>
        /// Handles the event from the browser host telling us its closing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _callback_OnBrowserHostClosing(object sender, EventArgs e)
        {
            SuspendMP(false);
        }

        /// <summary>
        /// Handle wnd proc from the browser host to try and translate to an action
        /// </summary>
        /// <param name="msg"></param>
        bool _callback_OnBrowserWndProc(Message msg)
        {
            Action action;
            char key;
            Keys keyCode;

            //if (InputDevices.WndProc(ref msg, out action, out key, out keyCode))
            //{
            //    //If remote doesn't fire event directly we manually fire it
            //    if (action != null && action.wID != Action.ActionType.ACTION_INVALID)
            //    {
            //        GUIWindowManager_OnNewAction(action);
            //    }

            //    if (keyCode != Keys.A)
            //    {
            //        var ke = new KeyEventArgs(keyCode);
            //        _callback_OnBrowserKeyPress(ke.KeyValue);
            //    }
            //    return true;
            //}
            return false;
        }

        /// <summary>
        /// When the browser has a key press, try and map it to an action and fire the on action 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _callback_OnBrowserKeyPress(int keyPressed)
        {
            //var action = new Action();

            //if (ActionTranslator.GetAction(-1, new Key(0, keyPressed), ref action))
            //{
            //    GUIWindowManager_OnNewAction(action);
            //}
            //else
            //{
            //    //See if it's mapped to KeyPressed instead
            //    if (keyPressed >= (int)Keys.A && keyPressed <= (int)Keys.Z)
            //        keyPressed += 32; //convert to char code
            //    if (ActionTranslator.GetAction(-1, new Key(keyPressed, 0), ref action))
            //        GUIWindowManager_OnNewAction(action);
            //}
        }
        #endregion

        protected void Ended()
        {
            State = PlayerState.Ended;
            FireEnded();
        }

        public void Stop()
        {
            State = PlayerState.Ended;
            FireEnded();
        }

        public string Name { get; private set; }
        public PlayerState State { get; private set; }
        public string MediaItemTitle { get; private set; }

        #region Event handling

        protected void FireStarted()
        {
            if (_started != null)
                _started(this);
        }

        protected void FireStateReady()
        {
            if (_stateReady != null)
                _stateReady(this);
        }

        protected void FireStopped()
        {
            if (_stopped != null)
                _stopped(this);
        }

        protected void FireEnded()
        {
            if (_ended != null)
                _ended(this);
        }

        protected void FirePlaybackStateChanged()
        {
            if (_playbackStateChanged != null)
                _playbackStateChanged(this);
        }

        #endregion

        #region IPlayerEvents implementation

        public void InitializePlayerEvents(PlayerEventDlgt started, PlayerEventDlgt stateReady, PlayerEventDlgt stopped,
            PlayerEventDlgt ended, PlayerEventDlgt playbackStateChanged, PlayerEventDlgt playbackError)
        {
            _started = started;
            _stateReady = stateReady;
            _stopped = stopped;
            _ended = ended;
            _playbackStateChanged = playbackStateChanged;
            _playbackError = playbackError;
        }

        public void ResetPlayerEvents()
        {
            _started = null;
            _stateReady = null;
            _stopped = null;
            _ended = null;
            _playbackStateChanged = null;
            _playbackError = null;
        }

        #endregion
    }
}

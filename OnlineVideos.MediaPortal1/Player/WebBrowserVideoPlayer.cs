using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using OnlineVideos.Helpers;
using OnlineVideos.Sites;
using MediaPortal.InputDevices;
using ActionType = MediaPortal.GUI.Library.Action.ActionType;
using Action = MediaPortal.GUI.Library.Action;
using System.Windows.Forms;
using OnlineVideos.Sites.Proxy.WebBrowserPlayerService;
using OnlineVideos.Sites.WebBrowserPlayerService.ServiceImplementation;

namespace OnlineVideos.MediaPortal1.Player
{
    /// <summary>
    /// Player which automates a web browser - will minimise MediaPortal and shell to the WebBrowserHost when play is requested
    /// </summary>
    public class WebBrowserVideoPlayer : IPlayer, OVSPLayer
    {
        private IntPtr _mpWindowHandle = IntPtr.Zero;
        private bool _mpWindowHidden = false;
        private Process _browserProcess;
        private string _automationType;
        private string _username;
        private string _password;
        private string _lastError;
        private WebBrowserPlayerCallbackServiceProxy _callbackServiceProxy;
        private WebBrowserPlayerCallback _callback = new WebBrowserPlayerCallback();
        private WebBrowserPlayerServiceProxy _serviceProxy;

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
            var browserConfig = (util as IBrowserSiteUtil);
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
            var processes = System.Diagnostics.Process.GetProcessesByName("OnlineVideos.WebAutomation.BrowserHost");
            if (processes != null && processes.Count() > 0)
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

        /// <summary>
        /// Play the specified file - file will actually be the video id from the website 
        /// This method will hide MediaPortal and run the BrowserHost - BorwserHost needs to support the WebAutomationType and have the code to actually navigate to the site
        /// </summary>
        /// <param name="strFile"></param>
        /// <returns></returns>
        public override bool Play(string strFile)
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
            var dir = MediaPortal.Configuration.Config.GetFolder(MediaPortal.Configuration.Config.Dir.Base);
            
            _browserProcess.StartInfo.FileName = Path.Combine(OnlineVideoSettings.Instance.DllsDir, "OnlineVideos.WebAutomation.BrowserHost.exe");
            _browserProcess.StartInfo.Arguments = string.Format("\"{0} \" \"{1}\" \"{2}\" \"{3}\" \"{4}\"",
                                            dir,
                                            strFile,
                                            _automationType,
                                            (string.IsNullOrEmpty(_username) ? "_" : _username),
                                            (string.IsNullOrEmpty(_password) ? "_" : _password));
            _browserProcess.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
            
            // Restart MP or Restore MP Window if needed
            _browserProcess.Exited += new EventHandler(BrowserProcess_Exited);

            // Hide MediaPortal
            if (_browserProcess.Start())
            {
                Thread.Sleep(2000); // Sleep for 2 seconds to allow the browser host to load - should prevent the desktop flashing up
                ReinitialiseService();
                SuspendMP(true);
                if (_browserProcess != null)
                {
                    ProcessHelper.SetForeground(_browserProcess.MainWindowHandle);
                    Redirect(_browserProcess.StandardError);
                }
                else
                {
                    OnlineVideos.Log.Error("Browser process closed on startup");
                    SuspendMP(false);
                }
            }
           
            return true;
        }

        /// <summary>
        /// When a new action is received we'll forward some of them to the browser host using key presses
        /// In future i'd like to refactor this to forward the action (somehow)
        /// </summary>
        /// <param name="action"></param>
        private void GUIWindowManager_OnNewAction(MediaPortal.GUI.Library.Action action)
        {
            // Forward the key on to the browser process 
            if (_browserProcess != null)
            {
                try
                {
                    if (_serviceProxy == null) ReinitialiseService();
                    _serviceProxy.OnNewAction(action.wID.ToString());
                }
                catch (Exception ex)
                {
                    OnlineVideos.Log.Error(ex);
                    ReinitialiseService(); // Attempt to reinitialise the connection to the service
                    _serviceProxy.OnNewAction(action.wID.ToString());
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
                };
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
                OnlineVideos.Log.Error(_lastError);
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
                InputDevices.Stop(); //stop input devices so they don't interfere when the browser player starts listening
                GUIWindowManager.OnNewAction += GUIWindowManager_OnNewAction;

                // hide mediaportal and suspend rendering 
                GUIGraphicsContext.BlankScreen = true;
                GUIGraphicsContext.form.Hide();
                GUIGraphicsContext.CurrentState = GUIGraphicsContext.State.SUSPENDING;
                
                _mpWindowHidden = true;
            }
            else //resume Mediaportal
            {
                GUIWindowManager.OnNewAction -= GUIWindowManager_OnNewAction;
                
                // Resume Mediaportal rendering
                GUIGraphicsContext.BlankScreen = false;
                GUIGraphicsContext.form.Show();
                
                ProcessHelper.SetForeground("mediaportal");
                
                GUIGraphicsContext.ResetLastActivity();
                
                GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_GETFOCUS, 0, 0, 0, 0, 0, null);
                GUIWindowManager.SendThreadMessage(msg);

                GUIGraphicsContext.CurrentState = GUIGraphicsContext.State.RUNNING;
                _mpWindowHidden = false;
            }
        }

        /// <summary>
        /// Clean up
        /// </summary>
        public override void Dispose()
        {
            // Clean up the service proxy
            if (_serviceProxy != null)
            {
                if (_serviceProxy.State == System.ServiceModel.CommunicationState.Opened) _serviceProxy.Close();
                _serviceProxy.Dispose();
            }
            // Clean up the callback service proxy
            if (_callbackServiceProxy != null)
            {
                if (_callbackServiceProxy.State == System.ServiceModel.CommunicationState.Opened) _callbackServiceProxy.Close();
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

            if (InputDevices.WndProc(ref msg, out action, out key, out keyCode))
            {
                //If remote doesn't fire event directly we manually fire it
                if (action != null && action.wID != Action.ActionType.ACTION_INVALID)
                {
                    GUIWindowManager_OnNewAction(action);
                }

                if (keyCode != Keys.A)
                {
                    var ke = new KeyEventArgs(keyCode);
                    _callback_OnBrowserKeyPress(ke.KeyValue);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// When the browser has a key press, try and map it to an action and fire the on action 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _callback_OnBrowserKeyPress(int keyPressed)
        {
            var action = new Action();

            if (ActionTranslator.GetAction(-1, new Key(0, keyPressed), ref action))
            {
                GUIWindowManager_OnNewAction(action);
            }
            else
            {
                //See if it's mapped to KeyPressed instead
                if (keyPressed >= (int)Keys.A && keyPressed <= (int)Keys.Z)
                   keyPressed += 32; //convert to char code
                if (ActionTranslator.GetAction(-1, new Key(keyPressed, 0), ref action))
                    GUIWindowManager_OnNewAction(action);
            }
        }
        #endregion
    }
}

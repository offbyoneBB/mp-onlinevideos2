using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using OnlineVideos.Helpers;
using OnlineVideos.Sites;

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

            // Wire up to an existing browser process if one exists
            var processes = System.Diagnostics.Process.GetProcessesByName("OnlineVideos.WebAutomation.BrowserHost");
            if (processes != null && processes.Count() > 0)
                _browserProcess = processes[0];
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
                ProcessHelper.SetForeground(_browserProcess.MainWindowHandle);
                return true;
            }

            GUIWindowManager.OnNewAction += GUIWindowManager_OnNewAction;

            // Set up the process
            // Process requires path to MediaPortal, Web Automation Type, Video Id, Username, Password
            _browserProcess = new Process();
            _browserProcess.StartInfo.UseShellExecute = false;
            _browserProcess.StartInfo.RedirectStandardError = true;
            _browserProcess.EnableRaisingEvents = true;
            //_browserProcess.StartInfo.FileName = "plugins\\Windows\\OnlineVideos\\OnlineVideos.Sites.WebAutomation.BrowserHost.exe";
            var dllDir = OnlineVideoSettings.Instance.DllsDir;
            _browserProcess.StartInfo.FileName = Path.Combine(OnlineVideoSettings.Instance.DllsDir, "OnlineVideos.WebAutomation.BrowserHost.exe");
            _browserProcess.StartInfo.Arguments = string.Format("\"{0} \" \"{1}\" \"{2}\" \"{3}\" \"{4}\"",
                                            dllDir,
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
                SuspendMP(true);
                Redirect(_browserProcess.StandardError);
            }

            ProcessHelper.SetForeground(_browserProcess.MainWindowHandle);
            
            return true;
        }

        /// <summary>
        /// We need to make sure that when the action is complete we set the browser window as the active one
        /// </summary>
        /// <param name="action"></param>
        private void GUIWindowManager_OnNewAction(MediaPortal.GUI.Library.Action action)
        {
            // Forward the key on to the browser process - I can't seem to get MediaPortal to detach from the remote key presses (using iMon as the control)
            if (_browserProcess != null && 
                    (action.wID == MediaPortal.GUI.Library.Action.ActionType.ACTION_KEY_PRESSED/* ||
                        action.wID == MediaPortal.GUI.Library.Action.ActionType.ACTION_MUSIC_PLAY ||
                        action.wID == MediaPortal.GUI.Library.Action.ActionType.ACTION_STOP ||
                        action.wID == MediaPortal.GUI.Library.Action.ActionType.ACTION_PLAY ||
                        action.wID == MediaPortal.GUI.Library.Action.ActionType.ACTION_PAUSE*/
                    ))
            {

                ProcessHelper.SetForeground(_browserProcess.MainWindowTitle);
                System.Windows.Forms.SendKeys.Send(new string((Char)action.m_key.KeyChar, 1).ToUpper());
                //ProcessHelper.SendKeyToProcess(_browserProcess.MainWindowTitle, GetKeyFromChar(action.m_key.KeyChar));
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
                //InputDevices.Stop();
                
                // hide mediaportal and suspend rendering 
                GUIGraphicsContext.BlankScreen = true;
                GUIGraphicsContext.form.Hide();
                GUIGraphicsContext.CurrentState = GUIGraphicsContext.State.SUSPENDING;
                // Hide the window
                _mpWindowHandle = ProcessHelper.SetWindowState("mediaportal", ProcessHelper.WINDOW_STATE.SW_HIDE);
                _mpWindowHidden = true;
            }
            else //resume Mediaportal
            {
                
                // Restore the window
                ProcessHelper.RestoreWindow(_mpWindowHandle);
                //InputDevices.Init();

                // Resume Mediaportal rendering
                GUIGraphicsContext.BlankScreen = false;
                GUIGraphicsContext.form.Show();
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

        /// <summary>
        /// Attempt to convert the keyChar to a keys enum value
        /// </summary>
        /// <param name="keyChar"></param>
        /// <returns></returns>
        private System.Windows.Forms.Keys GetKeyFromChar(int keyChar)
        {
            var converter = new System.Windows.Forms.KeysConverter();
            var keycharAsString = new string((Char)keyChar, 1).ToUpper();
            var convertedItem = converter.ConvertFrom(keycharAsString);
            if (convertedItem == null)
                return (System.Windows.Forms.Keys)(keyChar);
            else
                return (System.Windows.Forms.Keys)convertedItem;
        }

        public bool GoFullscreen { get; set; }

        public string SubtitleFile { get; set; }

        public string PlaybackUrl { get; set; }
    }
}

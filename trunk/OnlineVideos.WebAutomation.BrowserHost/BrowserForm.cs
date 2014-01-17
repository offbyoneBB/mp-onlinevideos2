
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OnlineVideos.Helpers;
using OnlineVideos.Sites.WebAutomation.BrowserHost.Factories;
using System.IO;
using System.Diagnostics;
using System.Configuration;
using OnlineVideos.Sites.Base;

namespace OnlineVideos.Sites.WebAutomation.BrowserHost
{
    public partial class BrowserForm : Form
    {
        public bool ForceClose { get; private set; }

        private string _connectorType;
        private string _videoInfo;
        private string _userName;
        private string _password;
        private BrowserUtilConnector _connector;

        private Keys? _playKey;
        private Keys? _pauseKey;
        private Keys? _stopKey;

        private bool _debugMode = false; // Allow for the form to be resized/lose focus in debug mode

        private Keys _lastKeyPressed;
        private DateTime _lastKeyPressedTime;

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
            InitializeComponent();
            _connectorType = connectorType;
            _videoInfo = videoInfo;
            _userName = userName;
            _password = password;
            _playKey = GetKeyForAction("PlayKey");
            _pauseKey = GetKeyForAction("PauseKey");
            _stopKey = GetKeyForAction("StopKey");
            _debugMode = false;

            var configValue = ConfigurationManager.AppSettings["DebugMode"];
            if (!string.IsNullOrEmpty(configValue) && configValue.ToUpper() == "TRUE")
                _debugMode = true;
            
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
                SetScreenState();
                
                ForceClose = false;
                this.Activate();
                this.Focus();
                
                _connector = BrowserInstanceConnectorFactory.GetConnector(_connectorType, OnlineVideoSettings.Instance.Logger, webBrowser);
                if (_connector == null)
                {
                    Log.Warn(string.Format("Unable to load connector type {0}, not found in any site utils",  _connectorType));
                    ForceQuit();
                    return;
                }
                _connector.PerformLogin(_userName, _password);

                var result = _connector.WaitForComplete(ForceQuitting, OnlineVideoSettings.Instance.UtilTimeout);

                if (result)
                {
                    _connector.PlayVideo(_videoInfo);
                    result = _connector.WaitForComplete(ForceQuitting, OnlineVideoSettings.Instance.UtilTimeout);
                    if (!result)
                        ForceQuit();
                }
            }
            catch (Exception ex)
            {
                //MediaPortal.Common.Utils.Logger.CommonLogger.Instance.Error(MediaPortal.Common.Utils.Logger.CommonLogType.Error, ex);
                Log.Error(ex);
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
            HandleKeyPress(e.KeyCode);
        }

        /// <summary>
        /// Handle the escape key click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void webBrowser_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            HandleKeyPress(e.KeyCode);
        }

        /// <summary>
        /// Event sink for key press
        /// </summary>
        /// <param name="keyPressed"></param>
        private void HandleKeyPress(Keys keyPressed)
        {
            // Ignore duplicate presses within 1 second (happens because sometimes both the browser and form fire the event)
            if (_lastKeyPressed == keyPressed && _lastKeyPressedTime.AddSeconds(1) > DateTime.Now) return;
            _lastKeyPressed = keyPressed;
            _lastKeyPressedTime = DateTime.Now;

            if (keyPressed == Keys.Escape)
                ForceQuit();
            if (_playKey.HasValue && keyPressed == _playKey)
                _connector.Play();
            if (_stopKey.HasValue && keyPressed == _stopKey)
                ForceQuit();
            if (_pauseKey.HasValue && keyPressed == _pauseKey)
                _connector.Pause();
        }

        /// <summary>
        /// Close the application
        /// </summary>
        private void ForceQuit()
        {
            BeginInvoke((MethodInvoker)delegate
            {
                this.Close();
                ForceClose = true;
                Process.GetCurrentProcess().Kill(); // In case we've got some weird browser issues or something hogging the process
            });
        }

        /// <summary>
        /// Attempt to keep the current application on top
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tmrKeepOnTop_Tick(object sender, EventArgs e)
        {
            ProcessHelper.SetForeground(Process.GetCurrentProcess().MainWindowHandle);
        }

        /// <summary>
        /// Lookup the key code for the specified action name
        /// </summary>
        /// <param name="actionName"></param>
        /// <returns></returns>
        private Keys? GetKeyForAction(string actionName)
        {
            var configValue = ConfigurationManager.AppSettings[actionName];
            if (string.IsNullOrEmpty(configValue)) return null;
            
            if (System.Enum.GetNames(typeof(Keys)).Where(x => x.ToUpper() == configValue).Count() > 0)
                return (Keys)System.Enum.Parse(typeof(Keys), configValue, true);
            
            return null;
        }

        /// <summary>
        /// Set the screen state depending on which mode it's in
        /// </summary>
        private void SetScreenState()
        {
            if (!_debugMode) Cursor.Hide();
            if (_debugMode) tmrKeepOnTop.Enabled = false;
            this.WindowState = _debugMode ? FormWindowState.Normal : FormWindowState.Maximized;
            this.FormBorderStyle = _debugMode ? FormBorderStyle.Sizable : FormBorderStyle.FixedDialog;
            this.ControlBox = _debugMode;            
        }

    }
}

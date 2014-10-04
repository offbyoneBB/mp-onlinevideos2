
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
using MediaPortal.GUI.Library;
using MediaPortal.InputDevices;
using Action = MediaPortal.GUI.Library.Action;

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

        public bool ForceClose { get; private set; }

        private string _connectorType;
        private string _videoInfo;
        private string _userName;
        private string _password;
        private BrowserUtilConnector _connector;

        private bool _debugMode = false; // Allow for the form to be resized/lose focus in debug mode

        private int _lastKeyPressed;
        private DateTime _lastKeyPressedTime;

        private PlayPauseToggle _lastPlayPauseState = PlayPauseToggle.Play;

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
            _debugMode = false;

            var configValue = ConfigurationManager.AppSettings["DebugMode"];
            if (!string.IsNullOrEmpty(configValue) && configValue.ToUpper() == "TRUE")
                _debugMode = true;

            //Load keyboard mappings
            ActionTranslator.Load();

            //Load remote mappings
            InputDevices.Init();

            //Some remotes will fire this event directly
            GUIGraphicsContext.OnNewAction += OnNewAction;
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
                else
                {
                    ForceQuit();
                }                
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(string.Format("{0}\r\n{1}", ex.Message, ex.StackTrace));
                Console.Error.Flush();
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
            HandleKeyPress(e.KeyValue);
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

        //Used to pass messages to remotes. Lifted from MediaPortal.cs
        protected override void WndProc(ref Message msg)
        {
            Action action;
            char key;
            Keys keyCode;
            if (InputDevices.WndProc(ref msg, out action, out key, out keyCode))
            {
                //If remote doesn't fire event directly we manually fire it
                if (action != null && action.wID != Action.ActionType.ACTION_INVALID)
                {
                    OnNewAction(action);
                }

                if (keyCode != Keys.A)
                {
                    var ke = new KeyEventArgs(keyCode);
                    OnKeyDown(ke);
                }
                return; // abort WndProc()
            }
            base.WndProc(ref msg);
        }

        /// <summary>
        /// Event sink for key press
        /// </summary>
        /// <param name="keyPressed"></param>
        private void HandleKeyPress(int keyPressed)
        {
            // Ignore duplicate presses within 1 seconds (happens because sometimes both the browser and form fire the event)
            if (_lastKeyPressed == keyPressed && _lastKeyPressedTime.AddSeconds(1) > DateTime.Now) return;
            _lastKeyPressed = keyPressed;
            _lastKeyPressedTime = DateTime.Now;
            
            Action action = new Action();
            //Try and get corresponding Action from key.
            //Some actions are mapped to KeyDown others to KeyPressed, try and handle both
            if (ActionTranslator.GetAction(-1, new Key(0, keyPressed), ref action))
            {
                OnNewAction(action);
            }
            else
            {
                //See if it's mapped to KeyPressed instead
                if (keyPressed >= (int)Keys.A && keyPressed <= (int)Keys.Z)
                    keyPressed += 32; //convert to char code
                if (ActionTranslator.GetAction(-1, new Key(keyPressed, 0), ref action))
                    OnNewAction(action);
            }
        }
        
        /// <summary>
        /// Handle actions
        /// We'll make play/pause a toggle, just so we can ensure we support media buttons properly
        /// </summary>
        /// <param name="action"></param>
        void OnNewAction(Action action)
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)delegate() { OnNewAction(action); });
                return;
            }
            switch (action.wID)
            {
                case Action.ActionType.ACTION_PLAY:
                case Action.ActionType.ACTION_MUSIC_PLAY:
                    if (_lastPlayPauseState == PlayPauseToggle.Play)
                        OnNewAction(new Action(Action.ActionType.ACTION_PAUSE, 0, 0));
                    else
                    {
                        _connector.Play();
                        _lastPlayPauseState = PlayPauseToggle.Play;
                    }
                    break;
                case Action.ActionType.ACTION_PAUSE:
                    if (_lastPlayPauseState == PlayPauseToggle.Pause)
                        OnNewAction(new Action(Action.ActionType.ACTION_PLAY, 0, 0));
                    else
                    {
                        _connector.Pause();
                        _lastPlayPauseState = PlayPauseToggle.Pause;
                    }
                    break;
                case Action.ActionType.ACTION_STOP:
                case Action.ActionType.ACTION_PREVIOUS_MENU:
                    ForceQuit();
                    break;
                default:
                    // fire the action on the connector also
                    _connector.OnAction(action.wID.ToString());
                    break;
            }

        }

        /// <summary>
        /// Close the application
        /// </summary>
        private void ForceQuit()
        {
            _connector.OnClosing();
            ForceClose = true;
            Process.GetCurrentProcess().Kill(); // In case we've got some weird browser issues or something hogging the process
        }

        /// <summary>
        /// Attempt to keep the current application on top
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tmrKeepOnTop_Tick(object sender, EventArgs e)
        {
            ProcessHelper.SetForeground(Process.GetCurrentProcess().MainWindowHandle);
            this.Activate();
            this.Focus();
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

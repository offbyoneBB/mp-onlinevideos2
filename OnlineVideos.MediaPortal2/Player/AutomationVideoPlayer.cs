using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.RawUrlResourceProvider;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.SkinEngine.InputManagement;
using OnlineVideos.Helpers;
using OnlineVideos.MediaPortal2.Interfaces.Metadata;
using OnlineVideos.MediaPortal2.ResourceAccess;
using OnlineVideos.Sites;
using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using OnlineVideos.Sites.Interfaces;
using Keys = OpenQA.Selenium.Keys;

namespace OnlineVideos.MediaPortal2
{
    /// <summary>
    /// Player which automates a web browser - will minimise MediaPortal and shell to the WebBrowserHost when play is requested
    /// </summary>
    public class AutomationVideoPlayer : IPlayer, IPlayerEvents, IUIContributorPlayer
    {
        private bool _mpWindowHidden;
        private IWebDriverSite _webDriverSite;
        private string _fileOrUrl;

        // Player event delegates
        protected PlayerEventDlgt _started = null;
        protected PlayerEventDlgt _stateReady = null;
        protected PlayerEventDlgt _stopped = null;
        protected PlayerEventDlgt _ended = null;
        protected PlayerEventDlgt _playbackStateChanged = null;
        protected PlayerEventDlgt _playbackError = null;

        /// <summary>
        /// We require the command line parameters for the web browser host
        /// Util should be an implementation of IBrowserSiteUtil
        /// </summary>
        /// <param name="mediaItem"></param>
        public void Prepare(MediaItem mediaItem)
        {
            string siteName;
            SiteUtilBase util;
            if (!MediaItemAspect.TryGetAttribute(mediaItem.Aspects, OnlineVideosAspect.ATTR_SITEUTIL, out siteName) || !OnlineVideoSettings.Instance.SiteUtilsList.TryGetValue(siteName, out util))
            {
                throw new ArgumentException("Could not determine used site util, can't start playback!");
            }

            _webDriverSite = util as IWebDriverSite;
            if (_webDriverSite == null) throw new ArgumentException("SiteUtil does not implement IWebDriverSite");
        }

        public bool Init(MediaItem mediaItem)
        {
            // Prepare process information
            Prepare(mediaItem);

            IResourceAccessor ra = null;
            try
            {
                ra = mediaItem.GetResourceLocator().CreateAccessor();
                RawUrlResourceAccessor rua = ra as RawUrlResourceAccessor;
                if (rua != null)
                {
                    _fileOrUrl = rua.URL;
                }
                else
                {
                    RawTokenResourceAccessor rra = ra as RawTokenResourceAccessor;
                    if (rra == null)
                        return false;
                    _fileOrUrl = rra.Token;
                }
                Play();
                return true;
            }
            catch (Exception)
            {
                // Log
            }
            finally
            {
                ra?.Dispose();
            }
            return false;
        }

        /// <summary>
        /// Play the specified file - file will actually be the video id from the website 
        /// This method will hide MediaPortal and run the BrowserHost - BrowserHost needs to support the WebAutomationType and have the code to actually navigate to the site
        /// </summary>
        /// <returns></returns>
        public bool Play()
        {
            try
            {
                _webDriverSite.StartPlayback(_fileOrUrl);
                var proxy = new KeyProxy();
                proxy.OnStop += delegate { Stop(); };
                _webDriverSite.SetKeyHandler(proxy);
                SuspendMP(true);

                // Keep input handling in MP2
                //((Form)ServiceRegistration.Get<IScreenControl>()).Activate();
            }
            catch (Exception e)
            {
                SuspendMP(false);
                return false;
            }
            return true;
        }

        private readonly Dictionary<Key, string> KEY_MAPPINGS = new Dictionary<Key, string>
        {
            //{ Key.Play, Keys. },
            { Key.PlayPause, Keys.Pause },
            { Key.Pause, Keys.Pause },
            //{ Key.Stop, OnlineVideos.Constants.ACTION_STOP },
            { Key.Info, Keys.Command },
            { Key.Left, Keys.Left },
            { Key.Right, Keys.Return },
            //{ Key.Previous, Keys.Backspace },
            //{ Key.Next, Keys. },
            { Key.Ok, OnlineVideos.Constants.ACTION_WINDOWED },
            { Key.Back, Keys.Backspace },
            { Key.Fullscreen, OnlineVideos.Constants.ACTION_FULLSCREEN },
            { Key.Red, OnlineVideos.Constants.ACTION_FULLSCREEN },
        };


        /// <summary>
        /// Handles key presses that should be simply forwarded to client (especially number keys).
        /// </summary>
        private bool IsPassThrough(Key key, out string action)
        {
            action = null;
            if (key.IsPrintableKey && key.RawCode.HasValue && key.RawCode.Value >= '0' && key.RawCode.Value <= '9')
            {
                action = key.RawCode.Value.ToString();
                return true;
            }
            return false;
        }

        private void ProcessActionArguments(ref string action)
        {
            if (action == OnlineVideos.Constants.ACTION_WINDOWED)
            {
                // Make sure the MP2 GUI is back visible
                ServiceRegistration.Get<IScreenControl>().Restore();
                _webDriverSite?.SetWindowBoundaries(
                    new System.Drawing.Point((int)TargetBounds.Left, (int)TargetBounds.Top),
                    new System.Drawing.Size((int)TargetBounds.Width, (int)TargetBounds.Height));
            }
            if (action == OnlineVideos.Constants.ACTION_FULLSCREEN)
            {
                SuspendMP(true);
                _webDriverSite?.Fullscreen();
            }
            if (action == OnlineVideos.Constants.ACTION_STOP)
            {
                Shutdown();
            }
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
                InputManager.Instance.KeyPreview += InstanceOnKeyPressed;

                // Minimise MePo to tray - this is preferable 
                ToggleMinimise(true);

                _mpWindowHidden = true;
            }
            else //resume Mediaportal
            {
                InputManager.Instance.KeyPreview -= InstanceOnKeyPressed;

                // Resume Mediaportal rendering
                ToggleMinimise(false);

                ProcessHelper.SetForeground("MediaPortal 2");

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
            ServiceRegistration.Get<IScreenControl>().DisableTopMost = shouldMinimise;
            if (shouldMinimise)
                ServiceRegistration.Get<IScreenControl>().Minimize();
            else
                ServiceRegistration.Get<IScreenControl>().Restore();
        }

        /// <summary>
        /// When a new action is received we'll forward them to the browser host using a WCF service
        /// </summary>
        /// <param name="key"></param>
        private void InstanceOnKeyPressed(ref Key key)
        {
            string action;
            if (KEY_MAPPINGS.TryGetValue(key, out action) || IsPassThrough(key, out action))
            {
                ProcessActionArguments(ref action);
                try
                {
                    _webDriverSite.HandleAction(action);
                }
                catch (Exception ex)
                {
                }
            }
            // While player is active prevent any other key handling inside MP2, just forward the matching actions to WebBrowserPlayer
            key = Key.None; // Handled
        }

        /// <summary>
        /// Clean up
        /// </summary>
        public void Dispose()
        {
            Shutdown();
        }

        private void Shutdown()
        {
            if (_mpWindowHidden)
                SuspendMP(false);
            // Clean up the service proxy
            try
            {
                _webDriverSite?.ShutDown();
            }
            catch { }
        }

        protected void Ended()
        {
            State = PlayerState.Ended;
            FireEnded();
        }

        public void Stop()
        {
            Shutdown();
            State = PlayerState.Ended;
            FireEnded();
        }

        public string Name { get; private set; }
        public PlayerState State { get; private set; }
        public string MediaItemTitle { get; private set; }

        #region Event handling

        protected void FireStarted()
        {
            _started?.Invoke(this);
        }

        protected void FireStateReady()
        {
            _stateReady?.Invoke(this);
        }

        protected void FireStopped()
        {
            _stopped?.Invoke(this);
        }

        protected void FireEnded()
        {
            _ended?.Invoke(this);
        }

        protected void FirePlaybackStateChanged()
        {
            _playbackStateChanged?.Invoke(this);
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

        public Type UIContributorType
        {
            get { return typeof(WebBrowserPlayerUIContributor); }
        }

        public RectangleF TargetBounds { get; set; }
    }

    public class KeyProxy : MarshalByRefObject, IWebDriverKeyHandler
    {
        public EventHandler OnStop;

        public bool HandleKey(string key)
        {
            if (key == "MediaStop" || key == "Escape")
            {
                OnStop?.Invoke(this, EventArgs.Empty);
                return true;
            }

            return false;
        }
    }
}


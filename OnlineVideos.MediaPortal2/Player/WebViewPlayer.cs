//#define OSDForDevTools

using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.RawUrlResourceProvider;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.Utilities.Screens;
using Microsoft.Web.WebView2.Core;
using OnlineVideos.Helpers;
using OnlineVideos.MediaPortal2.Interfaces.Metadata;
using OnlineVideos.MediaPortal2.ResourceAccess;
using OnlineVideos.Sites;
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace OnlineVideos.MediaPortal2.Player
{
    public class WebViewPlayer : IPlayer, IPlayerEvents, IMediaPlaybackControl, IUIContributorPlayer, IOVSPLayer
    {
        public const string WEBVIEW_MIMETYPE = "video/webview2";
        private string _fileOrUrl;
        private Control _playerForm;

        // Player event delegates
        protected PlayerEventDlgt _started = null;
        protected PlayerEventDlgt _stateReady = null;
        protected PlayerEventDlgt _stopped = null;
        protected PlayerEventDlgt _ended = null;
        protected PlayerEventDlgt _playbackStateChanged = null;
        protected PlayerEventDlgt _playbackError = null;

        private IWebViewSiteUtilBase _siteUtil;
        private WebViewHelper _wvHelper;

        private Microsoft.Web.WebView2.WinForms.WebView2 _webView;

        private PlayState _playState;
        private RectangleF _targetBounds;
        public string SubtitleFile { get; set; }
        public string PlaybackUrl { get; set; }
        public bool GoFullscreen { get; set; }

        public WebViewPlayer()
        {
            //_mainForm = Control.FromHandle(ServiceRegistration.Get<IScreenControl>().MainWindowHandle);
        }

        private void Invoke(Action action)
        {
            if (_playerForm.InvokeRequired)
            {
                IAsyncResult iar = _playerForm.BeginInvoke(action);
                iar.AsyncWaitHandle.WaitOne();
            }
            else
            {
                action.Invoke();
            }
        }

        private bool Invoke(Func<bool> func)
        {
            if (_playerForm.InvokeRequired)
            {
                IAsyncResult iar = _playerForm.BeginInvoke(func);
                iar.AsyncWaitHandle.WaitOne();
                return (bool)_playerForm.EndInvoke(iar);
            }
            else
            {
                return func.Invoke();
            }
        }


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

            _siteUtil = util as IWebViewSiteUtilBase;
            if (_siteUtil == null) throw new ArgumentException("SiteUtil does not implement IWebDriverSite");
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
            if (_wvHelper == null)
            {
                var mainForm = ServiceRegistration.Get<IScreenControl>() as Form;
                _wvHelper = WebViewHelper.GetInstance(mainForm);
                _webView = _wvHelper.GetWebViewForPlayer;
                _playerForm = _wvHelper.SynchronizationContext;
            }
            return Invoke(PlayInternal);
        }

        private bool PlayInternal()
        {
            SplashScreen loadingScreen = null;
            try
            {
                //var imageName = MediaPortal.Utilities.FileSystem.FileUtils.BuildAssemblyRelativePath("loading.gif");
                //loadingScreen = new SplashScreen
                //{
                //    SplashBackgroundImage = Image.FromFile(imageName),
                //    ScaleToFullscreen = false,
                //    FadeOutDuration = TimeSpan.FromMilliseconds(2000),
                //    UsePictureBox = true,
                //    TopMost = true
                //};
                //// Fullscreen splash
                //loadingScreen.ShowSplashScreen();

                //_webView.Location = new Point((int)TargetBounds.Left, (int)TargetBounds.Top);
                //_webView.Size = new Size(400, 300); //new Size((int)TargetBounds.Width, (int)TargetBounds.Height);
                //_webView.Name = "webview";
                //_webView.Visible = false;
                //_webView.Enabled = false;
                _playState = PlayState.Init;
                ServiceRegistration.Get<MediaPortal.Common.Logging.ILogger>().Info("WebViewPlayer: Play '{0}'", _fileOrUrl);

                _webView.NavigationCompleted += WebView_FirstNavigationCompleted;
                //if (_mainForm != null)
                //{
                //    _mainForm.Controls.Add(_webView);
                //}
                // TODO:
                //GUIGraphicsContext.form.Controls.Add(_webView);
                //GUIWaitCursor.Init(); GUIWaitCursor.Show(); // init and show the wait cursor while buffering

                _webView.Source = new Uri(_fileOrUrl);
                _wvHelper.Show();

                //if (GoFullscreen) GUIWindowManager.ActivateWindow(GUIOnlineVideoFullscreen.WINDOW_FULLSCREEN_ONLINEVIDEO);
#if OSDForDevTools
                GUIWindowManager.OnNewAction += GUIWindowManager_OnNewAction;
#endif
            }
            catch (Exception e)
            {
                return false;
            }
            finally
            {
                loadingScreen?.CloseSplashScreen();
            }
            return true;
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
            try
            {
                // TODO:
                //GUIGraphicsContext.form.Controls.Remove(_webView);
                //if (_mainForm != null)
                //{
                //    _mainForm.Controls.Remove(_webView);
                //}
                _webView.NavigationCompleted -= WebView_FirstNavigationCompleted;
                _webView.NavigationCompleted -= WebView_FurtherNavigationCompleted;
                _wvHelper.Hide();
                _wvHelper.SetUrl("about:blank");
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
            FireStopped();
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
            get { return typeof(WebViewPlayerUIContributor); }
        }

        public RectangleF TargetBounds
        {
            get => _targetBounds;
            set
            {
                _targetBounds = value;
                if (_webView != null)
                {
                    System.Action si = () =>
                    {
                        _playerForm.Location = new Point((int)_targetBounds.Left, (int)_targetBounds.Top);
                        _playerForm.Size = new Size((int)_targetBounds.Width, (int)_targetBounds.Height);
                        //_webView.Location = new Point(FullScreen ? 0 : GUIGraphicsContext.VideoWindow.X, FullScreen ? 0 : GUIGraphicsContext.VideoWindow.Y);
                        //_webView.ClientSize = new Size(FullScreen ? GUIGraphicsContext.Width : GUIGraphicsContext.VideoWindow.Width, FullScreen ? GUIGraphicsContext.Height : GUIGraphicsContext.VideoWindow.Height);
                    };
                    Invoke(si);
                    //_videoRectangle = new Rectangle(_webView.Location.X, _webView.Location.Y, _webView.ClientSize.Width, _webView.ClientSize.Height);
                    //_sourceRectangle = _videoRectangle;
                }
            }
        }

        public bool SetPlaybackRate(double value)
        {
            return false;
        }

        public void Pause()
        {
            if (_webView != null && _playState == PlayState.Playing || _playState == PlayState.Paused)
            {
                if (_playState == PlayState.Playing)
                {
                    _playState = PlayState.Paused;
                    if (_siteUtil is IWebViewHTMLMediaElement)
                    {
                        var v = GetVideoElementSelector;
                        if (!String.IsNullOrEmpty(v))
                            _wvHelper.Execute(v + ".pause()");
                    }
                    else
                    if (_siteUtil is IWebViewSiteUtil)
                    {
                        ((IWebViewSiteUtil)_siteUtil).DoPause();
                    }
                }
                else
                {
                    _playState = PlayState.Playing;
                    if (_siteUtil is IWebViewHTMLMediaElement)
                    {
                        var v = GetVideoElementSelector;
                        if (!String.IsNullOrEmpty(v))
                            _wvHelper.Execute(v + ".play()");
                    }
                    else
                    if (_siteUtil is IWebViewSiteUtil)
                    {
                        ((IWebViewSiteUtil)_siteUtil).DoPlay();
                    }
                }
                //if (disableVMRWhenRunning)
                //    GUIGraphicsContext.Vmr9Active = _playState==PlayState.Playing;
            }
        }

        public void Resume()
        {
            Play();
        }

        public void Restart()
        {
            CurrentTime = TimeSpan.Zero;
        }

        public TimeSpan CurrentTime
        {
            get
            {
                var v = GetVideoElementSelector;
                if (!String.IsNullOrEmpty(v))
                {
                    var bb = _wvHelper.ExecuteFunc(v + @".currentTime");
                    float res;
                    if (float.TryParse(bb, NumberStyles.Any, CultureInfo.InvariantCulture, out res)) 
                        return TimeSpan.FromSeconds(res);
                }
                return TimeSpan.Zero;
            }
            set
            {
                var v = GetVideoElementSelector;
                if (!String.IsNullOrEmpty(v))
                {
                    _wvHelper.ExecuteFunc(v + ".currentTime=" + value.TotalSeconds.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        public TimeSpan Duration
        {
            get
            {
                var v = GetVideoElementSelector;
                if (!String.IsNullOrEmpty(v))
                {
                    var bb = _wvHelper.ExecuteFunc(v + @".duration");
                    float res;
                    if (float.TryParse(bb, NumberStyles.Any, CultureInfo.InvariantCulture, out res)) return TimeSpan.FromSeconds(res);
                }
                return TimeSpan.Zero;
            }
        }

        public double PlaybackRate { get; } = 1.0;
        public bool IsPlayingAtNormalRate { get; } = true;
        public bool IsSeeking { get; } = false;

        public bool IsPaused => _playState == PlayState.Paused;

        public bool CanSeekForwards { get; } = true;
        public bool CanSeekBackwards { get; } = true;


        private string GetVideoElementSelector
        {
            get
            {
                if (_siteUtil is IWebViewHTMLMediaElement)
                    return ((IWebViewHTMLMediaElement)_siteUtil).VideoElementSelector;
                return null;
            }
        }

        private void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
                ServiceRegistration.Get<ILog>().Error("Error initializing webview: {0}", e.InitializationException.Message);
            else
            {
                //if (GoFullscreen) GUIWindowManager.ActivateWindow(GUIOnlineVideoFullscreen.WINDOW_FULLSCREEN_ONLINEVIDEO);
            }
        }

        private void WebView_FirstNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            //if (disableVMRWhenRunning)
            //    GUIGraphicsContext.Vmr9Active = true;
            _playerForm.Visible = true;
            //GUIWaitCursor.Hide(); // hide the wait cursor
            _playState = PlayState.Playing;
            _siteUtil.StartPlayback();
            _webView.NavigationCompleted -= WebView_FirstNavigationCompleted;
            _webView.NavigationCompleted += WebView_FurtherNavigationCompleted;
        }

        private void WebView_FurtherNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            bool doStopPlayback = false;
            if (_siteUtil is IWebViewSiteUtil)
                ((IWebViewSiteUtil)_siteUtil).OnPageLoaded(ref doStopPlayback);
            if (doStopPlayback)
                Stop();
        }
    }


    public class WebViewPlayerUIContributor : BaseVideoPlayerUIContributor
    {
        public const string SCREEN_FS = "FullscreenContentOV";
        public const string SCREEN_CP = "CurrentlyPlayingOV";

        public override bool BackgroundDisabled
        {
            get { return false; }
        }

        public override string Screen
        {
            get
            {
                if (_mediaWorkflowStateType == MediaWorkflowStateType.CurrentlyPlaying)
                    return SCREEN_CP;
                if (_mediaWorkflowStateType == MediaWorkflowStateType.FullscreenContent)
                    return SCREEN_FS;
                return null;
            }
        }
    }
}

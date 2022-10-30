using System;
using System.Drawing;
using OnlineVideos.Sites;
using OnlineVideos.Helpers;
using MediaPortal.Player;
using MediaPortal.GUI.Library;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace OnlineVideos.MediaPortal1.Player
{
    public class WebViewPlayer : IPlayer, OVSPLayer
    {
        private bool disableVMRWhenRunning = true;//when true no mousecursor is rendered...
        private IWebViewSiteUtilBase _siteUtil;
        private WebViewHelper wvHelper;

        private WebView2 webView;

        private PlayState playState;
        public string SubtitleFile { get; set; }
        public string PlaybackUrl { get; set; }
        public bool GoFullscreen { get; set; }

        public WebViewPlayer(IWebViewSiteUtilBase siteUtil)
        {
            _siteUtil = siteUtil;
            disableVMRWhenRunning = !OSInfo.OSInfo.Win8OrLater();
        }

        public override void Dispose()
        {
        }
        private void DoDispose()
        {
            GUIGraphicsContext.form.Controls.Remove(webView);
            webView.NavigationCompleted -= WebView_FirstNavigationCompleted;
            webView.NavigationCompleted -= WebView_FurtherNavigationCompleted;
            webView.Source = new Uri("about:blank");
        }

        public override bool Play(string strFile)
        {
            try
            {
                wvHelper = WebViewHelper.Instance;
                webView = wvHelper.GetWebViewForPlayer;
                webView.Location = new Point(0, 0);
                webView.Size = GUIGraphicsContext.form.Size;
                webView.Name = "webview";
                webView.Visible = false;
                webView.Enabled = false;
                playState = PlayState.Init;

                webView.NavigationCompleted += WebView_FirstNavigationCompleted;
                GUIGraphicsContext.form.Controls.Add(webView);
                GUIWaitCursor.Init(); GUIWaitCursor.Show(); // init and show the wait cursor while buffering

                webView.Source = new Uri(strFile);
                if (GoFullscreen) GUIWindowManager.ActivateWindow(GUIOnlineVideoFullscreen.WINDOW_FULLSCREEN_ONLINEVIDEO);
                GUIWindowManager.OnNewAction += GUIWindowManager_OnNewAction;

                return true;
            }
            catch (Exception e)
            {
                OnlineVideos.Log.Error("Error playing " + strFile + ": " + e.Message);
                return false;
            }
        }

        private void GUIWindowManager_OnNewAction(MediaPortal.GUI.Library.Action action)
        {
            if (action.wID == MediaPortal.GUI.Library.Action.ActionType.ACTION_SHOW_OSD)
            {
                webView.CoreWebView2.OpenDevToolsWindow();
            }
        }

        public override void SetVideoWindow()
        {
            if (webView != null)
            {
                System.Action si = () =>
                {
                    webView.Location = new Point(FullScreen ? 0 : GUIGraphicsContext.VideoWindow.X, FullScreen ? 0 : GUIGraphicsContext.VideoWindow.Y);
                    webView.ClientSize = new Size(FullScreen ? GUIGraphicsContext.Width : GUIGraphicsContext.VideoWindow.Width, FullScreen ? GUIGraphicsContext.Height : GUIGraphicsContext.VideoWindow.Height);
                };

                if (webView.InvokeRequired)
                {
                    IAsyncResult iar = webView.BeginInvoke(si);
                    iar.AsyncWaitHandle.WaitOne();
                }
                else
                {
                    si();
                }

                _videoRectangle = new Rectangle(webView.Location.X, webView.Location.Y, webView.ClientSize.Width, webView.ClientSize.Height);
                _sourceRectangle = _videoRectangle;
            }
        }

        public override void Pause()
        {
            if (webView != null && playState == PlayState.Playing || playState == PlayState.Paused)
            {
                if (playState == PlayState.Playing)
                {
                    playState = PlayState.Paused;
                    if (_siteUtil is IWebViewHTMLMediaElement)
                    {
                        var v = GetVideoElementSelector;
                        if (!String.IsNullOrEmpty(v))
                            wvHelper.Execute(v + ".pause()");
                    }
                    else
                    if (_siteUtil is IWebViewSiteUtil)
                    {
                        ((IWebViewSiteUtil)_siteUtil).DoPause();
                    }
                }
                else
                {
                    playState = PlayState.Playing;
                    if (_siteUtil is IWebViewHTMLMediaElement)
                    {
                        var v = GetVideoElementSelector;
                        if (!String.IsNullOrEmpty(v))
                            wvHelper.Execute(v + ".play()");
                    }
                    else
                    if (_siteUtil is IWebViewSiteUtil)
                    {
                        ((IWebViewSiteUtil)_siteUtil).DoPlay();
                    }
                }
                if (disableVMRWhenRunning)
                    GUIGraphicsContext.Vmr9Active = playState==PlayState.Playing;
            }
        }

        public override void Stop()
        {
            playState = PlayState.Ended;
            if (webView != null)
                DoDispose();
            GUIWindowManager.OnNewAction -= GUIWindowManager_OnNewAction;

            if (disableVMRWhenRunning)
                GUIGraphicsContext.Vmr9Active = false;
        }

        public override bool Initializing
        {
            get { return playState == PlayState.Init; }
        }

        public override bool Paused
        {
            get { return playState == PlayState.Paused; }
        }

        public override bool Playing
        {
            get { return !Ended; }
        }

        public override bool Stopped
        {
            get { return Initializing || Ended; }
        }

        public override bool Ended
        {
            get { return playState == PlayState.Ended; }
        }

        public override bool HasVideo
        {
            get { return true; }
        }

        public override bool HasViz
        {
            get { return true; }
        }

        private void SetCurrentPos(double dTime)
        {
            System.Globalization.NumberFormatInfo nfi = new System.Globalization.NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";
            var v = GetVideoElementSelector;
            if (!String.IsNullOrEmpty(v))
            {
                wvHelper.ExecuteFunc(v + ".currentTime=" + dTime.ToString(nfi));
            }
        }

        public override void SeekRelative(double dTime)
        {
            if (_siteUtil is IWebViewHTMLMediaElement && playState != PlayState.Init)
            {
                dTime = CurrentPosition + dTime;
                if (dTime < 0.0d) dTime = 0.0d;
                if (dTime < Duration)
                {
                    SetCurrentPos(dTime);
                }
            }
        }

        public override void SeekAbsolute(double dTime)
        {
            if (_siteUtil is IWebViewHTMLMediaElement && playState != PlayState.Init)
            {
                if (dTime < 0.0d) dTime = 0.0d;
                if (dTime < Duration)
                {
                    SetCurrentPos(dTime);
                }
            }
        }

        public override void SeekRelativePercentage(int iPercentage)
        {
            if (_siteUtil is IWebViewHTMLMediaElement && playState != PlayState.Init)
            {
                double d = Duration;
                double fCurPercent = (CurrentPosition / d) * 100.0d;
                double fOnePercent = d / 100.0d;
                fCurPercent = fCurPercent + (double)iPercentage;
                fCurPercent *= fOnePercent;
                if (fCurPercent < 0.0d) fCurPercent = 0.0d;
                if (fCurPercent < d)
                {
                    SetCurrentPos(fCurPercent);
                }
            }
        }

        public override void SeekAsolutePercentage(int iPercentage)
        {
            if (_siteUtil is IWebViewHTMLMediaElement && playState != PlayState.Init)
            {
                if (iPercentage < 0) iPercentage = 0;
                else if (iPercentage >= 100) iPercentage = 100;
                double fPercent = Duration / 100.0f;
                fPercent *= (double)iPercentage;
                SetCurrentPos(fPercent);
            }
        }

        private string GetVideoElementSelector
        {
            get
            {
                if (_siteUtil is IWebViewHTMLMediaElement)
                    return ((IWebViewHTMLMediaElement)_siteUtil).VideoElementSelector;
                return null;
            }
        }

        public override double Duration
        {
            get
            {
                var v = GetVideoElementSelector;
                if (!String.IsNullOrEmpty(v))
                {
                    var bb = wvHelper.ExecuteFunc(v + @".duration");
                    float res;
                    if (float.TryParse(bb, out res)) return res;
                }
                return 0;
            }
        }
        public override double CurrentPosition
        {
            get
            {
                var v = GetVideoElementSelector;
                if (!String.IsNullOrEmpty(v))
                {
                    var bb = wvHelper.ExecuteFunc(v + @".currentTime");
                    float res;
                    if (float.TryParse(bb, out res)) return res;
                }
                return 0;
            }
        }

        private void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
                OnlineVideos.Log.Error("Error initializing webview: {0}", e.InitializationException.Message);
            else
            {
                if (GoFullscreen) GUIWindowManager.ActivateWindow(GUIOnlineVideoFullscreen.WINDOW_FULLSCREEN_ONLINEVIDEO);
            }
        }

        private void WebView_FirstNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (disableVMRWhenRunning)
                GUIGraphicsContext.Vmr9Active = true;
            webView.Visible = true;
            GUIWaitCursor.Hide(); // hide the wait cursor
            playState = PlayState.Playing;
            _siteUtil.OnInitialized();
            webView.NavigationCompleted -= WebView_FirstNavigationCompleted;
            webView.NavigationCompleted += WebView_FurtherNavigationCompleted;
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


}

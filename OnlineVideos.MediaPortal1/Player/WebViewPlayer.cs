using System;
using System.Drawing;
using System.IO;
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
        private bool disableVMRWhenRunning = true;
        private IWebViewSiteUtil _siteUtil;
        private WebViewHelper wvHelper;

        private WebView2 webView;

        private PlayState playState;
        public string SubtitleFile { get; set; }
        public string PlaybackUrl { get; set; }
        public bool GoFullscreen { get; set; }

        public WebViewPlayer(IWebViewSiteUtil siteUtil)
        {
            _siteUtil = siteUtil;
        }

        private void DoDispose()
        {
            GUIGraphicsContext.form.Controls.Remove(webView);
            webView.Dispose();
            webView = null;
        }
        public override void Dispose()
        {
            if (webView != null)
                DoDispose();
        }

        public override bool Play(string strFile)
        {

            String cacheFolder = Path.Combine(Path.GetTempPath(), "WebViewplayer");
            webView = new WebView2();

            webView.CreationProperties = new CoreWebView2CreationProperties() { UserDataFolder = cacheFolder };
            webView.Location = new Point(0, 0);
            webView.Size = GUIGraphicsContext.form.Size;
            webView.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom |
                             System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            webView.Name = "webview";
            webView.Visible = false;
            webView.Enabled = false;
            webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
            playState = PlayState.Init;
            wvHelper = new WebViewHelper(webView);

            webView.NavigationCompleted += WebView_FirstNavigationCompleted;
            GUIGraphicsContext.form.Controls.Add(webView);
            GUIWaitCursor.Init(); GUIWaitCursor.Show(); // init and show the wait cursor while buffering

            webView.Source = new Uri(strFile);

            return true;
        }

        public override void Pause()
        {
            if (webView != null && playState == PlayState.Playing || playState == PlayState.Paused)
            {
                if (playState == PlayState.Playing)
                {
                    playState = PlayState.Paused;
                    _siteUtil.DoPause(wvHelper);
                }
                else
                {
                    playState = PlayState.Playing;
                    _siteUtil.DoPlay(wvHelper);
                }
            }
        }

        public override void Stop()
        {
            playState = PlayState.Ended;
            if (webView != null)
                DoDispose();

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

        private void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
                Log.Instance.Error("Error initializing webview: {0}", e.InitializationException.Message);
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
            _siteUtil.OnInitialized(wvHelper);
            webView.NavigationCompleted -= WebView_FirstNavigationCompleted;
            webView.NavigationCompleted += WebView_FurtherNavigationCompleted;
        }

        private void WebView_FurtherNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            _siteUtil.OnPageLoaded(wvHelper);
        }
    }


}

using OnlineVideos.Helpers;

namespace OnlineVideos.Sites
{
    public interface IWebViewSiteUtil
    {
        void OnInitialized(WebViewHelper webViewHelper);
        void OnPageLoaded(WebViewHelper webViewHelper, ref bool doStopPlayback);
        void DoPause(WebViewHelper webViewHelper);
        void DoPlay(WebViewHelper webViewHelper);

        void SetWebviewHelper(WebViewHelper webViewHelper);
    }
}

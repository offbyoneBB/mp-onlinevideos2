using OnlineVideos.Helpers;

namespace OnlineVideos.Sites
{
    public interface IWebViewSiteUtil
    {
        void OnInitialized(WebViewHelper webViewHelper);
        void DoPause(WebViewHelper webViewHelper);
        void DoPlay(WebViewHelper webViewHelper);
    }
}

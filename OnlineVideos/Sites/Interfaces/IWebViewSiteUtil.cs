using OnlineVideos.Helpers;

namespace OnlineVideos.Sites
{
    public interface IWebViewSiteUtilBase
    {
        void OnInitialized();
        void SetWebviewHelper(WebViewHelper webViewHelper);
    }

    public interface IWebViewSiteUtil : IWebViewSiteUtilBase
    {
        void OnPageLoaded(ref bool doStopPlayback);
        void DoPause();
        void DoPlay();
    }
    public interface IWebViewHTMLMediaElement : IWebViewSiteUtilBase
    {
        string VideoElementSelector { get; }
    }
}

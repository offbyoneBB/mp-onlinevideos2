using System;
using Microsoft.Web.WebView2.WinForms;

namespace OnlineVideos.Helpers
{
    public class WebViewHelper : MarshalByRefObject
    {
        public WebView2 webView;
        public WebViewHelper(WebView2 aWebView)
        {
            webView = aWebView;
        }

        public void execute(string js)
        {
            exec(js);
        }
        private async void exec(string js)
        {
            await webView.ExecuteScriptAsync(js);
        }
    }
}

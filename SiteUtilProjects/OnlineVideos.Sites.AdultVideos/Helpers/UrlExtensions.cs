using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace OnlineVideos.Sites.Helpers
{
    public static class UrlExtensions
    {
        public static string CombineUrl(this string baseUrl, string relativeUrl)
        {
            var uriResult = new Uri(new Uri(baseUrl.DecodeUrlString()), relativeUrl.DecodeUrlString());
            return uriResult.ToString().DecodeUrlString();
        }

        private static string DecodeUrlString(this string url)
        {
            string newUrl;
            while ((newUrl = Uri.UnescapeDataString(url.DecodeHtmlString())) != url)
                url = newUrl;
            return newUrl;
        }

        private static string DecodeHtmlString(this string html)
        {
            return HttpUtility.HtmlDecode(html);
        }
    }
}

using System;

namespace OnlineVideos
{
    public static class CookieHelper
    {
        [System.Runtime.InteropServices.DllImport("wininet.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        public static extern bool InternetGetCookie(string url, string cookieName, System.Text.StringBuilder cookieData, ref int size);

        [System.Runtime.InteropServices.DllImport("wininet.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        public static extern bool InternetSetCookie(string lpszUrlName, string lpszCookieName, string lpszCookieData);

        [System.Runtime.InteropServices.DllImport("wininet.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        public static extern bool InternetSetOption(int hInternet, int dwOption, string lpBuffer, int dwBufferLength);

        public static bool SetIECookie(string url, System.Net.Cookie cookie)
        {
            // set all IE requests in one process -> FileSource (URL) filter will then send those cookies on request
            InternetSetOption(0, 42, null, 0);
            // set the cookie for IE
            return InternetSetCookie(url, cookie.Name, cookie.Value);
        }

        public static System.Net.CookieContainer GetIECookies(Uri uri)
        {
            System.Net.CookieContainer cookies = null;

            // Determine the size of the cookie
            int datasize = 256;
            System.Text.StringBuilder cookieData = new System.Text.StringBuilder(datasize);

            if (!InternetGetCookie(uri.ToString(), null, cookieData, ref datasize))
            {
                if (datasize < 0) return null;

                // Allocate stringbuilder large enough to hold the cookie
                cookieData = new System.Text.StringBuilder(datasize);
                if (!InternetGetCookie(uri.ToString(), null, cookieData, ref datasize)) return null;
            }

            if (cookieData.Length > 0)
            {
                cookies = new System.Net.CookieContainer();
                cookies.SetCookies(uri, cookieData.ToString().Replace(';', ','));
            }
            return cookies;
        }
    }
}

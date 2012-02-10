using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace OnlineVideos.Sites.georgius
{
    public static class Utils
    {
        public static String FormatAbsoluteUrl(String relativeUrl, String baseUrl)
        {
            if (relativeUrl.Contains("http"))
            {
                return relativeUrl;
            }

            int askIndex = baseUrl.IndexOf('?');
            if (askIndex >= 0)
            {
                baseUrl = baseUrl.Substring(0, askIndex);
            }

            if (relativeUrl.StartsWith("/"))
            {
                Uri baseUri = new Uri(baseUrl);
                return String.Format("{0}{1}", baseUri.GetLeftPart(UriPartial.Authority), relativeUrl);
            }
            else
            {
                if (!baseUrl.EndsWith("/"))
                {
                    baseUrl = String.Format("{0}/", baseUrl);
                }

                Uri baseUri = new Uri(baseUrl);
                return String.Format("{0}{1}", baseUri.GetLeftPart(UriPartial.Path), relativeUrl);
            }            
        }

        public static string GetWebDataFromPost(string url, string postData, CookieContainer cc = null, string referer = null, IWebProxy proxy = null, bool forceUTF8 = false, string userAgent = null, Encoding encoding = null)
        {
            try
            {
                Log.Debug("GetWebDataFromPost: '{0}'", url);

                // request the data
                byte[] data = encoding != null ? encoding.GetBytes(postData) : Encoding.UTF8.GetBytes(postData);

                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                if (request == null) return "";
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                if (!String.IsNullOrEmpty(userAgent))
                    request.UserAgent = userAgent;
                else
                    request.UserAgent = OnlineVideoSettings.Instance.UserAgent;
                request.ContentLength = data.Length;
                request.ProtocolVersion = HttpVersion.Version10;
                request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
                if (!String.IsNullOrEmpty(referer)) request.Referer = referer;
                if (cc != null) request.CookieContainer = cc;
                if (proxy != null) request.Proxy = proxy;
                
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (cc != null)
                    {
                        cc.SetCookies(new Uri(url), response.Headers[HttpResponseHeader.SetCookie]);
                    }
                    Stream responseStream;
                    if (response.ContentEncoding.ToLower().Contains("gzip"))
                        responseStream = new System.IO.Compression.GZipStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                    else if (response.ContentEncoding.ToLower().Contains("deflate"))
                        responseStream = new System.IO.Compression.DeflateStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                    else
                        responseStream = response.GetResponseStream();

                    // UTF8 is the default encoding as fallback
                    Encoding responseEncoding = Encoding.UTF8;
                    // try to get the response encoding if one was specified and neither forceUTF8 nor encoding were set as parameters
                    if (!forceUTF8 && encoding == null && !String.IsNullOrEmpty(response.CharacterSet.Trim())) responseEncoding = Encoding.GetEncoding(response.CharacterSet.Trim(new char[] { ' ', '"' }));
                    // the caller did specify a forced encoding
                    if (encoding != null) responseEncoding = encoding;
                    // the caller wants to force UTF8
                    if (forceUTF8) responseEncoding = Encoding.UTF8;

                    using (StreamReader reader = new StreamReader(responseStream, responseEncoding, true))
                    {
                        string str = reader.ReadToEnd();
                        return str.Trim();
                    }
                }
            }
            finally
            {
            }
        }
    }
}

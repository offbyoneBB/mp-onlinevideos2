using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace OnlineVideos.Sites.Utils.NaviX
{
    class NaviXWebRequest
    {
        public NaviXWebRequest(string url, string referer = null, string cookies = null, string method = "get", string agent = null, string postData = "", Dictionary<string, string> headers = null)
        {
            this.RequestUrl = url;
            if (!string.IsNullOrEmpty(referer))
                Referer = referer;
            if (!string.IsNullOrEmpty(cookies))
                RequestCookies = cookies;
            if (!string.IsNullOrEmpty(method))
                Method = method;
            if (!string.IsNullOrEmpty(agent))
                UserAgent = agent;
            if (!string.IsNullOrEmpty(postData))
                PostData = postData;
            if (headers != null)
                RequestHeaders = headers;
        }

        public bool GetWebData()
        {
            if (string.IsNullOrEmpty(RequestUrl))
                return false;
            try
            {
                Uri uri = new Uri(RequestUrl);
                HttpWebRequest req = WebRequest.Create(uri) as HttpWebRequest;
                if (req == null)
                    return false;

                if (!string.IsNullOrEmpty(Referer))
                    req.Referer = Referer;

                CookieContainer cookieJar = new CookieContainer();
                if (!string.IsNullOrEmpty(RequestCookies))
                    foreach (string cook in RequestCookies.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string[] cookKeyVal = cook.Split('=');
                        if (cookKeyVal.Length > 1)
                            cookieJar.Add(new Cookie(cookKeyVal[0].Trim(), cookKeyVal[1].Trim(), uri.AbsolutePath, uri.Host));
                    }
                req.CookieContainer = cookieJar;

                if (!string.IsNullOrEmpty(UserAgent))
                    req.UserAgent = UserAgent;

                req.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");

                if (RequestHeaders != null)
                    foreach (string header in RequestHeaders.Keys)
                        req.Headers[header] = RequestHeaders[header];

                if (Method != null && Method.ToLower() == "post")
                {
                    req.Method = "POST";
                    req.ContentType = "application/x-www-form-urlencoded";
                    byte[] data = Encoding.UTF8.GetBytes(PostData);
                    req.ContentLength = data.Length;

                    System.IO.Stream requestStream = req.GetRequestStream();
                    requestStream.Write(data, 0, data.Length);
                    requestStream.Close();
                }

                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                {
                    if (req.RequestUri.Equals(res.ResponseUri))
                    {
                        if (res.ContentLength > 0 && res.ContentLength < 1024)
                        {
                            getUrl = res.ResponseUri.ToString();
                        }
                        else
                            getUrl = RequestUrl;
                    }
                    else
                        getUrl = res.ResponseUri.OriginalString;

                    System.IO.Stream responseStream;
                    if (res.ContentEncoding.ToLower().Contains("gzip"))
                        responseStream = new System.IO.Compression.GZipStream(res.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                    else if (res.ContentEncoding.ToLower().Contains("deflate"))
                        responseStream = new System.IO.Compression.DeflateStream(res.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                    else
                        responseStream = res.GetResponseStream();

                    headers = new Dictionary<string, string>();
                    foreach (string s in res.Headers.AllKeys)
                        headers[s] = res.Headers[s];

                    cookies = new Dictionary<string, string>();
                    foreach (Cookie cookie in res.Cookies)
                        cookies[cookie.Name] = cookie.Value;

                    Encoding responseEncoding = Encoding.UTF8;
                    if (!String.IsNullOrEmpty(res.CharacterSet.Trim()))
                        responseEncoding = Encoding.GetEncoding(res.CharacterSet.Trim(new char[] { ' ', '"' }));

                    using (System.IO.StreamReader reader = new System.IO.StreamReader(responseStream, responseEncoding, true))
                        content = reader.ReadToEnd().Trim();
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Warn("NaviXWebRequest: error - {0}\n{1}", ex.Message, ex.StackTrace);
                return false;
            }
        }

        Dictionary<string, string> headers = null;
        public Dictionary<string, string> Headers 
        { 
            get { return headers; } 
        }

        Dictionary<string, string> cookies = null;
        public Dictionary<string, string> Cookies 
        { 
            get { return cookies; } 
        }

        string content = null;
        public string Content
        {
            get
            {
                if (content == null)
                    return "";
                else
                    return content;
            }
        }

        string getUrl = null;
        public string GetURL
        {
            get
            {
                if (getUrl == null)
                    return "";
                else
                    return getUrl;
            }
        }

        public string RequestUrl { get; set; }

        public string Referer { get; set; }

        public string RequestCookies { get; set; }

        public string Method { get; set; }

        public string UserAgent { get; set; }

        public string PostData { get; set; }

        public Dictionary<string, string> RequestHeaders { get; set; }
    }
}

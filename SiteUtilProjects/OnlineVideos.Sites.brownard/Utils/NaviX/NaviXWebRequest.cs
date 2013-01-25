using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace OnlineVideos.Sites.Utils.NaviX
{
    class NaviXWebRequest
    {
        public NaviXWebRequest(string url)
        {
            this.RequestUrl = url;
        }

        public bool GetWebData()
        {
            if (string.IsNullOrEmpty(RequestUrl))
                return false;

            Log.Debug("NaviX: Processor: Web Request");
            Log.Debug("NaviX: Processor:\t Action: {0}", Action);
            Log.Debug("NaviX: Processor:\t Url: {0}", RequestUrl);
            Log.Debug("NaviX: Processor:\t Referer: {0}", Referer);
            Log.Debug("NaviX: Processor:\t Cookies: {0}", RequestCookies);
            Log.Debug("NaviX: Processor:\t Method: {0}", Method);
            Log.Debug("NaviX: Processor:\t Useragent: {0}", UserAgent);
            Log.Debug("NaviX: Processor:\t Post Data: {0}", PostData);

            responseHeaders = new Dictionary<string, string>();
            responseCookies = new Dictionary<string, string>();
            
            try
            {
                Uri uri = new Uri(RequestUrl);
                HttpWebRequest request = WebRequest.Create(uri) as HttpWebRequest;
                if (request == null)
                    return false;

                if (!string.IsNullOrEmpty(Referer)) request.Referer = Referer;
                if (!string.IsNullOrEmpty(UserAgent)) request.UserAgent = UserAgent;

                request.CookieContainer = new CookieContainer();
                if (!string.IsNullOrEmpty(RequestCookies))
                {
                    foreach (string cookie in RequestCookies.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string[] cookKeyVal = cookie.Split('=');
                        if (cookKeyVal.Length > 1)
                            request.CookieContainer.Add(new Cookie(cookKeyVal[0].Trim(), cookKeyVal[1].Trim(), uri.AbsolutePath, uri.Host));
                    }
                }

                request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
                if (RequestHeaders != null)
                    foreach (string header in RequestHeaders.Keys)
                        request.Headers[header] = RequestHeaders[header];

                if (Action != null && Action.ToLower() == "headers")
                    request.Method = "HEAD";
                else if (Method != null && Method.ToLower() == "post")
                {
                    request.Method = "POST";
                    request.ContentType = "application/x-www-form-urlencoded";
                    byte[] data = Encoding.UTF8.GetBytes(PostData);
                    request.ContentLength = data.Length;
                    using (System.IO.Stream requestStream = request.GetRequestStream())
                        requestStream.Write(data, 0, data.Length);
                }

                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    if (request.RequestUri.Equals(response.ResponseUri))
                    {
                        if (response.ContentLength > 0 && response.ContentLength < 1024)
                        {
                            getUrl = response.ResponseUri.ToString();
                        }
                        else
                            getUrl = RequestUrl;
                    }
                    else
                        getUrl = response.ResponseUri.OriginalString;

                    System.IO.Stream responseStream;
                    if (response.ContentEncoding.ToLower().Contains("gzip"))
                        responseStream = new System.IO.Compression.GZipStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                    else if (response.ContentEncoding.ToLower().Contains("deflate"))
                        responseStream = new System.IO.Compression.DeflateStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                    else
                        responseStream = response.GetResponseStream();

                    foreach (string s in response.Headers.AllKeys)
                        responseHeaders[s] = response.Headers[s];
                    foreach (Cookie cookie in response.Cookies)
                        responseCookies[cookie.Name] = cookie.Value;

                    Encoding responseEncoding = Encoding.UTF8;
                    if (!String.IsNullOrEmpty(response.CharacterSet.Trim()))
                        responseEncoding = Encoding.GetEncoding(response.CharacterSet.Trim(new char[] { ' ', '"' }));

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

        Dictionary<string, string> responseHeaders = null;
        public Dictionary<string, string> ResponseHeaders 
        { 
            get { return responseHeaders; } 
        }

        Dictionary<string, string> responseCookies = null;
        public Dictionary<string, string> ResponseCookies 
        { 
            get { return responseCookies; } 
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

        public string Action { get; set; }

        public string Referer { get; set; }

        public string RequestCookies { get; set; }

        public string Method { get; set; }

        public string UserAgent { get; set; }

        public string PostData { get; set; }

        public Dictionary<string, string> RequestHeaders { get; set; }
    }
}

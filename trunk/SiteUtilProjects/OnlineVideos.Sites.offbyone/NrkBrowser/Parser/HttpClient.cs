using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Vattenmelon.Nrk.Parser.Http
{
    public class HttpClient : IHttpClient
    {
        private CookieContainer cookieContainer;
        private string userAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.0)";
        //"Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.5) Gecko/20091102 Firefox/3.5.5 (.NET CLR 3.5.30729)"

        public HttpClient(CookieContainer container)
        {
            cookieContainer = container;
        }

        /// <summary>
        /// Fetches the url by using the HTTP GET method
        /// </summary>
        /// <param name="url">The url to retrieve</param>
        /// <returns></returns>
        public string GetUrl(string url)
        {
            HttpWebRequest request = CreateRequest(url);

            return HandleResponse(request);
        }

        private HttpWebRequest CreateRequest(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = userAgent;
            request.CookieContainer = cookieContainer;
            // Set some reasonable limits on resources used by this request
            request.MaximumAutomaticRedirections = 4;
            request.MaximumResponseHeadersLength = 4;
            // Set credentials to use for this request.
            request.Credentials = CredentialCache.DefaultCredentials;
            return request;
        }

        public string PostUrl(string url, string postData)
        {

            HttpWebRequest request = CreateRequest(url);

            ///Post-specific stuff
            request.Method = WebRequestMethods.Http.Post;
            UTF8Encoding encoding = new UTF8Encoding();
            byte[] postDataAsByteArray = encoding.GetBytes(postData);
            request.Headers.Add("X-MicrosoftAjax", "Delta=true");
            request.Headers.Add("Pragma", "no-cache");
            request.KeepAlive = true;
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            request.Headers.Set("Accept-Encoding", "gzip, deflate");
            request.Headers.Add(HttpRequestHeader.CacheControl, "no-cache");
            request.Referer = url;
            request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            request.ContentLength = postDataAsByteArray.Length;
            Stream newStream = request.GetRequestStream();
            newStream.Write(postDataAsByteArray, 0, postDataAsByteArray.Length);
            newStream.Close();
            
            return HandleResponse(request);
        }

        private static string HandleResponse(HttpWebRequest request)
        {
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            // Get the stream associated with the response.
            System.IO.Stream receiveStream = response.GetResponseStream();
            // Pipes the stream to a higher level stream reader with the required encoding format. 
            StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
            string ret = readStream.ReadToEnd();
            response.Close();
            readStream.Close();
            return ret;
        }
    }
}

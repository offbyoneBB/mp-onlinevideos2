﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using OnlineVideos._3rdParty.Newtonsoft.Json.Linq;

namespace OnlineVideos.Sites.HboNordic
{
    internal class HboWebCache
    {
        class WebCacheEntry
        {
            public DateTime LastUpdated { get; set; }
            public string Data { get; set; }
        }

        #region Singleton
        HboWebCache() 
        {
            // only use cache if a timeout > 0 was set
            if (OnlineVideoSettings.Instance.CacheTimeout > 0)
            {
                cleanUpTimer = new Timer(CleanCache, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
            }
        }
        static HboWebCache instance;
        public static HboWebCache Instance { get { if (instance == null) instance = new HboWebCache(); return instance; } }
        #endregion

        Timer cleanUpTimer;
        Dictionary<string, WebCacheEntry> cache = new Dictionary<string, WebCacheEntry>();

        public string this[string url]
        {
            get
            {
                if (OnlineVideoSettings.Instance.CacheTimeout > 0) // only use cache if a timeout > 0 was set
                {
                    lock (this)
                    {
                        WebCacheEntry result = null;
                        if (cache.TryGetValue(url, out result))
                        {
                            return result.Data;
                        }
                    }
                }
                return null;
            }
            set
            {
                if (OnlineVideoSettings.Instance.CacheTimeout > 0) // only use cache if a timeout > 0 was set
                {
                    lock (this)
                    {
                        cache[url] = new WebCacheEntry() { Data = value, LastUpdated = DateTime.Now };
                    }
                }
            }
        }

        void CleanCache(object state)
        {
            lock (this)
            {
                List<string> outdatedKeys = new List<string>();

                foreach (string key in cache.Keys)
                    if ((DateTime.Now - cache[key].LastUpdated).TotalMinutes >= OnlineVideoSettings.Instance.CacheTimeout)
                        outdatedKeys.Add(key);

                foreach (string key in outdatedKeys) cache.Remove(key);
            }
        }

        public T GetWebData<T>(string url, string postData = null, CookieContainer cookies = null, string referer = null, IWebProxy proxy = null, bool forceUTF8 = false, bool allowUnsafeHeader = false, string userAgent = null, Encoding encoding = null, NameValueCollection headers = null, bool cache = true, string requestMethod = null, string contentType = null)
        {
            string webData = GetWebData(url, postData, cookies, referer, proxy, forceUTF8, allowUnsafeHeader, userAgent, encoding, headers, cache, requestMethod, contentType);
            if (typeof(T) == typeof(string))
            {
                return (T)(object)webData;
            }
            else if (typeof(T) == typeof(JToken))
            {
                return (T)(object)JToken.Parse(webData);
            }
            else if (typeof(T) == typeof(JObject))
            {
                return (T)(object)JObject.Parse(webData);
            }
            else if (typeof(T) == typeof(RssToolkit.Rss.RssDocument))
            {
                return (T)(object)RssToolkit.Rss.RssDocument.Load(webData);
            }
            else if (typeof(T) == typeof(System.Xml.XmlDocument))
            {
                var xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.LoadXml(webData);
                return (T)(object)xmlDoc;
            }
            else if (typeof(T) == typeof(System.Xml.Linq.XDocument))
            {
                return (T)(object)System.Xml.Linq.XDocument.Parse(webData);
            }
            else if (typeof(T) == typeof(HtmlAgilityPack.HtmlDocument))
            {
                HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(webData);
                return (T)(object)htmlDoc;
            }

            return default(T);
        }

        public string GetWebData(string url, string postData = null, CookieContainer cookies = null, string referer = null, IWebProxy proxy = null, bool forceUTF8 = false, bool allowUnsafeHeader = false, string userAgent = null, Encoding encoding = null, NameValueCollection headers = null, bool cache = true, string requestMethod = null, string contentType = null)
        {
            requestMethod = string.IsNullOrEmpty(requestMethod) ? ((postData == null) ? "GET" : "POST") : requestMethod;
            // do not use the cache when not doing a GET
            if (requestMethod != "GET") cache = false;
            // set a few headers if none were given
            if (headers == null)
            {
                headers = new NameValueCollection();
                headers.Add("Accept", "*/*"); // accept any content type
                headers.Add("User-Agent", userAgent ?? OnlineVideoSettings.Instance.UserAgent); // set the default OnlineVideos UserAgent when none specified
            }
            if (referer != null) headers.Set("Referer", referer);
            HttpWebResponse response = null;
            try
            {
                // build a CRC of the url and all headers + proxy + cookies for caching
                string requestCRC = Helpers.EncryptionUtils.CalculateCRC32(
                    string.Format("{0}{1}{2}{3}",
                    url,
                    headers != null ? string.Join("&", (from item in headers.AllKeys select string.Format("{0}={1}", item, headers[item])).ToArray()) : "",
                    proxy != null ? proxy.GetProxy(new Uri(url)).AbsoluteUri : "",
                    cookies != null ? cookies.GetCookieHeader(new Uri(url)) : ""));

                // try cache first
                string cachedData = cache ? WebCache.Instance[requestCRC] : null;
                Log.Debug("GetWebData-{2}{1}: '{0}'", url, cachedData != null ? " (cached)" : "", requestMethod);
                if (cachedData != null) return cachedData;

                // build the request
                if (allowUnsafeHeader) Helpers.DotNetFrameworkHelper.SetAllowUnsafeHeaderParsing(true);
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                if (request == null) return "";
                request.Method = requestMethod;
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate; // turn on automatic decompression of both formats (adds header "AcceptEncoding: gzip,deflate" to the request)
                if (cookies != null) request.CookieContainer = cookies; // set cookies if given
                if (proxy != null) request.Proxy = proxy; // send the request over a proxy if given
                if (headers != null) // set user defined headers
                {
                    foreach (var headerName in headers.AllKeys)
                    {
                        switch (headerName.ToLowerInvariant())
                        {
                            case "accept":
                                request.Accept = headers[headerName];
                                break;
                            case "user-agent":
                                request.UserAgent = headers[headerName];
                                break;
                            case "referer":
                                request.Referer = headers[headerName];
                                break;
                            default:
                                request.Headers.Set(headerName, headers[headerName]);
                                break;
                        }
                    }
                }
                if (postData != null)
                {
                    contentType = string.IsNullOrEmpty(contentType) ? "application/x-www-form-urlencoded" : contentType;
                    byte[] data = encoding != null ? encoding.GetBytes(postData) : Encoding.UTF8.GetBytes(postData);
                    request.ContentLength = data.Length;
                    request.ProtocolVersion = HttpVersion.Version10;
                    Stream requestStream = request.GetRequestStream();
                    requestStream.Write(data, 0, data.Length);
                    requestStream.Close();
                }
                if (!string.IsNullOrEmpty(contentType))
                {
                    request.ContentType = contentType;
                }
                // request the data
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch (WebException webEx)
                {
                    Log.Debug(webEx.Message);
                    response = (HttpWebResponse)webEx.Response; // if the server returns a 404 or similar .net will throw a WebException that has the response
                }
                Stream responseStream = response.GetResponseStream();

                // UTF8 is the default encoding as fallback
                Encoding responseEncoding = Encoding.UTF8;
                // try to get the response encoding if one was specified and neither forceUTF8 nor encoding were set as parameters
                if (!forceUTF8 && encoding == null && response.CharacterSet != null && !String.IsNullOrEmpty(response.CharacterSet.Trim())) responseEncoding = Encoding.GetEncoding(response.CharacterSet.Trim(new char[] { ' ', '"' }));
                // the caller did specify a forced encoding
                if (encoding != null) responseEncoding = encoding;
                // the caller wants to force UTF8
                if (forceUTF8) responseEncoding = Encoding.UTF8;

                using (StreamReader reader = new StreamReader(responseStream, responseEncoding, true))
                {
                    string str = reader.ReadToEnd().Trim();
                    // add to cache if HTTP Status was 200 and we got more than 500 bytes (might just be an errorpage otherwise)
                    if (cache && response.StatusCode == HttpStatusCode.OK && str.Length > 500) WebCache.Instance[requestCRC] = str;
                    return str;
                }
            }
            finally
            {
                if (response != null) ((IDisposable)response).Dispose();
                // disable unsafe header parsing if it was enabled
                if (allowUnsafeHeader) Helpers.DotNetFrameworkHelper.SetAllowUnsafeHeaderParsing(false);
            }
        }

        public string GetRedirectedUrl(string url, string referer = null)
        {
            HttpWebResponse httpWebresponse = null;
            try
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                if (request == null) return url;
                request.UserAgent = OnlineVideoSettings.Instance.UserAgent;
                request.AllowAutoRedirect = true;
                request.Timeout = 15000;
                if (!string.IsNullOrEmpty(referer)) request.Referer = referer;
                // invoke getting the Response async and abort as soon as data is coming in 
                // (according to docs - this is after headers are completely received)
                var result = request.BeginGetResponse((ar) => request.Abort(), null);
                // wait for the completion (or abortion) of the async response
                while (!result.IsCompleted) System.Threading.Thread.Sleep(10);
                httpWebresponse = request.EndGetResponse(result) as HttpWebResponse;
                if (httpWebresponse == null) return url;
                if (request.RequestUri.Equals(httpWebresponse.ResponseUri))
                    return url;
                else
                    return httpWebresponse.ResponseUri.OriginalString;
            }
            catch (Exception ex)
            {
                Log.Warn(ex.ToString());
            }
            finally
            {
                if (httpWebresponse != null)
                {
                    try
                    {
                        httpWebresponse.Close();
                    }
                    catch (Exception ex)
                    {
                        Log.Warn(ex.ToString());
                    }
                }
            }
            return url;
        }
    }
}

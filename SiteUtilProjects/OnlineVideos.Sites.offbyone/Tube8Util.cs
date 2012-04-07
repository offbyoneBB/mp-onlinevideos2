using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;

namespace OnlineVideos.Sites
{
    public class Tube8Util : GenericSiteUtil
    {
        string referer;

        public override string getPlaylistUrl(string resultUrl)
        {
            referer = resultUrl;
            if (regEx_PlaylistUrl != null)
            {
                string dataPage = GetWebData(resultUrl, GetCookie(), forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride);
                Match matchPlaylistUrl = regEx_PlaylistUrl.Match(dataPage);
                if (matchPlaylistUrl.Success)
                {
                    return string.Format(playlistUrlFormatString, matchPlaylistUrl.Groups["hash"].Value, matchPlaylistUrl.Groups["id"].Value);
                }
                else return string.Empty;
            }
            else
                return resultUrl;
        }

        public override Dictionary<string, string> GetPlaybackOptions(string playlistUrl)
        {
            string dataPage = GetWebData(playlistUrl, GetCookie(), referer: referer, forceUTF8: forceUTF8Encoding, allowUnsafeHeader: allowUnsafeHeaders, encoding: encodingOverride, additionalHeaders: new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("X-Requested-With", "XMLHttpRequest") });
            string url = Newtonsoft.Json.Linq.JObject.Parse(dataPage).Value<string>(fileUrlRegEx);
            Dictionary<string, string> playbackOptions = new Dictionary<string, string>();
            playbackOptions.Add("Standard", url);
            return playbackOptions;
        }
        public static string GetWebData(string url, CookieContainer cc = null, string referer = null, IWebProxy proxy = null, bool forceUTF8 = false, bool allowUnsafeHeader = false, string userAgent = null, Encoding encoding = null, List<KeyValuePair<string, string>> additionalHeaders = null)
        {
            HttpWebResponse response = null;
            try
            {
                string requestCRC = Utils.EncryptLine(string.Format("{0}{1}{2}{3}{4}", url, referer, userAgent, proxy != null ? proxy.GetProxy(new Uri(url)).AbsoluteUri : "", cc != null ? cc.GetCookieHeader(new Uri(url)) : ""));

                // try cache first
                string cachedData = WebCache.Instance[requestCRC];
                Log.Debug("GetWebData{1}: '{0}'", url, cachedData != null ? " (cached)" : "");
                if (cachedData != null) return cachedData;

                // request the data
                if (allowUnsafeHeader) Utils.SetAllowUnsafeHeaderParsing(true);
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                if (request == null) return "";
                if (!String.IsNullOrEmpty(userAgent))
                    request.UserAgent = userAgent; // set specific UserAgent if given
                else
                    request.UserAgent = OnlineVideoSettings.Instance.UserAgent; // set OnlineVideos default UserAgent
                request.Accept = "*/*"; // we accept any content type
                request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate"); // we accept compressed content
                if (!String.IsNullOrEmpty(referer)) request.Referer = referer; // set referer if given
                if (cc != null) request.CookieContainer = cc; // set cookies if given
                if (proxy != null) request.Proxy = proxy; // send the request over a proxy if given
                if (additionalHeaders != null) // add user defined headers
                {
                    foreach (var additionalheader in additionalHeaders)
                    {
                        request.Headers.Set(additionalheader.Key, additionalheader.Value);
                    }
                }
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch (WebException webEx)
                {
                    Log.Debug(webEx.Message);
                    response = (HttpWebResponse)webEx.Response; // if the server returns a 404 or similar .net will throw a WebException that has the response
                }
                Stream responseStream;
                if (response == null) return "";
                if (response.ContentEncoding.ToLower().Contains("gzip"))
                    responseStream = new System.IO.Compression.GZipStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                else if (response.ContentEncoding.ToLower().Contains("deflate"))
                    responseStream = new System.IO.Compression.DeflateStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                else
                    responseStream = response.GetResponseStream();

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
                    if (response.StatusCode == HttpStatusCode.OK && str.Length > 500) WebCache.Instance[requestCRC] = str;
                    return str;
                }
            }
            finally
            {
                if (response != null) ((IDisposable)response).Dispose();
                // disable unsafe header parsing if it was enabled
                if (allowUnsafeHeader) Utils.SetAllowUnsafeHeaderParsing(false);
            }
        }

    }
}

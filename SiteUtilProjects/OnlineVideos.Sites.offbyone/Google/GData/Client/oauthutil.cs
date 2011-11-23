/* Copyright (c) 2011 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 * 
*/

using System;
using System.Text;
using System.Globalization;
using System.Net;
using System.IO;
using System.Collections.Generic;

namespace Google.GData.Client {
    /// <summary>
    /// Provides a means to generate an OAuth signature suitable for use
    /// with Google OAuth requests.
    /// </summary>
    public class OAuthUtil {
        // Google OAuth endpoints
        private static String requestTokenUrl = "https://www.google.com/accounts/OAuthGetRequestToken";
        private static String userAuthorizationUrl = "https://www.google.com/accounts/OAuthAuthorizeToken";
        private static String accessTokenUrl = "https://www.google.com/accounts/OAuthGetAccessToken";

        /// <summary>
        /// Generates an OAuth header.
        /// </summary>
        /// <param name="uri">The URI of the request</param>
        /// <param name="consumerKey">The consumer key</param>
        /// <param name="consumerSecret">The consumer secret</param>
        /// <param name="httpMethod">The http method</param>
        /// <returns>The OAuth authorization header</returns>
        public static string GenerateHeader(Uri uri, String consumerKey, String consumerSecret, String httpMethod) {
            return GenerateHeader(uri, consumerKey, consumerSecret, string.Empty, string.Empty, httpMethod);
        }

        /// <summary>
        /// Generates an OAuth header.
        /// </summary>
        /// <param name="uri">The URI of the request</param>
        /// <param name="consumerKey">The consumer key</param>
        /// <param name="consumerSecret">The consumer secret</param>
        /// <param name="token">The OAuth token</param>
        /// <param name="tokenSecret">The OAuth token secret</param>
        /// <param name="httpMethod">The http method</param>
        /// <returns>The OAuth authorization header</returns>
        public static string GenerateHeader(Uri uri, String consumerKey, String consumerSecret, String token,
            String tokenSecret, String httpMethod) {
            OAuthParameters parameters = new OAuthParameters() { 
                ConsumerKey = consumerKey, ConsumerSecret = consumerSecret, Token = token, TokenSecret = tokenSecret, SignatureMethod = OAuthBase.HMACSHA1SignatureType 
            };
            return GenerateHeader(uri, httpMethod, parameters);
        }

        /// <summary>
        /// Generates an OAuth header.
        /// </summary>
        /// <param name="uri">The URI of the request</param>
        /// <param name="httpMethod">The http method</param>
        /// <param name="parameters">The OAuth parameters</param>
        /// <returns>The OAuth authorization header</returns>
        public static string GenerateHeader(Uri uri, string httpMethod, OAuthParameters parameters) {
            parameters.Timestamp = OAuthBase.GenerateTimeStamp();
            parameters.Nonce = OAuthBase.GenerateNonce();

            string signature = OAuthBase.GenerateSignature(uri, httpMethod, parameters);

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Authorization: OAuth {0}=\"{1}\",", OAuthBase.OAuthVersionKey, OAuthBase.OAuthVersion);
            sb.AppendFormat("{0}=\"{1}\",", OAuthBase.OAuthNonceKey, OAuthBase.EncodingPerRFC3986(parameters.Nonce));
            sb.AppendFormat("{0}=\"{1}\",", OAuthBase.OAuthTimestampKey, OAuthBase.EncodingPerRFC3986(parameters.Timestamp));
            sb.AppendFormat("{0}=\"{1}\",", OAuthBase.OAuthConsumerKeyKey, OAuthBase.EncodingPerRFC3986(parameters.ConsumerKey));
            if (parameters.BaseProperties.ContainsKey(OAuthBase.OAuthVerifierKey)) {
                sb.AppendFormat("{0}=\"{1}\",", OAuthBase.OAuthVerifierKey, OAuthBase.EncodingPerRFC3986(parameters.BaseProperties[OAuthBase.OAuthVerifierKey]));
            }
            if (!String.IsNullOrEmpty(parameters.Token)) {
                sb.AppendFormat("{0}=\"{1}\",", OAuthBase.OAuthTokenKey, OAuthBase.EncodingPerRFC3986(parameters.Token));
            }
            if (parameters.BaseProperties.ContainsKey(OAuthBase.OAuthCallbackKey)) {
                sb.AppendFormat("{0}=\"{1}\",", OAuthBase.OAuthCallbackKey, OAuthBase.EncodingPerRFC3986(parameters.BaseProperties[OAuthBase.OAuthCallbackKey]));
            }
            sb.AppendFormat("{0}=\"{1}\",", OAuthBase.OAuthSignatureMethodKey, OAuthBase.HMACSHA1SignatureType);
            sb.AppendFormat("{0}=\"{1}\"", OAuthBase.OAuthSignatureKey, OAuthBase.EncodingPerRFC3986(signature));

            return sb.ToString();
        }

        /// <summary>
        /// Contacts Google for a request token, first step of the OAuth authentication process.
        /// When successful, updates the OAuthParameter instance passed as parameter by setting
        /// Token and TokenSecret.
        /// </summary>
        /// <param name="parameters">The OAuth parameters</param>
        public static void GetUnauthorizedRequestToken(OAuthParameters parameters) {
            Uri requestUri = new Uri(string.Format("{0}?scope={1}", requestTokenUrl, OAuthBase.EncodingPerRFC3986(parameters.Scope)));

            // callback is only needed when getting the request token
            bool callbackExists = false;
            if (!string.IsNullOrEmpty(parameters.Callback)) {
                parameters.BaseProperties.Add(OAuthBase.OAuthCallbackKey, parameters.Callback);
                callbackExists = true;
            }

            string headers = GenerateHeader(requestUri, "GET", parameters);
            WebRequest request = WebRequest.Create(requestUri);
            request.Headers.Add(headers);

            WebResponse response = request.GetResponse();
            string result = "";
            if (response != null) {
                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);
                result = reader.ReadToEnd();
            }

            if (callbackExists) {
                parameters.BaseProperties.Remove(OAuthBase.OAuthCallbackKey);
            }

            // split results and update parameters
            SortedDictionary<string, string> responseValues = OAuthBase.GetQueryParameters(result);
            parameters.Token = responseValues[OAuthBase.OAuthTokenKey];
            parameters.TokenSecret = responseValues[OAuthBase.OAuthTokenSecretKey];
        }

        /// <summary>
        /// Generates the url which the user should visit in order to authenticate and
        /// authorize with the Service Provider.
        /// When successful, updates the OAuthParameter instance passed as parameter by setting
        /// Token and TokenSecret.
        /// </summary>
        /// <param name="parameters">The OAuth parameters</param>
        /// <returns>The full authorization url the user should visit</returns>
        public static string CreateUserAuthorizationUrl(OAuthParameters parameters) {
            StringBuilder sb = new StringBuilder();
            sb.Append(userAuthorizationUrl);
            sb.AppendFormat("?{0}={1}", OAuthBase.OAuthTokenKey, OAuthBase.EncodingPerRFC3986(parameters.Token));
            if (!string.IsNullOrEmpty(parameters.Callback)) {
                sb.AppendFormat("&{0}={1}", OAuthBase.OAuthCallbackKey, OAuthBase.EncodingPerRFC3986(parameters.Callback));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Helper method which parses a querystring for the OAuth related parameters.
        /// It updates the OAuthParameter instance passed as parameter by setting
        /// Token, TokenSecret and Verifier (if present).
        /// </summary>
        /// <param name="parameters">The OAuth parameters</param>
        public static void UpdateOAuthParametersFromCallback(string queryString, OAuthParameters parameters) {
            //split results and update parameters
            SortedDictionary<string, string> responseValues = OAuthBase.GetQueryParameters(queryString);
            parameters.Token = responseValues[OAuthBase.OAuthTokenKey];
            if (responseValues.ContainsKey(OAuthBase.OAuthTokenSecretKey)) {
                parameters.TokenSecret = responseValues[OAuthBase.OAuthTokenSecretKey];
            }
            if (responseValues.ContainsKey(OAuthBase.OAuthVerifierKey)) {
                parameters.Verifier = responseValues[OAuthBase.OAuthVerifierKey];
            }
        }

        /// <summary>
        /// Exchanges the user-authorized request token for an access token.
        /// When successful, updates the OAuthParameter instance passed as parameter by setting
        /// Token and TokenSecret.
        /// </summary>
        /// <param name="parameters">The OAuth parameters</param>
        public static void GetAccessToken(OAuthParameters parameters) {
            Uri requestUri = new Uri(accessTokenUrl);

            string headers = GenerateHeader(requestUri, "GET", parameters);
            WebRequest request = WebRequest.Create(requestUri);
            request.Headers.Add(headers);

            WebResponse response = request.GetResponse();
            string result = "";
            if (response != null) {
                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);
                result = reader.ReadToEnd();
            }

            //split results and update parameters
            SortedDictionary<string, string> responseValues = OAuthBase.GetQueryParameters(result);
            parameters.Token = responseValues[OAuthBase.OAuthTokenKey];
            parameters.TokenSecret = responseValues[OAuthBase.OAuthTokenSecretKey];
        }
    }
}

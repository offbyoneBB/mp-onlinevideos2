/* Copyright (c) 2006 Google Inc.
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
*/
#region Using directives

#define USE_TRACING

using System;
using System.Security.Cryptography;
using System.Net;
using System.Text;
using System.Globalization;
using System.Collections.Generic;




#endregion

//////////////////////////////////////////////////////////////////////
// Contains AuthSubUtil, a helper class for authsub communications
//////////////////////////////////////////////////////////////////////
namespace Google.GData.Client
{
    //////////////////////////////////////////////////////////////////////
    /// <summary>helper class for communications between a 3rd party site and Google using the AuthSub protocol
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public sealed class AuthSubUtil
    {
        private static string DEFAULT_PROTOCOL = "https"; 
        private static string DEFAULT_DOMAIN = "www.google.com";
        private static string DEFAULT_HANDLER = "/accounts/AuthSubRequest";

        // to prevent the compiler from creating a default public one.
        private AuthSubUtil() { }

        //////////////////////////////////////////////////////////////////////
        /// <summary>Creates the request URL to be used to retrieve an AuthSub 
        /// token. On success, the user will be redirected to the continue URL 
        /// with the AuthSub token appended to the URL.
        /// Use getTokenFromReply(String) to retrieve the token from the reply.
        /// </summary> 
        /// <param name="continueUrl">the URL to redirect to on successful 
        /// token retrieval</param>
        /// <param name="scope">the scope of the requested AuthSub token</param>
        /// <param name="secure">if the token will be used securely</param>
        /// <param name="session"> if the token will be exchanged for a
        ///  session cookie</param>
        /// <returns>the URL to be used to retrieve the AuthSub token</returns>
        //////////////////////////////////////////////////////////////////////
        public static string getRequestUrl(string continueUrl,
                                           string scope,
                                           bool secure,
                                           bool session)
        {

            return getRequestUrl(DEFAULT_PROTOCOL, DEFAULT_DOMAIN, continueUrl, scope,
                                 secure, session);
        }



        //////////////////////////////////////////////////////////////////////
        /// <summary>Creates the request URL to be used to retrieve an AuthSub 
        /// token. On success, the user will be redirected to the continue URL 
        /// with the AuthSub token appended to the URL.
        /// Use getTokenFromReply(String) to retrieve the token from the reply.
        /// </summary> 
        /// <param name="hostedDomain">the name of the hosted domain, 
        /// like www.myexample.com</param>
        /// <param name="continueUrl">the URL to redirect to on successful 
        /// token retrieval</param>
        /// <param name="scope">the scope of the requested AuthSub token</param>
        /// <param name="secure">if the token will be used securely</param>
        /// <param name="session"> if the token will be exchanged for a
        ///  session cookie</param>
        /// <returns>the URL to be used to retrieve the AuthSub token</returns>
        //////////////////////////////////////////////////////////////////////
        public static string getRequestUrl(string hostedDomain,
                                           string continueUrl,
                                           string scope,
                                           bool secure,
                                           bool session)
        {

            return getRequestUrl(hostedDomain, DEFAULT_PROTOCOL, DEFAULT_DOMAIN, DEFAULT_HANDLER,
            					continueUrl, scope, secure, session);
        }



        //////////////////////////////////////////////////////////////////////
        /// <summary>Creates the request URL to be used to retrieve an AuthSub 
        /// token. On success, the user will be redirected to the continue URL 
        /// with the AuthSub token appended to the URL.
        /// Use getTokenFromReply(String) to retrieve the token from the reply.
        /// </summary> 
        /// <param name="protocol">the protocol to use to communicate with the 
        /// server</param>
        /// <param name="authenticationDomain">the domain at which the authentication server 
        /// exists</param>
        /// <param name="continueUrl">the URL to redirect to on successful 
        /// token retrieval</param>
        /// <param name="scope">the scope of the requested AuthSub token</param>
        /// <param name="secure">if the token will be used securely</param>
        /// <param name="session"> if the token will be exchanged for a
        ///  session cookie</param>
        /// <returns>the URL to be used to retrieve the AuthSub token</returns>
        //////////////////////////////////////////////////////////////////////
        public static string getRequestUrl(string protocol,
                                           string authenticationDomain,
                                           string continueUrl,
                                           string scope,
                                           bool secure,
                                           bool session)
        {
            return getRequestUrl(null, protocol, authenticationDomain, DEFAULT_HANDLER, 
            					continueUrl, scope, secure, session);
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>Creates the request URL to be used to retrieve an AuthSub 
        /// token. On success, the user will be redirected to the continue URL 
        /// with the AuthSub token appended to the URL.
        /// Use getTokenFromReply(String) to retrieve the token from the reply.
        /// </summary> 
        /// <param name="protocol">the protocol to use to communicate with the 
        /// server</param>
        /// <param name="authenticationDomain">the domain at which the authentication server 
        /// exists</param>
        /// <param name="handler">the location of the authentication handler
        ///  (defaults to "/accounts/AuthSubRequest".</param>
        /// <param name="continueUrl">the URL to redirect to on successful 
        /// token retrieval</param>
        /// <param name="scope">the scope of the requested AuthSub token</param>
        /// <param name="secure">if the token will be used securely</param>
        /// <param name="session"> if the token will be exchanged for a
        ///  session cookie</param>
        /// <returns>the URL to be used to retrieve the AuthSub token</returns>
        //////////////////////////////////////////////////////////////////////
        public static string getRequestUrl(string protocol,
                                           string authenticationDomain,
                                           string handler,
                                           string continueUrl,
                                           string scope,
                                           bool secure,
                                           bool session)
        {

            return getRequestUrl(null, protocol, authenticationDomain, handler, continueUrl, 
                scope, secure, session);
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>Creates the request URL to be used to retrieve an AuthSub 
        /// token. On success, the user will be redirected to the continue URL 
        /// with the AuthSub token appended to the URL.
        /// Use getTokenFromReply(String) to retrieve the token from the reply.
        /// </summary> 
        /// <param name="hostedDomain">the name of the hosted domain, 
        /// like www.myexample.com</param>
        /// <param name="protocol">the protocol to use to communicate with the 
        /// server</param>
        /// <param name="authenticationDomain">the domain at which the authentication server 
        /// exists</param>
        /// <param name="handler">the location of the authentication handler
        ///  (defaults to "/accounts/AuthSubRequest".</param>
        /// <param name="continueUrl">the URL to redirect to on successful 
        /// token retrieval</param>
        /// <param name="scope">the scope of the requested AuthSub token</param>
        /// <param name="secure">if the token will be used securely</param>
        /// <param name="session"> if the token will be exchanged for a
        ///  session cookie</param>
        /// <returns>the URL to be used to retrieve the AuthSub token</returns>
        //////////////////////////////////////////////////////////////////////
        public static string getRequestUrl(string hostedDomain,
                                           string protocol,
                                           string authenticationDomain,
                                           string handler,
                                           string continueUrl,
                                           string scope,
                                           bool secure,
                                           bool session)
        {

            StringBuilder url = new StringBuilder(protocol);
            url.Append("://");
            url.Append(authenticationDomain);
            url.Append(handler);
            url.Append("?");

            addParameter(url, "next", continueUrl);
            url.Append("&");
            addParameter(url, "scope", scope);
            url.Append("&");
            addParameter(url, "secure", secure ? "1" : "0");
            url.Append("&");
            addParameter(url, "session", session ? "1" : "0");
            if (hostedDomain != null)
            {
                url.Append("&");
                addParameter(url, "hd", hostedDomain);
            }
            return url.ToString();
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary> 
        /// Adds the query parameter with the given name and value to the URL.
        /// </summary> 
        //////////////////////////////////////////////////////////////////////
        private static void addParameter(StringBuilder url,
                                         string name,
                                         string value)
        {
            // encode them
            name = Utilities.UriEncodeReserved(name);
            value = Utilities.UriEncodeReserved(value);
            // Append the name/value pair
            url.Append(name);
            url.Append('='); 
            url.Append(value);
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Returns the URL to use to exchange the one-time-use token for
        ///  a session token.
        /// </summary> 
        /// <returns>the URL to exchange for the session token</returns>
        //////////////////////////////////////////////////////////////////////
        public static string getSessionTokenUrl()
        {
            return getSessionTokenUrl(DEFAULT_PROTOCOL, DEFAULT_DOMAIN);       
        }
        //end of public static string getSessionTokenUrl()


    
        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Returns the URL to use to exchange the one-time-use token for
        ///  a session token.
        /// </summary> 
        /// <param name="protocol">the protocol to use to communicate with
        /// the server</param>
        /// <param name="domain">the domain at which the authentication server 
        /// exists</param>
        /// <returns>the URL to exchange for the session token</returns>
        //////////////////////////////////////////////////////////////////////
        public static string getSessionTokenUrl(string protocol, string domain)
        {
            return protocol + "://" + domain + "/accounts/AuthSubSessionToken";  
        }
        //end of public static string getSessionTokenUrl()


       //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Returns the URL that handles token revocation, using the default
        /// domain and the default protocol
        /// </summary> 
        /// <returns>the URL to exchange for the session token</returns>
        //////////////////////////////////////////////////////////////////////
        public static string getRevokeTokenUrl() 
        {
            return getRevokeTokenUrl(DEFAULT_PROTOCOL, DEFAULT_DOMAIN);
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Returns the URL that handles token revocation.
        /// </summary> 
        /// <param name="protocol">the protocol to use to communicate with
        /// the server</param>
        /// <param name="domain">the domain at which the authentication server 
        /// exists</param>
        /// <returns>the URL to exchange for the session token</returns>
        //////////////////////////////////////////////////////////////////////
        public static string getRevokeTokenUrl(string protocol, string domain) 
        {
            return protocol + "://" + domain + "/accounts/AuthSubRevokeToken";
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>
        ///  Parses and returns the AuthSub token returned by Google on a successful
        ///  AuthSub login request.  The token will be appended as a query parameter
        /// to the continue URL specified while making the AuthSub request.
        /// </summary> 
        /// <param name="uri">The reply URI to parse </param>
        /// <returns>the token value of the URI, or null if none </returns>
        //////////////////////////////////////////////////////////////////////
        public static string getTokenFromReply(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            char [] deli = {'?','&'}; 
            TokenCollection tokens = new TokenCollection(uri.Query, deli); 
            foreach (String token in tokens )
            {
                if (token.Length > 0)
                {
                    char [] otherDeli = {'='};
                    String [] parameters = token.Split(otherDeli,2); 
                    if (parameters[0] == "token")
                    {
                        return parameters[1]; 
                    }
                }
            }
            return null; 
        }
        //end of public static string getTokenFromReply(URL url)

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Exchanges the one time use token returned in the URL for a session
        /// token. If the key is non-null, the token will be used securely, 
        /// and the request will be signed
        /// </summary> 
        /// <param name="onetimeUseToken">the token send by google in the URL</param>
        /// <param name="key">the private key used to sign</param>
        /// <returns>the session token</returns>
        //////////////////////////////////////////////////////////////////////
        public static String exchangeForSessionToken(String onetimeUseToken, 
                                                     AsymmetricAlgorithm key)
        {
            return exchangeForSessionToken(DEFAULT_PROTOCOL, DEFAULT_DOMAIN,
                                           onetimeUseToken, key);    
        }
        //end of public static String exchangeForSessionToken(String onetimeUseToken, PrivateKey key)




        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Exchanges the one time use token returned in the URL for a session
        /// token. If the key is non-null, the token will be used securely, 
        /// and the request will be signed
        /// </summary> 
        /// <param name="protocol">the protocol to use to communicate with the 
        /// server</param>
        /// <param name="domain">the domain at which the authentication server 
        /// exists</param>
        /// <param name="onetimeUseToken">the token send by google in the URL</param>
        /// <param name="key">the private key used to sign</param>
        /// <returns>the session token</returns>
        //////////////////////////////////////////////////////////////////////
        public static string exchangeForSessionToken(string protocol,
                                                     string domain,
                                                     string onetimeUseToken, 
                                                     AsymmetricAlgorithm key)
        {
            HttpWebResponse response = null;
            string authSubToken = null; 

            try
            {
                string sessionUrl = getSessionTokenUrl(protocol, domain);
                Uri uri = new Uri(sessionUrl);

                HttpWebRequest request = WebRequest.Create(uri) as HttpWebRequest;


                string header = formAuthorizationHeader(onetimeUseToken, key, uri, "GET");
                request.Headers.Add(header); 

                response = request.GetResponse() as HttpWebResponse; 

            }
            catch (WebException e)
            {
                Tracing.TraceMsg("exchangeForSessionToken failed " + e.Status); 
                throw new GDataRequestException("Execution of exchangeForSessionToken", e);
            }
            if (response != null)
            {
                int code= (int)response.StatusCode;
                if (code != 200)
                {
                    throw new GDataRequestException("Execution of exchangeForSessionToken request returned unexpected result: " +code,  response); 
                }
                // get the body and parse it
                authSubToken = Utilities.ParseValueFormStream(response.GetResponseStream(), GoogleAuthentication.AuthSubToken); 
            }

            Tracing.Assert(authSubToken != null, "did not find an auth token in exchangeForSessionToken"); 

            return authSubToken; 

        }
        //end of public static String exchangeForSessionToken(String onetimeUseToken, PrivateKey key)


        /// <summary>
        ///  Revokes the specified token. If the <code>key</code> is non-null, 
        /// the token will be used securely and the request to revoke the 
        /// token will be signed.
        /// </summary>
        /// <param name="token">the AuthSub token to revoke</param>
        /// <param name="key">the private key to sign the request</param>
        public static void revokeToken(string token,
                                 AsymmetricAlgorithm key)
        {        
            revokeToken(DEFAULT_PROTOCOL, DEFAULT_DOMAIN, token, key);
        }

  
        /// <summary>
        ///  Revokes the specified token. If the <code>key</code> is non-null, 
        /// the token will be used securely and the request to revoke the 
        /// token will be signed.
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="domain"></param>
        /// <param name="token">the AuthSub token to revoke</param>
        /// <param name="key">the private key to sign the request</param>
        public static void revokeToken(String protocol,
                                 String domain,
                                 String token,
                                 AsymmetricAlgorithm key)
        {
        
            HttpWebResponse response = null;
        
            try
            {
                string revokeUrl = getRevokeTokenUrl(protocol, domain);
                Uri uri = new Uri(revokeUrl);

                HttpWebRequest request = WebRequest.Create(uri) as HttpWebRequest;


                string header = formAuthorizationHeader(token, key, uri, "GET");
                request.Headers.Add(header); 

                response = request.GetResponse() as HttpWebResponse; 

            }
            catch (WebException e)
            {
                Tracing.TraceMsg("revokeToken failed " + e.Status); 
                throw new GDataRequestException("Execution of revokeToken", e);
            }
            if (response != null)
            {
                int code= (int)response.StatusCode;
                if (code != 200)
                {
                    throw new GDataRequestException("Execution of revokeToken request returned unexpected result: " +code,  response); 
                }
            }
       }


        //////////////////////////////////////////////////////////////////////
        /// <summary>Forms the AuthSub authorization header.
        /// if key is null, the token will be in insecure mode, otherwise 
        /// the token will be used securely and the header contains
        /// a signature
        /// </summary> 
        /// <param name="token">the AuthSub token to use </param>
        /// <param name="key">the private key to used </param>
        /// <param name="requestUri">the request uri to use </param>
        /// <param name="requestMethod">the HTTP method to use </param>
        /// <returns>the authorization header </returns>
        //////////////////////////////////////////////////////////////////////
        public static string formAuthorizationHeader(string token, 
                                                     AsymmetricAlgorithm key, 
                                                     Uri requestUri, 
                                                     string requestMethod)
        {
            if (key == null)
            {
                return String.Format(CultureInfo.InvariantCulture, "Authorization: AuthSub token=\"{0}\"", token);
            }
            else
            {
                if (requestUri == null)
                {
                    throw new ArgumentNullException("requestUri");
                }

                // Form signature for secure mode
                TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
                int timestamp = (int)t.TotalSeconds;

                string nounce = generateULONGRnd(); 

                string dataToSign = String.Format(CultureInfo.InvariantCulture,
                                                    "{0} {1} {2} {3}", 
                                                  requestMethod, 
                                                  requestUri.AbsoluteUri,
                                                  timestamp.ToString(CultureInfo.InvariantCulture), 
                                                  nounce);


                byte[] signature = sign(dataToSign, key);

                string encodedSignature = Convert.ToBase64String(signature);

                string algorithmName = key is DSACryptoServiceProvider ? "dsa-sha1" : "rsa-sha1";

                return String.Format(CultureInfo.InvariantCulture,
                                        "Authorization: AuthSub token=\"{0}\" data=\"{1}\" sig=\"{2}\" sigalg=\"{3}\"",
                                     token, dataToSign, encodedSignature,  algorithmName);
            }

        }
        //end of public static string formAuthorizationHeader(string token, p key, Uri requestUri, string requestMethod)


        //////////////////////////////////////////////////////////////////////
        /// <summary>Retrieves information about the AuthSub token. 
        /// If the <code>key</code> is non-null, the token will be used securely
        /// and the request to revoke the token will be signed.
        /// </summary> 
        /// <param name="token">tthe AuthSub token for which to receive information </param>
        /// <param name="key">the private key to sign the request</param>
        /// <returns>the token information in the form of a Dictionary from the name of the
        ///  attribute to the value of the attribute</returns>
        //////////////////////////////////////////////////////////////////////
        public static Dictionary<String, String> GetTokenInfo(String token,
                                             AsymmetricAlgorithm key)
        {
            return GetTokenInfo(DEFAULT_PROTOCOL, DEFAULT_DOMAIN, token, key);
        }


       
        //////////////////////////////////////////////////////////////////////
        /// <summary>Retrieves information about the AuthSub token. 
        /// If the <code>key</code> is non-null, the token will be used securely
        /// and the request to revoke the token will be signed.
        /// </summary> 
        /// <param name="protocol">the protocol to use to communicate with the server</param>
        /// <param name="domain">the domain at which the authentication server exists</param>
        /// <param name="token">tthe AuthSub token for which to receive information </param>
        /// <param name="key">the private key to sign the request</param>
        /// <returns>the token information in the form of a Dictionary from the name of the
        ///  attribute to the value of the attribute</returns>
        //////////////////////////////////////////////////////////////////////
        public static Dictionary<String, String> GetTokenInfo(String protocol,
                                             String domain,
                                             String token,
                                             AsymmetricAlgorithm key)
        {

            HttpWebResponse response; 
        
            try
            {
                string tokenInfoUrl = GetTokenInfoUrl(protocol, domain);
                Uri uri = new Uri(tokenInfoUrl);
    
                HttpWebRequest request = WebRequest.Create(uri) as HttpWebRequest;
    
    
                string header = formAuthorizationHeader(token, key, uri, "GET");
                request.Headers.Add(header); 
    
                response = request.GetResponse() as HttpWebResponse; 
    
            }        
            catch (WebException e)
            {
                Tracing.TraceMsg("GetTokenInfo failed " + e.Status); 
                throw new GDataRequestException("Execution of GetTokenInfo", e);
            }

            if (response != null)
            {
                int code= (int)response.StatusCode;
                if (code != 200)
                {
                    throw new GDataRequestException("Execution of revokeToken request returned unexpected result: " +code,  response); 
                }
                TokenCollection tokens = Utilities.ParseStreamInTokenCollection(response.GetResponseStream());
                if (tokens != null)
                {
                    return tokens.CreateDictionary();
                }
            }
            return null; 
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>creates a max 20 character long string of random numbers</summary>
        /// <returns> the string containing random numbers</returns>
        //////////////////////////////////////////////////////////////////////
        private static string generateULONGRnd()
        {
            byte[] randomNumber = new byte[20];

            // Create a new instance of the RNGCryptoServiceProvider. 
            RNGCryptoServiceProvider Gen = new RNGCryptoServiceProvider();

            // Fill the array with a random value.
            Gen.GetBytes(randomNumber);

            StringBuilder x = new StringBuilder(20);
            for (int i = 0; i < 20; i++)
            {
                if (randomNumber[i] == 0 && x.Length == 0)
                {
                    continue; 
                }
                x.Append(Convert.ToInt16(randomNumber[i], CultureInfo.InvariantCulture).ToString()[0]);
            }
            return x.ToString(); 
        }
        //end of private static string generateULONGRnd()



        //////////////////////////////////////////////////////////////////////
        /// <summary>signs the data with the given key</summary>
        /// <param name="dataToSign">the data to sign </param>
        /// <param name="key">the private key to used </param>
        /// <returns> the signed data</returns>
        //////////////////////////////////////////////////////////////////////
        private static byte[]  sign(string dataToSign, AsymmetricAlgorithm key) 
        {
            byte[] data = new ASCIIEncoding().GetBytes(dataToSign);

            try
            {
                RSACryptoServiceProvider providerRSA = key as RSACryptoServiceProvider; 
                if (providerRSA != null)
                {
                    return providerRSA.SignData(data, new SHA1CryptoServiceProvider());
                }
                DSACryptoServiceProvider providerDSA = key as DSACryptoServiceProvider;
                if (providerDSA != null)
                {
                    return providerDSA.SignData(data); 
                }
            }
            catch (CryptographicException e)
            {
                Tracing.TraceMsg(e.Message);
            }
            return null; 
        }



        //////////////////////////////////////////////////////////////////////
        /// <summary>Returns the URL that handles token information call.</summary>
        /// <param name="protocol">the protocol to use to communicate with the server</param>
        /// <param name="domain">the domain at which the authentication server exists</param>
        /// <returns> the URL that handles token information call.</returns>
        //////////////////////////////////////////////////////////////////////
        private static String GetTokenInfoUrl(String protocol,
                                    String domain) {
            return protocol + "://" + domain + "/accounts/AuthSubTokenInfo";
        }


    }
    //end of public class AuthSubUtil
} 
/////////////////////////////////////////////////////////////////////////////

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
 * 
 * Author: Andrew Smith <andy@snae.net> 22/11/08
*/

using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;

namespace Google.GData.Client
{
    /// <summary>
    /// A request factory to generate an authorization header suitable for use
    /// with two-legged OAuth.
    /// </summary>
    public class GOAuthRequestFactory : GDataGAuthRequestFactory
    {
        /// <summary>this factory's agent</summary> 
        public const string GDataGAuthSubAgent = "GOAuthRequestFactory-CS/1.0.0";

        private String tokenSecret;
        private String token;

        private String consumerSecret;
        private String consumerKey;

        //////////////////////////////////////////////////////////////////////
        /// <summary>default constructor</summary> 
        //////////////////////////////////////////////////////////////////////
        public GOAuthRequestFactory(string service, string applicationName)
            : base(service, applicationName)
        {
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>default constructor</summary> 
        //////////////////////////////////////////////////////////////////////
        public override IGDataRequest CreateRequest(GDataRequestType type, Uri uriTarget)
        {
            return new GOAuthRequest(type, uriTarget, this);
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string ConsumerSecret</summary> 
        /// <returns>the ConsumerSecret for the oauth request </returns>
        //////////////////////////////////////////////////////////////////////
        public string ConsumerSecret
        {
            get { return this.consumerSecret; }
            set { this.consumerSecret = value; }
        }
        // end of accessor public string ConsumerSecret

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string ConsumerKey</summary> 
        /// <returns>the ConsumerKey used for the oauth request </returns>
        //////////////////////////////////////////////////////////////////////
        public string ConsumerKey
        {
            get { return this.consumerKey; }
            set { this.consumerKey = value; }
        }
        // end of accessor public string ConsumerKey

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string TokenSecret</summary> 
        /// <returns>the TokenSecret for the oauth request </returns>
        //////////////////////////////////////////////////////////////////////
        public string TokenSecret
        {
            get { return this.tokenSecret; }
            set { this.tokenSecret = value; }
        }
        // end of accessor public string ConsumerSecret

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string TokenSecret</summary> 
        /// <returns>the Token for the oauth request </returns>
        //////////////////////////////////////////////////////////////////////
        public string Token
        {
            get { return this.token; }
            set { this.token = value; }
        }
        // end of accessor public string ConsumerSecret

    }
    /////////////////////////////////////////////////////////////////////////////


    //////////////////////////////////////////////////////////////////////
    /// <summary>GOAuthSubRequest implementation</summary> 
    //////////////////////////////////////////////////////////////////////
    public class GOAuthRequest : GDataGAuthRequest
    {
        /// <summary>holds the factory instance</summary> 
        private GOAuthRequestFactory factory;


        //////////////////////////////////////////////////////////////////////
        /// <summary>default constructor</summary> 
        //////////////////////////////////////////////////////////////////////
        internal GOAuthRequest(GDataRequestType type, Uri uriTarget, GOAuthRequestFactory factory)
            :
                base(type, uriTarget, factory)
        {
            this.factory = factory;
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>sets up the correct credentials for this call</summary> 
        //////////////////////////////////////////////////////////////////////
        protected override void EnsureCredentials()
        {
            HttpWebRequest http = this.Request as HttpWebRequest;

            if (string.IsNullOrEmpty(this.factory.ConsumerKey) || string.IsNullOrEmpty(this.factory.ConsumerSecret))
            {
                throw new GDataRequestException("ConsumerKey and ConsumerSecret must be provided to use GOAuthRequestFactory");
            }

            string oauthHeader = OAuthUtil.GenerateHeader(http.RequestUri, 
                                                            this.factory.ConsumerKey, 
                                                            this.factory.ConsumerSecret, 
                                                            this.factory.Token, 
                                                            this.factory.TokenSecret,
                                                            http.Method);
            this.Request.Headers.Remove("Authorization"); // needed?
            this.Request.Headers.Add(oauthHeader);
        }
        /////////////////////////////////////////////////////////////////////////////
    }
    /////////////////////////////////////////////////////////////////////////////
}
/////////////////////////////////////////////////////////////////////////////

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
using System.IO;
using System.Net;
using System.Security.Cryptography;

#endregion

/// <summary>
/// contains AuthSub request helper classes
/// </summary>
namespace Google.GData.Client {
    /// <summary>
    /// GDataAuthSubRequestFactory implementation
    /// </summary>
    public class GAuthSubRequestFactory : GDataGAuthRequestFactory {

        /// holds the private key that is used to sign the requests
        private AsymmetricAlgorithm privateKey;

        /// <summary>
        /// default constructor
        /// </summary>
        public GAuthSubRequestFactory(string service, string applicationName)
            : base(service, applicationName) {
        }
        
        /// <summary>
        /// default constructor
        /// </summary>
        public override IGDataRequest CreateRequest(GDataRequestType type, Uri uriTarget) {
            return new GAuthSubRequest(type, uriTarget, this);
        }

        /// <summary>
        /// accessor method public string Token
        /// </summary>
        /// <returns>
        /// the string token for the authsub request
        /// </returns>
        public string Token {
            get { return this.GAuthToken; }
            set { this.GAuthToken = value; }
        }

        /// <summary>
        /// accessor method public AsymmetricAlgorithm PrivateKey
        /// </summary>
        /// <returns>
        /// the private Key used for the authsub request
        /// </returns>
        public AsymmetricAlgorithm PrivateKey {
            get { return this.privateKey; }
            set { this.privateKey = value; }
        }

    }

    /// <summary>
    /// base GDataRequest implementation
    /// </summary>
    public class GAuthSubRequest : GDataGAuthRequest {
        /// <summary>holds the factory instance</summary> 
        private GAuthSubRequestFactory factory;

        /// <summary>
        /// default constructor
        /// </summary>
        internal GAuthSubRequest(GDataRequestType type, Uri uriTarget, GAuthSubRequestFactory factory) :
            base(type, uriTarget, factory) {
            this.factory = factory;
        }

        /// <summary>
        /// sets up the correct credentials for this call, pending 
        /// security scheme
        /// </summary>
        protected override void EnsureCredentials() {
            HttpWebRequest http = this.Request as HttpWebRequest;

            string header = AuthSubUtil.formAuthorizationHeader(this.factory.Token,
                this.factory.PrivateKey,
                http.RequestUri,
                http.Method);
            this.Request.Headers.Add(header);
        }
    }
}

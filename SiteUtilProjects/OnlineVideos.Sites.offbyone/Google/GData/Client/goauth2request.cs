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
 * Author: Alain Vongsouvanh <alainv@google.com>
*/

using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;

namespace Google.GData.Client {
    /// <summary>
    /// A request factory to generate an authorization header suitable for use
    /// with OAuth 2.0.
    /// </summary>
    public class GOAuth2RequestFactory : GDataGAuthRequestFactory {
        /// <summary>this factory's agent</summary>
        public const string GDataGAuthSubAgent = "GOAuth2RequestFactory-CS/1.0.0";

        /// <summary>
        /// Constructor.
        /// </summary>
        public GOAuth2RequestFactory(string service, string applicationName, OAuth2Parameters parameters)
            : base(service, applicationName) {
            this.Parameters = parameters;
        }

        /// <summary>
        /// default constructor.
        /// </summary>
        public override IGDataRequest CreateRequest(GDataRequestType type, Uri uriTarget) {
            return new GOAuth2Request(type, uriTarget, this);
        }

        public OAuth2Parameters Parameters { get; set; }
    }

    /// <summary>
    /// GOAuthSubRequest implementation.
    /// </summary>
    public class GOAuth2Request : GDataGAuthRequest {
        /// <summary>holds the factory instance</summary>
        private GOAuth2RequestFactory factory;

        /// <summary>
        /// default constructor.
        /// </summary>
        internal GOAuth2Request(GDataRequestType type, Uri uriTarget, GOAuth2RequestFactory factory)
            : base(type, uriTarget, factory) {
            this.factory = factory;
        }

        /// <summary>
        /// sets up the correct credentials for this call.
        /// </summary>
        protected override void EnsureCredentials() {
            HttpWebRequest http = this.Request as HttpWebRequest;

            if (string.IsNullOrEmpty(this.factory.Parameters.AccessToken)) {
                throw new GDataRequestException("An access token must be provided to use GOAuthRequestFactory");
            }

            this.Request.Headers.Remove("Authorization"); // needed?
            this.Request.Headers.Add("Authorization", String.Format(
                "{0} {1}", this.factory.Parameters.TokenType, this.factory.Parameters.AccessToken));
        }

        public override void Execute() {
            try {
                base.Execute();
            } catch (GDataRequestException re) {
                HttpWebResponse webResponse = re.Response as HttpWebResponse;
                if (webResponse != null && webResponse.StatusCode == HttpStatusCode.Unauthorized) {
                    Tracing.TraceMsg("Access token might have expired, refreshing.");
                    Reset();
                    try {
                        OAuthUtil.RefreshAccessToken(this.factory.Parameters);
                    } catch (WebException e) {
                        Tracing.TraceMsg("Failed to refresh access token: " + e.StackTrace);
                        throw re;
                    }
                    base.Execute();
                } else {
                    throw;
                }
            }
        }

    }
}

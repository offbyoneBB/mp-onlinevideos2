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
 * 
*/

using System;
using System.Text;
using System.Collections.Generic;

namespace Google.GData.Client {
    /// <summary>
    /// Stores the parameters used to make OAuth requests
    /// </summary>
    public class OAuthParameters {
        public readonly SortedDictionary<string, string> BaseProperties = new SortedDictionary<string, string>();
        public readonly Dictionary<string, string> ExtraProperties = new Dictionary<string, string>();

        public string Callback {
            get {
                return safeGet(ExtraProperties, OAuthBase.OAuthCallbackKey);
            }
            set {
                addOrUpdate(ExtraProperties, OAuthBase.OAuthCallbackKey, value);
            }
        }

        public string ConsumerKey {
            get {
                return safeGet(BaseProperties, OAuthBase.OAuthConsumerKeyKey);
            }
            set {
                addOrUpdate(BaseProperties, OAuthBase.OAuthConsumerKeyKey, value);
            }
        }

        public string ConsumerSecret {
            get {
                return safeGet(ExtraProperties, OAuthBase.OAuthConsumerSecretKey);
            }
            set {
                addOrUpdate(ExtraProperties, OAuthBase.OAuthConsumerSecretKey, value);
            }
        }

        public string Nonce {
            get {
                return safeGet(BaseProperties, OAuthBase.OAuthNonceKey);
            }
            set {
                addOrUpdate(BaseProperties, OAuthBase.OAuthNonceKey, value);
            }
        }

        public string Scope {
            get {
                return safeGet(ExtraProperties, OAuthBase.OAuthScopeKey);
            }
            set {
                addOrUpdate(ExtraProperties, OAuthBase.OAuthScopeKey, value);
            }
        }

        public string Signature {
            get {
                return safeGet(ExtraProperties, OAuthBase.OAuthSignatureKey);
            }
            set {
                addOrUpdate(ExtraProperties, OAuthBase.OAuthSignatureKey, value);
            }
        }

        public string SignatureMethod {
            get {
                return safeGet(BaseProperties, OAuthBase.OAuthSignatureMethodKey);
            }
            set {
                addOrUpdate(BaseProperties, OAuthBase.OAuthSignatureMethodKey, value);
            }
        }

        public string Timestamp {
            get {
                return safeGet(BaseProperties, OAuthBase.OAuthTimestampKey);
            }
            set {
                addOrUpdate(BaseProperties, OAuthBase.OAuthTimestampKey, value);
            }
        }

        public string Token {
            get {
                return safeGet(BaseProperties, OAuthBase.OAuthTokenKey);
            }
            set {
                addOrUpdate(BaseProperties, OAuthBase.OAuthTokenKey, value);
            }
        }

        public string TokenSecret {
            get {
                return safeGet(ExtraProperties, OAuthBase.OAuthTokenSecretKey);
            }
            set {
                addOrUpdate(ExtraProperties, OAuthBase.OAuthTokenSecretKey, value);
            }
        }

        public string Verifier {
            get {
                return safeGet(BaseProperties, OAuthBase.OAuthVerifierKey);
            }
            set {
                addOrUpdate(BaseProperties, OAuthBase.OAuthVerifierKey, value);
            }
        }

        /// <summary>
        /// Adds a new key-value pair to the dictionary or updates the value if the key is already present
        /// </summary>
        private void addOrUpdate(IDictionary<string, string> dictionary, string key, string value) {
            if (dictionary.ContainsKey(key)) {
                if (value == null) {
                    dictionary.Remove(key);
                } else {
                    dictionary[key] = value;
                }
            } else if (value != null) {
                dictionary.Add(key, value);
            }
        }

        /// <summary>
        /// Returns the value corresponding to the key in the dictionary or null if the key is not present
        /// </summary>
        private string safeGet(IDictionary<string, string> dictionary, string key) {
            if (dictionary.ContainsKey(key)) {
                return dictionary[key];
            } else {
                return null;
            }
        }
    }
}

﻿/*
Copyright 2013 Google Inc

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using OnlineVideos._3rdParty.Newtonsoft.Json;

namespace Google.Apis.Auth.OAuth2
{
    /// <summary>Client credential details for installed and web applications.</summary>
    public sealed class ClientSecrets
    {
        /// <summary>Gets or sets the client identifier.</summary>
        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        /// <summary>Gets or sets the client Secret.</summary>
        [JsonProperty("client_secret")]
        public string ClientSecret { get; set; }
    }
}

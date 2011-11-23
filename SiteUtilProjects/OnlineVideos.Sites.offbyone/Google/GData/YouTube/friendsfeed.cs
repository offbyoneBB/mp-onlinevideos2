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

using System;
using System.Collections;
using System.Text;
using System.Xml;
using Google.GData.Client;
using Google.GData.Extensions;

namespace Google.GData.YouTube {
    /// <summary>
    /// A user's contacts feed lists all of the contacts for a specified user.
    /// To request the currently logged-in user's contact list, send an HTTP
    ///  GET request to the following URL. 
    ///     https://gdata.youtube.com/feeds/api/users/default/contacts
    /// To request another user's contact list, send an HTTP GET request to the following URL. 
    ///     https://gdata.youtube.com/feeds/api/users/username/contacts
    /// In the URL above, you must replace the text username with the user's YouTube username.
    /// Contacts can be classified as either Friends or Family.
    /// </summary>
    public class FriendsFeed : YouTubeFeed {
        /// <summary>
        ///  default constructor
        /// </summary>
        /// <param name="uriBase">the base URI of the feedEntry</param>
        /// <param name="iService">the Service to use</param>
        public FriendsFeed(Uri uriBase, IService iService)
            : base(uriBase, iService) {
        }

        /// <summary>
        /// this needs to get implemented by subclasses
        /// </summary>
        /// <returns>AtomEntry</returns>
        public override AtomEntry CreateFeedEntry() {
            return new FriendsEntry();
        }
    }
}


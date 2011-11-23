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
#define USE_TRACING

using System;
using System.Xml;
using System.IO;
using System.Collections;
using Google.GData.Client;
using Google.GData.Extensions;
using Google.GData.Extensions.MediaRss;
using Google.GData.Extensions.Exif;
using Google.GData.Extensions.Location;
using Google.GData.Extensions.AppControl;

namespace Google.GData.YouTube {
    /// <summary>
    /// An individual entry inside the FriendsFeed. It represents a contact of the user
    /// </summary>
    public class FriendsEntry : YouTubeBaseEntry {
        /// <summary>
        /// Category used to label entries that friends
        /// </summary>
        public static AtomCategory FRIENDS_CATEGORY =
            new AtomCategory(YouTubeNameTable.KIND_FRIEND, new AtomUri(BaseNameTable.gKind));

        /// <summary>
        /// Constructs a new FriendsEntry instance
        /// </summary>
        public FriendsEntry() : base() {
            Tracing.TraceMsg("Created FriendsEntry");
            // add status and username
            this.AddExtension(new Status());
            this.AddExtension(new UserName());
            Categories.Add(FRIENDS_CATEGORY);
        }

        /// <summary>
        /// getter/setter for Status subelement
        /// </summary>
        public string Status {
            get {
                return getYouTubeExtensionValue(YouTubeNameTable.Status);
            }
            set {
                setYouTubeExtension(YouTubeNameTable.Status, value);
            }
        }

        /// <summary>
        /// getter/setter for UserName subelement
        /// </summary>
        public string UserName {
            get {
                return getYouTubeExtensionValue(YouTubeNameTable.UserName);
            }
            set {
                setYouTubeExtension(YouTubeNameTable.UserName, value);
            }
        }
    }
}




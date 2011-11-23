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
    /// A user's playlists feed contains a list of the playlists created by
    ///  that user. If you are requesting the playlists feed for the currently
    ///  authenticated user, the feed will contain both public and private playlists. 
    /// However, if you send an unauthenticated request or request playlists created 
    /// by someone other than the currently authenticated user, the feed will only
    ///  contain public playlists.
    /// In a playlists feed, each entry contains information about a single playlist, 
    /// including the playlist title, description and author. The gd:feedLink tag 
    /// in the entry identifies the URL that allows you to retrieve the playlist feed, 
    /// which specifies information about the videos in the playlist.
    /// </summary>
    public class PlaylistsFeed : YouTubeFeed {
        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="uriBase">the base URI of the feedEntry</param>
        /// <param name="iService">the Service to use</param>
        public PlaylistsFeed(Uri uriBase, IService iService)
            : base(uriBase, iService) {
        }

        /// <summary>
        /// this needs to get implemented by subclasses
        /// </summary>
        /// <returns>AtomEntry</returns>
        public override AtomEntry CreateFeedEntry() {
            return new PlaylistsEntry();
        }
    }
}


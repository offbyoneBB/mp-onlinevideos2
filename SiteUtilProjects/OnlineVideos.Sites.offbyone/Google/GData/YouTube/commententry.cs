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
using System.Collections.Generic;
using Google.GData.Client;

namespace Google.GData.YouTube {
    /// <summary>
    /// Entry API customization class for defining comment entries in an comments feed.
    /// </summary>
    public class CommentEntry : AbstractEntry {
        /// <summary>
        /// Constructs a new CommentEntry instance
        /// </summary>
        public CommentEntry() : base() {
        }

        /// <summary>
        /// returns the list of reply links inside the entry. Not that modifying that list 
        /// will not modify link collection. This is a readonly copy. But the items in that 
        /// list are the same as in the linkcollection, so you can remove them from there
        /// </summary>
        public List<AtomLink> Replies {
            get {
                return this.Links.FindServiceList(YouTubeNameTable.ReplyToRelationship, AtomLink.ATOM_TYPE);
            }
        }

        /// <summary>
        /// Adds a reply link to this commententry
        ///    -> this new entry will reply to the passed in entry when the comment is 
        ///    submitted. This will not protect from adding the same guy several times.
        /// </summary>
        /// <param name="theOriginalComment"></param>
        /// <returns></returns>
        public void ReplyTo(CommentEntry theOriginalComment) {
            if (theOriginalComment.SelfUri == null) {
                throw new ArgumentException("You can only reply to an entry with a valid SelfUri");
            }

            AtomLink link = new AtomLink(AtomLink.ATOM_TYPE, YouTubeNameTable.ReplyToRelationship);
            link.HRef = theOriginalComment.SelfUri;
            this.Links.Add(link);
        }
    }
}




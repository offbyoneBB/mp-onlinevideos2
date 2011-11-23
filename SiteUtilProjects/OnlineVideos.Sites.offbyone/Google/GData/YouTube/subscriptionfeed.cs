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
    /// A user's subscriptions feed contains a list of the channels, 
    /// favorite video lists and search queries to which the user has subscribed.
    /// In a subscriptions feed, each entry contains information about a single
    ///  subscription. Each entry contains the following key tags:
    /// The gd:feedLink tag in the entry identifies the URL that allows 
    /// you to retrieve videos for the subscription.
    /// For one of the category tags in the entry, the scheme attribute
    /// value will be http://gdata.youtube.com/schemas/2007/subscriptiontypes.cat. 
    /// That tag's term attribute indicates whether the entry describes a 
    /// subscription to a channel (term="channel"), another user's 
    /// favorite videos list (term="favorites"), or to videos that match
    ///  specific keywords (term="query").
    /// If the subscription is to another user's channel or list of favorite videos, 
    /// the yt:username tag will identify the user who owns the channel or favorite video list.
    /// If the subscription is to a keyword query, the yt:queryString element will
    /// contain the subscribed-to query term.
    /// </summary>
    public class SubscriptionFeed : YouTubeFeed {
        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="uriBase">the base URI of the feedEntry</param>
        /// <param name="iService">the Service to use</param>
        public SubscriptionFeed(Uri uriBase, IService iService)
            : base(uriBase, iService) {
        }

        /// <summary>
        /// this needs to get implemented by subclasses
        /// </summary>
        /// <returns>AtomEntry</returns>
        public override AtomEntry CreateFeedEntry() {
            return new SubscriptionEntry();
        }
    }
}


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
using System.IO;
using System.Collections;
using System.Text;
using System.Net;
using Google.GData.Client;
using Google.GData.Extensions;
using System.Collections.Generic;

namespace Google.GData.YouTube {
    /// <summary>
    /// The YouTube Data API allows applications to perform functions normally 
    /// executed on the YouTube website. The API enables your application to search 
    /// for YouTube videos and to retrieve standard video feeds, comments and video
    /// responses. 
    /// In addition, the API lets your application upload videos to YouTube or 
    /// update existing videos. Your can also retrieve playlists, subscriptions, 
    /// user profiles and more. Finally, your application can submit 
    /// authenticated requests to enable users to create playlists, 
    /// subscriptions, contacts and other account-specific entities.
    /// </summary>
    public class YouTubeService : MediaService {
        /// <summary>
        /// default category for YouTube
        /// </summary>
        public const string DefaultCategory = "http://gdata.youtube.com/schemas/2007/categories.cat";

        /// <summary>
        /// the YouTube authentication handler URL
        /// </summary>
        public const string AuthenticationHandler = "https://www.google.com/accounts/ClientLogin";

        private string developerID;

        /// <summary>
        /// obsolete constructor
        /// </summary>
        /// <param name="applicationName">the application name</param>
        /// <param name="client">the client identifier</param>
        /// <param name="developerKey">the developerKey</param>/// 
        [Obsolete("The client id was removed from the YouTubeService, use the constructor without a clientid")]
        public YouTubeService(string applicationName, string client, string developerKey)
            : base(ServiceNames.YouTube, applicationName) {
            if (developerKey == null) {
                throw new ArgumentNullException("developerKey");
            }

            this.NewFeed += new ServiceEventHandler(this.OnNewFeed);
            developerID = developerKey;
            OnRequestFactoryChanged();
        }

        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="applicationName">the application name</param>
        /// <param name="developerKey">the developerKey</param>/// 
        public YouTubeService(string applicationName, string developerKey)
            : base(ServiceNames.YouTube, applicationName) {
            if (developerKey == null) {
                throw new ArgumentNullException("developerKey");
            }

            this.NewFeed += new ServiceEventHandler(this.OnNewFeed);
            developerID = developerKey;
            OnRequestFactoryChanged();
        }

        /// <summary>
        /// readonly constructor 
        /// </summary>
        /// <param name="applicationName">the application identifier</param>
        public YouTubeService(string applicationName)
            : base(ServiceNames.YouTube, applicationName) {
            this.NewFeed += new ServiceEventHandler(this.OnNewFeed);
            OnRequestFactoryChanged();
        }

        /// <summary>
        /// overloaded to create typed version of Query
        /// </summary>
        /// <param name="feedQuery"></param>
        /// <returns>EventFeed</returns>
        public YouTubeFeed Query(YouTubeQuery feedQuery) {
            return base.Query(feedQuery) as YouTubeFeed;
        }

        /// <summary>
        /// returns a playlist feed based on a youtubeQuery
        /// </summary>
        /// <param name="feedQuery"></param>
        /// <returns>EventFeed</returns>
        public PlaylistFeed GetPlaylist(YouTubeQuery feedQuery) {
            return base.Query(feedQuery) as PlaylistFeed;
        }

        /// <summary>
        /// returns a playlist feed based on a youtubeQuery
        /// </summary>
        /// <param name="feedQuery"></param>
        /// <returns>EventFeed</returns>
        public FriendsFeed GetFriends(YouTubeQuery feedQuery) {
            return base.Query(feedQuery) as FriendsFeed;
        }

        /// <summary>
        /// returns a playlists feed based on a youtubeQuery
        /// </summary>
        /// <param name="feedQuery"></param>
        /// <returns>EventFeed</returns>
        public PlaylistsFeed GetPlaylists(YouTubeQuery feedQuery) {
            return base.Query(feedQuery) as PlaylistsFeed;
        }

        /// <summary>
        /// returns a subscription feed based on a youtubeQuery
        /// </summary>
        /// <param name="feedQuery"></param>
        /// <returns>EventFeed</returns>
        public SubscriptionFeed GetSubscriptions(YouTubeQuery feedQuery) {
            return base.Query(feedQuery) as SubscriptionFeed;
        }

        /// <summary>
        /// returns a message feed based on a youtubeQuery
        /// </summary>
        /// <param name="feedQuery"></param>
        /// <returns>EventFeed</returns>
        public MessageFeed GetMessages(YouTubeQuery feedQuery) {
            return base.Query(feedQuery) as MessageFeed;
        }

        /// <summary>
        /// overloaded to create typed version of Query. Returns an 
        /// Activities feed
        /// </summary>
        /// <param name="feedQuery"></param>
        /// <returns>ActivitiesFeed</returns>
        public ActivitiesFeed Query(ActivitiesQuery feedQuery) {
            return base.Query(feedQuery) as ActivitiesFeed;
        }

        /// <summary>
        /// upload a new video to this users youtube account
        /// </summary>
        /// <param name="userName">the username (account) to use</param>
        /// <param name="entry">the youtube entry</param>
        /// <returns></returns>
        public YouTubeEntry Upload(string userName, YouTubeEntry entry) {
            if (String.IsNullOrEmpty(userName)) {
                userName = "default";
            }

            Uri uri = new Uri("https://uploads.gdata.youtube.com/feeds/api/users/" + userName + "/uploads");
            return base.Insert(uri, entry);
        }

        /// <summary>
        /// upload a new video to the default/authenticated account
        /// </summary>
        /// <param name="entry">the youtube entry</param>
        /// <returns></returns>
        public YouTubeEntry Upload(YouTubeEntry entry) {
            return Upload(null, entry);
        }

        /// <summary>
        /// by default all services now use version 1 for the protocol.
        /// this needs to be overridden by a service to specify otherwise. 
        /// YouTube uses version 2
        /// </summary>
        /// <returns></returns>
        protected override void InitVersionInformation() {
            this.ProtocolMajor = VersionDefaults.VersionTwo;
        }

        /// <summary>
        /// Method for browser-based upload, gets back a non-Atom response
        /// </summary>
        /// <param name="newEntry">The YouTubeEntry containing the metadata for a video upload</param>
        /// <returns>A FormUploadToken object containing an upload token and POST url</returns>
        public FormUploadToken FormUpload(YouTubeEntry newEntry) {
            if (newEntry == null) {
                throw new ArgumentNullException("newEntry");
            } 
            
            Uri uri = new Uri("https://gdata.youtube.com/action/GetUploadToken");
            Stream returnStream = EntrySend(uri, newEntry, GDataRequestType.Insert);
            FormUploadToken token = new FormUploadToken(returnStream);
            returnStream.Close();

            return token;
        }

        /// <summary>
        /// notifier if someone changes the requestfactory of the service
        /// </summary>
        public override void OnRequestFactoryChanged() {
            base.OnRequestFactoryChanged();
            GDataGAuthRequestFactory factory = this.RequestFactory as GDataGAuthRequestFactory;
            if (factory != null && this.developerID != null) {
                RemoveOldKeys(factory.CustomHeaders);
                factory.CustomHeaders.Add(GoogleAuthentication.YouTubeDevKey + this.developerID);
                factory.Handler = YouTubeService.AuthenticationHandler;
            }
        }

        private static void RemoveOldKeys(List<string> headers) {
            foreach (string header in headers) {
                if (header.StartsWith(GoogleAuthentication.WebKey)) {
                    headers.Remove(header);
                    return;
                }
            }
            return;
        }

        /// <summary>eventchaining. We catch this by from the base service, which 
        /// would not by default create an atomFeed</summary> 
        /// <param name="sender"> the object which send the event</param>
        /// <param name="e">FeedParserEventArguments, holds the feedentry</param> 
        /// <returns> </returns>
        protected void OnNewFeed(object sender, ServiceEventArgs e) {
            Tracing.TraceMsg("Created new YouTube Feed");
            if (e == null) {
                throw new ArgumentNullException("e");
            }

            if (e.Uri.AbsolutePath.IndexOf("feeds/api/playlists/") != -1) {
                // playlists base url https://gdata.youtube.com/feeds/api/playlists/
                e.Feed = new PlaylistFeed(e.Uri, e.Service);
            } else if (e.Uri.AbsolutePath.IndexOf("feeds/api") != -1 &&
                e.Uri.AbsolutePath.IndexOf("contacts") != -1) {
                // contacts feeds are https://gdata.youtube.com/feeds/api/users/username/contacts
                e.Feed = new FriendsFeed(e.Uri, e.Service);
            } else if (e.Uri.AbsolutePath.IndexOf("feeds/api/users") != -1 &&
                e.Uri.AbsolutePath.IndexOf("playlists") != -1) {
                // user based list of playlists are https://gdata.youtube.com/feeds/api/users/username/playlists
                e.Feed = new PlaylistsFeed(e.Uri, e.Service);
            } else if (e.Uri.AbsolutePath.IndexOf("feeds/api/users") != -1 &&
                e.Uri.AbsolutePath.IndexOf("subscriptions") != -1) {
                // user based list of subscriptions are https://gdata.youtube.com/feeds/api/users/username/subscriptions
                e.Feed = new SubscriptionFeed(e.Uri, e.Service);
            } else if (e.Uri.AbsolutePath.IndexOf("feeds/api/users") != -1 &&
                e.Uri.AbsolutePath.IndexOf("inbox") != -1) {
                // user based list of messages are https://gdata.youtube.com/feeds/api/users/username/inbox
                e.Feed = new MessageFeed(e.Uri, e.Service);
            } else if (e.Uri.AbsolutePath.IndexOf("feeds/api/videos") != -1 &&
                e.Uri.AbsolutePath.IndexOf("comments") != -1) {
                // user based list of comments are https://gdata.youtube.com/feeds/api/videos/videoid/comments
                e.Feed = new CommentsFeed(e.Uri, e.Service);
            } else if (e.Uri.AbsolutePath.IndexOf("feeds/api/users") != -1 &&
                e.Uri.AbsolutePath.IndexOf("uploads") != -1) {
                // user based upload service url https://gdata.youtube.com/feeds/api/users/videoid/uploads
                e.Feed = new YouTubeFeed(e.Uri, e.Service);
            } else if (e.Uri.AbsolutePath.IndexOf("feeds/api/events") != -1 &&
                e.Uri.PathAndQuery.IndexOf("author") != -1) {
                // event feeds https://gdata.youtube.com/feeds/api/events?author=usernames
                e.Feed = new ActivitiesFeed(e.Uri, e.Service);
            } else if (e.Uri.AbsolutePath.IndexOf("feeds/api/users") != -1 &&
                e.Uri.AbsolutePath.IndexOf("friendsactivity") != -1) {
                // event feeds https://gdata.youtube.com/feeds/api/users/default/friendsactivity
                e.Feed = new ActivitiesFeed(e.Uri, e.Service);
            } else if (IsProfileUri(e.Uri)) {
                // user based list of playlists are https://gdata.youtube.com/feeds/api/users/username/playlists
                e.Feed = new ProfileFeed(e.Uri, e.Service);
            } else {
                // everything not detected yet, is a youtubefeed.
                e.Feed = new YouTubeFeed(e.Uri, e.Service);
            }
        }

        private bool IsProfileUri(Uri uri) {
            String str = uri.AbsolutePath;
            if (str.StartsWith("/")) {
                str = str.Substring(1);
            }

            if (str.EndsWith("/")) {
                // remove the last char
                str.Remove(str.Length - 1, 1);
            }

            if (str.StartsWith("feeds/api/users/")) {
                str = str.Substring(16);
                // now there should be one word left, no more slashes
                if (str.IndexOf('/') == -1)
                    return true;
            }

            return false;
        }
    }
}

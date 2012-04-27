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
using Google.GData.YouTube;
using Google.GData.Extensions.MediaRss;
using System.Collections.Generic;

namespace Google.YouTube {
    public class Complaint : Entry {
        /// <summary>
        /// creates the inner contact object when needed
        /// </summary>
        /// <returns></returns>
        protected override void EnsureInnerObject() {
            if (this.AtomEntry == null) {
                this.AtomEntry = new ComplaintEntry();
            }
        }

        /// <summary>
        /// readonly accessor to the typed underlying atom object
        /// </summary>
        public ComplaintEntry ComplaintEntry {
            get {
                return this.AtomEntry as ComplaintEntry;
            }
        }

        /// <summary>
        /// sets the type of the complaint
        /// </summary>
        public ComplaintEntry.ComplaintType Type {
            get {
                if (this.ComplaintEntry != null) {
                    return this.ComplaintEntry.Type;
                }
                return ComplaintEntry.ComplaintType.UNKNOWN;
            }
            set {
                EnsureInnerObject();
                this.ComplaintEntry.Type = value;
            }
        }

        /// <summary>
        /// sets the verbose part of the complaint, stored in the yt:content element
        /// </summary>
        public string ComplaintDescription {
            get {
                if (this.ComplaintEntry != null) {
                    return this.ComplaintEntry.Complaint;
                }
                return null;
            }
            set {
                EnsureInnerObject();
                this.ComplaintEntry.Complaint = value;
            }
        }
    }

    /// <summary>
    /// the Comment entry for a Comments Feed, a feed of Comment for YouTube
    /// </summary>
    public class Comment : Entry {
        /// <summary>
        /// creates the inner contact object when needed
        /// </summary>
        /// <returns></returns>
        protected override void EnsureInnerObject() {
            if (this.AtomEntry == null) {
                this.AtomEntry = new CommentEntry();
            }
        }

        /// <summary>
        /// readonly accessor to the underlying CommentEntry object.
        /// </summary>
        public CommentEntry CommentEntry {
            get {
                return this.AtomEntry as CommentEntry;
            }
        }

        /// <summary>
        /// adds the replyToLinks to this comment
        /// </summary>
        /// <param name="c">The comment this comment is replying to</param>
        public void ReplyTo(Comment c) {
            if (c == null || c.CommentEntry == null) {
                throw new ArgumentNullException("c can not be null or c.CommentEntry can not be null");
            }

            EnsureInnerObject();
            if (this.CommentEntry != null) {
                this.CommentEntry.ReplyTo(c.CommentEntry);
            }
        }
    }

    /// <summary>
    /// the subscription entry for a subscriptionfeed Feed
    /// </summary>
    public class Subscription : Entry {
        /// <summary>
        /// readonly accessor for the SubscriptionEntry that is underneath this object.
        /// </summary>
        /// <returns></returns>
        public SubscriptionEntry SubscriptionEntry {
            get {
                return this.AtomEntry as SubscriptionEntry;
            }
        }

        /// <summary>
        /// creates the inner contact object when needed
        /// </summary>
        /// <returns></returns>
        protected override void EnsureInnerObject() {
            if (this.AtomEntry == null) {
                this.AtomEntry = new SubscriptionEntry();
            }
        }

        /// <summary>
        ///  returns the subscription type
        /// </summary>
        /// <returns></returns>
        public SubscriptionEntry.SubscriptionType Type {
            get {
                EnsureInnerObject();
                return this.SubscriptionEntry.Type;
            }
            set {
                EnsureInnerObject();
                this.SubscriptionEntry.Type = value;
            }
        }

        /// <summary>
        /// The user who is the owner of this subscription
        /// </summary>
        public string UserName {
            get {
                EnsureInnerObject();
                return this.SubscriptionEntry.UserName;
            }
            set {
                EnsureInnerObject();
                this.SubscriptionEntry.UserName = value;
            }
        }

        /// <summary>
        /// if the subscription is a keyword query, this will be the 
        /// subscribed to query term
        /// </summary>
        public string QueryString {
            get {
                EnsureInnerObject();
                return this.SubscriptionEntry.QueryString;
            }
            set {
                EnsureInnerObject();
                this.SubscriptionEntry.QueryString = value;
            }
        }

        /// <summary>
        /// the id of the playlist you are subscriped to
        /// </summary>
        public string PlaylistId {
            get {
                EnsureInnerObject();
                return this.SubscriptionEntry.PlaylistId;
            }
            set {
                EnsureInnerObject();
                this.SubscriptionEntry.PlaylistId = value;
            }
        }

        /// <summary>
        /// the human readable name of the playlist you are subscribed to
        /// </summary>
        public string PlaylistTitle {
            get {
                EnsureInnerObject();
                return this.SubscriptionEntry.PlaylistTitle;
            }
            set {
                EnsureInnerObject();
                this.SubscriptionEntry.PlaylistTitle = value;
            }
        }
    }

    /// <summary>
    /// the Activity entry for an Activities Feed, a feed of activities for the friends/contacts
    /// of the logged in user
    /// </summary>
    /// <returns></returns>
    public class Activity : Entry {
        /// <summary>
        /// creates the inner contact object when needed
        /// </summary>
        /// <returns></returns>
        protected override void EnsureInnerObject() {
            if (this.AtomEntry == null) {
                this.AtomEntry = new ActivityEntry();
            }
        }

        /// <summary>
        /// readonly accessor for the YouTubeEntry that is underneath this object.
        /// </summary>
        /// <returns></returns>
        public ActivityEntry ActivityEntry {
            get {
                return this.AtomEntry as ActivityEntry;
            }
        }

        /// <summary>
        /// specifies a unique ID that YouTube uses to identify a video.
        /// </summary>
        /// <returns></returns>
        public string VideoId {
            get {
                EnsureInnerObject();
                if (this.ActivityEntry.VideoId != null) {
                    return this.ActivityEntry.VideoId.Value;
                }
                return null;
            }
        }

        /// <summary>
        /// the type of the activity
        /// </summary>
        public ActivityType Type {
            get {
                EnsureInnerObject();
                return this.ActivityEntry.Type;
            }
        }

        /// <summary>
        /// the username of the friend who was added,
        /// or the user whom was subscribed to
        /// </summary>
        public string Username {
            get {
                EnsureInnerObject();
                if (this.ActivityEntry.Username != null) {
                    return this.ActivityEntry.Username.Value;
                }
                return null;
            }
        }
    }

    /// <summary>
    /// the Playlist entry for a Playlist Feed, a feed of Playlist for YouTube
    /// </summary>
    public class Playlist : Entry {
        /// <summary>
        /// creates the inner contact object when needed
        /// </summary>
        /// <returns></returns>
        protected override void EnsureInnerObject() {
            if (this.AtomEntry == null) {
                this.AtomEntry = new PlaylistsEntry();
            }
        }

        /// <summary>
        /// returns the internal atomentry as a PlaylistsEntry
        /// </summary>
        /// <returns></returns>
        public PlaylistsEntry PlaylistsEntry {
            get {
                return this.AtomEntry as PlaylistsEntry;
            }
        }

        /// <summary>
        /// specifies the number of entries in a playlist feed. This tag appears in the entries in a 
        /// playlists feed, where each entry contains information about a single playlist.
        /// </summary>
        /// <returns></returns>
        public int CountHint {
            get {
                EnsureInnerObject();
                return this.PlaylistsEntry.CountHint;
            }
        }
    }

    /// <summary>
    /// the Show entry in feed&lt;Shows&gt; for YouTube
    /// </summary>
    public class Show : Entry {
        /// <summary>
        /// creates the inner show object when needed
        /// </summary>
        /// <returns></returns>
        protected override void EnsureInnerObject() {
            if (this.AtomEntry == null) {
                this.AtomEntry = new ShowEntry();
            }
        }

        /// <summary>
        /// returns the internal atomentry as a ShowEntry
        /// </summary>
        /// <returns></returns>
        public ShowEntry ShowEntry {
            get {
                return this.AtomEntry as ShowEntry;
            }
        }

        /// <summary>
        /// contains a summary or description of a show.
        /// </summary>
        /// <returns></returns>
        public string Description {
            get {
                if (this.ShowEntry != null &&
                    this.ShowEntry.Media != null &&
                    this.ShowEntry.Media.Description != null) {
                    return this.ShowEntry.Media.Description.Value;
                }
                return null;
            }
            set {
                EnsureInnerObject();
                if (this.ShowEntry.Media == null) {
                    this.ShowEntry.Media = new Google.GData.YouTube.MediaGroup();
                }
                if (this.ShowEntry.Media.Description == null) {
                    this.ShowEntry.Media.Description = new MediaDescription();
                }
                this.ShowEntry.Media.Description.Value = value;
            }
        }

        /// <summary>
        /// returns the URL for a feed of show seasons
        /// </summary>
        /// <returns></returns>
        public string SeasonUrl {
            get {
                if (this.ShowEntry != null &&
                    this.ShowEntry.FeedLink != null) {
                    try {
                        return this.ShowEntry.FeedLink.Href;
                    } catch (FormatException) {
                        return null;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// returns the keywords for the video, see MediaKeywords for more
        /// </summary>
        /// <returns></returns>
        public string Keywords {
            get {
                if (this.ShowEntry != null && this.ShowEntry.Media != null && this.ShowEntry.Media.Keywords != null) {
                    return this.ShowEntry.Media.Keywords.Value;
                }
                return null;
            }
            set {
                EnsureInnerObject();
                if (this.ShowEntry.Media == null) {
                    this.ShowEntry.Media = new Google.GData.YouTube.MediaGroup();
                }
                if (this.ShowEntry.Media.Keywords == null) {
                    this.ShowEntry.Media.Keywords = new MediaKeywords();
                }
                this.ShowEntry.Media.Keywords.Value = value;
            }
        }

        /// <summary>
        /// the title of the show. Overloaded to keep entry.title and the media.title 
        /// in sync. 
        /// </summary>
        /// <returns></returns>
        public override string Title {
            get {
                return base.Title;
            }
            set {
                base.Title = value;
                EnsureInnerObject();
                if (this.ShowEntry.Media == null) {
                    this.ShowEntry.Media = new Google.GData.YouTube.MediaGroup();
                }
                if (this.ShowEntry.Media.Title == null) {
                    this.ShowEntry.Media.Title = new MediaTitle();
                }
                this.ShowEntry.Media.Title.Value = value;
            }
        }

        /// <summary>
        /// returns the collection of thumbnails for the show
        /// </summary>
        /// <returns></returns>
        public ExtensionCollection<MediaThumbnail> Thumbnails {
            get {
                if (this.ShowEntry != null) {
                    if (this.ShowEntry.Media == null) {
                        this.ShowEntry.Media = new Google.GData.YouTube.MediaGroup();
                    }
                    return this.ShowEntry.Media.Thumbnails;
                }
                return null;
            }
        }
    }

    /// <summary>
    /// the Show entry in feed&lt;Shows&gt; for YouTube
    /// </summary>
    public class ShowSeason : Entry {
        /// <summary>
        /// creates the inner show season object when needed
        /// </summary>
        /// <returns></returns>
        protected override void EnsureInnerObject() {
            if (this.AtomEntry == null) {
                this.AtomEntry = new ShowSeasonEntry();
            }
        }

        /// <summary>
        /// returns the internal atomentry as a ShowSeasonEntry
        /// </summary>
        /// <returns></returns>
        public ShowSeasonEntry ShowSeasonEntry {
            get {
                return this.AtomEntry as ShowSeasonEntry;
            }
        }

        /// <summary>
        /// returns the count of expected Clips for the season
        /// </summary>
        /// <returns></returns>
        public int ClipCount {
            get {
                EnsureInnerObject();
                if (this.ShowSeasonEntry.ClipLink != null) {
                    return this.ShowSeasonEntry.ClipLink.CountHint;
                }
                return 0;
            }
        }

        /// <summary>
        /// returns the feed URL for season Clips
        /// </summary>
        /// <returns></returns>
        public string ClipUrl {
            get {
                EnsureInnerObject();
                if (this.ShowSeasonEntry.ClipLink != null) {
                    return this.ShowSeasonEntry.ClipLink.Href;
                }
                return null;
            }
        }

        /// <summary>
        /// returns the count of expected Episodes for the season
        /// </summary>
        /// <returns></returns>
        public int EpisodeCount {
            get {
                EnsureInnerObject();
                if (this.ShowSeasonEntry.EpisodeLink != null) {
                    return this.ShowSeasonEntry.EpisodeLink.CountHint;
                }
                return 0;
            }
        }

        /// <summary>
        /// returns the feed URL for season Episodes
        /// </summary>
        /// <returns></returns>
        public string EpisodeUrl {
            get {
                EnsureInnerObject();
                if (this.ShowSeasonEntry.EpisodeLink != null) {
                    return this.ShowSeasonEntry.EpisodeLink.Href;
                }
                return null;
            }
        }
    }

    /// <summary>the Video Entry in feed&lt;Videos&gt; for YouTube
    /// </summary> 
    public class Video : Entry {
        /// <summary>
        /// creates the inner contact object when needed
        /// </summary>
        /// <returns></returns>
        protected override void EnsureInnerObject() {
            if (this.AtomEntry == null) {
                this.AtomEntry = new YouTubeEntry();
            }
        }

        /// <summary>
        /// readonly accessor for the YouTubeEntry that is underneath this object.
        /// </summary>
        /// <returns></returns>
        public YouTubeEntry YouTubeEntry {
            get {
                return this.AtomEntry as YouTubeEntry;
            }
        }

        /// <summary>
        /// specifies a unique ID that YouTube uses to identify a video.
        /// </summary>
        /// <returns></returns>
        public string VideoId {
            get {
                EnsureInnerObject();
                return this.YouTubeEntry.VideoId;
            }
            set {
                EnsureInnerObject();
                this.YouTubeEntry.VideoId = value;
            }
        }

        /// <summary>
        /// contains a summary or description of a video. This field is required in requests to 
        /// upload or update a video's metadata. The description should be sentence-based, 
        /// rather than a list of keywords, and may be displayed in search results. The description has a 
        /// maximum length of 5000 characters and may contain all valid UTF-8 characters except &lt; and &gt; 
        /// </summary>
        /// <returns></returns>
        public string Description {
            get {
                if (this.YouTubeEntry != null &&
                    this.YouTubeEntry.Media != null &&
                    this.YouTubeEntry.Media.Description != null) {
                    return this.YouTubeEntry.Media.Description.Value;
                }
                return null;
            }
            set {
                EnsureInnerObject();
                if (this.YouTubeEntry.Media == null) {
                    this.YouTubeEntry.Media = new Google.GData.YouTube.MediaGroup();
                }
                if (this.YouTubeEntry.Media.Description == null) {
                    this.YouTubeEntry.Media.Description = new MediaDescription();
                }
                this.YouTubeEntry.Media.Description.Value = value;
            }
        }

        /// <summary>
        /// the title of the Video. Overloaded to keep entry.title and the media.title 
        ///  in sync. 
        /// </summary>
        /// <returns></returns>
        public override string Title {
            get {
                return base.Title;
            }
            set {
                base.Title = value;
                EnsureInnerObject();
                if (this.YouTubeEntry.Media == null) {
                    this.YouTubeEntry.Media = new Google.GData.YouTube.MediaGroup();
                }
                if (this.YouTubeEntry.Media.Title == null) {
                    this.YouTubeEntry.Media.Title = new MediaTitle();
                }
                this.YouTubeEntry.Media.Title.Value = value;
            }
        }

        /// <summary>
        /// returns the categories for the video
        /// </summary>
        /// <returns></returns>
        public ExtensionCollection<MediaCategory> Tags {
            get {
                EnsureInnerObject();
                if (this.YouTubeEntry.Media == null) {
                    this.YouTubeEntry.Media = new Google.GData.YouTube.MediaGroup();
                }
                return this.YouTubeEntry.Media.Categories;
            }
        }

        /// <summary>
        /// returns the keywords for the video, see MediaKeywords for more
        /// </summary>
        /// <returns></returns>
        public string Keywords {
            get {
                if (this.YouTubeEntry != null) {
                    if (this.YouTubeEntry.Media != null) {
                        if (this.YouTubeEntry.Media.Keywords != null) {
                            return this.YouTubeEntry.Media.Keywords.Value;
                        }
                    }
                }
                return null;
            }
            set {
                EnsureInnerObject();
                if (this.YouTubeEntry.Media == null) {
                    this.YouTubeEntry.Media = new Google.GData.YouTube.MediaGroup();
                }
                if (this.YouTubeEntry.Media.Keywords == null) {
                    this.YouTubeEntry.Media.Keywords = new MediaKeywords();
                }
                this.YouTubeEntry.Media.Keywords.Value = value;
            }
        }

        /// <summary>
        /// returns the collection of thumbnails for the video
        /// </summary>
        /// <returns></returns>
        public ExtensionCollection<MediaThumbnail> Thumbnails {
            get {
                if (this.YouTubeEntry != null) {
                    if (this.YouTubeEntry.Media == null) {
                        this.YouTubeEntry.Media = new Google.GData.YouTube.MediaGroup();
                    }
                    return this.YouTubeEntry.Media.Thumbnails;
                }
                return null;
            }
        }

        /// <summary>
        /// returns the collection of thumbnails for the vido
        /// </summary>
        /// <returns></returns>
        public ExtensionCollection<Google.GData.YouTube.MediaContent> Contents {
            get {
                if (this.YouTubeEntry != null) {
                    if (this.YouTubeEntry.Media == null) {
                        this.YouTubeEntry.Media = new Google.GData.YouTube.MediaGroup();
                    }
                    return this.YouTubeEntry.Media.Contents;
                }
                return null;
            }
        }

        /// <summary>
        /// specifies a URL where the full-length video is available through a media player that runs 
        /// inside a web browser. In a YouTube Data API response, this specifies the URL for the page 
        /// on YouTube's website that plays the video
        /// </summary>
        /// <returns></returns>
        public Uri WatchPage {
            get {
                if (this.YouTubeEntry != null &&
                    this.YouTubeEntry.Media != null &&
                    this.YouTubeEntry.Media.Player != null) {
                    return new Uri(this.YouTubeEntry.Media.Player.Url);
                }
                return null;
            }
        }

        /// <summary>
        /// identifies the owner of a video.
        /// </summary>
        /// <returns></returns>
        public string Uploader {
            get {
                if (this.YouTubeEntry != null &&
                    this.YouTubeEntry.Media != null &&
                    this.YouTubeEntry.Media.Credit != null) {
                    return this.YouTubeEntry.Media.Credit.Value;
                }
                return null;
            }
            set {
                EnsureInnerObject();
                if (this.YouTubeEntry.Media == null) {
                    this.YouTubeEntry.Media = new Google.GData.YouTube.MediaGroup();
                }
                if (this.YouTubeEntry.Media.Credit == null) {
                    this.YouTubeEntry.Media.Credit = new Google.GData.YouTube.MediaCredit();
                }
                this.YouTubeEntry.Media.Credit.Value = value;
            }
        }

        /// <summary>
        /// access to the Media group subelement
        /// </summary>
        public Google.GData.YouTube.MediaGroup Media {
            get {
                if (this.YouTubeEntry != null) {
                    return this.YouTubeEntry.Media;
                }
                return null;
            }
            set {
                EnsureInnerObject();
                this.YouTubeEntry.Media = value;
            }
        }

        /// <summary>
        /// returns the viewcount for the video
        /// </summary>
        /// <returns></returns>
        public int ViewCount {
            get {
                if (this.YouTubeEntry != null && this.YouTubeEntry.Statistics != null) {
                    return Int32.Parse(this.YouTubeEntry.Statistics.ViewCount);
                }
                return -1;
            }
        }

        /// <summary>
        /// returns the number of comments for the video
        /// </summary>
        /// <returns></returns>
        public int CommmentCount {
            get {
                if (this.YouTubeEntry != null &&
                    this.YouTubeEntry.Comments != null &&
                    this.YouTubeEntry.Comments.FeedLink != null) {
                    return this.YouTubeEntry.Comments.FeedLink.CountHint;
                }
                return -1;
            }
        }

        /// <summary>
        /// returns the rating for a video
        /// </summary>
        /// <returns></returns>
        public int Rating {
            get {
                if (this.YouTubeEntry != null &&
                    this.YouTubeEntry.Rating != null) {
                    try {
                        return this.YouTubeEntry.Rating.Value;
                    } catch (FormatException) {
                        return -1;
                    }
                }
                return -1;
            }
            set {
                EnsureInnerObject();
                if (this.YouTubeEntry.Rating == null) {
                    this.YouTubeEntry.Rating = new Rating();
                }
                this.YouTubeEntry.Rating.Value = (int)value;
            }
        }

        /// <summary>
        /// returns the average rating for a video
        /// </summary>
        /// <returns></returns>
        public double RatingAverage {
            get {
                if (this.YouTubeEntry != null &&
                    this.YouTubeEntry.Rating != null) {
                    return this.YouTubeEntry.Rating.Average;
                }
                return -1;
            }
        }

        /// <summary>
        /// returns the ratings Uri, to post a rating to.
        /// </summary>
        public Uri RatingsUri {
            get {
                Uri ratings = null;
                if (this.YouTubeEntry != null) {
                    AtomUri r = this.YouTubeEntry.RatingsLink;
                    if (r != null) {
                        ratings = new Uri(r.ToString());
                    }
                }
                return ratings;
            }
        }

        /// <summary>
        /// returns the response Uri, to post a video response to.
        /// </summary>
        public Uri ResponseUri {
            get {
                Uri response = null;
                if (this.YouTubeEntry != null) {
                    AtomUri r = this.YouTubeEntry.VideoResponsesUri.ToString();
                    if (r != null) {
                        response = new Uri(r.ToString());
                    }
                }
                return response;
            }
        }

        /// <summary>
        /// returns the complaint Uri, to post a complaint to.
        /// </summary>
        public Uri ComplaintUri {
            get {
                Uri uri = null;
                if (this.YouTubeEntry != null) {
                    AtomUri r = this.YouTubeEntry.ComplaintUri;
                    if (r != null) {
                        uri = new Uri(r.ToString());
                    }
                }
                return uri;
            }
        }

        /// <summary>
        /// boolean property shortcut to set the mediagroup/yt:private element. Setting this to true 
        /// adds the element, if not already there (otherwise nothing happens)
        /// setting this to false, removes it
        /// </summary>
        /// <returns></returns>
        public bool Private {
            get {
                if (this.YouTubeEntry != null) {
                    return this.YouTubeEntry.Private;
                }
                return false;
            }
            set {
                EnsureInnerObject();
                this.YouTubeEntry.Private = value;
            }
        }

        /// <summary>
        /// The yt:state tag contains information that describes the status of a video. 
        /// Video entries that contain a yt:state tag are not playable. 
        /// For videos that failed to upload or were rejected after the upload process, the reasonCode 
        /// attribute and the tag value provide insight into the reason for the upload problem. 
        /// Deleted entries only appear in playlist and inbox feeds and are only visible to the playlist 
        /// or inbox owner.
        /// </summary>
        public State Status {
            get {
                EnsureInnerObject();
                return this.YouTubeEntry.State;
            }
        }

        public int? EpisodeNumber {
            get {
                EnsureInnerObject();
                if (this.YouTubeEntry.Episode != null) {
                    return this.YouTubeEntry.Episode.Number;
                }
                return null;
            }
        }
    }

    /// <summary>
    /// subclass of a video to represent a video that is part of a playlist
    /// </summary>
    public class PlayListMember : Video {
        /// <summary>
        /// creates the inner contact object when needed
        /// </summary>
        /// <returns></returns>
        protected override void EnsureInnerObject() {
            if (this.AtomEntry == null) {
                this.AtomEntry = new PlaylistEntry();
            }
        }

        /// <summary>
        /// readonly accessor for the YouTubeEntry that is underneath this object.
        /// </summary>
        /// <returns></returns>
        public PlaylistEntry PlaylistEntry {
            get {
                return this.AtomEntry as PlaylistEntry;
            }
        }

        /// <summary>
        /// if the video is a playlist reference, gets and sets its position in the playlist
        /// </summary>
        public int Position {
            get {
                if (this.PlaylistEntry != null) {
                    return this.PlaylistEntry.Position;
                }
                return -1;
            }
            set {
                EnsureInnerObject();
                this.PlaylistEntry.Position = value;
            }
        }
    }

    /// <summary>
    /// YouTube specific class for request settings,
    /// adds support for developer key and clientid
    /// </summary>
    /// <returns></returns>
    public class YouTubeRequestSettings : RequestSettings {
        private string developerKey;

        /// <summary>
        /// A constructor for a readonly scenario.
        /// </summary>
        /// <param name="applicationName">The name of the application</param>
        /// <param name="developerKey">the developer key to use</param>
        /// <returns></returns>
        public YouTubeRequestSettings(string applicationName, string developerKey)
            : base(applicationName) {
            this.developerKey = developerKey;
        }

        /// <summary>
        /// A constructor for a client login scenario
        /// </summary>
        /// <param name="applicationName">The name of the application</param>
        /// <param name="developerKey">the developer key to use</param>
        /// <param name="userName">the username</param>
        /// <param name="passWord">the password</param>
        /// <returns></returns>
        public YouTubeRequestSettings(string applicationName, string developerKey, string userName, string passWord)
            : base(applicationName, userName, passWord) {
            this.developerKey = developerKey;
        }

        /// <summary>
        /// a constructor for a web application authentication scenario        
        /// </summary>
        /// <param name="applicationName">The name of the application</param>
        /// <param name="developerKey">the developer key to use</param>
        /// <param name="authSubToken">the authentication token</param>
        /// <returns></returns>
        public YouTubeRequestSettings(string applicationName, string developerKey, string authSubToken)
            : base(applicationName, authSubToken) {
            this.developerKey = developerKey;
        }

        /// <summary>
        ///  a constructor for OpenAuthentication login use cases using 2 or 3 legged oAuth
        /// </summary>
        /// <param name="applicationName">The name of the application</param>
        /// <param name="developerKey">the developer key to use</param>
        /// <param name="consumerKey">the consumerKey to use</param>
        /// <param name="consumerSecret">the consumerSecret to use</param>
        /// <param name="token">The token to be used</param>
        /// <param name="tokenSecret">The tokenSecret to be used</param>
        /// <param name="user">the username to use</param>
        /// <param name="domain">the domain to use</param>
        /// <returns></returns>
        public YouTubeRequestSettings(string applicationName,
            string developerKey,
            string consumerKey, string consumerSecret,
            string token, string tokenSecret,
            string user, string domain)
            : base(applicationName, consumerKey, consumerSecret,
            token, tokenSecret, user, domain) {
            this.developerKey = developerKey;
        }

        /// <summary>
        /// returns the developer key
        /// </summary>
        /// <returns></returns>
        public string DeveloperKey {
            get {
                return this.developerKey;
            }
        }
    }

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
    ///  <example>
    ///         The following code illustrates a possible use of   
    ///          the <c>YouTubeRequest</c> object:  
    ///          <code>    
    ///           YouTubeRequestSettings settings = new YouTubeRequestSettings("yourApp", "yourClient", "yourKey");
    ///            settings.PageSize = 50; 
    ///            settings.AutoPaging = true;
    ///             YouTubeRequest f = new YouTubeRequest(settings);
    ///         Feed<Video> feed = f.GetStandardFeed(YouTubeQuery.MostPopular);
    ///     
    ///         foreach (Video v in feed.Entries)
    ///         {
    ///             Feed<Comment> list= f.GetComments(v);
    ///             foreach (Comment c in list.Entries)
    ///             {
    ///                 Console.WriteLine(c.Title);
    ///             }
    ///         }
    ///  </code>
    ///  </example>
    public class YouTubeRequest : FeedRequest<YouTubeService> {
        /// <summary>
        /// default constructor for a YouTubeRequest
        /// </summary>
        /// <param name="settings"></param>
        public YouTubeRequest(YouTubeRequestSettings settings)
            : base(settings) {
            if (settings.DeveloperKey != null) {
                this.Service = new YouTubeService(settings.Application, settings.DeveloperKey);
            } else {
                this.Service = new YouTubeService(settings.Application);
            }

            PrepareService();
        }

        /// <summary>
        /// returns a Feed of videos for a given username
        /// </summary>
        /// <param name="user">the username</param>
        /// <returns>a feed of Videos</returns>
        public Feed<Video> GetVideoFeed(string user) {
            YouTubeQuery q = PrepareQuery<YouTubeQuery>(YouTubeQuery.CreateUserUri(user));
            return PrepareFeed<Video>(q);
        }

        /// <summary>
        /// returns one of the youtube default feeds. 
        /// </summary>
        /// <param name="feedspec">the string representation of the URI to use</param>
        /// <returns>a feed of Videos</returns>
        public Feed<Video> GetStandardFeed(string feedspec) {
            YouTubeQuery q = PrepareQuery<YouTubeQuery>(feedspec);
            return PrepareFeed<Video>(q);
        }

        /// <summary>
        /// returns the youtube standard show feed. 
        /// </summary>
        /// <param name="feedspec">the string representation of the URI to use</param>
        /// <returns>a feed of Videos</returns>
        public Feed<Show> GetStandardShowFeed(string feedspec) {
            YouTubeQuery q = PrepareQuery<YouTubeQuery>(feedspec);
            return PrepareFeed<Show>(q);
        }

        /// <summary>
        /// returns a Feed of favorite videos for a given username
        /// </summary>
        /// <param name="user">the username</param>
        /// <returns>a feed of Videos</returns>
        public Feed<Video> GetFavoriteFeed(string user) {
            YouTubeQuery q = PrepareQuery<YouTubeQuery>(YouTubeQuery.CreateFavoritesUri(user));
            return PrepareFeed<Video>(q);
        }

        /// <summary>
        /// returns a Feed of subscriptions for a given username
        /// </summary>
        /// <param name="user">the username</param>
        /// <returns>a feed of Videos</returns>
        public Feed<Subscription> GetSubscriptionsFeed(string user) {
            YouTubeQuery q = PrepareQuery<YouTubeQuery>(YouTubeQuery.CreateSubscriptionUri(user));
            return PrepareFeed<Subscription>(q);
        }

        /// <summary>
        /// returns a Feed of playlists for a given username
        /// </summary>
        /// <param name="user">the username</param>
        /// <returns>a feed of Videos</returns>
        public Feed<Playlist> GetPlaylistsFeed(string user) {
            YouTubeQuery q = PrepareQuery<YouTubeQuery>(YouTubeQuery.CreatePlaylistsUri(user));
            return PrepareFeed<Playlist>(q);
        }

        /// <summary>
        /// returns a Feed of shows for a given username
        /// </summary>
        /// <param name="user">the username</param>
        /// <returns>a feed of Shows</returns>
        public Feed<Show> GetShowsFeed(string user) {
            YouTubeQuery q = PrepareQuery<YouTubeQuery>(YouTubeQuery.CreateShowsUri(user));
            return PrepareFeed<Show>(q);
        }

        /// <summary>
        /// returns a Feed of seasons for a given show
        /// </summary>
        /// <param name="user">the username</param>
        /// <returns>a feed of Shows</returns>
        public Feed<ShowSeason> GetShowSeasonFeed(string uri) {
            YouTubeQuery q = PrepareQuery<YouTubeQuery>(uri);
            return PrepareFeed<ShowSeason>(q);
        }

        /// <summary>
        /// returns a Feed of videos for a given show season
        /// </summary>
        /// <param name="user">the username</param>
        /// <returns>a feed of Shows</returns>
        public Feed<Video> GetShowSeasonVideos(string uri) {
            YouTubeQuery q = PrepareQuery<YouTubeQuery>(uri);
            return PrepareFeed<Video>(q);
        }

        /// <summary>
        /// returns the related videos for a given video
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public Feed<Video> GetRelatedVideos(Video v) {
            if (v.YouTubeEntry != null) {
                if (v.YouTubeEntry.RelatedVideosUri != null) {
                    YouTubeQuery q = PrepareQuery<YouTubeQuery>(v.YouTubeEntry.RelatedVideosUri.ToString());
                    return PrepareFeed<Video>(q);
                }
            }
            return null;
        }

        /// <summary>
        ///  gets the response videos for a given video
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public Feed<Video> GetResponseVideos(Video v) {
            if (v.YouTubeEntry != null) {
                if (v.YouTubeEntry.VideoResponsesUri != null) {
                    YouTubeQuery q = PrepareQuery<YouTubeQuery>(v.YouTubeEntry.VideoResponsesUri.ToString());
                    return PrepareFeed<Video>(q);
                }
            }
            return null;
        }

        /// <summary>
        /// gets the comments for a given video
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public Feed<Comment> GetComments(Video v) {
            if (v.YouTubeEntry != null &&
                v.YouTubeEntry.Comments != null &&
                v.YouTubeEntry.Comments.FeedLink != null &&
                v.YouTubeEntry.Comments.FeedLink.Href != null) {
                YouTubeQuery q = PrepareQuery<YouTubeQuery>(v.YouTubeEntry.Comments.FeedLink.Href);
                return PrepareFeed<Comment>(q);
            }
            return new Feed<Comment>(null);
        }

        /// <summary>
        /// gets the activities that your contacts/friends did recently 
        /// </summary>
        /// <returns></returns>
        public Feed<Activity> GetActivities() {
            return GetActivities(DateTime.MinValue);
        }

        /// <summary>
        /// gets the activities for a list of users
        /// </summary>
        /// <param name="youTubeUsers">The list of youtube user ids</param>
        /// <returns></returns>
        public Feed<Activity> GetActivities(List<string> youTubeUsers) {
            return GetActivities(youTubeUsers, DateTime.MinValue);
        }

        /// <summary>
        /// gets the activities for a list of users
        /// </summary>
        /// <param name="youTubeUsers">The list of youtube user ids</param>
        /// <returns></returns>
        public Feed<Activity> GetActivities(List<string> youTubeUsers, DateTime since) {
            if (this.Settings == null) {
                return new Feed<Activity>(null);
            }

            UserActivitiesQuery q = new UserActivitiesQuery();
            q.ModifiedSince = since;
            q.Authors = youTubeUsers;
            PrepareQuery(q);
            return PrepareFeed<Activity>(q);
        }

        /// <summary>
        /// gets the activities that your contacts/friends did recently, from the 
        /// given datetime point
        /// </summary>
        /// <returns></returns>
        public Feed<Activity> GetActivities(DateTime since) {
            if (this.Settings == null) {
                return new Feed<Activity>(null);
            }

            ActivitiesQuery q = new ActivitiesQuery();
            q.ModifiedSince = since;
            PrepareQuery(q);
            return PrepareFeed<Activity>(q);
        }

        /** 
           <summary>
            returns the feed of videos for a given playlist
           </summary>
            <example>
                The following code illustrates a possible use of   
                the <c>GetPlaylist</c> method:  
                <code>    
                  YouTubeRequestSettings settings = new YouTubeRequestSettings("yourApp", "yourClient", "yourKey", "username", "pwd");
                  YouTubeRequest f = new YouTubeRequest(settings);
                  Feed&lt;Playlist&gt; feed = f.GetPlaylistsFeed(null);
                </code>
            </example>
            <param name="p">the playlist to get the videos for</param>
            <returns></returns>
        */
        public Feed<PlayListMember> GetPlaylist(Playlist p) {
            if (p.AtomEntry != null &&
                p.AtomEntry.Content != null &&
                p.AtomEntry.Content.AbsoluteUri != null) {
                YouTubeQuery q = PrepareQuery<YouTubeQuery>(p.AtomEntry.Content.AbsoluteUri);
                return PrepareFeed<PlayListMember>(q);
            }
            return new Feed<PlayListMember>(null);
        }

        /// <summary>
        /// uploads or inserts a new video for the default authenticated user.
        /// </summary>
        /// <param name="v">the created video to be used</param>
        /// <returns></returns>
        public Video Upload(Video v) {
            return Upload(null, v);
        }

        /// <summary>
        /// uploads or inserts a new video for a given user.
        /// </summary>
        /// <param name="userName">if this is null the default authenticated user will be used</param>
        /// <param name="v">the created video to be used</param>
        /// <returns></returns>
        public Video Upload(string userName, Video v) {
            Video rv = null;
            YouTubeEntry e = this.Service.Upload(userName, v.YouTubeEntry);
            if (e != null) {
                rv = new Video();
                rv.AtomEntry = e;
            }
            return rv;
        }

        /// <summary>
        /// creates the form upload token for a video
        /// </summary>
        /// <param name="v">the created video to be used</param>
        /// <returns></returns>
        public FormUploadToken CreateFormUploadToken(Video v) {
            if (v.YouTubeEntry.MediaSource != null) {
                throw new ArgumentException("The Video should not have a media file attached to it");
            }
            return this.Service.FormUpload(v.YouTubeEntry);
        }

        /// <summary>
        /// returns the video this activity was related to
        /// </summary>
        /// <param name="activity"></param>
        /// <returns></returns>
        public Video GetVideoForActivity(Activity activity) {
            Video rv = null;

            if (activity.ActivityEntry != null) {
                AtomUri address = activity.ActivityEntry.VideoLink;
                YouTubeQuery q = PrepareQuery<YouTubeQuery>(address.ToString());
                YouTubeFeed f = this.Service.Query(q);

                if (f != null && f.Entries.Count > 0) {
                    rv = new Video();
                    rv.AtomEntry = f.Entries[0];
                }
            }

            return rv;
        }

        /// <summary>
        /// adds a comment to a video
        /// </summary>
        /// <param name="v">the video you want to comment on</param>
        /// <param name="c">the comment you want to post</param>
        /// <returns></returns>
        public Comment AddComment(Video v, Comment c) {
            Comment rc = null;

            if (v.YouTubeEntry != null &&
                v.YouTubeEntry.Comments != null &&
                v.YouTubeEntry.Comments.FeedLink != null) {
                Uri target = CreateUri(v.YouTubeEntry.Comments.FeedLink.Href);
                rc = new Comment();
                rc.AtomEntry = this.Service.Insert(target, c.AtomEntry);
            }

            return rc;
        }

        /// <summary>
        /// adds a video to an existing playlist
        /// </summary>
        /// <param name="m">the new playlistmember</param>
        /// <param name="p">the playlist to add tot</param>
        /// <returns></returns>
        public PlayListMember AddToPlaylist(Playlist p, PlayListMember m) {
            PlayListMember newMember = null;

            if (p.PlaylistsEntry != null &&
                p.PlaylistsEntry.Content != null &&
                p.PlaylistsEntry.Content.Src != null) {
                Uri target = CreateUri(p.PlaylistsEntry.Content.Src.Content);
                newMember = new PlayListMember();
                newMember.AtomEntry = this.Service.Insert(target, m.AtomEntry);
            }

            return newMember;
        }

        /// <summary>
        /// Takes a list of activities, and gets the video meta data from youtube 
        /// for those activites that identify a video
        /// </summary>
        /// <param name="list">a list of activities</param>
        /// <returns>a video feed, with no entries, if there were no video related activities</returns>
        public Feed<Video> GetVideoMetaData(List<Activity> list) {
            Feed<Video> meta = null;
            if (list.Count > 0) {
                List<Video> videos = new List<Video>();

                foreach (Activity a in list) {
                    if (a.VideoId != null) {
                        Video v = new Video();
                        v.Id = YouTubeQuery.CreateVideoUri(a.VideoId);
                        videos.Add(v);
                    }
                }

                if (videos.Count > 0) {
                    meta = this.Batch(videos, CreateUri(YouTubeQuery.BatchVideoUri), GDataBatchOperationType.query);
                }
            }

            return meta == null ? new Feed<Video>(null) : meta;
        }

        /// <summary>
        /// returns a single Video (the first) from that stream. Usefull to parse insert/update 
        /// response streams
        /// </summary>
        /// <param name="inputStream"></param>
        /// <returns></returns>
        public Video ParseVideo(Stream inputStream) {
            return ParseEntry<Video>(inputStream, new Uri(YouTubeQuery.DefaultVideoUri));
        }
    }
}


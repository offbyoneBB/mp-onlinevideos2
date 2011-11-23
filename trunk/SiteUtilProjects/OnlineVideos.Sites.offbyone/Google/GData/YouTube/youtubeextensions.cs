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
using System.Xml;
using Google.GData.Client;
using Google.GData.Extensions;
using Google.GData.Extensions.MediaRss;
using System.Globalization;
using System.IO;

namespace Google.GData.YouTube {
    /// <summary>
    /// short table to hold the namespace and the prefix
    /// </summary>
    public class YouTubeNameTable {
        /// <summary>static string to specify the YouTube namespace supported</summary>
        public const string NSYouTube = "http://gdata.youtube.com/schemas/2007";
        /// <summary>static string to specify the Google YouTube prefix used</summary>
        public const string ytPrefix = "yt";
        /// <summary>static string for the ratings relationship</summary>
        public const string RatingsRelationship = NSYouTube + "#video.ratings";
        /// <summary>static string for the in reply to  relationship</summary>
        public const string ReplyToRelationship = NSYouTube + "#in-reply-to";

        /// <summary>string for the video kind category</summary>
        public const string KIND_VIDEO = NSYouTube + "#video";
        /// <summary>string for the complaint kind category</summary>
        public const string KIND_COMPLAINT = NSYouTube + "#complaint";
        /// <summary>string for the comment kind category</summary>
        public const string KIND_COMMENT = NSYouTube + "#comment";
        /// <summary>string for the playlistLink kind category</summary>
        public const string KIND_PLAYLIST_LINK = NSYouTube + "#playlistLink";
        /// <summary>string for the subscription kind category</summary>
        public const string KIND_SUBSCRIPTION = NSYouTube + "#subscription";
        /// <summary>string for the friend kind category</summary>
        public const string KIND_FRIEND = NSYouTube + "#friend";
        /// <summary>string for the rating kind category</summary>
        public const string KIND_RATING = NSYouTube + "#rating";
        /// <summary>string for the userProfile kind category</summary>
        public const string KIND_USER_PROFILE = NSYouTube + "#userProfile";
        /// <summary>string for the playlist kind category</summary>
        public const string KIND_PLAYLIST = NSYouTube + "#playlist";
        /// <summary>string for the videoMessage kind category</summary>
        public const string KIND_VIDEO_MESSAGE = NSYouTube + "#videoMessage";

        /// <summary>
        /// The schema used for friends entries
        /// </summary>
        public const string FriendsCategorySchema = NSYouTube + "/contact.cat";

        /// <summary>
        /// The schema used for subscription entries
        /// </summary>
        public const string SubscriptionCategorySchema = NSYouTube + "/subscriptiontypes.cat";

        /// <summary>
        /// The schema used for complaint entries
        /// </summary>
        public const string ComplaintCategorySchema = NSYouTube + "/complaint-reasons.cat";

        /// <summary>
        /// The schema used for user events  entries
        /// </summary>
        public const string EventsCategorySchema = NSYouTube + "/userevents.cat";

        /// <summary>
        /// string for the user rated category term
        /// </summary>
        public const string VideoRatedCategory = "video_rated";

        /// <summary>
        /// string for the user shared category term
        /// </summary>
        public const string VideoSharedCategory = "video_shared";

        /// <summary>
        /// string for the user uploaded category term
        /// </summary>
        public const string VideoUploadedCategory = "video_uploaded";

        /// <summary>
        /// string for the user favorited category term
        /// </summary>
        public const string VideoFavoritedCategory = "video_favorited";
        /// <summary>
        /// string for the user commented category term
        /// </summary>
        public const string VideoCommentedCategory = "video_commented";
        /// <summary>
        /// string for the user friend added category term
        /// </summary>
        public const string FriendAddedCategory = "friend_added";
        /// <summary>
        /// string for the user subscriptoin added category term
        /// </summary>
        public const string UserSubscriptionAddedCategory = "user_subscription_added";

        /// <summary>
        /// age element string
        /// </summary>
        public const string Age = "age";
        /// <summary>
        /// books element string
        /// </summary>
        public const string Books = "books";
        /// <summary>
        /// The schema used for categories
        /// </summary>
        public const string CategorySchema = NSYouTube + "/categories.cat";
        /// <summary>
        /// Company element string
        /// </summary>
        public const string Company = "company";
        /// <summary>
        /// content element string
        /// </summary>
        public const string Content = "content";
        /// <summary>
        /// Description element string
        /// </summary>
        public const string Description = "description";
        /// <summary>
        /// The schema used for developer tags
        /// </summary>
        public const string DeveloperTagSchema = NSYouTube + "/developertags.cat";
        /// <summary>
        /// Duration element string
        /// </summary>
        public const string Duration = "duration";
        /// <summary>
        /// FirstName element string
        /// </summary>
        public const string FirstName = "firstName";
        /// <summary>
        /// Gender element string
        /// </summary>
        public const string Gender = "gender";
        /// <summary>
        /// Hobbies element string
        /// </summary>
        public const string Hobbies = "hobbies";
        /// <summary>
        /// HomeTown element string
        /// </summary>
        public const string HomeTown = "hometown";
        /// <summary>
        /// The schema used for keywords
        /// </summary>
        public const string KeywordSchema = NSYouTube + "/keywords.cat";
        /// <summary>
        /// LastName element string
        /// </summary>
        public const string LastName = "lastName";
        /// <summary>
        /// Location element string
        /// </summary>
        public const string Location = "location";
        /// <summary>
        /// Movies element string
        /// </summary>
        public const string Movies = "movies";
        /// <summary>
        /// Music element string
        /// </summary>
        public const string Music = "music";
        /// <summary>
        /// NoEmbed element string
        /// </summary>
        public const string NoEmbed = "noembed";
        /// <summary>
        /// Occupation element string
        /// </summary>
        public const string Occupation = "occupation";
        /// <summary>
        /// Position element string
        /// </summary>
        public const string Position = "position";
        /// <summary>
        /// Private element string
        /// </summary>
        public const string Private = "private";
        /// <summary>
        /// QueryString element string
        /// </summary>
        public const string QueryString = "queryString";
        /// <summary>
        /// Racy element string
        /// </summary>
        public const string Racy = "racy";
        /// <summary>
        /// Recorded element string
        /// </summary>
        public const string Recorded = "recorded";
        /// <summary>
        /// The related videos URI in the link collection
        /// </summary>
        public const string RelatedVideo = NSYouTube + "#video.related";
        /// <summary>
        /// Relationship element string
        /// </summary>
        public const string Relationship = "relationship";
        /// <summary>
        /// The video response URI in the link collection
        /// </summary>
        public const string ResponseVideo = NSYouTube + "#video.responses";
        /// <summary>
        /// The video complaint URI in the link collection
        /// </summary>
        public const string Complaint = NSYouTube + "#video.complaints";
        /// <summary>
        /// School element string
        /// </summary>
        public const string School = "school";
        /// <summary>
        /// State element string
        /// </summary>
        public const string State = "state";
        /// <summary>
        /// Statistics element string
        /// </summary>
        public const string Statistics = "statistics";
        /// <summary>
        /// Status element string
        /// </summary>
        public const string Status = "status";
        /// <summary>
        /// UserName element string
        /// </summary>
        public const string UserName = "username";
        /// <summary>
        /// counthint element string for playlist feeds
        /// </summary>
        public const string CountHint = "countHint";
        /// <summary>
        /// videoid element string for playlist feeds
        /// </summary>
        public const string VideoID = "videoid";
        /// <summary>
        /// uploaded element string for playlist feeds
        /// </summary>
        public const string Uploaded = "uploaded";
        /// <summary>
        /// yt:rating element string
        /// </summary>
        public const string YtRating = "rating";
        /// <summary>
        /// yt:accessControl element string
        /// </summary>
        public const string AccessControl = "accessControl";

        /// <summary>
        /// title for a playlist
        /// </summary>
        public const string PlaylistTitle = "playlistTitle";

        /// <summary>
        /// id for a playlist
        /// </summary>
        public const string PlaylistId = "playlistId";
    }

    /// <summary>
    /// this Category entry will be returned for the list of official YouTubeCategories, 
    /// using the <seealso cref="YouTubeQuery.GetYouTubeCategories"/> method
    /// </summary>
    public class YouTubeCategory : Google.GData.Client.AtomCategory {
        /// <summary>
        /// Indicates that new videos may be assigned to that category. New videos 
        /// cannot be assigned to categories that are not marked as assignable
        /// </summary>
        /// 
        public bool Assignable {
            get {
                return (this.FindExtension("assignable", YouTubeNameTable.NSYouTube) != null);
            }
        }

        /// <summary>
        /// The presence of the &lt;yt:browsable&gt; tag indicates that the corresponding 
        /// category is browsable on YouTube in one or more countries. The tag's regions 
        /// attribute contains a space-delimited list of two-letter regionIDs that 
        /// identifies the particular countries where the category is browsable. 
        /// </summary>
        public string Browsable {
            get {
                XmlExtension x = this.FindExtension("browsable", YouTubeNameTable.NSYouTube) as XmlExtension;
                if (x != null && x.Node != null) {
                    return x.Node.Attributes["regions"].Value;
                }
                return null;
            }
        }

        /// <summary>
        /// Categories that are neither assignable or browsable are deprecated and are identified as such
        /// </summary>
        public bool Deprecated {
            get {
                return (this.FindExtension("deprecated", YouTubeNameTable.NSYouTube) != null);
            }
        }
    }

    /// <summary>
    /// YouTube specific MediaGroup element. It adds Duration and Private 
    /// subelements as well as using a different version of MediaCredit
    /// </summary>
    /// <returns></returns>
    public class MediaGroup : Google.GData.Extensions.MediaRss.MediaGroup {
        private ExtensionCollection<MediaContent> contents;

        public MediaGroup() : base() {
            this.ExtensionFactories.Add(new Duration());
            this.ExtensionFactories.Add(new Private());
            this.ExtensionFactories.Add(new VideoId());

            // replace the media group default media credit with a new one. 
            MediaCredit c = new Google.GData.YouTube.MediaCredit();
            this.ReplaceFactory(c.XmlName, c.XmlNameSpace, c);
            // replace the media group default media content with a new one. 
            MediaContent m = new Google.GData.YouTube.MediaContent();
            this.ReplaceFactory(m.XmlName, m.XmlNameSpace, m);
        }

        /// <summary>
        /// property accessor for the Contents Collection 
        /// </summary>
        public ExtensionCollection<MediaContent> Contents {
            get {
                if (this.contents == null) {
                    this.contents = new ExtensionCollection<MediaContent>(this);
                }
                return this.contents;
            }
        }

        /// <summary>
        /// returns the media:credit element
        /// </summary>
        public new MediaCredit Credit {
            get {
                return FindExtension(MediaRssNameTable.MediaRssCredit,
                    MediaRssNameTable.NSMediaRss) as Google.GData.YouTube.MediaCredit;
            }
            set {
                ReplaceExtension(MediaRssNameTable.MediaRssCredit,
                    MediaRssNameTable.NSMediaRss,
                    value);
            }
        }

        /// <summary>
        /// returns the yt:duration element
        /// </summary>
        public Duration Duration {
            get {
                return FindExtension(YouTubeNameTable.Duration,
                    YouTubeNameTable.NSYouTube) as Duration;
            }
            set {
                ReplaceExtension(YouTubeNameTable.Duration,
                    YouTubeNameTable.NSYouTube,
                    value);
            }
        }

        /// <summary>
        /// returns the yt:duration element
        /// </summary>
        public Private Private {
            get {
                return FindExtension(YouTubeNameTable.Private,
                    YouTubeNameTable.NSYouTube) as Private;
            }
            set {
                ReplaceExtension(YouTubeNameTable.Private,
                    YouTubeNameTable.NSYouTube,
                    value);
            }
        }

        /// <summary>
        /// property accessor for the VideoID, if applicable
        /// </summary>
        public VideoId VideoId {
            get {
                return FindExtension(YouTubeNameTable.VideoID,
                    YouTubeNameTable.NSYouTube) as VideoId;
            }
            set {
                ReplaceExtension(YouTubeNameTable.VideoID,
                    YouTubeNameTable.NSYouTube,
                    value);
            }
        }
    }

    public class MediaCredit : Google.GData.Extensions.MediaRss.MediaCredit {
        /// <summary>
        /// default constructor for media:credit
        /// </summary>
        public MediaCredit() : base() {
            this.AttributeNamespaces.Add("type", YouTubeNameTable.NSYouTube);
            this.Attributes.Add("type", null);
        }

        /// <summary>
        /// returns the type of the credit element
        /// </summary>
        /// <returns></returns>
        public string Type {
            get {
                return this.Attributes["type"] as string;
            }
            set {
                this.Attributes["type"] = value;
            }
        }
    }

    public class MediaContent : Google.GData.Extensions.MediaRss.MediaContent {
        /// <summary>
        /// default constructor for media:credit
        /// </summary>
        public MediaContent() : base() {
            this.AttributeNamespaces.Add("format", YouTubeNameTable.NSYouTube);
            this.Attributes.Add("format", null);
        }

        /// <summary>
        /// returns the type of the credit element
        /// </summary>
        /// <returns></returns>
        public string Format {
            get {
                return this.Attributes["format"] as string;
            }
            set {
                this.Attributes["format"] = value;
            }
        }
    }

    /// <summary>
    /// The yt:accessControl element indicates whether users are allowed to rate a video,
    /// rate comments about the video, add a video response to the video or embed the
    /// video on third-party websites.
    /// </summary>
    public class YtAccessControl : SimpleElement {
        /// <summary>the action xml attribute</summary>
        private const string actionAttribute = "action";
        /// <summary>the permission xml attribute</summary>
        private const string permissionAttribute = "permission";

        /// <summary>the rate action</summary>
        public const string RateAction = "rate";
        /// <summary>the comment action</summary>
        public const string CommentAction = "comment";
        /// <summary>the commentVote action</summary>
        public const string CommentVoteAction = "commentVote";
        /// <summary>the videoRespond action</summary>
        public const string VideoRespondAction = "videoRespond";
        /// <summary>the list action</summary>
        public const string ListAction = "list";
        /// <summary>the embed action</summary>
        public const string EmbedAction = "embed";
        /// <summary>the syndicate action</summary>
        public const string SyndicateAction = "syndicate";

        /// <summary>the allowed permission</summary>
        public const string AllowedPermission = "allowed";
        /// <summary>the denied permission</summary>
        public const string DeniedPermission = "denied";
        /// <summary>the moderated permission</summary>
        public const string ModeratedPermission = "moderated";

        /// <summary>
        /// default constructor for yt:accessControl.
        /// </summary>
        public YtAccessControl()
            : base(YouTubeNameTable.AccessControl,
            YouTubeNameTable.ytPrefix,
            YouTubeNameTable.NSYouTube) {
            this.Attributes.Add(actionAttribute, null);
            this.Attributes.Add(permissionAttribute, null);
        }

        /// <summary>
        /// alternative constructor for yt:accessControl that allows
        /// to specify initial values.
        /// </summary>
        public YtAccessControl(string action, string permission)
            : base(YouTubeNameTable.AccessControl,
            YouTubeNameTable.ytPrefix,
            YouTubeNameTable.NSYouTube) {
            this.Attributes.Add(actionAttribute, action);
            this.Attributes.Add(permissionAttribute, permission);
        }

        /// <summary>
        /// convenience accessor for action.
        /// </summary>
        /// <returns></returns>
        public string Action {
            get {
                return this.Attributes[actionAttribute] as string;
            }
            set {
                this.Attributes[actionAttribute] = value;
            }
        }

        /// <summary>
        /// convenience accessor for permission.
        /// </summary>
        /// <returns></returns>
        public string Permission {
            get {
                return this.Attributes[permissionAttribute] as string;
            }
            set {
                this.Attributes[permissionAttribute] = value;
            }
        }
    }

    /// <summary>
    /// id schema extension describing an ID.
    /// </summary>
    public class Age : SimpleElement {
        /// <summary>
        /// default constructor 
        /// </summary>
        public Age()
            : base(YouTubeNameTable.Age, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }

        /// <summary>
        /// default constructor with an initial value as a string 
        /// </summary>
        public Age(string initValue)
            : base(YouTubeNameTable.Age, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// Books schema extension describing a YouTube Books
    /// </summary>
    public class Books : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public Books()
            : base(YouTubeNameTable.Books, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }
        /// <summary>
        /// constructor with an init value
        /// </summary>
        /// <param name="initValue"></param>
        public Books(string initValue)
            : base(YouTubeNameTable.Books, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// Company schema extension describing a YouTube Company
    /// </summary>
    public class Company : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public Company()
            : base(YouTubeNameTable.Company, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }
        /// <summary>
        /// constructor taking the initial value
        /// </summary>
        /// <param name="initValue"></param>
        public Company(string initValue)
            : base(YouTubeNameTable.Company, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// content schema extension describing a YouTube complaint
    /// </summary>
    public class Content : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public Content()
            : base(YouTubeNameTable.Content, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }
        /// <summary>
        /// constructor taking the initial value
        /// </summary>
        /// <param name="initValue"></param>
        public Content(string initValue)
            : base(YouTubeNameTable.Content, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// Description schema extension describing a YouTube Description
    /// </summary>
    public class Description : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public Description()
            : base(YouTubeNameTable.Description, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }
        /// <summary>
        /// constructor taking the initial value
        /// </summary>
        /// <param name="initValue"></param>
        public Description(string initValue)
            : base(YouTubeNameTable.Description, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// Duration schema extension describing a YouTube Duration
    /// </summary>
    public class Duration : SimpleElement {
        /// <summary>the seconds xml attribute </summary>
        public const string AttributeSeconds = "seconds";

        /// <summary>
        /// default constructor
        /// </summary>
        public Duration()
            : base(YouTubeNameTable.Duration, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) {
            this.Attributes.Add(AttributeSeconds, null);
        }

        /// <summary>
        /// constructor taking the initial value
        /// </summary>
        /// <param name="initValue"></param>
        public Duration(string initValue)
            : base(YouTubeNameTable.Duration, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) {
            this.Attributes.Add(AttributeSeconds, null);
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>returns you the seconds attribute</summary>
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Seconds {
            get {
                return this.Attributes[AttributeSeconds] as string;
            }
            set {
                this.Attributes[AttributeSeconds] = value;
            }
        }
    }

    /// <summary>
    /// FirstName schema extension describing a YouTube FirstName
    /// </summary>
    public class FirstName : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public FirstName()
            : base(YouTubeNameTable.FirstName, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }
        /// <summary>
        /// constructor taking the initial value
        /// </summary>
        /// <param name="initValue"></param>
        public FirstName(string initValue)
            : base(YouTubeNameTable.FirstName, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// Gender schema extension describing a YouTube Gender
    /// </summary>
    public class Gender : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public Gender()
            : base(YouTubeNameTable.Gender, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }
        /// <summary>
        /// constructor taking the initial value
        /// </summary>
        /// <param name="initValue"></param>
        public Gender(string initValue)
            : base(YouTubeNameTable.Gender, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// Hobbies schema extension describing a YouTube Hobbies
    /// </summary>
    public class Hobbies : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public Hobbies()
            : base(YouTubeNameTable.Hobbies, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }
        /// <summary>
        /// constructor taking the initial value
        /// </summary>
        /// <param name="initValue"></param>
        public Hobbies(string initValue)
            : base(YouTubeNameTable.Hobbies, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// HomeTown schema extension describing a YouTube HomeTown
    /// </summary>
    public class HomeTown : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public HomeTown()
            : base(YouTubeNameTable.HomeTown, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }
        /// <summary>
        /// constructor taking the initial value
        /// </summary>
        /// <param name="initValue"></param>
        public HomeTown(string initValue)
            : base(YouTubeNameTable.HomeTown, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// LastName schema extension describing a YouTube LastName
    /// </summary>
    public class LastName : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public LastName()
            : base(YouTubeNameTable.LastName, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }
        /// <summary>
        /// constructor taking the initial value
        /// </summary>
        /// <param name="initValue"></param>
        public LastName(string initValue)
            : base(YouTubeNameTable.LastName, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// Location schema extension describing a YouTube Location
    /// </summary>
    public class Location : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public Location()
            : base(YouTubeNameTable.Location, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }
        /// <summary>
        /// constructor taking the initial value
        /// </summary>
        /// <param name="initValue"></param>
        public Location(string initValue)
            : base(YouTubeNameTable.Location, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// Movies schema extension describing a YouTube Movies
    /// </summary>
    public class Movies : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public Movies()
            : base(YouTubeNameTable.Movies, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }
        /// <summary>
        /// constructor taking the initial value
        /// </summary>
        /// <param name="initValue"></param>
        public Movies(string initValue)
            : base(YouTubeNameTable.Movies, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// Music schema extension describing a YouTube Music
    /// </summary>
    public class Music : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public Music()
            : base(YouTubeNameTable.Music, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }
        /// <summary>
        /// constructor taking the initial value
        /// </summary>
        /// <param name="initValue"></param>
        public Music(string initValue)
            : base(YouTubeNameTable.Music, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// NoEmbed schema extension describing a YouTube NoEmbed
    /// </summary>
    public class NoEmbed : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public NoEmbed()
            : base(YouTubeNameTable.NoEmbed, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }
        /// <summary>
        /// constructor taking the initial value
        /// </summary>
        /// <param name="initValue"></param>
        public NoEmbed(string initValue)
            : base(YouTubeNameTable.NoEmbed, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// Occupation schema extension describing a YouTube Occupation
    /// </summary>
    public class Occupation : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public Occupation()
            : base(YouTubeNameTable.Occupation, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }
        /// <summary>
        /// constructor taking the initial value
        /// </summary>
        /// <param name="initValue"></param>
        public Occupation(string initValue)
            : base(YouTubeNameTable.Occupation, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// Position schema extension describing a YouTube Position
    /// </summary>
    public class Position : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public Position()
            : base(YouTubeNameTable.Position, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }
        /// <summary>
        /// constructor taking the initial value
        /// </summary>
        /// <param name="initValue"></param>
        public Position(string initValue)
            : base(YouTubeNameTable.Position, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// Private schema extension describing a YouTube Private
    /// </summary>
    public class Private : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public Private()
            : base(YouTubeNameTable.Private, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }
        /// <summary>
        /// constructor taking the initial value
        /// </summary>
        /// <param name="initValue"></param>
        public Private(string initValue)
            : base(YouTubeNameTable.Private, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// QueryString schema extension describing a YouTube QueryString
    /// </summary>
    public class QueryString : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public QueryString()
            : base(YouTubeNameTable.QueryString, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }
        /// <summary>
        /// constructor taking the initial value
        /// </summary>
        /// <param name="initValue"></param>
        public QueryString(string initValue)
            : base(YouTubeNameTable.QueryString, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// Racy schema extension describing a YouTube Racy
    /// </summary>
    [Obsolete("replaced media:rating")]
    public class Racy : SimpleElement {
        /// <summary>
        /// default constructor 
        /// </summary>
        public Racy()
            : base(YouTubeNameTable.Racy, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }
        /// <summary>
        /// constructor taking the initial value
        /// </summary>
        /// <param name="initValue"></param>
        public Racy(string initValue)
            : base(YouTubeNameTable.Racy, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// Recorded schema extension describing a YouTube Recorded
    /// </summary>
    public class Recorded : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public Recorded()
            : base(YouTubeNameTable.Recorded, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }
        /// <summary>
        /// constructor taking the initial value
        /// </summary>
        /// <param name="initValue"></param>
        public Recorded(string initValue)
            : base(YouTubeNameTable.Recorded, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// Relationship schema extension describing a YouTube Relationship
    /// </summary>
    public class Relationship : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public Relationship()
            : base(YouTubeNameTable.Relationship, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }
        /// <summary>
        /// constructor taking the initial value
        /// </summary>
        /// <param name="initValue"></param>
        public Relationship(string initValue)
            : base(YouTubeNameTable.Relationship, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// Identifies the school that the user attended according to the information 
    /// in the user's public YouTube profile.
    /// </summary>
    public class School : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public School()
            : base(YouTubeNameTable.School, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }
        /// <summary>
        /// constructor taking the initial value
        /// </summary>
        /// <param name="initValue"></param>
        public School(string initValue)
            : base(YouTubeNameTable.School, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// State schema extension describing a YouTube State, child of app:control
    /// The state tag contains information that describes the status of a video. 
    /// For videos that failed to upload or were rejected after the upload 
    /// process, the reasonCode attribute and the tag value provide 
    /// insight into the reason for the upload problem.
    /// </summary>
    public class State : SimpleElement {
        /// <summary>the name xml attribute </summary>
        public const string AttributeName = "name";
        /// <summary>the reasonCode xml attribute </summary>
        public const string AttributeReason = "reasonCode";
        /// <summary>the help xml attribute </summary>
        public const string AttributeHelp = "helpUrl";

        /// <summary>
        /// default constructor
        /// </summary>
        public State()
            : base(YouTubeNameTable.State, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) {
            this.Attributes.Add(AttributeName, null);
            this.Attributes.Add(AttributeReason, null);
            this.Attributes.Add(AttributeHelp, null);
        }
        /// <summary>
        /// constructor taking the initial value
        /// </summary>
        /// <param name="initValue"></param>
        public State(string initValue)
            : base(YouTubeNameTable.State, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) {
            this.Attributes.Add(AttributeName, null);
            this.Attributes.Add(AttributeReason, null);
            this.Attributes.Add(AttributeHelp, null);
        }

        /// <summary>The name attribute identifies the status of an unpublished video. 
        /// Valid values for this tag are processing, deleted, rejected and failed.</summary>
        /// <returns> </returns>
        public string Name {
            get {
                return this.Attributes[AttributeName] as string;
            }
            set {
                this.Attributes[AttributeName] = value;
            }
        }

        /// <summary>The reasonCode attribute provides information about why a video failed 
        /// to upload or was rejected after the uploading process. The yt:state tag will not 
        /// include a reasonCode attribute if the value of the name 
        /// attribute is processing. </summary>
        /// <returns> </returns>
        public string Reason {
            get {
                return this.Attributes[AttributeReason] as string;
            }
            set {
                this.Attributes[AttributeReason] = value;
            }
        }

        /// <summary>The helpUrl parameter contains a link to a YouTube Help 
        /// Center page that may help the developer or the video owner to 
        /// diagnose the reason that an upload failed or was rejected..</summary>
        /// <returns> </returns>
        public string Help {
            get {
                return this.Attributes[AttributeHelp] as string;
            }
            set {
                this.Attributes[AttributeHelp] = value;
            }
        }
    }

    /// <summary>
    /// The statistics tag provides statistics about a video or a user. 
    /// The statistics tag is not provided in a video entry if the value 
    /// of the viewCount attribute is 0.
    /// </summary>
    public class Statistics : SimpleElement {
        /// <summary>the viewCount xml attribute </summary>
        public const string AttributeViewCount = "viewCount";
        /// <summary>the videoWatchCount xml attribute </summary>
        public const string AttributeWatchCount = "videoWatchCount";
        /// <summary>the subscriberCount xml attribute </summary>
        public const string AttributeSubscriberCount = "subscriberCount";
        /// <summary>the lastWebAccess xml attribute </summary>
        public const string AttributeLastWebAccess = "lastWebAccess";
        /// <summary>the favoriteCount xml attribute </summary>
        public const string AttributeFavoriteCount = "favoriteCount";

        /// <summary>
        /// default constructor
        /// </summary>
        public Statistics()
            : base(YouTubeNameTable.Statistics, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) {
            this.Attributes.Add(AttributeViewCount, null);
            this.Attributes.Add(AttributeWatchCount, null);
            this.Attributes.Add(AttributeSubscriberCount, null);
            this.Attributes.Add(AttributeLastWebAccess, null);
            this.Attributes.Add(AttributeFavoriteCount, null);
        }

        /// <summary>
        /// constructor taking the initial value
        /// </summary>
        /// <param name="initValue"></param>
        public Statistics(string initValue)
            : base(YouTubeNameTable.Statistics, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) {
            this.Attributes.Add(AttributeViewCount, null);
            this.Attributes.Add(AttributeWatchCount, null);
            this.Attributes.Add(AttributeSubscriberCount, null);
            this.Attributes.Add(AttributeLastWebAccess, null);
            this.Attributes.Add(AttributeFavoriteCount, null);
        }

        /// <summary>convenience accessor for the ViewCount</summary> 
        /// <returns> </returns>
        public string ViewCount {
            get {
                return this.Attributes[AttributeViewCount] as string;
            }
            set {
                this.Attributes[AttributeViewCount] = value;
            }
        }

        /// <summary>convenience accessor for the Watched Count</summary> 
        /// <returns> </returns>
        public string WatchCount {
            get {
                return this.Attributes[AttributeWatchCount] as string;
            }
            set {
                this.Attributes[AttributeWatchCount] = value;
            }
        }

        /// <summary>convenience accessor for the SubscriberCount</summary> 
        /// <returns> </returns>
        public string SubscriberCount {
            get {
                return this.Attributes[AttributeSubscriberCount] as string;
            }
            set {
                this.Attributes[AttributeSubscriberCount] = value;
            }
        }

        /// <summary>convenience accessor for the LastWebAccess</summary> 
        /// <returns> </returns>
        public string LastWebAccess {
            get {
                return this.Attributes[AttributeLastWebAccess] as string;
            }
            set {
                this.Attributes[AttributeLastWebAccess] = value;
            }
        }

        /// <summary>convenience accessor for the Favorite</summary> 
        /// <returns> </returns>
        public string FavoriteCount {
            get {
                return this.Attributes[AttributeFavoriteCount] as string;
            }
            set {
                this.Attributes[AttributeFavoriteCount] = value;
            }
        }
    }

    /// <summary>
    /// The countHint element specifies the number of entries in a playlist feed. 
    /// The countHint tag appears in the entries in a playlists feed, where each entry contains 
    /// information about a single playlist
    /// </summary>
    public class CountHint : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public CountHint()
            : base(YouTubeNameTable.CountHint, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }

        /// <summary>
        /// constructor taking the initial value
        /// </summary>
        /// <param name="initValue"></param>
        public CountHint(string initValue)
            : base(YouTubeNameTable.CountHint, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// Status schema extension describing a YouTube Status
    /// </summary>
    public class Status : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public Status()
            : base(YouTubeNameTable.Status, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }

        /// <summary>
        /// constructor taking the initial value
        /// </summary>
        /// <param name="initValue"></param>
        public Status(string initValue)
            : base(YouTubeNameTable.Status, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// UserName schema extension describing a YouTube UserName
    /// </summary>
    public class UserName : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public UserName()
            : base(YouTubeNameTable.UserName, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }

        /// <summary>
        /// constructor taking the initial value
        /// </summary>
        /// <param name="initValue"></param>
        public UserName(string initValue)
            : base(YouTubeNameTable.UserName, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// Uploaded schema extension describing a YouTube uploaded date
    /// </summary>
    public class Uploaded : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public Uploaded()
            : base(YouTubeNameTable.Uploaded, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }

        /// <summary>
        /// constructor with an init value
        /// </summary>
        /// <param name="initValue"></param>
        public Uploaded(string initValue)
            : base(YouTubeNameTable.Uploaded, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// VideoId schema extension describing a YouTube video identifier
    /// </summary>
    public class VideoId : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public VideoId()
            : base(YouTubeNameTable.VideoID, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }

        /// <summary>
        /// constructor with an init value
        /// </summary>
        /// <param name="initValue"></param>
        public VideoId(string initValue)
            : base(YouTubeNameTable.VideoID, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// PlaylistId schema extension describing a YouTube playlist identifier
    /// </summary>
    public class PlaylistId : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public PlaylistId()
            : base(YouTubeNameTable.PlaylistId, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }

        /// <summary>
        /// constructor with an init value
        /// </summary>
        /// <param name="initValue"></param>
        public PlaylistId(string initValue)
            : base(YouTubeNameTable.PlaylistId, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// PlaylistTitle schema extension describing a YouTube playlist title
    /// </summary>
    public class PlaylistTitle : SimpleElement {
        /// <summary>
        /// default constructor
        /// </summary>
        public PlaylistTitle()
            : base(YouTubeNameTable.PlaylistTitle, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube) { }

        /// <summary>
        /// constructor with an init value
        /// </summary>
        /// <param name="initValue"></param>
        public PlaylistTitle(string initValue)
            : base(YouTubeNameTable.PlaylistTitle, YouTubeNameTable.ytPrefix, YouTubeNameTable.NSYouTube, initValue) { }
    }

    /// <summary>
    /// The yt:rating element contains information about the number of users who gave
    /// the video a positive or negative rating as well as the totale number of ratings
    /// that the video received.
    /// </summary>
    public class YtRating : SimpleElement {
        /// <summary>the numLikes xml attribute </summary>
        private const string numLikesAttribute = "numLikes";
        /// <summary>the numDislikes xml attribute</summary>
        private const string numDislikesAttribute = "numDislikes";
        /// <summary>the value xml attribute</summary>
        private const string valueAttribute = "value";

        /// <summary>the like video rating</summary>
        public const string Like = "like";
        /// <summary>the dislike video rating</summary>
        public const string Dislikes = "dislikes";

        /// <summary>
        /// default constructor for yt:rating.
        /// </summary>
        public YtRating()
            : base(YouTubeNameTable.YtRating,
            YouTubeNameTable.ytPrefix,
            YouTubeNameTable.NSYouTube) {
            this.Attributes.Add(numLikesAttribute, null);
            this.Attributes.Add(numDislikesAttribute, null);
            this.Attributes.Add(valueAttribute, null);
        }

        /// <summary>
        /// default constructor for yt:rating.
        /// </summary>
        public YtRating(string value)
            : base(YouTubeNameTable.YtRating,
            YouTubeNameTable.ytPrefix,
            YouTubeNameTable.NSYouTube) {
            this.Attributes.Add(numLikesAttribute, null);
            this.Attributes.Add(numDislikesAttribute, null);
            this.Attributes.Add(valueAttribute, value);
        }

        /// <summary>
        /// convenience accessor for Likes Count.
        /// </summary>
        /// <returns></returns>
        public string NumLikes {
            get {
                return this.Attributes[numLikesAttribute] as string;
            }
            set {
                this.Attributes[numLikesAttribute] = value;
            }
        }

        /// <summary>
        /// convenience accessor for Dislikes Count.
        /// </summary>
        /// <returns></returns>
        public string NumDislikes {
            get {
                return this.Attributes[numDislikesAttribute] as string;
            }
            set {
                this.Attributes[numDislikesAttribute] = value;
            }
        }

        /// <summary>
        /// The positive ("like") or negative ("dislike") rating for the video.
        /// </summary>
        /// <returns></returns>
        public string RatingValue {
            get {
                return this.Attributes[valueAttribute] as string;
            }
            set {
                this.Attributes[valueAttribute] = value;
            }
        }
    }

    /// <summary>
    /// Simple class to hold the response of a browser-based upload request
    /// </summary>
    public class FormUploadToken {
        /// <summary>
        /// The URL that the browser must POST to
        /// </summary>
        private string url;
        /// <summary>
        /// The token which much be included in the browser form.
        /// </summary>
        private string token;

        /// <summary>
        /// Simple constructor that initializes private members
        /// </summary>
        /// <param name="url">The URL that the browser must POST to</param>
        /// <param name="token">The token which much be included in the browser form.</param>
        public FormUploadToken(string url, string token) {
            this.url = url;
            this.token = token;
        }

        /// <summary>
        /// Constructor that initializes the object from a server response
        /// </summary>
        /// <param name="stream">Stream containing a server response</param>
        public FormUploadToken(Stream stream) {
            XmlDocument doc = new XmlDocument();
            doc.Load(stream);

            this.url = doc.GetElementsByTagName("url")[0].InnerText;
            this.token = doc.GetElementsByTagName("token")[0].InnerText;
        }

        /// <summary>
        /// Property to access the URL the browser must POST to
        /// </summary>
        public string Url {
            get {
                return this.url;
            }
            set {
                this.url = value;
            }
        }

        /// <summary>
        /// Property to access the token the browser must include in the form POST
        /// </summary>
        public string Token {
            get {
                return this.token;
            }
            set {
                this.token = value;
            }
        }
    }
}


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
using System.Text;
using System.Globalization;
using System.Diagnostics;
using System.Collections.Generic;
using Google.GData.Client;

namespace Google.GData.YouTube {
    /// <summary>
    /// A subclass of FeedQuery, to create an YouTube query URI.
    /// The YouTube Data API supports the following standard Google Data query parameters.
    /// Name	Definition
    /// alt	        The alt parameter specifies the format of the feed to be returned. 
    ///             Valid values for this parameter are atom, rss and json. The default 
    ///             value is atom and this document only explains the format of Atom responses.
    /// author	    The author parameter restricts the search to videos uploaded by a 
    ///             particular YouTube user. The Videos uploaded by a specific user 
    ///             section discusses this parameter in more detail.
    /// max-results	The max-results parameter specifies the maximum number of results 
    ///             that should be included in the result set. This parameter works 
    ///             in conjunction with the start-index parameter to determine which 
    ///             results to return. For example, to request the second set of 10 
    ///             results Ð i.e. results 11-20 Ð set the max-results parameter to 10 
    ///             and the start-index parameter to 11. The default value of this 
    ///             parameter for all Google Data APIs is 25, and the maximum value is 50. 
    ///             However, for displaying lists of videos, we recommend that you 
    ///             set the max-results parameter to 10.
    /// start-index	The start-index parameter specifies the index of the first matching result 
    ///             that should be included in the result set. This parameter uses a one-based 
    ///             index, meaning the first result is 1, the second result is 2 and so forth. 
    ///             This parameter works in conjunction with the max-results parameter to determine
    ///             which results to return. For example, to request the second set of 10 
    ///             results Ð i.e. results 11-20 Ð set the start-index parameter to 11 
    ///             and the max-results parameter to 10.
    /// Please see the Google Data APIs Protocol Reference for more information about standard Google 
    /// Data API functionality or about these specific parameters.
    /// Custom parameters for the YouTube Data API
    /// In addition to the standard Google Data query parameters, the YouTube Data API defines 
    /// the following API-specific query parameters. These parameters are only available on video
    /// and playlist feeds.
    /// Name	    Definition
    /// orderby	    The orderby parameter specifies the value that will be used to sort videos in the
    ///             search result set. Valid values for this parameter are relevance, published, viewCount 
    ///             and rating. In addition, you can request results that are most relevant to a specific 
    ///             language by setting the parameter value to relevance_lang_languageCode, where 
    ///             languageCode is an ISO 639-1 two-letter language code. (Use the values zh-Hans for 
    ///             simplified Chinese and zh-Hant for traditional Chinese.) In addition, please note that 
    ///             results in other languages will still be returned if they are highly relevant to the 
    ///             search query term.
    ///             The default value for this parameter is relevance for a search results feed. For a
    ///             playlist feed, the default ordering is based on the position of each video in the playlist. 
    ///             For a user's playlists or subscriptions feed, the default ordering is arbitrary.
    /// client	    The client parameter is an alphanumeric string that identifies your application. The 
    ///             client parameter is an alternate way of specifying your client ID. You can also use the 
    ///             X-GData-Client request header to specify your client ID. Your application does not need to
    ///             specify your client ID twice by using both the client parameter and the X-GData-Client 
    ///             request header, but it should provide your client ID using at least one of those two methods.
    /// format	    The format parameter specifies that videos must be available in a particular video format. 
    ///             Your request can specify any of the following formats:
    ///     Value	    Video Format
    ///         1	    RTSP streaming URL for mobile video playback. H.263 video (up to 176x144) and AMR audio.
    ///         5	    HTTP URL to the embeddable player (SWF) for this video. This format is not available for a
    ///                 video that is not embeddable. Developers commonly add format=5 to their queries to restrict
    ///                 results to videos that can be embedded on their sites.
    ///         6	    RTSP streaming URL for mobile video playback. MPEG-4 SP video (up to 176x144) and AAC audio
    /// lr	    The lr parameter restricts the search to videos that have a title, description or keywords in a
    ///         specific language. Valid values for the lr parameter are ISO 639-1 two-letter language codes. 
    ///         You can also use the values zh-Hans for simplified Chinese and zh-Hant for traditional Chinese. This
    ///         parameter can be used when requesting any video feeds other than standard feeds.
    /// restriction	The restriction parameter identifies the IP address that should be used to filter videos 
    ///         that can only be played in specific countries. By default, the API filters out videos that cannot 
    ///         be played in the country from which you send API requests. This restriction is based on your 
    ///         client application's IP address.
    ///         To request videos playable from a specific computer, include the restriction parameter 
    ///         in your request and set the parameter value to the IP address of the computer where the videos
    ///         will be played Ð e.g. restriction=255.255.255.255.
    ///         To request videos that are playable in a specific country, include the restriction parameter in your 
    ///         request and set the parameter value to the ISO 3166 two-letter country code of the country where 
    ///         the videos will be played Ð e.g. restriction=DE.
    /// time	The time parameter, which is only available for the top_rated, top_favorites, most_viewed, 
    ///         most_discussed, most_linked and most_responded standard feeds, restricts the search to videos 
    ///         uploaded within the specified time. Valid values for this parameter are today (1 day), 
    ///         this_week (7 days), this_month (1 month) and all_time. The default value for this parameter is all_time.
    /// </summary>
    public class YouTubeQuery : FeedQuery {
        /// <summary>
        /// describing the requested video format
        /// </summary>
        public enum VideoFormat {
            /// <summary>
            /// no parameter. Setting the accessLevel to undefined
            /// implies the server default
            /// </summary>
            FormatUndefined = 0,
            /// <summary>
            /// RTSP streaming URL for mobile video playback. H.263 video (up to 176x144) and AMR audio.
            /// </summary>
            RTSP = 1,
            /// <summary>
            /// HTTP URL to the embeddable player
            /// </summary>
            Embeddable = 5,
            /// <summary>
            /// SRTSP streaming URL for mobile video playback.
            /// </summary>
            Mobile = 6,
        }

        /// <summary>
        /// describing the requested video format
        /// </summary>
        public enum UploadTime {
            /// <summary>
            /// time undefined, default value for the server
            /// </summary>
            UploadTimeUndefined,
            /// <summary>
            /// today (1day)
            /// </summary>
            Today,
            /// <summary>
            /// This week (7days)
            /// </summary>
            ThisWeek,
            /// <summary>
            /// 1 month
            /// </summary>
            ThisMonth,
            /// <summary>all time</summary>
            AllTime
        }

        /// <summary>
        /// describing the possible safe search values
        /// <seealso cref="YouTubeQuery.SafeSearch"/>
        /// </summary>
        public enum SafeSearchValues {
            /// <summary>no restriction</summary>
            None,
            /// <summary>moderate restriction</summary>
            Moderate,
            /// <summary>strict restriction</summary>
            Strict
        }

        private SafeSearchValues safeSearch;
        private List<VideoFormat> formats;
        private string videoQuery;
        private string orderBy;
        private string client;
        private string lr;
        private string restriction;
        private UploadTime uploadTime = UploadTime.UploadTimeUndefined;

        private string location;
        private string locationRadius;
        private string uploader;

        /// <summary>
        /// the standard feeds URL
        /// </summary>
        public const string StandardFeeds = "https://gdata.youtube.com/feeds/api/standardfeeds/";

        /// <summary>
        /// youTube base video URI 
        /// </summary>
        public const string DefaultVideoUri = "https://gdata.youtube.com/feeds/api/videos";

        /// <summary>
        /// youTube base video URI for batch operations 
        /// </summary>
        public const string BatchVideoUri = "https://gdata.youtube.com/feeds/api/videos/batch";

        /// <summary>
        /// youTube base mobile video URI 
        /// </summary>
        public const string MobileVideoUri = "https://gdata.youtube.com/feeds/mobile/videos";

        /// <summary>
        /// youTube base standard top rated video URI 
        /// </summary>
        public const string TopRatedVideo = YouTubeQuery.StandardFeeds + "top_rated";

        /// <summary>
        /// youTube base standard favorites video URI 
        /// </summary>
        public const string FavoritesVideo = YouTubeQuery.StandardFeeds + "top_favorites";

        /// <summary>
        /// youTube base standard most viewed video URI 
        /// </summary>
        public const string MostViewedVideo = YouTubeQuery.StandardFeeds + "most_viewed";

        /// <summary>
        /// youTube base standard most recent video URI 
        /// </summary>
        public const string MostRecentVideo = YouTubeQuery.StandardFeeds + "most_recent";

        /// <summary>
        /// youTube base standard most popular video URI 
        /// </summary>
        public const string MostPopular = YouTubeQuery.StandardFeeds + "most_popular";

        /// <summary>
        /// youTube base standard most discussed video URI 
        /// </summary>
        public const string MostDiscussedVideo = YouTubeQuery.StandardFeeds + "most_discussed";

        /// <summary>
        /// youTube base standard most linked video URI 
        /// </summary>
        public const string MostLinkedVideo = YouTubeQuery.StandardFeeds + "most_linked";

        /// <summary>
        /// youTube base standard most responded video URI 
        /// </summary>
        public const string MostRespondedVideo = YouTubeQuery.StandardFeeds + "most_responded";

        /// <summary>
        /// youTube base standard recently featured video URI 
        /// </summary>
        public const string RecentlyFeaturedVideo = YouTubeQuery.StandardFeeds + "recently_featured";

        /// <summary>
        /// youTube base standard mobile phones video URI 
        /// </summary>
        public const string MobilePhonesVideo = YouTubeQuery.StandardFeeds + "watch_on_mobile";

        /// <summary>
        /// default users upload account
        /// </summary>
        public const string DefaultUploads = "https://gdata.youtube.com/feeds/api/users/default/uploads";

        /// <summary>
        /// base uri for user based feeds
        /// </summary>
        public const string BaseUserUri = "https://gdata.youtube.com/feeds/api/users/";

        /// <summary>
        /// base constructor
        /// </summary>
        public YouTubeQuery()
            : base() {
            this.CategoryQueriesAsParameter = true;
            this.SafeSearch = SafeSearchValues.Moderate;
        }

        /// <summary>
        /// base constructor, with initial queryUri
        /// </summary>
        /// <param name="queryUri">the query to use</param>
        public YouTubeQuery(string queryUri)
            : base(queryUri) {
            this.CategoryQueriesAsParameter = true;
            this.SafeSearch = SafeSearchValues.Moderate;
        }

        /// <summary>
        /// format	    The format parameter specifies that videos must be available in a particular video format. 
        ///             Your request can specify any of the following formats:
        ///     Value	    Video Format
        ///         1	    RTSP streaming URL for mobile video playback. H.263 video (up to 176x144) and AMR audio.
        ///         5	    HTTP URL to the embeddable player (SWF) for this video. This format is not available for a
        ///                 video that is not embeddable. Developers commonly add format=5 to their queries to restrict
        ///                 results to videos that can be embedded on their sites.
        ///         6	    RTSP streaming URL for mobile video playback. MPEG-4 SP video (up to 176x144) and AAC audio
        /// </summary>
        /// <returns> the list of formats</returns>
        public List<VideoFormat> Formats {
            get {
                if (this.formats == null) {
                    this.formats = new List<VideoFormat>();
                }
                return this.formats;
            }
        }

        /// <summary>accessor method public UploadTime Time</summary> 
        /// <returns> </returns>
        public UploadTime Time {
            get { return this.uploadTime; }
            set { this.uploadTime = value; }
        }

        /// <summary>
        /// The orderby parameter, which is only supported for video feeds, 
        /// specifies the value that will be used to sort videos in the search
        ///  result set. Valid values for this parameter are relevance, 
        /// published, viewCount and rating. In addition, you can request
        ///  results that are most relevant to a specific language by
        ///  setting the parameter value to relevance_lang_languageCode, 
        /// where languageCode is an ISO 639-1 two-letter 
        /// language code. (Use the values zh-Hans for simplified Chinese
        ///  and zh-Hant for traditional Chinese.) In addition, 
        /// please note that results in other languages will still be 
        /// returned if they are highly relevant to the search query term.
        /// The default value for this parameter is relevance 
        /// for a search results feed.
        /// accessor method public string OrderBy</summary> 
        /// <returns> </returns>
        public string OrderBy {
            get { return this.orderBy; }
            set { this.orderBy = value; }
        }

        /// <summary>
        /// The client parameter is an alphanumeric string that identifies your
        ///  application. The client parameter is an alternate way of specifying 
        /// your client ID. You can also use the X-GData-Client request header to
        ///  specify your client ID. Your application does not need to 
        /// specify your client ID twice by using both the client parameter and 
        /// the X-GData-Client request header, but it should provide your 
        /// client ID using at least one of those two methods.
        /// Note that you should set this normally on the YouTubeService object,
        /// this property is only included for completeness
        /// </summary> 
        /// <returns> </returns>
        public string Client {
            get { return this.client; }
            set { this.client = value; }
        }

        /// <summary>
        /// The lr parameter restricts the search to videos that have a title, 
        /// description or keywords in a specific language. Valid values for 
        /// the lr parameter are ISO 639-1 two-letter language codes. You can
        /// also use the values zh-Hans for simplified Chinese and zh-Hant
        ///  for traditional Chinese. This parameter can be used when requesting 
        /// any video feeds other than standard feeds.
        /// </summary> 
        public string LR {
            get { return this.lr; }
            set { this.lr = value; }
        }

        /// <summary>
        /// <para>
        /// The safeSearch parameter indicates whether the search results should include 
        /// restricted content as well as standard content. YouTube will determine whether 
        /// content is restricted based on the user's IP address or location, which you specify
        ///  in your API request using the restriction parameter. If you do request restricted
        ///  content, then feed entries for videos that contain restricted content will 
        /// contain the &gt;media:rating&lt; element.
        /// </para>
        ///  The following values are valid for this parameter:
        /// </para>
        /// <list>
        /// <listheader><term>Value</term><description>Description</description></listheader>
        /// <item><term>none</term><description>YouTube will not perform any filtering on the search result set.</description></iterm>
        /// <item><term>moderate</term><description>YouTube should try to exclude the most explicit content from the search result set. Based on their 
        ///   content, search results could be removed from search results or demoted in search results.</description></iterm>
        /// <item><term>strict</term><description>YouTube should try to exclude all restricted content from the search result set. Based on their content, search 
        /// results could be removed from search results or demoted in search results</description></iterm>
        ///<para>The default value for this parameter is moderate.</para>
        ///<para>SafeSearch filtering for the YouTube Data API is designed to function similarly to SafeSearch Filtering for Google WebSearch results. 
        /// Please note that YouTube makes every effort to remove restricted content from search results in accordance with the SafeSearch setting that you specify. 
        /// However, filters may not be 100% accurate and restricted videos may occasionally appear in search results even if you have specified strict SafeSearch filtering. 
        /// If this happens, please flag the video by filing a complaint, which will help us to better identify restricted content.</para>
        ///<para>Note: The safeSearch parameter was introduced in version 2.0 of the YouTube Data API and replaced the racy parameter, which was used in version 1.0.</para>
        /// </summary>
        public SafeSearchValues SafeSearch {
            get { return this.safeSearch; }
            set { this.safeSearch = value; }
        }

        /// <summary>
        /// The location parameter restricts the search to videos that have a geographical location specified in their metadata. The parameter can be used in either of the following contexts:
        /// <para>The parameter value can specify geographic coordinates (latitude,longitude) that identify a particular location. In this context, the location parameter 
        /// operates in conjunction with the location-radius parameter to define a geographic area. The API response will then contain videos that are associated with a 
        /// geographical location within that area.</para>
        /// <para>Note that when a user uploads a video to YouTube, the user can associate a location with the video by either specifying geographic coordinates (-122.08427,37.42307) 
        /// or by providing a descriptive address (Munich, Germany). As such, some videos may be associated with a location within the area specified in a search query
        ///  even though those videos are not associated with specific coordinates that can be plotted on a map.</para>
        /// <para>To exclude videos from the API response if those videos are associated with a descriptive address but not with specific geographic coordinates, append 
        /// an exclamation point ("!") to the end of the parameter value. This practice effectively ensures that all videos in the API response can be plotted on a map.</para>
        /// <para>The following examples show sample uses of this parameter:</para>
        /// <para>location=37.42307,-122.08427&location-radius=100km</para>
        /// <para>location=37.42307,-122.08427!&location-radius=100km</para>
        /// <para>location=37.42307,-122.08427&location-radius=100km</para>
        /// <para>In an API response, feed entries that are associated with specific coordinates will contain the georss:where tag and may also contain the yt:location tag. 
        /// Feed entries that are associated with a descriptive address but not with specific geographic cooordinates specify the address using the yt:location tag.
        /// <para>The parameter value can be  a single exclamation point. In this context, the parameter does not require a value and its presence serves to
        ///  restrict the search results to videos that have a geographical location, but it does not enable you to find videos with a specific geographical location. 
        /// This parameter can be used with all video feeds. A video that has a geographical location will have a georss:where tag in its metadata.<para>
        /// </summary>
        public string Location {
            get {
                return this.location;
            }
            set {
                this.location = value;
            }
        }

        /// <summary>
        ///  The location-radius parameter, in conjunction with the location parameter, defines a geographic area. If the geographic coordinates associated with a video fall 
        /// within that area, then the video may be included in search results.
        /// <para>The location-radius parameter value must be a floating point number followed by a measurement unit. Valid measurement units are m, km, ft and mi. 
        /// For example, valid parameter values include "1500m", "5km", "10000ft" and "0.75mi". The API will return an error if the radius is greater than 1000 kilometers.</para>
        ///  <seealso cref="YouTubeQuery.Location"/>
        /// </summary>
        /// <returns></returns>
        public string LocationRadius {
            get {
                return this.locationRadius;
            }
            set {
                this.locationRadius = value;
            }
        }

        /// <summary>
        /// The restriction parameter identifies the IP address that should be 
        /// used to filter videos that can only be played in specific countries. 
        /// We recommend that you always use this parameter to specify the end 
        /// user's IP address. (By default, the API filters out videos that
        ///  cannot be played in the country from which you send API requests. 
        /// This restriction is based on your client application's IP address.)
        /// To request videos playable from a specific computer, include the 
        /// restriction parameter in your request and set the parameter value 
        /// to the IP address of the computer where the videos will be 
        /// played Ð e.g. restriction=255.255.255.255.
        /// To request videos that are playable in a specific country, 
        /// include the restriction parameter in your request and set 
        /// the parameter value to the ISO 3166 two-letter country code 
        /// of the country where the videos will be played
        ///  Ð e.g. restriction=DE.
        /// </summary> 
        /// <returns> </returns>
        public string Restriction {
            get { return this.restriction; }
            set { this.restriction = value; }
        }

        /// <summary>
        /// The uploader parameter, which is only supported for search requests, lets you restrict a query to YouTube 
        /// partner videos. A YouTube partner is a person or organization that has been accepted into and participates 
        /// in the YouTube Partner Program.
        /// <para>In an API response, a feed entry contains a partner video if the entry contains a media:credit tag for 
        /// which the value of the yt:type attribute is partner.</para>
        /// </summary>
        /// <returns></returns>
        public string Uploader {
            get {
                return this.uploader;
            }
            set {
                this.uploader = value;
            }

        }

        /// <summary>
        /// convenience method to create an URI based on a userID
        /// for the subscriptions
        /// </summary>
        /// <param name="userID"></param>
        /// <returns>string</returns>
        public static string CreateSubscriptionUri(string userID) {
            return CreateCustomUri(userID, "subscriptions");
        }

        /// <summary>
        /// convenience method to create an URI based on a userID
        /// for the playlists of an user
        /// </summary>
        /// <param name="userID"></param>
        /// <returns>string</returns>
        public static string CreatePlaylistsUri(string userID) {
            return CreateCustomUri(userID, "playlists");
        }

        /// <summary>
        /// convenience method to create an URI based on a userID
        /// for the favorites of an user
        /// </summary>
        /// <param name="userID"></param>
        /// <returns>string</returns>
        public static string CreateFavoritesUri(string userID) {
            return CreateCustomUri(userID, "favorites");
        }

        /// <summary>
        /// convenience method to create an URI based on a userID
        /// for the messages of an user
        /// </summary>
        /// <param name="userID"></param>
        /// <returns>string</returns>
        public static string CreateMessagesUri(string userID) {
            return CreateCustomUri(userID, "inbox");
        }

        /// <summary>
        /// convenience method to create an URI based on a userID
        /// for the contacts of an user
        /// </summary>
        /// <param name="userID"></param>
        /// <returns>string</returns>
        public static string CreateContactsUri(string userID) {
            return CreateCustomUri(userID, "contacts");
        }

        /// <summary>
        /// convenience method to create an URI based on a userID
        /// for the uploaded videos of an user
        /// </summary>
        /// <param name="userID"></param>
        /// <returns>string</returns>
        public static string CreateUserUri(string userID) {
            return CreateCustomUri(userID, "uploads");
        }

        /// <summary>
        /// assuming you have a video ID, returns the watch uri as a string
        /// </summary>
        /// <param name="videoID"></param>
        /// <returns></returns>
        public static string CreateVideoWatchUri(string videoID) {
            return "https://www.youtube.com/watch?v=" + Google.GData.Client.Utilities.UriEncodeUnsafe(videoID);
        }

        /// <summary>
        /// assuming you have a video ID, returns the video feed uri as a string
        /// </summary>
        /// <param name="videoID"></param>
        /// <returns></returns>
        public static string CreateVideoUri(string videoID) {
            return DefaultVideoUri + "/" + Google.GData.Client.Utilities.UriEncodeUnsafe(videoID);
        }

        // helper method for the above publics
        private static string CreateCustomUri(string userID, string path) {
            if (String.IsNullOrEmpty(userID)) {
                return YouTubeQuery.BaseUserUri + "default/" + path;
            }
            return YouTubeQuery.BaseUserUri + userID + "/" + path;
        }

        /// <summary>
        /// retrieves the youtubecategories collection from the default
        /// location at http://gdata.youtube.com/schemas/2007/categories.cat
        /// </summary>
        /// <returns></returns>
        public static AtomCategoryCollection GetYouTubeCategories() {
            return GetCategories(new Uri("http://gdata.youtube.com/schemas/2007/categories.cat"), new YouTubeCategoryCollection());
        }

        /// <summary>
        /// retrieves a category collection from the given URL
        /// The owner should be a new Collection object, like:
        /// <code>
        ///		GetCategories(new Uri("http://gdata.youtube.com/schemas/2007/categories.cat"), 
        ///					  new YouTubeCategoryCollection())
        /// </code>
        /// </summary>
        /// <returns></returns>
        public static AtomCategoryCollection GetCategories(Uri uri, AtomBase owner) {
            // first order is to get the document into an xml dom
            XmlTextReader textReader = new XmlTextReader(uri.AbsoluteUri);

            AtomFeedParser parser = new AtomFeedParser();
            AtomCategoryCollection collection = parser.ParseCategories(textReader, owner);
            return collection;
        }

        /// <summary>protected void ParseUri</summary> 
        /// <param name="targetUri">takes an incoming Uri string and parses all the properties out of it</param>
        /// <returns>throws a query exception when it finds something wrong with the input, otherwise returns a baseuri</returns>
        protected override Uri ParseUri(Uri targetUri) {
            base.ParseUri(targetUri);
            if (targetUri == null) {
                return this.Uri;
            }

            char[] deli = { '?', '&' };

            string source = HttpUtility.UrlDecode(targetUri.Query);
            TokenCollection tokens = new TokenCollection(source, deli);
            foreach (string token in tokens) {
                if (token.Length == 0) {
                    continue;
                }
                 
                char[] otherDeli = { '=' };
                string[] parameters = token.Split(otherDeli, 2);
                switch (parameters[0]) {
                    case "format":
                        if (parameters[1] != null) {
                            string[] formats = parameters[1].Split(new char[] { ',' });
                            foreach (string f in formats) {
                                this.Formats.Add((VideoFormat)Enum.Parse(typeof(VideoFormat), f));
                            }
                        }
                        break;
                    case "orderby":
                        this.OrderBy = parameters[1];
                        break;
                    case "client":
                        this.Client = parameters[1];
                        break;
                    case "lr":
                        this.LR = parameters[1];
                        break;
                    case "location":
                        this.Location = parameters[1];
                        break;
                    case "location-radius":
                        this.LocationRadius = parameters[1];
                        break;
                    case "uploader":
                        this.Uploader = parameters[1];
                        break;
                    case "safeSearch":
                        if ("none" == parameters[1]) {
                            this.SafeSearch = SafeSearchValues.None;
                        } else if ("moderate" == parameters[1]) {
                            this.SafeSearch = SafeSearchValues.Moderate;
                        } else if ("strict" == parameters[1]) {
                            this.SafeSearch = SafeSearchValues.Strict;
                        }
                        break;
                    case "restriction":
                        this.Restriction = parameters[1];
                        break;
                    case "time":
                        if ("all_time" == parameters[1]) {
                            this.Time = UploadTime.AllTime;
                        } else if ("this_month" == parameters[1]) {
                            this.Time = UploadTime.ThisMonth;
                        } else if ("today" == parameters[1]) {
                            this.Time = UploadTime.Today;
                        } else if ("this_week" == parameters[1]) {
                            this.Time = UploadTime.ThisWeek;
                        } else {
                            this.Time = UploadTime.UploadTimeUndefined;
                        }
                        break;
                }
            }
            return this.Uri;
        }

        /// <summary>Creates the partial URI query string based on all
        ///  set properties.</summary> 
        /// <returns> string => the query part of the URI </returns>
        protected override string CalculateQuery(string basePath) {
            string path = base.CalculateQuery(basePath);
            StringBuilder newPath = new StringBuilder(path, 2048);
            char paramInsertion = InsertionParameter(path);
            if (this.formats != null) {
                string res = "";
                foreach (VideoFormat v in this.formats) {
                    if (res.Length > 0) {
                        res += ",";
                    }
                    res += (int)v;
                }

                if (res.Length > 0) {
                    newPath.Append(paramInsertion);
                    newPath.AppendFormat(CultureInfo.InvariantCulture, "format={0}", Utilities.UriEncodeReserved(res));
                    paramInsertion = '&';
                }
            }

            if (this.Time != UploadTime.UploadTimeUndefined) {
                string res = "";
                switch (this.Time) {
                    case UploadTime.AllTime:
                        res = "all_time";
                        break;
                    case UploadTime.ThisMonth:
                        res = "this_month";
                        break;
                    case UploadTime.ThisWeek:
                        res = "this_week";
                        break;
                    case UploadTime.Today:
                        res = "today";
                        break;
                }
                paramInsertion = AppendQueryPart(res, "time", paramInsertion, newPath);
            }

            if (this.SafeSearch != SafeSearchValues.Moderate) {
                string res = "";
                switch (this.SafeSearch) {
                    case SafeSearchValues.None:
                        res = "none";
                        break;
                    case SafeSearchValues.Strict:
                        res = "strict";
                        break;
                }
                paramInsertion = AppendQueryPart(res, "safeSearch", paramInsertion, newPath);
            }

            paramInsertion = AppendQueryPart(this.Location, "location", paramInsertion, newPath);
            paramInsertion = AppendQueryPart(this.LocationRadius, "location-radius", paramInsertion, newPath);
            paramInsertion = AppendQueryPart(this.Uploader, "uploader", paramInsertion, newPath);
            paramInsertion = AppendQueryPart(this.OrderBy, "orderby", paramInsertion, newPath);
            paramInsertion = AppendQueryPart(this.Client, "client", paramInsertion, newPath);
            paramInsertion = AppendQueryPart(this.LR, "lr", paramInsertion, newPath);
            paramInsertion = AppendQueryPart(this.Restriction, "restriction", paramInsertion, newPath);

            return newPath.ToString();
        }
    }

    /// <summary>
    /// A subclass of FeedQuery, to create an Activities Query for YouTube. 
    /// A user activity feed contains information about actions that an authenticated user's 
    /// friends have recently taken on the YouTube site. 
    public class ActivitiesQuery : FeedQuery {
        /// <summary>
        /// youTube events feed for friends activities  
        /// </summary>
        public const string ActivityFeedUri = "https://gdata.youtube.com/feeds/api/users/default/friendsactivity";

        /// <summary>
        /// base constructor
        /// </summary>
        public ActivitiesQuery()
            : base(ActivitiesQuery.ActivityFeedUri) {
        }
    }

    /// <summary>
    /// A subclass of FeedQuery, to create an Activities Query for YouTube. 
    /// A user activity feed contains information about actions that an authenticated user's 
    /// friends have recently taken on the YouTube site. 
    public class UserActivitiesQuery : FeedQuery {
        /// <summary>
        /// youTube events feed for friends activities  
        /// </summary>
        public const string ActivityFeedUri = "https://gdata.youtube.com/feeds/api/events";

        private List<string> authors = new List<string>();

        /// <summary>
        /// base constructor
        /// </summary>
        public UserActivitiesQuery()
            : base(UserActivitiesQuery.ActivityFeedUri) {
        }

        /// <summary>holds the list of authors we want to search for</summary> 
        public List<string> Authors {
            get { return this.authors; }
            set { this.authors = value; }
        }

        /// <summary>Creates the partial URI query string based on all
        ///  set properties.</summary> 
        /// <returns> string => the query part of the URI </returns>
        protected override string CalculateQuery(string basePath) {
            string path = base.CalculateQuery(basePath);
            StringBuilder newPath = new StringBuilder(path, 2048);
            char paramInsertion = InsertionParameter(path);

            string allAuthors = "";

            foreach (string s in this.authors) {
                if (allAuthors.Length > 0) {
                    allAuthors += ",";
                }
                allAuthors += s;
            }

            paramInsertion = AppendQueryPart(allAuthors, "author", paramInsertion, newPath);
            return newPath.ToString();
        }
    }
}


/*=======================================================================
  Copyright (C) Microsoft Corporation.  All rights reserved.

  THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
  PARTICULAR PURPOSE.
=======================================================================*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace RssToolkit.Rss
{
    /// <summary>
    /// RssItem
    /// </summary>
    [Serializable]
    public class RssItem
    {
        private string _author;
        private List<RssCategory> _categories;
        private string _comments;
        private string _description;
        private RssEnclosure _enclosure;
        private RssGuid _guid;
        private string _link;
        private string _pubDate;
        private string _title;
        private RssSource _source;

        /// <summary>
        /// Gets or sets the author.
        /// </summary>
        /// <value>The author.</value>
        [XmlElement("author")]
        public string Author
        {
            get 
            { 
                return _author; 
            }

            set 
            { 
                _author = value; 
            }
        }

        /// <summary>
        /// Gets or sets the categories.
        /// </summary>
        /// <value>The categories.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), XmlElement("category")]
        public List<RssCategory> Categories
        {
            get
            {
                return _categories;
            }

            set
            {
                _categories = value;
            }
        }

        /// <summary>
        /// Gets or sets the comments.
        /// </summary>
        /// <value>The comments.</value>
        [XmlElement("comments")]
        public string Comments
        {
            get
            {
                return _comments;
            }

            set
            {
                _comments = value;
            }
        }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        [XmlElement("description")]
        public string Description
        {
            get
            {
                return _description;
            }

            set
            {
                _description = value;
            }
        }

        /// <summary>
        /// Gets or sets the enclosure.
        /// </summary>
        /// <value>The enclosure.</value>
        [XmlElement("enclosure")]
        public RssEnclosure Enclosure
        {
            get
            {
                return _enclosure;
            }

            set
            {
                _enclosure = value;
            }
        }

        /// <summary>
        /// Gets or sets the GUID.
        /// </summary>
        /// <value>The GUID.</value>
        [XmlElement("guid")]
        public RssGuid Guid
        {
            get
            {
                return _guid;
            }

            set
            {
                _guid = value;
            }
        }

        /// <summary>
        /// Gets or sets the link.
        /// </summary>
        /// <value>The link.</value>
        [XmlElement("link")]
        public string Link
        {
            get
            {
                return _link;
            }

            set
            {
                _link = value;
            }
        }

        /// <summary>
        /// Gets or sets the pub date.
        /// </summary>
        /// <value>The pub date.</value>
        [XmlElement("pubDate")]
        public string PubDate
        {
            get
            {
                return _pubDate;
            }

            set
            {
                _pubDate = value;
            }   
        }

        /// <summary>
        /// Gets the parsed pub date.
        /// </summary>
        /// <value>The parsed pub date.</value>
        [XmlElement("pubDateParsed")]
        public System.DateTime PubDateParsed
        {
            get
            {
                return RssXmlHelper.Parse(_pubDate);
            }

            set
            {
                _pubDate = RssXmlHelper.ToRfc822(value);
            }
        }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        [XmlElement("title")]
        public string Title
        {
            get
            {
                return _title;
            }

            set
            {
                _title = value;
            }
        }

        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        /// <value>The source.</value>
        [XmlElement("source")]
        public RssSource Source
        {
            get
            {
                return _source;
            }

            set
            {
                _source = value;
            }
        }

        #region Blip specific

        /// <summary>
        /// The number of seconds the media object plays.
        /// </summary>
        [XmlElement(ElementName = "runtime", Namespace = "http://blip.tv/dtd/blip/1.0")]
        public string Blip_Runtime { get; set; }

        #endregion

        #region iTunes specific

        /// <summary>
        /// The number of seconds the media object plays.
        /// </summary>
        [XmlElement(ElementName = "duration", Namespace = "http://www.itunes.com/dtds/podcast-1.0.dtd")]
        public string iT_Duration { get; set; }

        #endregion        

        #region GameTrailers specific

        /// <summary>
        /// This URL points to the thumbnail image for the media (size: 178x74)
        /// </summary>
        [XmlElement(ElementName = "image", Namespace = "http://www.gametrailers.com/rssexplained.php")]
        public string GT_Image { get; set; }

        /// <summary>
        /// This is the title we've assigned to the media. (ex. "Trailer 3")
        /// </summary>
        [XmlElement(ElementName = "movieTitle", Namespace = "http://www.gametrailers.com/rssexplained.php")]
        public string GT_MovieTitle { get; set; }

        /// <summary>
        /// This is the name of the game the media is attached to. This name has been shortened to fit space restrictions, so may not match other references. (ex. "Worms 4")
        /// </summary>
        [XmlElement(ElementName = "gameName", Namespace = "http://www.gametrailers.com/rssexplained.php")]
        public string GT_GameName { get; set; }

        /// <summary>
        /// This is the unique ID assigned to the game the media is attached to.
        /// </summary>
        [XmlElement(ElementName = "gameID", Namespace = "http://www.gametrailers.com/rssexplained.php")]
        public string GT_GameId { get; set; }
        
        /// <summary>
        /// This URL points to the Gametrailers.com gamepage for the game the media is attached to.
        /// </summary>
        [XmlElement(ElementName = "gameURL", Namespace = "http://www.gametrailers.com/rssexplained.php")]
        public string GT_GameUrl { get; set; }

        /// <summary>
        /// For each platform that applies to the media, there is a platform tag (ex. "pc" OR "ps2")
        /// </summary>
        /// <value>The items.</value>
        [XmlElement(ElementName = "platform", Namespace = "http://www.gametrailers.com/rssexplained.php")]
        public List<string> GT_Platforms { get; set; }

        /// <summary>
        /// For each media type (quicktime and windows media are most common), there is a fileType tag. 
        /// </summary>
        [XmlElement(ElementName = "fileType", Namespace = "http://www.gametrailers.com/rssexplained.php")]
        public List<GT_File> GT_Files { get; set; }

        [Serializable]
        public class GT_File
        {           
            /// <summary>
            /// This is the extension of the fileType. (ex. "mov" for Quicktime movies)
            /// </summary>
            [XmlElement(ElementName = "type", Namespace = "")]
            public string Type { get; set; }

            /// <summary>
            /// This is the size of the media reported in Bytes.
            /// </summary>
            [XmlElement(ElementName = "fileSize", Namespace = "")]
            public int Size { get; set; }

            /// <summary>
            /// This URL is the link to the media in this format.
            /// </summary>
            [XmlElement(ElementName = "link", Namespace = "")]
            public string Url { get; set; }
        }

        #endregion

        #region Yahoo Media RSS specific
        
        /// <summary>
        /// Media objects that are not the same content should not be included in the same media:group element.
        /// The sequence of these items implies the order of presentation. 
        /// This element can be used to publish any type of media.
        /// </summary>
        [XmlElement(ElementName = "content", Namespace = "http://search.yahoo.com/mrss/")]
        public List<MediaContent> MediaContents { get; set; }

        /// <summary>
        ///  Allows grouping of media:content elements that are effectively the same content, yet different representations.
        ///  For instance: the same song recorded in both the WAV and MP3 format. 
        ///  It's an optional element that must only be used for this purpose.
        /// </summary>
        [XmlElement(ElementName = "group", Namespace = "http://search.yahoo.com/mrss/")]
        public List<MediaGroup> MediaGroups { get; set; }

        /// <summary>
        /// The title of the particular media object.
        /// </summary>
        [XmlElement(ElementName = "title", Namespace = "http://search.yahoo.com/mrss/")]
        public string MediaTitle { get; set; }

        /// <summary>
        /// Short description describing the media object typically a sentence in length.
        /// </summary>
        [XmlElement(ElementName = "description", Namespace = "http://search.yahoo.com/mrss/")]
        public string MediaDescription { get; set; }

        /// <summary>
        /// Allows particular images to be used as representative images for the media object.
        /// If multiple thumbnails are included, and time coding is not at play, it is assumed that the images are in order of importance.
        /// </summary>
        [XmlElement(ElementName = "thumbnail", Namespace = "http://search.yahoo.com/mrss/")]
        public List<MediaThumbnail> MediaThumbnails { get; set; }      

        [Serializable]
        public class MediaContent
        {
            /// <summary>
            /// Should specify the direct url to the media object. If not included, a media:player element must be specified.
            /// </summary>
            [XmlAttribute(AttributeName = "url")]
            public string Url { get; set; }

            /// <summary>
            /// The standard MIME type of the object. It is an optional attribute.
            /// </summary>
            [XmlAttribute(AttributeName = "type")]
            public string Type { get; set; }

            /// <summary>
            ///  The type of object (image | audio | video | document | executable). It is an optional attribute.
            /// </summary>
            [XmlAttribute(AttributeName = "medium")]
            public string Medium { get; set; }

            /// <summary>
            /// The number of seconds the media object plays. It is an optional attribute.
            /// </summary>
            [XmlAttribute(AttributeName = "duration")]
            public string Duration { get; set; }

            /// <summary>
            /// The width of the media object. It is an optional attribute.
            /// </summary>
            [XmlAttribute(AttributeName = "width")]
            public int Width { get; set; }

            /// <summary>
            /// The height of the media object. It is an optional attribute.
            /// </summary>
            [XmlAttribute(AttributeName = "height")]
            public int Height { get; set; }
           
            /// <summary>
            /// The bitrate of the mediaobject. It is an optional attribute.
            /// </summary>
            [XmlAttribute(AttributeName = "bitrate")]
            public float Bitrate { get; set; }

            /// <summary>
            /// The size in bytes of the mediaobject. It is an optional attribute.
            /// </summary>
            [XmlAttribute(AttributeName = "fileSize")]
            public string FileSize { get; set; }

            /// <summary>
            /// The primary language encapsulated in the media object. Language codes possible are detailed in RFC 3066. It is an optional attribute.
            /// </summary>
            [XmlAttribute(AttributeName = "lang")]
            public string Lang { get; set; }

            /// <summary>
            /// The title of the particular media object.
            /// </summary>
            [XmlElement(ElementName = "title", Namespace = "http://search.yahoo.com/mrss/")]
            public string MediaTitle { get; set; }

            /// <summary>
            /// Short description describing the media object typically a sentence in length.
            /// </summary>
            [XmlElement(ElementName = "description", Namespace = "http://search.yahoo.com/mrss/")]
            public string MediaDescription { get; set; }

            /// <summary>
            /// Allows particular images to be used as representative images for the media object.
            /// If multiple thumbnails are included, and time coding is not at play, it is assumed that the images are in order of importance.
            /// </summary>
            [XmlElement(ElementName = "thumbnail", Namespace = "http://search.yahoo.com/mrss/")]
            public List<MediaThumbnail> MediaThumbnails { get; set; }      
        }       

        [Serializable]
        public class MediaGroup
        {
            [XmlElement(ElementName = "content", Namespace = "http://search.yahoo.com/mrss/")]
            public List<MediaContent> MediaContents { get; set; }

            /// <summary>
            /// The title of the particular media object.
            /// </summary>
            [XmlElement(ElementName = "title", Namespace = "http://search.yahoo.com/mrss/")]
            public string MediaTitle { get; set; }

            /// <summary>
            /// Short description describing the media object typically a sentence in length.
            /// </summary>
            [XmlElement(ElementName = "description", Namespace = "http://search.yahoo.com/mrss/")]
            public string MediaDescription { get; set; }

            /// <summary>
            /// Allows particular images to be used as representative images for the media object.
            /// If multiple thumbnails are included, and time coding is not at play, it is assumed that the images are in order of importance.
            /// </summary>
            [XmlElement(ElementName = "thumbnail", Namespace = "http://search.yahoo.com/mrss/")]
            public List<MediaThumbnail> MediaThumbnails { get; set; }
        }

        [Serializable]
        public class MediaThumbnail
        {
            /// <summary>
            /// Specifies the url of the thumbnail. It is a required attribute.
            /// </summary>
            [XmlAttribute(AttributeName = "url")]
            public string Url { get; set; }

            public override string ToString()
            {
                return Url;
            }
        }

        #endregion
    }
}

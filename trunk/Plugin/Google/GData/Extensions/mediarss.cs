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
using System.Globalization;

namespace Google.GData.Extensions.MediaRss {

    /// <summary>
    /// helper to instantiate all factories defined in here and attach 
    /// them to a base object
    /// </summary>
    public class MediaRssExtensions
    {
        /// <summary>
        /// helper to add all MediaRss extensions to a base object
        /// </summary>
        /// <param name="baseObject"></param>
        public static void AddExtension(AtomBase baseObject) 
        {
            baseObject.AddExtension(new MediaGroup());
        }
    }


    /// <summary>
    /// short table for constants related to mediaRss declarations
    /// </summary>
    public class MediaRssNameTable 
    {
          /// <summary>static string to specify the mediarss namespace
          /// TODO: picasa has the namespace slighly wrong.
          /// </summary>
        public const string NSMediaRss  = "http://search.yahoo.com/mrss/";
        /// <summary>static string to specify the used mediarss prefix</summary>
        public const string mediaRssPrefix  = "media";
        /// <summary>static string to specify the media:group element</summary>
        public const string MediaRssGroup    = "group";
        /// <summary>static string to specify the media:credit element</summary>
        public const string MediaRssCredit    = "credit";
        /// <summary>static string to specify the media:content element</summary>
        public const string MediaRssContent    = "content";
        /// <summary>static string to specify the media:category element</summary>
        public const string MediaRssCategory    = "category";
        /// <summary>static string to specify the media:description element</summary>
        public const string MediaRssDescription    = "description";
         /// <summary>static string to specify the media:keywords element</summary>
        public const string MediaRssKeywords    = "keywords";
        /// <summary>static string to specify the media:thumbnail element</summary>
        public const string MediaRssThumbnail    = "thumbnail";
        /// <summary>static string to specify the media:title element</summary>
        public const string MediaRssTitle    = "title";
        /// <summary>static string to specify the media:rating element</summary>
        public const string MediaRssRating = "rating";
        /// <summary>static string to specify the media:restriction element</summary>
        public const string MediaRssRestriction = "restriction";
        /// <summary>static string to specify the media:player element</summary>
        public const string MediaRssPlayer    = "player";

        /// <summary>
        /// the attribute string for the URL attribute
        /// </summary>
        public const string AttributeUrl = "url";
        /// <summary>
        /// the attribute string for the height attribute
        /// </summary>
        public const string AttributeHeight = "height";
        /// <summary>
        /// the attribute string for the width attribute
        /// </summary>
        public const string AttributeWidth = "width";
        /// <summary>
        /// the attribute string for the time attribute
        /// </summary>
        public const string AttributeTime = "time";
        /// <summary>
        /// the attribute string for the type attribute
        /// </summary>
        public const string AttributeType = "type";
        /// <summary>
        /// the attribute string for the isDefault attribute
        /// </summary>
        public const string AttributeDefault = "isDefault";
        /// <summary>
        /// the attribute string for the expression attribute
        /// </summary>
        public const string AttributeExpression = "expression";
        /// <summary>
        /// the attribute string for the duration attribute
        /// </summary>
        public const string AttributeDuration = "duration";




    }

    /// <summary>
    /// MediaGroup container element for the MediaRss namespace
    /// </summary>
    public class MediaGroup : SimpleContainer
    {
        private ExtensionCollection<MediaThumbnail> thumbnails;
        private ExtensionCollection<MediaContent> contents;
        private ExtensionCollection<MediaCategory> categories;
        /// <summary>
        /// default constructor for media:group
        /// </summary>
        public MediaGroup() :
            base(MediaRssNameTable.MediaRssGroup,
                 MediaRssNameTable.mediaRssPrefix,
                 MediaRssNameTable.NSMediaRss)
        {
            this.ExtensionFactories.Add(new MediaCredit());
            this.ExtensionFactories.Add(new MediaDescription());
            this.ExtensionFactories.Add(new MediaContent());
            this.ExtensionFactories.Add(new MediaKeywords());
            this.ExtensionFactories.Add(new MediaThumbnail());
            this.ExtensionFactories.Add(new MediaTitle());
            this.ExtensionFactories.Add(new MediaCategory());
            this.ExtensionFactories.Add(new MediaRating());
            this.ExtensionFactories.Add(new MediaRestriction());
            this.ExtensionFactories.Add(new MediaPlayer());
        }

        /// <summary>
        /// returns the media:credit element
        /// </summary>
        public MediaCredit Credit
        {
            get
            {
                return FindExtension(MediaRssNameTable.MediaRssCredit,
                                     MediaRssNameTable.NSMediaRss) as MediaCredit;
            }
            set
            {
                ReplaceExtension(MediaRssNameTable.MediaRssCredit,
                                MediaRssNameTable.NSMediaRss,
                                value);
            }
        }

        /// <summary>
        /// returns the media:credit element
        /// </summary>
        public MediaRestriction Restriction
        {
            get
            {
                return FindExtension(MediaRssNameTable.MediaRssRestriction,
                                     MediaRssNameTable.NSMediaRss) as MediaRestriction;
            }
            set
            {
                ReplaceExtension(MediaRssNameTable.MediaRssRestriction,
                                MediaRssNameTable.NSMediaRss,
                                value);
            }
        }

        /// <summary>
        /// returns the media:content element
        /// </summary>
        public MediaContent Content
        {
            get
            {
                return FindExtension(MediaRssNameTable.MediaRssContent,
                                     MediaRssNameTable.NSMediaRss) as MediaContent;
            }
            set
            {
                ReplaceExtension(MediaRssNameTable.MediaRssContent,
                                MediaRssNameTable.NSMediaRss,
                                value);
            }
        }


        /// <summary>
        /// returns the media:credit element
        /// </summary>
        public MediaDescription Description
        {
            get
            {
                return FindExtension(MediaRssNameTable.MediaRssDescription,
                                     MediaRssNameTable.NSMediaRss) as MediaDescription;
            }
            set
            {
                ReplaceExtension(MediaRssNameTable.MediaRssDescription,
                                MediaRssNameTable.NSMediaRss,
                                value);
            }
        }
        /// <summary>
        /// The media:keywords tag contains a comma-separated list of words associated with a video. 
        /// You must provide at least one keyword for each video in your feed. This field has a 
        /// maximum length of 120 characters, including commas, and may contain all valid UTF-8 
        /// characters except &gt; and &lt;  In addition, each keyword must be at least two characters 
        /// long and may not be longer than 25 characters.
        /// </summary>
        public MediaKeywords Keywords
        {
            get
            {
                return FindExtension(MediaRssNameTable.MediaRssKeywords,
                                     MediaRssNameTable.NSMediaRss) as MediaKeywords;
            }
            set
            {
                ReplaceExtension(MediaRssNameTable.MediaRssKeywords,
                                MediaRssNameTable.NSMediaRss,
                                value);
            }
        }
        /// <summary>
        /// returns the media:credit element
        /// </summary>
        public MediaTitle Title
        {
            get
            {
                return FindExtension(MediaRssNameTable.MediaRssTitle,
                                     MediaRssNameTable.NSMediaRss) as MediaTitle;
            }
            set
            {
                ReplaceExtension(MediaRssNameTable.MediaRssTitle,
                                MediaRssNameTable.NSMediaRss,
                                value);
            }
        }

        /// <summary>
        /// returns the media:rating element
        /// </summary>
        public MediaRating Rating
        {
            get
            {
                return FindExtension(MediaRssNameTable.MediaRssRating,
                                     MediaRssNameTable.NSMediaRss) as MediaRating;
            }
            set
            {
                ReplaceExtension(MediaRssNameTable.MediaRssRating,
                                MediaRssNameTable.NSMediaRss,
                                value);
            }
        }

        /// <summary>
        ///  property accessor for the Thumbnails 
        /// </summary>
        public ExtensionCollection<MediaThumbnail> Thumbnails
        {
            get 
            {
                if (this.thumbnails == null)
                {
                    this.thumbnails = new ExtensionCollection<MediaThumbnail>(this); 
                }
                return this.thumbnails;
            }
        }

        
        /// <summary>
        ///  property accessor for the Contents Collection 
        /// </summary>
        public ExtensionCollection<MediaContent> Contents
        {
            get 
            {
                if (this.contents == null)
                {
                    this.contents = new ExtensionCollection<MediaContent>(this); 
                }
                return this.contents;
            }
        }

        /// <summary>
        ///  property accessor for the Category Collection 
        /// </summary>
        public ExtensionCollection<MediaCategory> Categories
        {
            get 
            {
                if (this.categories == null)
                {
                    this.categories = new ExtensionCollection<MediaCategory>(this); 
                }
                return this.categories;
            }
        }


        /// <summary>
        /// returns the media:player element
        /// </summary>
        public MediaPlayer Player
        {
            get
            {
                return FindExtension(MediaRssNameTable.MediaRssPlayer,
                                     MediaRssNameTable.NSMediaRss) as MediaPlayer;
            }
            set
            {
                ReplaceExtension(MediaRssNameTable.MediaRssPlayer,
                                MediaRssNameTable.NSMediaRss,
                                value);
            }
        }

   }

    /// <summary>
    /// media:credit schema extension describing an credit given to media
    /// it's a child of media:group
    /// </summary>
    public class MediaCredit : SimpleElement
    {
        /// <summary>
        /// default constructor for media:credit
        /// </summary>
        public MediaCredit()
        : base(MediaRssNameTable.MediaRssCredit, 
               MediaRssNameTable.mediaRssPrefix,
               MediaRssNameTable.NSMediaRss)
        {
            this.Attributes.Add("role", null);
            this.Attributes.Add("scheme", null);
        }

        /// <summary>
        /// default constructor for media:credit with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public MediaCredit(string initValue)
        : base(MediaRssNameTable.MediaRssCredit, 
               MediaRssNameTable.mediaRssPrefix,
               MediaRssNameTable.NSMediaRss, initValue)
        {
            this.Attributes.Add("role", null);
            this.Attributes.Add("scheme", null);
        }

        /// <summary>
        ///  returns the schem of the credit element
        /// </summary>
        /// <returns></returns>
        public string Scheme
        {
            get
            {
                return this.Attributes["scheme"] as string;
            }
            set
            {
                this.Attributes["scheme"] = value;
            }
        }
        /// <summary>
        ///  returns the role of the credit element
        /// </summary>
        /// <returns></returns>
        public string Role
        {
            get
            {
                return this.Attributes["role"] as string;
            }
            set
            {
                this.Attributes["role"] = value;
            }
        }
    }


    /// <summary>
    /// media:restriction schema extension identifies the country or countries where a
    ///  video may or may not be played. The attribute value is a space-delimited 
    /// list of ISO 3166 two-letter country codes. 
    /// </summary>
    public class MediaRestriction : SimpleElement
    {
        const string AttributeType = "type";
        const string AttributeRel = "relationship";
        /// <summary>
        /// default constructor for media:credit
        /// </summary>
        public MediaRestriction()
        : base(MediaRssNameTable.MediaRssRestriction,
               MediaRssNameTable.mediaRssPrefix,
               MediaRssNameTable.NSMediaRss)
        {
            this.Attributes.Add(AttributeType, null);
            this.Attributes.Add(AttributeRel, null);
        }

        /// <summary>
        /// default constructor for media:credit with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public MediaRestriction(string initValue)
        : base(MediaRssNameTable.MediaRssRestriction, 
               MediaRssNameTable.mediaRssPrefix,
               MediaRssNameTable.NSMediaRss, initValue)
        {
            this.Attributes.Add(AttributeType, null);
            this.Attributes.Add(AttributeRel, null);
        }

        /// <summary>
        ///  returns the schem of the credit element
        /// </summary>
        /// <returns></returns>
        public string Type
        {
            get
            {
                return this.Attributes[AttributeType] as string;
            }
            set
            {
                this.Attributes[AttributeType] = value;
            }
        }
        /// <summary>
        ///  returns the role of the credit element
        /// </summary>
        /// <returns></returns>
        public string Relationship
        {
            get
            {
                return this.Attributes[AttributeRel] as string;
            }
            set
            {
                this.Attributes[AttributeRel] = value;
            }
        }
    }



    /// <summary>
    /// media:description schema extension describing an description given to media
    /// it's a child of media:group
    /// </summary>
    public class MediaDescription : SimpleElement
    {
        /// <summary>
        /// default constructor for media:description 
        /// </summary>
        public MediaDescription()
        : base(MediaRssNameTable.MediaRssDescription, 
               MediaRssNameTable.mediaRssPrefix,
               MediaRssNameTable.NSMediaRss)
        {
            this.Attributes.Add("type", null);
        }

        /// <summary>
        /// default constructor for media:description with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public MediaDescription(string initValue)
        : base(MediaRssNameTable.MediaRssDescription, 
               MediaRssNameTable.mediaRssPrefix,
               MediaRssNameTable.NSMediaRss, initValue)
        {
            this.Attributes.Add("type", null);
        }
    }

    /// <summary>
    /// media:player schema extension describing the player URL
    /// it's a child of media:group
    /// </summary>
    public class MediaPlayer : SimpleElement
    {
        /// <summary>
        /// default constructor for media:content
        /// </summary>
        public MediaPlayer()
        : base(MediaRssNameTable.MediaRssPlayer, 
               MediaRssNameTable.mediaRssPrefix,
               MediaRssNameTable.NSMediaRss)
        {
            this.Attributes.Add(MediaRssNameTable.AttributeUrl, null);
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>convienience accessor for the Url</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Url
        {
           get 
            {
                return this.Attributes[MediaRssNameTable.AttributeUrl] as string;
            }
            set
            {
                this.Attributes[MediaRssNameTable.AttributeUrl] = value;
            }
        }
        // end of accessor public string Url
    }

    /// <summary>
    /// media:content schema extension describing the content URL
    /// it's a child of media:group
    /// </summary>
    public class MediaContent : SimpleElement
    {
        /// <summary>
        /// default constructor for media:content
        /// </summary>
        public MediaContent()
        : base(MediaRssNameTable.MediaRssContent, 
               MediaRssNameTable.mediaRssPrefix,
               MediaRssNameTable.NSMediaRss)
        {
            this.Attributes.Add(MediaRssNameTable.AttributeUrl, null);
            this.Attributes.Add(MediaRssNameTable.AttributeType, null);
            this.Attributes.Add(MediaRssNameTable.AttributeDefault, null);
            this.Attributes.Add(MediaRssNameTable.AttributeExpression, null);
            this.Attributes.Add(MediaRssNameTable.AttributeDuration, null);
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>convienience accessor for the Url</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Url
        {
           get 
            {
                return this.Attributes[MediaRssNameTable.AttributeUrl] as string;
            }
            set
            {
                this.Attributes[MediaRssNameTable.AttributeUrl] = value;
            }
        }
        // end of accessor public string Url


        //////////////////////////////////////////////////////////////////////
        /// <summary>convienience accessor for the Height</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Height
        {
           get 
            {
                return this.Attributes[MediaRssNameTable.AttributeHeight] as string;
            }
            set
            {
                this.Attributes[MediaRssNameTable.AttributeHeight] = value;
            }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>convienience accessor for the Width</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Width
        {
           get 
            {
                return this.Attributes[MediaRssNameTable.AttributeWidth] as string;
            }
            set
            {
                this.Attributes[MediaRssNameTable.AttributeWidth] = value;
            }
        }
        // end of accessor public string Url

        //////////////////////////////////////////////////////////////////////
        /// <summary>convienience accessor for the Width</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Type
        {
           get 
            {
                return this.Attributes[MediaRssNameTable.AttributeType] as string;
            }
            set
            {
                this.Attributes[MediaRssNameTable.AttributeType] = value;
            }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>convienience accessor for the Duration</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Duration
        {
            get
            {
                return this.Attributes[MediaRssNameTable.AttributeDuration] as string;
            }
            set
            {
                this.Attributes[MediaRssNameTable.AttributeDuration] = value;
            }
        }



    }

     /// <summary>
    /// media:content schema extension describing the content URL
    /// it's a child of media:group
    /// </summary>
    public class MediaCategory : SimpleElement
    {
        /// <summary>
        /// default constructor for media:content
        /// </summary>
        public MediaCategory()
        : base(MediaRssNameTable.MediaRssCategory, 
               MediaRssNameTable.mediaRssPrefix,
               MediaRssNameTable.NSMediaRss)
        {
            this.Attributes.Add("scheme", null);
            this.Attributes.Add("label", null);
        }

         /// <summary>
        /// default constructor for media:credit with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public MediaCategory(string initValue)
        : base(MediaRssNameTable.MediaRssCategory, 
               MediaRssNameTable.mediaRssPrefix,
               MediaRssNameTable.NSMediaRss, initValue)
        {
            this.Attributes.Add("scheme", null);
            this.Attributes.Add("lable", null);
        }

        /// <summary>
        /// Constructor for MediaCategory with an initial value
        /// and a scheme.
        /// </summary>
        /// <param name="initValue">The value of the tag</param>
        /// <param name="scheme">The scheme of the tag</param>
        public MediaCategory(string initValue, string scheme)
        : this(initValue)
        {
            this.Attributes["scheme"] = scheme;
        }
    }

    /// <summary>
    /// media:keywords schema extension describing a 
    /// comma seperated list of keywords
    /// it's a child of media:group
    /// </summary>
    public class MediaKeywords : SimpleElement
    {
        /// <summary>
        /// default constructor for media:keywords
        /// </summary>
        public MediaKeywords()
        : base(MediaRssNameTable.MediaRssKeywords, 
               MediaRssNameTable.mediaRssPrefix,
               MediaRssNameTable.NSMediaRss)
        {}

        /// <summary>
        /// default constructor for media:keywords with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public MediaKeywords(string initValue)
        : base(MediaRssNameTable.MediaRssKeywords,
               MediaRssNameTable.mediaRssPrefix,
               MediaRssNameTable.NSMediaRss, initValue)
        {}
    }

    /// <summary>
    /// media:thumbnail schema extension describing a 
    /// thumbnail URL with height/width
    /// it's a child of media:group
    /// </summary>
    public class MediaThumbnail : SimpleElement
    {
        /// <summary>
        /// default constructor for media:thumbnail
        /// </summary>
        public MediaThumbnail()
        : base(MediaRssNameTable.MediaRssThumbnail, 
               MediaRssNameTable.mediaRssPrefix,
               MediaRssNameTable.NSMediaRss)
        {
            this.Attributes.Add(MediaRssNameTable.AttributeUrl, null);
            this.Attributes.Add(MediaRssNameTable.AttributeHeight, null);
            this.Attributes.Add(MediaRssNameTable.AttributeWidth, null);
            this.Attributes.Add(MediaRssNameTable.AttributeTime, null);
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>convienience accessor for the Url</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Url
        {
           get 
            {
                return this.Attributes[MediaRssNameTable.AttributeUrl] as string;
            }
            set
            {
                this.Attributes[MediaRssNameTable.AttributeUrl] = value;
            }
        }
        // end of accessor public string Url


        //////////////////////////////////////////////////////////////////////
        /// <summary>convienience accessor for the Height</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Height
        {
           get 
            {
                return this.Attributes[MediaRssNameTable.AttributeHeight] as string;
            }
            set
            {
                this.Attributes[MediaRssNameTable.AttributeHeight] = value;
            }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>convienience accessor for the Width</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Width
        {
           get 
            {
                return this.Attributes[MediaRssNameTable.AttributeWidth] as string;
            }
            set
            {
                this.Attributes[MediaRssNameTable.AttributeWidth] = value;
            }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>convienience accessor for the time attribute</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Time
        {
            get
            {
                return this.Attributes[MediaRssNameTable.AttributeTime] as string;
            }
            set
            {
                this.Attributes[MediaRssNameTable.AttributeTime] = value;
            }
        }
        // end of accessor public string Url
    }

    /// <summary>
    /// media:title schema extension describing the title given to media
    /// it's a child of media:group
    /// </summary>
    public class MediaTitle : SimpleElement
    {
        /// <summary>
        /// default constructor for media:title
        /// </summary>
        public MediaTitle()
        : base(MediaRssNameTable.MediaRssTitle, 
               MediaRssNameTable.mediaRssPrefix,
               MediaRssNameTable.NSMediaRss)
        {
            this.Attributes.Add("type", null);
        }

        /// <summary>
        /// default constructor for media:title with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public MediaTitle(string initValue)
        : base(MediaRssNameTable.MediaRssTitle, 
               MediaRssNameTable.mediaRssPrefix,
               MediaRssNameTable.NSMediaRss, initValue)
        {
            this.Attributes.Add("type", null);
        }
    }

    /// <summary>
    /// media:rating schema extension describing the rating given to media
    /// it's a child of media:group
    /// </summary>
    public class MediaRating : SimpleElement
    {
        /// <summary>
        /// default constructor for media:rating
        /// </summary>
        public MediaRating()
            : base(MediaRssNameTable.MediaRssRating,
                   MediaRssNameTable.mediaRssPrefix,
                   MediaRssNameTable.NSMediaRss)
        {
            this.Attributes.Add("scheme", null);
            this.Attributes.Add("country", null);
        }

        /// <summary>
        /// default constructor for media:rating with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public MediaRating(string initValue)
            : base(MediaRssNameTable.MediaRssRating,
                   MediaRssNameTable.mediaRssPrefix,
                   MediaRssNameTable.NSMediaRss, initValue)
        {
            this.Attributes.Add("scheme", null);
            this.Attributes.Add("country", null);
        }

        /// <summary>
        /// Constructor for MediaRating with an initial value
        /// and a scheme.
        /// </summary>
        /// <param name="initValue">The value of the tag</param>
        /// <param name="scheme">The scheme of the tag</param>
        public MediaRating(string initValue, string scheme)
        : this(initValue)
        {
            this.Attributes["scheme"] = scheme;
        }

        /// <summary>
        ///  returns the schem for the media rating
        /// </summary>
        /// <returns></returns>
        public string Scheme
        {
            get
            {
                return this.Attributes["scheme"] as string;
            }
            set
            {
                this.Attributes["scheme"] = value;
            }
        }

        /// <summary>
        ///  returns the country for this rating
        /// </summary>
        /// <returns></returns>
        public string Country
        {
            get
            {
                return this.Attributes["country"] as string;
            }
            set
            {
                this.Attributes["country"] = value;
            }
        }
    }
}

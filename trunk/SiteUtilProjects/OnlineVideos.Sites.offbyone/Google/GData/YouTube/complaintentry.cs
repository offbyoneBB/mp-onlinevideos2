/* Copyright (c) 2008 Google Inc.
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
/* Change history
* Oct 13 2008  Joe Feser       joseph.feser@gmail.com
* Removed warnings
* 
*/
#define USE_TRACING

using System;
using System.Xml;
using System.IO;
using System.Collections;
using Google.GData.Client;
using Google.GData.Extensions;

namespace Google.GData.YouTube {
    /// <summary>
    /// Entry API customization class for defining comment entries in an comments feed.
    /// </summary>
    public class ComplaintEntry : YouTubeBaseEntry {
        /// <summary>
        /// Describes the nature of the complaint
        /// </summary>
        public enum ComplaintType {
            /// <summary>
            /// The video contains sexual content
            /// </summary>
            PORN,
            /// <summary>
            /// The video contains violent or repulsive content
            /// </summary>
            VIOLENCE,
            /// <summary>
            /// The video contains hateful or abusive content
            /// </summary>
            HATE,
            /// <summary>
            /// The video contains harmful or dangerous acts
            /// </summary>
            DANGEROUS,
            /// <summary>
            /// The video infringes on the complainant's rights or copyright.
            /// </summary>
            RIGHTS,
            /// <summary>
            /// The video is clearly spam.
            /// </summary>
            SPAM,
            /// <summary>
            /// The type of complaint is not set yet.
            /// </summary>
            UNKNOWN
        }

        /// <summary>
        /// Constructs a new CommentEntry instance
        /// </summary>
        public ComplaintEntry() : base() {
            Tracing.TraceMsg("Created complaint entry");

            Content c = new Content();
            this.AddExtension(c);
        }

        /// <summary>
        /// getter/setter for yt:content subelement
        /// </summary>
        public string Complaint {
            get {
                return getYouTubeExtensionValue(YouTubeNameTable.Content);
            }
            set {
                setYouTubeExtension(YouTubeNameTable.Content, value);
            }
        }

        /// <summary>
        /// gets and sets the associated atom:category
        /// </summary>
        public ComplaintType Type {
            get {
                ComplaintType t = ComplaintType.UNKNOWN;

                foreach (AtomCategory category in this.Categories) {
                    if (category.Scheme == YouTubeNameTable.ComplaintCategorySchema) {
                        try {
                            t = (ComplaintType)Enum.Parse(typeof(ComplaintType), category.Term, true);
                        } catch (ArgumentException) {
                            t = ComplaintType.UNKNOWN;
                        }
                    }
                }
                return t;
            }
            set {
                AtomCategory cat = null;
                foreach (AtomCategory category in this.Categories) {
                    if (category.Scheme == YouTubeNameTable.ComplaintCategorySchema) {
                        cat = category;
                        break;
                    }
                }
                if (cat == null) {
                    cat = new AtomCategory();
                    this.Categories.Add(cat);
                }
                cat.Term = value.ToString();
                cat.Scheme = new AtomUri(YouTubeNameTable.ComplaintCategorySchema);
            }
        }
    }
}




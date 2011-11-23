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
/* Change history
* Oct 13 2008  Joe Feser       joseph.feser@gmail.com
* Converted ArrayLists and other .NET 1.1 collections to use Generics
* Combined IExtensionElement and IExtensionElementFactory interfaces
* 
*/
#define USE_TRACING

using System;
using System.Xml;
using System.IO;
using System.Collections;
using Google.GData.Client;
using Google.GData.Extensions;
using Google.GData.Extensions.MediaRss;
using System.Globalization;

namespace Google.GData.YouTube {
    /// <summary>
    /// A user profile contains information that the user lists on his YouTube profile page.
    /// </summary>
    public class ProfileEntry : YouTubeBaseEntry {
        /// <summary>
        /// Category used to label entries that friends
        /// </summary>
        public static AtomCategory PROFILE_CATEGORY =
        new AtomCategory(YouTubeNameTable.KIND_USER_PROFILE, new AtomUri(BaseNameTable.gKind));

        private ExtensionCollection<FeedLink> links;
        /// <summary>
        /// Constructs a new ProfileEntry instance
        /// </summary>
        public ProfileEntry()
            : base() {
            Tracing.TraceMsg("Created ProfileEntry");
            Categories.Add(PROFILE_CATEGORY);

            this.AddExtension(new Age());
            this.AddExtension(new Books());
            this.AddExtension(new Company());
            this.AddExtension(new FirstName());
            this.AddExtension(new LastName());
            this.AddExtension(new Hobbies());
            this.AddExtension(new Gender());
            this.AddExtension(new Location());
            this.AddExtension(new Movies());
            this.AddExtension(new Music());
            this.AddExtension(new Occupation());
            this.AddExtension(new School());
            this.AddExtension(new UserName());
            this.AddExtension(new Statistics());
            this.AddExtension(new FeedLink());
            this.AddExtension(new Description());
            this.AddExtension(new Relationship());
            this.AddExtension(new HomeTown());
        }

        /// <summary>
        /// The yt:age tag specifies the user's age, which is calculated based on the birthdate provided 
        /// </summary>
        /// <returns></returns>
        public int Age {
            get {
                return Convert.ToInt32(getYouTubeExtensionValue(YouTubeNameTable.Age), CultureInfo.InvariantCulture);
            }
            set {
                setYouTubeExtension(YouTubeNameTable.Age, value.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// The yt:company tag identifies the company that the user works for as entered by the user 
        /// in the user's public YouTube profile
        /// </summary>
        public string Company {
            get {
                return getYouTubeExtensionValue(YouTubeNameTable.Company);
            }
            set {
                setYouTubeExtension(YouTubeNameTable.Company, value);
            }
        }

        /// <summary>
        /// The yt:books tag identifies the user's favorite books as entered in the user's YouTube public profile
        /// </summary>
        public string Books {
            get {
                return getYouTubeExtensionValue(YouTubeNameTable.Books);
            }
            set {
                setYouTubeExtension(YouTubeNameTable.Books, value);
            }
        }

        /// <summary>
        /// the users firstname per public profile
        /// </summary>
        public string Firstname {
            get {
                return getYouTubeExtensionValue(YouTubeNameTable.FirstName);
            }
            set {
                setYouTubeExtension(YouTubeNameTable.FirstName, value);
            }
        }

        /// <summary>
        /// the users lastname per public profile
        /// </summary>
        public string Lastname {
            get {
                return getYouTubeExtensionValue(YouTubeNameTable.LastName);
            }
            set {
                setYouTubeExtension(YouTubeNameTable.LastName, value);
            }
        }

        /// <summary>
        /// the users hobbies per public profile
        /// </summary>
        public string Hobbies {
            get {
                return getYouTubeExtensionValue(YouTubeNameTable.Hobbies);
            }
            set {
                setYouTubeExtension(YouTubeNameTable.Hobbies, value);
            }
        }

        /// <summary>
        /// the users gender per public profile
        /// </summary>
        public string Gender {
            get {
                return getYouTubeExtensionValue(YouTubeNameTable.Gender);
            }
            set {
                setYouTubeExtension(YouTubeNameTable.Gender, value);
            }
        }

        /// <summary>
        /// the users location per public profile
        /// </summary>
        public string Location {
            get {
                return getYouTubeExtensionValue(YouTubeNameTable.Location);
            }
            set {
                setYouTubeExtension(YouTubeNameTable.Location, value);
            }
        }

        /// <summary>
        /// the users favorite movies per public profile
        /// </summary>
        public string Movies {
            get {
                return getYouTubeExtensionValue(YouTubeNameTable.Movies);
            }
            set {
                setYouTubeExtension(YouTubeNameTable.Movies, value);
            }
        }

        /// <summary>
        /// the users favorite music per public profile
        /// </summary>
        public string Music {
            get {
                return getYouTubeExtensionValue(YouTubeNameTable.Music);
            }
            set {
                setYouTubeExtension(YouTubeNameTable.Music, value);
            }
        }

        /// <summary>
        /// the users occupation per public profile
        /// </summary>
        public string Occupation {
            get {
                return getYouTubeExtensionValue(YouTubeNameTable.Occupation);
            }
            set {
                setYouTubeExtension(YouTubeNameTable.Occupation, value);
            }
        }

        /// <summary>
        /// the users school per public profile
        /// </summary>
        public string School {
            get {
                return getYouTubeExtensionValue(YouTubeNameTable.School);
            }
            set {
                setYouTubeExtension(YouTubeNameTable.School, value);
            }
        }

        /// <summary>
        /// getter/setter for UserName subelement
        /// </summary>
        public string UserName {
            get {
                return getYouTubeExtensionValue(YouTubeNameTable.UserName);
            }
            set {
                setYouTubeExtension(YouTubeNameTable.UserName, value);
            }
        }

        /// <summary>
        /// The yt:relationship tag identifies the user's relationship status
        /// </summary>
        public string Relationship {
            get {
                return getYouTubeExtensionValue(YouTubeNameTable.Relationship);
            }
            set {
                setYouTubeExtension(YouTubeNameTable.Relationship, value);
            }
        }

        /// <summary>
        /// The yt:hometown tag identifies the user's hometown
        /// </summary>
        public string Hometown {
            get {
                return getYouTubeExtensionValue(YouTubeNameTable.HomeTown);
            }
            set {
                setYouTubeExtension(YouTubeNameTable.HomeTown, value);
            }
        }

        /// <summary>
        /// returns the yt:statistics element
        /// </summary>
        /// <returns></returns>
        public Statistics Statistics {
            get {
                return FindExtension(YouTubeNameTable.Statistics,
                    YouTubeNameTable.NSYouTube) as Statistics;
            }
            set {
                ReplaceExtension(YouTubeNameTable.Statistics,
                    YouTubeNameTable.NSYouTube,
                    value);
            }
        }

        /// <summary>
        ///  property accessor for the Thumbnails 
        /// </summary>
        public ExtensionCollection<FeedLink> FeedLinks {
            get {
                if (this.links == null) {
                    this.links = new ExtensionCollection<FeedLink>(this);
                }
                return this.links;
            }
        }

        /// <summary>
        /// getter/setter for Description subelement
        /// </summary>
        [Obsolete("replaced with Summary.Text")]
        public string Description {
            get {
                return getDescription();
            }
            set {
                setDescription(value);
            }
        }
    }
}

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
#region Using directives

#define USE_TRACING

using System;
using System.Xml;
using System.IO;
using System.Globalization;
using System.ComponentModel;
using System.Runtime.InteropServices;

#endregion

// <summary>Contains AtomSource, an object to represent the atom:source
// element.</summary> 
namespace Google.GData.Client {
    /// <summary>TypeConverter, so that AtomHead shows up in the property pages
    /// </summary> 
    [ComVisible(false)]
    public class AtomSourceConverter : ExpandableObjectConverter {
        ///<summary>Standard type converter method</summary>
        public override bool CanConvertTo(ITypeDescriptorContext context, System.Type destinationType) {
            if (destinationType == typeof(AtomSource) || destinationType == typeof(AtomFeed)) {
                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }

        ///<summary>Standard type converter method</summary>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, System.Type destinationType) {
            AtomSource atomSource = value as AtomSource;

            if (destinationType == typeof(System.String) && atomSource != null) {
                return "Feed: " + atomSource.Title;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

    }

    /// <summary>Represents the AtomSource object. If an atom:entry is copied from one feed 
    /// into another feed, then the source atom:feed's metadata (all child elements of atom:feed other
    /// than the atom:entry elements) MAY be preserved within the copied entry by adding an atom:source 
    /// child element, if it is not already present in the entry, and including some or all of the source
    /// feed's Metadata elements as the atom:source element's children. Such metadata SHOULD be preserved 
    /// if the source atom:feed contains any of the child elements atom:author, atom:contributor, 
    /// atom:rights, or atom:category and those child elements are not present in the source atom:entry.
    /// </summary> 
    /*
    atomSource =
        element atom:source {
           atomCommonAttributes,
           (atomAuthor?
            & atomCategory*
            & atomContributor*
            & atomGenerator?
            & atomIcon?
            & atomId?
            & atomLink*
            & atomLogo?
            & atomRights?
            & atomSubtitle?
            & atomTitle?
            & atomUpdated?
            & extensionElement*)
        }
    */

    [TypeConverterAttribute(typeof(AtomSourceConverter)), DescriptionAttribute("Expand to see the options for the feed")]
    public class AtomSource : AtomBase {
        /// <summary>author collection</summary> 
        private AtomPersonCollection authors;
        /// <summary>contributors collection</summary> 
        private AtomPersonCollection contributors;
        /// <summary>category collection</summary> 
        private AtomCategoryCollection categories;
        /// <summary>the generator</summary> 
        private AtomGenerator generator;
        /// <summary>icon, essentially an atom link</summary> 
        private AtomIcon icon;
        /// <summary>ID</summary> 
        private AtomId id;
        /// <summary>link collection</summary> 
        private AtomLinkCollection links;
        /// <summary>logo, essentially an image link</summary> 
        private AtomLogo logo;
        /// <summary>rights, former copyrights</summary> 
        private AtomTextConstruct rights;
        /// <summary>subtitle as string</summary> 
        private AtomTextConstruct subTitle;
        /// <summary>title property as string</summary> 
        private AtomTextConstruct title;
        /// <summary>updated time stamp</summary> 
        private DateTime updated;

        /// <summary>public void AtomSource()</summary>
        public AtomSource() {
        }

        /// <summary>public AtomSource(AtomFeed feed)</summary> 
        public AtomSource(AtomFeed feed)
            : this() {
            Tracing.Assert(feed != null, "feed should not be null");
            if (feed == null) {
                throw new ArgumentNullException("feed");
            }

            // now copy them
            this.authors = feed.Authors;
            this.contributors = feed.Contributors;
            this.categories = feed.Categories;
            this.Generator = feed.Generator;
            this.Icon = feed.Icon;
            this.Logo = feed.Logo;
            this.Id = feed.Id;
            this.links = feed.Links;
            this.Rights = feed.Rights;
            this.Subtitle = feed.Subtitle;
            this.Title = feed.Title;
            this.Updated = feed.Updated;
        }
        
        /// <summary>accessor method public Authors AtomPersonCollection</summary> 
        /// <returns> </returns>
        public AtomPersonCollection Authors {
            get {
                if (this.authors == null) {
                    this.authors = new AtomPersonCollection();
                }
                return this.authors;
            }
        }

        /// <summary>accessor method public Contributors AtomPersonCollection</summary> 
        /// <returns> </returns>
        public AtomPersonCollection Contributors {
            get {
                if (this.contributors == null) {
                    this.contributors = new AtomPersonCollection();
                }
                return this.contributors;
            }
        }

        /// <summary>accessor method public Links AtomLinkCollection</summary> 
        /// <returns> </returns>
        public AtomLinkCollection Links {
            get {
                if (this.links == null) {
                    this.links = new AtomLinkCollection();
                }
                return this.links;
            }
        }

        /// <summary>returns the category collection</summary> 
        public AtomCategoryCollection Categories {
            get {
                if (this.categories == null) {
                    this.categories = new AtomCategoryCollection();
                }
                return this.categories;
            }
        }

        /// <summary>accessor method public FeedGenerator Generator</summary> 
        /// <returns> </returns>
        public AtomGenerator Generator {
            get { return this.generator; }
            set { this.Dirty = true; this.generator = value; }
        }

        /// <summary>accessor method public AtomIcon Icon</summary> 
        /// <returns> </returns>
        public AtomIcon Icon {
            get { return this.icon; }
            set { this.Dirty = true; this.icon = value; }
        }

        /// <summary>accessor method public AtomLogo Logo</summary> 
        /// <returns> </returns>
        public AtomLogo Logo {
            get { return this.logo; }
            set { this.Dirty = true; this.logo = value; }
        }

        /// <summary>accessor method public DateTime LastUpdated</summary> 
        /// <returns> </returns>
        public DateTime Updated {
            get { return this.updated; }
            set { this.Dirty = true; this.updated = value; }
        }

        /// <summary>accessor method public string Title</summary> 
        /// <returns> </returns>
        public AtomTextConstruct Title {
            get { return this.title; }
            set { this.Dirty = true; this.title = value; }
        }

        /// <summary>accessor method public string Subtitle</summary> 
        /// <returns> </returns>
        public AtomTextConstruct Subtitle {
            get { return this.subTitle; }
            set { this.Dirty = true; this.subTitle = value; }
        }

        /// <summary>accessor method public string Id</summary> 
        /// <returns> </returns>
        public AtomId Id {
            get { return this.id; }
            set { this.Dirty = true; this.id = value; }
        }

        /// <summary>accessor method public string Rights</summary> 
        /// <returns> </returns>
        public AtomTextConstruct Rights {
            get { return this.rights; }
            set { this.Dirty = true; this.rights = value; }
        }

        #region Persistence overloads

        /// <summary>Returns the constant representing this XML element.</summary> 
        public override string XmlName {
            get { return AtomParserNameTable.XmlSourceElement; }
        }

        /// <summary>saves the inner state of the element</summary> 
        /// <param name="writer">the xmlWriter to save into </param>
        protected override void SaveInnerXml(XmlWriter writer) {
            base.SaveInnerXml(writer);
            // saving Authors
            foreach (AtomPerson person in this.Authors) {
                person.SaveToXml(writer);
            }

            // saving Contributors
            foreach (AtomPerson person in this.Contributors) {
                person.SaveToXml(writer);
            }

            // saving Categories
            foreach (AtomCategory category in this.Categories) {
                category.SaveToXml(writer);
            }

            // saving the generator
            if (this.Generator != null) {
                this.Generator.SaveToXml(writer);
            }

            // save the icon
            if (this.Icon != null) {
                this.Icon.SaveToXml(writer);
            }

            // save the logo
            if (this.Logo != null) {
                this.Logo.SaveToXml(writer);
            }

            // save the ID
            if (this.Id != null) {
                this.Id.SaveToXml(writer);
            }

            // save the Links
            foreach (AtomLink link in this.Links) {
                link.SaveToXml(writer);
            }

            if (this.Rights != null) {
                this.Rights.SaveToXml(writer);
            }

            if (this.Subtitle != null) {
                this.Subtitle.SaveToXml(writer);
            }

            if (this.Title != null) {
                this.Title.SaveToXml(writer);
            }

            // date time construct, save here.
            WriteLocalDateTimeElement(writer, AtomParserNameTable.XmlUpdatedElement, this.Updated);
        }

        #endregion

        #region overloaded for property changes, xml:base

        /// <summary>just go down the child collections</summary> 
        /// <param name="uriBase"> as currently calculated</param>
        internal override void BaseUriChanged(AtomUri uriBase) {
            base.BaseUriChanged(uriBase);

            foreach (AtomPerson person in this.Authors) {
                person.BaseUriChanged(uriBase);
            }

            // saving Contributors
            foreach (AtomPerson person in this.Contributors) {
                person.BaseUriChanged(uriBase);
            }

            // saving Categories
            foreach (AtomCategory category in this.Categories) {
                category.BaseUriChanged(uriBase);
            }

            // saving the generator
            if (this.Generator != null) {
                this.Generator.BaseUriChanged(uriBase);
            }

            // save the icon
            if (this.Icon != null) {
                this.Icon.BaseUriChanged(uriBase);
            }

            // save the logo
            if (this.Logo != null) {
                this.Logo.BaseUriChanged(uriBase);
            }

            // save the ID
            if (this.Id != null) {
                this.Id.BaseUriChanged(uriBase);
            }

            // save the Links
            foreach (AtomLink link in this.Links) {
                link.BaseUriChanged(uriBase);
            }

            if (this.Rights != null) {
                this.Rights.BaseUriChanged(uriBase);
            }

            if (this.Subtitle != null) {
                this.Subtitle.BaseUriChanged(uriBase);
            }

            if (this.Title != null) {
                this.Title.BaseUriChanged(uriBase);
            }
        }

        /// <summary>calls the action on this object and all children</summary> 
        /// <param name="action">an IAtomBaseAction interface to call </param>
        /// <returns>true or false, pending outcome</returns>
        public override bool WalkTree(IBaseWalkerAction action) {
            if (base.WalkTree(action)) {
                return true;
            }

            foreach (AtomPerson person in this.Authors) {
                if (person.WalkTree(action)) {
                    return true;
                }
            }

            // saving Contributors
            foreach (AtomPerson person in this.Contributors) {
                if (person.WalkTree(action)) {
                    return true;
                }
            }

            // saving Categories
            foreach (AtomCategory category in this.Categories) {
                if (category.WalkTree(action)) {
                    return true;
                }
            }

            // saving the generator
            if (this.Generator != null) {
                if (this.Generator.WalkTree(action)) {
                    return true;
                }
            }

            // save the icon
            if (this.Icon != null) {
                if (this.Icon.WalkTree(action)) {
                    return true;
                }
            }

            // save the logo
            if (this.Logo != null) {
                if (this.Logo.WalkTree(action)) {
                    return true;
                }
            }

            // save the ID
            if (this.Id != null) {
                if (this.Id.WalkTree(action)) {
                    return true;
                }
            }

            // save the Links
            foreach (AtomLink link in this.Links) {

                if (link.WalkTree(action)) {
                    return true;
                }
            }

            if (this.Rights != null) {
                if (this.Rights.WalkTree(action)) {
                    return true;
                }
            }

            if (this.Subtitle != null) {
                if (this.Subtitle.WalkTree(action)) {
                    return true;
                }
            }

            if (this.Title != null) {
                if (this.Title.WalkTree(action)) {
                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}


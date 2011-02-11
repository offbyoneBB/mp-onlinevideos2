/* Copyright (c) 2006-2008 Google Inc.
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
#region Using directives

#define USE_TRACING

using System;
using System.Xml;
using System.Net;
using System.IO;
using System.Collections;
using System.Globalization;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Google.GData.Extensions.AppControl;

#endregion

//////////////////////////////////////////////////////////////////////
// <summary>Contains AtomEntry, an object to represent the atom:entry
// element.</summary>
//////////////////////////////////////////////////////////////////////
namespace Google.GData.Client
{
#if WindowsCE || PocketPC
#else 
    //////////////////////////////////////////////////////////////////////
    /// <summary>TypeConverter, so that AtomEntry shows up in the property pages
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    [ComVisible(false)]
    public class AtomEntryConverter : ExpandableObjectConverter
    {
        ///<summary>Standard type converter method</summary>
        public override bool CanConvertTo(ITypeDescriptorContext context, System.Type destinationType) 
        {
            if (destinationType == typeof(AtomEntry))
                return true;
        
            return base.CanConvertTo(context, destinationType);
        }

        ///<summary>Standard type converter method</summary>
        public override object ConvertTo(ITypeDescriptorContext context,CultureInfo culture, object value, System.Type destinationType) 
        {
            AtomEntry entry = value as AtomEntry; 
            if (destinationType == typeof(System.String) && entry != null)
            {
                return "Entry: " + entry.Title;
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
        
    }
    /////////////////////////////////////////////////////////////////////////////


    //////////////////////////////////////////////////////////////////////
    /// <summary>AtomEntry object, representing an item in the RSS/Atom feed
    ///  Version 1.0 removed atom-Head
    ///    element atom:entry {
    ///       atomCommonAttributes,
    ///       (atomAuthor*
    ///         atomCategory*
    ///        atomContent?
    ///        atomContributor*
    ///        atomId
    ///        atomLink*
    ///        atomPublished?
    ///        atomRights?
    ///        atomSource?
    ///        atomSummary?
    ///        atomTitle
    ///        atomUpdated
    ///        extensionElement*)
    ///    }
    ///  </summary>
    //////////////////////////////////////////////////////////////////////
    [TypeConverterAttribute(typeof(AtomEntryConverter)), DescriptionAttribute("Expand to see the entry objects for the feed.")]
#endif
    public class AtomEntry : AtomBase
    {
        #region standard entry properties as returned by query
        /// <summary>/feed/entry/title property as string</summary> 
        private AtomTextConstruct title;
        /// <summary>/feed/entry/id property as string</summary> 
        private AtomId id;
        /// <summary>/feed/entry/link collection</summary> 
        private AtomLinkCollection   links;
        /// <summary>/feed/entry/updated property as string</summary> 
        private DateTime lastUpdateDate;
        /// <summary>/feed/entry/published property as string</summary> 
        private DateTime publicationDate;
        /// <summary>/feed/entry/author property as Author object</summary> 
        private AtomPersonCollection authors;
        /// <summary>/feed/entry/atomContributor property as Author object</summary> 
        private AtomPersonCollection contributors;
        /// <summary>The "atom:rights" element is a Text construct that conveys a human-readable copyright statement for an entry or feed.</summary> 
        private AtomTextConstruct rights;
        /// <summary>/feed/entry/category/@term property as a list of AtomCategories</summary> 
        private AtomCategoryCollection categories; 
        /// <summary>The "atom:summary" element is a Text construct that conveys a short summary, abstract or excerpt of an entry.</summary> 
        private AtomTextConstruct summary;

        /// <summary>contains the content as an object</summary> 
        private AtomContent content;

        /// <summary>atom:source element</summary> 
        private AtomSource source;
        /// <summary>GData service to use</summary> 
        private IService service;
        /// <summary>holds the owning feed</summary> 
        private AtomFeed feed; 
        // holds batch information for an entry
        private GDataBatchEntryData batchData;  

        

        #endregion



        #region Persistence overloads
        //////////////////////////////////////////////////////////////////////
        /// <summary>Returns the constant representing this XML element.</summary> 
        //////////////////////////////////////////////////////////////////////
        public override string XmlName 
        {
            get { return AtomParserNameTable.XmlAtomEntryElement; }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>checks to see if we are a batch feed, if so, adds the batchNS</summary> 
        /// <param name="writer">the xmlwriter, where we want to add default namespaces to</param>
        //////////////////////////////////////////////////////////////////////
        protected override void AddOtherNamespaces(XmlWriter writer) 
        {
            base.AddOtherNamespaces(writer); 
            if (this.BatchData != null)
            {
                Utilities.EnsureGDataBatchNamespace(writer); 
            }
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>checks if this is a namespace 
        /// decl that we already added</summary> 
        /// <param name="node">XmlNode to check</param>
        /// <returns>true if this node should be skipped </returns>
        //////////////////////////////////////////////////////////////////////
        protected override bool SkipNode(XmlNode node)
        {
            if (base.SkipNode(node)==true)
            {
                return true; 
            }

            Tracing.TraceMsg("in skipnode for node: " + node.Name + "--" + node.Value); 
            if (this.BatchData != null)
            {
				if (node.NodeType == XmlNodeType.Attribute &&
					node.Name.StartsWith("xmlns") &&
					(String.Compare(node.Value, BaseNameTable.gBatchNamespace) == 0)) {
					return true;
				}
            }
            return false; 
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>saves the inner state of the element</summary> 
        /// <param name="writer">the xmlWriter to save into </param>
        //////////////////////////////////////////////////////////////////////
        protected override void SaveInnerXml(XmlWriter writer)
        {
            // saving title
            Tracing.TraceMsg("Entering save inner XML on AtomEntry");

            if (this.batchData != null)
            {
                this.batchData.Save(writer);
            }

            if (this.title != null)
            {
                Tracing.TraceMsg("Saving Title: " + this.Title.Text);
                this.Title.SaveToXml(writer);
            }
            if (this.id != null)
            {
                this.Id.SaveToXml(writer);
            }
            foreach (AtomLink link in this.Links )
            {
                link.SaveToXml(writer);
            }
            foreach (AtomPerson person in this.Authors )
            {
                person.SaveToXml(writer);
            }
            foreach (AtomPerson person in this.Contributors )
            {
                person.SaveToXml(writer);
            }
            foreach (AtomCategory category in this.Categories )
            {
                category.SaveToXml(writer);
            }
            if (this.rights != null)
            {
                this.Rights.SaveToXml(writer);
            }
            if (this.summary != null)
            {
                this.Summary.SaveToXml(writer);
            }
            if (this.content != null)
            {
                this.Content.SaveToXml(writer);
            }
            if (this.source != null)
            {
                this.Source.SaveToXml(writer);
            }

            WriteLocalDateTimeElement(writer, AtomParserNameTable.XmlUpdatedElement, this.Updated);
            WriteLocalDateTimeElement(writer, AtomParserNameTable.XmlPublishedElement, this.Published);
        }
        /////////////////////////////////////////////////////////////////////////////
        #endregion


        
        /// <summary>
        /// default AtomEntry constructor. Adds the AppControl element
        /// as a default extension
        /// </summary>
        public AtomEntry()
        {
            this.AddExtension(new AppControl());
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for feed</summary> 
        //////////////////////////////////////////////////////////////////////
        public AtomFeed Feed
        {
            get {return this.feed;}
        }
        /////////////////////////////////////////////////////////////////////////////

        /// <summary>internal method to set the feed</summary> 
        internal void setFeed(AtomFeed feed)
        {
            if (feed != null)
            {
                this.Dirty = true; 
                this.Service = feed.Service;
            }
            this.feed = feed; 
        }



        //////////////////////////////////////////////////////////////////////
        /// <summary>helper method to create a new, decoupled entry based on a feedEntry</summary> 
        /// <param name="entryToImport">the entry from a feed that you want to put somewhere else</param>
        /// <returns> the new entry ready to be inserted</returns>
        //////////////////////////////////////////////////////////////////////
        public static AtomEntry ImportFromFeed(AtomEntry entryToImport)
        {
            Tracing.Assert(entryToImport != null, "entryToImport should not be null");
            if (entryToImport == null)
            {
                throw new ArgumentNullException("entryToImport"); 
            }
            AtomEntry entry=null; 
            entry = (AtomEntry)Activator.CreateInstance(entryToImport.GetType());
            entry.CopyEntry(entryToImport);

            entry.Id = null; 

            // if the source is empty, set the source to the old feed

            if (entry.Source == null)
            {
                entry.Source = entryToImport.Feed;
            }
            Tracing.TraceInfo("Imported entry: " + entryToImport.Title.Text + " to: " + entry.Title.Text); 
            return entry;
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method for the GData Service to use</summary> 
        //////////////////////////////////////////////////////////////////////
        public IService Service
        {
            get {return this.service;}
            set {this.Dirty = true;  this.service = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor to the batchdata for the entry</summary> 
        /// <returns> GDataBatch object </returns>
        //////////////////////////////////////////////////////////////////////
        public GDataBatchEntryData BatchData
        {
            get {return this.batchData;}
            set {this.batchData = value;}
        }
        // end of accessor public GDataBatch BatchData


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public Uri EditUri</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomUri EditUri
        {
            get 
            {
                AtomLink link = this.Links.FindService(BaseNameTable.ServiceEdit, AtomLink.ATOM_TYPE);
                // scan the link collection
                return link == null ? null : link.HRef;
            }
            set
            {
                AtomLink link = this.Links.FindService(BaseNameTable.ServiceEdit, AtomLink.ATOM_TYPE);
                if (link == null)
                {
                    link = new AtomLink(AtomLink.ATOM_TYPE, BaseNameTable.ServiceEdit);
                    this.Links.Add(link);
                }
                link.HRef = value;
            }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor for the self URI</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomUri SelfUri
        {
            get 
            {
                AtomLink link = this.Links.FindService(BaseNameTable.ServiceSelf, AtomLink.ATOM_TYPE);
                // scan the link collection
                return link == null ? null : link.HRef;
            }
            set
            {
                AtomLink link = this.Links.FindService(BaseNameTable.ServiceSelf, AtomLink.ATOM_TYPE);
                if (link == null)
                {
                    link = new AtomLink(AtomLink.ATOM_TYPE, BaseNameTable.ServiceSelf);
                    this.Links.Add(link);
                }
                link.HRef = value;
            }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor to find the edit-media link</summary> 
        /// <returns>the Uri as AtomUri to the media upload Service</returns>
        //////////////////////////////////////////////////////////////////////
        public AtomUri MediaUri
        {
            get 
            {
                // scan the link collection
                AtomLink link = this.Links.FindService(BaseNameTable.ServiceMedia, null);
                return link == null ? null : link.HRef;
            }
            set
            {
                AtomLink link = this.Links.FindService(BaseNameTable.ServiceMedia, null);
                if (link == null)
                {
                    link = new AtomLink(null, BaseNameTable.ServiceMedia);
                    this.Links.Add(link);
                }
                link.HRef = value;
            }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor to find the alternate link, in HTML only
        /// The method scans the link collection for a link that is of type rel=alternate
        /// and has a media type of HTML, otherwise it return NULL. The same is true for setting this. 
        /// If you need to use a rel/alternate with a different media type, you need
        /// to use the links collection directly</summary> 
        /// <returns>the Uri as AtomUri to HTML representation</returns>
        //////////////////////////////////////////////////////////////////////
        public AtomUri AlternateUri
        {
            get 
            {
                // scan the link collection
                AtomLink link = this.Links.FindService(BaseNameTable.ServiceAlternate, AtomLink.HTML_TYPE);
                return link == null ? null : link.HRef;
            }
            set
            {
                AtomLink link = this.Links.FindService(BaseNameTable.ServiceAlternate, AtomLink.HTML_TYPE);
                if (link == null)
                {
                    link = new AtomLink(AtomLink.HTML_TYPE, BaseNameTable.ServiceAlternate);
                    this.Links.Add(link);
                }
                link.HRef = value;
            }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Feed</summary> 
        /// <returns>returns the Uri as string for the feed service </returns>
        //////////////////////////////////////////////////////////////////////
        public string FeedUri
        {
            get 
            {
                AtomLink link = this.Links.FindService(BaseNameTable.ServiceFeed, AtomLink.ATOM_TYPE);
                // scan the link collection
                return link == null ? null : Utilities.CalculateUri(this.Base, this.ImpliedBase, link.HRef.ToString());
            }
            set
            {
                AtomLink link = this.Links.FindService(BaseNameTable.ServiceFeed, AtomLink.ATOM_TYPE);
                if (link == null)
                {
                    link = new AtomLink(AtomLink.ATOM_TYPE, BaseNameTable.ServiceFeed);
                    this.Links.Add(link);
                }
                link.HRef = new AtomUri(value);
            }
        }
        

       //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public DateTime UpdateDate</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public DateTime Updated
        {
            get {return this.lastUpdateDate;}
            set {this.Dirty = true;  this.lastUpdateDate = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public DateTime PublicationDate</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public DateTime Published
        {
            get {return this.publicationDate;}
            set {this.Dirty = true;  this.publicationDate = value;}
        }
        /////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// returns the app:control element
        /// </summary>
        /// <returns></returns>
        public AppControl AppControl
        {
            get
            {
                return FindExtension(BaseNameTable.XmlElementPubControl,
                                     BaseNameTable.AppPublishingNamespace(this)) as AppControl;
            }
            set
            {
                ReplaceExtension(BaseNameTable.XmlElementPubControl,
                                     BaseNameTable.AppPublishingNamespace(this),
                                value);
            }
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>specifies if app:control/app:draft is yes or no. 
        /// this is determined by walking the extension elements collection</summary> 
        /// <returns>true if this is a draft element</returns>
        //////////////////////////////////////////////////////////////////////
        public bool IsDraft
        {
            get
            {
                if (this.AppControl != null && this.AppControl.Draft != null)
                {
                    return this.AppControl.Draft.BooleanValue;
                }
                return false;
            }

            set
            {
                this.Dirty = true;
                if (this.AppControl == null)
                {
                    this.AppControl = new AppControl();
                }
                if (this.AppControl.Draft == null)
                {
                    this.AppControl.Draft = new AppDraft();
                }
                this.AppControl.Draft.BooleanValue = value;
            }
        }
        // end of accessor public bool IsDraft


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public Contributors AtomPersonCollection</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomPersonCollection Authors
        {
            get 
            {
                if (this.authors == null)
                {
                    this.authors = new AtomPersonCollection();
                }
                return this.authors; 
            }
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public Contributors AtomPersonCollection</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomPersonCollection Contributors
        {
            get 
            {
                if (this.contributors == null)
                {
                    this.contributors = new AtomPersonCollection();
                }
                return this.contributors; 
            }
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Content</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomContent Content
        {
            get 
            {
                if (this.content == null)
                {
                    this.content = new AtomContent();
                }
                return this.content;
            }
            set {this.Dirty = true;  this.content = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Summary</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomTextConstruct Summary
        {
            get 
            {
                if (this.summary == null)
                {
                    this.summary = new AtomTextConstruct(AtomTextConstructElementType.Summary); 
                }
                return this.summary;
            }
            set {this.Dirty = true;  this.summary = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public Links AtomLinkCollection</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomLinkCollection Links
        {
            get 
            {
                if (this.links == null)
                {
                    this.links = new AtomLinkCollection();
                }
                return this.links; 
            }
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>holds an array of AtomCategory objects</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomCategoryCollection Categories
        {
            get 
            {
                if (this.categories == null)
                {
                    this.categories = new AtomCategoryCollection();
                }
                return this.categories; 
            }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public AtomId Id</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomId Id
        {
            get 
            {
                if (this.id == null)
                {
                    this.id = new AtomId();
                }
                return this.id;
            }
            set {this.Dirty = true;  this.id = value;}
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public AtomTextConstruct Title</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomTextConstruct Title
        {
            get 
            {
                if (this.title == null)
                {
                    this.title = new AtomTextConstruct(AtomTextConstructElementType.Title); 
                }
                return this.title;
            }

            set {this.Dirty = true;  this.title = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>if the entry was copied, represents the source</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomSource Source
        {
            get {return this.source; }
            set 
            {
                this.Dirty = true;  
                AtomFeed feed = value as AtomFeed; 
                if (feed != null)
                {
                    Tracing.TraceInfo("need to copy a feed to a source"); 
                    this.source = new AtomSource(feed); 
                }
                else
                {
                    this.source = value;
                }
                
            }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string rights</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomTextConstruct Rights
        {
            get 
            {
                
                if (this.rights == null)
                {
                    this.rights = new AtomTextConstruct(AtomTextConstructElementType.Rights); 
                }
                return this.rights;
            }
            set {this.Dirty = true;  this.rights = value;}
        }
        /////////////////////////////////////////////////////////////////////////////



        #region EDITING


        //////////////////////////////////////////////////////////////////////
        /// <summary>returns whether or not the entry is read-only </summary> 
        //////////////////////////////////////////////////////////////////////
        public bool ReadOnly
        {
            get {
                return this.EditUri == null ? true : false; 
            }
        }
        /////////////////////////////////////////////////////////////////////////////
        

        //////////////////////////////////////////////////////////////////////
        /// <summary>commits the item to the server</summary> 
        /// <returns>throws an exception if an error occured updating, returns 
        /// the updated entry from the service</returns>
        //////////////////////////////////////////////////////////////////////
        public AtomEntry Update()
        {
            if (this.Service == null)
                throw new InvalidOperationException("No Service object set"); 

            AtomEntry updatedEntry = Service.Update(this);
            if (updatedEntry != null)
            {
                this.CopyEntry(updatedEntry);
                this.MarkElementDirty(false);
                return updatedEntry;
            }
            return null;
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>deletes the item from the server</summary> 
        /// <returns>throws an exception if an error occured updating</returns>
        /////////////////////////////////////////////////////////////////////
        public void Delete()
        {
            if (this.Service == null)
                throw new InvalidOperationException("No Service object set"); 
            Service.Delete(this);
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>takes the updated entry returned and sets the properties to this object</summary> 
        /// <param name="updatedEntry"> </param>
        //////////////////////////////////////////////////////////////////////
        protected void CopyEntry(AtomEntry updatedEntry)
        {
            Tracing.Assert(updatedEntry != null, "updatedEntry should not be null");
            if (updatedEntry == null)
            {
                throw new ArgumentNullException("updatedEntry"); 
            }
            
            this.title = updatedEntry.Title;
            this.authors = updatedEntry.Authors;
            this.id = updatedEntry.Id;
            this.links = updatedEntry.Links;
            this.lastUpdateDate = updatedEntry.Updated;
            this.publicationDate = updatedEntry.Published;
            this.authors = updatedEntry.Authors;
            this.rights = updatedEntry.Rights;
            this.categories = updatedEntry.Categories;
            this.summary = updatedEntry.Summary;
            this.content = updatedEntry.Content;
            this.source = updatedEntry.Source;

            this.ExtensionElements.Clear();

            foreach (IExtensionElementFactory extension in updatedEntry.ExtensionElements)
            {
                this.ExtensionElements.Add(extension);
            }
        }
        /////////////////////////////////////////////////////////////////////////////


        #endregion


        /// <summary>
        /// this is the subclassing method for AtomBase derived 
        /// classes to overload what childelements should be created
        /// needed to create CustomLink type objects, like WebContentLink etc
        /// </summary>
        /// <param name="reader">The XmlReader that tells us what we are working with</param>
        /// <param name="parser">the parser is primarily used for nametable comparisons</param>
        /// <returns>AtomBase</returns>
        public override AtomBase CreateAtomSubElement(XmlReader reader, AtomFeedParser parser)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }
            if (parser == null)
            {
                throw new ArgumentNullException("parser");
            }

            Object localname = reader.LocalName;

            if (localname.Equals(parser.Nametable.Source))
            {
                return new AtomSource();
            } else if (localname.Equals(parser.Nametable.Content))
            {
                return new AtomContent();
            }
            return base.CreateAtomSubElement(reader, parser);
            
        }


        #region overloaded for property changes, xml:base
        //////////////////////////////////////////////////////////////////////
        /// <summary>just go down the child collections</summary> 
        /// <param name="uriBase"> as currently calculated</param>
        //////////////////////////////////////////////////////////////////////
        internal override void BaseUriChanged(AtomUri uriBase)
        {
            base.BaseUriChanged(uriBase);
            // now pass it to the properties.
            uriBase = new AtomUri(Utilities.CalculateUri(this.Base, uriBase, null));
            
            if (this.Title != null)
            {
                this.Title.BaseUriChanged(uriBase);
            }
            if (this.Id != null)
            {
                this.Id.BaseUriChanged(uriBase);
            }
            foreach (AtomLink link in this.Links )
            {
                link.BaseUriChanged(uriBase);
            }
            foreach (AtomPerson person in this.Authors )
            {
                person.BaseUriChanged(uriBase);
            }
            foreach (AtomPerson person in this.Contributors )
            {
                person.BaseUriChanged(uriBase);
            }
            foreach (AtomCategory category in this.Categories )
            {
                category.BaseUriChanged(uriBase);
            }
            if (this.Rights != null)
            {
                this.Rights.BaseUriChanged(uriBase);
            }
            if (this.Summary != null)
            {
                this.Summary.BaseUriChanged(uriBase);
            }
            if (this.Content != null)
            {
                this.Content.BaseUriChanged(uriBase);
            }
            if (this.Source != null)
            {
                this.Source.BaseUriChanged(uriBase);
            }
        }
        /////////////////////////////////////////////////////////////////////////////

 
        //////////////////////////////////////////////////////////////////////
        /// <summary>calls the action on this object and all children</summary> 
        /// <param name="action">an IAtomBaseAction interface to call </param>
        /// <returns>true or false, pending outcome</returns>
        //////////////////////////////////////////////////////////////////////
        public override bool WalkTree(IBaseWalkerAction action)
        {
            if (base.WalkTree(action))
            {
                return true;
            }
            foreach (AtomPerson person in this.Authors)
            {
                if (person.WalkTree(action))
                    return true;
            }
            // saving Contributors
            foreach (AtomPerson person in this.Contributors)
            {
                if (person.WalkTree(action))
                    return true;
            }
            // saving Categories
            foreach (AtomCategory category in this.Categories )
            {
                if (category.WalkTree(action))
                    return true;
            }
            if (this.id != null)
            {
                if (this.id.WalkTree(action))
                    return true;
            }
            // save the Links
            foreach (AtomLink link in this.Links)
            {
                if (link.WalkTree(action))
                    return true;
            }
            if (this.rights != null)
            {
                if (this.rights.WalkTree(action))
                    return true;
            }
            if (this.title != null)
            {
                if (this.title.WalkTree(action))
                    return true;
            }
            if (this.summary != null)
            {
                if (this.summary.WalkTree(action))
                    return true;
            }
            if (this.content != null)
            {
                if (this.content.WalkTree(action))
                    return true;
            }
            if (this.source != null)
            {
                if (this.source.WalkTree(action))
                    return true;
            }
            // nothing dirty at all
            return false; 
        }
        /////////////////////////////////////////////////////////////////////////////
        #endregion

         
        /// <summary>
        /// Parses the inner state of the element
        /// </summary>
        /// <param name="e">The extension element that should be added to this entry</param>
        /// <param name="parser">The AtomFeedParser that called this</param>
        public virtual void Parse(ExtensionElementEventArgs e, AtomFeedParser parser)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }
        

            Tracing.TraceMsg("Entering Parse on AbstractEntry");
            XmlNode node = e.ExtensionElement;
            if (this.ExtensionFactories != null && this.ExtensionFactories.Count > 0)
            {
                Tracing.TraceMsg("Entring default Parsing for AbstractEntry");

                IExtensionElementFactory f = FindExtensionFactory(node.LocalName, 
                                                                  node.NamespaceURI);
                if (f != null)
                {
                    this.ExtensionElements.Add(f.CreateInstance(node, parser));
                    e.DiscardEntry = true;
                }
            }
            return;
        }


    }
    /////////////////////////////////////////////////////////////////////////////

}
/////////////////////////////////////////////////////////////////////////////
 

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
using System;
using System.Collections;
using System.Text;
using System.Xml;
using Google.GData.Client;
using System.Globalization;
using System.Collections.Generic;

namespace Google.GData.Extensions {

    /// <summary>
    /// Extensible type used in many places.
    /// </summary>
    public abstract class ExtensionBase : IExtensionElementFactory, IVersionAware
    {

        private string xmlName;
        private string xmlPrefix;
        private string xmlNamespace;

        private List<XmlNode> unknownChildren;
        /// <summary>
        /// this holds the attribute list for an extension element
        /// </summary>
        private SortedList attributes;
        private SortedList attributeNamespaces;



        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="name">the xml name</param>
        /// <param name="prefix">the xml prefix</param>
        /// <param name="ns">the xml namespace</param>
        protected ExtensionBase(string name, string prefix, string ns)
        {
            this.xmlName = name;
            this.xmlPrefix = prefix;
            this.xmlNamespace = ns;
        }




        private VersionInformation versionInfo = new VersionInformation();
        internal VersionInformation VersionInfo
        {
            get
            {
                return this.versionInfo;
            }
        }

        /// <summary>
        /// returns the major protocol version number this element 
        /// is working against. 
        /// </summary>
        /// <returns></returns>
        public int ProtocolMajor
        {
            get
            {
                return this.versionInfo.ProtocolMajor;
            }
            set
            {
                this.versionInfo.ProtocolMajor  = value;
                this.VersionInfoChanged();
            }
        }

        /// <summary>
        /// returns the minor protocol version number this element 
        /// is working against. 
        /// </summary>
        /// <returns></returns>
        public int ProtocolMinor
        {
            get
            {
                return this.versionInfo.ProtocolMinor;
            }
            set
            {
                this.versionInfo.ProtocolMinor  = value;
                this.VersionInfoChanged();
            }
        }


        /// <summary>
        /// virtual to be overloaded by subclasses which are interested in reacting on versioninformation
        /// changes
        /// </summary>
        protected virtual void VersionInfoChanged()
        {
        }

        /// <summary>
        /// method for subclasses who need to change a namespace for parsing/persistence during runtime
        /// </summary>
        /// <param name="ns"></param>
        protected void SetXmlNamespace(string ns)
        {
            this.xmlNamespace = ns; 
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>accesses the Attribute list. The keys are the attribute names
        /// the values the attribute values</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public SortedList Attributes
        {
            get 
            {
                return getAttributes();
            }
        }
        // end of accessor public SortedList Attributes
 
        //////////////////////////////////////////////////////////////////////
        /// <summary>accesses the Attribute list. The keys are the attribute names
        /// the values the attribute values</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public SortedList AttributeNamespaces
        {
            get 
            {
                return getAttributeNamespaces();
            }
        }
        // end of accessor public SortedList Attributes


        /// <summary>
        /// returns the attributes list
        /// </summary>
        /// <returns>SortedList</returns>
        internal SortedList getAttributes()
        {
            if (this.attributes == null)
            {
                this.attributes = new SortedList();
            }
            return this.attributes;
        }

        /// <summary>
        /// returns the attribute namespace list
        /// </summary>
        /// <returns>SortedList</returns>
        internal SortedList getAttributeNamespaces()
        {
            if (this.attributeNamespaces == null)
            {
                this.attributeNamespaces = new SortedList();
            }
            return this.attributeNamespaces;
        }

        
        #region overloaded for persistence

        //////////////////////////////////////////////////////////////////////
        /// <summary>Returns the constant representing this XML element.</summary> 
        //////////////////////////////////////////////////////////////////////
        public string XmlName
        {
            get { return this.xmlName; }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>Returns the constant representing this XML element.</summary> 
        //////////////////////////////////////////////////////////////////////
        public string XmlNameSpace
        {
            get { return this.xmlNamespace; }
        }
        //////////////////////////////////////////////////////////////////////
        /// <summary>Returns the constant representing this XML element.</summary> 
        //////////////////////////////////////////////////////////////////////
        public string XmlPrefix
        {
            get { return this.xmlPrefix; }
        }

        /// <summary>
        /// debugging helper
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return base.ToString() + " for: " + XmlNameSpace + "- " + XmlName;
        }


        /// <summary>
        ///  returns the list of childnodes that are unknown to the extension
        ///  used for example for the GD:ExtendedProperty
        /// </summary>
        /// <returns></returns>
        public List<XmlNode> ChildNodes
        {
            get 
            {
                if (this.unknownChildren == null)
                {
                    this.unknownChildren = new List<XmlNode>();
                }
                return this.unknownChildren;
            }
        }


         //////////////////////////////////////////////////////////////////////
        /// <summary>Parses an xml node to create an instance of this  object.</summary> 
        /// <param name="node">the xml parses node, can be NULL</param>
        /// <param name="parser">the xml parser to use if we need to dive deeper</param>
        /// <returns>the created IExtensionElement object</returns>
        //////////////////////////////////////////////////////////////////////
        public virtual IExtensionElementFactory CreateInstance(XmlNode node, AtomFeedParser parser) 
        {
            Tracing.TraceCall();

            ExtensionBase e = null;

            if (node != null)
            {
                object localname = node.LocalName;
                if (!localname.Equals(this.XmlName) ||
                    !node.NamespaceURI.Equals(this.XmlNameSpace))
                {
                    return null;
                }
            } 
            // memberwise close is fine here, as everything is identical beside the value
            e = this.MemberwiseClone() as ExtensionBase;
            e.InitInstance(this);

            e.ProcessAttributes(node);
            e.ProcessChildNodes(node, parser);

            return e;
        }

        /// <summary>
        /// used to copy the unknown childnodes for later saving
        /// </summary>
        public virtual void ProcessChildNodes(XmlNode node, AtomFeedParser parser) 
        {
            if (node != null && node.HasChildNodes)
            {
                XmlNode childNode = node.FirstChild;
                while (childNode != null)
                {
                    if (childNode.NodeType == XmlNodeType.Element)
                    {
                        this.ChildNodes.Add(childNode);
                    }
                    childNode = childNode.NextSibling;
                }
            }
        }
  
        /// <summary>
        /// used to copy the attribute lists over
        /// </summary>
        /// <param name="factory"></param>
        protected void InitInstance(ExtensionBase factory)
        {
            this.attributes = null;
            this.attributeNamespaces = null;
            this.unknownChildren = null;
            for (int i=0; i < factory.getAttributes().Count; i++)
            {
                string name = factory.getAttributes().GetKey(i) as string;
                string value = factory.getAttributes().GetByIndex(i) as string;
                this.getAttributes().Add(name, value);
            }
        }

        /// <summary>
        /// default method override to handle attribute processing
        /// the base implementation does process the attributes list
        /// and reads all that are in there.
        /// </summary>
        /// <param name="node">XmlNode with attributes</param>
        public virtual void ProcessAttributes(XmlNode node)
        {
            if (node != null && node.Attributes != null)
            {
                for (int i = 0; i < node.Attributes.Count; i++)
                {
                    this.getAttributes()[node.Attributes[i].LocalName] = node.Attributes[i].Value; 
                }
            }
            return;
        }

        /// <summary>
        /// Persistence method for the EnumConstruct object
        /// </summary>
        /// <param name="writer">the xmlwriter to write into</param>
        public virtual void Save(XmlWriter writer)
        {
            writer.WriteStartElement(XmlPrefix, XmlName, XmlNameSpace);
            if (this.attributes != null)
            {
                for (int i=0; i < this.getAttributes().Count; i++)
                {
                    if (this.getAttributes().GetByIndex(i) != null)
                    {
                        string name = this.getAttributes().GetKey(i) as string;
                        string value = Utilities.ConvertToXSDString(this.getAttributes().GetByIndex(i));
                        string ns = this.getAttributeNamespaces()[name] as string; 
                        if (Utilities.IsPersistable(name) && Utilities.IsPersistable(value))
                        {
                            if (ns == null)
                            {
                                writer.WriteAttributeString(name, value);
                            } 
                            else
                            {
                                writer.WriteAttributeString(name, ns, value);
                            }
                        }
                    }
                }
            }
            SaveInnerXml(writer);

            foreach (XmlNode node in this.ChildNodes)
            {
                if (node != null)
                {
                    node.WriteTo(writer);
                }
            }
            writer.WriteEndElement();
        }

        /// <summary>
        /// a subclass that want's to save addtional XML would need to overload this
        /// the default implementation does nothing
        /// </summary>
        /// <param name="writer"></param>
        public virtual void SaveInnerXml(XmlWriter writer)
        {
        }
        #endregion
    }
}  

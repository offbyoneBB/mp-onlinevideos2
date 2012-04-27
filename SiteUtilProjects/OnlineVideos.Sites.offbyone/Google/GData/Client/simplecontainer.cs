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
    /// base class to implement extensions holding extensions
    /// TODO: at one point think about using this as the base for atom:base
    /// as there is some utility overlap between the 2 of them
    /// </summary>
    public class SimpleContainer : ExtensionBase, IExtensionContainer {
        private ExtensionList extensions;
        private ExtensionList extensionFactories;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="name">the xml name</param>
        /// <param name="prefix">the xml prefix</param>
        /// <param name="ns">the xml namespace</param>
        protected SimpleContainer(string name, string prefix, string ns)
            : base(name, prefix, ns) {
        }

        /// <summary>the list of extensions for this container
        /// the elements in that list MUST implement IExtensionElementFactory 
        /// and IExtensionElement</summary> 
        /// <returns> </returns>
        public ExtensionList ExtensionElements {
            get {
                if (this.extensions == null) {
                    this.extensions = new ExtensionList(this);
                }
                return this.extensions;
            }
        }

        /// <summary>
        /// Finds a specific ExtensionElement based on its local name
        /// and its namespace. If namespace is NULL, the first one where
        /// the localname matches is found. If there are extensionelements that do 
        /// not implrment ExtensionElementFactory, they will not be taken into account
        /// </summary>
        /// <param name="localName">the xml local name of the element to find</param>
        /// <param name="ns">the namespace of the elementToPersist</param>
        /// <returns>Object</returns>
        public IExtensionElementFactory FindExtension(string localName, string ns) {
            return Utilities.FindExtension(this.ExtensionElements, localName, ns);
        }

        /// <summary>
        /// all extension elements that match a namespace/localname
        /// given will be removed and the new one will be inserted
        /// </summary> 
        /// <param name="localName">the local name to find</param>
        /// <param name="ns">the namespace to match, if null, ns is ignored</param>
        /// <param name="obj">the new element to put in</param>
        public void ReplaceExtension(string localName, string ns, IExtensionElementFactory obj) {
            DeleteExtensions(localName, ns);
            this.ExtensionElements.Add(obj);
        }

        /// <summary>
        /// all extension element factories that match a namespace/localname
        /// given will be removed and the new one will be inserted
        /// </summary> 
        /// <param name="localName">the local name to find</param>
        /// <param name="ns">the namespace to match, if null, ns is ignored</param>
        /// <param name="obj">the new element to put in</param>
        public void ReplaceFactory(string localName, string ns, IExtensionElementFactory obj) {
            ExtensionList arr = Utilities.FindExtensions(this.ExtensionFactories, localName, ns, new ExtensionList(this));
            foreach (IExtensionElementFactory ob in arr) {
                this.ExtensionFactories.Remove(ob);
            }
            this.ExtensionFactories.Add(obj);
        }

        /// <summary>
        /// Finds all ExtensionElement based on its local name
        /// and its namespace. If namespace is NULL, all where
        /// the localname matches is found. If there are extensionelements that do 
        /// not implrment ExtensionElementFactory, they will not be taken into account
        /// Primary use of this is to find XML nodes
        /// </summary>
        /// <param name="localName">the xml local name of the element to find</param>
        /// <param name="ns">the namespace of the element to find</param>
        /// <returns>none</returns>
        public ExtensionList FindExtensions(string localName, string ns) {
            return Utilities.FindExtensions(this.ExtensionElements,
                localName, ns, new ExtensionList(this));
        }

        /// <summary>
        /// Finds all ExtensionElement based on its local name
        /// and its namespace. If namespace is NULL, all where
        /// the localname matches is found. If there are extensionelements that do 
        /// not implement ExtensionElementFactory, they will not be taken into account
        /// Primary use of this is to find XML nodes
        /// </summary>
        /// <param name="localName">the xml local name of the element to find</param>
        /// <param name="ns">the namespace of the element to find</param>
        /// <returns>none</returns>
        public List<T> FindExtensions<T>(string localName, string ns) where T : IExtensionElementFactory {
            return Utilities.FindExtensions<T>(this.ExtensionElements, localName, ns);
        }

        /// <summary>
        /// Deletes all Extensions from the Extension list that match
        /// a localName and a Namespace. 
        /// </summary>
        /// <param name="localName">the local name to find</param>
        /// <param name="ns">the namespace to match, if null, ns is ignored</param>
        /// <returns>int - the number of deleted extensions</returns>
        public int DeleteExtensions(string localName, string ns) {
            // Find them first
            ExtensionList arr = FindExtensions(localName, ns);
            foreach (IExtensionElementFactory ob in arr) {
                this.extensions.Remove(ob);
            }
            return arr.Count;
        }

        /// <summary>the list of extensions for this container
        /// the elements in that list MUST implement IExtensionElementFactory 
        /// and IExtensionElement</summary> 
        /// <returns> </returns>
        public ExtensionList ExtensionFactories {
            get {
                if (this.extensionFactories == null) {
                    this.extensionFactories = new ExtensionList(this);
                }
                return this.extensionFactories;
            }
        }

        /// <summary>Parses an xml node to create a Who object.</summary> 
        /// <param name="node">the node to work on, can be NULL</param>
        /// <param name="parser">the xml parser to use if we need to dive deeper</param>
        /// <returns>the created SimpleElement object</returns>
        public override IExtensionElementFactory CreateInstance(XmlNode node, AtomFeedParser parser) {
            Tracing.TraceCall("for: " + XmlName);

            if (node != null) {
                object localname = node.LocalName;
                if (!localname.Equals(this.XmlName) ||
                    !node.NamespaceURI.Equals(this.XmlNameSpace)) {
                    return null;
                }
            }

            SimpleContainer sc = null;
            // create a new container
            sc = this.MemberwiseClone() as SimpleContainer;
            sc.InitInstance(this);

            sc.ProcessAttributes(node);
            sc.ProcessChildNodes(node, parser);
            return sc;
        }

        /// <summary>
        /// need so setup the namespace based on the version information     
        /// </summary>
        protected override void VersionInfoChanged() {
            base.VersionInfoChanged();

            this.VersionInfo.ImprintVersion(this.extensions);
            this.VersionInfo.ImprintVersion(this.extensionFactories);
        }

        /// <summary>
        /// used to copy the unknown childnodes for later saving
        /// </summary>
        /// <param name="node">the node to process</param>
        /// <param name="parser">the feed parser to pass down if need be</param>
        public override void ProcessChildNodes(XmlNode node, AtomFeedParser parser) {
            if (node != null && node.HasChildNodes) {
                XmlNode childNode = node.FirstChild;
                while (childNode != null) {
                    bool fProcessed = false;
                    if (childNode is XmlElement) {
                        foreach (IExtensionElementFactory f in this.ExtensionFactories) {
                            if (String.Compare(childNode.NamespaceURI, f.XmlNameSpace) == 0) {
                                if (String.Compare(childNode.LocalName, f.XmlName) == 0) {
                                    Tracing.TraceMsg("Added extension to SimpleContainer for: " + f.XmlName);
                                    ExtensionElements.Add(f.CreateInstance(childNode, parser));
                                    fProcessed = true;
                                    break;
                                }
                            }
                        }
                    }
                    if (!fProcessed) {
                        this.ChildNodes.Add(childNode);
                    }
                    childNode = childNode.NextSibling;
                }
            }
        }

        /// <summary>
        /// saves out the inner xml, so all of our subelements
        /// gets called from Save, whcih takes care of saving attributes
        /// </summary>
        /// <param name="writer"></param>
        public override void SaveInnerXml(XmlWriter writer) {
            if (this.extensions != null) {
                foreach (IExtensionElementFactory e in this.ExtensionElements) {
                    e.Save(writer);
                }
            }
        }

        protected void SetStringValue<T>(string value, string elementName, string ns) where T : SimpleElement, new() {
            T v = null;
            if (!String.IsNullOrEmpty(value)) {
                v = new T();
                v.Value = value;
            }

            ReplaceExtension(elementName, ns, v);
        }

        protected string GetStringValue<T>(string elementName, string ns) where T : SimpleElement {
            T e = FindExtension(elementName, ns) as T;
            if (e != null) {
                return e.Value;
            }
            return null;
        }
    }
}  

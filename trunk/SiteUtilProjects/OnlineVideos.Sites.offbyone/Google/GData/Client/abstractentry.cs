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
using System.Net;
using System.Collections;
using Google.GData.Extensions.AppControl;
using Google.GData.Extensions;



#endregion

//////////////////////////////////////////////////////////////////////
// <summary>Contains AtomEntry, an object to represent the atom:entry
// element.</summary>
//////////////////////////////////////////////////////////////////////
namespace Google.GData.Client
{
   /// <summary>
    /// Entry API customization class for defining entries in a custom feed
    /// </summary>
    public abstract class AbstractEntry : AtomEntry, ISupportsEtag
    {
        private string eTag;
        /// <summary>
        /// default constructor, adding app:edited and etag extensions
        /// </summary>
        public AbstractEntry()
        {
            this.AddExtension(new AppEdited());
        }

        private MediaSource mediaSource;

        /// <summary>
        /// base implementation, as with the abstract feed, we are adding
        /// the gnamespace
        /// </summary>
        /// <param name="writer">The XmlWrite, where we want to add default namespaces to</param>
        protected override void AddOtherNamespaces(XmlWriter writer)
        {
            base.AddOtherNamespaces(writer);
            Utilities.EnsureGDataNamespace(writer);
        }

        /// <summary>
        /// Checks if this is a namespace declaration that we already added
        /// </summary>
        /// <param name="node">XmlNode to check</param>
        /// <returns>True if this node should be skipped</returns>
        protected override bool SkipNode(XmlNode node)
        {
            if (base.SkipNode(node))
            {
                return true;
            }

            return (node.NodeType == XmlNodeType.Attribute && 
                    node.Name.StartsWith("xmlns") && 
                    (String.Compare(node.Value,BaseNameTable.gNamespace)==0));
        }


        /// <summary>
        /// helper to toggle categories
        /// </summary>
        /// <param name="cat"></param>
        /// <param name="value"></param>
        public void ToggleCategory(AtomCategory cat, bool value)
        {
            if (value)
            {
                if (!this.Categories.Contains(cat))
                {
                    this.Categories.Add(cat);
                }
            } 
            else 
            { 
                this.Categories.Remove(cat);
            }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>access the associated media element. Note, that setting this
        /// WILL cause subsequent updates to be done using MIME multipart posts
        /// </summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public MediaSource MediaSource
        {
            get {return this.mediaSource;}
            set {this.mediaSource = value;}
        }
        // end of accessor public MediaSource Media

        //////////////////////////////////////////////////////////////////////
        /// <summary>returns this entries etag, if any
        /// This is a protocol version 2 feature
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        public string Etag
        {
            get
            {
                return eTag;
            }
            set
            {
                eTag = value;
            }
        }
        



        /// <summary>
        /// returns the app:edited element of the entry, if any. 
        /// This is a protocol version 2 feature
        /// </summary>
        public AppEdited Edited
        {
            get
            {

                return FindExtension(BaseNameTable.XmlElementPubEdited,
                                     BaseNameTable.NSAppPublishingFinal) as AppEdited;
            }
            set
            {
                ReplaceExtension(BaseNameTable.XmlElementPubEdited,
                                 BaseNameTable.NSAppPublishingFinal,
                                 value);
            }
        }


        /// <summary>
        /// we have one string based getter
        /// usage is: entry.getExtensionValue("namespace", "tagname") to get the elements value
        /// </summary>
        /// <param name="extension">the name of the extension to look for</param>
        /// <param name="ns">the namespace of the extension to look for</param>
        /// <returns>value as string, or NULL if the extension was not found</returns>
        public string GetExtensionValue(string extension, string ns) 
        {
            SimpleElement e = FindExtension(extension, ns) as SimpleElement;
            if (e != null)
            {
                return e.Value;
            }
            return null;
        }




        /// <summary>
        /// we have one string based setter
        /// usage is: entry.setExtensionValue("tagname", "ns", "value") to set the element
        /// this will create the extension if it's not there
        /// note, you can ofcourse, just get an existing one and work with that 
        /// object: 
        /// </summary>
        /// <param name="extension">the name of the extension to look for</param>
        /// <param name="ns">the namespace of the extension to look for</param>
        /// <param name="newValue">the new value for this extension element</param>
        /// <returns>SimpleElement, either a brand new one, or the one
        /// returned by the service</returns>
        public SimpleElement SetExtensionValue(string extension, string ns, string newValue) 
        {
            if (extension == null)
            {
                throw new System.ArgumentNullException("extension");
            }
            
            SimpleElement ele = FindExtension(extension, ns) as SimpleElement;
            if (ele == null)
            {
                ele = CreateExtension(extension, ns) as SimpleElement;
                if (ele == null)
                {
                    throw new System.ArgumentException("The namespace or tagname was invalid");
                }
                this.ExtensionElements.Add(ele);
            }
            ele.Value = newValue;
            return ele;
        }


        protected void SetStringValue<T>(string value, string elementName, string ns) where T : SimpleElement, new()
        {
            T v = null;
            if (!String.IsNullOrEmpty(value))
            {
                v = new T();
                v.Value = value;
            }
           
            ReplaceExtension(elementName, ns, v);
        }


        protected string GetStringValue<T>(string elementName, string ns) where T : SimpleElement
        {
            T e =  FindExtension(elementName, ns) as T;
            if (e!= null)
            {
                return e.Value;
            }
            return null;
        }


    }
}
/////////////////////////////////////////////////////////////////////////////
 

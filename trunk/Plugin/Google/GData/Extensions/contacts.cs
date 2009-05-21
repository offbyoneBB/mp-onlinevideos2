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
using System.Collections;
using System.Text;
using Google.GData.Client;

namespace Google.GData.Extensions 
{

    /// <summary>
    /// helper to instantiate all factories defined in here and attach 
    /// them to a base object
    /// </summary>
    public class ContactsExtensions
    {
        /// <summary>
        /// helper to add all MediaRss extensions to a base object
        /// </summary>
        /// <param name="baseObject"></param>
        public static void AddExtension(AtomBase baseObject) 
        {
            baseObject.AddExtension(new EMail());
            baseObject.AddExtension(new Deleted());
            baseObject.AddExtension(new IMAddress());
            baseObject.AddExtension(new Organization());
            baseObject.AddExtension(new PhoneNumber());
            baseObject.AddExtension(new PostalAddress());
        }
    }

    //////////////////////////////////////////////////////////////////////
    /// <summary>if an extension element has a deleted member, it should implement
    /// this interface. 
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public interface IContainsDeleted
    {
        /// <summary>
        /// returns if an entry contains a Deleted extension. ReadOnly
        /// </summary>
        bool Deleted { get;}
    }

    /// <summary>
    /// holds static strings indicating several often used relationship 
    /// values for the contacts API
    /// </summary>
    public static class ContactsRelationships
    {
        /// <summary>
        /// indicates a home email in the rel field
        /// </summary>
        public const string IsHome = "http://schemas.google.com/g/2005#home";
        /// <summary>
        /// indicates an undefined email in the rel field, label might be used to be
        /// more precise
        /// </summary>
        public const string IsOther = "http://schemas.google.com/g/2005#other";
        /// <summary>
        /// indicates a work email in the rel field
        /// </summary>
        public const string IsWork = "http://schemas.google.com/g/2005#work";

        /// <summary>
        /// indicates a general value in the rel field
        /// </summary>
        public const string IsGeneral = "http://schemas.google.com/g/2005#general";

        /// <summary>
        /// indicates a car related value in the rel field
        /// </summary>
        public const string IsCar = "http://schemas.google.com/g/2005#car";

        /// <summary>
        /// indicates a fax value in the rel field
        /// </summary>
        public const string IsFax = "http://schemas.google.com/g/2005#fax";

        /// <summary>
        /// indicates a home fax value in the rel field
        /// </summary>
        public const string IsHomeFax = "http://schemas.google.com/g/2005#home_fax";

        /// <summary>
        /// indicates a work fax value in the rel field
        /// </summary>
        public const string IsWorkFax = "http://schemas.google.com/g/2005#work_fax";

        /// <summary>
        /// indicates an internal extension value in the rel field
        /// </summary>
        public const string IsInternalExtension = "http://schemas.google.com/g/2005#internal-extension";

        /// <summary>
        /// indicates a mobile number value in the rel field
        /// </summary>
        public const string IsMobile = "http://schemas.google.com/g/2005#mobile";

        /// <summary>
        /// indicates a pager value in the rel field
        /// </summary>
        public const string IsPager = "http://schemas.google.com/g/2005#pager";

        /// <summary>
        /// indicates a satellite value in the rel field
        /// </summary>
        public const string IsSatellite = "http://schemas.google.com/g/2005#satellite";

        /// <summary>
        /// indicates a voip value in the rel field
        /// </summary>
        public const string IsVoip = "http://schemas.google.com/g/2005#voip";
    }


    /// <summary>
    /// gd:email schema extension describing an email address in contacts
    /// </summary>
    public class EMail : CommonAttributesElement
    {

        /// <summary>
        /// default constructor for gd:email
        /// </summary>
        public EMail()
        : base(GDataParserNameTable.XmlEmailElement, 
               GDataParserNameTable.gDataPrefix,
               GDataParserNameTable.gNamespace)
        {
            addAttributes();
        }

        /// <summary>
        /// default constructor for gd:email with an initial value for the email
        /// </summary>
        /// <param name="emailAddress">the initial email address</param>
        public EMail(string emailAddress)
        : base(GDataParserNameTable.XmlEmailElement, 
               GDataParserNameTable.gDataPrefix,
               GDataParserNameTable.gNamespace)
        {
            addAttributes();
            this.Address = emailAddress;
        }

        /// <summary>
        /// default constructor for gd:email with an initial value for the email
        /// and the relationship
        /// </summary>
        /// <param name="emailAddress">the initial email address</param>
        /// <param name="relationship">the value for the email relationship</param> 
        public EMail(string emailAddress, string relationship)
        : base(GDataParserNameTable.XmlEmailElement, 
               GDataParserNameTable.gDataPrefix,
               GDataParserNameTable.gNamespace)
        {
            addAttributes();
            this.Address = emailAddress;
            this.Rel = relationship;    
        }

        /// <summary>
        /// default constructor for gd:mail with initialization of namespaces
        /// </summary>
        /// <param name="element"></param>
        /// <param name="prefix"></param>
        /// <param name="ns"></param>
        public EMail(string element, string prefix, string ns) : base(element, prefix, ns)
        {
            addAttributes();
        }



        private void addAttributes()
        {
            this.Attributes.Add(GDataParserNameTable.XmlAttributeAddress, null);
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Address</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Address
        {
            get {return this.Attributes[GDataParserNameTable.XmlAttributeAddress] as string;}
            set {this.Attributes[GDataParserNameTable.XmlAttributeAddress] = value;}
        }
        // end of accessor public string Address


    }

    /// <summary>
    /// gd:deleted schema extension describing an deleted address in contacts
    /// </summary>
    public class Deleted : ExtensionBase
    {
        /// <summary>
        /// default constructor for gd:deleted 
        /// </summary>
        public Deleted()
        : base(GDataParserNameTable.XmlDeletedElement, 
               GDataParserNameTable.gDataPrefix,
               GDataParserNameTable.gNamespace)
        {
        }
    }

    /// <summary>
    /// gd:deleted schema extension describing an deleted address in contacts
    /// </summary>
    public class IMAddress : EMail
    {
        /// <summary>
        /// default constructor for gd:deleted 
        /// </summary>
        public IMAddress()
        : base(GDataParserNameTable.XmlIMElement, 
               GDataParserNameTable.gDataPrefix,
               GDataParserNameTable.gNamespace)
        {
            this.Attributes.Add(GDataParserNameTable.XmlAttributeProtocol, null);
        }

        /// <summary>
        /// default constructor with an initial value
        /// </summary>
        /// <param name="initValue"></param>
        public IMAddress(string initValue) 
            : base(GDataParserNameTable.XmlIMElement, 
                   GDataParserNameTable.gDataPrefix,
                   GDataParserNameTable.gNamespace)
            {
                this.Attributes.Add(GDataParserNameTable.XmlAttributeProtocol, null);
                this.Address = initValue;
            }

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method for the protocol</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Protocol
        {
            get {return this.Attributes[GDataParserNameTable.XmlAttributeProtocol] as string;}
            set {this.Attributes[GDataParserNameTable.XmlAttributeProtocol] = value;}
        }
        // end of accessor public string Address

    }


    /// <summary>
    ///  this interface defines a common set of properties found on classes derived
    /// from CommonAttributesElement (like EMail) and Organization.
    /// </summary>
    public interface ICommonAttributes
    {
        /// <summary>
        /// presents the Rel attribute in the xml element
        /// </summary>
        string Rel
        {
            get;
            set;
        }
        /// <summary>
        /// presents the Label attribute in the xml element
        /// </summary>
        string Label
        {
            get;
            set;
        }
        /// <summary>
        /// presents the Primary attribute in the xml element
        /// </summary>
        bool Primary
        {
            get;
            set;
        }
    }




    /// <summary>
    /// a base class used for PostalAddress and others. 
    /// </summary>
    public class CommonAttributesElement : SimpleElement, ICommonAttributes
    {

        /// <summary>
        /// default constructore with namesapce init
        /// </summary>
        /// <param name="element"></param>
        /// <param name="prefix"></param>
        /// <param name="ns"></param>
        public CommonAttributesElement(string element, string prefix, string ns) : base(element, prefix, ns)
        {
            addAttributes();
        }

        /// <summary>
        /// default constructor with namespaces and an init value
        /// </summary>
        /// <param name="element"></param>
        /// <param name="prefix"></param>
        /// <param name="ns"></param>
        /// <param name="init"></param>
        public CommonAttributesElement(string element, string prefix, string ns, string init) : base(element, prefix, ns, init)
        {
            addAttributes();
        }

        private void addAttributes()
        {
            this.Attributes.Add(GDataParserNameTable.XmlAttributeLabel, null);
            this.Attributes.Add(GDataParserNameTable.XmlAttributePrimary, null);
            this.Attributes.Add(GDataParserNameTable.XmlAttributeRel, null);
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Primary</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public bool Primary
        {
            get {return ("true" == (this.Attributes[GDataParserNameTable.XmlAttributePrimary] as string));}
            set {this.Attributes[GDataParserNameTable.XmlAttributePrimary] = value == true? Utilities.XSDTrue : Utilities.XSDFalse;}
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method for the Rel Value. Note you can only set this
        /// or Label, not both</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Rel
        {
            get {return this.Attributes[GDataParserNameTable.XmlAttributeRel] as string;}
            set 
            {
                if (this.Label != null)
                {
                    throw new System.ArgumentException("Label already has a value. You can only set Label or Rel");
                } 
                this.Attributes[GDataParserNameTable.XmlAttributeRel] = value;
            }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method for the Label value. Note you can only set this or
        /// Rel, not both</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Label
        {
            get {return this.Attributes[GDataParserNameTable.XmlAttributeLabel] as string;}
            set 
            {
                if (this.Rel != null)
                {
                    throw new System.ArgumentException("Rel already has a value. You can only set Label or Rel");
                } 
                this.Attributes[GDataParserNameTable.XmlAttributeLabel] = value;
            }
        }


        /// <summary>
        /// returns if the email is the home email address
        /// </summary>
        public bool Home
        {
            get 
            {
                if (this.Rel == ContactsRelationships.IsHome)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// returns if the email is the home email address
        /// </summary>
        public bool Work       {
            get 
            {
                if (this.Rel == ContactsRelationships.IsWork)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// returns if the email is the home email address
        /// </summary>
        public bool Other
        {
            get 
            {
                if (this.Rel == ContactsRelationships.IsOther)
                {
                    return true;
                }
                return false;
            }
        }
    }


    /// <summary>
    /// gd:PostalAddress element
    /// </summary>
    public class PostalAddress : CommonAttributesElement
    {
        /// <summary>
        /// default empty constructor for gd:PostalAddress
        /// </summary>
        public PostalAddress()
        : base(GDataParserNameTable.XmlPostalAddressElement, 
               GDataParserNameTable.gDataPrefix,
               GDataParserNameTable.gNamespace)
        {
        }

        /// <summary>
        ///  default constructor with an initial value
        /// </summary>
        public PostalAddress(string init)
        : base(GDataParserNameTable.XmlPostalAddressElement, 
               GDataParserNameTable.gDataPrefix,
               GDataParserNameTable.gNamespace, init)
        {
        }
    }


    /// <summary>
    /// gd:phonenumber element
    /// </summary>
    public class PhoneNumber : CommonAttributesElement
    {
        /// <summary>
        /// default empty constructor for gd:phonenumber
        /// </summary>
        public PhoneNumber()
        : base(GDataParserNameTable.XmlPhoneNumberElement, 
               GDataParserNameTable.gDataPrefix,
               GDataParserNameTable.gNamespace)
        {
            this.Attributes.Add(GDataParserNameTable.XmlAttributeUri, null);

        }

        /// <summary>
        ///  default constructor with an initial value
        /// </summary>
        public PhoneNumber(string init)
        : base(GDataParserNameTable.XmlPhoneNumberElement, 
               GDataParserNameTable.gDataPrefix,
               GDataParserNameTable.gNamespace, init)
        {
            this.Attributes.Add(GDataParserNameTable.XmlAttributeUri, null);
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string uri</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Uri
        {
            get {return this.Attributes[GDataParserNameTable.XmlAttributeUri] as string;}
            set {this.Attributes[GDataParserNameTable.XmlAttributeUri] = value;}
        }
    }

   /// <summary>
    /// GData schema extension describing an organization
    /// </summary>
    public class Organization : SimpleContainer, ICommonAttributes
    {
        /// <summary>
        /// default constructor for media:group
        /// </summary>
        public Organization() :
            base(GDataParserNameTable.XmlOrganizationElement,
                 BaseNameTable.gDataPrefix,
                 BaseNameTable.gNamespace)
        {
            this.ExtensionFactories.Add(new OrgName());
            this.ExtensionFactories.Add(new OrgTitle());
            this.Attributes.Add(GDataParserNameTable.XmlAttributeLabel, null);
            this.Attributes.Add(GDataParserNameTable.XmlAttributePrimary, null);
            this.Attributes.Add(GDataParserNameTable.XmlAttributeRel, null);
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method for the Rel Value. Note you can only set this
        /// or Label, not both</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Rel
        {
            get {return this.Attributes[GDataParserNameTable.XmlAttributeRel] as string;}
            set 
            {
                if (this.Label != null)
                {
                    throw new System.ArgumentException("Label already has a value. You can only set Label or Rel");
                } 
                this.Attributes[GDataParserNameTable.XmlAttributeRel] = value;
            }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method for the Label value. Note you can only set this or
        /// Rel, not both</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Label
        {
            get {return this.Attributes[GDataParserNameTable.XmlAttributeLabel] as string;}
            set 
            {
                if (this.Rel != null)
                {
                    throw new System.ArgumentException("Rel already has a value. You can only set Label or Rel");
                } 
                this.Attributes[GDataParserNameTable.XmlAttributeLabel] = value;
            }
        }

        /// <summary>
        /// sets the value of the OrgName element
        /// </summary>
        /// <returns></returns>
        public string Name 
        {
            get 
            {
                OrgName name = FindExtension(GDataParserNameTable.XmlOrgNameElement,
                                        BaseNameTable.gNamespace) as OrgName;
                if (name != null)
                {
                    return name.Value;
                }
                return null;
            }
            set
            {
                OrgName name = null;

                if (String.IsNullOrEmpty(value) == false)
                {
                    name = new OrgName(value);
                }
               
                ReplaceExtension(GDataParserNameTable.XmlOrgNameElement,
                                        BaseNameTable.gNamespace,
                                        name);
            }
        }

        /// <summary>
        /// access the OrgTitle element to set/get the title
        /// </summary>
        /// <returns></returns>
        public string Title 
        {
            get 
            {
                OrgTitle title = FindExtension(GDataParserNameTable.XmlOrgTitleElement,
                                        BaseNameTable.gNamespace) as OrgTitle;
                if (title != null)
                {
                    return title.Value;
                }
                return null;
            }
            set
            {
                OrgTitle title = null;
               
                if (String.IsNullOrEmpty(value) == false)
                {
                    title = new OrgTitle(value);
                }
               
                ReplaceExtension(GDataParserNameTable.XmlOrgTitleElement,
                                        BaseNameTable.gNamespace,
                                        title);

            }
        }
        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Primary</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public bool Primary
        {
            get {return ("true" == (this.Attributes[GDataParserNameTable.XmlAttributePrimary] as string));}
            set {this.Attributes[GDataParserNameTable.XmlAttributePrimary] = value == true? Utilities.XSDTrue : Utilities.XSDFalse;}
        }

        /// <summary>
        /// returns if the email is the home email address
        /// </summary>
        public bool Home
        {
            get 
            {
                if (this.Rel == ContactsRelationships.IsHome)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// returns if the email is the home email address
        /// </summary>
        public bool Other
        {
            get 
            {
                if (this.Rel == ContactsRelationships.IsOther)
                {
                    return true;
                }
                return false;
            }
        }
    }

    /// <summary>
    /// gd:OrgName schema extension describing an organization name
    /// it's a child of Organization
    /// </summary>
    public class OrgName : SimpleElement
    {
        /// <summary>
        /// default constructor for gd:OrgName 
        /// </summary>
        public OrgName()
        : base(GDataParserNameTable.XmlOrgNameElement, 
               BaseNameTable.gDataPrefix,
               BaseNameTable.gNamespace)
        {}

        /// <summary>
        /// default constructor for gd:OrgName  with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public OrgName(string initValue)
        : base(GDataParserNameTable.XmlOrgNameElement, 
               BaseNameTable.gDataPrefix,
               BaseNameTable.gNamespace, initValue)
        {}
    }

    /// <summary>
    /// gd:OrgTitle schema extension describing the title of a person in an organization
    /// it's a child of Organization
    /// </summary>
    public class OrgTitle : SimpleElement
    {
        /// <summary>
        /// default constructor for gd:OrgTitle 
        /// </summary>
        public OrgTitle()
        : base(GDataParserNameTable.XmlOrgTitleElement, 
               BaseNameTable.gDataPrefix,
               BaseNameTable.gNamespace)
        {}

        /// <summary>
        /// default constructor for gd:OrgTitle  with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public OrgTitle(string initValue)
        : base(GDataParserNameTable.XmlOrgTitleElement, 
               BaseNameTable.gDataPrefix,
               BaseNameTable.gNamespace, initValue)
        {}
    }
  
}

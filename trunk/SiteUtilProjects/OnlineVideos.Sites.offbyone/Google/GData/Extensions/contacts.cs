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
    public class ContactsKindExtensions
    {
        /// <summary>
        /// helper to add all MediaRss extensions to a base object
        /// </summary>
        /// <param name="baseObject"></param>
        public static void AddExtension(AtomBase baseObject) 
        {
            baseObject.AddExtension(new EMail());
            baseObject.AddExtension(new IMAddress());
            baseObject.AddExtension(new Organization());
            baseObject.AddExtension(new PhoneNumber());
            baseObject.AddExtension(new Name());
            baseObject.AddExtension(new StructuredPostalAddress());
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
        public const string IsHome = BaseNameTable.gNamespace + "#home";
        /// <summary>
        /// indicates an undefined email in the rel field, label might be used to be
        /// more precise
        /// </summary>
        public const string IsOther = BaseNameTable.gNamespace + "#other";
        /// <summary>
        /// indicates a work email in the rel field
        /// </summary>
        public const string IsWork = BaseNameTable.gNamespace + "#work";

        /// <summary>
        /// indicates a general value in the rel field
        /// </summary>
        public const string IsGeneral = BaseNameTable.gNamespace + "#general";

        /// <summary>
        /// indicates a car related value in the rel field
        /// </summary>
        public const string IsCar = BaseNameTable.gNamespace + "#car";

        /// <summary>
        /// indicates a fax value in the rel field
        /// </summary>
        public const string IsFax = BaseNameTable.gNamespace + "#fax";

        /// <summary>
        /// indicates a home fax value in the rel field
        /// </summary>
        public const string IsHomeFax = BaseNameTable.gNamespace + "#home_fax";

        /// <summary>
        /// indicates a work fax value in the rel field
        /// </summary>
        public const string IsWorkFax = BaseNameTable.gNamespace + "#work_fax";

        /// <summary>
        /// indicates an internal extension value in the rel field
        /// </summary>
        public const string IsInternalExtension = BaseNameTable.gNamespace + "#internal-extension";

        /// <summary>
        /// indicates a mobile number value in the rel field
        /// </summary>
        public const string IsMobile = BaseNameTable.gNamespace + "#mobile";

        /// <summary>
        /// indicates a pager value in the rel field
        /// </summary>
        public const string IsPager = BaseNameTable.gNamespace + "#pager";

        /// <summary>
        /// indicates a satellite value in the rel field
        /// </summary>
        public const string IsSatellite = BaseNameTable.gNamespace + "#satellite";

        /// <summary>
        /// indicates a voip value in the rel field
        /// </summary>
        public const string IsVoip = BaseNameTable.gNamespace + "#voip";

        /// <summary>
        /// Assistant's number
        /// </summary>
        public const string IsAssistant = BaseNameTable.gNamespace + "#assistant";

        /// <summary>
        /// Callback number
        /// </summary>
        public const string IsCallback = BaseNameTable.gNamespace + "#callback";

        /// <summary>
        /// CompanyMain
        /// </summary>
        public const string IsCompanyMain = BaseNameTable.gNamespace + "#company_main";

        /// <summary>
        /// ISDN number
        /// </summary>
        public const string IsISDN = BaseNameTable.gNamespace + "#isdn";
        
        /// <summary>
        /// Main number
        /// </summary>
        public const string IsMain = BaseNameTable.gNamespace + "#main";
        
        /// <summary>
        /// OtherFax number
        /// </summary>
        public const string IsOtherFax = BaseNameTable.gNamespace + "#other_fax";
        
        /// <summary>
        /// Radio number
        /// </summary>
        public const string IsRadio = BaseNameTable.gNamespace + "#radio";
        
        /// <summary>
        /// Telex number
        /// </summary>
        public const string IsTelex = BaseNameTable.gNamespace + "#telex";
        
        /// <summary>
        /// TTY_TDD number
        /// </summary>
        public const string IsTTY_TDD = BaseNameTable.gNamespace + "#tty_tdd";
        
        /// <summary>
        /// WorkMobile number
        /// </summary>
        public const string IsWorkMobile = BaseNameTable.gNamespace + "#work_mobile";
        
        /// <summary>
        /// WorkPager number
        /// </summary>
        public const string IsWorkPager = BaseNameTable.gNamespace + "#work_pager";

        /// <summary>
        /// Netmeeting relationship
        /// </summary>
        public const string IsNetmeeting = BaseNameTable.gNamespace + "#netmeeting";
    }

 /// <summary>
    /// holds static strings indicating several often used protocol 
    /// values for the contacts API in the IM element
    /// </summary>
    public static class ContactsProtocols
    {
        /// <summary>
        /// AOL Instant Messenger protocol
        /// </summary>
        public const string IsAIM = BaseNameTable.gNamespace + "#AIM"; 

        /// <summary>
        /// MSN protocol
        /// </summary>
        public const string IsMSN = BaseNameTable.gNamespace + "#MSN";
        
        /// <summary>
        /// Yahoo protocol
        /// </summary>
        public const string IsYahoo = BaseNameTable.gNamespace + "#YAHOO";
        
        /// <summary>
        /// Skype protocol
        /// </summary>
        public const string IsSkype = BaseNameTable.gNamespace + "#SKYPE";
        
        /// <summary>
        /// QQ protocol
        /// </summary>
        public const string IsQQ = BaseNameTable.gNamespace + "#QQ";
        
        /// <summary>
        /// GoogleTalk protocol
        /// </summary>
        public const string IsGoogleTalk = BaseNameTable.gNamespace + "#GOOGLE_TALK";
        
        /// <summary>
        /// ICQ protocol
        /// </summary>
        public const string IsICQ = BaseNameTable.gNamespace + "#ICQ";
        
        /// <summary>
        /// Jabber protocol
        /// </summary>
        public const string IsJabber = BaseNameTable.gNamespace + "#JABBER";
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
            this.Attributes.Add(GDataParserNameTable.XmlAttributeDisplayName, null);
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
    /// a base class used for several contacts related classes and others. 
    /// </summary>
    public class LinkAttributesElement : SimpleElement, ICommonAttributes
    {

        /// <summary>
        /// default constructore with namesapce init
        /// </summary>
        /// <param name="element"></param>
        /// <param name="prefix"></param>
        /// <param name="ns"></param>
        public LinkAttributesElement(string element, string prefix, string ns) : base(element, prefix, ns)
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
        public LinkAttributesElement(string element, string prefix, string ns, string init) : base(element, prefix, ns, init)
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
            set {this.Attributes[GDataParserNameTable.XmlAttributePrimary] = value ? Utilities.XSDTrue : Utilities.XSDFalse;}
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
  }




    /// <summary>
    /// a base class used for PostalAddress and others. 
    /// </summary>
    public class CommonAttributesElement : LinkAttributesElement
    {

        /// <summary>
        /// default constructore with namesapce init
        /// </summary>
        /// <param name="element"></param>
        /// <param name="prefix"></param>
        /// <param name="ns"></param>
        public CommonAttributesElement(string element, string prefix, string ns) : base(element, prefix, ns)
        {
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
        }
        /// <summary>
        /// returns if the email is the home email address
        /// </summary>
        public bool Home
        {
            get 
            {
                return IsType(ContactsRelationships.IsHome);
            }
        }

        /// <summary>
        /// returns if the email is the home email address
        /// </summary>
        public bool Work       {
            get 
            {
                return IsType(ContactsRelationships.IsWork);
            }
        }

        /// <summary>
        /// returns if the email is the home email address
        /// </summary>
        public bool Other
        {
            get 
            {
                return IsType(ContactsRelationships.IsOther);
            }
        }

        /// <summary>
        /// returns true if the element is of the given relationship type
        /// </summary>
        /// <param name="relationShip"></param>
        /// <returns></returns>
        public bool IsType(string relationShip)
        {
            return this.Rel == relationShip;
        }
    }


    /// <summary>
    /// gd:PostalAddress element
    /// </summary>
    [Obsolete("Use New Version StructuredPostalAddress")]
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
            this.ExtensionFactories.Add(new OrgDepartment());
            this.ExtensionFactories.Add(new OrgJobDescription());
            this.ExtensionFactories.Add(new OrgSymbol());
            this.ExtensionFactories.Add(new Where());
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
                return GetStringValue<OrgName>(GDataParserNameTable.XmlOrgNameElement,
                                        BaseNameTable.gNamespace);
                            }
            set
            {
                SetStringValue<OrgName>(value, GDataParserNameTable.XmlOrgNameElement,
                                        BaseNameTable.gNamespace);
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
                return GetStringValue<OrgTitle>(GDataParserNameTable.XmlOrgTitleElement,
                                        BaseNameTable.gNamespace);
                            }
            set
            {
                SetStringValue<OrgTitle>(value, GDataParserNameTable.XmlOrgTitleElement,
                                        BaseNameTable.gNamespace);
            }
        }

        /// <summary>
        /// Symbol of the organization.
        /// </summary>
        /// <returns></returns>
        public string Symbol 
        {
            get 
            {
                return GetStringValue<OrgSymbol>(GDataParserNameTable.XmlOrgSymbolElement,
                                        BaseNameTable.gNamespace);
                            }
            set
            {
                SetStringValue<OrgSymbol>(value, GDataParserNameTable.XmlOrgSymbolElement,
                                        BaseNameTable.gNamespace);
            }
        }

        /// <summary>
        /// Department of the organization.
        /// </summary>
        /// <returns></returns>
        public string Department 
        {
            get 
            {
                return GetStringValue<OrgDepartment>(GDataParserNameTable.XmlOrgDepartmentElement,
                                        BaseNameTable.gNamespace);
                            }
            set
            {
                SetStringValue<OrgDepartment>(value, GDataParserNameTable.XmlOrgDepartmentElement,
                                        BaseNameTable.gNamespace);
            }
        }

        /// <summary>
        /// Job Description in the organization.
        /// </summary>
        /// <returns></returns>
        public string JobDescription 
        {
            get 
            {
                return GetStringValue<OrgJobDescription>(GDataParserNameTable.XmlOrgJobDescriptionElement,
                                        BaseNameTable.gNamespace);
                            }
            set
            {
                SetStringValue<OrgJobDescription>(value, GDataParserNameTable.XmlOrgJobDescriptionElement,
                                        BaseNameTable.gNamespace);
            }
        }

        
        /// <summary>
        /// Location associated with the organization
        /// </summary>
        /// <returns></returns>
        public String Location
        {
            get 
            {
                Where w = FindExtension(GDataParserNameTable.XmlWhereElement, BaseNameTable.gNamespace) as Where;
                return w != null ? w.ValueString : null;
            }
            set
            { 
                Where w = null;
                if (value != null)
                {
                    w = new Where(null, null, value);
                }
                ReplaceExtension(GDataParserNameTable.XmlWhereElement, BaseNameTable.gNamespace, w);
            }
        }

        



        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Primary</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public bool Primary
        {
            get {return ("true" == (this.Attributes[GDataParserNameTable.XmlAttributePrimary] as string));}
            set {this.Attributes[GDataParserNameTable.XmlAttributePrimary] = value ? Utilities.XSDTrue : Utilities.XSDFalse;}
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

    /// <summary>
    /// describes a department within an organization.
    /// it's a child of Organization
    /// </summary>
    public class OrgDepartment : SimpleElement
    {
        /// <summary>
        /// default constructor for gd:OrgDepartment 
        /// </summary>
        public OrgDepartment()
        : base(GDataParserNameTable.XmlOrgDepartmentElement, 
               BaseNameTable.gDataPrefix,
               BaseNameTable.gNamespace)
        {}

        /// <summary>
        /// default constructor for gd:OrgDepartment  with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public OrgDepartment(string initValue)
        : base(GDataParserNameTable.XmlOrgDepartmentElement, 
               BaseNameTable.gDataPrefix,
               BaseNameTable.gNamespace, initValue)
        {}
    }

    /// <summary>
    /// Describes a job within an organization.
    /// it's a child of Organization
    /// </summary>
    public class OrgJobDescription : SimpleElement
    {
        /// <summary>
        /// default constructor for gd:XmlOrgJobDescriptionElement 
        /// </summary>
        public OrgJobDescription()
        : base(GDataParserNameTable.XmlOrgJobDescriptionElement, 
               BaseNameTable.gDataPrefix,
               BaseNameTable.gNamespace)
        {}

        /// <summary>
        /// default constructor for gd:XmlOrgJobDescriptionElement  with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public OrgJobDescription(string initValue)
        : base(GDataParserNameTable.XmlOrgJobDescriptionElement, 
               BaseNameTable.gDataPrefix,
               BaseNameTable.gNamespace, initValue)
        {}
    }

    /// <summary>
    /// Provides a symbol of an organization
    /// it's a child of Organization
    /// </summary>
    public class OrgSymbol : SimpleElement
    {
        /// <summary>
        /// default constructor for OrgSymbol
        /// </summary>
        public OrgSymbol()
        : base(GDataParserNameTable.XmlOrgSymbolElement, 
               BaseNameTable.gDataPrefix,
               BaseNameTable.gNamespace)
        {}

        /// <summary>
        /// default constructor for OrgSymbol with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public OrgSymbol(string initValue)
        : base(GDataParserNameTable.XmlOrgSymbolElement, 
               BaseNameTable.gDataPrefix,
               BaseNameTable.gNamespace, initValue)
        {}
    }


#region Contacts version 3 additions

    /// <summary>
    /// Phoneticname schema extension 
    /// </summary>
    public class PhoneticName : SimpleElement
    {
        public static string AttributeYomi = "yomi";

        /// <summary>
        /// default constructor for PhoneticName
        /// </summary>
        public PhoneticName(string xmlElement, string xmlPrefix, string xmlNamespace)
            : base(xmlElement, xmlPrefix, xmlNamespace)
        {
            this.Attributes.Add(AttributeYomi, null);
        }
    
        /// <summary>
        /// default constructor for PhoneticName with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public PhoneticName(string xmlElement, string xmlPrefix, string xmlNamespace, string initValue, string initYomi)
            : base(xmlElement, xmlPrefix, xmlNamespace, initValue)            
        {
            this.Attributes.Add(AttributeYomi, initYomi);
        }
         

        /// <summary>
        /// Phonetic representation
        /// </summary>
        /// <returns></returns>
        public string Yomi
        {
            get
            {
                return this.Attributes[AttributeYomi] as string;
            }
            set 
            {
                this.Attributes[AttributeYomi] = value;
            }
        }
    }

    /// <summary>
    /// GivenName schema extension 
    /// </summary>
    public class GivenName : PhoneticName
    {

        /// <summary>
        /// default constructor for GivenName
        /// </summary>
        public GivenName()
            : base(GDataParserNameTable.GivenNameElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace)
        {
        }
    
        /// <summary>
        /// default constructor for GivenName with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public GivenName(string initValue, string initYomi)
            : base(GDataParserNameTable.GivenNameElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace, initValue, initYomi)
        {
        }
    }

    /// <summary>
    /// Allows storing person's name in a structured way. Consists of given name, additional name, family name, prefix, suffix and full name
    /// </summary>
    public class Name : SimpleContainer
    {
        /// <summary>
        /// default constructor for Name
        /// </summary>
        public Name()
            : base(GDataParserNameTable.NameElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace)
        {
            this.ExtensionFactories.Add(new GivenName());
            this.ExtensionFactories.Add(new AdditionalName());
            this.ExtensionFactories.Add(new FamilyName());
            this.ExtensionFactories.Add(new NamePrefix());
            this.ExtensionFactories.Add(new NameSuffix());
            this.ExtensionFactories.Add(new FullName());
        }


        /// <summary>
        /// Person's given name.
        /// </summary>
        /// <returns></returns>
        public string GivenName 
        {
            get 
            {
                return GetStringValue<GivenName>(GDataParserNameTable.GivenNameElement,
                                        BaseNameTable.gNamespace);
                            }
            set
            {
                SetStringValue<GivenName>(value, GDataParserNameTable.GivenNameElement,
                                        BaseNameTable.gNamespace);
                
            }
        }

        /// <summary>
        /// Person's given name phonetics
        /// </summary>
        /// <returns></returns>
        public string GivenNamePhonetics
        {
            get 
            {
                return GetYomiValue<GivenName>(GDataParserNameTable.GivenNameElement,
                                        BaseNameTable.gNamespace);
            }
            set
            {
                SetYomiValue<GivenName>(value, GDataParserNameTable.GivenNameElement,
                                        BaseNameTable.gNamespace);
            }
         }


        /// <summary>
        /// Additional name of the person, eg. middle name.
        /// </summary>
        /// <returns></returns>
        public string AdditionalName 
        {
            get 
            {
                return GetStringValue<AdditionalName>(GDataParserNameTable.AdditionalNameElement,
                        BaseNameTable.gNamespace);

            }
            set
            {
                SetStringValue<AdditionalName>(value, GDataParserNameTable.AdditionalNameElement,
                                        BaseNameTable.gNamespace);
       
            }
        }

        /// <summary>
        /// Person's additional name phonetics
        /// </summary>
        /// <returns></returns>
        public string AdditionalNamePhonetics
        {
            get 
            {
                return GetYomiValue<AdditionalName>(GDataParserNameTable.AdditionalNameElement,
                                        BaseNameTable.gNamespace);
            }
            set
            {
                SetYomiValue<AdditionalName>(value, GDataParserNameTable.AdditionalNameElement,
                                        BaseNameTable.gNamespace);
            }
        }


        /// <summary>
        /// Person's family name.
        /// </summary>
        /// <returns></returns>
        public string FamilyName 
        {
            get 
            {
                return GetStringValue<FamilyName>(GDataParserNameTable.FamilyNameElement,
                        BaseNameTable.gNamespace);

            }
            set
            {
                SetStringValue<FamilyName>(value, GDataParserNameTable.FamilyNameElement,
                                        BaseNameTable.gNamespace);
       
            }
        }

        /// <summary>
        /// FamilyName phonetics
        /// </summary>
        /// <returns></returns>
        public string FamilyNamePhonetics
        {
            get 
            {
                return GetYomiValue<FamilyName>(GDataParserNameTable.FamilyNameElement,
                                        BaseNameTable.gNamespace);
            }
            set
            {
                SetYomiValue<FamilyName>(value, GDataParserNameTable.FamilyNameElement,
                                        BaseNameTable.gNamespace);
            }
        }

        /// <summary>
        /// Honorific prefix, eg. 'Mr' or 'Mrs'.
        /// </summary>
        /// <returns></returns>
        public string NamePrefix 
        {
            get 
            {
                return GetStringValue<NamePrefix>(GDataParserNameTable.NamePrefixElement,
                        BaseNameTable.gNamespace);

            }
            set
            {
                SetStringValue<NamePrefix>(value, GDataParserNameTable.NamePrefixElement,
                                        BaseNameTable.gNamespace);
       
            }
        }

        /// <summary>
        /// Honorific suffix, eg. 'san' or 'III'.
        /// </summary>
        /// <returns></returns>
        public string NameSuffix 
        {
            get 
            {
                return GetStringValue<NameSuffix>(GDataParserNameTable.NameSuffixElement,
                        BaseNameTable.gNamespace);

            }
            set
            {
                SetStringValue<NameSuffix>(value, GDataParserNameTable.NameSuffixElement,
                                        BaseNameTable.gNamespace);
       
            }
        }

        /// <summary>
        /// Unstructured representation of the name
        /// </summary>
        /// <returns></returns>
        public string FullName 
        {
            get 
            {
                return GetStringValue<FullName>(GDataParserNameTable.FullNameElement,
                        BaseNameTable.gNamespace);

            }
            set
            {
                SetStringValue<FullName>(value, GDataParserNameTable.FullNameElement,
                                        BaseNameTable.gNamespace);
       
            }
        }

        private void SetYomiValue<T>(string value, string elementName, string ns) where T : PhoneticName
        {
            T t =  FindExtension(elementName, ns) as T;
                
            if (t == null)
            {
                throw new ArgumentException("Can not set phonetics if there is no value itself set");
            }
            t.Yomi = value;
        }


        private string GetYomiValue<T>(string elementName, string ns) where T : PhoneticName
        {
            T t =  FindExtension(elementName, ns) as T;
            if (t != null)
            {
                return t.Yomi;
            }
            return null;
        }
    }

   
   

    /// <summary>
    /// AdditionalName schema extension 
    /// </summary>
    public class AdditionalName : PhoneticName
    {
        /// <summary>
        /// default constructor for AdditionalName
        /// </summary>
        public AdditionalName()
            : base(GDataParserNameTable.AdditionalNameElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace)
        {
        }
    
        /// <summary>
        /// default constructor for AdditionalName with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public AdditionalName(string initValue, string initYomi)
            : base(GDataParserNameTable.AdditionalNameElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace, initValue, initYomi)
        {
        }
    }

    /// <summary>
    /// FamilyName schema extension 
    /// </summary>
    public class FamilyName : PhoneticName
    {
        /// <summary>
        /// default constructor for AdditionalName
        /// </summary>
        public FamilyName()
            : base(GDataParserNameTable.FamilyNameElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace)
        {
        }
    
        /// <summary>
        /// default constructor for AdditionalName with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public FamilyName(string initValue, string initYomi)
            : base(GDataParserNameTable.FamilyNameElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace, initValue, initYomi)
        {
        }
    }


    /// <summary>
    /// NamePrefix schema extension 
    /// </summary>
    public class NamePrefix : SimpleElement
    {
        /// <summary>
        /// default constructor for NamePrefix
        /// </summary>
        public NamePrefix()
            : base(GDataParserNameTable.NamePrefixElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace)
        {
        }
    
        /// <summary>
        /// default constructor for NamePrefix with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public NamePrefix(string initValue)
            : base(GDataParserNameTable.NamePrefixElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace, initValue)
        {
        }
    }

    /// <summary>
    /// NameSuffix schema extension 
    /// </summary>
    public class NameSuffix : SimpleElement
    {
        /// <summary>
        /// default constructor for NameSuffix
        /// </summary>
        public NameSuffix()
            : base(GDataParserNameTable.NameSuffixElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace)
        {
        }
    
        /// <summary>
        /// default constructor for NameSuffix with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public NameSuffix(string initValue)
            : base(GDataParserNameTable.NameSuffixElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace, initValue)
        {
        }
    }

    /// <summary>
    /// FullName schema extension 
    /// </summary>
    public class FullName : SimpleElement
    {
        /// <summary>
        /// default constructor for FullName
        /// </summary>
        public FullName()
            : base(GDataParserNameTable.FullNameElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace)
        {
        }
    
        /// <summary>
        /// default constructor for FullName with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public FullName(string initValue)
            : base(GDataParserNameTable.FullNameElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace, initValue)
        {
        }
    }



    /// <summary>
    /// Postal address split into components. It allows to store the address in locale independent 
    /// format. The fields can be interpreted and used to generate formatted, locale dependent 
    /// address. The following elements reperesent parts of the address: agent, house name, street, 
    /// P.O. box, neighborhood, city, subregion, region, postal code, country. The subregion element 
    /// is not used for postal addresses, it is provided for extended uses of addresses only. In 
    /// order to store postal address in an unstructured form formatted address field is provided.
    /// </summary>
    public class StructuredPostalAddress : SimpleContainer, ICommonAttributes
    {
        /// <summary>
        /// default constructor for StructuredPostalAddress
        /// </summary>
        public StructuredPostalAddress()
            : base(GDataParserNameTable.StructuredPostalAddressElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace)
        {
            this.Attributes.Add(GDataParserNameTable.XmlAttributeLabel, null);
            this.Attributes.Add(GDataParserNameTable.XmlAttributePrimary, null);
            this.Attributes.Add(GDataParserNameTable.XmlAttributeRel, null);
   
            this.Attributes.Add(GDataParserNameTable.XmlAttributeMailClass, null);
            this.Attributes.Add(GDataParserNameTable.XmlAttributeUsage, null);
            
            this.ExtensionFactories.Add(new Agent());
            this.ExtensionFactories.Add(new Housename());
            this.ExtensionFactories.Add(new Street());
            this.ExtensionFactories.Add(new Pobox());
            this.ExtensionFactories.Add(new Neighborhood());
            this.ExtensionFactories.Add(new City());
            this.ExtensionFactories.Add(new Subregion());
            this.ExtensionFactories.Add(new Region());
            this.ExtensionFactories.Add(new Postcode());
            this.ExtensionFactories.Add(new Country());
            this.ExtensionFactories.Add(new FormattedAddress());
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Primary</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public bool Primary
        {
            get {return ("true" == (this.Attributes[GDataParserNameTable.XmlAttributePrimary] as string));}
            set {this.Attributes[GDataParserNameTable.XmlAttributePrimary] = value ? Utilities.XSDTrue : Utilities.XSDFalse;}
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


        //////////////////////////////////////////////////////////////////////
        /// <summary>The context in which this addess can be used. Local addresses 
        /// may differ in layout from general addresses, and frequently use local 
        /// script (as opposed to Latin script) as well, though local script is 
        /// allowed in general addresses. Unless specified general usage is assumed.</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Usage
        {
            get {return this.Attributes[GDataParserNameTable.XmlAttributeUsage] as string;}
            set {this.Attributes[GDataParserNameTable.XmlAttributeUsage] = value;}
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>Classes of mail accepted at the address.
        ///  Unless specified both is assumed</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string MailClass
        {
            get {return this.Attributes[GDataParserNameTable.XmlAttributeMailClass] as string;}
            set {this.Attributes[GDataParserNameTable.XmlAttributeMailClass] = value;}
        }

        /// <summary>
        /// The agent who actually receives the mail. 
        /// Used in work addresses. Also for 'in care of' or 'c/o'.
        /// </summary>
        /// <returns></returns>
        public string Agent 
        {
            get 
            {
                return GetStringValue<Agent>(GDataParserNameTable.AgentElement,
                        BaseNameTable.gNamespace);
            }
            set
            {
                SetStringValue<Agent>(value, GDataParserNameTable.AgentElement,
                                        BaseNameTable.gNamespace);
            }
        }
        
        /// <summary>
        /// Used in places where houses or buildings have names 
        /// (and not necessarily numbers), eg. "The Pillars".
        /// </summary>
        /// <returns></returns>
        public string Housename 
        {
            get 
            {
                return GetStringValue<Housename>(GDataParserNameTable.HousenameElement,
                        BaseNameTable.gNamespace);
            }
            set
            {
                SetStringValue<Housename>(value, GDataParserNameTable.HousenameElement,
                                        BaseNameTable.gNamespace);
            }
        }
        
        /// <summary>
        /// Can be street, avenue, road, etc. This element also
        ///  includes the house number and room/apartment/flat/floor number.
        /// </summary>
        /// <returns></returns>
        public string Street 
        {
            get 
            {
                return GetStringValue<Street>(GDataParserNameTable.StreetElement,
                        BaseNameTable.gNamespace);
            }
            set
            {
                SetStringValue<Street>(value, GDataParserNameTable.StreetElement,
                                        BaseNameTable.gNamespace);
            }
        }
        
        /// <summary>
        /// Covers actual P.O. boxes, drawers, locked bags, etc. 
        /// This is usually but not always mutually exclusive with street.
        /// </summary>
        /// <returns></returns>
        public string Pobox 
        {
            get 
            {
                return GetStringValue<Pobox>(GDataParserNameTable.PoboxElement,
                        BaseNameTable.gNamespace);
            }
            set
            {
                SetStringValue<Pobox>(value, GDataParserNameTable.PoboxElement,
                                        BaseNameTable.gNamespace);
            }
        }
        
        /// <summary>
        /// This is used to disambiguate a street address when a city contains more 
        /// than one street with the same name, or to specify a small place whose 
        /// mail is routed through a larger postal town. In China it could be a 
        /// county or a minor city.
        /// </summary>
        /// <returns></returns>
        public string Neighborhood 
        {
            get 
            {
                return GetStringValue<Neighborhood>(GDataParserNameTable.NeighborhoodElement,
                        BaseNameTable.gNamespace);
            }
            set
            {
                SetStringValue<Neighborhood>(value, GDataParserNameTable.NeighborhoodElement,
                                        BaseNameTable.gNamespace);
            }
        }
        
        /// <summary>
        /// Can be city, village, town, borough, etc. This is the postal town and not 
        /// necessarily the place of residence or place of business.
        /// </summary>
        /// <returns></returns>
        public string City 
        {
            get 
            {
                return GetStringValue<City>(GDataParserNameTable.CityElement,
                        BaseNameTable.gNamespace);
            }
            set
            {
                SetStringValue<City>(value, GDataParserNameTable.CityElement,
                                        BaseNameTable.gNamespace);
            }
        }
        

        /// <summary>
        /// Handles administrative districts such as U.S. or U.K. counties that are not 
        /// used for mail addressing purposes. Subregion is not intended for delivery 
        /// addresses.
        /// </summary>
        /// <returns></returns>
        public string Subregion 
        {
            get 
            {
                return GetStringValue<Subregion>(GDataParserNameTable.SubregionElement,
                        BaseNameTable.gNamespace);
            }
            set
            {
                SetStringValue<Subregion>(value, GDataParserNameTable.SubregionElement,
                                        BaseNameTable.gNamespace);
            }
        }
        
        /// <summary>
        /// A state, province, county (in Ireland), Land (in Germany), departement 
        /// (in France), etc.
        /// </summary>
        /// <returns></returns>
        public string Region 
        {
            get 
            {
                return GetStringValue<Region>(GDataParserNameTable.RegionElement,
                        BaseNameTable.gNamespace);
            }
            set
            {
                SetStringValue<Region>(value, GDataParserNameTable.RegionElement,
                                        BaseNameTable.gNamespace);
            }
        }
        
        /// <summary>
        /// Postal code. Usually country-wide, but sometimes specific to the city 
        /// (e.g. "2" in "Dublin 2, Ireland" addresses).
        /// </summary>
        /// <returns></returns>
        public string Postcode 
        {
            get 
            {
                return GetStringValue<Postcode>(GDataParserNameTable.PostcodeElement,
                        BaseNameTable.gNamespace);
            }
            set
            {
                SetStringValue<Postcode>(value, GDataParserNameTable.PostcodeElement,
                                        BaseNameTable.gNamespace);
            }
        }
        
        /// <summary>
        /// The name or code of the country.
        /// </summary>
        /// <returns></returns>
        public string Country 
        {
            get 
            {
                return GetStringValue<Country>(GDataParserNameTable.CountryElement,
                        BaseNameTable.gNamespace);
            }
            set
            {
                SetStringValue<Country>(value, GDataParserNameTable.CountryElement,
                                        BaseNameTable.gNamespace);
            }
        }
        
        /// <summary>
        /// The full, unstructured postal address.
        /// </summary>
        /// <returns></returns>
        public string FormattedAddress 
        {
            get 
            {
                return GetStringValue<FormattedAddress>(GDataParserNameTable.FormattedAddressElement,
                        BaseNameTable.gNamespace);
            }
            set
            {
                SetStringValue<FormattedAddress>(value, GDataParserNameTable.FormattedAddressElement,
                                        BaseNameTable.gNamespace);
            }
        }
    }

    /// <summary>
    /// Agent schema extension 
    /// </summary>
    public class Agent : SimpleElement
    {
        /// <summary>
        /// default constructor for Agent
        /// </summary>
        public Agent()
            : base(GDataParserNameTable.AgentElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace)
        {
        }
    
        /// <summary>
        /// default constructor for Agent with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public Agent(string initValue)
            : base(GDataParserNameTable.AgentElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace, initValue)
        {
        }
    }

    /// <summary>
    /// Housename schema extension 
    /// </summary>
    public class Housename : SimpleElement
    {
        /// <summary>
        /// default constructor for Housename
        /// </summary>
        public Housename()
            : base(GDataParserNameTable.HousenameElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace)
        {
        }
    
        /// <summary>
        /// default constructor for Housename with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public Housename(string initValue)
            : base(GDataParserNameTable.HousenameElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace, initValue)
        {
        }
    }

    /// <summary>
    /// Street schema extension 
    /// </summary>
    public class Street : SimpleElement
    {
        /// <summary>
        /// default constructor for Street
        /// </summary>
        public Street()
            : base(GDataParserNameTable.StreetElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace)
        {
        }
    
        /// <summary>
        /// default constructor for Street with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public Street(string initValue)
            : base(GDataParserNameTable.StreetElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace, initValue)
        {
        }
    }

    /// <summary>
    /// Pobox schema extension 
    /// </summary>
    public class Pobox : SimpleElement
    {
        /// <summary>
        /// default constructor for Pobox
        /// </summary>
        public Pobox()
            : base(GDataParserNameTable.PoboxElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace)
        {
        }
    
        /// <summary>
        /// default constructor for Pobox with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public Pobox(string initValue)
            : base(GDataParserNameTable.PoboxElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace, initValue)
        {
        }
    }

    /// <summary>
    /// Neighborhood schema extension 
    /// </summary>
    public class Neighborhood : SimpleElement
    {
        /// <summary>
        /// default constructor for Neighborhood
        /// </summary>
        public Neighborhood()
            : base(GDataParserNameTable.NeighborhoodElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace)
        {
        }
    
        /// <summary>
        /// default constructor for Neighborhood with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public Neighborhood(string initValue)
            : base(GDataParserNameTable.NeighborhoodElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace, initValue)
        {
        }
    }

    /// <summary>
    /// City schema extension 
    /// </summary>
    public class City : SimpleElement
    {
        /// <summary>
        /// default constructor for City
        /// </summary>
        public City()
            : base(GDataParserNameTable.CityElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace)
        {
        }
    
        /// <summary>
        /// default constructor for City with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public City(string initValue)
            : base(GDataParserNameTable.CityElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace, initValue)
        {
        }
    }

    /// <summary>
    /// Subregion schema extension 
    /// </summary>
    public class Subregion : SimpleElement
    {
        /// <summary>
        /// default constructor for Subregion
        /// </summary>
        public Subregion()
            : base(GDataParserNameTable.SubregionElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace)
        {
        }
    
        /// <summary>
        /// default constructor for Subregion with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public Subregion(string initValue)
            : base(GDataParserNameTable.SubregionElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace, initValue)
        {
        }
    }

    /// <summary>
    /// Region schema extension 
    /// </summary>
    public class Region : SimpleElement
    {
        /// <summary>
        /// default constructor for Region
        /// </summary>
        public Region()
            : base(GDataParserNameTable.RegionElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace)
        {
        }
    
        /// <summary>
        /// default constructor for Region with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public Region(string initValue)
            : base(GDataParserNameTable.RegionElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace, initValue)
        {
        }
    }

    /// <summary>
    /// Postcode schema extension 
    /// </summary>
    public class Postcode : SimpleElement
    {
        /// <summary>
        /// default constructor for Postcode
        /// </summary>
        public Postcode()
            : base(GDataParserNameTable.PostcodeElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace)
        {
        }
    
        /// <summary>
        /// default constructor for Postcode with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public Postcode(string initValue)
            : base(GDataParserNameTable.PostcodeElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace, initValue)
        {
        }
    }

    /// <summary>
    /// Country schema extension 
    /// </summary>
    public class Country : SimpleElement
    {
        /// <summary>
        /// default constructor for Country
        /// </summary>
        public Country()
            : base(GDataParserNameTable.CountryElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace)
        {
        }
    
        /// <summary>
        /// default constructor for Country with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public Country(string initValue)
            : base(GDataParserNameTable.CountryElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace, initValue)
        {
        }
    }

    /// <summary>
    /// FormattedAddress schema extension 
    /// </summary>
    public class FormattedAddress : SimpleElement
    {
        /// <summary>
        /// default constructor for FormattedAddress
        /// </summary>
        public FormattedAddress()
            : base(GDataParserNameTable.FormattedAddressElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace)
        {
        }
    
        /// <summary>
        /// default constructor for FormattedAddress with an initial value
        /// </summary>
        /// <param name="initValue"/>
        public FormattedAddress(string initValue)
            : base(GDataParserNameTable.FormattedAddressElement, 
                   BaseNameTable.gDataPrefix,
                   BaseNameTable.gNamespace, initValue)
        {
        }
    }





#endregion
  
}

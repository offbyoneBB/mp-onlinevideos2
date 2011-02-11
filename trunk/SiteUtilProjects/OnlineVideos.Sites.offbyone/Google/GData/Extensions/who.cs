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
using System.Xml;
using System.Collections;
using System.Text;
using System.Globalization;
using Google.GData.Client;

namespace Google.GData.Extensions {

  

    /// <summary>
    /// GData schema extension describing a person.
    /// It contains a gd:entryLink element containing the described person.
    /// </summary>
    public class Who : IExtensionElementFactory
    {

        /// <summary>
        /// Relation type. Describes the meaning of this association.
        /// </summary>
        public class RelType
        {
            /// <summary>Relationship value Attendee</summary>
            public const string EVENT_ATTENDEE = BaseNameTable.gNamespacePrefix + "event.attendee";
            /// <summary>Relationship value Organizer</summary>
            public const string EVENT_ORGANIZER = BaseNameTable.gNamespacePrefix + "event.organizer";
            /// <summary>Relationship value Speaker</summary>
            public const string EVENT_SPEAKER = BaseNameTable.gNamespacePrefix + "event.speaker";
            /// <summary>Relationship value Performer</summary>
            public const string EVENT_PERFORMER = BaseNameTable.gNamespacePrefix + "event.performer";
            /// <summary>Relationship value Assigned To</summary>
            public const string TASK_ASSIGNED_TO = BaseNameTable.gNamespacePrefix + "task.assigned-to";
            /// <summary>Relationship value Message From</summary>
            public const string MESSAGE_FROM = BaseNameTable.gNamespacePrefix + "message.from";
            /// <summary>Relationship value message is a reply to</summary>
            public const string MESSAGE_REPLY_TO = BaseNameTable.gNamespacePrefix + "message.reply-to";
            /// <summary>Relationship value message goes to</summary>
            public const string MESSAGE_TO = BaseNameTable.gNamespacePrefix + "message.to";
            /// <summary>Relationship value message CC</summary>
            public const string MESSAGE_CC = BaseNameTable.gNamespacePrefix + "message.cc";
            /// <summary>Relationship value message BCC</summary>
            public const string MESSAGE_BCC = BaseNameTable.gNamespacePrefix + "message.bcc";
        }

        /// <summary>
        /// AttendeeType class
        /// </summary>
        public class AttendeeType : EnumConstruct
        {
            /// <summary>
            ///  default constructor
            /// </summary>
            public AttendeeType()
                : base(GDataParserNameTable.XmlAttendeeTypeElement)
            {
            }

            /// <summary>this attendee is required</summary>
            public const string EVENT_REQUIRED = BaseNameTable.gNamespacePrefix + "event.required";
            /// <summary>this attendee is optional</summary>
            public const string EVENT_OPTIONAL = BaseNameTable.gNamespacePrefix + "event.optional";

            /// <summary>
            /// the xml parsing method
            /// </summary>
            /// <param name="node">the xml node holding the attendeeStatus</param>
            /// <returns>AttendeeType</returns>

            public static AttendeeType parse(XmlNode node)
            {
                AttendeeType attendee = null;
                if (String.Compare(node.NamespaceURI, BaseNameTable.gNamespace, true, CultureInfo.InvariantCulture) == 0)
                {
                    attendee = new AttendeeType();
                    attendee.Value = Utilities.GetAttributeValue("value", node);
                }
                return attendee;
            }
        }

        /// <summary>
        /// represents the status of the attendee
        /// </summary>
        public class AttendeeStatus : EnumConstruct
        {
            /// <summary>
            /// default constructor
            /// </summary>
            public AttendeeStatus()
                : base(GDataParserNameTable.XmlAttendeeStatusElement)
            {
            }
            /// <summary>attendee was invited</summary>
            public const string EVENT_INVITED = BaseNameTable.gNamespacePrefix + "event.invited";
            /// <summary> attendee has accepted</summary>
            public const string EVENT_ACCEPTED = BaseNameTable.gNamespacePrefix + "event.accepted";
            /// <summary>attendee might or might not...</summary>
            public const string EVENT_TENTATIVE = BaseNameTable.gNamespacePrefix + "event.tentative";
            /// <summary>this attendee declined politely</summary>
            public const string EVENT_DECLINED = BaseNameTable.gNamespacePrefix + "event.declined";

            /// <summary>
            /// the xml parsing method
            /// </summary>
            /// <param name="node">the xml node holding the attendeeStatus</param>
            /// <returns>AttendeeStatus</returns>

            public static AttendeeStatus parse(XmlNode node)
            {
                AttendeeStatus attendee = null;
                if (String.Compare(node.NamespaceURI, BaseNameTable.gNamespace, true, CultureInfo.InvariantCulture) == 0)
                {
                    attendee = new AttendeeStatus();
                    attendee.Value = Utilities.GetAttributeValue("value", node);
                }
                return attendee;
            }
        }


        #region Attributes
        /// <summary>
        ///  relationship description as a String
        /// </summary>
        private string rel;

        /// <summary>
        /// String description of the person.
        /// </summary>
        private String valueString;

        /// <summary>
        /// email adress of the person
        /// </summary>
        private String email;

        /// <summary>
        ///  Type of event attendee.
        /// </summary>
        private AttendeeType attendeeType;

        /// <summary>
        ///  Status of event attendee.
        /// </summary>
        private AttendeeStatus attendeeStatus;

        /// <summary>
        /// Nested person entry.
        /// </summary>
        private EntryLink entryLink;

        #endregion

        #region Public Methods

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Rel</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public string Rel
        {
            get { return rel; }
            set { rel = value; }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string ValueString</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public String ValueString
        {
            get { return this.valueString; }
            set { this.valueString = value; }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method for the email property</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public String Email
        {
            get { return this.email; }
            set { this.email = value; }
        }

        /// <summary>
        ///  Attendee_Type accessor
        /// </summary>
        public AttendeeType Attendee_Type
        {
            get { return attendeeType; }
            set { attendeeType = value; }
        }

        /// <summary>
        ///  Attendee_Status accessor
        /// </summary>
        public AttendeeStatus Attendee_Status
        {
            get { return attendeeStatus; }
            set { attendeeStatus = value; }
        }

        /// <summary>
        ///  EntryLink Accessor
        /// </summary>
        public EntryLink EntryLink
        {
            get { return entryLink; }
            set { entryLink = value; }
        }

        #endregion

        #region overloaded from IExtensionElementFactory
        //////////////////////////////////////////////////////////////////////
        /// <summary>Parses an xml node to create a Where  object.</summary> 
        /// <param name="node">the node to parse node</param>
        /// <param name="parser">the xml parser to use if we need to dive deeper</param>
        /// <returns>the created Where  object</returns>
        //////////////////////////////////////////////////////////////////////
        public IExtensionElementFactory CreateInstance(XmlNode node, AtomFeedParser parser)
        {
            Tracing.TraceCall();
            Who who = null;

            if (node != null)
            {
                object localname = node.LocalName;
                if (!localname.Equals(this.XmlName) ||
                    !node.NamespaceURI.Equals(this.XmlNameSpace))
                {
                    return null;
                }
            }

            who = new Who();
            if (node != null)
            {
                if (node.Attributes != null)
                {
                    if (node.Attributes[GDataParserNameTable.XmlAttributeRel] != null)
                    {
                        who.Rel = node.Attributes[GDataParserNameTable.XmlAttributeRel].Value;
                    }
    
                    if (node.Attributes[GDataParserNameTable.XmlAttributeValueString] != null)
                    {
                        who.valueString = node.Attributes[GDataParserNameTable.XmlAttributeValueString].Value;
                    }
                    if (node.Attributes[GDataParserNameTable.XmlAttributeEmail] != null)
                    {
                        who.email = node.Attributes[GDataParserNameTable.XmlAttributeEmail].Value;
                    }
                }
    
                if (node.HasChildNodes)
                {
                    XmlNode childNode = node.FirstChild;
                    while (childNode != null)
                    {
						if (childNode is XmlElement)
						{
							if (childNode.LocalName == GDataParserNameTable.XmlAttendeeTypeElement)
							{
								who.Attendee_Type = AttendeeType.parse(childNode);
							}
							else if (childNode.LocalName == GDataParserNameTable.XmlAttendeeStatusElement)
							{
								who.Attendee_Status = AttendeeStatus.parse(childNode);
							}
							else if (childNode.LocalName == GDataParserNameTable.XmlEntryLinkElement)
							{
								who.EntryLink = EntryLink.ParseEntryLink(childNode, parser);
							}
						}
                        childNode = childNode.NextSibling;
                    }
                }
            }
            return who;
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>Returns the constant representing this XML element.</summary> 
        //////////////////////////////////////////////////////////////////////
        public string XmlName
        {
            get { return GDataParserNameTable.XmlWhoElement; }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>Returns the constant representing this XML element.</summary> 
        //////////////////////////////////////////////////////////////////////
        public string XmlNameSpace
        {
            get { return BaseNameTable.gNamespace; }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>Returns the constant representing this XML element.</summary> 
        //////////////////////////////////////////////////////////////////////
        public string XmlPrefix
        {
            get { return BaseNameTable.gDataPrefix; }
        }

        #endregion


        #region overloaded for persistence

        /// <summary>
        /// Persistence method for the Who object
        /// </summary>
        /// <param name="writer">the xmlwriter to write into</param>
        public void Save(XmlWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException("writer");

            if (Utilities.IsPersistable(this.Rel) ||
                Utilities.IsPersistable(this.valueString) ||
                Utilities.IsPersistable(this.email) ||
                this.attendeeType != null ||
                this.attendeeStatus != null ||
                this.entryLink != null)
            {
                
                writer.WriteStartElement(BaseNameTable.gDataPrefix, XmlName, BaseNameTable.gNamespace);
    
                if (Utilities.IsPersistable(this.Rel))
                {
                    writer.WriteAttributeString(GDataParserNameTable.XmlAttributeRel, this.Rel);
                }
                else
                {
                    throw new ClientFeedException("g:who/@rel is required.");
                }
    
                if (Utilities.IsPersistable(this.valueString))
                {
                    writer.WriteAttributeString(GDataParserNameTable.XmlAttributeValueString, this.valueString);
                }

                if (Utilities.IsPersistable(this.email))
                {
                    writer.WriteAttributeString(GDataParserNameTable.XmlAttributeEmail, this.email);
                }
    
                if (attendeeType != null)
                {
                    attendeeType.Save(writer);
                }
                if (attendeeStatus != null)
                {
                    attendeeStatus.Save(writer);
                }
                if (entryLink != null)
                {
                    entryLink.Save(writer);
                }
                writer.WriteEndElement();
            }
        }
        #endregion
    }
}

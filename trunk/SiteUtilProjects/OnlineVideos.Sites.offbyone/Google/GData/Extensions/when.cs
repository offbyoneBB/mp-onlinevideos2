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
using Google.GData.Client;
using System.Globalization;

namespace Google.GData.Extensions 
{

    /// <summary>
    /// GData schema extension describing a period of time.
    /// </summary>
    public class When : IExtensionElementFactory
    {

        /// <summary>
        /// Event start time (required).
        /// </summary>
        private DateTime startTime;

        /// <summary>
        /// Event end time (optional).
        /// </summary>
        private DateTime endTime;

        /// <summary>
        /// String description of the event times.
        /// </summary>
        private String valueString;

        /// <summary>
        /// flag, indicating if an all day status
        /// </summary>
        private bool fAllDay; 
        
        /// <summary>
        /// reminder object to set reminder durations
        /// </summary>
        private ExtensionCollection<Reminder> reminders;

        /// <summary>
        /// Constructs a new instance of a When object.
        /// </summary>
        public When() : base()
        {
        }

        /// <summary>
        /// Constructs a new instance of a When object with provided data.
        /// </summary>
        /// <param name="start">The beginning of the event.</param>
        /// <param name="end">The end of the event.</param>
        public When(DateTime start, DateTime end) : this()
        {
            this.StartTime = start;
            this.EndTime = end;
        }

        /// <summary>
        /// Constructs a new instance of a When object with provided data.
        /// </summary>
        /// <param name="start">The beginning of the event.</param>
        /// <param name="end">The end of the event.</param>
        /// <param name="allDay">A flag to indicate an all day event.</param>
        public When(DateTime start, DateTime end, bool allDay) : this(start, end)
        {
            this.AllDay = allDay;
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public DateTime StartTime</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public DateTime StartTime
        {
            get { return startTime; }
            set { startTime = value; }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public DateTime EndTime</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public DateTime EndTime
        {
            get { return endTime; }
            set { endTime = value; }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>reminder accessor</summary> 
        //////////////////////////////////////////////////////////////////////
        public ExtensionCollection<Reminder> Reminders
        {
            get 
            {
                if (this.reminders == null)
                {
                    this.reminders = new ExtensionCollection<Reminder>(null);
                }
                return this.reminders; 
            }
        }
        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string ValueString</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public String ValueString
        {
            get { return valueString; }
            set { valueString = value; }
        }


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method to the allday event flag</summary>
        /// <returns>true if it's an all day event</returns>
        //////////////////////////////////////////////////////////////////////
        public bool AllDay
        {
            get { return this.fAllDay; }
            set { this.fAllDay = value; }
        }

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
            When when = null;

            if (node != null)
            {
                object localname = node.LocalName;
                if (!localname.Equals(this.XmlName) ||
                    !node.NamespaceURI.Equals(this.XmlNameSpace))
                {
                    return null;
                }
            }

            bool startTimeFlag = false, endTimeFlag = false;

            when = new When();
            if (node != null)
            {
                if (node.Attributes != null)
                {
                    String value = node.Attributes[GDataParserNameTable.XmlAttributeStartTime] != null ? 
                        node.Attributes[GDataParserNameTable.XmlAttributeStartTime].Value : null; 
                    if (value != null)
                    {
                        startTimeFlag = true;
                        when.startTime = DateTime.Parse(value);
                        when.AllDay = (value.IndexOf('T') == -1); 
                    }
                
                    value = node.Attributes[GDataParserNameTable.XmlAttributeEndTime] != null ? 
                        node.Attributes[GDataParserNameTable.XmlAttributeEndTime].Value : null; 
                
                    if (value != null)
                    {
                        endTimeFlag = true;
                        when.endTime = DateTime.Parse(value); 
                        when.AllDay = when.AllDay && (value.IndexOf('T') == -1); 
                    }
                
                    if (node.Attributes[GDataParserNameTable.XmlAttributeValueString] != null)
                    {
                        when.valueString = node.Attributes[GDataParserNameTable.XmlAttributeValueString].Value;
                    }
                }
                // single event, g:reminder is inside g:when
                if (node.HasChildNodes)
                {
                    XmlNode whenChildNode = node.FirstChild;
                    IExtensionElementFactory f = new Reminder() as IExtensionElementFactory;
                    while (whenChildNode != null && whenChildNode is XmlElement)
                    {
                        if (String.Compare(whenChildNode.NamespaceURI, f.XmlNameSpace, true, CultureInfo.InvariantCulture) == 0)
                        {
                            if (String.Compare(whenChildNode.LocalName, f.XmlName, true, CultureInfo.InvariantCulture) == 0)
                            {
                                Reminder r = f.CreateInstance(whenChildNode, null) as Reminder;
                                when.Reminders.Add(r);
                            }
                        }
                        whenChildNode = whenChildNode.NextSibling;
                    }
                }
            }
            
            if (!startTimeFlag)
            {
                throw new ClientFeedException("g:when/@startTime is required.");
            }

            if (endTimeFlag && when.startTime.CompareTo(when.endTime) > 0)
            {
                throw new ClientFeedException("g:when/@startTime must be less than or equal to g:when/@endTime.");
            }

            return when;
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>Returns the constant representing this XML element.
        /// </summary> 
        //////////////////////////////////////////////////////////////////////
        public string XmlName
        {
            get { return GDataParserNameTable.XmlWhenElement; }
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
        /// Persistence method for the When object
        /// </summary>
        /// <param name="writer">the xmlwriter to write into</param>
        public void Save(XmlWriter writer)
        {

            if (Utilities.IsPersistable(this.valueString) ||
                Utilities.IsPersistable(this.startTime) ||
                Utilities.IsPersistable(this.endTime))

            {
                writer.WriteStartElement(BaseNameTable.gDataPrefix, XmlName, BaseNameTable.gNamespace);
                if (startTime != new DateTime(1, 1, 1))
                {

                    string date = this.fAllDay ? Utilities.LocalDateInUTC(this.startTime) 
                                                : Utilities.LocalDateTimeInUTC(this.startTime);
                    writer.WriteAttributeString(GDataParserNameTable.XmlAttributeStartTime, date);
                }
                else
                {
                    throw new ClientFeedException("g:when/@startTime is required.");
                }
    
                if (endTime != new DateTime(1, 1, 1))
                {
                    string date = this.fAllDay ? Utilities.LocalDateInUTC(this.endTime) 
                                                : Utilities.LocalDateTimeInUTC(this.endTime);
                    writer.WriteAttributeString(GDataParserNameTable.XmlAttributeEndTime, date);
                }
    
                if (Utilities.IsPersistable(this.valueString))
                {
                    writer.WriteAttributeString(GDataParserNameTable.XmlAttributeValueString, this.valueString);
                }
                if (this.reminders != null)
                {
                    foreach (Reminder r in this.Reminders)
                    {
                        r.Save(writer);
                    }
                }
                writer.WriteEndElement();
            }
        }
        #endregion
    }
}

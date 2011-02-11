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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Google.GData.Client;

namespace Google.GData.Extensions {

    /// <summary>
    /// GData schema extension describing a reminder on an event.
    /// </summary>
    /// <remarks>
    /// <para>You can represent a set of reminders where each has a (1) reminder
    /// period and (2) notification method.  The method can be either "sms",
    /// "email", "alert", "none", "all".</para>
    ///
    /// <para>The meaning of this set of reminders differs based on whether you
    /// are reading or writing feeds.  When reading, the set of reminders
    /// returned on an event takes into account both defaults on a
    /// parent recurring event (when applicable) as well as the user's
    /// defaults on calendar.  If there are no gd:reminders returned that
    /// means the event has absolutely no reminders.  "none" or "all" will
    /// not apply in this case.</para>
    ///
    /// <para>Writing is different because we have to be backwards-compatible
    /// (see *) with the old way of setting reminders.  For easier analysis
    /// we describe all the behaviors defined in the table below.  (Notice
    /// we only include cases for minutes, as the other cases specified in
    /// terms of days/hours/absoluteTime can be converted to this case.)</para>
    ///
    /// <para>Notice method is case-sensitive: must be in lowercase!</para>
    ///
    /// <list type="table">
    ///     <listheader>
    ///         <term></term>
    ///         <term>No method or method=all</term>
    ///         <term>method=none</term>
    ///         <term>method=email|sms|alert</term>
    ///     </listheader>
    ///     <item>
    ///         <term>No gd:rem</term>
    ///         <term>*No reminder</term>
    ///         <term>N/A</term>
    ///         <term>N/A</term>
    ///     </item>
    ///     <item>
    ///         <term>1 gd:rem</term>
    ///         <term>*Use user's default settings</term>
    ///         <term>No reminder</term>
    ///         <term>InvalidEntryException</term>
    ///     </item>
    ///     <item>
    ///         <term>1 gd:rem min=0</term>    
    ///         <term>*Use user's default settings</term>
    ///         <term>No reminder</term>
    ///         <term>InvalidEntryException</term>
    ///     </item>
    ///     <item>
    ///         <term>1 gd:rem min=-1</term>
    ///         <term>*No reminder</term>
    ///         <term>No reminder</term>
    ///         <term>InvalidEntryException</term>
    ///     </item>
    ///     <item>
    ///         <term>1 gd:rem min=+n</term>
    ///         <term>*Override with no +n for user's selected methods</term>
    ///         <term>No reminder</term>
    ///         <term>Set exactly one reminder on event at +n with given method</term>
    ///     </item>
    ///     <item>
    ///         <term>Multiple gd:rem</term>
    ///         <term>InvalidEntryException</term>
    ///         <term>InvalidEntryException</term>
    ///         <term>Copy this set exactly</term>
    ///     </item>
    /// </list>
    /// 
    /// <para>Hence, to override an event with a set of reminder time, method
    /// pairs, just specify them exactly.  To clear an event of all
    /// overrides (and go back to inheriting the user's defaults), one can
    /// simply specify a single gd:reminder with no extra attributes.  To
    /// have NO event reminders on an event, either set a single
    /// gd:reminder with negative reminder time, or simply update the event
    /// with a single gd:reminder method=none.</para>
    /// </remarks>
    public class Reminder : IExtensionElementFactory
    {
        /// <summary>
        /// the different reminder methods available
        /// </summary>
        public enum ReminderMethod {
            /// <summary>
            /// visible alert
            /// </summary>
                alert,
            /// <summary>
            /// all alerts
            /// </summary>
                all,
            /// <summary>
            /// alert per email
            /// </summary>
                email,
            /// <summary>
            /// no aert
            /// </summary>
                none,
            /// <summary>
            /// alert per SMS
            /// </summary>
                sms,
            /// <summary>
            /// no alert specified (invalid)
            /// </summary>
                unspecified
         };

        /// <summary>
        /// Number of days before the event.
        /// </summary>
        private int days;

        /// <summary>
        /// Number of hours.
        /// </summary>
        private int hours;

        /// <summary>
        /// Number of minutes.
        /// </summary>
        private int minutes;

        /// <summary>
        /// Absolute time of the reminder.
        /// </summary>
        private DateTime absoluteTime;

        /// <summary>
        /// holds the method type
        /// </summary>
        private ReminderMethod method;

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public Method Method</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public ReminderMethod Method
        {
            get {return this.method;}
            set {this.method = value;}
        }
        // end of accessor public Method Method

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public Days</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public int Days
        {
            get { return days;}
            set { days = value;}
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public Hours</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public int Hours
        {
            get { return hours;}
            set { hours = value;}
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public Minutes</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public int Minutes
        {
            get { return minutes;}
            set { minutes = value;}
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public absoluteTime</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public DateTime AbsoluteTime
        {
            get { return absoluteTime;}
            set { absoluteTime = value;}
        }


        #region overloaded from IExtensionElementFactory
        //////////////////////////////////////////////////////////////////////
        /// <summary>Parses an xml node to create a Reminder object.</summary> 
        /// <param name="node">the node to parse node</param>
        /// <param name="parser">the xml parser to use if we need to dive deeper</param>
        /// <returns>the created Reminder object</returns>
        //////////////////////////////////////////////////////////////////////
        public IExtensionElementFactory CreateInstance(XmlNode node, AtomFeedParser parser)
        {
            Tracing.TraceCall();
            Reminder reminder = null;
          
            if (node != null)
            {
                object localname = node.LocalName;
                if (!localname.Equals(this.XmlName) ||
                    !node.NamespaceURI.Equals(this.XmlNameSpace))
                {
                    return null;
                }
            }

            reminder = new Reminder();
            if (node != null && node.Attributes != null)
            {
                if (node.Attributes[GDataParserNameTable.XmlAttributeAbsoluteTime] != null)
                {
                    try
                    {
                        reminder.AbsoluteTime =
                        DateTime.Parse(node.Attributes[GDataParserNameTable.XmlAttributeAbsoluteTime].Value);
                    }
                    catch (FormatException fe)
                    {
                        throw new ArgumentException("Invalid g:reminder/@absoluteTime.", fe);
                    }
                }

                if (node.Attributes[GDataParserNameTable.XmlAttributeDays] != null)
                {
                    try
                    {
                        reminder.Days = Int32.Parse(node.Attributes[GDataParserNameTable.XmlAttributeDays].Value);
                    }
                    catch (FormatException fe)
                    {
                        throw new ArgumentException("Invalid g:reminder/@days.", fe);
                    }
                }

                if (node.Attributes[GDataParserNameTable.XmlAttributeHours] != null)
                {
                    try
                    {
                        reminder.Hours = Int32.Parse(node.Attributes[GDataParserNameTable.XmlAttributeHours].Value);
                    }
                    catch (FormatException fe)
                    {
                        throw new ArgumentException("Invalid g:reminder/@hours.", fe);
                    }
                }

                if (node.Attributes[GDataParserNameTable.XmlAttributeMinutes] != null)
                {
                    try
                    {
                        reminder.Minutes = Int32.Parse(node.Attributes[GDataParserNameTable.XmlAttributeMinutes].Value);
                    }
                    catch (FormatException fe)
                    {
                        throw new ArgumentException("Invalid g:reminder/@minutes.", fe);
                    }
                }

                if (node.Attributes[GDataParserNameTable.XmlAttributeMethod] != null)
                {
                    try
                    {
                        reminder.Method = (ReminderMethod)Enum.Parse(typeof(ReminderMethod), 
                                                                     node.Attributes[GDataParserNameTable.XmlAttributeMethod].Value,
                                                                     true);
                    }
                    catch (Exception e)
                    {
                        throw new ArgumentException("Invalid g:reminder/@method.", e);
                    }
                }
            }
            return reminder;
        }
        //////////////////////////////////////////////////////////////////////
        /// <summary>Returns the constant representing this XML element.</summary> 
        //////////////////////////////////////////////////////////////////////
        public string XmlName
        {
            get { return GDataParserNameTable.XmlReminderElement;}
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

        /// <summary>
        /// Persistence method for the Reminder  object
        /// </summary>
        /// <param name="writer">the xmlwriter to write into</param>
        public void Save(XmlWriter writer)
        {
            writer.WriteStartElement(XmlPrefix, XmlName, XmlNameSpace);

            if (Days > 0)
            {
                writer.WriteAttributeString(GDataParserNameTable.XmlAttributeDays, this.Days.ToString());
            }

            if (Hours > 0)
            {
                writer.WriteAttributeString(GDataParserNameTable.XmlAttributeHours, this.Hours.ToString());
            }

            if (Minutes > 0)
            {
                writer.WriteAttributeString(GDataParserNameTable.XmlAttributeMinutes, this.Minutes.ToString());
            }

            if (AbsoluteTime != new DateTime(1, 1, 1))
            {
                string date = Utilities.LocalDateTimeInUTC(AbsoluteTime);
                writer.WriteAttributeString(GDataParserNameTable.XmlAttributeAbsoluteTime, date);
            }

            if (this.Method != ReminderMethod.unspecified)
            {
                writer.WriteAttributeString(GDataParserNameTable.XmlAttributeMethod, this.Method.ToString());
            }
            writer.WriteEndElement();
        }
    }
}





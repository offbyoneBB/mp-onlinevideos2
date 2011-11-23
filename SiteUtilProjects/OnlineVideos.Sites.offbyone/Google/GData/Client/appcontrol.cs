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
using Google.GData.Client;
using System.Globalization;

namespace Google.GData.Extensions.AppControl {

    /// <summary>
    /// app:control schema extension
    /// </summary>
    public class AppControl : SimpleContainer {
        /// <summary>
        /// default constructor for app:control
        /// </summary>
        public AppControl()
            : this(BaseNameTable.AppPublishingNamespace(null)) {
        }

        /// <summary>
        /// app:control constructor with namespace as parameter
        /// </summary>
        public AppControl(string ns) :
            base(BaseNameTable.XmlElementPubControl,
            BaseNameTable.gAppPublishingPrefix,
            ns) {
            this.ExtensionFactories.Add(new AppDraft());
        }

        /// <summary>
        /// returns the app:draft element
        /// </summary>
        public AppDraft Draft {
            get {
                return FindExtension(BaseNameTable.XmlElementPubDraft,
                    BaseNameTable.AppPublishingNamespace(this)) as AppDraft;
            }
            set {
                ReplaceExtension(BaseNameTable.XmlElementPubDraft,
                    BaseNameTable.AppPublishingNamespace(this),
                    value);
            }
        }

        /// <summary>
        /// need so setup the namespace based on the version information     
        /// </summary>
        protected override void VersionInfoChanged() {
            base.VersionInfoChanged();
            this.SetXmlNamespace(BaseNameTable.AppPublishingNamespace(this));
        }
    }

    /// <summary>
    /// app:draft schema extension describing that an entry is in draft mode
    /// it's a child of app:control
    /// </summary>
    public class AppDraft : SimpleElement {
        /// <summary>
        /// default constructor for app:draft
        /// </summary>
        public AppDraft()
            : base(BaseNameTable.XmlElementPubDraft,
            BaseNameTable.gAppPublishingPrefix,
            BaseNameTable.AppPublishingNamespace(null)) { }

        /// <summary>
        /// default constructor for app:draft
        /// </summary>
        public AppDraft(bool isDraft)
            : base(BaseNameTable.XmlElementPubDraft,
            BaseNameTable.gAppPublishingPrefix,
            BaseNameTable.AppPublishingNamespace(null),
            isDraft ? "yes" : "no") { }

        /// <summary>
        ///  Accessor Method for the value as integer
        /// </summary>
        public override bool BooleanValue {
            get { return this.Value == "yes" ? true : false; }
            set { this.Value = value ? "yes" : "no"; }
        }

        /// <summary>
        /// need so setup the namespace based on the version information
        /// changes
        /// </summary>
        protected override void VersionInfoChanged() {
            base.VersionInfoChanged();
            this.SetXmlNamespace(BaseNameTable.AppPublishingNamespace(this));
        }
    }

    /// <summary>
    /// The "app:edited" element is a Date construct (as defined by
    /// [RFC4287]), whose content indicates the last time an Entry was
    /// edited.  If the entry has not been edited yet, the content indicates
    /// the time it was created.  Atom Entry elements in Collection Documents
    /// SHOULD contain one app:edited element, and MUST NOT contain more than
    /// one.
    /// The server SHOULD change the value of this element every time an
    /// Entry Resource or an associated Media Resource has been edited
    /// </summary>
    public class AppEdited : SimpleElement {
        /// <summary>
        /// creates a default app:edited element
        /// </summary>
        public AppEdited()
            : base(BaseNameTable.XmlElementPubEdited,
            BaseNameTable.gAppPublishingPrefix,
            BaseNameTable.NSAppPublishingFinal) { }

        /// <summary>
        /// creates a default app:edited element with the given datetime value
        /// </summary>
        public AppEdited(DateTime dateValue)
            : base(BaseNameTable.XmlElementPubEdited,
            BaseNameTable.gAppPublishingPrefix,
            BaseNameTable.NSAppPublishingFinal) {
            this.Value = Utilities.LocalDateTimeInUTC(dateValue);
        }

        /// <summary>
        /// creates an app:edited element with the string as it's
        /// default value. The string has to conform to RFC4287
        /// </summary>
        /// <param name="dateInUtc"></param>
        public AppEdited(string dateInUtc)
            : base(BaseNameTable.XmlElementPubEdited,
            BaseNameTable.gAppPublishingPrefix,
            BaseNameTable.NSAppPublishingFinal,
            dateInUtc) {
        }

        /// <summary>
        ///  Accessor Method for the value as a DateTime
        /// </summary>
        public DateTime DateValue {
            get {
                return DateTime.Parse(this.Value, CultureInfo.InvariantCulture);
            }
            set {
                this.Value = Utilities.LocalDateTimeInUTC(value);
            }
        }
    }
}

namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, DebuggerStepThrough, GeneratedCode("xsd", "2.0.50727.3038"), DesignerCategory("code"), XmlType(AnonymousType=true)]
    public class mainnavigationMainnaviitem
    {
        private bool delimiterField;
        private bool delimiterFieldSpecified;
        private string eventIdField;
        private string eventNameField;
        private bool isStartPageField;
        private mainnavigationMainnaviitemKey keyField;
        private bool keyFieldSpecified;
        private int ttlField;
        private bool ttlFieldSpecified;
        private string valueField;

        [XmlAttribute]
        public bool delimiter
        {
            get
            {
                return this.delimiterField;
            }
            set
            {
                this.delimiterField = value;
            }
        }

        [XmlIgnore]
        public bool delimiterSpecified
        {
            get
            {
                return this.delimiterFieldSpecified;
            }
            set
            {
                this.delimiterFieldSpecified = value;
            }
        }

        [XmlAttribute]
        public string eventId
        {
            get
            {
                return this.eventIdField;
            }
            set
            {
                this.eventIdField = value;
            }
        }

        [XmlAttribute]
        public string eventName
        {
            get
            {
                return this.eventNameField;
            }
            set
            {
                this.eventNameField = value;
            }
        }

        [XmlAttribute]
        public bool isStartPage
        {
            get
            {
                return this.isStartPageField;
            }
            set
            {
                this.isStartPageField = value;
            }
        }

        [XmlAttribute]
        public mainnavigationMainnaviitemKey key
        {
            get
            {
                return this.keyField;
            }
            set
            {
                this.keyField = value;
            }
        }

        [XmlIgnore]
        public bool keySpecified
        {
            get
            {
                return this.keyFieldSpecified;
            }
            set
            {
                this.keyFieldSpecified = value;
            }
        }

        [XmlAttribute]
        public int ttl
        {
            get
            {
                return this.ttlField;
            }
            set
            {
                this.ttlField = value;
            }
        }

        [XmlIgnore]
        public bool ttlSpecified
        {
            get
            {
                return this.ttlFieldSpecified;
            }
            set
            {
                this.ttlFieldSpecified = value;
            }
        }

        [XmlText]
        public string Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }
}


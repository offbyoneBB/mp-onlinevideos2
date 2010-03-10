namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, XmlType(AnonymousType=true), GeneratedCode("xsd", "2.0.50727.3038"), DebuggerStepThrough, DesignerCategory("code")]
    public class imageseriesImage
    {
        private string copyrightField;
        private string descriptionField;
        private string displayDurationField;
        private string originDetailsField;
        private string originField;
        private string urlField;

        public string copyright
        {
            get
            {
                return this.copyrightField;
            }
            set
            {
                this.copyrightField = value;
            }
        }

        public string description
        {
            get
            {
                return this.descriptionField;
            }
            set
            {
                this.descriptionField = value;
            }
        }

        public string displayDuration
        {
            get
            {
                return this.displayDurationField;
            }
            set
            {
                this.displayDurationField = value;
            }
        }

        public string origin
        {
            get
            {
                return this.originField;
            }
            set
            {
                this.originField = value;
            }
        }

        public string originDetails
        {
            get
            {
                return this.originDetailsField;
            }
            set
            {
                this.originDetailsField = value;
            }
        }

        [XmlElement(DataType="anyURI")]
        public string url
        {
            get
            {
                return this.urlField;
            }
            set
            {
                this.urlField = value;
            }
        }
    }
}


namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, DebuggerStepThrough, GeneratedCode("xsd", "2.0.50727.3038"), XmlRoot(Namespace="", IsNullable=false), DesignerCategory("code"), XmlType(AnonymousType=true)]
    public class teaserlist
    {
        private bool additionalTeaserField;
        private bool additionalTeaserFieldSpecified;
        private int batchField;
        private bool batchFieldSpecified;
        private teaser[] teasersField;

        public bool additionalTeaser
        {
            get
            {
                return this.additionalTeaserField;
            }
            set
            {
                this.additionalTeaserField = value;
            }
        }

        [XmlIgnore]
        public bool additionalTeaserSpecified
        {
            get
            {
                return this.additionalTeaserFieldSpecified;
            }
            set
            {
                this.additionalTeaserFieldSpecified = value;
            }
        }

        public int batch
        {
            get
            {
                return this.batchField;
            }
            set
            {
                this.batchField = value;
            }
        }

        [XmlIgnore]
        public bool batchSpecified
        {
            get
            {
                return this.batchFieldSpecified;
            }
            set
            {
                this.batchFieldSpecified = value;
            }
        }

        [XmlArrayItem("teaser", IsNullable=false)]
        public teaser[] teasers
        {
            get
            {
                return this.teasersField;
            }
            set
            {
                this.teasersField = value;
            }
        }
    }
}


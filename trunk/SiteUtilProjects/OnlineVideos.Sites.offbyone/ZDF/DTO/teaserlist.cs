namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, GeneratedCode("xsd", "2.0.50727.3038"), XmlRoot(Namespace="", IsNullable=false), DebuggerStepThrough, DesignerCategory("code"), XmlType(AnonymousType=true)]
    public class teaserlist
    {
        private bool additionalTeaserField;
        private bool additionalTeaserFieldSpecified;
        private int batchField;
        private bool batchFieldSpecified;
        private ZDFMediathek2009.Code.DTO.information informationField;
        private teaserlistSearchResult searchResultField;
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

        public ZDFMediathek2009.Code.DTO.information information
        {
            get
            {
                return this.informationField;
            }
            set
            {
                this.informationField = value;
            }
        }

        public teaserlistSearchResult searchResult
        {
            get
            {
                return this.searchResultField;
            }
            set
            {
                this.searchResultField = value;
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


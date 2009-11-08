namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, DebuggerStepThrough, XmlType(AnonymousType=true), XmlRoot(Namespace="", IsNullable=false), DesignerCategory("code"), GeneratedCode("xsd", "2.0.50727.3038")]
    public class teaser
    {
        private ZDFMediathek2009.Code.DTO.context contextField;
        private ZDFMediathek2009.Code.DTO.details detailsField;
        private bool hasSkinField;
        private bool hasSkinFieldSpecified;
        private ZDFMediathek2009.Code.DTO.information informationField;
        private teaserMember memberField;
        private bool memberFieldSpecified;
        private value[] miscField;
        private podcastUrl[] podcastField;
        private teaserimagesTeaserimage[] teaserimagesField;
        private ZDFMediathek2009.Code.DTO.type typeField;

        public ZDFMediathek2009.Code.DTO.context context
        {
            get
            {
                return this.contextField;
            }
            set
            {
                this.contextField = value;
            }
        }

        public ZDFMediathek2009.Code.DTO.details details
        {
            get
            {
                return this.detailsField;
            }
            set
            {
                this.detailsField = value;
            }
        }

        [XmlAttribute]
        public bool hasSkin
        {
            get
            {
                return this.hasSkinField;
            }
            set
            {
                this.hasSkinField = value;
            }
        }

        [XmlIgnore]
        public bool hasSkinSpecified
        {
            get
            {
                return this.hasSkinFieldSpecified;
            }
            set
            {
                this.hasSkinFieldSpecified = value;
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

        [XmlAttribute]
        public teaserMember member
        {
            get
            {
                return this.memberField;
            }
            set
            {
                this.memberField = value;
            }
        }

        [XmlIgnore]
        public bool memberSpecified
        {
            get
            {
                return this.memberFieldSpecified;
            }
            set
            {
                this.memberFieldSpecified = value;
            }
        }

        [XmlArrayItem("value", IsNullable=false)]
        public value[] misc
        {
            get
            {
                return this.miscField;
            }
            set
            {
                this.miscField = value;
            }
        }

        [XmlArrayItem("url", IsNullable=false)]
        public podcastUrl[] podcast
        {
            get
            {
                return this.podcastField;
            }
            set
            {
                this.podcastField = value;
            }
        }

        [XmlArrayItem("teaserimage", IsNullable=false)]
        public teaserimagesTeaserimage[] teaserimages
        {
            get
            {
                return this.teaserimagesField;
            }
            set
            {
                this.teaserimagesField = value;
            }
        }

        public ZDFMediathek2009.Code.DTO.type type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }
    }
}


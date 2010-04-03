namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, XmlType(AnonymousType=true), DesignerCategory("code"), GeneratedCode("xsd", "2.0.50727.3038"), XmlRoot(Namespace="", IsNullable=false), DebuggerStepThrough]
    public class interactive
    {
        private string bgColorField;
        private ZDFMediathek2009.Code.DTO.context contextField;
        private string copyrightField;
        private ZDFMediathek2009.Code.DTO.details detailsField;
        private bool hasSkinField;
        private bool hasSkinFieldSpecified;
        private uint heightField;
        private bool heightFieldSpecified;
        private ZDFMediathek2009.Code.DTO.information informationField;
        private teaserMember memberField;
        private bool memberFieldSpecified;
        private value[] miscField;
        private podcastUrl[] podcastField;
        private bool socialBookmarksField;
        private bool socialBookmarksFieldSpecified;
        private string swfURLField;
        private teaserimagesTeaserimage[] teaserimagesField;
        private ZDFMediathek2009.Code.DTO.type typeField;
        private uint widthField;
        private bool widthFieldSpecified;

        public string bgColor
        {
            get
            {
                return this.bgColorField;
            }
            set
            {
                this.bgColorField = value;
            }
        }

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

        public uint height
        {
            get
            {
                return this.heightField;
            }
            set
            {
                this.heightField = value;
            }
        }

        [XmlIgnore]
        public bool heightSpecified
        {
            get
            {
                return this.heightFieldSpecified;
            }
            set
            {
                this.heightFieldSpecified = value;
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

        public bool socialBookmarks
        {
            get
            {
                return this.socialBookmarksField;
            }
            set
            {
                this.socialBookmarksField = value;
            }
        }

        [XmlIgnore]
        public bool socialBookmarksSpecified
        {
            get
            {
                return this.socialBookmarksFieldSpecified;
            }
            set
            {
                this.socialBookmarksFieldSpecified = value;
            }
        }

        [XmlElement(DataType="anyURI")]
        public string swfURL
        {
            get
            {
                return this.swfURLField;
            }
            set
            {
                this.swfURLField = value;
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

        public uint width
        {
            get
            {
                return this.widthField;
            }
            set
            {
                this.widthField = value;
            }
        }

        [XmlIgnore]
        public bool widthSpecified
        {
            get
            {
                return this.widthFieldSpecified;
            }
            set
            {
                this.widthFieldSpecified = value;
            }
        }
    }
}


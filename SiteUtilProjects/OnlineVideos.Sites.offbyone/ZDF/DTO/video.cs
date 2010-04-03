namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, DebuggerStepThrough, XmlRoot(Namespace="", IsNullable=false), GeneratedCode("xsd", "2.0.50727.3038"), DesignerCategory("code"), XmlType(AnonymousType=true)]
    public class video
    {
        private videoCaption captionField;
        private ZDFMediathek2009.Code.DTO.context contextField;
        private string copyrightField;
        private value[] cuepointsField;
        private ZDFMediathek2009.Code.DTO.details detailsField;
        private bool embedField;
        private bool embedFieldSpecified;
        private videoFormitaet[] formitaetenField;
        private bool hasSkinField;
        private bool hasSkinFieldSpecified;
        private ZDFMediathek2009.Code.DTO.information informationField;
        private videoLiveChat liveChatField;
        private teaserMember memberField;
        private bool memberFieldSpecified;
        private value[] miscField;
        private videoMoreInfos moreInfosField;
        private podcastUrl[] podcastField;
        private bool socialBookmarksField;
        private bool socialBookmarksFieldSpecified;
        private teaserimagesTeaserimage[] teaserimagesField;
        private ZDFMediathek2009.Code.DTO.type typeField;

        public videoCaption caption
        {
            get
            {
                return this.captionField;
            }
            set
            {
                this.captionField = value;
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

        [XmlArrayItem("value", IsNullable=false)]
        public value[] cuepoints
        {
            get
            {
                return this.cuepointsField;
            }
            set
            {
                this.cuepointsField = value;
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

        public bool embed
        {
            get
            {
                return this.embedField;
            }
            set
            {
                this.embedField = value;
            }
        }

        [XmlIgnore]
        public bool embedSpecified
        {
            get
            {
                return this.embedFieldSpecified;
            }
            set
            {
                this.embedFieldSpecified = value;
            }
        }

        [XmlArrayItem("formitaet", IsNullable=false)]
        public videoFormitaet[] formitaeten
        {
            get
            {
                return this.formitaetenField;
            }
            set
            {
                this.formitaetenField = value;
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

        public videoLiveChat liveChat
        {
            get
            {
                return this.liveChatField;
            }
            set
            {
                this.liveChatField = value;
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

        public videoMoreInfos moreInfos
        {
            get
            {
                return this.moreInfosField;
            }
            set
            {
                this.moreInfosField = value;
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


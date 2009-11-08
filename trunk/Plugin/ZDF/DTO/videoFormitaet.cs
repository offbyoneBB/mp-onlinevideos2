namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, DebuggerStepThrough, XmlType(AnonymousType=true), GeneratedCode("xsd", "2.0.50727.3038"), DesignerCategory("code")]
    public class videoFormitaet
    {
        private uint audioBitrateField;
        private bool audioBitrateFieldSpecified;
        private string basetypeField;
        private uint bruttoBitrateField;
        private bool bruttoBitrateFieldSpecified;
        private string[] facetsField;
        private ulong filesizeField;
        private bool filesizeFieldSpecified;
        private uint heightField;
        private bool heightFieldSpecified;
        private bool isDownloadField;
        private videoFormitaetQuality qualityField;
        private string ratioField;
        private string urlField;
        private uint videoBitrateField;
        private bool videoBitrateFieldSpecified;
        private uint widthField;
        private bool widthFieldSpecified;

        public uint audioBitrate
        {
            get
            {
                return this.audioBitrateField;
            }
            set
            {
                this.audioBitrateField = value;
            }
        }

        [XmlIgnore]
        public bool audioBitrateSpecified
        {
            get
            {
                return this.audioBitrateFieldSpecified;
            }
            set
            {
                this.audioBitrateFieldSpecified = value;
            }
        }

        [XmlAttribute]
        public string basetype
        {
            get
            {
                return this.basetypeField;
            }
            set
            {
                this.basetypeField = value;
            }
        }

        public uint bruttoBitrate
        {
            get
            {
                return this.bruttoBitrateField;
            }
            set
            {
                this.bruttoBitrateField = value;
            }
        }

        [XmlIgnore]
        public bool bruttoBitrateSpecified
        {
            get
            {
                return this.bruttoBitrateFieldSpecified;
            }
            set
            {
                this.bruttoBitrateFieldSpecified = value;
            }
        }

        [XmlArrayItem("facet", IsNullable=false)]
        public string[] facets
        {
            get
            {
                return this.facetsField;
            }
            set
            {
                this.facetsField = value;
            }
        }

        public ulong filesize
        {
            get
            {
                return this.filesizeField;
            }
            set
            {
                this.filesizeField = value;
            }
        }

        [XmlIgnore]
        public bool filesizeSpecified
        {
            get
            {
                return this.filesizeFieldSpecified;
            }
            set
            {
                this.filesizeFieldSpecified = value;
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

        [XmlAttribute]
        public bool isDownload
        {
            get
            {
                return this.isDownloadField;
            }
            set
            {
                this.isDownloadField = value;
            }
        }

        public videoFormitaetQuality quality
        {
            get
            {
                return this.qualityField;
            }
            set
            {
                this.qualityField = value;
            }
        }

        public string ratio
        {
            get
            {
                return this.ratioField;
            }
            set
            {
                this.ratioField = value;
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

        public uint videoBitrate
        {
            get
            {
                return this.videoBitrateField;
            }
            set
            {
                this.videoBitrateField = value;
            }
        }

        [XmlIgnore]
        public bool videoBitrateSpecified
        {
            get
            {
                return this.videoBitrateFieldSpecified;
            }
            set
            {
                this.videoBitrateFieldSpecified = value;
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


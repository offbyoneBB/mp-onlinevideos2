namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, DesignerCategory("code"), GeneratedCode("xsd", "2.0.50727.3038"), XmlRoot(Namespace="", IsNullable=false), DebuggerStepThrough, XmlType(AnonymousType=true)]
    public class details
    {
        private string airtimeEndField;
        private string airtimeField;
        private string assetIdField;
        private string categoriesField;
        private string channelField;
        private detailsFsk fskField;
        private bool fskFieldSpecified;
        private bool hasCaptionField;
        private bool hasCaptionFieldSpecified;
        private bool hasDownloadField;
        private bool hasDownloadFieldSpecified;
        private bool hasHDField;
        private bool hasHDFieldSpecified;
        private bool hasPodcastField;
        private bool hasPodcastFieldSpecified;
        private bool hasRSSField;
        private bool hasRSSFieldSpecified;
        private string lengthField;
        private string originChannelIdField;
        private string originChannelTitleField;
        private string otherChannelsField;
        private string timetoliveField;
        private bool tippField;
        private bool tippFieldSpecified;
        private string vcmsUrlField;

        public string airtime
        {
            get
            {
                return this.airtimeField;
            }
            set
            {
                this.airtimeField = value;
            }
        }

        public string airtimeEnd
        {
            get
            {
                return this.airtimeEndField;
            }
            set
            {
                this.airtimeEndField = value;
            }
        }

        public string assetId
        {
            get
            {
                return this.assetIdField;
            }
            set
            {
                this.assetIdField = value;
            }
        }

        public string categories
        {
            get
            {
                return this.categoriesField;
            }
            set
            {
                this.categoriesField = value;
            }
        }

        public string channel
        {
            get
            {
                return this.channelField;
            }
            set
            {
                this.channelField = value;
            }
        }

        public detailsFsk fsk
        {
            get
            {
                return this.fskField;
            }
            set
            {
                this.fskField = value;
            }
        }

        [XmlIgnore]
        public bool fskSpecified
        {
            get
            {
                return this.fskFieldSpecified;
            }
            set
            {
                this.fskFieldSpecified = value;
            }
        }

        public bool hasCaption
        {
            get
            {
                return this.hasCaptionField;
            }
            set
            {
                this.hasCaptionField = value;
            }
        }

        [XmlIgnore]
        public bool hasCaptionSpecified
        {
            get
            {
                return this.hasCaptionFieldSpecified;
            }
            set
            {
                this.hasCaptionFieldSpecified = value;
            }
        }

        public bool hasDownload
        {
            get
            {
                return this.hasDownloadField;
            }
            set
            {
                this.hasDownloadField = value;
            }
        }

        [XmlIgnore]
        public bool hasDownloadSpecified
        {
            get
            {
                return this.hasDownloadFieldSpecified;
            }
            set
            {
                this.hasDownloadFieldSpecified = value;
            }
        }

        public bool hasHD
        {
            get
            {
                return this.hasHDField;
            }
            set
            {
                this.hasHDField = value;
            }
        }

        [XmlIgnore]
        public bool hasHDSpecified
        {
            get
            {
                return this.hasHDFieldSpecified;
            }
            set
            {
                this.hasHDFieldSpecified = value;
            }
        }

        public bool hasPodcast
        {
            get
            {
                return this.hasPodcastField;
            }
            set
            {
                this.hasPodcastField = value;
            }
        }

        [XmlIgnore]
        public bool hasPodcastSpecified
        {
            get
            {
                return this.hasPodcastFieldSpecified;
            }
            set
            {
                this.hasPodcastFieldSpecified = value;
            }
        }

        public bool hasRSS
        {
            get
            {
                return this.hasRSSField;
            }
            set
            {
                this.hasRSSField = value;
            }
        }

        [XmlIgnore]
        public bool hasRSSSpecified
        {
            get
            {
                return this.hasRSSFieldSpecified;
            }
            set
            {
                this.hasRSSFieldSpecified = value;
            }
        }

        public string length
        {
            get
            {
                return this.lengthField;
            }
            set
            {
                this.lengthField = value;
            }
        }

        public string originChannelId
        {
            get
            {
                return this.originChannelIdField;
            }
            set
            {
                this.originChannelIdField = value;
            }
        }

        public string originChannelTitle
        {
            get
            {
                return this.originChannelTitleField;
            }
            set
            {
                this.originChannelTitleField = value;
            }
        }

        public string otherChannels
        {
            get
            {
                return this.otherChannelsField;
            }
            set
            {
                this.otherChannelsField = value;
            }
        }

        public string timetolive
        {
            get
            {
                return this.timetoliveField;
            }
            set
            {
                this.timetoliveField = value;
            }
        }

        public bool tipp
        {
            get
            {
                return this.tippField;
            }
            set
            {
                this.tippField = value;
            }
        }

        [XmlIgnore]
        public bool tippSpecified
        {
            get
            {
                return this.tippFieldSpecified;
            }
            set
            {
                this.tippFieldSpecified = value;
            }
        }

        public string vcmsUrl
        {
            get
            {
                return this.vcmsUrlField;
            }
            set
            {
                this.vcmsUrlField = value;
            }
        }
    }
}


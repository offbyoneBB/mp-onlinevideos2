namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, XmlType(AnonymousType=true), DesignerCategory("code"), GeneratedCode("xsd", "2.0.50727.3038"), XmlRoot(Namespace="", IsNullable=false), DebuggerStepThrough]
    public class response
    {
        private ItemChoiceType itemElementNameField;
        private object itemField;
        private ZDFMediathek2009.Code.DTO.status statusField;

        [XmlElement("mceUpdate", typeof(int)), XmlElement("suggestlist", typeof(suggestlist)), XmlElement("channellist", typeof(channellist)), XmlElement("imageseries", typeof(imageseries)), XmlElement("interactive", typeof(interactive)), XmlElement("ivwUrls", typeof(ivwUrls)), XmlElement("mainnavigation", typeof(mainnavigation)), XmlElement("page", typeof(page)), XmlElement("session", typeof(string)), XmlElement("skindetails", typeof(skindetails)), XmlElement("teaserlist", typeof(teaserlist)), XmlElement("video", typeof(video)), XmlElement("feedlist", typeof(feedlist)), XmlChoiceIdentifier("ItemElementName"), XmlElement("configuration", typeof(configuration))]
        public object Item
        {
            get
            {
                return this.itemField;
            }
            set
            {
                this.itemField = value;
            }
        }

        [XmlIgnore]
        public ItemChoiceType ItemElementName
        {
            get
            {
                return this.itemElementNameField;
            }
            set
            {
                this.itemElementNameField = value;
            }
        }

        public ZDFMediathek2009.Code.DTO.status status
        {
            get
            {
                return this.statusField;
            }
            set
            {
                this.statusField = value;
            }
        }
    }
}


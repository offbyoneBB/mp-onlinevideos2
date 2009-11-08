namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, XmlType(AnonymousType=true), XmlRoot(Namespace="", IsNullable=false), GeneratedCode("xsd", "2.0.50727.3038"), DesignerCategory("code"), DebuggerStepThrough]
    public class response
    {
        private ItemChoiceType itemElementNameField;
        private object itemField;
        private ZDFMediathek2009.Code.DTO.status statusField;

        [XmlElement("imageseries", typeof(imageseries)), XmlElement("suggestlist", typeof(suggestlist)), XmlElement("feedlist", typeof(feedlist)), XmlElement("ivwUrls", typeof(ivwUrls)), XmlElement("mainnavigation", typeof(mainnavigation)), XmlElement("interactive", typeof(interactive)), XmlElement("page", typeof(page)), XmlElement("session", typeof(string)), XmlElement("skindetails", typeof(skindetails)), XmlElement("configuration", typeof(configuration)), XmlElement("teaserlist", typeof(teaserlist)), XmlElement("mceUpdate", typeof(int)), XmlChoiceIdentifier("ItemElementName"), XmlElement("video", typeof(video)), XmlElement("channellist", typeof(channellist))]
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


namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, DebuggerStepThrough, XmlType(AnonymousType=true), GeneratedCode("xsd", "2.0.50727.3038"), DesignerCategory("code")]
    public class podcastUrl
    {
        private podcastUrlKey keyField;
        private string valueField;

        [XmlAttribute]
        public podcastUrlKey key
        {
            get
            {
                return this.keyField;
            }
            set
            {
                this.keyField = value;
            }
        }

        [XmlText]
        public string Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }
}


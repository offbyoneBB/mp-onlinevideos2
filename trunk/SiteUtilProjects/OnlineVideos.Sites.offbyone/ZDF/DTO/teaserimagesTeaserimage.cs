namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, GeneratedCode("xsd", "2.0.50727.3038"), XmlType(AnonymousType=true), DebuggerStepThrough, DesignerCategory("code")]
    public class teaserimagesTeaserimage
    {
        private string altField;
        private string keyField;
        private string valueField;

        [XmlAttribute]
        public string alt
        {
            get
            {
                return this.altField;
            }
            set
            {
                this.altField = value;
            }
        }

        [XmlAttribute]
        public string key
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


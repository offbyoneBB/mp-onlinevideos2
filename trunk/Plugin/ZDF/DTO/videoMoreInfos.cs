namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, DebuggerStepThrough, XmlType(AnonymousType=true), GeneratedCode("xsd", "2.0.50727.3038"), DesignerCategory("code")]
    public class videoMoreInfos
    {
        private string titleField;
        private string urlField;

        public string title
        {
            get
            {
                return this.titleField;
            }
            set
            {
                this.titleField = value;
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
    }
}


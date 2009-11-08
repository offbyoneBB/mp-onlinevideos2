namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, DesignerCategory("code"), XmlType(AnonymousType=true), GeneratedCode("xsd", "2.0.50727.3038"), DebuggerStepThrough]
    public class videoCaption
    {
        private string offsetField;
        private string urlField;

        public string offset
        {
            get
            {
                return this.offsetField;
            }
            set
            {
                this.offsetField = value;
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


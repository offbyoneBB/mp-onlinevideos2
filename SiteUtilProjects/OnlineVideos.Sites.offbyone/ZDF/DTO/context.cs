namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, XmlType(AnonymousType=true), DebuggerStepThrough, XmlRoot(Namespace="", IsNullable=false), GeneratedCode("xsd", "2.0.50727.3038"), DesignerCategory("code")]
    public class context
    {
        private string contextLinkField;

        [XmlElement(DataType="anyURI")]
        public string contextLink
        {
            get
            {
                return this.contextLinkField;
            }
            set
            {
                this.contextLinkField = value;
            }
        }
    }
}


namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, DesignerCategory("code"), XmlRoot(Namespace="", IsNullable=false), DebuggerStepThrough, GeneratedCode("xsd", "2.0.50727.3038"), XmlType(AnonymousType=true)]
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


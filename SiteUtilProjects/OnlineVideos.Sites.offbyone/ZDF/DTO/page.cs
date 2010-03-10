namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, GeneratedCode("xsd", "2.0.50727.3038"), XmlType(AnonymousType=true), DebuggerStepThrough, DesignerCategory("code"), XmlRoot(Namespace="", IsNullable=false)]
    public class page
    {
        private pagePageElement[] pageElementField;

        [XmlElement("pageElement")]
        public pagePageElement[] pageElement
        {
            get
            {
                return this.pageElementField;
            }
            set
            {
                this.pageElementField = value;
            }
        }
    }
}


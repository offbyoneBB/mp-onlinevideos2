namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, XmlType(AnonymousType=true), XmlRoot(Namespace="", IsNullable=false), GeneratedCode("xsd", "2.0.50727.3038"), DebuggerStepThrough, DesignerCategory("code")]
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


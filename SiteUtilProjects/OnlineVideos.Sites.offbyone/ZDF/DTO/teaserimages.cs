namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, GeneratedCode("xsd", "2.0.50727.3038"), DebuggerStepThrough, DesignerCategory("code"), XmlRoot(Namespace="", IsNullable=false), XmlType(AnonymousType=true)]
    public class teaserimages
    {
        private teaserimagesTeaserimage[] teaserimageField;

        [XmlElement("teaserimage")]
        public teaserimagesTeaserimage[] teaserimage
        {
            get
            {
                return this.teaserimageField;
            }
            set
            {
                this.teaserimageField = value;
            }
        }
    }
}


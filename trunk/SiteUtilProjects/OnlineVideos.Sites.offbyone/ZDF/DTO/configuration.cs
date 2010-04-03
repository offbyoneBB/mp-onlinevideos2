namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, DesignerCategory("code"), XmlType(AnonymousType=true), XmlRoot(Namespace="", IsNullable=false), DebuggerStepThrough, GeneratedCode("xsd", "2.0.50727.3038")]
    public class configuration
    {
        private value[] dictionaryField;
        private value[] systemField;

        [XmlArrayItem("value", IsNullable=false)]
        public value[] dictionary
        {
            get
            {
                return this.dictionaryField;
            }
            set
            {
                this.dictionaryField = value;
            }
        }

        [XmlArrayItem("value", IsNullable=false)]
        public value[] system
        {
            get
            {
                return this.systemField;
            }
            set
            {
                this.systemField = value;
            }
        }
    }
}


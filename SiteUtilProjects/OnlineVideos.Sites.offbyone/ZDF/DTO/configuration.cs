namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, XmlRoot(Namespace="", IsNullable=false), DesignerCategory("code"), XmlType(AnonymousType=true), GeneratedCode("xsd", "2.0.50727.3038"), DebuggerStepThrough]
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


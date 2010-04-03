namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, XmlType(AnonymousType=true), GeneratedCode("xsd", "2.0.50727.3038"), DebuggerStepThrough, DesignerCategory("code")]
    public class teaserlistSearchResultType
    {
        private string numberField;
        private string valueField;

        [XmlAttribute(DataType="integer")]
        public string number
        {
            get
            {
                return this.numberField;
            }
            set
            {
                this.numberField = value;
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


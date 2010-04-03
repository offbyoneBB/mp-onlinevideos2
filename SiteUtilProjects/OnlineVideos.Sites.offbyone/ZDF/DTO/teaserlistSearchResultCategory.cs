namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, DebuggerStepThrough, GeneratedCode("xsd", "2.0.50727.3038"), XmlType(AnonymousType=true), DesignerCategory("code")]
    public class teaserlistSearchResultCategory
    {
        private string idField;
        private string numberField;
        private string valueField;

        [XmlAttribute]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

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


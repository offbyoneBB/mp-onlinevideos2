namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, DesignerCategory("code"), GeneratedCode("xsd", "2.0.50727.3038"), XmlType(AnonymousType=true), DebuggerStepThrough]
    public class channellistChannel
    {
        private channellistChannelInitialLetter initialLetterField;
        private bool valueField;

        [XmlAttribute]
        public channellistChannelInitialLetter initialLetter
        {
            get
            {
                return this.initialLetterField;
            }
            set
            {
                this.initialLetterField = value;
            }
        }

        [XmlText]
        public bool Value
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


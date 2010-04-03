namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, GeneratedCode("xsd", "2.0.50727.3038"), XmlType(AnonymousType=true), XmlRoot(Namespace="", IsNullable=false), DebuggerStepThrough, DesignerCategory("code")]
    public class channellist
    {
        private channellistChannel[] channelField;

        [XmlElement("channel")]
        public channellistChannel[] channel
        {
            get
            {
                return this.channelField;
            }
            set
            {
                this.channelField = value;
            }
        }
    }
}


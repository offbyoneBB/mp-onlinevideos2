namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, XmlType(AnonymousType=true), GeneratedCode("xsd", "2.0.50727.3038"), DebuggerStepThrough, DesignerCategory("code")]
    public class pagePageElement
    {
        private ZDFMediathek2009.Code.DTO.teaser teaserField;
        private string textField;
        private string titleField;

        public ZDFMediathek2009.Code.DTO.teaser teaser
        {
            get
            {
                return this.teaserField;
            }
            set
            {
                this.teaserField = value;
            }
        }

        public string text
        {
            get
            {
                return this.textField;
            }
            set
            {
                this.textField = value;
            }
        }

        public string title
        {
            get
            {
                return this.titleField;
            }
            set
            {
                this.titleField = value;
            }
        }
    }
}


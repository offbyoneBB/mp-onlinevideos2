namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, XmlRoot(Namespace="", IsNullable=false), GeneratedCode("xsd", "2.0.50727.3038"), DebuggerStepThrough, DesignerCategory("code"), XmlType(AnonymousType=true)]
    public class information
    {
        private string detailField;
        private string shortTitleField;
        private string titleField;

        public string detail
        {
            get
            {
                return this.detailField;
            }
            set
            {
                this.detailField = value;
            }
        }

        public string shortTitle
        {
            get
            {
                return this.shortTitleField;
            }
            set
            {
                this.shortTitleField = value;
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


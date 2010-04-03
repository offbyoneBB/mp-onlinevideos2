namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, XmlType(AnonymousType=true), XmlRoot(Namespace="", IsNullable=false), DebuggerStepThrough, DesignerCategory("code"), GeneratedCode("xsd", "2.0.50727.3038")]
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


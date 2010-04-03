namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, XmlType(AnonymousType=true), DesignerCategory("code"), XmlRoot(Namespace="", IsNullable=false), DebuggerStepThrough, GeneratedCode("xsd", "2.0.50727.3038")]
    public class status
    {
        private string debuginfoField;
        private statusStatuscode statuscodeField;

        public string debuginfo
        {
            get
            {
                return this.debuginfoField;
            }
            set
            {
                this.debuginfoField = value;
            }
        }

        public statusStatuscode statuscode
        {
            get
            {
                return this.statuscodeField;
            }
            set
            {
                this.statuscodeField = value;
            }
        }
    }
}


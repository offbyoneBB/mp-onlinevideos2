namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [Serializable, DebuggerStepThrough, GeneratedCode("xsd", "2.0.50727.3038"), DesignerCategory("code"), XmlType(AnonymousType=true)]
    public class teaserlistSearchResult
    {
        private int batchField;
        private teaserlistSearchResultBroadcast[] broadcastsField;
        private teaserlistSearchResultCategory[] categoriesField;
        private teaserlistSearchResultProperty[] propertiesField;
        private teaserlistSearchResultStation[] stationsField;
        private teaserlistSearchResultType[] typesField;

        public int batch
        {
            get
            {
                return this.batchField;
            }
            set
            {
                this.batchField = value;
            }
        }

        [XmlArrayItem("broadcast", IsNullable=false)]
        public teaserlistSearchResultBroadcast[] broadcasts
        {
            get
            {
                return this.broadcastsField;
            }
            set
            {
                this.broadcastsField = value;
            }
        }

        [XmlArrayItem("category", IsNullable=false)]
        public teaserlistSearchResultCategory[] categories
        {
            get
            {
                return this.categoriesField;
            }
            set
            {
                this.categoriesField = value;
            }
        }

        [XmlArrayItem("property", IsNullable=false)]
        public teaserlistSearchResultProperty[] properties
        {
            get
            {
                return this.propertiesField;
            }
            set
            {
                this.propertiesField = value;
            }
        }

        [XmlArrayItem("station", IsNullable=false)]
        public teaserlistSearchResultStation[] stations
        {
            get
            {
                return this.stationsField;
            }
            set
            {
                this.stationsField = value;
            }
        }

        [XmlArrayItem("type", IsNullable=false)]
        public teaserlistSearchResultType[] types
        {
            get
            {
                return this.typesField;
            }
            set
            {
                this.typesField = value;
            }
        }
    }
}


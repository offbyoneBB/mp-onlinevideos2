namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.Xml.Serialization;

    [Serializable, GeneratedCode("xsd", "2.0.50727.3038"), XmlType(AnonymousType=true)]
    public enum videoFormitaetQuality
    {
        hd = 0,
        high = 2,
        low = 3,
        [XmlEnum("very high")]
        veryhigh = 1
    }
}


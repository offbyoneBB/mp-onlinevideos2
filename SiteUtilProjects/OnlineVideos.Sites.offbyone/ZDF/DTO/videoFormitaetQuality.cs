namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.Xml.Serialization;

    [Serializable, XmlType(AnonymousType=true), GeneratedCode("xsd", "2.0.50727.3038")]
    public enum videoFormitaetQuality
    {
        hd = 0,
        high = 2,
        low = 4,
        med = 3,
        [XmlEnum("veryhigh")]
        OBSOLETE_veryhigh = 5,
        [XmlEnum("very high")]
        OBSOLETE_veryhigh2 = 6,
        [XmlEnum("very-high")]
        veryhigh = 1
    }
}


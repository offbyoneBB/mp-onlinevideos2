namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.Xml.Serialization;

    [Serializable, GeneratedCode("xsd", "2.0.50727.3038"), XmlType(AnonymousType=true)]
    public enum statusStatuscode
    {
        ok,
        error,
        notFound,
        mailNotSent,
        wrongParameter,
        missingParameter,
        geolocation,
        fsk
    }
}


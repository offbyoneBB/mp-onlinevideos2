namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.Xml.Serialization;

    [Serializable, XmlType(AnonymousType=true), GeneratedCode("xsd", "2.0.50727.3038"), XmlRoot(Namespace="", IsNullable=false)]
    public enum type
    {
        einzelsendung,
        thema,
        sendung,
        topthema,
        rubrik,
        @event,
        video,
        livevideo,
        podcast,
        link,
        imageseries_informativ,
        imageseries_emotional,
        imageseries_informativaudio,
        imageseries_emotionalaudio,
        interactive_360,
        interactive_basic,
        interactive_infografik,
        interactive_quiz,
        interactive_liveticker
    }
}


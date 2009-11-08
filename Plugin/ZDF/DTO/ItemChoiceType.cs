namespace ZDFMediathek2009.Code.DTO
{
    using System;
    using System.CodeDom.Compiler;
    using System.Xml.Serialization;

    [Serializable, GeneratedCode("xsd", "2.0.50727.3038"), XmlType(IncludeInSchema=false)]
    public enum ItemChoiceType
    {
        channellist,
        configuration,
        feedlist,
        imageseries,
        interactive,
        ivwUrls,
        mainnavigation,
        mceUpdate,
        page,
        session,
        skindetails,
        suggestlist,
        teaserlist,
        video
    }
}


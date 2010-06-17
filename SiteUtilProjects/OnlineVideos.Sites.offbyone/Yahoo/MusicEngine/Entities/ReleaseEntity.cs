using System;
using System.Collections.Generic;
using System.Text;

namespace YahooMusicEngine.Entities
{
  public class ReleaseEntity : Service
  {
    private string title;

    public string Title
    {
      get { return title; }
      set { title = value; }
    }

    public int Year { get; set; }

    public override void ParseXml(System.Xml.XmlNode node)
    {
      base.ParseXml(node);
      Title = ParseXmlAttributeAsString(node, "title", string.Empty);
      Year = ParseXmlAttributeAsInt(node, "releaseYear", 0);
    }

    public ReleaseEntity()
    {
      Title = string.Empty;
      Year = 0;
    }


    public ReleaseEntity(System.Xml.XmlNode node)
    {
      Title = string.Empty;
      ParseXml(node);
    }
  }
}

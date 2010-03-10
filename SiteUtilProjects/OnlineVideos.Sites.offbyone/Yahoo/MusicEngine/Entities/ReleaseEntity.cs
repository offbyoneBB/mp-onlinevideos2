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


    public override void ParseXml(System.Xml.XmlNode node)
    {
      base.ParseXml(node);
      Title = ParseXmlAttributeAsString(node, "title", string.Empty);
    }

    public ReleaseEntity()
    {
      Title = string.Empty;
    }


    public ReleaseEntity(System.Xml.XmlNode node)
    {
      Title = string.Empty;
      ParseXml(node);
    }
  }
}

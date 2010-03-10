using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;

namespace YahooMusicEngine.Entities
{
  public class ImageEntity : Service
  {
    public ImageEntity(XmlNode node)
    {
      ParseXml(node);
    }

    public ImageEntity()
    {
      Url = string.Empty;
      size = 0;
    }

    private string url;
    public string Url
    {
      get { return url; }
      set { url = value; }
    }


    private int size;

    public int Size
    {
      get { return size; }
      set { size = value; }
    }

    public override void ParseXml(XmlNode node)
    {
      Url = ParseXmlAttributeAsString(node, "url", string.Empty);
      Size = ParseXmlAttributeAsInt(node, "size", 0);
      base.ParseXml(node);
    }
  }
}

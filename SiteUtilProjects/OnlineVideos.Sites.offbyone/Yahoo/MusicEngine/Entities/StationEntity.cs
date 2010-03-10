using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace YahooMusicEngine.Entities
{
  public class StationEntity : Service
  {
    private string itemType;

    public string ItemType
    {
      get { return itemType; }
      set { itemType = value; }
    }

    private string stationType;

    public string StationType
    {
      get { return stationType; }
      set { stationType = value; }
    }

    public override void ParseXml(XmlNode node)
    {
      base.ParseXml(node);
      ItemType = ParseXmlAttributeAsString(node, "itemType", string.Empty);
      StationType = ParseXmlAttributeAsString(node, "stationType", string.Empty);
    }

    public StationEntity()
    {
    }

    public StationEntity(XmlNode node)
    {
      ParseXml(node);
    }
  }
}

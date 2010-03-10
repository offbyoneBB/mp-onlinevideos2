using System;
using System.Collections.Generic;
using System.Text;

namespace YahooMusicEngine.Entities
{
  public class SpellResultEntity:Service
  {
    private string token;

    public string Token
    {
      get { return token; }
      set { token = value; }
    }

    private string suggestion;

    public string Suggestion
    {
      get { return suggestion; }
      set { suggestion = value; }
    }

    public SpellResultEntity()
    {
      Token = string.Empty;
      Suggestion = string.Empty;
    }

    public SpellResultEntity(System.Xml.XmlNode node)
    {
      ParseXml(node);
    }

    public override void ParseXml(System.Xml.XmlNode node)
    {
      this.Token = ParseXmlAttributeAsString(node, "token", string.Empty);
      this.Suggestion = ParseXmlAttributeAsString(node, "suggestion", string.Empty);
      base.ParseXml(node);
    }

  }
}

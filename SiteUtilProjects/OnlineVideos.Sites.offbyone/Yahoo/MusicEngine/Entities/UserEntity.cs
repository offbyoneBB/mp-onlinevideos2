using System;
using System.Collections.Generic;
using System.Text;

namespace YahooMusicEngine.Entities
{
  public class UserEntity: Service
  {

    private bool isMember;

    public bool IsMember
    {
      get { return isMember; }
      set { isMember = value; }
    }

    private string ymid;

    public string Ymid
    {
      get { return ymid; }
      set { ymid = value; }
    }

    private string countryCode;

    public string CountryCode
    {
      get { return countryCode; }
      set { countryCode = value; }
    }


    public UserEntity()
    {
      IsMember = false;
      Ymid = string.Empty;
      CountryCode = "us";
    }

    public UserEntity(System.Xml.XmlNode node)
    {
      ParseXml(node);
    }

    public override void ParseXml(System.Xml.XmlNode node)
    {
      base.ParseXml(node);
      this.IsMember = ParseXmlAttributeAsBool(node, "isMember", false);
      this.Ymid = ParseXmlAttributeAsString(node, "ymid", string.Empty);
      this.CountryCode = ParseXmlAttributeAsString(node, "countryCode", string.Empty);
      if (this.Ymid == "0")
        this.Ymid = string.Empty;
    }
  }
}

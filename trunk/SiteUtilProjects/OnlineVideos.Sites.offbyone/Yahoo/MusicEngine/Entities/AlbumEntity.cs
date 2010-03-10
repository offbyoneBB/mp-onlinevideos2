using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;

namespace YahooMusicEngine.Entities
{
  public class AlbumEntity : Service
  {
    private ReleaseEntity release;

    public ReleaseEntity Release
    {
      get { return release; }
      set { release = value; }
    }

    public override void ParseXml(System.Xml.XmlNode node)
    {
      base.ParseXml(node);
      XmlNode releasenodes = node.SelectSingleNode("Release");
      if (releasenodes != null)
        Release.ParseXml(releasenodes);
    }

    public AlbumEntity()
    {
      Release = new ReleaseEntity();
    }

    public AlbumEntity(System.Xml.XmlNode node)
    {
      Release = new ReleaseEntity();
      ParseXml(node);
    }

  }
}

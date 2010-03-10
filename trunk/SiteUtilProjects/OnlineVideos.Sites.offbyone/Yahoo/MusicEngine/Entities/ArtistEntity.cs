using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;

namespace YahooMusicEngine.Entities
{
  public class ArtistEntity : Service
  {
    private string sortName;

    public string SortName
    {
      get { return sortName; }
      set { sortName = value; }
    }

    private int trackCount;

    /// <summary>
    /// Gets or sets the track count.
    /// </summary>
    /// <value>The track count.</value>
    public int TrackCount
    {
      get { return trackCount; }
      set { trackCount = value; }
    }

    private string website;

    public string Website
    {
      get { return website; }
      set { website = value; }
    }

    private int flags;

    public int Flags
    {
      get { return flags; }
      set { flags = value; }
    }


    public override void ParseXml(System.Xml.XmlNode node)
    {
      this.SortName = ParseXmlAttributeAsString(node, "sortName", string.Empty);
      this.Website = ParseXmlAttributeAsString(node, "website", string.Empty);
      this.TrackCount = ParseXmlAttributeAsInt(node, "trackCount", 0);
      this.Flags = ParseXmlAttributeAsInt(node, "flags", 0);
      base.ParseXml(node);
    }

    public ArtistEntity(XmlNode node)
    {
      ParseXml(node);
    }
    
    public ArtistEntity()
    {
      this.SortName =string.Empty;
      this.Website = string.Empty;
      this.TrackCount = 0;
      this.Flags = 0;

    }
  }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace YahooMusicEngine.Entities
{
  public class CategoryEntity : Service
  {
    
    private int videoCount;
    public int VideoCount
    {
      get { return videoCount; }
      set { videoCount = value; }
    }

    private int artistCount;

    public int ArtistCount
    {
      get { return artistCount; }
      set { artistCount = value; }
    }

    private int releaseCount;

    public int ReleaseCount
    {
      get { return releaseCount; }
      set { releaseCount = value; }
    }

    private int trackCount;

    public int TrackCount
    {
      get { return trackCount; }
      set { trackCount = value; }
    }

    private bool hasVideoStation;

    public bool HasVideoStation
    {
      get { return hasVideoStation; }
      set { hasVideoStation = value; }
    }

    private bool hasAudioStation;

    public bool HasAudioStation
    {
      get { return hasAudioStation; }
      set { hasAudioStation = value; }
    }

    private List<CategoryEntity> childs;

    public List<CategoryEntity> Childs
    {
      get { return childs; }
      set { childs = value; }
    }
    
    private CategoryEntity parent;
    public CategoryEntity Parent
    {
      get { return parent; }
      set { parent = value; }
    }

    public CategoryEntity()
    {
      this.ArtistCount = 0;
      this.HasAudioStation = false;
      this.HasVideoStation = false;
      this.Id = string.Empty;
      this.Name = string.Empty;
      this.Rating = -1;
      this.ReleaseCount = 0;
      this.TrackCount = 0;
      this.VideoCount = 0;
      this.Childs = new List<CategoryEntity>();
    }

    public CategoryEntity(System.Xml.XmlNode node)
    {
      ParseXml(node);
      this.Childs = new List<CategoryEntity>();
    }

    private CategoryTreeTypes type;

    public CategoryTreeTypes Type
    {
      get { return type; }
      set { type = value; }
    }

    /// <summary>
    /// Parses the XML.
    /// </summary>
    /// <param name="node">The node.</param>
    public override void ParseXml(System.Xml.XmlNode node)
    {
      base.ParseXml(node);
      Name = ParseXmlAttributeAsString(node, "name", string.Empty);
      string sType = ParseXmlAttributeAsString(node, "type", string.Empty);
      switch (sType.ToLower())
      {
        case "genre":
         Type=CategoryTreeTypes.genre;
         break;
        case "theme":
          Type = CategoryTreeTypes.theme;
          break;
        default:
          Type = CategoryTreeTypes.era;
          break;
      }
      VideoCount = ParseXmlAttributeAsInt(node, "videoCount", 0);
      ArtistCount = ParseXmlAttributeAsInt(node, "artistCount", 0);
      ReleaseCount = ParseXmlAttributeAsInt(node, "releaseCount", 0);
      TrackCount = ParseXmlAttributeAsInt(node, "trackCount", 0);
      HasVideoStation = ParseXmlAttributeAsBool(node, "hasVideoStation", false);
      HasAudioStation = ParseXmlAttributeAsBool(node, "hasAudioStation", false);
    }


  }
}

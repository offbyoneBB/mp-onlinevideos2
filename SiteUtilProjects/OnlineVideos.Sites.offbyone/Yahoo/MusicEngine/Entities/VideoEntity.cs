using System;
using System.Collections.Generic;
using System.Text;

namespace YahooMusicEngine.Entities
{
  public class VideoEntity:Service 
  {
    private int copyrightYear;

    public int CopyrightYear
    {
      get { return copyrightYear; }
      set { copyrightYear = value; }
    }

    private int duration;

    /// <summary>
    /// 	Duration of the video, in seconds.
    /// </summary>
    /// <value>The duration.</value>
    public int Duration
    {
      get { return duration; }
      set { duration = value; }
    }

    private bool _explicit ;

    /// <summary>
    /// Whether or not the release contains explicit lyrics
    /// </summary>
    /// <value><c>true</c> if explicit; otherwise, <c>false</c>.</value>
    public bool Explicit
    {
      get { return _explicit; }
      set { _explicit = value; }
    }

    private int flags;

    /// <summary>
    /// Bitmask of video flags. Can be a combination of the possible values:
    ///
    ///
    ///      1: images (whether we have images in general)
    ///        To test for a flag in XPath, you can do [floor(@flags div FLAGVALUE) mod 2 = 1]. FLAGVALUE is the value of the flag you're looking for. Example: * /Video[floor(@flags div 1) mod 2 = 1] gets all videos with images.
    /// 
    /// 
    /// 
    /// </summary>
    /// <value>The flags.</value>
    public int Flags
    {
      get { return flags; }
      set { flags = value; }
    }

    private string label;

    /// <summary>
    /// 	Record label name
    /// </summary>
    /// <value>The label.</value>
    public string Label
    {
      get { return label; }
      set { label = value; }
    }

    private bool localOnly;

    public bool LocalOnly
    {
      get { return localOnly; }
      set { localOnly = value; }
    }

    private string rights;

    public string Rights
    {
      get { return rights; }
      set { rights = value; }
    }

    private string title;

    public string Title
    {
      get { return title; }
      set { title = value; }
    }

    private string typeID;

    public string TypeID
    {
      get { return typeID; }
      set { typeID = value; }
    }

    public override void ParseXml(System.Xml.XmlNode node)
    {
      base.ParseXml(node);
      this.CopyrightYear = ParseXmlAttributeAsInt(node, "copyrightYear", 0);
      this.Duration = ParseXmlAttributeAsInt(node, "duration", 0);
      this.Explicit = ParseXmlAttributeAsBool(node, "explicit", false);
      this.Flags = ParseXmlAttributeAsInt(node, "flags", 0);
      this.Label = ParseXmlAttributeAsString(node, "label", string.Empty);
      this.LocalOnly = ParseXmlAttributeAsBool(node, "localOnly", false);
      this.Rights = ParseXmlAttributeAsString(node, "rights", string.Empty);
      this.Title = ParseXmlAttributeAsString(node, "title", string.Empty);
      this.TypeID = ParseXmlAttributeAsString(node, "typeID", string.Empty);
    }

    public VideoEntity(System.Xml.XmlNode node)
    {
      ParseXml(node);
    }
    
    public VideoEntity()
    {

    }
  }
}

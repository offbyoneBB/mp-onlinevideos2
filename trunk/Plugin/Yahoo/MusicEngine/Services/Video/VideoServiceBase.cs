using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using YahooMusicEngine.Entities;

namespace YahooMusicEngine.Services
{
  public class VideoServiceBase:IService
  {
    private string category;

    public string Category
    {
      get { return category; }
      set { category = value; }
    }

    private List<VideoResponse> items;
    public List<VideoResponse> Items
    {
      get { return items; }
      set { items = value; }
    }

    private int start;
    public int Start
    {
      get { return start; }
      set { start = value; }
    }


    private int count;

    public int Count
    {
      get { return count; }
      set { count = value; }
    }

    private int total;

    public int Total
    {
      get { return total; }
      set { total = value; }
    }

    private string description;

    public string Description
    {
      get { return description; }
      set { description = value; }
    }

    public virtual void Parse(System.Xml.XmlDocument doc)
    {
      Items.Clear();
      XmlNode body = doc.SelectSingleNode("Videos");
      Service ser = new Service();
      Total = ser.ParseXmlAttributeAsInt(body, "total", 0);
      Description = ser.ParseXmlAttributeAsString(body, "description", string.Empty);
      Start = ser.ParseXmlAttributeAsInt(body, "start", 1);
      Count = ser.ParseXmlAttributeAsInt(body, "count", 25);
      XmlNodeList bodynodes = doc.SelectNodes("Videos/Video");
      foreach (XmlNode node in bodynodes)
      {
        VideoResponse it = new VideoResponse();
        XmlNode artistnode = node.SelectSingleNode("Artist");
        if (artistnode != null)
        {
          it.Artist = new ArtistEntity(artistnode);
        }
        XmlNode imagenode = node.SelectSingleNode("Image");
        if (imagenode != null)
        {
          it.Image = new ImageEntity(imagenode);
        }
        XmlNodeList categorynodes = node.SelectNodes("Category");
        foreach (XmlNode catnode in categorynodes)
        {
          it.Categories.Add(new CategoryEntity(catnode));
        }

        XmlNodeList albumnodes = node.SelectNodes("Album");
        foreach (XmlNode albnode in albumnodes)
        {
          it.Albums.Add(new AlbumEntity(albnode));
        }

        it.Video.ParseXml(node);
        Items.Add(it);
      }
    }



    #region IService Members

    public string ServiceName
    {
      get { return "video"; }
    }

    public virtual string Resource
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public virtual Dictionary<string, string> Params
    {
      get
      {
        throw new Exception("The method or operation is not implemented.");
      }
      set
      {
        throw new Exception("The method or operation is not implemented.");
      }
    }

    #endregion
  }
}

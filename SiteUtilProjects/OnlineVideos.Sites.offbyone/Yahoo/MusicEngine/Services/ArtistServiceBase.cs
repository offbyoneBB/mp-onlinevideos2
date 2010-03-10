using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using YahooMusicEngine.Entities;


namespace YahooMusicEngine.Services
{
  public class ArtistServiceBase
  {
    private List<ArtistResponse> items;

    public List<ArtistResponse> Items
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

    private string response;

    /// <summary>
    /// Comma-separated list of artist response chunks to return
    /// </summary>
    /// <value>The response.</value>
    public string Response
    {
      get { return response; }
      set { response = value; }
    }

    public virtual Dictionary<string, string> Params
    {
      get
      {
        Dictionary<string, string> param = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(Response))
          param.Add("response", this.Response);
        //if (this.Count != 25)
        //  param.Add("count", this.Count.ToString());
        return param;
      }
      set
      {
        throw new Exception("The method or operation is not implemented.");
      }
    }

    public virtual void Parse(System.Xml.XmlDocument doc)
    {
      Items.Clear();
      XmlNodeList bodynodes = doc.SelectNodes("Artists/Artist");
      foreach (XmlNode node in bodynodes)
      {
        ArtistResponse at = new ArtistResponse();
        XmlNodeList videonodes = node.SelectNodes("Video");
        foreach (XmlNode vidnode in videonodes)
        {
          at.Videos.Add(new VideoEntity(vidnode));
        }
        XmlNodeList artistnode = node.SelectNodes("TopSimilarArtists/Artist");
        foreach (XmlNode artnode in artistnode)
        {
          at.TopSimilarArtists.Add(new ArtistEntity(artnode));
        }
        XmlNode imagenode = node.SelectSingleNode("Image");
        if (imagenode != null)
        {
          at.Image = new ImageEntity(imagenode);
        }

        at.Artist.ParseXml(node);
        Items.Add(at);
      }
    }
  }
}

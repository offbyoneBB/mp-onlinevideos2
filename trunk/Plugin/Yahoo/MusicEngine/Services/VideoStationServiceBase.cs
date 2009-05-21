using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using YahooMusicEngine.Entities;

namespace YahooMusicEngine.Services
{
  public class VideoStationServiceBase
  {
    private List<StationResponse> items;

    public List<StationResponse> Items
    {
      get { return items; }
      set { items = value; }
    }

    public string ServiceName
    {
      get { return "station"; }
    }

    public VideoStationServiceBase()
    {
      Items = new List<StationResponse>();
    }

    public virtual void Parse(System.Xml.XmlDocument doc)
    {
      Items.Clear();
      XmlNodeList bodynodes = doc.SelectNodes("Stations/Station");
      foreach (XmlNode node in bodynodes)
      {
        StationResponse st = new StationResponse();
        st.Station.ParseXml(node);
        Items.Add(st);
      }
    }
  }
}

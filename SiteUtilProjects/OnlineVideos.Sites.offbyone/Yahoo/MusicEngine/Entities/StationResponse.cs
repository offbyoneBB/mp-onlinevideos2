using System;
using System.Collections.Generic;
using System.Text;

namespace YahooMusicEngine.Entities
{
  public class StationResponse
  {
    private StationEntity station;

    public StationEntity Station
    {
      get { return station; }
      set { station = value; }
    }
    public StationResponse()
    {
      Station = new StationEntity();
    }
  }
}

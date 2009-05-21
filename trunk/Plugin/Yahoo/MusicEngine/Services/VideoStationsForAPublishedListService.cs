using System;
using System.Collections.Generic;
using System.Text;

namespace YahooMusicEngine.Services
{
  public class VideoStationsForAPublishedListService: VideoStationServiceBase, IService
  {
    #region IService Members

    public string Resource
    {
      get { return string.Format("video/list/published/{0}", Id); }
    }

    private string  id;

    public string  Id
    {
      get { return id; }
      set { id = value; }
    }

    public Dictionary<string, string> Params
    {
      get
      {
        Dictionary<string, string> param = new Dictionary<string, string>();
        return param;
      }
      set
      {
        throw new Exception("The method or operation is not implemented.");
      }
    }

    #endregion
    public VideoStationsForAPublishedListService()
      : base()
    {
      Id = string.Empty;
    }
  }
}

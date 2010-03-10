using System;
using System.Collections.Generic;
using System.Text;

namespace YahooMusicEngine.Services
{
  public class ArtistByIdService : ArtistServiceBase, IService
  {


    #region IService Members

    public string ServiceName
    {
      get { return "artist"; }
    }

    public string Resource
    {
      get { return string.Format("item/{0}", Id); }
    }

    private string id;

    /// <summary>
    /// Comma-separated list of artist IDs.
    /// </summary>
    /// <value>The id.</value>
    public string Id
    {
      get { return id; }
      set { id = value; }
    }

    public ArtistByIdService()
    {
      this.Items = new List<YahooMusicEngine.Entities.ArtistResponse>();
      Response = "topsimilar,videos";
      Count = 25;
    }

    #endregion
  }
}

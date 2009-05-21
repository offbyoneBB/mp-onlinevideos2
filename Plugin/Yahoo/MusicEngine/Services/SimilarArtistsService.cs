using System;
using System.Collections.Generic;
using System.Text;
using YahooMusicEngine.Entities;

namespace YahooMusicEngine.Services
{
  public class SimilarArtistsService : ArtistServiceBase, IService
  {
      #region IService Members

    public string ServiceName
    {
      get { return "artist"; }
    }

    public string Resource
    {
      get { return string.Format("list/similar/{0}", Id); }
    }
    #endregion

    private string id;

    /// 
    /// <summary>
    /// Artist ID to return similar artists for
    /// </summary>
    /// <value>The id.</value>
    public string Id
    {
      get { return id; }
      set { id = value; }
    }

    public SimilarArtistsService()
    {
      this.Items = new List<YahooMusicEngine.Entities.ArtistResponse>();
      Response = string.Empty;
      Count = 25;
    }


  }
}

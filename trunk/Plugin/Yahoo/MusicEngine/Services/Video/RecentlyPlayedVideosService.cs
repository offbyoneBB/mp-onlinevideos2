using System;
using System.Collections.Generic;
using System.Text;

namespace YahooMusicEngine.Services
{
  public class RecentlyPlayedVideosService : VideoServiceBase, IService
  {
    #region IService Members

    public override string Resource
    {
      get { return string.Format("list/recentlyplayed/{0}", this.Ymid); }
    }

    private string ymid;

    /// <summary>
    /// Yahoo! Music User ID
    /// </summary>
    /// <value>The ymid.</value>
    public string Ymid
    {
      get { return ymid; }
      set { ymid = value; }
    }

    public override Dictionary<string, string> Params
    {
      get
      {
        return new Dictionary<string, string>();
      }
      set
      {
        throw new Exception("The method or operation is not implemented.");
      }
    }

    #endregion

    public RecentlyPlayedVideosService()
    {
      Items = new List<YahooMusicEngine.Entities.VideoResponse>();
    }
  }
}

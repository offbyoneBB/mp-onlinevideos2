using System;
using System.Collections.Generic;
using System.Text;

namespace YahooMusicEngine.Services
{
  public class UserFavoriteVideosService : VideoServiceBase, IService
  {
    #region IService Members

    public override string Resource
    {
      get { return string.Format("list/favorites/{0}/{1}", this.Ymid, this.Ids); }
    }

    private string ymid;

    public string Ymid
    {
      get { return ymid; }
      set { ymid = value; }
    }
    
    private string ids;

    /// <summary>
    /// Saves the specified video(s) to the user's favorite videos.
    /// </summary>
    /// <value>The ids.</value>
    public string Ids
    {
      get { return ids; }
      set { ids = value; }
    }


    public override Dictionary<string, string> Params
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

    public UserFavoriteVideosService()
    {
      Items = new List<YahooMusicEngine.Entities.VideoResponse>();
      Ymid = string.Empty;
    }
  }
}

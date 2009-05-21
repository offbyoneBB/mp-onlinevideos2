using System;
using System.Collections.Generic;
using System.Text;

namespace YahooMusicEngine.Services
{
  public class UserFavoriteVideosReadService : VideoServiceBase, IService
  {
    #region IService Members

    public override string Resource
    {
      get { return string.Format("list/favorites/{0}", this.Ymid); }
    }

    private string ymid;

    public string Ymid
    {
      get { return ymid; }
      set { ymid = value; }
    }


    public override Dictionary<string, string> Params
    {
      get
      {
        Dictionary<string, string> param = new Dictionary<string, string>();
        if (this.Count != 25)
          param.Add("count", this.Count.ToString());
        return param;
      }
      set
      {
        throw new Exception("The method or operation is not implemented.");
      }
    }

    #endregion

    public UserFavoriteVideosReadService()
    {
      Items = new List<YahooMusicEngine.Entities.VideoResponse>();
      Ymid = string.Empty;
      Count = 25;
    }
  }
}
